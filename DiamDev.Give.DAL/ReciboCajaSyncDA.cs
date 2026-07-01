using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using DiamDev.Give.Entities;

namespace DiamDev.Give.DAL
{
    public class ReciboCajaSyncDA
    {
        // ⚠️ Apunta al módulo de recibos. PRUEBAS = POS-SmartK66 (RecibosContext).
        // El día del cutover NO se toca esto: solo se cambia la BD dentro de
        // RecibosContext en el Web.config (a POS-SmartK66_DEV). Cero cambios de código.
        private const string CONN_NAME = "RecibosContext";

        // Fallback si no existe el AppSetting "SyncLoteRecibos" o viene mal escrito.
        private const int LOTE_FALLBACK = 2000;

        private string ConnString
            => ConfigurationManager.ConnectionStrings[CONN_NAME].ConnectionString;

        /// <summary>
        /// Tamaño de lote por defecto, leído de App.config (AppSettings -> SyncLoteRecibos).
        /// Si el setting no existe o no es un entero válido, usa LOTE_FALLBACK (2000).
        /// Equivale en TS a: Number(process.env.SYNC_LOTE) || 2000.
        /// </summary>
        private int LoteDefault
        {
            get
            {
                string raw = ConfigurationManager.AppSettings["SyncLoteRecibos"];
                return (int.TryParse(raw, out int v) && v > 0) ? v : LOTE_FALLBACK;
            }
        }

        // ──────────────────────────────────────────────────────────────────
        // PASADA NORMAL: PENDIENTE -> OPERADO
        // ──────────────────────────────────────────────────────────────────

        /// <summary>
        /// Trae un lote de recibos PENDIENTES de una empresa, priorizando los que
        /// llevan más tiempo sin revisarse (los nuevos, con NULL, van primero).
        /// Esta es la "cola rotativa" que evita que el job se atasque.
        ///
        /// top = null  -> usa el lote configurado en App.config (recomendado).
        /// top = N     -> fuerza un tamaño puntual (útil en pruebas/tests).
        /// </summary>
        public List<string> ObtenerRecibosPendientes(string empresa, int? top = null)
        {
            int lote = top ?? LoteDefault;

            var ids = new List<string>();
            const string sql = @"
                SELECT TOP (@top) ID_RECIBO
                FROM dbo.REC_CAJA_ENC
                WHERE SYNC_ESTADO = 'PENDIENTE'
                  AND ID_EMPRESA  = @empresa
                ORDER BY CASE WHEN SYNC_ULTIMO_CHECK IS NULL THEN 0 ELSE 1 END, -- nuevos primero
                         SYNC_ULTIMO_CHECK ASC,                                 -- luego el más viejo
                         ROWID ASC;                                             -- DESEMPATE estable (PK única)";

            using (var cn = new SqlConnection(ConnString))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.AddWithValue("@top", lote);
                cmd.Parameters.AddWithValue("@empresa", empresa);
                cn.Open();
                using (var rd = cmd.ExecuteReader())
                    while (rd.Read())
                        ids.Add(rd["ID_RECIBO"].ToString());
            }
            return ids;
        }

        /// <summary>Marca un recibo como OPERADO con las referencias de SAP.
        /// Idempotente: solo actúa si sigue PENDIENTE.</summary>
        public void MarcarReciboOperado(SapCobroAplicado cobro, string empresa)
        {
            const string sql = @"
                UPDATE dbo.REC_CAJA_ENC
                SET SYNC_ESTADO       = 'OPERADO',
                    SAP_DOCENTRY      = @docEntry,
                    SAP_DOCNUM        = @docNum,
                    FECHA_OPERADO     = SYSDATETIME(),
                    SYNC_ULTIMO_CHECK = SYSDATETIME(),
                    SYNC_OBSERVACION  = NULL
                WHERE ID_RECIBO  = @idRecibo
                  AND ID_EMPRESA = @empresa
                  AND SYNC_ESTADO = 'PENDIENTE';";

            using (var cn = new SqlConnection(ConnString))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.AddWithValue("@docEntry", cobro.SapDocEntry);
                cmd.Parameters.AddWithValue("@docNum", cobro.SapDocNum);
                cmd.Parameters.AddWithValue("@idRecibo", cobro.IdRecibo);
                cmd.Parameters.AddWithValue("@empresa", empresa);
                cn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // ──────────────────────────────────────────────────────────────────
        // PASADA INVERSA: OPERADO -> (sigue activo | anulado | rehecho)
        // ──────────────────────────────────────────────────────────────────

        /// <summary>
        /// Trae un lote de recibos OPERADO de una empresa (cola rotativa, igual que
        /// los pendientes). Devuelve también el DocEntry/DocNum que SQL tiene guardado,
        /// para poder comparar contra lo que SAP reporta hoy y detectar:
        ///   - anulación (ya no aparece activo en SAP)
        ///   - anuló+rehízo (sigue activo pero con DocEntry distinto)
        /// </summary>
        public List<SapCobroAplicado> ObtenerRecibosOperados(string empresa, int? top = null)
        {
            int lote = top ?? LoteDefault;

            var lista = new List<SapCobroAplicado>();
            const string sql = @"
                SELECT TOP (@top) ID_RECIBO, SAP_DOCENTRY, SAP_DOCNUM
                FROM dbo.REC_CAJA_ENC
                WHERE SYNC_ESTADO = 'OPERADO'
                  AND ID_EMPRESA  = @empresa
                ORDER BY CASE WHEN SYNC_ULTIMO_CHECK IS NULL THEN 0 ELSE 1 END,
                         SYNC_ULTIMO_CHECK ASC,
                         ROWID ASC;";

            using (var cn = new SqlConnection(ConnString))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.AddWithValue("@top", lote);
                cmd.Parameters.AddWithValue("@empresa", empresa);
                cn.Open();
                using (var rd = cmd.ExecuteReader())
                    while (rd.Read())
                    {
                        lista.Add(new SapCobroAplicado
                        {
                            IdRecibo = rd["ID_RECIBO"].ToString(),
                            SapDocEntry = rd["SAP_DOCENTRY"] == DBNull.Value
                                            ? 0 : Convert.ToInt32(rd["SAP_DOCENTRY"]),
                            SapDocNum = rd["SAP_DOCNUM"] == DBNull.Value
                                            ? 0 : Convert.ToInt32(rd["SAP_DOCNUM"])
                        });
                    }
            }
            return lista;
        }

        /// <summary>
        /// OPCIÓN A: un recibo OPERADO que ya NO está activo en SAP (anulado) vuelve
        /// a PENDIENTE para que la cola lo re-evalúe. Limpia las referencias SAP y
        /// deja constancia en SYNC_OBSERVACION. Idempotente: solo si sigue OPERADO.
        /// </summary>
        public void RegresarReciboAPendiente(string idRecibo, string empresa, string observacion)
        {
            const string sql = @"
                UPDATE dbo.REC_CAJA_ENC
                SET SYNC_ESTADO       = 'PENDIENTE',
                    SAP_DOCENTRY      = NULL,
                    SAP_DOCNUM        = NULL,
                    FECHA_OPERADO     = NULL,
                    SYNC_ULTIMO_CHECK = SYSDATETIME(),
                    SYNC_OBSERVACION  = @obs
                WHERE ID_RECIBO  = @idRecibo
                  AND ID_EMPRESA = @empresa
                  AND SYNC_ESTADO = 'OPERADO';";

            using (var cn = new SqlConnection(ConnString))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.AddWithValue("@idRecibo", idRecibo);
                cmd.Parameters.AddWithValue("@empresa", empresa);
                cmd.Parameters.AddWithValue("@obs", (object)observacion ?? DBNull.Value);
                cn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Caso anuló+rehízo: el recibo sigue OPERADO en SAP pero bajo un DocEntry
        /// nuevo. Re-apuntamos SQL al pago vigente. Idempotente: solo si sigue OPERADO.
        /// </summary>
        public void ActualizarReferenciasSap(SapCobroAplicado cobro, string empresa, string observacion)
        {
            const string sql = @"
                UPDATE dbo.REC_CAJA_ENC
                SET SAP_DOCENTRY      = @docEntry,
                    SAP_DOCNUM        = @docNum,
                    SYNC_ULTIMO_CHECK = SYSDATETIME(),
                    SYNC_OBSERVACION  = @obs
                WHERE ID_RECIBO  = @idRecibo
                  AND ID_EMPRESA = @empresa
                  AND SYNC_ESTADO = 'OPERADO';";

            using (var cn = new SqlConnection(ConnString))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.AddWithValue("@docEntry", cobro.SapDocEntry);
                cmd.Parameters.AddWithValue("@docNum", cobro.SapDocNum);
                cmd.Parameters.AddWithValue("@idRecibo", cobro.IdRecibo);
                cmd.Parameters.AddWithValue("@empresa", empresa);
                cmd.Parameters.AddWithValue("@obs", (object)observacion ?? DBNull.Value);
                cn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // ──────────────────────────────────────────────────────────────────
        // ROTACIÓN DE COLA
        // ──────────────────────────────────────────────────────────────────

        /// <summary>Sella SYNC_ULTIMO_CHECK en bloque para los recibos del lote que
        /// NO requirieron cambio, para que roten al final de la cola. Un solo UPDATE.
        /// El parámetro 'estado' permite usarlo tanto para la cola de PENDIENTES
        /// (default) como para la de OPERADOS confirmados sin cambio.</summary>
        public void MarcarUltimoCheckLote(List<string> idsRecibo, string empresa,
                                          string estado = "PENDIENTE")
        {
            if (idsRecibo == null || idsRecibo.Count == 0) return;

            var nombres = new List<string>();
            using (var cn = new SqlConnection(ConnString))
            using (var cmd = new SqlCommand { Connection = cn })
            {
                for (int i = 0; i < idsRecibo.Count; i++)
                {
                    string p = "@id" + i;
                    nombres.Add(p);
                    cmd.Parameters.AddWithValue(p, idsRecibo[i]);
                }
                cmd.Parameters.AddWithValue("@empresa", empresa);
                cmd.Parameters.AddWithValue("@estado", estado);
                cmd.CommandText =
                    "UPDATE dbo.REC_CAJA_ENC SET SYNC_ULTIMO_CHECK = SYSDATETIME() " +
                    "WHERE ID_EMPRESA = @empresa AND SYNC_ESTADO = @estado " +
                    "AND ID_RECIBO IN (" + string.Join(",", nombres) + ");";
                cn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}