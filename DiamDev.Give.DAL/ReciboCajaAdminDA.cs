using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using DiamDev.Give.Entities;

namespace DiamDev.Give.DAL
{
    /// <summary>
    /// Acceso a datos del Dashboard de Supervisión y del mantenimiento de
    /// REC_CAJA_SERIES. ADO.NET puro contra RecibosContext (mismo criterio
    /// que APK66Context: el correlativo vive aquí, no en EF).
    /// </summary>
    public class ReciboCajaAdminDA
    {
        private static string Cs()
        {
            return ConfigurationManager
                .ConnectionStrings["RecibosContext"].ConnectionString;
        }

        // ═══════════════════════════════════════════════════════════
        //  DASHBOARD
        // ═══════════════════════════════════════════════════════════

        /// <summary>Tarjetas de resumen en UN solo viaje (agregación condicional).</summary>
        public DashboardResumenRecibos ObtenerResumen(string empresa, int diasUmbral)
        {
            var res = new DashboardResumenRecibos { DiasUmbral = diasUmbral };

            const string sql = @"
                SELECT
                    SUM(CASE WHEN SYNC_ESTADO='DESCUADRE' THEN 1 ELSE 0 END) AS Descuadres,
                    SUM(CASE WHEN SYNC_ESTADO='DESCUADRE' THEN ISNULL(MONTO_T_DOC_GTQ,0) ELSE 0 END) AS DescuadresMonto,
                    SUM(CASE WHEN SYNC_ESTADO='PENDIENTE' THEN 1 ELSE 0 END) AS Pendientes,
                    SUM(CASE WHEN SYNC_ESTADO='PENDIENTE'
                              AND DATEDIFF(DAY, FECHA_REGISTRO, SYSDATETIME()) >= @dias
                             THEN 1 ELSE 0 END) AS Envejecidos,
                    SUM(CASE WHEN SYNC_ESTADO='PENDIENTE'
                              AND ISNULL(SYNC_OBSERVACION,'') LIKE '%Anulado en SAP%'
                             THEN 1 ELSE 0 END) AS Anulados,
                    SUM(CASE WHEN SYNC_ESTADO='OPERADO'
                              AND CAST(FECHA_OPERADO AS DATE) = CAST(SYSDATETIME() AS DATE)
                             THEN 1 ELSE 0 END) AS OperadosHoy,
                    SUM(CASE WHEN SYNC_ESTADO='OPERADO'
                              AND FECHA_OPERADO >= DATEADD(DAY,-7,SYSDATETIME())
                             THEN 1 ELSE 0 END) AS OperadosSemana
                FROM dbo.REC_CAJA_ENC
                WHERE ISNULL(STATUS,'A') <> 'X'
                  AND (@emp = '' OR ID_EMPRESA = @emp);";

            using (var cn = new SqlConnection(Cs()))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.AddWithValue("@emp", empresa ?? "");
                cmd.Parameters.AddWithValue("@dias", diasUmbral);
                cn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    if (rd.Read())
                    {
                        res.Descuadres = ValInt(rd["Descuadres"]);
                        res.DescuadresMontoGtq = ValDec(rd["DescuadresMonto"]);
                        res.PendientesTotal = ValInt(rd["Pendientes"]);
                        res.PendientesEnvejecidos = ValInt(rd["Envejecidos"]);
                        res.PendientesAnulados = ValInt(rd["Anulados"]);
                        res.OperadosHoy = ValInt(rd["OperadosHoy"]);
                        res.OperadosSemana = ValInt(rd["OperadosSemana"]);
                    }
                }
            }
            return res;
        }

        /// <summary>
        /// Recibos "con novedad" (DESCUADRE + PENDIENTE). La clasificación fina
        /// (ANULADO_SAP / ENVEJECIDO) se hace en C# para no duplicar reglas en SQL.
        /// </summary>
        public List<DashboardFilaRecibo> ObtenerDetalle(string empresa, int diasUmbral)
        {
            var lista = new List<DashboardFilaRecibo>();

            const string sql = @"
                SELECT ID_RECIBO, ID_EMPRESA, NOMBRE_CLIENTE, USUARIO,
                       ISNULL(MONTO_T_DOC_GTQ,0) AS MONTO_GTQ,
                       SYNC_ESTADO, ISNULL(SYNC_OBSERVACION,'') AS OBS,
                       FECHA_REGISTRO,
                       DATEDIFF(DAY, FECHA_REGISTRO, SYSDATETIME()) AS DIAS
                FROM dbo.REC_CAJA_ENC
                WHERE ISNULL(STATUS,'A') <> 'X'
                  AND SYNC_ESTADO IN ('PENDIENTE','DESCUADRE')
                  AND (@emp = '' OR ID_EMPRESA = @emp)
                ORDER BY
                    CASE SYNC_ESTADO WHEN 'DESCUADRE' THEN 0 ELSE 1 END,
                    DIAS DESC;";

            using (var cn = new SqlConnection(Cs()))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.AddWithValue("@emp", empresa ?? "");
                cn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        string estado = Convert.ToString(rd["SYNC_ESTADO"]);
                        string obs = Convert.ToString(rd["OBS"]);
                        int dias = ValInt(rd["DIAS"]);

                        string situacion;
                        if (estado == "DESCUADRE") situacion = "DESCUADRE";
                        else if (obs.IndexOf("Anulado en SAP",
                                 StringComparison.OrdinalIgnoreCase) >= 0) situacion = "ANULADO_SAP";
                        else if (dias >= diasUmbral) situacion = "ENVEJECIDO";
                        else situacion = "PENDIENTE";

                        lista.Add(new DashboardFilaRecibo
                        {
                            IdRecibo = Convert.ToString(rd["ID_RECIBO"]),
                            IdEmpresa = Convert.ToString(rd["ID_EMPRESA"]),
                            NombreCliente = Convert.ToString(rd["NOMBRE_CLIENTE"]),
                            Usuario = Convert.ToString(rd["USUARIO"]),
                            MontoGtq = ValDec(rd["MONTO_GTQ"]),
                            SyncEstado = estado,
                            SyncObservacion = obs,
                            FechaRegistro = rd["FECHA_REGISTRO"] == DBNull.Value ? ""
                                              : Convert.ToDateTime(rd["FECHA_REGISTRO"]).ToString("yyyy-MM-dd"),
                            DiasAntiguedad = dias,
                            Situacion = situacion
                        });
                    }
                }
            }
            return lista;
        }

        // ═══════════════════════════════════════════════════════════
        //  SERIES (CRUD)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Todas las series con su máximo correlativo REAL ya emitido.
        /// TRY_CAST protege contra IDs con sufijo no numérico (devuelven NULL).
        /// </summary>
        public List<ReciboCajaSerie> ObtenerSeries()
        {
            var lista = new List<ReciboCajaSerie>();

            const string sql = @"
                SELECT S.ROWID, S.EMPRESA, S.DEPTO, S.SERIE, S.NUMERACION,
                       ISNULL(S.SERIE_NC,'')      AS SERIE_NC,
                       ISNULL(S.NUMERACION_NC,0)  AS NUMERACION_NC,
                       ISNULL(M.MAX_USADO, 0)     AS MAX_USADO
                FROM dbo.REC_CAJA_SERIES S
                OUTER APPLY (
                    SELECT MAX(TRY_CAST(RIGHT(E.ID_RECIBO, 5) AS INT)) AS MAX_USADO
                    FROM dbo.REC_CAJA_ENC E
                    WHERE E.ID_EMPRESA = S.EMPRESA
                      AND E.ID_RECIBO LIKE S.SERIE + '%'
                ) M
                ORDER BY S.EMPRESA, S.DEPTO;";

            using (var cn = new SqlConnection(Cs()))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                        lista.Add(new ReciboCajaSerie
                        {
                            RowId = ValInt(rd["ROWID"]),
                            Empresa = Convert.ToString(rd["EMPRESA"]),
                            Depto = Convert.ToString(rd["DEPTO"]),
                            Serie = Convert.ToString(rd["SERIE"]),
                            Numeracion = ValInt(rd["NUMERACION"]),
                            SerieNc = Convert.ToString(rd["SERIE_NC"]),
                            NumeracionNc = ValInt(rd["NUMERACION_NC"]),
                            MaxUsado = ValInt(rd["MAX_USADO"])
                        });
                }
            }
            return lista;
        }

        /// <summary>¿Existe ya (EMPRESA, DEPTO)? excluyendo opcionalmente un ROWID (edición).</summary>
        public bool ExisteEmpresaDepto(string empresa, string depto, int excluirRowId)
        {
            const string sql = @"
                SELECT COUNT(*) FROM dbo.REC_CAJA_SERIES
                WHERE EMPRESA = @emp AND DEPTO = @depto AND ROWID <> @rowid;";
            using (var cn = new SqlConnection(Cs()))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.AddWithValue("@emp", empresa);
                cmd.Parameters.AddWithValue("@depto", depto);
                cmd.Parameters.AddWithValue("@rowid", excluirRowId);
                cn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        /// <summary>¿Existe ya esa SERIE en la empresa? (el prefijo debe ser único).</summary>
        public bool ExisteSerie(string empresa, string serie, int excluirRowId)
        {
            const string sql = @"
                SELECT COUNT(*) FROM dbo.REC_CAJA_SERIES
                WHERE EMPRESA = @emp AND SERIE = @serie AND ROWID <> @rowid;";
            using (var cn = new SqlConnection(Cs()))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.AddWithValue("@emp", empresa);
                cmd.Parameters.AddWithValue("@serie", serie);
                cmd.Parameters.AddWithValue("@rowid", excluirRowId);
                cn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        /// <summary>Máximo correlativo emitido para una serie (0 si nunca emitió).</summary>
        public int ObtenerMaxUsado(string empresa, string serie)
        {
            const string sql = @"
                SELECT ISNULL(MAX(TRY_CAST(RIGHT(ID_RECIBO,5) AS INT)), 0)
                FROM dbo.REC_CAJA_ENC
                WHERE ID_EMPRESA = @emp AND ID_RECIBO LIKE @serie + '%';";
            using (var cn = new SqlConnection(Cs()))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.AddWithValue("@emp", empresa);
                cmd.Parameters.AddWithValue("@serie", serie);
                cn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public void InsertarSerie(ReciboCajaSerie s)
        {
            const string sql = @"
                INSERT INTO dbo.REC_CAJA_SERIES
                    (EMPRESA, DEPTO, SERIE, NUMERACION, SERIE_NC, NUMERACION_NC)
                VALUES (@emp, @depto, @serie, @num, @serieNc, @numNc);";
            using (var cn = new SqlConnection(Cs()))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.AddWithValue("@emp", s.Empresa);
                cmd.Parameters.AddWithValue("@depto", s.Depto);
                cmd.Parameters.AddWithValue("@serie", s.Serie);
                cmd.Parameters.AddWithValue("@num", s.Numeracion);
                cmd.Parameters.AddWithValue("@serieNc", s.SerieNc ?? "");
                cmd.Parameters.AddWithValue("@numNc", s.NumeracionNc);
                cn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void ActualizarSerie(ReciboCajaSerie s)
        {
            const string sql = @"
                UPDATE dbo.REC_CAJA_SERIES
                SET EMPRESA = @emp, DEPTO = @depto, SERIE = @serie,
                    NUMERACION = @num, SERIE_NC = @serieNc, NUMERACION_NC = @numNc
                WHERE ROWID = @rowid;";
            using (var cn = new SqlConnection(Cs()))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.AddWithValue("@rowid", s.RowId);
                cmd.Parameters.AddWithValue("@emp", s.Empresa);
                cmd.Parameters.AddWithValue("@depto", s.Depto);
                cmd.Parameters.AddWithValue("@serie", s.Serie);
                cmd.Parameters.AddWithValue("@num", s.Numeracion);
                cmd.Parameters.AddWithValue("@serieNc", s.SerieNc ?? "");
                cmd.Parameters.AddWithValue("@numNc", s.NumeracionNc);
                cn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public ReciboCajaSerie ObtenerSeriePorRowId(int rowId)
        {
            const string sql = @"
                SELECT ROWID, EMPRESA, DEPTO, SERIE, NUMERACION,
                       ISNULL(SERIE_NC,'') AS SERIE_NC, ISNULL(NUMERACION_NC,0) AS NUMERACION_NC
                FROM dbo.REC_CAJA_SERIES WHERE ROWID = @rowid;";
            using (var cn = new SqlConnection(Cs()))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.AddWithValue("@rowid", rowId);
                cn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    if (!rd.Read()) return null;
                    return new ReciboCajaSerie
                    {
                        RowId = ValInt(rd["ROWID"]),
                        Empresa = Convert.ToString(rd["EMPRESA"]),
                        Depto = Convert.ToString(rd["DEPTO"]),
                        Serie = Convert.ToString(rd["SERIE"]),
                        Numeracion = ValInt(rd["NUMERACION"]),
                        SerieNc = Convert.ToString(rd["SERIE_NC"]),
                        NumeracionNc = ValInt(rd["NUMERACION_NC"])
                    };
                }
            }
        }

        public void EliminarSerie(int rowId)
        {
            const string sql = "DELETE FROM dbo.REC_CAJA_SERIES WHERE ROWID = @rowid;";
            using (var cn = new SqlConnection(Cs()))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.AddWithValue("@rowid", rowId);
                cn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // Helpers
        private static int ValInt(object o) =>
            o != null && o != DBNull.Value ? Convert.ToInt32(o) : 0;
        private static decimal ValDec(object o) =>
            o != null && o != DBNull.Value ? Convert.ToDecimal(o) : 0m;
    }
}