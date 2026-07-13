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
        /// Trae, para un lote de recibos, la MONEDA y el MONTO_T_DOC necesarios
        /// para conciliar contra RCT2. Un solo viaje a SQL.
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
                    "SELECT ID_RECIBO, MONEDA, MONTO_T_DOC " +
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
                                                                          : Convert.ToDecimal(rd["MONTO_T_DOC"])
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
        /// </summary>
        public List<ReciboRevisionSql> ObtenerRecibosParaRevision(string empresa)
        {
            var lista = new List<ReciboRevisionSql>();
            using (var cn = new SqlConnection(CsRecibosF5()))
            {
                cn.Open();
                using (var cmd = new SqlCommand(@"
                    SELECT ID_RECIBO,
                           ISNULL(SAP_DOCENTRY, 0) AS SAP_DOCENTRY,
                           ISNULL(SAP_DOCNUM, 0)  AS SAP_DOCNUM,
                           SYNC_ESTADO
                    FROM dbo.REC_CAJA_ENC
                    WHERE ID_EMPRESA = @emp
                      AND SYNC_ESTADO IN ('OPERADO','DESCUADRE')
                      AND STATUS = 'A'", cn))
                {
                    cmd.Parameters.AddWithValue("@emp", empresa);
                    using (var rd = cmd.ExecuteReader())
                        while (rd.Read())
                            lista.Add(new ReciboRevisionSql
                            {
                                IdRecibo = Convert.ToString(rd["ID_RECIBO"]),
                                SapDocEntry = Convert.ToInt32(rd["SAP_DOCENTRY"]),
                                SapDocNum = Convert.ToInt32(rd["SAP_DOCNUM"]),
                                SyncEstado = Convert.ToString(rd["SYNC_ESTADO"])
                            });
                }
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