using System;
using System.Collections.Generic;
using System.Data;
using DiamDev.Give.Entities;
using System.Data.Odbc;
using System.Linq;
using System.Configuration;   // ← NUEVO: para leer AppSettings (ConfigurationManager)

namespace DiamDev.Give.DAL
{
    /// <summary>
    /// Repositorio HANA. NO usa HanaConnection directo — delega en HanaHelper
    /// (ya configurado con AppSettings HANA_Server / HANA_User / etc.).
    ///
    /// El mapeo empresa → schema HANA ahora vive en configuración (AppSettings),
    /// no hardcodeado. Así el cutover prod↔pruebas es cambiar el .config, sin recompilar.
    /// </summary>
    public class HanaRepository
    {
        // ─────────────────────────────────────────────
        // CLIENTES (SP INF_CLIENTES_REC) — sin cambios funcionales
        // ─────────────────────────────────────────────
        public List<ClienteHana> BuscarClientes(string empresa, string agente)
        {
            var lista = new List<ClienteHana>();

            string schema = ResolverSchema(empresa);
            if (schema == null) return lista; // empresa desconocida

            string query = string.Format(
                "CALL \"{0}\".\"INF_CLIENTES_REC\"('{1}')",
                schema,
                Esc(agente ?? ""));

            try
            {
                DataTable dt = HanaHelper.EjecutarConsulta(query);
                foreach (DataRow row in dt.Rows)
                {
                    lista.Add(new ClienteHana
                    {
                        CardCode = LeerCampo(row, "CardCode"),
                        CardName = LeerCampo(row, "CardName"),
                        Address = LeerCampo(row, "Address"),
                        LicTradNum = LeerCampo(row, "LicTradNum"),
                        SlpName = LeerCampo(row, "SlpName"),
                        Email = LeerCampo(row, "E_mail"),
                        Currency = NormalizarMoneda(LeerCampo(row, "Currency"))
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception(
                    string.Format("Error HANA al buscar clientes ({0} / {1}): {2}",
                        empresa, schema, ex.Message), ex);
            }

            return lista;
        }

        /// <summary>
        /// Devuelve, de un lote de IDs de recibo, cuáles ya están operados en SAP
        /// (existen en ORCT con Canceled='N'). Si un ID tiene varias filas activas
        /// (se anuló y se rehízo), se queda con el DocEntry más reciente.
        /// </summary>
        public List<SapCobroAplicado> ObtenerCobrosOperados(string empresa, List<string> idsRecibo)
        {
            var resultado = new List<SapCobroAplicado>();
            if (idsRecibo == null || idsRecibo.Count == 0) return resultado;

            string schema = ResolverSchema(empresa);            // ← unificado (antes MapEmpresaSchema)
            if (schema == null)
                throw new ArgumentException("Empresa sin schema HANA: " + empresa);

            // Procesamos en lotes para no armar un IN gigantesco contra HANA.
            const int TAM_LOTE = 200;
            for (int i = 0; i < idsRecibo.Count; i += TAM_LOTE)
            {
                var lote = idsRecibo.Skip(i).Take(TAM_LOTE)
                                    .Select(x => (x ?? "").Trim())
                                    .Where(x => x.Length > 0)
                                    .ToList();
                if (lote.Count == 0) continue;

                // Placeholders posicionales de ODBC:  ?,?,?
                string placeholders = string.Join(",", lote.Select(_ => "?"));

                string sql = string.Format(
                    "SELECT \"DocEntry\", \"DocNum\", \"CardCode\", \"DocDate\", \"U_Recibocaja_Webapp\" " +
                    "FROM \"{0}\".\"ORCT\" " +
                    "WHERE \"Canceled\" = 'N' AND TRIM(\"U_Recibocaja_Webapp\") IN ({1})",
                    schema, placeholders);

                var parametros = lote
                    .Select(id => new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = id })
                    .ToArray();

                DataTable dt = HanaHelper.EjecutarConsulta(sql, parametros);

                foreach (DataRow r in dt.Rows)
                {
                    resultado.Add(new SapCobroAplicado
                    {
                        IdRecibo = Convert.ToString(r["U_Recibocaja_Webapp"]).Trim(),
                        SapDocEntry = Convert.ToInt32(r["DocEntry"]),
                        SapDocNum = Convert.ToInt32(r["DocNum"]),
                        CardCode = r["CardCode"] == DBNull.Value ? null : Convert.ToString(r["CardCode"]),
                        FechaPago = r["DocDate"] == DBNull.Value ? (DateTime?)null
                                                                    : Convert.ToDateTime(r["DocDate"])
                    });
                }
            }

            // Un ID con varias filas activas (anuló+rehízo) → nos quedamos con el DocEntry mayor.
            return resultado
                .GroupBy(x => x.IdRecibo)
                .Select(g => g.OrderByDescending(x => x.SapDocEntry).First())
                .ToList();
        }

        /// <summary>
        /// Para un lote de DocEntry de pagos (ORCT), devuelve la suma de lo aplicado
        /// en RCT2, en ambas monedas:
        ///   - MontoGTQ  = SUM(SumApplied)  (moneda local)
        ///   - MontoUSD  = SUM(AppliedFC)   (moneda extranjera)
        /// El BLL elige cuál comparar según la MONEDA del recibo.
        ///
        /// Enlace SAP: RCT2."DocNum" = ORCT."DocEntry" (rareza histórica de SAP B1:
        /// la columna se llama DocNum pero guarda el DocEntry del pago).
        ///
        /// Un DocEntry con RCT2 vacío (anticipo) simplemente NO aparece en el
        /// diccionario devuelto -> el BLL lo interpreta como "sin líneas que conciliar".
        /// </summary>
        public Dictionary<int, MontoAplicadoSap> ObtenerMontosAplicados(string empresa, List<int> docEntries)
        {
            var resultado = new Dictionary<int, MontoAplicadoSap>();
            if (docEntries == null || docEntries.Count == 0) return resultado;

            string schema = ResolverSchema(empresa);            // ← unificado (antes MapEmpresaSchema)
            if (schema == null)
                throw new ArgumentException("Empresa sin schema HANA: " + empresa);

            // Lotes para no armar un IN gigante contra HANA (igual criterio que ObtenerCobrosOperados).
            const int TAM_LOTE = 200;
            var distintos = docEntries.Distinct().ToList();

            for (int i = 0; i < distintos.Count; i += TAM_LOTE)
            {
                var lote = distintos.Skip(i).Take(TAM_LOTE).ToList();
                if (lote.Count == 0) continue;

                string placeholders = string.Join(",", lote.Select(_ => "?"));

                // Agrupamos en HANA por DocNum (=DocEntry del pago) y sumamos ambas monedas.
                string sql = string.Format(
                    "SELECT \"DocNum\" AS \"DocEntryPago\", " +
                    "       SUM(\"SumApplied\") AS \"TotalGTQ\", " +
                    "       SUM(\"AppliedFC\")  AS \"TotalUSD\" " +
                    "FROM \"{0}\".\"RCT2\" " +
                    "WHERE \"DocNum\" IN ({1}) " +
                    "GROUP BY \"DocNum\"",
                    schema, placeholders);

                var parametros = lote
                    .Select(de => new OdbcParameter { OdbcType = OdbcType.Int, Value = de })
                    .ToArray();

                DataTable dt = HanaHelper.EjecutarConsulta(sql, parametros);

                foreach (DataRow r in dt.Rows)
                {
                    int docEntry = Convert.ToInt32(r["DocEntryPago"]);
                    resultado[docEntry] = new MontoAplicadoSap
                    {
                        DocEntry = docEntry,
                        MontoGTQ = r["TotalGTQ"] == DBNull.Value ? 0m : Convert.ToDecimal(r["TotalGTQ"]),
                        MontoUSD = r["TotalUSD"] == DBNull.Value ? 0m : Convert.ToDecimal(r["TotalUSD"])
                    };
                }
            }

            return resultado;
        }

        // ─────────────────────────────────────────────
        // PAGOS COMPLETOS (Fase 5 — DESCUADRE)
        // ─────────────────────────────────────────────
        /// <summary>
        /// Para un lote de recibos, devuelve TODOS los ORCT etiquetados con ellos
        /// (activos Y anulados), con: totales del pago, lo aplicado según RCT2 y
        /// las facturas (OINV.DocNum) que cada pago dejó aplicadas.
        ///
        /// A diferencia de ObtenerCobrosOperados (que filtra Canceled='N' y colapsa
        /// a 1 fila por recibo), aquí se conserva TODO: un recibo puede tener N
        /// pagos en SAP (ORCT manuales de Créditos con el mismo U_Recibocaja_Webapp).
        /// </summary>
        public List<SapPagoDetalle> ObtenerPagosSapDetalle(string empresa, List<string> idsRecibo)
        {
            var pagos = new List<SapPagoDetalle>();
            if (idsRecibo == null || idsRecibo.Count == 0) return pagos;

            string schema = ResolverSchema(empresa);
            if (schema == null)
                throw new ArgumentException("Empresa sin schema HANA: " + empresa);

            const int TAM_LOTE = 200;

            // ── 1) ORCT: todos los pagos etiquetados, SIN filtrar Canceled ──
            for (int i = 0; i < idsRecibo.Count; i += TAM_LOTE)
            {
                var lote = idsRecibo.Skip(i).Take(TAM_LOTE)
                                    .Select(x => (x ?? "").Trim())
                                    .Where(x => x.Length > 0)
                                    .ToList();
                if (lote.Count == 0) continue;

                string placeholders = string.Join(",", lote.Select(_ => "?"));

                string sql = string.Format(
                    "SELECT \"DocEntry\", \"DocNum\", \"Canceled\", \"DocDate\", \"DocCurr\", " +
                    "       \"DocTotal\", \"DocTotalFC\", \"U_Recibocaja_Webapp\" " +
                    "FROM \"{0}\".\"ORCT\" " +
                    "WHERE TRIM(\"U_Recibocaja_Webapp\") IN ({1})",
                    schema, placeholders);

                var parametros = lote
                    .Select(id => new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = id })
                    .ToArray();

                DataTable dt = HanaHelper.EjecutarConsulta(sql, parametros);

                foreach (DataRow r in dt.Rows)
                {
                    pagos.Add(new SapPagoDetalle
                    {
                        IdRecibo = Convert.ToString(r["U_Recibocaja_Webapp"]).Trim(),
                        DocEntry = Convert.ToInt32(r["DocEntry"]),
                        DocNum = Convert.ToInt32(r["DocNum"]),
                        Canceled = "Y".Equals(Convert.ToString(r["Canceled"]).Trim(),
                                                 StringComparison.OrdinalIgnoreCase),
                        FechaPago = r["DocDate"] == DBNull.Value
                                        ? (DateTime?)null : Convert.ToDateTime(r["DocDate"]),
                        MonedaDoc = NormalizarMoneda(LeerCampo(r, "DocCurr")),
                        DocTotalGTQ = LeerDecimal(r, "DocTotal"),
                        DocTotalUSD = LeerDecimal(r, "DocTotalFC")
                    });
                }
            }

            if (pagos.Count == 0) return pagos;

            // ── 2) RCT2: sumas aplicadas por pago (reutiliza el método existente) ──
            var docEntries = pagos.Select(p => p.DocEntry).Distinct().ToList();
            Dictionary<int, MontoAplicadoSap> sumas = ObtenerMontosAplicados(empresa, docEntries);

            // ── 3) RCT2 + OINV: qué facturas dejó aplicadas cada pago ──
            Dictionary<int, List<string>> facturas = ObtenerFacturasAplicadas(schema, docEntries);

            foreach (var p in pagos)
            {
                if (sumas.TryGetValue(p.DocEntry, out var s))
                {
                    p.TieneLineasRct2 = true;
                    p.AplicadoGTQ = s.MontoGTQ;
                    p.AplicadoUSD = s.MontoUSD;
                }
                if (facturas.TryGetValue(p.DocEntry, out var f))
                    p.FacturasAplicadas = f;
            }

            return pagos;
        }

        /// <summary>
        /// DocNum de facturas (OINV) aplicadas por cada pago.
        /// Enlace SAP: RCT2."DocNum" = DocEntry del PAGO (quirk histórico) y
        /// RCT2."DocEntry" = DocEntry del documento aplicado; InvType 13 = factura.
        /// </summary>
        private Dictionary<int, List<string>> ObtenerFacturasAplicadas(string schema, List<int> docEntries)
        {
            var resultado = new Dictionary<int, List<string>>();
            if (docEntries == null || docEntries.Count == 0) return resultado;

            const int TAM_LOTE = 200;
            for (int i = 0; i < docEntries.Count; i += TAM_LOTE)
            {
                var lote = docEntries.Skip(i).Take(TAM_LOTE).ToList();
                string placeholders = string.Join(",", lote.Select(_ => "?"));

                string sql = string.Format(
                    "SELECT T1.\"DocNum\" AS \"DocEntryPago\", T2.\"DocNum\" AS \"FacturaDocNum\" " +
                    "FROM \"{0}\".\"RCT2\" T1 " +
                    "INNER JOIN \"{0}\".\"OINV\" T2 ON T2.\"DocEntry\" = T1.\"DocEntry\" " +
                    "WHERE T1.\"InvType\" = 13 AND T1.\"DocNum\" IN ({1})",
                    schema, placeholders);

                var parametros = lote
                    .Select(de => new OdbcParameter { OdbcType = OdbcType.Int, Value = de })
                    .ToArray();

                DataTable dt = HanaHelper.EjecutarConsulta(sql, parametros);
                foreach (DataRow r in dt.Rows)
                {
                    int de = Convert.ToInt32(r["DocEntryPago"]);
                    string fac = Convert.ToString(r["FacturaDocNum"]);
                    if (!resultado.TryGetValue(de, out var lista))
                        resultado[de] = lista = new List<string>();
                    if (!lista.Contains(fac)) lista.Add(fac);
                }
            }
            return resultado;
        }

        // ─────────────────────────────────────────────
        // FACTURAS / PEDIDOS (Vista RC_FACTURAS_REC_CAJ)
        // ─────────────────────────────────────────────
        /// <summary>
        /// Trae documentos disponibles (FACTURA/PEDIDO) de un cliente desde la
        /// vista RC_FACTURAS_REC_CAJ del schema SAP correspondiente.
        ///
        /// La vista ya filtra DocStatus='O' (abierta) y CANCELED='N' (no anulada),
        /// así que solo le agregamos el WHERE por Tipo + CardCode.
        ///
        /// NOTA: NO filtramos por la columna "Empresa" de la vista — siempre trae
        /// 'GRACO' hardcodeado (bug latente de la vista). La separación por empresa
        /// la da el schema (SBOBOLIK / SBOESCOCESA / SBO_GRACO).
        /// </summary>
        public List<DocumentoRecibo> ObtenerFacturas(string empresa, string clienteId, string tipoDoc)
        {
            var lista = new List<DocumentoRecibo>();

            string schema = ResolverSchema(empresa);
            if (schema == null) return lista;

            string tipo = string.IsNullOrWhiteSpace(tipoDoc)
                            ? "FACTURA"
                            : tipoDoc.Trim().ToUpper();

            // HanaHelper sólo recibe un string → escapamos comillas (misma convención
            // que BuscarClientes). Filtramos por Tipo + CardCode. 
            string query = string.Format(
                "SELECT \"DocNum\", \"DocDate\", \"DocCur\", \"DocTotal\", \"PaidToDate\", " +
                "\"U_SERIE_FACE\", \"U_NUMERO_DOCUMENTO\", \"CardCode\", \"CardName\", \"Tipo\" " +
                "FROM \"{0}\".\"RC_FACTURAS_REC_CAJ\" " +
                "WHERE \"Tipo\" = '{1}' AND \"CardCode\" = '{2}' " +
                "ORDER BY \"DocDate\" DESC",
                schema, Esc(tipo), Esc(clienteId ?? ""));

            try
            {
                DataTable dt = HanaHelper.EjecutarConsulta(query);
                foreach (DataRow row in dt.Rows)
                {
                    lista.Add(new DocumentoRecibo
                    {
                        NoDocumento = LeerCampo(row, "DocNum"),
                        FechaDoc = LeerFecha(row, "DocDate"),
                        MontoFact = LeerDecimal(row, "DocTotal"),
                        Pagado = LeerDecimal(row, "PaidToDate"),
                        Moneda = NormalizarMoneda(LeerCampo(row, "DocCur")), // QTZ → GTQ
                        FelSerie = LeerCampo(row, "U_SERIE_FACE"),
                        FelNumero = LeerCampo(row, "U_NUMERO_DOCUMENTO")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception(
                    string.Format("Error HANA al buscar facturas ({0} / {1}): {2}",
                        empresa, schema, ex.Message), ex);
            }

            return lista;
        }

        // ── Helpers privados ──────────────────────────────────────────────
        /// <summary>
        /// Traduce el código de moneda de SAP al código canónico de la app.
        /// SAP usa "QTZ" para el Quetzal; los dropdowns de la vista usan "GTQ".
        /// </summary>
        private static string NormalizarMoneda(string monedaSap)
        {
            var m = (monedaSap ?? "").Trim().ToUpper();
            switch (m)
            {
                case "QTZ":
                case "Q":
                    return "GTQ";
                default:
                    return m; // USD, EUR, etc. pasan tal cual
            }
        }

        /// <summary>
        /// Mapea empresa → schema HANA leyendo de AppSettings (un solo lugar,
        /// reutilizado por clientes, facturas, tipo de cambio y sincronizador).
        ///
        /// Claves esperadas en el .config:
        ///   HanaSchema.GRACO / HanaSchema.FAES / HanaSchema.BOLIK
        ///
        /// - Empresa vacía o desconocida (no GRACO/FAES/BOLIK) → devuelve null
        ///   (los callers lo interpretan como "sin resultados").
        /// - Empresa conocida PERO sin su clave en el .config → lanza excepción
        ///   clara. Es un error de despliegue: preferimos que truene con un mensaje
        ///   entendible antes que devolver vacío en silencio (que confunde el diagnóstico).
        /// </summary>
        private static string ResolverSchema(string empresa)
        {
            if (string.IsNullOrWhiteSpace(empresa)) return null;

            string emp = empresa.Trim().ToUpperInvariant();
            string schema = ConfigurationManager.AppSettings["HanaSchema." + emp];

            if (string.IsNullOrWhiteSpace(schema) &&
                (emp == "GRACO" || emp == "FAES" || emp == "BOLIK"))
            {
                throw new Exception(
                    "Falta la clave de AppSettings 'HanaSchema." + emp + "' en el .config. " +
                    "Agregala (ej. \"TEST_SBOGRACO\" en pruebas, \"SBO_GRACO\" en producción).");
            }

            return string.IsNullOrWhiteSpace(schema) ? null : schema.Trim();
        }

        private static string Esc(string valor) =>
            (valor ?? "").Replace("'", "''");

        private static string LeerCampo(DataRow row, string columna)
        {
            try
            {
                return row[columna] != DBNull.Value
                    ? Convert.ToString(row[columna]) ?? ""
                    : "";
            }
            catch { return ""; } // si la columna no existe, no explota
        }

        private static decimal LeerDecimal(DataRow row, string columna)
        {
            try
            {
                var v = row[columna];
                return (v != null && v != DBNull.Value) ? Convert.ToDecimal(v) : 0m;
            }
            catch { return 0m; }
        }

        private static DateTime LeerFecha(DataRow row, string columna)
        {
            try
            {
                var v = row[columna];
                return (v != null && v != DBNull.Value) ? Convert.ToDateTime(v) : DateTime.Today;
            }
            catch { return DateTime.Today; }
        }

        // ─────────────────────────────────────────────
        // TIPO DE CAMBIO (ORTT) — GTQ por 1 USD
        // ─────────────────────────────────────────────
        /// <summary>
        /// Trae la tasa USD vigente desde SAP (tabla ORTT del schema de la empresa).
        /// Regla: la última tasa con RateDate &lt;= fecha de corte (recibo u hoy).
        /// </summary>
        public decimal ObtenerTipoCambio(string empresa, DateTime? fecha = null)
        {
            string schema = ResolverSchema(empresa);
            if (schema == null)
                throw new Exception($"Empresa desconocida para tipo de cambio: '{empresa}'.");

            string fechaCorte = (fecha ?? DateTime.Today).ToString("yyyy-MM-dd");

            string query = string.Format(
                "SELECT TOP 1 \"Rate\" " +
                "FROM \"{0}\".\"ORTT\" " +
                "WHERE \"Currency\" = 'USD' AND \"RateDate\" <= TO_DATE('{1}','YYYY-MM-DD') " +
                "ORDER BY \"RateDate\" DESC",
                schema, fechaCorte);

            try
            {
                DataTable dt = HanaHelper.EjecutarConsulta(query);
                if (dt.Rows.Count == 0 || dt.Rows[0][0] == DBNull.Value)
                    throw new Exception(
                        $"No hay tipo de cambio USD cargado en SAP ({schema}) al {fechaCorte}.");

                decimal rate = Convert.ToDecimal(dt.Rows[0][0]);
                if (rate <= 0)
                    throw new Exception($"Tipo de cambio USD inválido en SAP ({schema}): {rate}.");

                return rate;   // ej. 7.619820
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Error HANA al obtener tipo de cambio ({empresa} / {schema}): {ex.Message}", ex);
            }
        }
        // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        /* ============================================================================
            Contratos HANA usados por Borradores NC.

            Columnas reales de las dos vistas, confirmadas contra SYS.VIEW_COLUMNS:

            RC_FACTURAS_BORRNC                    INF_VRC_FACRNC
                1 Empresa            VARCHAR(5)       1 Tipo      VARCHAR(8)
                2 SlpName            NVARCHAR(155)    2 Factura   INTEGER
                3 DocNum             INTEGER          3 Nota      INTEGER
                4 U_SERIE_FACE       NVARCHAR(20)     4 DocDate   TIMESTAMP
                5 U_NUMERO_DOCUMENTO NVARCHAR(150)    5 CardCode  NVARCHAR(15)
                6 DocDate            TIMESTAMP        6 CardName  NVARCHAR(200)
                7 CardCode           NVARCHAR(15)     7 DocCur    NVARCHAR(3)
                8 CardName           NVARCHAR(200)    8 DocTotal  DECIMAL(21,6)
                9 DocCur             NVARCHAR(3)      9 JrnlMemo  NVARCHAR(254)
            10 DocTotal           DECIMAL(21,6)   10 Comments  NVARCHAR(254)
            11 PaidToDate         DECIMAL(21,6)
        ============================================================================ */


        // ═════════════════════════════════════════════════════════════════════════════
        // MÉTODO 1 — Facturas disponibles para NC
        // ═════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Facturas de un cliente contra las que se puede emitir nota de crédito.
        ///
        /// Réplica de FrmFacturasCL_BorrNC del desktop, con tres diferencias:
        ///
        ///   1. El schema se resuelve por configuración (ResolverSchema), en vez de
        ///      un if/else if/else if con la misma consulta repetida tres veces y las
        ///      credenciales de SYSTEM incrustadas en el formulario.
        ///
        ///   2. El filtro de DocNum usa TO_VARCHAR explícito. La vista declara DocNum
        ///      como INTEGER; el legado hacía "DocNum" LIKE '%123%' confiando en la
        ///      conversión implícita de HANA, cuyo comportamiento depende de la
        ///      configuración del servidor.
        ///
        ///   3. Se trae PaidToDate, que el desktop ignoraba por completo.
        ///
        /// Los parámetros vacíos actúan como "sin filtro", igual que en el desktop.
        /// </summary>
        public List<FacturaBorradorNc> ObtenerFacturasBorradorNc(
            string empresa, string clienteId, string agente, string filtroDoc)
        {
            var lista = new List<FacturaBorradorNc>();

            string schema = ResolverSchema(empresa);
            if (schema == null) return lista;   // empresa desconocida

            var condiciones = new List<string>();

            if (!string.IsNullOrWhiteSpace(clienteId))
                condiciones.Add(string.Format("\"CardCode\" = '{0}'", Esc(clienteId.Trim())));

            if (!string.IsNullOrWhiteSpace(agente))
                condiciones.Add(string.Format("\"SlpName\" = '{0}'", Esc(agente.Trim())));

            if (!string.IsNullOrWhiteSpace(filtroDoc))
                condiciones.Add(string.Format(
                    "TO_VARCHAR(\"DocNum\") LIKE '%{0}%'", Esc(filtroDoc.Trim())));

            string where = condiciones.Count > 0
                ? " WHERE " + string.Join(" AND ", condiciones)
                : string.Empty;

            string query = string.Format(
                "SELECT \"DocNum\", \"DocDate\", \"CardCode\", \"CardName\", \"SlpName\", " +
                "       \"DocCur\", \"DocTotal\", \"PaidToDate\", " +
                "       \"U_SERIE_FACE\", \"U_NUMERO_DOCUMENTO\" " +
                "FROM \"{0}\".\"RC_FACTURAS_BORRNC\"{1} " +
                "ORDER BY \"DocDate\" DESC, \"DocNum\" DESC",
                schema, where);

            try
            {
                DataTable dt = HanaHelper.EjecutarConsulta(query);

                foreach (DataRow row in dt.Rows)
                {
                    lista.Add(new FacturaBorradorNc
                    {
                        // DocNum llega como INTEGER; se guarda como texto porque así
                        // viaja al resto del sistema (BORR_NC_DET.DOCUMENTO es
                        // NVARCHAR, para convivir con los datos legados).
                        DocNum = LeerCampo(row, "DocNum"),
                        DocDate = LeerFecha(row, "DocDate"),
                        CardCode = LeerCampo(row, "CardCode"),
                        CardName = LeerCampo(row, "CardName"),
                        SlpName = LeerCampo(row, "SlpName"),
                        Moneda = NormalizarMoneda(LeerCampo(row, "DocCur")),
                        DocTotal = LeerDecimal(row, "DocTotal"),
                        Pagado = LeerDecimal(row, "PaidToDate"),
                        SerieFel = LeerCampo(row, "U_SERIE_FACE"),
                        NumeroFel = LeerCampo(row, "U_NUMERO_DOCUMENTO")
                        // Acumulado, NcPreviaSap y Disponible los pone el BLL:
                        // dependen de datos que HANA no conoce.
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format(
                    "Error HANA al buscar facturas para NC ({0} / {1}): {2}",
                    empresa, schema, ex.Message), ex);
            }

            return lista;
        }


        // ═════════════════════════════════════════════════════════════════════════════
        // MÉTODO 2 — Notas de crédito y devoluciones ya emitidas en SAP
        // ═════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// NC y devoluciones ya emitidas en SAP contra una factura.
        ///
        /// El desktop consultaba esta vista solo en FrmAutorizaciones, como una
        /// pestaña informativa para el autorizador. Aquí se usa además para calcular
        /// el disponible neto y para dejar constancia en BORR_NC_DET.NC_PREVIA_SAP.
        ///
        /// Nota sobre el filtro: "Factura" es INTEGER en la vista, así que el número
        /// se valida como entero y se inyecta como número, no como cadena. Eso quita
        /// de raíz cualquier riesgo de inyección en este parámetro, y además evita la
        /// conversión implícita que hacía el legado.
        ///
        /// Devuelve lista vacía —nunca lanza por documento no numérico— porque un
        /// DocNum mal formado es un dato del usuario, no un fallo del sistema.
        /// </summary>
        public List<NotaCreditoPreviaSap> ObtenerNotasCreditoPrevias(
            string empresa, string documento)
        {
            var lista = new List<NotaCreditoPreviaSap>();

            string schema = ResolverSchema(empresa);
            if (schema == null) return lista;

            int docNum;
            if (!int.TryParse((documento ?? "").Trim(), out docNum))
                return lista;

            string query = string.Format(
                "SELECT \"Tipo\", \"Factura\", \"Nota\", \"DocDate\", \"CardCode\", " +
                "       \"CardName\", \"DocCur\", \"DocTotal\", \"JrnlMemo\", \"Comments\" " +
                "FROM \"{0}\".\"INF_VRC_FACRNC\" " +
                "WHERE \"Factura\" = {1} AND \"Nota\" IS NOT NULL " +
                "ORDER BY \"DocDate\" DESC",
                schema, docNum);

            try
            {
                DataTable dt = HanaHelper.EjecutarConsulta(query);

                foreach (DataRow row in dt.Rows)
                {
                    lista.Add(new NotaCreditoPreviaSap
                    {
                        Tipo = LeerCampo(row, "Tipo"),
                        Factura = LeerCampo(row, "Factura"),
                        Nota = LeerCampo(row, "Nota"),
                        Fecha = LeerFecha(row, "DocDate"),
                        CardCode = LeerCampo(row, "CardCode"),
                        CardName = LeerCampo(row, "CardName"),
                        Moneda = NormalizarMoneda(LeerCampo(row, "DocCur")),
                        Total = LeerDecimal(row, "DocTotal"),
                        Origen = LeerCampo(row, "JrnlMemo"),
                        Comentarios = LeerCampo(row, "Comments")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format(
                    "Error HANA al consultar NC previas del documento {0} ({1}): {2}",
                    documento, schema, ex.Message), ex);
            }

            return lista;
        }


        // ═════════════════════════════════════════════════════════════════════════════
        // MÉTODO 3 — Versión por lote (para el modal de facturas)
        // ═════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// NC previas de VARIAS facturas en una sola consulta.
        ///
        /// El modal puede traer decenas de facturas; una consulta por cada una serían
        /// decenas de viajes a HANA, que está en otra máquina de la red interna.
        /// Mismo criterio que BorradorNcDA.ObtenerAcumuladoDocumentos.
        ///
        /// Los DocNum se validan como enteros antes de armar el IN, así que la lista
        /// no puede contener nada más que números.
        /// </summary>
        public Dictionary<string, List<NotaCreditoPreviaSap>> ObtenerNotasCreditoPrevias(
            string empresa, IList<string> documentos)
        {
            var mapa = new Dictionary<string, List<NotaCreditoPreviaSap>>(
                            StringComparer.OrdinalIgnoreCase);

            string schema = ResolverSchema(empresa);
            if (schema == null || documentos == null || documentos.Count == 0) return mapa;

            var numeros = new List<int>();
            foreach (var d in documentos)
            {
                int n;
                if (int.TryParse((d ?? "").Trim(), out n) && !numeros.Contains(n))
                    numeros.Add(n);
            }
            if (numeros.Count == 0) return mapa;

            string query = string.Format(
                "SELECT \"Tipo\", \"Factura\", \"Nota\", \"DocDate\", \"CardCode\", " +
                "       \"CardName\", \"DocCur\", \"DocTotal\", \"JrnlMemo\", \"Comments\" " +
                "FROM \"{0}\".\"INF_VRC_FACRNC\" " +
                "WHERE \"Nota\" IS NOT NULL AND \"Factura\" IN ({1}) " +
                "ORDER BY \"Factura\", \"DocDate\" DESC",
                schema, string.Join(",", numeros));

            try
            {
                DataTable dt = HanaHelper.EjecutarConsulta(query);

                foreach (DataRow row in dt.Rows)
                {
                    string factura = LeerCampo(row, "Factura");
                    if (!mapa.ContainsKey(factura))
                        mapa[factura] = new List<NotaCreditoPreviaSap>();

                    mapa[factura].Add(new NotaCreditoPreviaSap
                    {
                        Tipo = LeerCampo(row, "Tipo"),
                        Factura = factura,
                        Nota = LeerCampo(row, "Nota"),
                        Fecha = LeerFecha(row, "DocDate"),
                        CardCode = LeerCampo(row, "CardCode"),
                        CardName = LeerCampo(row, "CardName"),
                        Moneda = NormalizarMoneda(LeerCampo(row, "DocCur")),
                        Total = LeerDecimal(row, "DocTotal"),
                        Origen = LeerCampo(row, "JrnlMemo"),
                        Comentarios = LeerCampo(row, "Comments")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format(
                    "Error HANA al consultar NC previas por lote ({0}): {1}",
                    schema, ex.Message), ex);
            }

            return mapa;
        }

    }
}
