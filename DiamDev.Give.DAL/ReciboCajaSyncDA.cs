using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using DiamDev.Give.Entities;
using System.Linq;

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

        // ── Lote de la pasada INVERSA (OPERADO/DESCUADRE) ──────────────────
        // Separado del lote de PENDIENTES a propósito: son colas de naturaleza
        // distinta. PENDIENTE es un estado de flujo que se autolimpia (~12 hoy);
        // OPERADO es acumulativo y crece para siempre (834 hoy, ~950/mes).
        private const int LOTE_REVISION_FALLBACK = 300;

        // Techo duro. SQL Server admite MÁXIMO 2,100 parámetros por comando, y
        // ObtenerDatosConciliacion / MarcarUltimoCheckLote generan UNO POR ID.
        // Sin este techo, al superar ~2,098 operados el comando lanzaría
        // SqlException, ProcesarEmpresa se lo tragaría en su catch, y la
        // detección de descuadres moriría EN SILENCIO para esa empresa.
        // Proyección con el ritmo de GRACO (~25 operados/día): ~octubre 2026.
        private const int LOTE_REVISION_MAXIMO = 1500;

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

        /// <summary>
        /// Tamaño de lote de la pasada inversa (App.config -> SyncLoteRevision).
        /// Si el setting falta o es inválido, usa LOTE_REVISION_FALLBACK (300).
        /// El valor se acota a [1, LOTE_REVISION_MAXIMO] en el punto de uso.
        /// </summary>
        private int LoteRevisionDefault
        {
            get
            {
                string raw = ConfigurationManager.AppSettings["SyncLoteRevision"];
                return (int.TryParse(raw, out int v) && v > 0) ? v : LOTE_REVISION_FALLBACK;
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
        /// ★ Devuelve ID_RECIBO -> ID_CLIENTE (antes solo List&lt;string&gt;).
        /// El cliente se usa para validar contra ORCT.CardCode antes de marcar
        /// OPERADO: un typo en el UDF U_Recibocaja_Webapp puede apuntar al
        /// recibo equivocado, y sin esta comparación el sync lo daría por bueno.
        /// Sale de la misma consulta: cero costo adicional.
        ///
        /// El orden del ORDER BY se aplica en SQL (define QUÉ entra en el TOP N).
        /// El Dictionary no preserva ese orden, pero da igual: aguas abajo solo
        /// se usa para armar lotes de IN(...), donde el orden es irrelevante.
        ///
        /// top = null  -> usa el lote configurado en App.config (recomendado).
        /// top = N     -> fuerza un tamaño puntual (útil en pruebas/tests).
        /// </summary>
        public Dictionary<string, string> ObtenerRecibosPendientes(string empresa, int? top = null)
        {
            int lote = top ?? LoteDefault;

            var mapa = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            const string sql = @"
                SELECT TOP (@top) ID_RECIBO, ISNULL(ID_CLIENTE, '') AS ID_CLIENTE
                FROM dbo.REC_CAJA_ENC
                WHERE SYNC_ESTADO = 'PENDIENTE'
                AND ID_EMPRESA  = @empresa
                AND ISNULL(STATUS, 'A') <> 'X'   -- anulados en web: invisibles para el sync
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
                    {
                        // Indexador y no Add(): si alguna vez hubiera un ID_RECIBO
                        // repetido dentro de la misma empresa, Add lanzaría y
                        // tumbaría el ciclo completo. El indexador solo sobrescribe.
                        mapa[rd["ID_RECIBO"].ToString()] = rd["ID_CLIENTE"].ToString();
                    }
            }
            return mapa;
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
                    AND SYNC_ESTADO = 'PENDIENTE'
                    AND ISNULL(STATUS, 'A') <> 'X';"; // carrera: anulado durante el viaje a HANA

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

        /// <summary>
        /// Deja constancia de que un pago de SAP dice apuntar a este recibo pero
        /// pertenece a OTRO cliente (típicamente, un dígito mal tecleado en el UDF
        /// U_Recibocaja_Webapp). NO cambia SYNC_ESTADO: el recibo sigue PENDIENTE
        /// hasta que Créditos corrija el enlace en SAP.
        ///
        /// Idempotente por diseño: el WHERE incluye
        ///     ISNULL(SYNC_OBSERVACION,'') &lt;&gt; @obs
        /// así que a partir del segundo ciclo el UPDATE afecta 0 filas y no
        /// genera escritura. Por eso el mensaje NO debe llevar fecha/hora: si
        /// cambiara en cada vuelta, escribiríamos una vez por ciclo, para siempre.
        /// </summary>
        public void MarcarPosibleErrorEnlace(string idRecibo, string empresa, string observacion)
        {
            const string sql = @"
                UPDATE dbo.REC_CAJA_ENC
                SET SYNC_OBSERVACION  = @obs,
                    SYNC_ULTIMO_CHECK = SYSDATETIME()
                WHERE ID_RECIBO  = @idRecibo
                  AND ID_EMPRESA = @empresa
                  AND SYNC_ESTADO = 'PENDIENTE'
                  AND ISNULL(STATUS, 'A') <> 'X'
                  AND ISNULL(SYNC_OBSERVACION, '') <> @obs;";

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
        /// [OBSOLETO — sustituido por ObtenerRecibosParaRevision]
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
                    AND SYNC_ESTADO = 'OPERADO'
                    AND ISNULL(STATUS, 'A') <> 'X';"; // no "revivir" recibos anulados en web

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

        /// <summary>
        /// Marca (o limpia) la bandera de conciliación de un recibo OPERADO.
        /// NO cambia SYNC_ESTADO: la conciliación es informativa, no correctiva.
        /// Usa el prefijo [CONCIL] para no pisar observaciones de la pasada inversa.
        ///
        /// observacion = texto  -> escribe "[CONCIL] ..." 
        /// observacion = null   -> limpia SOLO la parte [CONCIL] si existía (cuadra bien).
        /// </summary>
        public void MarcarConciliacion(string idRecibo, string empresa, string observacion)
        {
            // Estrategia simple y segura: guardamos la observación de conciliación en su
            // propia forma. Como hoy la única otra fuente que escribe SYNC_OBSERVACION es
            // la pasada inversa (anulado/reapuntado), y esos casos NO conviven con una
            // conciliación exitosa en la misma vuelta, un overwrite controlado es suficiente.
            // Si hay descuadre, gana el mensaje de conciliación (es lo accionable).
            const string sqlSet = @"
        UPDATE dbo.REC_CAJA_ENC
        SET SYNC_OBSERVACION  = @obs,
            SYNC_ULTIMO_CHECK = SYSDATETIME()
        WHERE ID_RECIBO  = @idRecibo
          AND ID_EMPRESA = @empresa
          AND SYNC_ESTADO = 'OPERADO';";

            // Si cuadra (observacion null), solo limpiamos si lo que hay es una marca [CONCIL].
            const string sqlClear = @"
        UPDATE dbo.REC_CAJA_ENC
        SET SYNC_OBSERVACION  = NULL,
            SYNC_ULTIMO_CHECK = SYSDATETIME()
        WHERE ID_RECIBO  = @idRecibo
          AND ID_EMPRESA = @empresa
          AND SYNC_ESTADO = 'OPERADO'
          AND SYNC_OBSERVACION LIKE '[[]CONCIL]%';";  // escapado: corchete literal

            bool cuadra = string.IsNullOrEmpty(observacion);
            string sql = cuadra ? sqlClear : sqlSet;

            using (var cn = new SqlConnection(ConnString))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.AddWithValue("@idRecibo", idRecibo);
                cmd.Parameters.AddWithValue("@empresa", empresa);
                if (!cuadra)
                    cmd.Parameters.AddWithValue("@obs", "[CONCIL] " + observacion);
                cn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Trae, para un lote de recibos, la MONEDA y los montos de conciliación.
        /// Un solo viaje a SQL.
        ///
        /// ★ Ahora también trae MONTO_T_REC. Es el que se compara contra
        ///   ORCT.DocTotal: ambos significan "cuánto dinero entró".
        ///   MONTO_T_DOC se conserva porque es el que corresponde a las líneas
        ///   del detalle (nivel informativo), no al dinero.
        /// </summary>
        public Dictionary<string, ReciboMontoSql> ObtenerDatosConciliacion(string empresa, List<string> idsRecibo)
        {
            var mapa = new Dictionary<string, ReciboMontoSql>(StringComparer.OrdinalIgnoreCase);
            if (idsRecibo == null || idsRecibo.Count == 0) return mapa;

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
                    "SELECT ID_RECIBO, MONEDA, MONTO_T_DOC, MONTO_T_REC " +
                    "FROM dbo.REC_CAJA_ENC " +
                    "WHERE ID_EMPRESA = @empresa AND ID_RECIBO IN (" + string.Join(",", nombres) + ");";
                cn.Open();
                using (var rd = cmd.ExecuteReader())
                    while (rd.Read())
                    {
                        string id = rd["ID_RECIBO"].ToString();
                        mapa[id] = new ReciboMontoSql
                        {
                            IdRecibo = id,
                            Moneda = rd["MONEDA"] == DBNull.Value ? "GTQ" : rd["MONEDA"].ToString(),
                            MontoTDoc = rd["MONTO_T_DOC"] == DBNull.Value ? 0m
                                                                          : Convert.ToDecimal(rd["MONTO_T_DOC"]),
                            MontoTRec = rd["MONTO_T_REC"] == DBNull.Value ? 0m
                                                                          : Convert.ToDecimal(rd["MONTO_T_REC"])
                        };
                    }
            }
            return mapa;
        }

        // ═══════════════════════════════════════════════════════════
        //  FASE 5 — DESCUADRE (anulación parcial en SAP)
        // ═══════════════════════════════════════════════════════════

        private static string CsRecibosF5()
        {
            return System.Configuration.ConfigurationManager
                .ConnectionStrings["RecibosContext"].ConnectionString;
        }

        /// <summary>
        /// Recibos que la pasada inversa debe revisar: OPERADO **y** DESCUADRE
        /// (este último para el self-healing). Solo recibos locales activos.
        ///
        /// ★ FIX 1 — COLA ROTATIVA. Antes NO tenía TOP: traía TODOS los operados
        /// históricos de la empresa en cada ciclo. El trabajo escalaba con el
        /// archivo histórico, no con la cola de trabajo real. Ahora usa el mismo
        /// patrón que ObtenerRecibosPendientes: lote acotado + rotación por
        /// SYNC_ULTIMO_CHECK. TODOS se siguen revisando; lo que cambia es que
        /// se reparten entre varios ciclos en vez de todos de golpe.
        ///
        /// ★ FIX 2 — LOS DESCUADRE VAN SIEMPRE PRIMERO (grupo 0 del ORDER BY).
        /// Son pocos (11 hoy) y son lo accionable: no pueden quedar esperando
        /// su turno de rotación detrás de cientos de operados que no cambian.
        ///
        /// ★ FIX 3 — TECHO DURO (ver LOTE_REVISION_MAXIMO): evita el
        /// SqlException por límite de parámetros aguas abajo.
        ///
        /// ★ FIX 4 — se trae SYNC_OBSERVACION para que ConciliarRecibo decida
        /// en memoria si hay marca [CONCIL] que limpiar.
        ///
        /// top = null -> usa el lote de App.config (recomendado).
        /// top = N    -> fuerza un tamaño puntual (pruebas/diagnóstico).
        /// </summary>
        public List<ReciboRevisionSql> ObtenerRecibosParaRevision(string empresa, int? top = null)
        {
            int lote = top ?? LoteRevisionDefault;
            if (lote < 1) lote = 1;
            if (lote > LOTE_REVISION_MAXIMO) lote = LOTE_REVISION_MAXIMO;

            var lista = new List<ReciboRevisionSql>();

            const string sql = @"
                SELECT TOP (@top)
                       ID_RECIBO,
                       ISNULL(SAP_DOCENTRY, 0)      AS SAP_DOCENTRY,
                       ISNULL(SAP_DOCNUM, 0)        AS SAP_DOCNUM,
                       SYNC_ESTADO,
                       ISNULL(SYNC_OBSERVACION, '') AS SYNC_OBSERVACION
                FROM dbo.REC_CAJA_ENC
                WHERE ID_EMPRESA  = @emp
                  AND STATUS      = 'A'
                  AND SYNC_ESTADO IN ('OPERADO','DESCUADRE')
                ORDER BY
                    -- 1) Los DESCUADRE nunca esperan turno: son lo accionable.
                    CASE WHEN SYNC_ESTADO = 'DESCUADRE' THEN 0 ELSE 1 END,
                    -- 2) Los nunca revisados (NULL) antes que los ya sellados.
                    CASE WHEN SYNC_ULTIMO_CHECK IS NULL THEN 0 ELSE 1 END,
                    -- 3) Luego el más viejo sin revisar.
                    SYNC_ULTIMO_CHECK ASC,
                    -- 4) Desempate estable por PK. Sin esto, dos filas con el
                    --    mismo timestamp podrían alternarse entre ciclos y una
                    --    quedaría sin revisarse nunca.
                    ROWID ASC;";

            using (var cn = new SqlConnection(CsRecibosF5()))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.AddWithValue("@top", lote);
                cmd.Parameters.AddWithValue("@emp", empresa);
                cn.Open();
                using (var rd = cmd.ExecuteReader())
                    while (rd.Read())
                        lista.Add(new ReciboRevisionSql
                        {
                            IdRecibo = Convert.ToString(rd["ID_RECIBO"]),
                            SapDocEntry = Convert.ToInt32(rd["SAP_DOCENTRY"]),
                            SapDocNum = Convert.ToInt32(rd["SAP_DOCNUM"]),
                            SyncEstado = Convert.ToString(rd["SYNC_ESTADO"]),
                            SyncObservacion = Convert.ToString(rd["SYNC_OBSERVACION"])
                        });
            }
            return lista;
        }

        /// <summary>OPERADO/DESCUADRE → DESCUADRE, con la observación del descuadre.</summary>
        public void MarcarReciboDescuadre(string idRecibo, string empresa, string observacion)
        {
            using (var cn = new SqlConnection(CsRecibosF5()))
            {
                cn.Open();
                using (var cmd = new SqlCommand(@"
                    UPDATE dbo.REC_CAJA_ENC
                    SET SYNC_ESTADO       = 'DESCUADRE',
                        SYNC_OBSERVACION  = @obs,
                        SYNC_ULTIMO_CHECK = SYSDATETIME()
                    WHERE ID_RECIBO = @id AND ID_EMPRESA = @emp", cn))
                {
                    cmd.Parameters.AddWithValue("@id", idRecibo);
                    cmd.Parameters.AddWithValue("@emp", empresa);
                    cmd.Parameters.AddWithValue("@obs", (object)observacion ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>DESCUADRE → OPERADO (self-healing: SAP volvió a cuadrar).</summary>
        public void MarcarReciboCuadrado(string idRecibo, string empresa, string observacion)
        {
            using (var cn = new SqlConnection(CsRecibosF5()))
            {
                cn.Open();
                using (var cmd = new SqlCommand(@"
                    UPDATE dbo.REC_CAJA_ENC
                    SET SYNC_ESTADO       = 'OPERADO',
                        SYNC_OBSERVACION  = @obs,
                        SYNC_ULTIMO_CHECK = SYSDATETIME()
                    WHERE ID_RECIBO = @id AND ID_EMPRESA = @emp", cn))
                {
                    cmd.Parameters.AddWithValue("@id", idRecibo);
                    cmd.Parameters.AddWithValue("@emp", empresa);
                    cmd.Parameters.AddWithValue("@obs", (object)observacion ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>Borra las marcas SYNC_DOC_ESTADO del detalle (al sanar o al regresar a PENDIENTE).</summary>
        public void LimpiarMarcasDetalle(string idRecibo, string empresa)
        {
            using (var cn = new SqlConnection(CsRecibosF5()))
            {
                cn.Open();
                using (var cmd = new SqlCommand(@"
                    UPDATE dbo.REC_CAJA_DET
                    SET SYNC_DOC_ESTADO = NULL
                    WHERE ID_RECIBO = @id AND ID_EMPRESA = @emp", cn))
                {
                    cmd.Parameters.AddWithValue("@id", idRecibo);
                    cmd.Parameters.AddWithValue("@emp", empresa);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Marca las líneas del detalle según lo que SAP reporta:
        ///  - FACTURA/PEDIDO cuyo NO_DOCUMENTO está aplicado por pagos ACTIVOS → 'APLICADO'
        ///  - FACTURA/PEDIDO que NO está en esa lista → 'ANULADO_SAP'
        /// Las líneas ANTICIPO/SALDO PENDIENTE (NO_DOCUMENTO NULL) no se marcan:
        /// las cubre el estado global DESCUADRE del encabezado.
        /// </summary>
        public void MarcarLineasAnuladas(string idRecibo, string empresa, List<string> facturasActivas)
        {
            facturasActivas = facturasActivas ?? new List<string>();

            using (var cn = new SqlConnection(CsRecibosF5()))
            {
                cn.Open();
                using (var tx = cn.BeginTransaction())
                {
                    // 1) Por defecto, toda línea FACTURA/PEDIDO queda ANULADO_SAP
                    using (var cmd = new SqlCommand(@"
                        UPDATE dbo.REC_CAJA_DET
                        SET SYNC_DOC_ESTADO = 'ANULADO_SAP'
                        WHERE ID_RECIBO = @id AND ID_EMPRESA = @emp
                          AND TIPO_DOC IN ('FACTURA','PEDIDO')", cn, tx))
                    {
                        cmd.Parameters.AddWithValue("@id", idRecibo);
                        cmd.Parameters.AddWithValue("@emp", empresa);
                        cmd.ExecuteNonQuery();
                    }

                    // 2) Las que SAP reporta aplicadas por pagos ACTIVOS → APLICADO
                    if (facturasActivas.Count > 0)
                    {
                        var nombres = facturasActivas.Select((f, i) => "@f" + i).ToList();
                        string sql = string.Format(@"
                            UPDATE dbo.REC_CAJA_DET
                            SET SYNC_DOC_ESTADO = 'APLICADO'
                            WHERE ID_RECIBO = @id AND ID_EMPRESA = @emp
                              AND NO_DOCUMENTO IN ({0})", string.Join(",", nombres));

                        using (var cmd = new SqlCommand(sql, cn, tx))
                        {
                            cmd.Parameters.AddWithValue("@id", idRecibo);
                            cmd.Parameters.AddWithValue("@emp", empresa);
                            for (int i = 0; i < facturasActivas.Count; i++)
                                cmd.Parameters.AddWithValue("@f" + i, facturasActivas[i]);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    tx.Commit();
                }
            }
        }

        /// <summary>
        /// Versión por LOTE de UpsertSapDocs: UNA sola conexión para todo el
        /// ciclo en vez de una por recibo (~845 aperturas por ciclo antes).
        ///
        /// El SqlCommand se crea una vez y solo se re-asignan los VALORES de los
        /// parámetros: el plan queda cacheado y no se re-parsea en cada vuelta.
        /// En TS sería preparar el statement fuera del for en vez de adentro.
        ///
        /// El try/catch por pago es deliberado: un ORCT con datos raros no debe
        /// tumbar la bitácora de los otros 844. Los errores se DEVUELVEN para
        /// que el BLL los sume a su lista y terminen en el log — mismo criterio
        /// que el catch por recibo que ya existía en RevisarAnulaciones.
        /// </summary>
        public List<string> UpsertSapDocsLote(string empresa,
            Dictionary<string, List<SapPagoDetalle>> pagosPorRecibo)
        {
            var errores = new List<string>();
            if (pagosPorRecibo == null || pagosPorRecibo.Count == 0) return errores;

            const string sql = @"
                UPDATE dbo.REC_CAJA_SAP_DOCS
                SET SAP_DOCNUM = @docnum, MONTO = @monto, MONEDA = @mon,
                    CANCELED = @canc, FACTURAS = @fact,
                    FECHA_ULT_CHECK = SYSDATETIME()
                WHERE ID_EMPRESA = @emp AND SAP_DOCENTRY = @docentry;

                IF @@ROWCOUNT = 0
                INSERT INTO dbo.REC_CAJA_SAP_DOCS
                    (ID_RECIBO, ID_EMPRESA, SAP_DOCENTRY, SAP_DOCNUM, MONTO,
                     MONEDA, CANCELED, FACTURAS, FECHA_ULT_CHECK)
                VALUES
                    (@id, @emp, @docentry, @docnum, @monto,
                     @mon, @canc, @fact, SYSDATETIME());";

            using (var cn = new SqlConnection(CsRecibosF5()))
            using (var cmd = new SqlCommand(sql, cn))
            {
                // AddWithValue infiere el tipo del valor de C#. Acá lo declaramos
                // explícito para no depender de esa inferencia en un bucle.
                // @monto va SIN Precision/Scale a propósito: ADO.NET los toma del
                // decimal que se le asigna. Si REC_CAJA_SAP_DOCS.MONTO resultara
                // tener una escala mayor a la de los valores, ajustar acá.
                cmd.Parameters.Add("@id", System.Data.SqlDbType.NVarChar, 15);
                cmd.Parameters.Add("@emp", System.Data.SqlDbType.NVarChar, 15);
                cmd.Parameters.Add("@docentry", System.Data.SqlDbType.Int);
                cmd.Parameters.Add("@docnum", System.Data.SqlDbType.Int);
                cmd.Parameters.Add("@monto", System.Data.SqlDbType.Decimal);
                cmd.Parameters.Add("@mon", System.Data.SqlDbType.NVarChar, 3);
                cmd.Parameters.Add("@canc", System.Data.SqlDbType.NVarChar, 1);
                cmd.Parameters.Add("@fact", System.Data.SqlDbType.NVarChar, -1);


                cn.Open();

                foreach (var par in pagosPorRecibo)
                {
                    if (par.Value == null || par.Value.Count == 0) continue;

                    foreach (var p in par.Value)
                    {
                        try
                        {
                            bool esUSD = "USD".Equals(p.MonedaDoc,
                                             StringComparison.OrdinalIgnoreCase);
                            string facturas = string.Join(",",
                                p.FacturasAplicadas ?? new List<string>());

                            cmd.Parameters["@id"].Value = par.Key;
                            cmd.Parameters["@emp"].Value = empresa;
                            cmd.Parameters["@docentry"].Value = p.DocEntry;
                            cmd.Parameters["@docnum"].Value = p.DocNum;
                            // La bitácora registra el DINERO DEL PAGO (ORCT.DocTotal),
                            // no lo aplicado en RCT2. Así SUM(MONTO) de los activos
                            // cuadra contra MONTO_T_REC y la tabla es auditable.
                            cmd.Parameters["@monto"].Value = p.MontoRecibido(esUSD);
                            // cmd.Parameters["@monto"].Value = p.MontoEfectivo(esUSD);
                            cmd.Parameters["@mon"].Value = esUSD ? "USD" : "GTQ";
                            cmd.Parameters["@canc"].Value = p.Canceled ? "Y" : "N";
                            cmd.Parameters["@fact"].Value =
                                string.IsNullOrEmpty(facturas) ? (object)DBNull.Value : facturas;

                            cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            errores.Add(string.Format("[{0}] SapDocs {1}/DocEntry {2}: {3}",
                                empresa, par.Key, p.DocEntry, ex.Message));
                        }
                    }
                }
            }
            return errores;
        }

        /// <summary>
        /// Bitácora 1 recibo ↔ N pagos SAP: registra/actualiza cada ORCT visto
        /// (activo o anulado) en REC_CAJA_SAP_DOCS. UPDATE + INSERT si no existía
        /// (patrón upsert clásico; la UNIQUE (ID_EMPRESA, SAP_DOCENTRY) lo protege).
        /// </summary>
        public void UpsertSapDocs(string idRecibo, string empresa, List<SapPagoDetalle> pagos)
        {
            if (pagos == null || pagos.Count == 0) return;

            using (var cn = new SqlConnection(CsRecibosF5()))
            {
                cn.Open();
                foreach (var p in pagos)
                {
                    bool esUSD = "USD".Equals(p.MonedaDoc, StringComparison.OrdinalIgnoreCase);
                    string facturas = string.Join(",", p.FacturasAplicadas ?? new List<string>());

                    using (var cmd = new SqlCommand(@"
                        UPDATE dbo.REC_CAJA_SAP_DOCS
                        SET SAP_DOCNUM = @docnum, MONTO = @monto, MONEDA = @mon,
                            CANCELED = @canc, FACTURAS = @fact,
                            FECHA_ULT_CHECK = SYSDATETIME()
                        WHERE ID_EMPRESA = @emp AND SAP_DOCENTRY = @docentry;

                        IF @@ROWCOUNT = 0
                        INSERT INTO dbo.REC_CAJA_SAP_DOCS
                            (ID_RECIBO, ID_EMPRESA, SAP_DOCENTRY, SAP_DOCNUM, MONTO,
                             MONEDA, CANCELED, FACTURAS, FECHA_ULT_CHECK)
                        VALUES
                            (@id, @emp, @docentry, @docnum, @monto,
                             @mon, @canc, @fact, SYSDATETIME());", cn))
                    {
                        cmd.Parameters.AddWithValue("@id", idRecibo);
                        cmd.Parameters.AddWithValue("@emp", empresa);
                        cmd.Parameters.AddWithValue("@docentry", p.DocEntry);
                        cmd.Parameters.AddWithValue("@docnum", p.DocNum);
                        cmd.Parameters.AddWithValue("@monto", p.MontoEfectivo(esUSD));
                        cmd.Parameters.AddWithValue("@mon", esUSD ? "USD" : "GTQ");
                        cmd.Parameters.AddWithValue("@canc", p.Canceled ? "Y" : "N");
                        cmd.Parameters.AddWithValue("@fact",
                            string.IsNullOrEmpty(facturas) ? (object)DBNull.Value : facturas);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}