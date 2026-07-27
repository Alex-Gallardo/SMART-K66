using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using DiamDev.Give.Entities;

namespace DiamDev.Give.DAL
{
    public class APK66Context : IDisposable
    {
        private readonly string _conn;

        public APK66Context()
        {
            // CUTOVER: el módulo de recibos ahora vive en POS-SmartK66 (GiveContext),
            // NO en APK66. Solo cambiamos a qué connection string apunta;
            // toda la lógica ADO.NET (correlativo transaccional) queda idéntica.

            // Módulo de recibos: connection string dedicado.
            // Pruebas = POS-SmartK66; producción = POS-SmartK66_DEV.
            _conn = ConfigurationManager
                        .ConnectionStrings["RecibosContext"]
                        .ConnectionString;
        }

        // ─────────────────────────────────────────────
        // USUARIOS
        // ─────────────────────────────────────────────

        // ─────────────────────────────────────────────
        // USUARIOS (APK66)
        // ─────────────────────────────────────────────

        /// <summary>
        /// [LEGACY] Obtiene la PLANTA desde RT_USUARIOS por ID_USR exacto.
        /// Se mantiene por compatibilidad, pero el flujo de recibos ahora usa
        /// ObtenerPlantaPorLogin (contra REC_CAJA_USUARIOS).
        /// </summary>
        public string ObtenerPlantaUsuario(string idUsuario)
        {
            using (var con = new SqlConnection(_conn))
            {
                con.Open();
                var cmd = new SqlCommand(
                    "SELECT ISNULL(PLANTA,'') FROM RT_USUARIOS WHERE ID_USR = @id", con);
                cmd.Parameters.AddWithValue("@id", idUsuario);
                return cmd.ExecuteScalar()?.ToString() ?? string.Empty;
            }
        }

        /// <summary>
        /// Obtiene la PLANTA del usuario de caja desde REC_CAJA_USUARIOS.
        ///
        /// El vínculo entre los dos mundos es:
        ///   POS.Usuario.Login (minúsculas)  ==  APK66.REC_CAJA_USUARIOS.ID_USR (mayúsculas)
        /// Por eso comparamos con UPPER() en ambos lados — así "ecalderon" encuentra "ECALDERON".
        ///
        /// Devuelve string.Empty si el login NO tiene usuario de caja vinculado
        /// (ej: 'admin', 'prueba'). El BLL/Controller traduce eso en un error claro
        /// ANTES de tocar la transacción del correlativo.
        /// </summary>
        public string ObtenerPlantaPorLogin(string login)
        {
            using (var con = new SqlConnection(_conn))
            {
                con.Open();
                var cmd = new SqlCommand(
                    @"SELECT ISNULL(PLANTA,'')
              FROM REC_CAJA_USUARIOS
              WHERE UPPER(ID_USR) = UPPER(@login)
                AND ISNULL(ESTADO,'') <> 'INACTIVO'", con);
                cmd.Parameters.AddWithValue("@login", login ?? "");
                return cmd.ExecuteScalar()?.ToString() ?? string.Empty;
            }
        }

        /// <summary>
        /// Devuelve el ID_USR "oficial" de APK66 (en mayúsculas, como está guardado)
        /// a partir del login POS. Útil para grabar REC_CAJA_ENC.USUARIO con el código
        /// canónico de APK66 en vez del login en minúsculas.
        /// Devuelve string.Empty si no hay vínculo.
        /// </summary>
        public string ObtenerIdUsrPorLogin(string login)
        {
            using (var con = new SqlConnection(_conn))
            {
                con.Open();
                var cmd = new SqlCommand(
                    @"SELECT ISNULL(ID_USR,'')
              FROM REC_CAJA_USUARIOS
              WHERE UPPER(ID_USR) = UPPER(@login)
                AND ISNULL(ESTADO,'') <> 'INACTIVO'", con);
                cmd.Parameters.AddWithValue("@login", login ?? "");
                return cmd.ExecuteScalar()?.ToString() ?? string.Empty;
            }
        }

        // ─────────────────────────────────────────────
        // GUARDAR RECIBO COMPLETO (transacción)
        // ─────────────────────────────────────────────

        /// <summary>
        /// ¿Existe una serie de numeración para (EMPRESA, DEPTO)?
        /// Se valida ANTES de la transacción del guardado: si falta, el subquery
        /// del correlativo devolvería NULL y el INSERT fallaría con un error críptico.
        /// </summary>
        public bool ExisteSerie(string empresa, string depto)
        {
            using (var con = new SqlConnection(_conn))
            {
                con.Open();
                var cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM REC_CAJA_SERIES WHERE EMPRESA = @emp AND DEPTO = @depto", con);
                cmd.Parameters.AddWithValue("@emp", empresa ?? "");
                cmd.Parameters.AddWithValue("@depto", depto ?? "");
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        /// <summary>
        /// Devuelve la SERIE (prefijo, ej. "RG12-") configurada para
        /// (EMPRESA, DEPTO) en REC_CAJA_SERIES, o string.Empty si no existe.
        /// Solo lectura: se usa para mostrarla en la UI (card del operador).
        /// </summary>
        public string ObtenerSerieDeDepto(string empresa, string depto)
        {
            using (var con = new SqlConnection(_conn))
            {
                con.Open();
                var cmd = new SqlCommand(
                    "SELECT ISNULL(SERIE,'') FROM REC_CAJA_SERIES WHERE EMPRESA = @emp AND DEPTO = @depto", con);
                cmd.Parameters.AddWithValue("@emp", empresa ?? "");
                cmd.Parameters.AddWithValue("@depto", depto ?? "");
                return cmd.ExecuteScalar()?.ToString() ?? string.Empty;
            }
        }

        /// <summary>
        /// Resuelve el DEPTO (cobrador) de un recibo a partir del prefijo de su ID.
        ///
        /// REC_CAJA_ENC no guarda DEPTO, pero el ID lo lleva embebido:
        ///   "RG12-08542" → serie "RG12-" → DEPTO "RODOLFO"
        ///
        /// ORDER BY LEN(SERIE) DESC gana el match más específico: si existieran
        /// "RG1-" y "RG12-", el ID "RG12-00001" debe resolver a la segunda.
        ///
        /// Devuelve "" si no hay serie que coincida, y NUNCA lanza: se usa para
        /// enriquecer eventos de bitácora, y un fallo acá jamás debe tumbar la
        /// operación que lo generó (misma regla que RegistrarEventoAnalytics).
        /// </summary>
        public string ObtenerDeptoDeRecibo(string idRecibo, string empresa)
        {
            if (string.IsNullOrWhiteSpace(idRecibo)) return string.Empty;

            try
            {
                using (var con = new SqlConnection(_conn))
                {
                    con.Open();
                    var cmd = new SqlCommand(@"
                        SELECT TOP 1 ISNULL(DEPTO,'')
                        FROM REC_CAJA_SERIES
                        WHERE EMPRESA = @emp
                          AND ISNULL(SERIE,'') <> ''
                          AND @id LIKE SERIE + '%'
                        ORDER BY LEN(SERIE) DESC", con);
                    cmd.Parameters.AddWithValue("@id", idRecibo.Trim());
                    cmd.Parameters.AddWithValue("@emp", empresa ?? "");
                    return cmd.ExecuteScalar()?.ToString() ?? string.Empty;
                }
            }
            catch { return string.Empty; }
        }

        /// <summary>
        /// Inserta encabezado + cobros + documentos en una sola transacción.
        /// Si cualquier paso falla, hace ROLLBACK automático.
        /// Después del guardado, enc.IdRecibo queda poblado con el ID generado.
        /// </summary>
        public void GuardarReciboCompleto(ReciboCajaEncabezado enc, string depto)
        {
            using (var con = new SqlConnection(_conn))
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    try
                    {
                        // ── 1. INSERT Encabezado + UPDATE series ──────────────
                        const string sqlEnc = @"
                            INSERT INTO REC_CAJA_ENC
                                (ID_RECIBO, ID_EMPRESA, ID_CLIENTE, NOMBRE_CLIENTE,
                                 DIRECCION, NIT, AGENTE, CORREO, MONEDA, STATUS,
                                 MONTO_T_REC, MONTO_T_DOC, SALDO, USUARIO,
                                 FECHA_RECIBO, FECHA_REGISTRO, REC_FISICO,
                                 CODIGO_USUARIO_EMPRESA,                                     -- ★ CÓDIGO
                                 MONEDA_BASE, TIPO_CAMBIO,                                   -- ★
                                 MONTO_T_REC_GTQ, MONTO_T_REC_USD,                           -- ★
                                 MONTO_T_DOC_GTQ, MONTO_T_DOC_USD, SALDO_GTQ, SALDO_USD)     -- ★
                            VALUES (
                                (SELECT (SERIE + RIGHT('0000' + CONVERT(NVARCHAR, (NUMERACION + 1)), 5))
                                 FROM REC_CAJA_SERIES
                                 WHERE EMPRESA = @empresa AND DEPTO = @depto),
                                @empresa, @idCliente, @nombreCliente,
                                @direccion, @nit, @agente, @correo, @moneda, 'A',
                                @montoRec, @montoDoc, @saldo, @usuario,
                                @fechaRec, SYSDATETIME(), @recFisico,
                                @codigoUE,                                                   -- ★ CÓDIGO
                                @monedaBase, @tipoCambio,                                    -- ★
                                @mtRecGtq, @mtRecUsd, @mtDocGtq, @mtDocUsd, @saldoGtq, @saldoUsd); -- ★
                            UPDATE REC_CAJA_SERIES
                               SET NUMERACION = NUMERACION + 1
                             WHERE EMPRESA = @empresa AND DEPTO = @depto;";

                        var cmdEnc = new SqlCommand(sqlEnc, con, tx);
                        cmdEnc.Parameters.AddWithValue("@empresa", enc.IdEmpresa);
                        cmdEnc.Parameters.AddWithValue("@depto", depto);
                        cmdEnc.Parameters.AddWithValue("@idCliente", enc.IdCliente);
                        cmdEnc.Parameters.AddWithValue("@nombreCliente", enc.NombreCliente);
                        cmdEnc.Parameters.AddWithValue("@direccion", enc.Direccion ?? "");
                        cmdEnc.Parameters.AddWithValue("@nit", enc.Nit ?? "");
                        cmdEnc.Parameters.AddWithValue("@agente", enc.Agente ?? "");
                        cmdEnc.Parameters.AddWithValue("@correo", enc.Correo ?? "");
                        cmdEnc.Parameters.AddWithValue("@moneda", enc.Moneda ?? "");
                        cmdEnc.Parameters.AddWithValue("@montoRec", enc.MontoTotalRecibo);
                        cmdEnc.Parameters.AddWithValue("@montoDoc", enc.MontoTotalDoc);
                        cmdEnc.Parameters.AddWithValue("@saldo", enc.Saldo);
                        cmdEnc.Parameters.AddWithValue("@usuario", enc.Usuario);
                        cmdEnc.Parameters.AddWithValue("@fechaRec", enc.FechaRecibo.ToString("yyyy-MM-dd"));
                        cmdEnc.Parameters.AddWithValue("@recFisico", enc.RecFisico ?? "");
                        // ★ CÓDIGO: código de Usuario_Empresa con el que se emitió.
                        // NULL (no cadena vacía) si viniera vacío — así los históricos
                        // y los nuevos "sin código" se ven igual en los reportes.
                        cmdEnc.Parameters.AddWithValue("@codigoUE",
                            string.IsNullOrWhiteSpace(enc.CodigoUsuario)
                                ? (object)DBNull.Value
                                : enc.CodigoUsuario.Trim());
                        // ★ nuevos parámetros duales del encabezado
                        cmdEnc.Parameters.AddWithValue("@monedaBase", enc.MonedaBase ?? "GTQ");
                        cmdEnc.Parameters.AddWithValue("@tipoCambio", (object)enc.TipoCambio ?? DBNull.Value);
                        cmdEnc.Parameters.AddWithValue("@mtRecGtq", enc.MontoTotalRecGtq);
                        cmdEnc.Parameters.AddWithValue("@mtRecUsd", enc.MontoTotalRecUsd);
                        cmdEnc.Parameters.AddWithValue("@mtDocGtq", enc.MontoTotalDocGtq);
                        cmdEnc.Parameters.AddWithValue("@mtDocUsd", enc.MontoTotalDocUsd);
                        cmdEnc.Parameters.AddWithValue("@saldoGtq", enc.SaldoGtq);
                        cmdEnc.Parameters.AddWithValue("@saldoUsd", enc.SaldoUsd);
                        cmdEnc.ExecuteNonQuery();

                        // ── 2. Recuperar el ID recién generado ────────────────
                        var cmdId = new SqlCommand(
                            @"SELECT TOP 1 ID_RECIBO FROM REC_CAJA_ENC
                              WHERE USUARIO = @usr AND ID_EMPRESA = @emp
                              ORDER BY FECHA_REGISTRO DESC",
                            con, tx);
                        cmdId.Parameters.AddWithValue("@usr", enc.Usuario);
                        cmdId.Parameters.AddWithValue("@emp", enc.IdEmpresa);
                        enc.IdRecibo = cmdId.ExecuteScalar()?.ToString() ?? "";

                        // ── 3. INSERT Cobros ──────────────────────────────────
                        const string sqlCobro = @"
                            INSERT INTO REC_CAJA_COBRO
                                (ID_RECIBO, ID_EMPRESA, TIPO_COBRO,
                                 BANCO, FECHA_DOC, NO_DOCUMENTO, MONTO, MONEDA,
                                 TIPO_CAMBIO, MONTO_GTQ, MONTO_USD)                  
                            VALUES (@id, @emp, @tipo, @banco, @fecha, @nodoc, @monto, @moneda,
                                    @tipoCambio, @montoGtq, @montoUsd)";

                        foreach (var c in enc.Cobros)
                        {
                            // BANCO y NO_DOCUMENTO siguen siendo NULL en EFECTIVO:
                            // no hay banco ni número de cheque que registrar.
                            bool esEfectivo = c.TipoCobro?.ToUpper() == "EFECTIVO";

                            var cmd = new SqlCommand(sqlCobro, con, tx);
                            cmd.Parameters.AddWithValue("@id", enc.IdRecibo);
                            cmd.Parameters.AddWithValue("@emp", enc.IdEmpresa);
                            cmd.Parameters.AddWithValue("@tipo", c.TipoCobro ?? "");
                            cmd.Parameters.AddWithValue("@banco", esEfectivo ? (object)DBNull.Value : (c.Banco ?? ""));

                            // ★ CAMBIO: la FECHA_DOC ya NO se anula en EFECTIVO.
                            // Cambió su significado de negocio: antes era "fecha del
                            // documento bancario" (cheque/transferencia); ahora es
                            // "fecha en que se recibió el dinero", y eso existe para
                            // TODA forma de pago. El BLL la valida como obligatoria.
                            // El DBNull queda solo como red de seguridad para datos
                            // que lleguen sin fecha por otra vía (no debería pasar).
                            cmd.Parameters.AddWithValue("@fecha", !c.FechaDoc.HasValue
                                                                       ? (object)DBNull.Value
                                                                       : c.FechaDoc.Value.ToString("yyyy-MM-dd"));

                            cmd.Parameters.AddWithValue("@nodoc", esEfectivo ? (object)DBNull.Value : (c.NoDocumento ?? ""));
                            cmd.Parameters.AddWithValue("@monto", c.Monto);
                            cmd.Parameters.AddWithValue("@moneda", c.Moneda ?? "");
                            cmd.Parameters.AddWithValue("@tipoCambio", (object)c.TipoCambio ?? DBNull.Value);  // ★
                            cmd.Parameters.AddWithValue("@montoGtq", c.MontoGtq);                              // ★
                            cmd.Parameters.AddWithValue("@montoUsd", c.MontoUsd);                              // ★
                            cmd.ExecuteNonQuery();
                        }

                        // ── 4. INSERT Documentos ──────────────────────────────
                        const string sqlDet = @"
                            INSERT INTO REC_CAJA_DET
                                (ID_RECIBO, ID_EMPRESA, TIPO_DOC, NO_DOCUMENTO,
                                 FECHA_DOC, STATUS, MONTO, MONEDA,
                                 MONTO_FACT, PAGADO, FEL_SERIE, FEL_NUMERO,
                                 TIPO_CAMBIO, MONTO_GTQ, MONTO_USD)                  
                            VALUES (@id, @emp, @tipo, @nodoc,
                                    @fecha, @status, @monto, @moneda,
                                    @mfact, @pagado, @serie, @nfel,
                                    @tipoCambio, @montoGtq, @montoUsd)";

                        foreach (var d in enc.Documentos)
                        {
                            // Regla de NULLs centralizada en Entities: ANTICIPO,
                            // DIFERENCIA y SALDO PENDIENTE no referencian un
                            // documento de SAP, así que NO_DOCUMENTO / FECHA_DOC /
                            // STATUS van NULL (réplica del UpdateNull() del desktop).
                            bool esSinDoc = TiposDocumentoRecibo.EsSinDocumento(d.TipoDoc);

                            var cmd = new SqlCommand(sqlDet, con, tx);
                            cmd.Parameters.AddWithValue("@id", enc.IdRecibo);
                            cmd.Parameters.AddWithValue("@emp", enc.IdEmpresa);
                            cmd.Parameters.AddWithValue("@tipo", d.TipoDoc ?? "");
                            cmd.Parameters.AddWithValue("@nodoc", esSinDoc ? (object)DBNull.Value : (d.NoDocumento ?? ""));
                            cmd.Parameters.AddWithValue("@fecha", esSinDoc || !d.FechaDoc.HasValue
                                                                       ? (object)DBNull.Value
                                                                       : d.FechaDoc.Value.ToString("yyyy-MM-dd"));
                            cmd.Parameters.AddWithValue("@status", esSinDoc ? (object)DBNull.Value : (d.Status ?? ""));
                            cmd.Parameters.AddWithValue("@monto", d.Monto);
                            cmd.Parameters.AddWithValue("@moneda", d.Moneda ?? "");
                            cmd.Parameters.AddWithValue("@mfact", d.MontoFact);
                            cmd.Parameters.AddWithValue("@pagado", d.Pagado);
                            cmd.Parameters.AddWithValue("@serie", d.FelSerie ?? "");
                            cmd.Parameters.AddWithValue("@nfel", d.FelNumero ?? "");
                            cmd.Parameters.AddWithValue("@tipoCambio", (object)d.TipoCambio ?? DBNull.Value);  // ★
                            cmd.Parameters.AddWithValue("@montoGtq", d.MontoGtq);                              // ★
                            cmd.Parameters.AddWithValue("@montoUsd", d.MontoUsd);                              // ★
                            cmd.ExecuteNonQuery();
                        }

                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        // ─────────────────────────────────────────────
        // BUSCAR RECIBO EXISTENTE
        // ─────────────────────────────────────────────
        public ReciboCajaEncabezado BuscarRecibo(string idRecibo, string empresa)
        {
            ReciboCajaEncabezado rec = null;
            using (var con = new SqlConnection(_conn))
            {
                con.Open();

                // Encabezado
                using (var cmd = new SqlCommand(
                    "SELECT * FROM REC_CAJA_ENC WHERE ID_RECIBO=@id AND ID_EMPRESA=@emp", con))
                {
                    cmd.Parameters.AddWithValue("@id", idRecibo);
                    cmd.Parameters.AddWithValue("@emp", empresa);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read()) return null;
                        rec = new ReciboCajaEncabezado
                        {
                            IdRecibo = r["ID_RECIBO"].ToString(),
                            IdEmpresa = r["ID_EMPRESA"].ToString(),
                            IdCliente = r["ID_CLIENTE"].ToString(),
                            NombreCliente = r["NOMBRE_CLIENTE"].ToString(),
                            Direccion = r["DIRECCION"].ToString(),
                            Nit = r["NIT"].ToString(),
                            Agente = r["AGENTE"].ToString(),
                            Correo = r["CORREO"].ToString(),
                            Moneda = r["MONEDA"].ToString(),
                            Status = r["STATUS"].ToString(),
                            MontoTotalRecibo = Val(r["MONTO_T_REC"]),
                            MontoTotalDoc = Val(r["MONTO_T_DOC"]),
                            Saldo = Val(r["SALDO"]),
                            Usuario = r["USUARIO"].ToString(),
                            RecFisico = r["REC_FISICO"].ToString(),
                            // ★ CÓDIGO: con qué código de Usuario_Empresa se emitió (null en históricos)
                            CodigoUsuario = r["CODIGO_USUARIO_EMPRESA"] != DBNull.Value
                                                ? r["CODIGO_USUARIO_EMPRESA"].ToString()
                                                : null,
                            FechaRecibo = r["FECHA_RECIBO"] != DBNull.Value
                                                   ? Convert.ToDateTime(r["FECHA_RECIBO"])
                                                   : DateTime.Today,

                            // ★ Moneda dual (necesario para Imprimir.cshtml)
                            MonedaBase = r["MONEDA_BASE"] != DBNull.Value ? r["MONEDA_BASE"].ToString() : "GTQ",
                            TipoCambio = r["TIPO_CAMBIO"] != DBNull.Value ? (decimal?)Convert.ToDecimal(r["TIPO_CAMBIO"]) : null,
                            MontoTotalRecGtq = Val(r["MONTO_T_REC_GTQ"]),
                            MontoTotalRecUsd = Val(r["MONTO_T_REC_USD"]),
                            MontoTotalDocGtq = Val(r["MONTO_T_DOC_GTQ"]),
                            MontoTotalDocUsd = Val(r["MONTO_T_DOC_USD"]),
                            SaldoGtq = Val(r["SALDO_GTQ"]),
                            SaldoUsd = Val(r["SALDO_USD"]),

                            // ── Fase 4: estado de sincronización ──
                            SyncEstado = r["SYNC_ESTADO"] != DBNull.Value ? r["SYNC_ESTADO"].ToString() : null,
                            SapDocEntry = r["SAP_DOCENTRY"] != DBNull.Value ? (int?)Convert.ToInt32(r["SAP_DOCENTRY"]) : null,
                            SapDocNum = r["SAP_DOCNUM"] != DBNull.Value ? (int?)Convert.ToInt32(r["SAP_DOCNUM"]) : null,
                            SyncObservacion = r["SYNC_OBSERVACION"] != DBNull.Value ? r["SYNC_OBSERVACION"].ToString() : null,   // ★
                            // ── Auditoría de anulación ──
                                                                                                                                 // ── Auditoría de anulación ──
                            AnuladoPor = r["ANULADO_POR"] != DBNull.Value ? r["ANULADO_POR"].ToString() : null,
                            MotivoAnulacion = r["MOTIVO"] != DBNull.Value ? r["MOTIVO"].ToString() : null,
                            FechaAnulacion = r["FECHA_ANULACION"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(r["FECHA_ANULACION"]) : null
                            };
                    }
                }

                // Cobros
                using (var cmd = new SqlCommand(
                    "SELECT * FROM REC_CAJA_COBRO WHERE ID_RECIBO=@id AND ID_EMPRESA=@emp", con))
                {
                    cmd.Parameters.AddWithValue("@id", idRecibo);
                    cmd.Parameters.AddWithValue("@emp", empresa);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                            rec.Cobros.Add(new ReciboCajaCobro
                            {
                                TipoCobro = r["TIPO_COBRO"].ToString(),
                                Banco = r["BANCO"].ToString(),
                                FechaDoc = r["FECHA_DOC"] != DBNull.Value
                                                  ? Convert.ToDateTime(r["FECHA_DOC"])
                                                  : (DateTime?)null,
                                NoDocumento = r["NO_DOCUMENTO"].ToString(),
                                Monto = Val(r["MONTO"]),
                                Moneda = r["MONEDA"].ToString(),
                                // ★ NUEVO: el TC con el que se convirtió ESTE cobro.
                                // Necesario para reimprimir/reconsultar sin recalcular.
                                TipoCambio = r["TIPO_CAMBIO"] != DBNull.Value
                                                  ? (decimal?)Convert.ToDecimal(r["TIPO_CAMBIO"])
                                                  : null,
                                MontoGtq = Val(r["MONTO_GTQ"]),
                                MontoUsd = Val(r["MONTO_USD"])
                            });
                    }
                }

                // Documentos
                using (var cmd = new SqlCommand(
                    "SELECT * FROM REC_CAJA_DET WHERE ID_RECIBO=@id AND ID_EMPRESA=@emp", con))
                {
                    cmd.Parameters.AddWithValue("@id", idRecibo);
                    cmd.Parameters.AddWithValue("@emp", empresa);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                            rec.Documentos.Add(new ReciboCajaDetalle
                            {
                                TipoDoc = r["TIPO_DOC"].ToString(),
                                NoDocumento = r["NO_DOCUMENTO"].ToString(),
                                FechaDoc = r["FECHA_DOC"] != DBNull.Value
                                                  ? Convert.ToDateTime(r["FECHA_DOC"])
                                                  : (DateTime?)null,
                                Status = r["STATUS"].ToString(),
                                Monto = Val(r["MONTO"]),
                                Moneda = r["MONEDA"].ToString(),
                                MontoFact = Val(r["MONTO_FACT"]),
                                Pagado = Val(r["PAGADO"]),
                                FelSerie = r["FEL_SERIE"].ToString(),
                                FelNumero = r["FEL_NUMERO"].ToString()
                            });
                    }
                }
            }
            return rec;
        }

        // ─────────────────────────────────────────────
        // ANULAR RECIBO: STATUS='X' + auditoría.
        //   MOTIVO          (legacy, nvarchar(150)) ← el porqué
        //   ANULADO_POR     (nueva)                 ← el quién
        //   FECHA_ANULACION (nueva)                 ← el cuándo
        // El AND STATUS='A' es el candado anti-carrera.
        // Devuelve filas afectadas (0 = no existía o ya anulado).
        // ─────────────────────────────────────────────
        public int AnularRecibo(string idRecibo, string empresa, string usuario, string motivo)
        {
            using (var con = new SqlConnection(_conn))
            {
                con.Open();
                using (var cmd = new SqlCommand(
                    @"UPDATE REC_CAJA_ENC
                 SET STATUS          = 'X',
                     ANULADO_POR     = @usuario,
                     MOTIVO          = @motivo,
                     FECHA_ANULACION = SYSDATETIME()
               WHERE ID_RECIBO  = @id
                 AND ID_EMPRESA = @emp
                 AND STATUS     = 'A'", con))
                {
                    cmd.Parameters.AddWithValue("@id", idRecibo);
                    cmd.Parameters.AddWithValue("@emp", empresa);
                    cmd.Parameters.AddWithValue("@usuario", usuario ?? "");
                    cmd.Parameters.AddWithValue("@motivo", motivo ?? "");
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        // ─────────────────────────────────────────────
        // DOCUMENTOS DISPONIBLES (MA_RECC_DOCTOS)
        // ─────────────────────────────────────────────
        public List<DocumentoRecibo> ObtenerDocumentos(string empresa, string clienteId, string tipoDoc)
        {
            var lista = new List<DocumentoRecibo>();
            using (var con = new SqlConnection(_conn))
            {
                con.Open();
                var cmd = new SqlCommand(@"
                    SELECT
                        DOCTO,
                        INVOICE_DATE,
                        INVOICE_STATUS,
                        CURRENCY_ID,
                        MONTO_FACT,
                        PAGADO
                    FROM MA_RECC_DOCTOS
                    WHERE ENTITY_ID   = @emp
                        AND CUSTOMER_ID = @cli
                        AND TIPO        = @tipo
                    ORDER BY INVOICE_DATE DESC", con);

                cmd.Parameters.AddWithValue("@emp", empresa);
                cmd.Parameters.AddWithValue("@cli", clienteId);
                cmd.Parameters.AddWithValue("@tipo", tipoDoc);

                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        lista.Add(new DocumentoRecibo
                        {
                            NoDocumento = r["DOCTO"].ToString(),
                            FechaDoc = r["INVOICE_DATE"] != DBNull.Value
                                              ? Convert.ToDateTime(r["INVOICE_DATE"])
                                              : DateTime.Today,
                            MontoFact = Val(r["MONTO_FACT"]),
                            Pagado = Val(r["PAGADO"]),
                            Moneda = r["CURRENCY_ID"].ToString(),
                            // FEL no existe en MA_RECC_DOCTOS — el usuario los ingresa manualmente
                            FelSerie = "",
                            FelNumero = ""
                        });
                    }
                }
            }
            return lista;
        }

        // ─────────────────────────────────────────────
        // PENDIENTES POR DOCUMENTO (para el modal de docs)
        // ─────────────────────────────────────────────
        /// <summary>
        /// Para un cliente+empresa+tipo, devuelve cuánto dinero está comprometido
        /// por documento en recibos "en tránsito", y en QUÉ recibos:
        ///
        ///   a) Recibos PENDIENTES: nada de ellos está en SAP todavía, así que
        ///      TODAS sus líneas cuentan (incluye los regresados a PENDIENTE por
        ///      anulación TOTAL en SAP).
        ///
        ///   b) Recibos en DESCUADRE: SOLO las líneas SYNC_DOC_ESTADO='ANULADO_SAP'.
        ///      El pago de esas líneas se revirtió en SAP (la factura reabrió y su
        ///      PaidToDate ya NO incluye ese dinero), pero caja SÍ lo recibió.
        ///      Las líneas 'APLICADO' NO se cuentan: su pago sigue vivo en SAP y
        ///      PaidToDate ya lo refleja — contarlas sería descontarlas DOS veces.
        ///
        /// Excluye OPERADOS cuadrados y recibos anulados localmente (STATUS='X').
        ///
        /// MONEDA DUAL: además del MONTO original (legacy), acumula MONTO_GTQ y
        /// MONTO_USD por documento. El BLL elige cuál restar según la MONEDA del
        /// documento — antes se restaba el MONTO en su moneda original contra el
        /// saldo de la factura en OTRA moneda (mismo bug que la vista de HANA).
        ///
        /// Retorna: diccionario NO_DOCUMENTO → { Monto, MontoGtq, MontoUsd, Recibos[] }.
        /// </summary>
        public Dictionary<string, PendienteDocumento> ObtenerPendientesPorDocumento(
            string empresa, string clienteId, string tipoDoc)
        {
            var mapa = new Dictionary<string, PendienteDocumento>(StringComparer.OrdinalIgnoreCase);

            const string sql = @"
        SELECT D.NO_DOCUMENTO, D.MONTO, D.MONEDA,
               ISNULL(D.MONTO_GTQ, 0) AS MONTO_GTQ,
               ISNULL(D.MONTO_USD, 0) AS MONTO_USD,
               E.ID_RECIBO, E.SYNC_ESTADO
        FROM REC_CAJA_DET D
        INNER JOIN REC_CAJA_ENC E
                ON E.ID_RECIBO  = D.ID_RECIBO
               AND E.ID_EMPRESA = D.ID_EMPRESA
        WHERE D.ID_EMPRESA   = @emp
          AND E.ID_CLIENTE   = @cli
          AND D.TIPO_DOC     = @tipo
          AND D.NO_DOCUMENTO IS NOT NULL
          AND ISNULL(E.STATUS, 'A') <> 'X'
          AND (
                ISNULL(E.SYNC_ESTADO, 'PENDIENTE') = 'PENDIENTE'
             OR (E.SYNC_ESTADO = 'DESCUADRE' AND D.SYNC_DOC_ESTADO = 'ANULADO_SAP')
          )
        ORDER BY D.NO_DOCUMENTO, E.ID_RECIBO;";

            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@emp", empresa ?? "");
                cmd.Parameters.AddWithValue("@cli", clienteId ?? "");
                cmd.Parameters.AddWithValue("@tipo", tipoDoc ?? "");
                con.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        string doc = r["NO_DOCUMENTO"].ToString().Trim();
                        if (doc.Length == 0) continue;

                        if (!mapa.TryGetValue(doc, out var p))
                            mapa[doc] = p = new PendienteDocumento();

                        decimal monto = Val(r["MONTO"]);
                        decimal gtq = Val(r["MONTO_GTQ"]);
                        decimal usd = Val(r["MONTO_USD"]);

                        // Fallback líneas históricas sin duales (pre-migración)
                        if (gtq == 0m && usd == 0m && monto != 0m)
                        {
                            if ("USD".Equals((r["MONEDA"] ?? "").ToString().Trim(),
                                             StringComparison.OrdinalIgnoreCase))
                                usd = monto;
                            else
                                gtq = monto;
                        }

                        p.Monto += monto;      // legacy: se conserva por compatibilidad
                        p.MontoGtq += gtq;
                        p.MontoUsd += usd;

                        string etiqueta = string.Format("{0} ({1})",
                            r["ID_RECIBO"],
                            r["SYNC_ESTADO"] == DBNull.Value ? "?" : r["SYNC_ESTADO"]);
                        if (!p.Recibos.Contains(etiqueta))
                            p.Recibos.Add(etiqueta);
                    }
                }
            }
            return mapa;
        }

        // ─────────────────────────────────────────────
        // ANTICIPOS EN TRÁNSITO (barra informativa del modal de docs)
        // ─────────────────────────────────────────────
        /// <summary>
        /// Suma los ANTICIPOS "en tránsito" de un cliente: dinero que caja ya recibió
        /// pero que aún no está operado (o quedó revertido) en SAP. Mismo criterio que
        /// ObtenerPendientesPorDocumento:
        ///   a) Recibos PENDIENTES (SYNC_ESTADO NULL cuenta como PENDIENTE: recibo
        ///      recién creado que el sincronizador aún no toca).
        ///   b) Recibos en DESCUADRE: solo líneas SYNC_DOC_ESTADO='ANULADO_SAP'.
        /// Excluye anulados localmente (STATUS='X').
        /// Devuelve totales DUALES (GTQ y USD) + los IDs de recibo involucrados.
        /// </summary>
        public AnticipoTransito ObtenerAnticiposTransito(string empresa, string clienteId)
        {
            var resultado = new AnticipoTransito();
            var recibos = new List<string>();

            const string sql = @"
        SELECT E.ID_RECIBO,
               D.MONTO,
               ISNULL(D.MONTO_GTQ, 0) AS MONTO_GTQ,
               ISNULL(D.MONTO_USD, 0) AS MONTO_USD,
               D.MONEDA
        FROM REC_CAJA_DET D
        INNER JOIN REC_CAJA_ENC E
                ON E.ID_RECIBO  = D.ID_RECIBO
               AND E.ID_EMPRESA = D.ID_EMPRESA
        WHERE E.ID_EMPRESA = @emp
          AND E.ID_CLIENTE = @cli
          AND D.TIPO_DOC   = 'ANTICIPO'
          AND ISNULL(E.STATUS, 'A') <> 'X'
          AND (
                ISNULL(E.SYNC_ESTADO, 'PENDIENTE') = 'PENDIENTE'
             OR (E.SYNC_ESTADO = 'DESCUADRE' AND D.SYNC_DOC_ESTADO = 'ANULADO_SAP')
          )
        ORDER BY E.ID_RECIBO;";

            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@emp", empresa ?? "");
                cmd.Parameters.AddWithValue("@cli", clienteId ?? "");
                con.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        decimal gtq = Val(r["MONTO_GTQ"]);
                        decimal usd = Val(r["MONTO_USD"]);

                        // Fallback para líneas históricas SIN duales (pre-migración):
                        // el MONTO original cuenta en su propia moneda; la otra queda en 0
                        // (mejor subestimar el equivalente que inventarlo sin TC).
                        if (gtq == 0m && usd == 0m)
                        {
                            decimal monto = Val(r["MONTO"]);
                            if ("USD".Equals((r["MONEDA"] ?? "").ToString().Trim(),
                                             StringComparison.OrdinalIgnoreCase))
                                usd = monto;
                            else
                                gtq = monto;
                        }

                        resultado.Gtq += gtq;
                        resultado.Usd += usd;

                        string id = Convert.ToString(r["ID_RECIBO"]).Trim();
                        if (!recibos.Contains(id)) recibos.Add(id);
                    }
                }
            }

            resultado.Cantidad = recibos.Count;
            resultado.Recibos = string.Join(", ", recibos);
            return resultado;
        }

        // Helper interno
        private static decimal Val(object o) =>
            o != null && o != DBNull.Value ? Convert.ToDecimal(o) : 0m;

        // ─────────────────────────────────────────────
        // ANALYTICS — bitácora append-only
        // ─────────────────────────────────────────────
        /// <summary>
        /// Registra un evento del módulo (CREADO/EDITADO/IMPRESO/ANULADO/ERROR_GUARDADO).
        /// Append-only: nunca actualiza ni borra. No lanza si falla (no debe tumbar
        /// el guardado del recibo por un fallo de log).
        /// </summary>
        public void RegistrarEventoAnalytics(
            string evento, string idRecibo, string idEmpresa, string depto,
            long usuarioId, string usuarioLogin, string moneda, decimal? tipoCambio,
            decimal montoGtq, decimal montoUsd, decimal saldoGtq,
            string payloadJson, string ipUsuario)
        {
            try
            {
                using (var con = new SqlConnection(_conn))
                {
                    con.Open();
                    var cmd = new SqlCommand(@"
                        INSERT INTO analyticsRecibos
                            (Evento, IdRecibo, IdEmpresa, Depto, UsuarioId, UsuarioLogin,
                             Moneda, TipoCambio, MontoGtq, MontoUsd, SaldoGtq,
                             PayloadJson, IpUsuario, FechaEvento)
                        VALUES
                            (@evento, @idRecibo, @idEmpresa, @depto, @usuarioId, @usuarioLogin,
                             @moneda, @tipoCambio, @montoGtq, @montoUsd, @saldoGtq,
                             @payloadJson, @ipUsuario, SYSDATETIME())", con);

                    cmd.Parameters.AddWithValue("@evento", evento ?? "");
                    cmd.Parameters.AddWithValue("@idRecibo", (object)idRecibo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@idEmpresa", (object)idEmpresa ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@depto", (object)depto ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@usuarioId", usuarioId);
                    cmd.Parameters.AddWithValue("@usuarioLogin", (object)usuarioLogin ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@moneda", (object)moneda ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@tipoCambio", (object)tipoCambio ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@montoGtq", montoGtq);
                    cmd.Parameters.AddWithValue("@montoUsd", montoUsd);
                    cmd.Parameters.AddWithValue("@saldoGtq", saldoGtq);
                    cmd.Parameters.AddWithValue("@payloadJson", (object)payloadJson ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ipUsuario", (object)ipUsuario ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                // El log NO debe tumbar el guardado. Si falla, se ignora en silencio.
                // (En producción podrías escribir a un log de archivo aquí.)
            }
        }

        public void Dispose() { }
    }
}