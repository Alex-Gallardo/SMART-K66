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
            _conn = ConfigurationManager
                        .ConnectionStrings["APK66Context"]
                        .ConnectionString;
        }

        // ─────────────────────────────────────────────
        // USUARIOS
        // ─────────────────────────────────────────────

        /// <summary>
        /// Obtiene la PLANTA del usuario en APK66.
        /// La PLANTA equivale al campo DEPTO en REC_CAJA_SERIES
        /// (determina qué serie de numeración se usa).
        /// NOTA: asumimos que User.Identity.Name en Smart-K66 == ID_USR en APK66.
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
                        // El subquery genera el correlativo: SERIE + '00001', '00002', etc.
                        const string sqlEnc = @"
                            INSERT INTO REC_CAJA_ENC
                                (ID_RECIBO, ID_EMPRESA, ID_CLIENTE, NOMBRE_CLIENTE,
                                 DIRECCION, NIT, AGENTE, CORREO, MONEDA, STATUS,
                                 MONTO_T_REC, MONTO_T_DOC, SALDO, USUARIO,
                                 FECHA_RECIBO, FECHA_REGISTRO, REC_FISICO)
                            VALUES (
                                (SELECT (SERIE + RIGHT('0000' + CONVERT(NVARCHAR, (NUMERACION + 1)), 5))
                                 FROM REC_CAJA_SERIES
                                 WHERE EMPRESA = @empresa AND DEPTO = @depto),
                                @empresa, @idCliente, @nombreCliente,
                                @direccion, @nit, @agente, @correo, @moneda, 'A',
                                @montoRec, @montoDoc, @saldo, @usuario,
                                @fechaRec, SYSDATETIME(), @recFisico
                            );
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
                                 BANCO, FECHA_DOC, NO_DOCUMENTO, MONTO, MONEDA)
                            VALUES (@id, @emp, @tipo, @banco, @fecha, @nodoc, @monto, @moneda)";

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
                            cmd.ExecuteNonQuery();
                        }

                        // ── 4. INSERT Documentos ──────────────────────────────
                        const string sqlDet = @"
                            INSERT INTO REC_CAJA_DET
                                (ID_RECIBO, ID_EMPRESA, TIPO_DOC, NO_DOCUMENTO,
                                 FECHA_DOC, STATUS, MONTO, MONEDA,
                                 MONTO_FACT, PAGADO, FEL_SERIE, FEL_NUMERO)
                            VALUES (@id, @emp, @tipo, @nodoc,
                                    @fecha, @status, @monto, @moneda,
                                    @mfact, @pagado, @serie, @nfel)";

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
                            cmd.ExecuteNonQuery();
                        }

                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;  // Re-lanza para que el BLL lo capture
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
                                                   : DateTime.Today
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

        public void Dispose() { }
    }
}