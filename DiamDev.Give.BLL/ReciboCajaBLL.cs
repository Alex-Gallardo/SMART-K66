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
        /// Cada operador incluye su Depto (Usuario_Empresa.SERIE_SAP), que es el
        /// DEPTO con el que se numerará el recibo en REC_CAJA_SERIES.
        /// Depto vacío = operador NO habilitado para emitir (el front lo avisa y
        /// el guardado lo rechaza).
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
                    // (necesitamos SERIE_SAP, no solo el string del código)
                    .GroupBy(r => r.Codigo.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(g =>
                    {
                        var reg = g.First();
                        var p = ueBl.ParseCodigo(reg.Codigo);
                        return new
                        {
                            Codigo = reg.Codigo.Trim(),
                            SapId = p.SapId,
                            // El texto del código ("12-RODOLFO" → "RODOLFO") cumple
                            // DOBLE rol: es el AGENTE (filtra clientes en HANA) y es
                            // el DEPTO (numera la serie en REC_CAJA_SERIES).
                            // SERIE_SAP NO se usa aquí: vincula con SAP, irrelevante
                            // para recibos.
                            Agente = p.AgenteNombre,
                            Depto = p.AgenteNombre
                        };
                    })
                    .ToList();

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

            List<DocumentoRecibo> lista =
                (tipo == "FACTURA" || tipo == "PEDIDO")
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
                enc.Moneda = NormalizarMonedaApp(enc.Moneda);

                // ── 2. Calcular los duales de cada cobro ──
                foreach (var c in enc.Cobros)
                {
                    var m = CalcularMontosDuales(c.Monto, c.Moneda, tc);
                    c.TipoCambio = tc; c.MontoGtq = m.Gtq; c.MontoUsd = m.Usd;
                }
                // ── 3. Calcular los duales de cada documento ──
                foreach (var d in enc.Documentos)
                {
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

                // ── 6. VALIDACIÓN NUEVA: el saldo en GTQ debe cuadrar a 0 ──
                // (reemplaza la vieja regla "monedas iguales -> saldo 0").
                // Tolerancia de 1 centavo por redondeo de conversiones.
                if (Math.Abs(enc.SaldoGtq) > 0.01m)
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
            string payload = Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                Motivo = motivo,
                SyncEstadoAlAnular = rec.SyncEstado,
                MontoTRec = rec.MontoTotalRecibo,
                Moneda = rec.Moneda
            });
            _apk.RegistrarEventoAnalytics(
                "ANULADO", idRecibo, empresa, null,
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
                new { Id = "BOLIK", Nombre = "Industrias Bolik", Permiso = "Control.ReciboCaja.Bolik", Clase = "empresa-bolik" },
                new  { Id= "TEST_GRACO", Nombre = "test GRaco pack ", Permiso = "Control.ReciboCaja.TestGraco", Clase = "test-empresa-graco"}
            };
        }

        /// <summary>
        /// Resuelve el DEPTO de serie del usuario POS (para armar el ID del recibo).
        /// Reemplaza al viejo ObtenerPlantaPorLogin (que leía de APK66).
        /// Lanza error claro si el usuario no está habilitado para recibos.
        /// </summary>
        public string ObtenerDeptoSerie(long usuarioId)
        {
            string depto = new RecibosCajaUsuarioDeptoDA().ObtenerDeptoPorUsuarioId(usuarioId);
            if (string.IsNullOrWhiteSpace(depto))
                throw new Exception(
                    "El usuario no está habilitado para emitir recibos de caja " +
                    "(sin DEPTO de serie asignado). Contacte al administrador.");
            return depto;
        }

        /// <summary>
        /// Resuelve el DEPTO de numeración del OPERADOR elegido (Usuario_Empresa).
        /// Reemplaza a ObtenerDeptoSerie(usuarioId) en el flujo de guardado:
        /// el depto ya no depende del usuario logueado, sino del operador.
        ///
        /// Valida (con errores claros, en orden):
        ///   1. Que venga un código.
        ///   2. Que el código pertenezca al usuario logueado para ESA empresa
        ///      (seguridad: el POST se puede falsificar; la UI es cosmética).
        ///   3. Que el operador tenga SERIE_SAP (depto) asignado.
        ///   4. Que exista la serie (EMPRESA, DEPTO) en REC_CAJA_SERIES.
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

            // El DEPTO es el texto del código: "12-RODOLFO" → "RODOLFO", "JORGE" → "JORGE".
            // (ParseCodigo ya maneja ambos formatos, con y sin guion.)
            string depto = new UsuarioEmpresaBL().ParseCodigo(reg.Codigo).AgenteNombre;
            if (string.IsNullOrWhiteSpace(depto))
                throw new Exception("El operador '" + codigo + "' no tiene un depto válido " +
                                    "(código vacío o mal formado en Usuario_Empresa). " +
                                    "Contacte al administrador.");
            depto = depto.Trim();

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

        /// <summary>Normaliza el código de moneda de la app: QTZ/Q → GTQ. USD pasa igual.</summary>
        private static string NormalizarMonedaApp(string moneda)
        {
            var m = (moneda ?? "").Trim().ToUpper();
            return (m == "QTZ" || m == "Q") ? "GTQ" : m;
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