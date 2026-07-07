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
        /// Devuelve solo las empresas de RECIBOS (GRACO/FAES/BOLIK) a las que el
        /// usuario tiene acceso según Usuario_Empresa (POS-SmartK66_DEV).
        ///
        /// Reutiliza UsuarioEmpresaDA/BL que ya existen. Filtra fuera EMPAQUES
        /// (...002) y cualquier otra empresa que recibos no maneja, y deduplica
        /// (Usuario_Empresa puede traer varias filas por empresa, una por agente).
        /// </summary>
        public List<dynamic> ObtenerEmpresasUsuario(long usuarioId)
        {
            // IDs numéricos que recibos sí maneja → su clave string para el front
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

            // Deduplicar por EmpresaId y quedarnos solo con las permitidas
            var idsUnicos = registros
                .Select(r => r.EmpresaId)
                .Where(id => permitidas.ContainsKey(id))
                .Distinct();

            var resultado = new List<dynamic>();
            foreach (var id in idsUnicos)
            {
                string clave = permitidas[id];
                resultado.Add(new { Id = clave, Nombre = nombres[clave] });
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
        ///   el resto         → APK66 (MA_RECC_DOCTOS), como antes.
        /// Además, enriquece cada documento con MontoPendiente: cuánto está
        /// comprometido en recibos PENDIENTES en SQL (incluye anulados-en-SAP).
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
            try
            {
                var pendientes = _apk.ObtenerPendientesPorDocumento(empresa, clienteId, tipo);
                if (pendientes.Count > 0)
                {
                    foreach (var d in lista)
                    {
                        var key = (d.NoDocumento ?? "").Trim();
                        if (key.Length > 0 && pendientes.TryGetValue(key, out decimal p))
                            d.MontoPendiente = p;
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