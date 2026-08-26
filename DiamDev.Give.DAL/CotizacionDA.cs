using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using DiamDev.Give.Entities;

namespace DiamDev.Give.DAL
{
    /// <summary>
    /// Persistencia SQL de Cotizaciones. Usa una conexión aislada que puede
    /// heredar servidor y credenciales de otra cadena mediante Alias.
    /// </summary>
    public class CotizacionDA : IDisposable
    {
        private const string ConnectionName = "CotizacionesContext";
        private const string AliasKey = "Alias";
        private readonly string _conn;

        public CotizacionDA()
        {
            _conn = ResolverCadenaConexion();
        }

        private static string ResolverCadenaConexion()
        {
            var perfil = ConfigurationManager.ConnectionStrings[ConnectionName];
            if (perfil == null || string.IsNullOrWhiteSpace(perfil.ConnectionString))
                throw new ConfigurationErrorsException(
                    "No existe una cadena de conexión válida llamada '" + ConnectionName + "'.");

            try
            {
                var propiedades = new DbConnectionStringBuilder
                {
                    ConnectionString = perfil.ConnectionString.Trim()
                };

                object nombreAlias;
                if (!propiedades.TryGetValue(AliasKey, out nombreAlias))
                    return new SqlConnectionStringBuilder(perfil.ConnectionString).ConnectionString;

                string alias = Convert.ToString(nombreAlias);
                alias = string.IsNullOrWhiteSpace(alias) ? "" : alias.Trim();
                if (alias.Length == 0 || string.Equals(alias, ConnectionName,
                    StringComparison.OrdinalIgnoreCase))
                    throw new ConfigurationErrorsException(
                        "El alias de CotizacionesContext no es válido.");

                var origen = ConfigurationManager.ConnectionStrings[alias];
                if (origen == null || string.IsNullOrWhiteSpace(origen.ConnectionString))
                    throw new ConfigurationErrorsException(
                        "No existe la cadena base indicada por CotizacionesContext.");

                var resultado = new SqlConnectionStringBuilder(origen.ConnectionString);
                propiedades.Remove(AliasKey);
                foreach (string clave in propiedades.Keys)
                    resultado[clave] = propiedades[clave];
                return resultado.ConnectionString;
            }
            catch (ArgumentException ex)
            {
                throw new ConfigurationErrorsException(
                    "La configuración de CotizacionesContext no es una cadena SQL válida.", ex);
            }
        }

        public bool ExisteSerie(string empresa)
        {
            const string sql = @"
                SELECT COUNT(*) FROM dbo.COT_SERIES
                WHERE EMPRESA = @empresa AND ACTIVO = 1;";

            using (var cn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@empresa", SqlDbType.NVarChar, 15).Value = empresa ?? "";
                cn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) == 1;
            }
        }

        /// <summary>
        /// Guarda encabezado y líneas en una sola transacción. El correlativo se
        /// consume bajo UPDLOCK/HOLDLOCK para que dos usuarios no obtengan el mismo.
        /// </summary>
        public void GuardarCompleta(CotizacionEncabezado enc)
        {
            if (enc == null) throw new ArgumentNullException("enc");
            if (enc.Detalles == null || enc.Detalles.Count == 0)
                throw new InvalidOperationException("La cotización no tiene productos.");

            const string sqlEnc = @"
                DECLARE @id NVARCHAR(30);

                UPDATE dbo.COT_SERIES WITH (UPDLOCK, HOLDLOCK)
                   SET NUMERACION = NUMERACION + 1,
                       MODIFICADO = SYSDATETIME()
                 WHERE EMPRESA = @empresa AND ACTIVO = 1;

                IF @@ROWCOUNT <> 1
                    THROW 53001, 'No hay una serie activa de cotizaciones para esta empresa.', 1;

                SELECT @id = SERIE + RIGHT(REPLICATE('0', 8) +
                             CONVERT(NVARCHAR(20), NUMERACION), 8)
                  FROM dbo.COT_SERIES WITH (UPDLOCK, HOLDLOCK)
                 WHERE EMPRESA = @empresa AND ACTIVO = 1;

                INSERT INTO dbo.COT_ENC
                    (ID_COTIZACION, ID_EMPRESA, FECHA, VALIDA_HASTA,
                     ID_CLIENTE, NOMBRE_CLIENTE, NIT, DIRECCION, CORREO,
                     CODIGO_OPERADOR, AGENTE, MONEDA, CONDICIONES_PAGO,
                     TIEMPO_ENTREGA, OBSERVACIONES, IMPORTE_BRUTO,
                     DESCUENTO_TOTAL, SUBTOTAL, IMPUESTO_TOTAL, TOTAL,
                     ESTADO, ID_USR, REGISTRO)
                VALUES
                    (@id, @empresa, @fecha, @validaHasta,
                     @idCliente, @nombreCliente, @nit, @direccion, @correo,
                     @codigoOperador, @agente, @moneda, @condicionesPago,
                     @tiempoEntrega, @observaciones, @importeBruto,
                     @descuentoTotal, @subtotal, @impuestoTotal, @total,
                     'VIGENTE', @idUsr, SYSDATETIME());

                SELECT @id;";

            const string sqlDet = @"
                INSERT INTO dbo.COT_DET
                    (ID_COTIZACION, ID_EMPRESA, LINEA, ITEM_CODE, ITEM_NAME,
                     DESCRIPCION, GRUPO, UNIDAD, LISTA_PRECIO, EXISTENCIA,
                     DISPONIBLE, CANTIDAD, PRECIO_LISTA, PRECIO_UNITARIO, DESCUENTO_PORCENTAJE,
                     GRUPO_IMPUESTO, IMPUESTO_PORCENTAJE, IMPORTE_BRUTO, DESCUENTO_MONTO,
                     SUBTOTAL, IMPUESTO_MONTO, TOTAL)
                VALUES
                    (@id, @empresa, @linea, @itemCode, @itemName,
                     @descripcion, @grupo, @unidad, @listaPrecio, @existencia,
                     @disponible, @cantidad, @precioLista, @precioUnitario, @descuentoPorcentaje,
                     @grupoImpuesto, @impuestoPorcentaje, @importeBruto, @descuentoMonto,
                     @subtotal, @impuestoMonto, @total);";

            using (var cn = new SqlConnection(_conn))
            {
                cn.Open();
                using (var tx = cn.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    try
                    {
                        using (var cmd = new SqlCommand(sqlEnc, cn, tx))
                        {
                            cmd.Parameters.Add("@empresa", SqlDbType.NVarChar, 15).Value = enc.IdEmpresa;
                            cmd.Parameters.Add("@fecha", SqlDbType.Date).Value = enc.Fecha.Date;
                            cmd.Parameters.Add("@validaHasta", SqlDbType.Date).Value = enc.ValidaHasta.Date;
                            cmd.Parameters.Add("@idCliente", SqlDbType.NVarChar, 20).Value = enc.IdCliente;
                            cmd.Parameters.Add("@nombreCliente", SqlDbType.NVarChar, 200).Value = enc.NombreCliente;
                            cmd.Parameters.Add("@nit", SqlDbType.NVarChar, 50).Value = Nulo(enc.Nit);
                            cmd.Parameters.Add("@direccion", SqlDbType.NVarChar, 300).Value = Nulo(enc.Direccion);
                            cmd.Parameters.Add("@correo", SqlDbType.NVarChar, 150).Value = Nulo(enc.Correo);
                            cmd.Parameters.Add("@codigoOperador", SqlDbType.NVarChar, 128).Value = enc.CodigoOperador;
                            cmd.Parameters.Add("@agente", SqlDbType.NVarChar, 155).Value = enc.Agente;
                            cmd.Parameters.Add("@moneda", SqlDbType.NVarChar, 5).Value = enc.Moneda;
                            cmd.Parameters.Add("@condicionesPago", SqlDbType.NVarChar, 250).Value = Nulo(enc.CondicionesPago);
                            cmd.Parameters.Add("@tiempoEntrega", SqlDbType.NVarChar, 250).Value = Nulo(enc.TiempoEntrega);
                            cmd.Parameters.Add("@observaciones", SqlDbType.NVarChar, 1500).Value = Nulo(enc.Observaciones);
                            cmd.Parameters.Add("@idUsr", SqlDbType.NVarChar, 100).Value = enc.IdUsr;
                            Decimal(cmd, "@importeBruto", enc.ImporteBruto, 20, 2);
                            Decimal(cmd, "@descuentoTotal", enc.DescuentoTotal, 20, 2);
                            Decimal(cmd, "@subtotal", enc.Subtotal, 20, 2);
                            Decimal(cmd, "@impuestoTotal", enc.ImpuestoTotal, 20, 2);
                            Decimal(cmd, "@total", enc.Total, 20, 2);
                            enc.IdCotizacion = Convert.ToString(cmd.ExecuteScalar());
                        }

                        if (string.IsNullOrWhiteSpace(enc.IdCotizacion))
                            throw new InvalidOperationException(
                                "No se pudo generar el número de cotización.");

                        foreach (var d in enc.Detalles)
                        {
                            using (var cmd = new SqlCommand(sqlDet, cn, tx))
                            {
                                cmd.Parameters.Add("@id", SqlDbType.NVarChar, 30).Value = enc.IdCotizacion;
                                cmd.Parameters.Add("@empresa", SqlDbType.NVarChar, 15).Value = enc.IdEmpresa;
                                cmd.Parameters.Add("@linea", SqlDbType.Int).Value = d.Linea;
                                cmd.Parameters.Add("@itemCode", SqlDbType.NVarChar, 50).Value = d.ItemCode;
                                cmd.Parameters.Add("@itemName", SqlDbType.NVarChar, 200).Value = d.ItemName;
                                cmd.Parameters.Add("@descripcion", SqlDbType.NVarChar, 500).Value = d.Descripcion;
                                cmd.Parameters.Add("@grupo", SqlDbType.NVarChar, 100).Value = Nulo(d.Grupo);
                                cmd.Parameters.Add("@unidad", SqlDbType.NVarChar, 100).Value = Nulo(d.Unidad);
                                cmd.Parameters.Add("@listaPrecio", SqlDbType.Int).Value = d.ListaPrecio;
                                Decimal(cmd, "@existencia", d.Existencia, 19, 6);
                                Decimal(cmd, "@disponible", d.Disponible, 19, 6);
                                Decimal(cmd, "@cantidad", d.Cantidad, 19, 6);
                                Decimal(cmd, "@precioLista", d.PrecioLista, 20, 6);
                                Decimal(cmd, "@precioUnitario", d.PrecioUnitario, 20, 6);
                                Decimal(cmd, "@descuentoPorcentaje", d.DescuentoPorcentaje, 9, 4);
                                cmd.Parameters.Add("@grupoImpuesto", SqlDbType.NVarChar, 8).Value = Nulo(d.GrupoImpuesto);
                                Decimal(cmd, "@impuestoPorcentaje", d.ImpuestoPorcentaje, 9, 4);
                                Decimal(cmd, "@importeBruto", d.ImporteBruto, 20, 2);
                                Decimal(cmd, "@descuentoMonto", d.DescuentoMonto, 20, 2);
                                Decimal(cmd, "@subtotal", d.Subtotal, 20, 2);
                                Decimal(cmd, "@impuestoMonto", d.ImpuestoMonto, 20, 2);
                                Decimal(cmd, "@total", d.Total, 20, 2);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        tx.Commit();
                    }
                    catch
                    {
                        try { tx.Rollback(); } catch { }
                        throw;
                    }
                }
            }
        }

        private const string SelectEnc = @"
            SELECT ID_COTIZACION, ID_EMPRESA, FECHA, VALIDA_HASTA,
                   ID_CLIENTE, NOMBRE_CLIENTE, NIT, DIRECCION, CORREO,
                   CODIGO_OPERADOR, AGENTE, MONEDA, CONDICIONES_PAGO,
                   TIEMPO_ENTREGA, OBSERVACIONES, IMPORTE_BRUTO,
                   DESCUENTO_TOTAL, SUBTOTAL, IMPUESTO_TOTAL, TOTAL,
                   CASE WHEN ESTADO = 'VIGENTE' AND VALIDA_HASTA < CONVERT(date, GETDATE())
                        THEN 'VENCIDA' ELSE ESTADO END AS ESTADO,
                   ID_USR, REGISTRO, ANULADO_POR, FECHA_ANULACION,
                   MOTIVO_ANULACION
            FROM dbo.COT_ENC ";

        public List<CotizacionEncabezado> Listar(
            string empresa, string estado, string idUsr, string agente,
            DateTime? desde, DateTime? hasta, string filtro)
        {
            string sql = @"
                SELECT TOP (500) ID_COTIZACION, ID_EMPRESA, FECHA, VALIDA_HASTA,
                       ID_CLIENTE, NOMBRE_CLIENTE, NIT, DIRECCION, CORREO,
                       CODIGO_OPERADOR, AGENTE, MONEDA, CONDICIONES_PAGO,
                       TIEMPO_ENTREGA, OBSERVACIONES, IMPORTE_BRUTO,
                       DESCUENTO_TOTAL, SUBTOTAL, IMPUESTO_TOTAL, TOTAL,
                       CASE WHEN ESTADO = 'VIGENTE' AND VALIDA_HASTA < CONVERT(date, GETDATE())
                            THEN 'VENCIDA' ELSE ESTADO END AS ESTADO,
                       ID_USR, REGISTRO, ANULADO_POR, FECHA_ANULACION,
                       MOTIVO_ANULACION
                FROM dbo.COT_ENC
                WHERE (@empresa IS NULL OR ID_EMPRESA = @empresa)
                  AND (@idUsr IS NULL OR ID_USR = @idUsr)
                  AND (@agente IS NULL OR AGENTE = @agente)
                  AND (@desde IS NULL OR FECHA >= @desde)
                  AND (@hasta IS NULL OR FECHA <= @hasta)
                  AND (@estado IS NULL OR
                       (@estado = 'VENCIDA' AND ESTADO = 'VIGENTE' AND VALIDA_HASTA < CONVERT(date, GETDATE())) OR
                       (@estado = 'VIGENTE' AND ESTADO = 'VIGENTE' AND VALIDA_HASTA >= CONVERT(date, GETDATE())) OR
                       (@estado = 'ANULADA' AND ESTADO = 'ANULADA'))
                  AND (@filtro IS NULL OR ID_COTIZACION LIKE @filtro
                       OR ID_CLIENTE LIKE @filtro OR NOMBRE_CLIENTE LIKE @filtro
                       OR AGENTE LIKE @filtro)
                ORDER BY FECHA DESC, REGISTRO DESC;";

            var lista = new List<CotizacionEncabezado>();
            using (var cn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@empresa", SqlDbType.NVarChar, 15).Value = Nulo(empresa);
                cmd.Parameters.Add("@estado", SqlDbType.VarChar, 20).Value = Nulo(estado);
                cmd.Parameters.Add("@idUsr", SqlDbType.NVarChar, 100).Value = Nulo(idUsr);
                cmd.Parameters.Add("@agente", SqlDbType.NVarChar, 155).Value = Nulo(agente);
                cmd.Parameters.Add("@desde", SqlDbType.Date).Value = desde.HasValue ? (object)desde.Value.Date : DBNull.Value;
                cmd.Parameters.Add("@hasta", SqlDbType.Date).Value = hasta.HasValue ? (object)hasta.Value.Date : DBNull.Value;
                cmd.Parameters.Add("@filtro", SqlDbType.NVarChar, 250).Value = string.IsNullOrWhiteSpace(filtro)
                    ? (object)DBNull.Value : "%" + filtro.Trim() + "%";
                cn.Open();
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) lista.Add(LeerEncabezado(r));
            }
            return lista;
        }

        public CotizacionEncabezado ObtenerPorId(string empresa, string idCotizacion)
        {
            CotizacionEncabezado enc;
            using (var cn = new SqlConnection(_conn))
            {
                cn.Open();
                using (var cmd = new SqlCommand(
                    SelectEnc + " WHERE ID_EMPRESA = @empresa AND ID_COTIZACION = @id;", cn))
                {
                    cmd.Parameters.Add("@empresa", SqlDbType.NVarChar, 15).Value = empresa ?? "";
                    cmd.Parameters.Add("@id", SqlDbType.NVarChar, 30).Value = idCotizacion ?? "";
                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read()) return null;
                        enc = LeerEncabezado(r);
                    }
                }

                const string sqlDet = @"
                    SELECT ROWID, LINEA, ITEM_CODE, ITEM_NAME, DESCRIPCION,
                           GRUPO, UNIDAD, LISTA_PRECIO, EXISTENCIA, DISPONIBLE,
                           CANTIDAD, PRECIO_LISTA, PRECIO_UNITARIO, DESCUENTO_PORCENTAJE, GRUPO_IMPUESTO,
                           IMPUESTO_PORCENTAJE, IMPORTE_BRUTO, DESCUENTO_MONTO,
                           SUBTOTAL, IMPUESTO_MONTO, TOTAL
                    FROM dbo.COT_DET
                    WHERE ID_EMPRESA = @empresa AND ID_COTIZACION = @id
                    ORDER BY LINEA;";

                using (var cmd = new SqlCommand(sqlDet, cn))
                {
                    cmd.Parameters.Add("@empresa", SqlDbType.NVarChar, 15).Value = empresa ?? "";
                    cmd.Parameters.Add("@id", SqlDbType.NVarChar, 30).Value = idCotizacion ?? "";
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read()) enc.Detalles.Add(new CotizacionDetalle
                        {
                            RowId = Convert.ToInt64(r["ROWID"]),
                            Linea = Convert.ToInt32(r["LINEA"]),
                            ItemCode = Txt(r["ITEM_CODE"]),
                            ItemName = Txt(r["ITEM_NAME"]),
                            Descripcion = Txt(r["DESCRIPCION"]),
                            Grupo = Txt(r["GRUPO"]),
                            Unidad = Txt(r["UNIDAD"]),
                            ListaPrecio = Convert.ToInt32(r["LISTA_PRECIO"]),
                            Existencia = Val(r["EXISTENCIA"]),
                            Disponible = Val(r["DISPONIBLE"]),
                            Cantidad = Val(r["CANTIDAD"]),
                            PrecioLista = Val(r["PRECIO_LISTA"]),
                            PrecioUnitario = Val(r["PRECIO_UNITARIO"]),
                            DescuentoPorcentaje = Val(r["DESCUENTO_PORCENTAJE"]),
                            GrupoImpuesto = Txt(r["GRUPO_IMPUESTO"]),
                            ImpuestoPorcentaje = Val(r["IMPUESTO_PORCENTAJE"]),
                            ImporteBruto = Val(r["IMPORTE_BRUTO"]),
                            DescuentoMonto = Val(r["DESCUENTO_MONTO"]),
                            Subtotal = Val(r["SUBTOTAL"]),
                            ImpuestoMonto = Val(r["IMPUESTO_MONTO"]),
                            Total = Val(r["TOTAL"])
                        });
                    }
                }
            }
            return enc;
        }

        public int Anular(string empresa, string idCotizacion, string usuario, string motivo)
        {
            const string sql = @"
                UPDATE dbo.COT_ENC
                   SET ESTADO = 'ANULADA', ANULADO_POR = @usuario,
                       FECHA_ANULACION = SYSDATETIME(), MOTIVO_ANULACION = @motivo
                 WHERE ID_EMPRESA = @empresa AND ID_COTIZACION = @id
                   AND ESTADO = 'VIGENTE';";
            using (var cn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@usuario", SqlDbType.NVarChar, 100).Value = usuario ?? "";
                cmd.Parameters.Add("@motivo", SqlDbType.NVarChar, 1000).Value = motivo ?? "";
                cmd.Parameters.Add("@empresa", SqlDbType.NVarChar, 15).Value = empresa ?? "";
                cmd.Parameters.Add("@id", SqlDbType.NVarChar, 30).Value = idCotizacion ?? "";
                cn.Open();
                return cmd.ExecuteNonQuery();
            }
        }

        private static CotizacionEncabezado LeerEncabezado(IDataRecord r)
        {
            return new CotizacionEncabezado
            {
                IdCotizacion = Txt(r["ID_COTIZACION"]),
                IdEmpresa = Txt(r["ID_EMPRESA"]),
                Fecha = Convert.ToDateTime(r["FECHA"]),
                ValidaHasta = Convert.ToDateTime(r["VALIDA_HASTA"]),
                IdCliente = Txt(r["ID_CLIENTE"]),
                NombreCliente = Txt(r["NOMBRE_CLIENTE"]),
                Nit = Txt(r["NIT"]),
                Direccion = Txt(r["DIRECCION"]),
                Correo = Txt(r["CORREO"]),
                CodigoOperador = Txt(r["CODIGO_OPERADOR"]),
                Agente = Txt(r["AGENTE"]),
                Moneda = Txt(r["MONEDA"]),
                CondicionesPago = Txt(r["CONDICIONES_PAGO"]),
                TiempoEntrega = Txt(r["TIEMPO_ENTREGA"]),
                Observaciones = Txt(r["OBSERVACIONES"]),
                ImporteBruto = Val(r["IMPORTE_BRUTO"]),
                DescuentoTotal = Val(r["DESCUENTO_TOTAL"]),
                Subtotal = Val(r["SUBTOTAL"]),
                ImpuestoTotal = Val(r["IMPUESTO_TOTAL"]),
                Total = Val(r["TOTAL"]),
                Estado = Txt(r["ESTADO"]),
                IdUsr = Txt(r["ID_USR"]),
                Registro = Fecha(r["REGISTRO"]),
                AnuladoPor = Txt(r["ANULADO_POR"]),
                FechaAnulacion = Fecha(r["FECHA_ANULACION"]),
                MotivoAnulacion = Txt(r["MOTIVO_ANULACION"])
            };
        }

        private static SqlParameter Decimal(
            SqlCommand cmd, string nombre, decimal valor, byte precision, byte scale)
        {
            var p = cmd.Parameters.Add(nombre, SqlDbType.Decimal);
            p.Precision = precision;
            p.Scale = scale;
            p.Value = valor;
            return p;
        }

        private static decimal Val(object valor)
        {
            return valor == null || valor == DBNull.Value ? 0m : Convert.ToDecimal(valor);
        }

        private static string Txt(object valor)
        {
            return valor == null || valor == DBNull.Value ? "" : Convert.ToString(valor);
        }

        private static DateTime? Fecha(object valor)
        {
            return valor == null || valor == DBNull.Value
                ? (DateTime?)null : Convert.ToDateTime(valor);
        }

        private static object Nulo(string valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? (object)DBNull.Value : valor.Trim();
        }

        public void Dispose() { }
    }
}
