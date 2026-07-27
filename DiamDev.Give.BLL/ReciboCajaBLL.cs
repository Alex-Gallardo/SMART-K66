using System;
using System.Collections.Generic;
using System.Linq;
using DiamDev.Give.DAL;
using DiamDev.Give.Entities;

namespace DiamDev.Give.BLL
{
    public class ReciboCajaBLL
    {
        private readonly APK66Context _apk;
        private readonly HanaRepository _hana;

        public ReciboCajaBLL()
        {
            _apk = new APK66Context();
            _hana = new HanaRepository();
        }

        // ─── USUARIOS ────────────────────────────────
        /// <summary>
        /// [LEGACY] Mantener por compatibilidad. El flujo nuevo usa ObtenerPlantaPorLogin.
        /// </summary>
        public string ObtenerPlantaUsuario(string idUsr) =>
            _apk.ObtenerPlantaUsuario(idUsr);

        /// <summary>
        /// [OBSOLETO — NO USAR] Apunta a REC_CAJA_USUARIOS, tabla inexistente en la BD actual
        /// (confirmado 2026: "Invalid object name 'REC_CAJA_USUARIOS'"). El flujo de recibos
        /// usa ObtenerDeptoSerie(usuarioId). Conservado solo hasta verificar que nada más lo llame.
        /// </summary>
        public string ObtenerPlantaPorLogin(string login)
        {
            string planta = _apk.ObtenerPlantaPorLogin(login);
            if (string.IsNullOrWhiteSpace(planta))
                throw new System.Exception(
                    $"El usuario '{login}' no está vinculado a un usuario de caja activo en APK66 " +
                    $"(REC_CAJA_USUARIOS), o no tiene PLANTA asignada. " +
                    $"Contacte al administrador para habilitarlo.");
            return planta;
        }

        /// <summary>Devuelve el ID_USR canónico de APK66 (mayúsculas) o el login si no hay vínculo.</summary>
        public string ObtenerIdUsrPorLogin(string login)
        {
            string idUsr = _apk.ObtenerIdUsrPorLogin(login);
            return string.IsNullOrWhiteSpace(idUsr) ? (login ?? "").ToUpper() : idUsr;
        }

        // ─── EMPRESAS DISPONIBLES POR USUARIO ─────────
        /// <summary>
        /// Empresas de RECIBOS del usuario + los OPERADORES (códigos) de cada una.
        /// Cada operador incluye su Depto (Usuario_Empresa.DEPTO_RECIBO), que es el
        /// DEPTO con el que se numerará el recibo en REC_CAJA_SERIES.
        /// Solo se devuelven operadores con DEPTO_RECIBO asignado; una empresa sin
        /// ningún operador válido NO se devuelve (no tendría nada que ofrecer).
        /// </summary>
        public List<dynamic> ObtenerEmpresasUsuario(long usuarioId)
        {
            var permitidas = new Dictionary<long, string>
            {
                { UsuarioEmpresaBL.ID_GRACO, "GRACO" },
                { UsuarioEmpresaBL.ID_FAES,  "FAES"  },
                { UsuarioEmpresaBL.ID_BOLIK, "BOLIK" }
            };

            var nombres = new Dictionary<string, string>
            {
                { "GRACO", "Graco Pack"       },
                { "FAES",  "Fabrica Escocesa" },
                { "BOLIK", "Industrias Bolik" }
            };

            var registros = new UsuarioEmpresaDA().ObtenerPorUsuarioId(usuarioId);
            var ueBl = new UsuarioEmpresaBL();

            var resultado = new List<dynamic>();

            foreach (var grupo in registros
                         .Where(r => permitidas.ContainsKey(r.EmpresaId))
                         .GroupBy(r => r.EmpresaId))
            {
                string clave = permitidas[grupo.Key];

                var codigos = grupo
                    .Where(r => !string.IsNullOrWhiteSpace(r.Codigo))
                    // GroupBy por Codigo = Distinct que conserva el registro completo
                    // (necesitamos DEPTO_RECIBO, no solo el string del código)
                    .GroupBy(r => r.Codigo.Trim(), StringComparer.OrdinalIgnoreCase)
                   .Select(g =>
                   {
                       var reg = g.First();
                       var p = ueBl.ParseCodigo(reg.Codigo);
                       string depto = (reg.DEPTO_RECIBO ?? "").Trim();
                       return new
                       {
                           Codigo = reg.Codigo.Trim(),
                           SapId = p.SapId,
                           Agente = p.AgenteNombre,
                           Depto = depto,
                           Serie = depto.Length > 0
                                       ? _apk.ObtenerSerieDeDepto(clave, depto)
                                       : "",
                           // Flag de venta de mostrador: la regla vive AQUÍ (BLL),
                           // el front solo la consume. Un solo lugar que mantener.
                           EncabezadoEditable = string.Equals(
                               p.AgenteNombre, OPERADOR_ENCABEZADO_EDITABLE,
                               StringComparison.OrdinalIgnoreCase)
                       };
                   })
                    // Solo operadores habilitados para recibos: DEPTO_RECIBO con valor.
                    // NULL o vacío = no emite → no aparece en "Operar como".
                    .Where(c => c.Depto.Length > 0)
                    .ToList();

                // Empresa sin operadores válidos: no se ofrece en el select.
                // (Elegirla solo llevaría al aviso "no tiene operadores".)
                if (codigos.Count == 0) continue;

                resultado.Add(new
                {
                    Id = clave,
                    Nombre = nombres[clave],
                    Codigos = codigos
                });
            }

            return resultado;
        }

        // ─── CLIENTES (HANA) ─────────────────────────
        /// <summary>
        /// Trae todos los clientes del agente para esa empresa desde HANA,
        /// luego filtra localmente (igual que el desktop con el ListBox).
        /// El parámetro 'filtro' puede ser código o nombre parcial.
        /// </summary>
        public List<ClienteHana> BuscarClientes(string empresa, string agente, string filtro)
        {
            var todos = _hana.BuscarClientes(empresa, agente);
            if (string.IsNullOrWhiteSpace(filtro)) return todos.Take(50).ToList();

            var f = filtro.ToUpper();
            return todos
                .Where(c => c.CardCode.ToUpper().Contains(f) || c.CardName.ToUpper().Contains(f))
                .Take(30)
                .ToList();
        }

        /// <summary>
        /// Enruta la fuente según el tipo:
        ///   FACTURA / PEDIDO → SAP HANA (vista RC_FACTURAS_REC_CAJ)
        ///   el resto         → SQL (MA_RECC_DOCTOS), como antes.
        /// Además, enriquece cada documento con:
        ///   - MontoPendiente:   comprometido en recibos en tránsito
        ///                       (PENDIENTES completos + líneas ANULADO_SAP de DESCUADRES).
        ///   - PendienteRecibos: en qué recibos está comprometido (tooltip del modal).
        /// El controller y el front no cambian de firma.
        /// </summary>
        public List<DocumentoRecibo> ObtenerDocumentos(string empresa, string clienteId, string tipoDoc)
        {
            var tipo = (tipoDoc ?? "").Trim().ToUpper();

            // ── Corto-circuito: tipos sin referencia documental ──
            // ANTICIPO / DIFERENCIA / SALDO PENDIENTE no tienen catálogo que
            // consultar. El front no debería llamar acá (la lupa está disabled),
            // pero el endpoint es público: sin esta guarda, un GET manual con
            // tipoDoc=DIFERENCIA se iría a MA_RECC_DOCTOS a buscar filas que no
            // existen. Devolver vacío es la respuesta correcta, no un error.
            if (TiposDocumentoRecibo.EsSinDocumento(tipo))
                return new List<DocumentoRecibo>();

            List<DocumentoRecibo> lista =
                TiposDocumentoRecibo.EsConsultableHana(tipo)
                    ? _hana.ObtenerFacturas(empresa, clienteId, tipo)
                    : _apk.ObtenerDocumentos(empresa, clienteId, tipo);

            // ── Merge de pendientes SQL sobre los documentos de la lista ──
            // Un solo viaje a SQL; lookup O(1) por documento. Si el cálculo
            // falla, NO tumbamos el modal: los docs salen con pendiente 0.
            // MONEDA DUAL: el pendiente se toma en LA MISMA moneda del documento
            // (MontoUsd para facturas USD, MontoGtq para GTQ) para que la resta
            // Saldo − Pendiente del modal sea siempre peras con peras.
            try
            {
                var pendientes = _apk.ObtenerPendientesPorDocumento(empresa, clienteId, tipo);
                if (pendientes.Count > 0)
                {
                    foreach (var d in lista)
                    {
                        var key = (d.NoDocumento ?? "").Trim();
                        if (key.Length > 0 && pendientes.TryGetValue(key, out PendienteDocumento p))
                        {
                            bool docEsUsd = "USD".Equals((d.Moneda ?? "").Trim(),
                                                         StringComparison.OrdinalIgnoreCase);
                            d.MontoPendiente = docEsUsd ? p.MontoUsd : p.MontoGtq;
                            d.PendienteRecibos = string.Join(", ", p.Recibos);
                        }
                    }
                }
            }
            catch
            {
                // Informativo, no bloqueante: preferimos mostrar el modal sin
                // la columna calculada a romper la búsqueda de documentos.
            }

            return lista;
        }

        /// <summary>
        /// Anticipos EN TRÁNSITO del cliente para la barra informativa del modal.
        /// Informativo, no bloqueante: si falla devuelve null y el modal sale sin barra.
        /// </summary>
        public AnticipoTransito ObtenerAnticiposTransito(string empresa, string clienteId)
        {
            try { return _apk.ObtenerAnticiposTransito(empresa ?? "", clienteId ?? ""); }
            catch { return null; }
        }

        /// <summary>
        /// Devuelve el tipo de cambio USD vigente para una empresa (referencia para la UI).
        /// Usa la fecha de hoy. El guardado vuelve a traerlo con la fecha del recibo.
        /// </summary>
        public decimal ObtenerTipoCambioDia(string empresa)
        {
            return _hana.ObtenerTipoCambio(empresa, DateTime.Today);
        }

        // ─── GUARDAR RECIBO ───────────────────────────
        /// <summary>
        /// Valida las reglas de negocio y guarda el recibo completo.
        /// Reglas extraídas del btnGuardar_Click del desktop:
        ///   1. Debe tener al menos un cobro y un documento.
        ///   2. Si monedas iguales → saldo debe ser 0.
        ///   3. Si monedas distintas → se guarda con advertencia (saldo permitido).
        /// </summary>
        public ResultadoRecibo GuardarRecibo(
            ReciboCajaEncabezado enc, string depto,
            long usuarioId, string usuarioLogin, string ipUsuario)
        {
            try
            {
                if (enc.Cobros == null || !enc.Cobros.Any())
                    return ResultadoRecibo.Error("Debe agregar al menos un cobro.");
                if (enc.Documentos == null || !enc.Documentos.Any())
                    return ResultadoRecibo.Error("Debe agregar al menos un documento.");
                if (string.IsNullOrWhiteSpace(enc.NombreCliente))
                    return ResultadoRecibo.Error("Debe seleccionar un cliente.");

                // ── Fecha del cobro OBLIGATORIA (todas las formas de pago) ──
                // La fecha representa CUÁNDO SE RECIBIÓ EL DINERO, así que aplica
                // igual a efectivo que a cheque o transferencia. Antes solo se
                // exigía para los no-efectivo y el DAL forzaba NULL en EFECTIVO.
                //
                // Esta es la validación REAL: la del front (Index.cshtml) es UX.
                // El POST se puede armar a mano con Postman y llegar hasta acá.
                // NO se valida el rango de la fecha: el negocio permite cualquier
                // fecha (pasada o futura), es decisión del cajero.
                var cobroSinFecha = enc.Cobros.FirstOrDefault(c => !c.FechaDoc.HasValue);
                if (cobroSinFecha != null)
                    return ResultadoRecibo.Error(
                        $"El cobro de tipo '{cobroSinFecha.TipoCobro}' no tiene fecha. " +
                        $"La fecha del cobro es obligatoria para todas las formas de pago.");

                // ── 0. Validar el CÓDIGO con el que opera (Usuario_Empresa) ──
                // El operador (CodigoUsuario) ya fue validado por ObtenerDeptoOperador
                // en el controller (pertenencia + depto parseado + serie existente).
                // Aquí solo normalizamos para grabarlo limpio.
                enc.CodigoUsuario = (enc.CodigoUsuario ?? "").Trim();

                // ── 1. Traer el tipo de cambio de SAP (al guardar), por fecha del recibo ──
                decimal tc;
                try
                {
                    tc = _hana.ObtenerTipoCambio(enc.IdEmpresa, enc.FechaRecibo);
                }
                catch (Exception exTc)
                {
                    return ResultadoRecibo.Error("No se pudo obtener el tipo de cambio de SAP: " + exTc.Message);
                }
                enc.TipoCambio = tc;
                enc.MonedaBase = "GTQ";

                // ★ FIX (moneda '##'): antes era NormalizarMonedaApp(enc.Moneda), que
                // solo corregía QTZ/Q y dejaba pasar cualquier otra cosa. El typeahead
                // de clientes copia OCRD.Currency de SAP al encabezado, y para socios
                // MULTIMONEDA ese campo vale '##' — así se colaron 11 recibos.
                enc.Moneda = NormalizarMonedaEncabezado(enc);

                // ── 2. Calcular los duales de cada cobro ──
                // ★ FIX: se normaliza la moneda de la LÍNEA antes de calcular, para que
                // lo que se graba en REC_CAJA_COBRO.MONEDA coincida con la moneda que
                // realmente usó CalcularMontosDuales. Antes, un "QTZ" se calculaba como
                // GTQ pero se guardaba como "QTZ": la etiqueta mentía sobre el cálculo.
                foreach (var c in enc.Cobros)
                {
                    c.Moneda = NormalizarMonedaLinea(c.Moneda);
                    var m = CalcularMontosDuales(c.Monto, c.Moneda, tc);
                    c.TipoCambio = tc; c.MontoGtq = m.Gtq; c.MontoUsd = m.Usd;
                }
                // ── 3. Calcular los duales de cada documento ──
                foreach (var d in enc.Documentos)
                {
                    d.Moneda = NormalizarMonedaLinea(d.Moneda);   // ★ FIX
                    var m = CalcularMontosDuales(d.Monto, d.Moneda, tc);
                    d.TipoCambio = tc; d.MontoGtq = m.Gtq; d.MontoUsd = m.Usd;
                }

                // ── 4. Totales en moneda original (legado) ──
                enc.MontoTotalRecibo = enc.Cobros.Sum(c => c.Monto);
                enc.MontoTotalDoc = enc.Documentos.Sum(d => d.Monto);
                enc.Saldo = enc.MontoTotalRecibo - enc.MontoTotalDoc;

                // ── 5. Totales DUALES (sumando líneas ya convertidas) ──
                enc.MontoTotalRecGtq = enc.Cobros.Sum(c => c.MontoGtq);
                enc.MontoTotalRecUsd = enc.Cobros.Sum(c => c.MontoUsd);
                enc.MontoTotalDocGtq = enc.Documentos.Sum(d => d.MontoGtq);
                enc.MontoTotalDocUsd = enc.Documentos.Sum(d => d.MontoUsd);
                enc.SaldoGtq = enc.MontoTotalRecGtq - enc.MontoTotalDocGtq;
                enc.SaldoUsd = enc.MontoTotalRecUsd - enc.MontoTotalDocUsd;

                // ── 6. Normalizar saldos y validar el cuadre ──
                // Redondeo a 2 decimales + clamp del residuo (±0.01) a CERO EXACTO.
                // Sin esto, los redondeos por línea dejaban saldos de "-0.01"/"-0.00"
                // que pasaban la tolerancia y se GRABABAN negativos. Regla:
                // recibo cuadrado = SALDO 0.00 positivo, siempre.
                enc.Saldo = NormalizarSaldo(enc.Saldo);
                enc.SaldoGtq = NormalizarSaldo(enc.SaldoGtq);
                enc.SaldoUsd = NormalizarSaldo(enc.SaldoUsd);

                if (enc.SaldoGtq != 0m)
                    return ResultadoRecibo.Error(
                        $"El saldo en GTQ no cuadra (Q{enc.SaldoGtq:N2}). " +
                        $"Cobros: Q{enc.MontoTotalRecGtq:N2} / Documentos: Q{enc.MontoTotalDocGtq:N2}.");

                // ── 7. Guardar (transacción ADO.NET con columnas duales) ──
                _apk.GuardarReciboCompleto(enc, depto);

                // ── 8. Analytics: evento CREADO (no tumba el guardado si falla) ──
                string payload = Newtonsoft.Json.JsonConvert.SerializeObject(enc);
                _apk.RegistrarEventoAnalytics(
                    "CREADO", enc.IdRecibo, enc.IdEmpresa, depto,
                    usuarioId, usuarioLogin, enc.Moneda, tc,
                    enc.MontoTotalRecGtq, enc.MontoTotalRecUsd, enc.SaldoGtq,
                    payload, ipUsuario);

                return ResultadoRecibo.Ok(enc.IdRecibo);
            }
            catch (Exception ex)
            {
                return ResultadoRecibo.Error("Error al guardar: " + ex.Message);
            }
        }

        // Operador especial de venta de mostrador: con él, el encabezado del
        // recibo (cliente, dirección, NIT, agente, correo) es capturable a mano.
        // Se compara contra el NOMBRE parseado del código ("1-SALA DE VENTAS" →
        // "SALA DE VENTAS"), así funciona en las 3 empresas aunque el número cambie.
        private const string OPERADOR_ENCABEZADO_EDITABLE = "SALA DE VENTAS";
        // Límites del motivo de anulación (espejo del front; el máximo = tamaño
        // real de la columna MOTIVO en REC_CAJA_ENC: nvarchar(150))
        private const int MIN_MOTIVO_ANULACION = 10;
        private const int MAX_MOTIVO_ANULACION = 150;

        // ─────────────────────────────────────────────
        // ANULAR RECIBO — reglas de negocio:
        //   1. Motivo obligatorio (>= MIN_MOTIVO_ANULACION caracteres reales).
        //   2. El recibo debe existir y no estar ya anulado.
        //   3. NO se puede anular si ya fue OPERADO en SAP o está en
        //      DESCUADRE: primero Créditos debe anular el pago en SAP.
        // Deja triple rastro: MOTIVO + ANULADO_POR + FECHA_ANULACION en el
        // encabezado, y evento ANULADO en analyticsRecibos (motivo en payload).
        // ─────────────────────────────────────────────
        public ResultadoRecibo AnularRecibo(
            string idRecibo, string empresa, string motivo,
            long usuarioId, string usuarioLogin, string ipUsuario)
        {
            var res = new ResultadoRecibo { Exito = false, IdRecibo = idRecibo };

            if (string.IsNullOrWhiteSpace(idRecibo) || string.IsNullOrWhiteSpace(empresa))
            {
                res.Mensaje = "Debe indicar el número de recibo y la empresa.";
                return res;
            }

            // ── Validación del motivo (espejo de la del front) ──
            motivo = (motivo ?? "").Trim();
            if (motivo.Length < MIN_MOTIVO_ANULACION)
            {
                res.Mensaje = "Debe indicar el motivo de la anulación (mínimo " +
                              MIN_MOTIVO_ANULACION + " caracteres).";
                return res;
            }
            if (motivo.Length > MAX_MOTIVO_ANULACION)
                motivo = motivo.Substring(0, MAX_MOTIVO_ANULACION);

            var rec = BuscarRecibo(idRecibo, empresa);
            if (rec == null)
            {
                res.Mensaje = "El recibo " + idRecibo + " no existe para la empresa " + empresa + ".";
                return res;
            }

            if ("X".Equals((rec.Status ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
            {
                res.Mensaje = "El recibo " + idRecibo + " ya está anulado" +
                              (string.IsNullOrWhiteSpace(rec.AnuladoPor)
                                  ? "."
                                  : " (por " + rec.AnuladoPor +
                                    (rec.FechaAnulacion.HasValue
                                        ? " el " + rec.FechaAnulacion.Value.ToString("dd/MM/yyyy HH:mm")
                                        : "") + ").");
                return res;
            }

            string sync = (rec.SyncEstado ?? "").Trim().ToUpperInvariant();
            if (sync == "OPERADO")
            {
                res.Mensaje = "No se puede anular: el recibo ya fue OPERADO en SAP" +
                              (rec.SapDocNum.HasValue ? " (Pago No. " + rec.SapDocNum + ")" : "") +
                              ". Solicite a Créditos anular primero el pago en SAP.";
                return res;
            }
            if (sync == "DESCUADRE")
            {
                res.Mensaje = "No se puede anular: el recibo está en DESCUADRE con SAP. " +
                              "Debe resolverse la conciliación antes de anularlo.";
                return res;
            }

            int filas = _apk.AnularRecibo(idRecibo, empresa, usuarioLogin, motivo);
            if (filas == 0)
            {
                res.Mensaje = "El recibo no pudo anularse (posiblemente ya fue anulado por otro usuario).";
                return res;
            }

            // ── Analytics: evento ANULADO con el motivo en el payload ──
            // ★ FIX: el DEPTO ya no viaja NULL. REC_CAJA_ENC no lo guarda, pero el
            // ID del recibo lleva el prefijo de serie ("RG12-08542" → "RODOLFO"),
            // así que se deriva. Sin esto, las anulaciones quedaban sin cobrador y
            // el ranking por depto de Analytics las perdía.
            string deptoAnul = _apk.ObtenerDeptoDeRecibo(idRecibo, empresa);

            string payload = Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                Motivo = motivo,
                SyncEstadoAlAnular = rec.SyncEstado,
                MontoTRec = rec.MontoTotalRecibo,
                Moneda = rec.Moneda
            });
            _apk.RegistrarEventoAnalytics(
                "ANULADO", idRecibo, empresa,
                string.IsNullOrWhiteSpace(deptoAnul) ? null : deptoAnul,   // ★ FIX
                usuarioId, usuarioLogin, rec.Moneda, rec.TipoCambio,
                rec.MontoTotalRecGtq, rec.MontoTotalRecUsd, rec.SaldoGtq,
                payload, ipUsuario);

            res.Exito = true;
            res.Mensaje = "Recibo " + idRecibo + " anulado correctamente.";
            return res;
        }

        // ─── BUSCAR RECIBO ────────────────────────────
        public ReciboCajaEncabezado BuscarRecibo(string idRecibo, string empresa) =>
            _apk.BuscarRecibo(idRecibo, empresa);

        // ─── EMPRESAS DISPONIBLES ─────────────────────

        /// <summary>
        /// [LEGACY] Catálogo fijo de las 3 empresas. Mantener por si algo lo usa,
        /// pero la vista ahora se llena con ObtenerEmpresasUsuario (filtrado por permiso).
        /// </summary>
        public List<dynamic> ObtenerEmpresas()
        {
            return new List<dynamic>
            {
                new { Id = "GRACO", Nombre = "Graco Pack",       Permiso = "Control.ReciboCaja.Graco", Clase = "empresa-graco" },
                new { Id = "FAES",  Nombre = "Fabrica Escocesa", Permiso = "Control.ReciboCaja.Faes",  Clase = "empresa-faes"  },
                new { Id = "BOLIK", Nombre = "Industrias Bolik", Permiso = "Control.ReciboCaja.Bolik", Clase = "empresa-bolik" }
                // ★ FIX: se quitó la entrada "TEST_GRACO" (dato de prueba). Estamos en
                // producción y este método es público: cualquier consumo futuro habría
                // ofrecido una empresa inexistente.
            };
        }

        /// <summary>
        /// Resuelve el DEPTO de numeración del OPERADOR elegido (Usuario_Empresa).
        ///
        /// Valida (con errores claros, en orden):
        ///   1. Que venga un código.
        ///   2. Que el código pertenezca al usuario logueado para ESA empresa
        ///      (seguridad: el POST se puede falsificar; la UI es cosmética).
        ///   3. Que el operador tenga DEPTO_RECIBO asignado (= habilitado para emitir).
        ///   4. Que exista la serie (EMPRESA, DEPTO) en REC_CAJA_SERIES.
        ///
        /// El DEPTO viene DECLARADO en Usuario_Empresa.DEPTO_RECIBO. Ya no se
        /// parsea del Codigo: el parseo daba "RODOLFO DIAZ" y la serie se llama
        /// "RODOLFO" — nunca empataban. Ahora el vínculo es explícito y por datos.
        /// </summary>
        public string ObtenerDeptoOperador(long usuarioId, string empresa, string codigo)
        {
            codigo = (codigo ?? "").Trim();
            string emp = (empresa ?? "").Trim().ToUpper();

            if (codigo.Length == 0)
                throw new Exception("Debe seleccionar el operador con el que emitirá el recibo.");

            if (!_empresaIds.TryGetValue(emp, out long empId))
                throw new Exception("Empresa no válida para recibos: '" + empresa + "'.");

            var reg = new UsuarioEmpresaDA()
                .ObtenerPorUsuarioId(usuarioId)
                .FirstOrDefault(r => r.EmpresaId == empId &&
                                     string.Equals((r.Codigo ?? "").Trim(), codigo,
                                                   StringComparison.OrdinalIgnoreCase));

            if (reg == null)
                throw new Exception("El operador '" + codigo + "' no está asignado a su usuario " +
                                    "para la empresa " + emp + ".");

            // DEPTO declarado, no derivado.
            string depto = (reg.DEPTO_RECIBO ?? "").Trim();
            if (depto.Length == 0)
                throw new Exception("El operador '" + codigo + "' no tiene DEPTO_RECIBO asignado " +
                                    "en Usuario_Empresa: no está habilitado para emitir recibos. " +
                                    "Contacte al administrador.");

            if (!_apk.ExisteSerie(emp, depto))
                throw new Exception("No existe serie de numeración para la empresa " + emp +
                                    " con el depto '" + depto + "' (REC_CAJA_SERIES). " +
                                    "Contacte al administrador.");

            return depto;
        }

        // ─── CÁLCULO DUAL DE MONEDA ───────────────────────────
        /// <summary>
        /// Dada una línea (monto + moneda original + tipo de cambio), devuelve
        /// los equivalentes en GTQ y USD aplicando la "regla de oro":
        ///   - El monto en la moneda ORIGINAL es exacto.
        ///   - El equivalente en la otra moneda se DERIVA (redondeado a 2 decimales).
        ///
        /// En TS sería:
        ///   (monto, moneda, tc) => moneda==='USD'
        ///       ? { gtq: round2(monto*tc), usd: monto }
        ///       : { gtq: monto, usd: round2(monto/tc) }
        /// </summary>
        public static MontosDuales CalcularMontosDuales(decimal monto, string moneda, decimal tipoCambio)
        {
            if (tipoCambio <= 0)
                throw new Exception("Tipo de cambio inválido (<= 0) al calcular montos duales.");

            bool esUsd = (moneda ?? "").Trim().ToUpper() == "USD";

            if (esUsd)
                return new MontosDuales
                {
                    Gtq = Math.Round(monto * tipoCambio, 2),
                    Usd = monto
                };
            else
                return new MontosDuales
                {
                    Gtq = monto,
                    Usd = Math.Round(monto / tipoCambio, 2)
                };
        }

        // Tolerancia de cuadre por redondeo de conversiones (1 centavo)
        private const decimal TOLERANCIA_SALDO = 0.01m;

        /// <summary>
        /// Redondea un saldo a 2 decimales y colapsa el residuo de redondeo
        /// (|saldo| ≤ 1 centavo) a CERO EXACTO. Así nunca se graba ni se
        /// muestra un "-0.01"/"-0.00" en un recibo que en realidad cuadra.
        /// En TS: s => Math.abs(round2(s)) <= 0.01 ? 0 : round2(s)
        /// </summary>
        private static decimal NormalizarSaldo(decimal saldo)
        {
            saldo = Math.Round(saldo, 2);
            return Math.Abs(saldo) <= TOLERANCIA_SALDO ? 0.00m : saldo;
        }

        /// <summary>Normaliza el código de moneda de la app: QTZ/Q → GTQ. USD pasa igual.</summary>
        private static string NormalizarMonedaApp(string moneda)
        {
            var m = (moneda ?? "").Trim().ToUpper();
            return (m == "QTZ" || m == "Q") ? "GTQ" : m;
        }

        // ─── ★ FIX · NORMALIZACIÓN ESTRICTA DE MONEDA ──────────────
        // NormalizarMonedaApp es una LISTA BLANCA de correcciones conocidas
        // (QTZ/Q → GTQ): todo lo demás pasa intacto. Eso está bien para limpiar
        // alias, pero no valida. Los dos métodos de abajo sí cierran el conjunto
        // a {GTQ, USD}, que son los únicos valores que el sistema entiende.

        /// <summary>
        /// Moneda del ENCABEZADO. Si no es GTQ ni USD, se deriva de las líneas
        /// (cobros primero, luego documentos), que son la fuente confiable: salen
        /// de los selects del formulario, no de SAP.
        ///
        /// El caso real: SAP B1 usa '##' en OCRD.Currency para marcar socios de
        /// negocio MULTIMONEDA. INF_CLIENTES_REC lo devuelve, el typeahead lo copia
        /// al encabezado, y así se grabaron 11 recibos con MONEDA = '##'.
        ///
        /// Último recurso: GTQ, la moneda base del sistema.
        /// </summary>
        public static string NormalizarMonedaEncabezado(ReciboCajaEncabezado enc)
        {
            if (enc == null) return "GTQ";

            string m = NormalizarMonedaApp(enc.Moneda);
            if (m == "GTQ" || m == "USD") return m;

            if (enc.Cobros != null)
                foreach (var c in enc.Cobros)
                {
                    string mc = NormalizarMonedaApp(c.Moneda);
                    if (mc == "GTQ" || mc == "USD") return mc;
                }

            if (enc.Documentos != null)
                foreach (var d in enc.Documentos)
                {
                    string md = NormalizarMonedaApp(d.Moneda);
                    if (md == "GTQ" || md == "USD") return md;
                }

            return "GTQ";
        }

        /// <summary>
        /// Moneda de una LÍNEA (cobro o documento). Solo hay dos valores válidos.
        /// Cualquier otra cosa cae a GTQ — que es exactamente lo que ya hacía
        /// CalcularMontosDuales con su `esUsd ? ... : else GTQ`. Acá solo alineamos
        /// la ETIQUETA con el cálculo: ningún monto cambia.
        /// </summary>
        private static string NormalizarMonedaLinea(string moneda)
        {
            return NormalizarMonedaApp(moneda) == "USD" ? "USD" : "GTQ";
        }

        // Mapa inverso: clave string de recibos → Id numérico de Usuario_Empresa
        private static readonly Dictionary<string, long> _empresaIds =
            new Dictionary<string, long>
            {
        { "GRACO", UsuarioEmpresaBL.ID_GRACO },
        { "FAES",  UsuarioEmpresaBL.ID_FAES  },
        { "BOLIK", UsuarioEmpresaBL.ID_BOLIK }
            };
    }

    /// <summary>
    /// Resultado de CalcularMontosDuales.
    /// En TS: type MontosDuales = { gtq: number; usd: number }
    /// </summary>
    public class MontosDuales
    {
        public decimal Gtq { get; set; }
        public decimal Usd { get; set; }
    }

}