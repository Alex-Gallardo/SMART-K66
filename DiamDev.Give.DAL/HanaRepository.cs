using System;
using System.Collections.Generic;
using System.Data;
using DiamDev.Give.Entities;
using System.Data.Odbc;
using System.Linq;

namespace DiamDev.Give.DAL
{
    /// <summary>
    /// Repositorio HANA. NO usa HanaConnection directo — delega en HanaHelper
    /// (ya configurado con AppSettings HANA_Server / HANA_User / etc.).
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

            string schema = MapEmpresaSchema(empresa);
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

        /// <summary>Mapa empresa → schema HANA. Si ya tenés uno en esta clase
        /// (el de INF_CLIENTES_REC), borrá este y reutilizá el tuyo.</summary>
        private static string MapEmpresaSchema(string empresa)
        {
            if (string.IsNullOrEmpty(empresa)) return null;
            switch (empresa.Trim().ToUpperInvariant())
            {
                case "GRACO": return "SBO_GRACO";
                case "FAES": return "SBOESCOCESA";
                case "BOLIK": return "SBOBOLIK";
                default: return null;
            }
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
        /// Si no normalizamos, la validación de saldo cree que cobro (GTQ) y
        /// documento (QTZ) son monedas distintas y permite saldos ≠ 0.
        ///
        /// Si tu SAP ya devuelve GTQ, este map es un no-op (inofensivo).
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

        /// <summary>Mapea empresa → schema SAP. Reutilizable por clientes y facturas.</summary>
        private static string ResolverSchema(string empresa)
        {
            switch ((empresa ?? "").Trim().ToUpper())
            {
                case "GRACO": return "SBO_GRACO";
                case "FAES": return "SBOESCOCESA";
                case "BOLIK": return "SBOBOLIK";
                default: return null;
            }
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
        /// El filtro por fecha es CRÍTICO: SAP tiene tasas futuras cargadas
        /// (ej. 1-dic-2026) que NO debemos tomar para un recibo de hoy.
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
    }
}