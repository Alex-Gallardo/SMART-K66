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
                            bool esEfectivo = c.TipoCobro?.ToUpper() == "EFECTIVO";
                            var cmd = new SqlCommand(sqlCobro, con, tx);
                            cmd.Parameters.AddWithValue("@id", enc.IdRecibo);
                            cmd.Parameters.AddWithValue("@emp", enc.IdEmpresa);
                            cmd.Parameters.AddWithValue("@tipo", c.TipoCobro ?? "");
                            cmd.Parameters.AddWithValue("@banco", esEfectivo ? (object)DBNull.Value : (c.Banco ?? ""));
                            cmd.Parameters.AddWithValue("@fecha", esEfectivo || !c.FechaDoc.HasValue
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
                            bool esAntOSaldo = d.TipoDoc == "ANTICIPO" || d.TipoDoc == "SALDO PENDIENTE";
                            var cmd = new SqlCommand(sqlDet, con, tx);
                            cmd.Parameters.AddWithValue("@id", enc.IdRecibo);
                            cmd.Parameters.AddWithValue("@emp", enc.IdEmpresa);
                            cmd.Parameters.AddWithValue("@tipo", d.TipoDoc ?? "");
                            cmd.Parameters.AddWithValue("@nodoc", esAntOSaldo ? (object)DBNull.Value : (d.NoDocumento ?? ""));
                            cmd.Parameters.AddWithValue("@fecha", esAntOSaldo || !d.FechaDoc.HasValue
                                                                       ? (object)DBNull.Value
                                                                       : d.FechaDoc.Value.ToString("yyyy-MM-dd"));
                            cmd.Parameters.AddWithValue("@status", esAntOSaldo ? (object)DBNull.Value : (d.Status ?? ""));
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
                            SyncObservacion = r["SYNC_OBSERVACION"] != DBNull.Value ? r["SYNC_OBSERVACION"].ToString() : null   // ★
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
                                Moneda = r["MONEDA"].ToString()
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