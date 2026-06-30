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

        private string ConnString
            => ConfigurationManager.ConnectionStrings[CONN_NAME].ConnectionString;

        /// <summary>
        /// Trae un lote de recibos PENDIENTES de una empresa, priorizando los que
        /// llevan más tiempo sin revisarse (los nuevos, con NULL, van primero).
        /// Esta es la "cola rotativa" que evita que el job se atasque.
        /// </summary>
        public List<string> ObtenerRecibosPendientes(string empresa, int top = 500)
        {
            var ids = new List<string>();
            const string sql = @"
                SELECT TOP (@top) ID_RECIBO
                FROM dbo.REC_CAJA_ENC
                WHERE SYNC_ESTADO = 'PENDIENTE'
                  AND ID_EMPRESA  = @empresa
                ORDER BY CASE WHEN SYNC_ULTIMO_CHECK IS NULL THEN 0 ELSE 1 END,  -- nuevos primero
                         SYNC_ULTIMO_CHECK ASC;"; 

            using (var cn = new SqlConnection(ConnString))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.AddWithValue("@top", top);
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

        /// <summary>Sella SYNC_ULTIMO_CHECK en bloque para los recibos del lote que
        /// NO aparecieron en SAP, para que roten al final de la cola. Un solo UPDATE.</summary>
        public void MarcarUltimoCheckLote(List<string> idsRecibo, string empresa)
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
                cmd.CommandText =
                    "UPDATE dbo.REC_CAJA_ENC SET SYNC_ULTIMO_CHECK = SYSDATETIME() " +
                    "WHERE ID_EMPRESA = @empresa AND SYNC_ESTADO = 'PENDIENTE' " +
                    "AND ID_RECIBO IN (" + string.Join(",", nombres) + ");";
                cn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}