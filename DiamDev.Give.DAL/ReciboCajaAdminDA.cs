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
        //  ALCANCE POR USUARIO_EMPRESA
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Construye el fragmento SQL del alcance y REGISTRA sus parámetros
        /// en el comando. Devuelve algo como:
        ///
        ///   AND ( (ID_EMPRESA=@alcEmp0 AND LTRIM(RTRIM(CODIGO_USUARIO_EMPRESA))=@alcCod0)
        ///      OR (ID_EMPRESA=@alcEmp1 AND ...) )
        ///
        /// ¿Por qué SQL dinámico en un proyecto donde nunca concatenamos?
        /// Porque lo que varía es la CANTIDAD de condiciones, no los valores:
        /// cada valor sigue viajando como parámetro con nombre. No hay ni un
        /// dato del usuario dentro del string → no hay superficie de inyección.
        /// La alternativa (Table-Valued Parameter) obligaría a crear un tipo
        /// de tabla en LAS DOS bases; no vale el costo para ~10 pares.
        ///
        /// Tres retornos posibles:
        ///   Global      → ""          (sin restricción)
        ///   Con pares   → el OR-block
        ///   Sin pares   → " AND 1 = 0 " ← FALLA CERRADO. Un usuario sin
        ///                 asignaciones no ve NADA. Devolver "" aquí sería el
        ///                 bug clásico: el que no tiene permisos, lo ve todo.
        ///
        /// Los recibos con CODIGO_USUARIO_EMPRESA NULL quedan fuera SIN código
        /// extra: en SQL, NULL = @x da UNKNOWN (no TRUE), y el WHERE solo deja
        /// pasar lo TRUE. Eso implementa la decisión "los NULL no los ve nadie".
        ///
        /// IMPORTANTE: llamar UNA sola vez por comando. Dos llamadas duplicarían
        /// los nombres de parámetro (@alcEmp0 ya existe) y SqlCommand tronaría.
        /// El string resultante SÍ se puede reusar en varios SELECT del batch.
        /// </summary>
        private static string PredicadoAlcance(SqlCommand cmd, AlcanceRecibos alcance)
        {
            if (alcance == null || alcance.Global) return "";
            if (alcance.Pares == null || alcance.Pares.Count == 0) return " AND 1 = 0 ";

            var ors = new List<string>();
            for (int i = 0; i < alcance.Pares.Count; i++)
            {
                string pEmp = "@alcEmp" + i;
                string pCod = "@alcCod" + i;

                ors.Add("(ID_EMPRESA = " + pEmp +
                        " AND LTRIM(RTRIM(CODIGO_USUARIO_EMPRESA)) = " + pCod + ")");

                cmd.Parameters.AddWithValue(pEmp, (alcance.Pares[i].Empresa ?? "").Trim());
                cmd.Parameters.AddWithValue(pCod, (alcance.Pares[i].Codigo ?? "").Trim());
            }

            return " AND (" + string.Join(" OR ", ors) + ") ";
        }

        // ═══════════════════════════════════════════════════════════
        //  DASHBOARD
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Tarjetas de resumen. Son DOS consultas en UN solo viaje al servidor
        /// (batch + NextResult), porque miden universos opuestos:
        ///
        ///   Resultado 1 → salud operativa: SOLO recibos vivos (STATUS &lt;&gt; 'X').
        ///   Resultado 2 → anulaciones web: SOLO recibos muertos (STATUS = 'X'),
        ///                 del mes en curso, medidos por FECHA_ANULACION.
        ///
        /// El ALCANCE se aplica a las dos: si un anulado no es del usuario,
        /// tampoco debe contarlo en su card. Se calcula UNA vez y se pega en
        /// ambos WHERE (los parámetros son compartidos por todo el batch).
        /// </summary>
        public DashboardResumenRecibos ObtenerResumen(string empresa, int diasUmbral,
            AlcanceRecibos alcance)
        {
            var res = new DashboardResumenRecibos { DiasUmbral = diasUmbral };

            using (var cn = new SqlConnection(Cs()))
            using (var cmd = new SqlCommand())
            {
                cmd.Connection = cn;

                // UNA sola llamada: registra los @alcEmpN/@alcCodN en el comando.
                string alc = PredicadoAlcance(cmd, alcance);

                // DATEADD(MONTH, DATEDIFF(MONTH, 0, fecha), 0) = "primer día de ese
                // mes a las 00:00". Truco clásico, funciona en cualquier versión
                // de SQL Server (DATEFROMPARTS exige 2012+).
                cmd.CommandText = @"
                    -- ══ Resultado 1: salud operativa (recibos VIVOS) ══
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
                      AND (@emp = '' OR ID_EMPRESA = @emp)" + alc + @";

                    -- ══ Resultado 2: anulados en la WEB, mes en curso (recibos MUERTOS) ══
                    SELECT
                        COUNT(*)                                   AS AnuladosMes,
                        ISNULL(SUM(ISNULL(MONTO_T_DOC_GTQ,0)), 0)  AS AnuladosMesMonto
                    FROM dbo.REC_CAJA_ENC
                    WHERE ISNULL(STATUS,'A') = 'X'
                      AND (@emp = '' OR ID_EMPRESA = @emp)
                      AND FECHA_ANULACION >= DATEADD(MONTH, DATEDIFF(MONTH, 0, SYSDATETIME()), 0)
                      AND FECHA_ANULACION <  DATEADD(MONTH, DATEDIFF(MONTH, 0, SYSDATETIME()) + 1, 0)" + alc + @";";

                cmd.Parameters.AddWithValue("@emp", empresa ?? "");
                cmd.Parameters.AddWithValue("@dias", diasUmbral);
                cn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    // ── Result set 1 ──
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

                    // ── Result set 2: avanzamos el lector al siguiente SELECT ──
                    if (rd.NextResult() && rd.Read())
                    {
                        res.AnuladosMes = ValInt(rd["AnuladosMes"]);
                        res.AnuladosMesMontoGtq = ValDec(rd["AnuladosMesMonto"]);
                    }
                }
            }
            return res;
        }

        /// <summary>
        /// Recibos para el grid del dashboard. Tres universos posibles:
        ///  - VIVOS (siempre): DESCUADRE + PENDIENTE, y OPERADO si incluirOperados.
        ///  - +ANULADOS (incluirAnulados=true): agrega los STATUS='X' al final.
        ///  - SOLO ANULADOS (soloAnulados=true): EXCLUYE el universo vivo.
        ///
        /// Sobre TODO eso se aplica el ALCANCE (Usuario_Empresa). Va DENTRO de la
        /// tabla derivada, o sea antes del TOP 500: filtrar después de un TOP es
        /// el mismo error que ya corregimos con soloAnulados — el tope se llenaría
        /// con recibos ajenos y los propios nunca llegarían.
        ///
        /// FECHA_FILTRO: el rango se aplica sobre FECHA_ANULACION si el recibo
        /// está anulado, y sobre FECHA_REGISTRO si no. Sin esto la card
        /// "Anulados (mes)" nunca cuadraría con el grid.
        /// </summary>
        public List<DashboardFilaRecibo> ObtenerDetalle(string empresa, int diasUmbral,
            DateTime? fechaIni, DateTime? fechaFin, bool incluirOperados,
            bool incluirAnulados, bool soloAnulados, AlcanceRecibos alcance)
        {
            var lista = new List<DashboardFilaRecibo>();

            using (var cn = new SqlConnection(Cs()))
            using (var cmd = new SqlCommand())
            {
                cmd.Connection = cn;
                string alc = PredicadoAlcance(cmd, alcance);

                cmd.CommandText = @"
                    SELECT TOP 500
                           T.ID_RECIBO, T.ID_EMPRESA, T.NOMBRE_CLIENTE, T.USUARIO,
                           T.MONTO_GTQ, T.SYNC_ESTADO, T.OBS, T.FECHA_REGISTRO, T.DIAS,
                           T.ES_ANULADO, T.ANULADO_POR, T.FECHA_ANULACION, T.MOTIVO
                    FROM (
                        SELECT
                            ID_RECIBO, ID_EMPRESA, NOMBRE_CLIENTE, USUARIO,
                            ISNULL(MONTO_T_DOC_GTQ, 0)   AS MONTO_GTQ,
                            ISNULL(SYNC_ESTADO, '')      AS SYNC_ESTADO,
                            ISNULL(SYNC_OBSERVACION, '') AS OBS,
                            FECHA_REGISTRO,
                            DATEDIFF(DAY, FECHA_REGISTRO, SYSDATETIME()) AS DIAS,
                            CASE WHEN ISNULL(STATUS,'A') = 'X' THEN 1 ELSE 0 END AS ES_ANULADO,
                            ISNULL(ANULADO_POR, '')      AS ANULADO_POR,
                            FECHA_ANULACION,
                            ISNULL(MOTIVO, '')           AS MOTIVO,
                            CASE WHEN ISNULL(STATUS,'A') = 'X'
                                 THEN ISNULL(FECHA_ANULACION, FECHA_REGISTRO)
                                 ELSE FECHA_REGISTRO
                            END                          AS FECHA_FILTRO
                        FROM dbo.REC_CAJA_ENC
                        WHERE (@emp = '' OR ID_EMPRESA = @emp)
                          AND (
                                -- Universo VIVO: se apaga por completo si soloAnulados
                                ( @soloAnul = 0
                                  AND ISNULL(STATUS,'A') <> 'X'
                                  AND ( SYNC_ESTADO IN ('PENDIENTE','DESCUADRE')
                                        OR (@incOper = 1 AND SYNC_ESTADO = 'OPERADO') ) )
                                -- Universo MUERTO: por petición explícita o modo exclusivo
                             OR ( (@incAnul = 1 OR @soloAnul = 1)
                                  AND ISNULL(STATUS,'A') = 'X' )
                              )" + alc + @"
                    ) T
                    WHERE (@fIni IS NULL OR T.FECHA_FILTRO >= @fIni)
                      AND (@fFin IS NULL OR T.FECHA_FILTRO < DATEADD(DAY, 1, @fFin))
                    ORDER BY
                        -- 1) Grupo: primero lo urgente, los muertos hasta el fondo
                        CASE WHEN T.ES_ANULADO = 1          THEN 3
                             WHEN T.SYNC_ESTADO='DESCUADRE' THEN 0
                             WHEN T.SYNC_ESTADO='PENDIENTE' THEN 1
                             ELSE 2 END,
                        -- 2) Vivos: el más atrasado primero. Anulados: constante 0.
                        CASE WHEN T.ES_ANULADO = 1 THEN 0 ELSE T.DIAS END DESC,
                        -- 3) Anulados: la anulación más RECIENTE primero (bitácora,
                        --    no cola de trabajo).
                        T.FECHA_FILTRO DESC;";

                cmd.Parameters.AddWithValue("@emp", empresa ?? "");
                cmd.Parameters.AddWithValue("@incOper", incluirOperados ? 1 : 0);
                cmd.Parameters.AddWithValue("@incAnul", incluirAnulados ? 1 : 0);
                cmd.Parameters.AddWithValue("@soloAnul", soloAnulados ? 1 : 0);
                cmd.Parameters.AddWithValue("@fIni", (object)fechaIni ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@fFin", (object)fechaFin ?? DBNull.Value);
                cn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        string estado = Convert.ToString(rd["SYNC_ESTADO"]);
                        string obs = Convert.ToString(rd["OBS"]);
                        int dias = ValInt(rd["DIAS"]);

                        bool esAnulado = ValInt(rd["ES_ANULADO"]) == 1;
                        string anuladoPor = Convert.ToString(rd["ANULADO_POR"]);
                        string motivo = Convert.ToString(rd["MOTIVO"]);
                        DateTime? fAnul = rd["FECHA_ANULACION"] == DBNull.Value
                                            ? (DateTime?)null
                                            : Convert.ToDateTime(rd["FECHA_ANULACION"]);

                        // ── Clasificación ──
                        // ES_ANULADO manda sobre todo lo demás: un recibo muerto
                        // no es "pendiente" ni "envejecido", aunque sus columnas
                        // de sincronización digan eso. STATUS y SYNC_ESTADO son
                        // ortogonales, pero para la UI el ciclo de vida gana.
                        string situacion;
                        if (esAnulado) situacion = "ANULADO";
                        else if (estado == "DESCUADRE") situacion = "DESCUADRE";
                        else if (estado == "OPERADO") situacion = "OPERADO";
                        else if (obs.IndexOf("Anulado en SAP",
                                 StringComparison.OrdinalIgnoreCase) >= 0) situacion = "ANULADO_SAP";
                        else if (dias >= diasUmbral) situacion = "ENVEJECIDO";
                        else situacion = "PENDIENTE";

                        // ── Observación legible para anulados ──
                        if (esAnulado)
                        {
                            string txt = "Anulado";
                            if (anuladoPor.Length > 0) txt += " por " + anuladoPor;
                            if (fAnul.HasValue) txt += " el " + fAnul.Value.ToString("dd/MM/yyyy HH:mm");
                            if (motivo.Length > 0) txt += " · " + motivo;
                            obs = txt;
                        }

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
                            Situacion = situacion,
                            AnuladoPor = anuladoPor,
                            FechaAnulacion = fAnul.HasValue
                                              ? fAnul.Value.ToString("yyyy-MM-dd HH:mm") : "",
                            MotivoAnulacion = motivo
                        });
                    }
                }
            }
            return lista;
        }

        // ═══════════════════════════════════════════════════════════
        //  SERIES (CRUD)  —  sin cambios
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