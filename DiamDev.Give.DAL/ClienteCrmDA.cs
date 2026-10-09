using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using DiamDev.Give.Entities;

namespace DiamDev.Give.DAL
{
    public class ClienteCrmDA
    {
        private readonly string _conexion;

        public ClienteCrmDA()
        {
            var perfil = ConfigurationManager.ConnectionStrings["ClientesCrmContext"];
            if (perfil == null || string.IsNullOrWhiteSpace(perfil.ConnectionString))
                throw new ConfigurationErrorsException("Falta ClientesCrmContext.");
            var partes = new DbConnectionStringBuilder { ConnectionString = perfil.ConnectionString };
            object alias;
            if (!partes.TryGetValue("Alias", out alias))
            {
                _conexion = new SqlConnectionStringBuilder(perfil.ConnectionString).ConnectionString;
                return;
            }
            var origen = ConfigurationManager.ConnectionStrings[Convert.ToString(alias)];
            if (origen == null) throw new ConfigurationErrorsException("Alias SQL de ClientesCrmContext no existe.");
            var cadena = new SqlConnectionStringBuilder(origen.ConnectionString);
            partes.Remove("Alias");
            foreach (string clave in partes.Keys) cadena[clave] = partes[clave];
            _conexion = cadena.ConnectionString;
        }

        public List<ClienteCrmSolicitud> ListarSolicitudes(string empresa, string estado, string filtro, string creador)
        {
            const string sql = @"SELECT ID, CLIENTE_ID, TIPO_SOLICITUD, ORIGEN_CLIENTE_ID, ORIGEN_VERSION, ORIGEN_CODIGO_SAP, EMPRESA, CODIGO_OPERADOR, AGENTE,
                ESTADO, FICHA_JSON, CREADO_POR, CREADO_EN, ENVIADO_EN, RESUELTO_EN,
                RESUELTO_POR, MOTIVO_RECHAZO, VERSION
                FROM dbo.CRM_SOLICITUD
                WHERE (@empresa IS NULL OR EMPRESA = @empresa)
                  AND (@estado IS NULL OR ESTADO = @estado)
                  AND (@creador IS NULL OR CREADO_POR = @creador)
                  AND (@filtro IS NULL OR RAZON_SOCIAL LIKE @filtro OR NIT_CLAVE LIKE @filtro
                       OR CONVERT(nvarchar(30), ID) = @exacto)
                ORDER BY CREADO_EN DESC, ID DESC;";
            var lista = new List<ClienteCrmSolicitud>();
            using (var cn = new SqlConnection(_conexion))
            using (var cmd = new SqlCommand(sql, cn))
            {
                P(cmd, "@empresa", empresa);
                P(cmd, "@estado", estado);
                P(cmd, "@creador", creador);
                P(cmd, "@filtro", string.IsNullOrWhiteSpace(filtro) ? null : "%" + filtro.Trim() + "%");
                P(cmd, "@exacto", filtro);
                cn.Open();
                using (var r = cmd.ExecuteReader()) while (r.Read()) lista.Add(MapSolicitud(r));
            }
            return lista;
        }

        public ClienteCrmSolicitud ObtenerSolicitud(long id)
        {
            const string sql = @"SELECT ID, CLIENTE_ID, TIPO_SOLICITUD, ORIGEN_CLIENTE_ID, ORIGEN_VERSION, ORIGEN_CODIGO_SAP, EMPRESA, CODIGO_OPERADOR, AGENTE,
                ESTADO, FICHA_JSON, CREADO_POR, CREADO_EN, ENVIADO_EN, RESUELTO_EN,
                RESUELTO_POR, MOTIVO_RECHAZO, VERSION
                FROM dbo.CRM_SOLICITUD WHERE ID = @id;";
            using (var cn = new SqlConnection(_conexion))
            using (var cmd = new SqlCommand(sql, cn))
            {
                P(cmd, "@id", id);
                cn.Open();
                using (var r = cmd.ExecuteReader()) return r.Read() ? MapSolicitud(r) : null;
            }
        }

        public List<ClienteCrmCliente> ListarClientes(string empresa, string filtro,
            bool incluirInactivos, string usuario = null)
        {
            const string sql = @"SELECT C.ID, E.EMPRESA, E.CODIGO_SAP, E.FICHA_JSON,
                E.ACTIVO, E.VERSION, E.ACTUALIZADO_EN
                FROM dbo.CRM_CLIENTE C
                JOIN dbo.CRM_CLIENTE_EMPRESA E ON E.CLIENTE_ID = C.ID
                WHERE (@empresa IS NULL OR E.EMPRESA = @empresa)
                  AND (@inactivos = 1 OR E.ACTIVO = 1)
                  AND (@usuario IS NULL OR EXISTS (SELECT 1 FROM dbo.CRM_CLIENTE_USUARIO U
                      WHERE U.CLIENTE_ID=C.ID AND U.EMPRESA=E.EMPRESA AND U.USUARIO=@usuario))
                  AND (@filtro IS NULL OR C.RAZON_SOCIAL LIKE @filtro
                      OR C.NOMBRE_COMERCIAL LIKE @filtro OR C.NIT_CLAVE LIKE @filtro
                      OR E.CODIGO_SAP LIKE @filtro)
                ORDER BY E.ACTUALIZADO_EN DESC, C.ID DESC;";
            var lista = new List<ClienteCrmCliente>();
            using (var cn = new SqlConnection(_conexion))
            using (var cmd = new SqlCommand(sql, cn))
            {
                P(cmd, "@empresa", empresa);
                P(cmd, "@inactivos", incluirInactivos);
                P(cmd, "@usuario", usuario);
                P(cmd, "@filtro", string.IsNullOrWhiteSpace(filtro) ? null : "%" + filtro.Trim() + "%");
                cn.Open();
                using (var r = cmd.ExecuteReader()) while (r.Read()) lista.Add(MapCliente(r));
            }
            return lista;
        }

        public ClienteCrmCliente ObtenerCliente(long id, string empresa)
        {
            const string sql = @"SELECT C.ID, E.EMPRESA, E.CODIGO_SAP, E.FICHA_JSON,
                E.ACTIVO, E.VERSION, E.ACTUALIZADO_EN
                FROM dbo.CRM_CLIENTE C
                JOIN dbo.CRM_CLIENTE_EMPRESA E ON E.CLIENTE_ID = C.ID
                WHERE C.ID = @id AND E.EMPRESA = @empresa;";
            using (var cn = new SqlConnection(_conexion))
            using (var cmd = new SqlCommand(sql, cn))
            {
                P(cmd, "@id", id); P(cmd, "@empresa", empresa);
                cn.Open();
                using (var r = cmd.ExecuteReader()) return r.Read() ? MapCliente(r) : null;
            }
        }

        public ClienteCrmCliente ObtenerClientePorCodigoSap(string empresa, string codigo)
        {
            const string sql = @"SELECT C.ID, E.EMPRESA, E.CODIGO_SAP, E.FICHA_JSON,
                E.ACTIVO, E.VERSION, E.ACTUALIZADO_EN
                FROM dbo.CRM_CLIENTE C JOIN dbo.CRM_CLIENTE_EMPRESA E ON E.CLIENTE_ID=C.ID
                WHERE E.EMPRESA=@empresa AND E.CODIGO_SAP=@codigo AND E.ACTIVO=1;";
            using (var cn = new SqlConnection(_conexion))
            using (var cmd = new SqlCommand(sql, cn))
            {
                P(cmd, "@empresa", empresa); P(cmd, "@codigo", codigo);
                cn.Open();
                using (var r = cmd.ExecuteReader()) return r.Read() ? MapCliente(r) : null;
            }
        }

        public bool ClientePropio(long id, string empresa, string usuario)
        {
            const string sql = @"SELECT COUNT(*) FROM dbo.CRM_CLIENTE_USUARIO
                WHERE CLIENTE_ID=@id AND EMPRESA=@empresa AND USUARIO=@usuario;";
            using (var cn = new SqlConnection(_conexion))
            using (var cmd = new SqlCommand(sql, cn))
            {
                P(cmd, "@id", id); P(cmd, "@empresa", empresa); P(cmd, "@usuario", usuario);
                cn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        public long GuardarSolicitud(ClienteCrmSolicitud solicitud, IList<ClienteCrmArchivo> archivos,
            bool enviar, string usuario, string ip)
        {
            using (var cn = new SqlConnection(_conexion))
            {
                cn.Open();
                using (var tx = cn.BeginTransaction())
                {
                    string antes = null;
                    if (enviar)
                    {
                        if (solicitud.TipoSolicitud == TiposSolicitudCliente.Alta)
                        {
                            const string existenteCrm = @"SELECT COUNT(*) FROM dbo.CRM_CLIENTE C WITH (UPDLOCK,HOLDLOCK)
                                JOIN dbo.CRM_CLIENTE_EMPRESA E WITH (UPDLOCK,HOLDLOCK) ON E.CLIENTE_ID=C.ID
                                WHERE E.EMPRESA=@empresa AND C.NIT_CLAVE=@nit;";
                            using (var cmd = new SqlCommand(existenteCrm, cn, tx))
                            {
                                P(cmd, "@empresa", solicitud.Empresa);
                                P(cmd, "@nit", NormalizarNit(solicitud.Ficha.NitDpi));
                                if (Convert.ToInt32(cmd.ExecuteScalar()) > 0)
                                    throw new InvalidOperationException("Este cliente ya tiene ficha CRM. Use Actualización de Cliente.");
                            }
                        }
                        const string pendiente = @"SELECT COUNT(*) FROM dbo.CRM_SOLICITUD WITH (UPDLOCK,HOLDLOCK)
                            WHERE EMPRESA=@empresa AND ESTADO='ENVIADA' AND ID<>@id AND
                            ((@tipo='ALTA' AND TIPO_SOLICITUD='ALTA' AND NIT_CLAVE=@nit) OR
                             (@tipo='ACTUALIZACION' AND TIPO_SOLICITUD='ACTUALIZACION' AND
                              ((@origenId IS NOT NULL AND ORIGEN_CLIENTE_ID=@origenId) OR
                               (@origenId IS NULL AND ORIGEN_CLIENTE_ID IS NULL AND ORIGEN_CODIGO_SAP=@origenSap))));";
                        using (var cmd = new SqlCommand(pendiente, cn, tx))
                        {
                            P(cmd, "@empresa", solicitud.Empresa);
                            P(cmd, "@nit", NormalizarNit(solicitud.Ficha.NitDpi));
                            P(cmd, "@id", solicitud.Id);
                            P(cmd, "@tipo", solicitud.TipoSolicitud);
                            P(cmd, "@origenId", solicitud.OrigenClienteId);
                            P(cmd, "@origenSap", solicitud.OrigenCodigoSap);
                            if (Convert.ToInt32(cmd.ExecuteScalar()) > 0)
                                throw new InvalidOperationException("Ya existe una solicitud enviada para este cliente en la empresa.");
                        }
                    }
                    if (solicitud.Id == 0)
                    {
                        const string sql = @"INSERT dbo.CRM_SOLICITUD
                            (TIPO_SOLICITUD,ORIGEN_CLIENTE_ID,ORIGEN_VERSION,ORIGEN_CODIGO_SAP,
                             EMPRESA,CODIGO_OPERADOR,AGENTE,RAZON_SOCIAL,NIT_CLAVE,ESTADO,FICHA_JSON,
                             COMPRA_ACTUAL,MONEDA,VENTA_12M,POTENCIAL_ANUAL,OBJETIVO_12M,CREADO_POR,ENVIADO_EN)
                            OUTPUT INSERTED.ID VALUES
                            (@tipo,@origenId,@origenVersion,@origenSap,
                             @empresa,@operador,@agente,@razon,@nit,@estado,@ficha,
                             @compra,@moneda,@venta,@potencial,@objetivo,@usuario,
                             CASE WHEN @enviar=1 THEN SYSDATETIME() ELSE NULL END);";
                        using (var cmd = new SqlCommand(sql, cn, tx))
                        {
                            ParamsSolicitud(cmd, solicitud, enviar, usuario);
                            solicitud.Id = Convert.ToInt64(cmd.ExecuteScalar());
                        }
                        if (solicitud.TipoSolicitud == TiposSolicitudCliente.Actualizacion &&
                            solicitud.OrigenClienteId.HasValue)
                            CopiarArchivosOrigen(cn, tx, solicitud, usuario, ip);
                    }
                    else
                    {
                        const string leer = @"SELECT CREADO_POR,ESTADO,VERSION,FICHA_JSON,TIPO_SOLICITUD,
                            ORIGEN_CLIENTE_ID,ORIGEN_VERSION,ORIGEN_CODIGO_SAP
                            FROM dbo.CRM_SOLICITUD WITH (UPDLOCK,HOLDLOCK) WHERE ID=@id;";
                        using (var cmd = new SqlCommand(leer, cn, tx))
                        {
                            P(cmd, "@id", solicitud.Id);
                            using (var r = cmd.ExecuteReader())
                            {
                                if (!r.Read()) throw new InvalidOperationException("Solicitud no encontrada.");
                                if (!string.Equals(Convert.ToString(r["CREADO_POR"]), usuario,
                                    StringComparison.OrdinalIgnoreCase))
                                    throw new UnauthorizedAccessException("Solo el creador puede editar esta solicitud.");
                                string estado = Convert.ToString(r["ESTADO"]);
                                if (estado != EstadosSolicitudCliente.Borrador && estado != EstadosSolicitudCliente.Rechazada)
                                    throw new InvalidOperationException("Solo se puede editar un borrador o una solicitud rechazada.");
                                if (Convert.ToInt32(r["VERSION"]) != solicitud.Version)
                                    throw new InvalidOperationException("La solicitud cambió. Actualice la página.");
                                if (Convert.ToString(r["TIPO_SOLICITUD"]) != solicitud.TipoSolicitud ||
                                    NLong(r, "ORIGEN_CLIENTE_ID") != solicitud.OrigenClienteId ||
                                    (r["ORIGEN_VERSION"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["ORIGEN_VERSION"])) != solicitud.OrigenVersion ||
                                    !string.Equals(Convert.ToString(r["ORIGEN_CODIGO_SAP"]), solicitud.OrigenCodigoSap ?? "", StringComparison.OrdinalIgnoreCase))
                                    throw new InvalidOperationException("No se puede cambiar el cliente de origen de una solicitud.");
                                antes = Convert.ToString(r["FICHA_JSON"]);
                            }
                        }
                        const string actualizar = @"UPDATE dbo.CRM_SOLICITUD SET
                            EMPRESA=@empresa,CODIGO_OPERADOR=@operador,AGENTE=@agente,
                            RAZON_SOCIAL=@razon,NIT_CLAVE=@nit,ESTADO=@estado,FICHA_JSON=@ficha,
                            COMPRA_ACTUAL=@compra,MONEDA=@moneda,VENTA_12M=@venta,POTENCIAL_ANUAL=@potencial,
                            OBJETIVO_12M=@objetivo,
                            ENVIADO_EN=CASE WHEN @enviar=1 THEN SYSDATETIME() ELSE NULL END,
                            RESUELTO_EN=NULL,
                            RESUELTO_POR=NULL,MOTIVO_RECHAZO=NULL,VERSION=VERSION+1 WHERE ID=@id;";
                        using (var cmd = new SqlCommand(actualizar, cn, tx))
                        {
                            ParamsSolicitud(cmd, solicitud, enviar, usuario);
                            P(cmd, "@id", solicitud.Id);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    foreach (var archivo in archivos) GuardarArchivo(cn, tx, "SOLICITUD_ID", solicitud.Id,
                        solicitud.Empresa, archivo, usuario, ip);
                    if (enviar) ValidarArchivos(cn, tx, solicitud.Id, true, solicitud.Empresa);
                    Auditar(cn, tx, "SOLICITUD", solicitud.Id, solicitud.Empresa, usuario,
                        enviar ? "ENVIAR" : "GUARDAR_BORRADOR", null, antes, solicitud.FichaJson, ip);
                    string codigoSapAnterior = string.IsNullOrWhiteSpace(antes) ? null :
                        Newtonsoft.Json.JsonConvert.DeserializeObject<ClienteCrmFicha>(antes).CodigoSapOrigen;
                    if (!string.Equals(codigoSapAnterior, solicitud.Ficha.CodigoSapOrigen,
                        StringComparison.OrdinalIgnoreCase))
                        Auditar(cn, tx, "SOLICITUD", solicitud.Id, solicitud.Empresa, usuario,
                            "CAMBIAR_CLIENTE_SAP", solicitud.Ficha.CodigoSapOrigen,
                            codigoSapAnterior, solicitud.Ficha.CodigoSapOrigen, ip);
                    tx.Commit();
                    return solicitud.Id;
                }
            }
        }

        public long ResolverSolicitud(long id, int version, bool aprobar, string motivo,
            string usuario, string ip)
        {
            using (var cn = new SqlConnection(_conexion))
            {
                cn.Open();
                using (var tx = cn.BeginTransaction())
                {
                    ClienteCrmSolicitud solicitud;
                    const string leer = @"SELECT ID,CLIENTE_ID,TIPO_SOLICITUD,ORIGEN_CLIENTE_ID,ORIGEN_VERSION,ORIGEN_CODIGO_SAP,EMPRESA,CODIGO_OPERADOR,AGENTE,
                        ESTADO,FICHA_JSON,CREADO_POR,CREADO_EN,ENVIADO_EN,RESUELTO_EN,
                        RESUELTO_POR,MOTIVO_RECHAZO,VERSION FROM dbo.CRM_SOLICITUD
                        WITH (UPDLOCK,HOLDLOCK) WHERE ID=@id;";
                    using (var cmd = new SqlCommand(leer, cn, tx))
                    {
                        P(cmd, "@id", id);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (!r.Read()) throw new InvalidOperationException("Solicitud no encontrada.");
                            solicitud = MapSolicitud(r);
                        }
                    }
                    if (solicitud.Estado != EstadosSolicitudCliente.Enviada || solicitud.Version != version)
                        throw new InvalidOperationException("La solicitud ya fue modificada o resuelta. Actualice la página.");
                    long? clienteId = null;
                    string fichaAnterior = null;
                    if (aprobar)
                    {
                        var ficha = Newtonsoft.Json.JsonConvert.DeserializeObject<ClienteCrmFicha>(solicitud.FichaJson);
                        if (solicitud.TipoSolicitud == TiposSolicitudCliente.Actualizacion)
                        {
                            if (solicitud.OrigenClienteId.HasValue)
                            {
                                const string origen = @"SELECT E.VERSION,E.ACTIVO,E.FICHA_JSON,C.NIT_CLAVE,
                                    (SELECT COUNT(*) FROM dbo.CRM_CLIENTE_EMPRESA X WHERE X.CLIENTE_ID=C.ID) AS EMPRESAS
                                    FROM dbo.CRM_CLIENTE C WITH (UPDLOCK,HOLDLOCK)
                                    JOIN dbo.CRM_CLIENTE_EMPRESA E WITH (UPDLOCK,HOLDLOCK) ON E.CLIENTE_ID=C.ID
                                    WHERE C.ID=@id AND E.EMPRESA=@empresa;";
                                using (var cmd = new SqlCommand(origen, cn, tx))
                                {
                                    P(cmd, "@id", solicitud.OrigenClienteId); P(cmd, "@empresa", solicitud.Empresa);
                                    using (var r = cmd.ExecuteReader())
                                    {
                                        if (!r.Read() || !Convert.ToBoolean(r["ACTIVO"]) ||
                                            !solicitud.OrigenVersion.HasValue ||
                                            Convert.ToInt32(r["VERSION"]) != solicitud.OrigenVersion.Value)
                                            throw new InvalidOperationException("La ficha original cambió. Revise la actualización antes de aprobar.");
                                        if (Convert.ToInt32(r["EMPRESAS"]) > 1 &&
                                            !string.Equals(Convert.ToString(r["NIT_CLAVE"]), NormalizarNit(ficha.NitDpi), StringComparison.OrdinalIgnoreCase))
                                            throw new InvalidOperationException("El NIT está compartido por varias empresas y no puede cambiarse desde esta actualización.");
                                        fichaAnterior = Convert.ToString(r["FICHA_JSON"]);
                                    }
                                }
                                clienteId = UpsertCliente(cn, tx, solicitud.Empresa, ficha, solicitud.FichaJson,
                                    null, usuario, solicitud.OrigenClienteId, true, true);
                            }
                            else
                            {
                                const string duplicado = @"SELECT COUNT(*) FROM dbo.CRM_CLIENTE_EMPRESA E WITH (UPDLOCK,HOLDLOCK)
                                    JOIN dbo.CRM_CLIENTE C ON C.ID=E.CLIENTE_ID
                                    WHERE E.EMPRESA=@empresa AND (E.CODIGO_SAP=@codigo OR C.NIT_CLAVE=@nit);";
                                using (var cmd = new SqlCommand(duplicado, cn, tx))
                                {
                                    P(cmd, "@empresa", solicitud.Empresa);
                                    P(cmd, "@codigo", solicitud.OrigenCodigoSap);
                                    P(cmd, "@nit", NormalizarNit(ficha.NitDpi));
                                    if (Convert.ToInt32(cmd.ExecuteScalar()) > 0)
                                        throw new InvalidOperationException("El cliente SAP ya tiene una ficha CRM. Inicie una actualización desde esa ficha.");
                                }
                                clienteId = UpsertCliente(cn, tx, solicitud.Empresa, ficha, solicitud.FichaJson,
                                    solicitud.OrigenCodigoSap, usuario, null, true, false);
                            }
                        }
                        else
                        {
                            const string existenteCrm = @"SELECT COUNT(*) FROM dbo.CRM_CLIENTE C WITH (UPDLOCK,HOLDLOCK)
                                JOIN dbo.CRM_CLIENTE_EMPRESA E WITH (UPDLOCK,HOLDLOCK) ON E.CLIENTE_ID=C.ID
                                WHERE E.EMPRESA=@empresa AND C.NIT_CLAVE=@nit;";
                            using (var cmd = new SqlCommand(existenteCrm, cn, tx))
                            {
                                P(cmd, "@empresa", solicitud.Empresa);
                                P(cmd, "@nit", NormalizarNit(ficha.NitDpi));
                                if (Convert.ToInt32(cmd.ExecuteScalar()) > 0)
                                    throw new InvalidOperationException("Este cliente ya tiene ficha CRM. Use Actualización de Cliente.");
                            }
                            clienteId = UpsertCliente(cn, tx, solicitud.Empresa, ficha, solicitud.FichaJson,
                                null, usuario, null, true, true);
                        }
                        VincularUsuario(cn, tx, clienteId.Value, solicitud.Empresa,
                            solicitud.CreadoPor, "SOLICITUD");
                        Auditar(cn, tx, "CLIENTE", clienteId, solicitud.Empresa, usuario,
                            solicitud.TipoSolicitud == TiposSolicitudCliente.Actualizacion ? "APROBACION_ACTUALIZACION" : "APROBACION_SOLICITUD",
                            "Solicitud #" + id, fichaAnterior, solicitud.FichaJson, ip);
                    }
                    const string actualizar = @"UPDATE dbo.CRM_SOLICITUD SET
                        ESTADO=@estado,CLIENTE_ID=@cliente,RESUELTO_EN=SYSDATETIME(),
                        RESUELTO_POR=@usuario,MOTIVO_RECHAZO=@motivo,VERSION=VERSION+1 WHERE ID=@id;";
                    using (var cmd = new SqlCommand(actualizar, cn, tx))
                    {
                        P(cmd, "@estado", aprobar ? EstadosSolicitudCliente.Aprobada : EstadosSolicitudCliente.Rechazada);
                        P(cmd, "@cliente", clienteId); P(cmd, "@usuario", usuario);
                        P(cmd, "@motivo", aprobar ? null : motivo); P(cmd, "@id", id);
                        cmd.ExecuteNonQuery();
                    }
                    Auditar(cn, tx, "SOLICITUD", id, solicitud.Empresa, usuario,
                        aprobar ? "APROBAR" : "RECHAZAR", motivo, solicitud.FichaJson,
                        aprobar ? Convert.ToString(clienteId) : motivo, ip);
                    tx.Commit();
                    return clienteId ?? 0;
                }
            }
        }

        public long GuardarCliente(ClienteCrmCliente cliente, IList<ClienteCrmArchivo> archivos,
            string usuario, string ip)
        {
            using (var cn = new SqlConnection(_conexion))
            {
                cn.Open();
                using (var tx = cn.BeginTransaction())
                {
                    string antes = null;
                    bool nuevaFicha = cliente.Id == 0;
                    if (nuevaFicha)
                    {
                        const string duplicado = @"SELECT COUNT(*) FROM dbo.CRM_CLIENTE C
                            JOIN dbo.CRM_CLIENTE_EMPRESA E ON E.CLIENTE_ID=C.ID
                            WHERE C.NIT_CLAVE=@nit AND E.EMPRESA=@empresa;";
                        using (var cmd = new SqlCommand(duplicado, cn, tx))
                        {
                            P(cmd, "@nit", NormalizarNit(cliente.Ficha.NitDpi));
                            P(cmd, "@empresa", cliente.Empresa);
                            if (Convert.ToInt32(cmd.ExecuteScalar()) > 0)
                                throw new InvalidOperationException("Ya existe una ficha de este NIT para la empresa. Edítela desde la cartera.");
                        }
                    }
                    if (cliente.Id != 0)
                    {
                        const string identidad = @"SELECT C.NIT_CLAVE,
                            (SELECT COUNT(*) FROM dbo.CRM_CLIENTE_EMPRESA E WHERE E.CLIENTE_ID=C.ID)
                            AS EMPRESAS FROM dbo.CRM_CLIENTE C WITH (UPDLOCK,HOLDLOCK) WHERE C.ID=@id;";
                        using (var cmd = new SqlCommand(identidad, cn, tx))
                        {
                            P(cmd, "@id", cliente.Id);
                            using (var r = cmd.ExecuteReader())
                            {
                                if (!r.Read()) throw new InvalidOperationException("Cliente no encontrado.");
                                if (Convert.ToInt32(r["EMPRESAS"]) > 1 &&
                                    !string.Equals(Convert.ToString(r["NIT_CLAVE"]),
                                        NormalizarNit(cliente.Ficha.NitDpi), StringComparison.OrdinalIgnoreCase))
                                    throw new InvalidOperationException("El NIT está compartido por varias empresas. Revise las fichas vinculadas antes de cambiarlo.");
                            }
                        }
                        const string leer = @"SELECT FICHA_JSON,VERSION,CODIGO_SAP,ACTIVO FROM dbo.CRM_CLIENTE_EMPRESA
                            WITH (UPDLOCK,HOLDLOCK) WHERE CLIENTE_ID=@id AND EMPRESA=@empresa;";
                        using (var cmd = new SqlCommand(leer, cn, tx))
                        {
                            P(cmd, "@id", cliente.Id); P(cmd, "@empresa", cliente.Empresa);
                            using (var r = cmd.ExecuteReader())
                            {
                                if (!r.Read()) throw new InvalidOperationException("Ficha no encontrada.");
                                if (Convert.ToInt32(r["VERSION"]) != cliente.Version)
                                    throw new InvalidOperationException("La ficha cambió. Actualice la página.");
                                antes = Newtonsoft.Json.JsonConvert.SerializeObject(new {
                                    Ficha = Convert.ToString(r["FICHA_JSON"]),
                                    CodigoSap = Convert.ToString(r["CODIGO_SAP"]),
                                    Activo = Convert.ToBoolean(r["ACTIVO"])
                                });
                            }
                        }
                    }
                    cliente.Id = UpsertCliente(cn, tx, cliente.Empresa, cliente.Ficha,
                        cliente.FichaJson, cliente.CodigoSap, usuario,
                        cliente.Id == 0 ? (long?)null : cliente.Id, cliente.Activo, false);
                    if (nuevaFicha)
                        VincularUsuario(cn, tx, cliente.Id, cliente.Empresa, usuario, "DIRECTA");
                    foreach (var archivo in archivos) GuardarArchivo(cn, tx, "CLIENTE_ID", cliente.Id,
                        cliente.Empresa, archivo, usuario, ip);
                    ValidarArchivos(cn, tx, cliente.Id, false, cliente.Empresa);
                    Auditar(cn, tx, "CLIENTE", cliente.Id, cliente.Empresa, usuario,
                        antes == null ? "CREAR" : "EDITAR", "Código SAP: " + (cliente.CodigoSap ?? "Sin vincular"),
                        antes, Newtonsoft.Json.JsonConvert.SerializeObject(new {
                            Ficha = cliente.FichaJson, CodigoSap = cliente.CodigoSap, Activo = cliente.Activo
                        }), ip);
                    tx.Commit();
                    return cliente.Id;
                }
            }
        }

        public void CambiarActivo(long id, string empresa, int version, bool activo, string usuario, string ip)
        {
            using (var cn = new SqlConnection(_conexion))
            {
                cn.Open();
                using (var tx = cn.BeginTransaction())
                {
                    const string sql = @"UPDATE dbo.CRM_CLIENTE_EMPRESA
                        SET ACTIVO=@activo,VERSION=VERSION+1,ACTUALIZADO_POR=@usuario,
                            ACTUALIZADO_EN=SYSDATETIME()
                        WHERE CLIENTE_ID=@id AND EMPRESA=@empresa AND VERSION=@version;";
                    using (var cmd = new SqlCommand(sql, cn, tx))
                    {
                        P(cmd, "@activo", activo); P(cmd, "@usuario", usuario);
                        P(cmd, "@id", id); P(cmd, "@empresa", empresa); P(cmd, "@version", version);
                        if (cmd.ExecuteNonQuery() != 1)
                            throw new InvalidOperationException("La ficha cambió. Actualice la página.");
                    }
                    Auditar(cn, tx, "CLIENTE", id, empresa, usuario,
                        activo ? "REACTIVAR" : "DESACTIVAR", null, null, null, ip);
                    tx.Commit();
                }
            }
        }

        public List<ClienteCrmArchivo> Archivos(long id, bool esSolicitud, string empresa = null)
        {
            string columna = esSolicitud ? "SOLICITUD_ID" : "CLIENTE_ID";
            string sql = esSolicitud
                ? "SELECT ID,TIPO,NOMBRE,CONTENT_TYPE,TAMANO FROM dbo.CRM_ARCHIVO WHERE SOLICITUD_ID=@id ORDER BY CREADO_EN DESC;"
                : @"SELECT A.ID,A.TIPO,A.NOMBRE,A.CONTENT_TYPE,A.TAMANO FROM dbo.CRM_ARCHIVO A
                    WHERE A.EMPRESA=@empresa AND (A.CLIENTE_ID=@id OR A.SOLICITUD_ID IN
                       (SELECT S.ID FROM dbo.CRM_SOLICITUD S WHERE S.CLIENTE_ID=@id
                        AND S.EMPRESA=@empresa AND S.ESTADO='APROBADA')) ORDER BY A.CREADO_EN DESC;";
            var lista = new List<ClienteCrmArchivo>();
            using (var cn = new SqlConnection(_conexion))
            using (var cmd = new SqlCommand(sql, cn))
            {
                P(cmd, "@id", id);
                if (!esSolicitud) P(cmd, "@empresa", empresa);
                cn.Open();
                using (var r = cmd.ExecuteReader()) while (r.Read())
                    lista.Add(new ClienteCrmArchivo { Id = Convert.ToInt64(r["ID"]),
                        Tipo = Convert.ToString(r["TIPO"]), Nombre = Convert.ToString(r["NOMBRE"]),
                        ContentType = Convert.ToString(r["CONTENT_TYPE"]), Tamano = Convert.ToInt32(r["TAMANO"]) });
            }
            return lista;
        }

        public ClienteCrmArchivo ObtenerArchivo(long id)
        {
            const string sql = @"SELECT ID,TIPO,NOMBRE,CONTENT_TYPE,TAMANO,CONTENIDO
                FROM dbo.CRM_ARCHIVO WHERE ID=@id;";
            using (var cn = new SqlConnection(_conexion))
            using (var cmd = new SqlCommand(sql, cn))
            {
                P(cmd, "@id", id); cn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    return new ClienteCrmArchivo { Id = id, Tipo = Convert.ToString(r["TIPO"]),
                        Nombre = Convert.ToString(r["NOMBRE"]), ContentType = Convert.ToString(r["CONTENT_TYPE"]),
                        Tamano = Convert.ToInt32(r["TAMANO"]), Contenido = (byte[])r["CONTENIDO"] };
                }
            }
        }

        public bool ArchivoPertenece(long archivoId, long entidadId, bool esSolicitud, string empresa = null)
        {
            string sql = esSolicitud
                ? "SELECT COUNT(*) FROM dbo.CRM_ARCHIVO WHERE ID=@archivo AND SOLICITUD_ID=@id"
                : @"SELECT COUNT(*) FROM dbo.CRM_ARCHIVO A WHERE A.ID=@archivo AND A.EMPRESA=@empresa
                    AND (A.CLIENTE_ID=@id OR A.SOLICITUD_ID IN
                    (SELECT S.ID FROM dbo.CRM_SOLICITUD S WHERE S.CLIENTE_ID=@id
                     AND S.EMPRESA=@empresa AND S.ESTADO='APROBADA'))";
            using (var cn = new SqlConnection(_conexion))
            using (var cmd = new SqlCommand(sql, cn))
            {
                P(cmd, "@archivo", archivoId); P(cmd, "@id", entidadId);
                if (!esSolicitud) P(cmd, "@empresa", empresa);
                cn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) == 1;
            }
        }

        public List<ClienteCrmEvento> Eventos(string entidad, long id, string empresa = null)
        {
            const string sql = @"SELECT ID,FECHA,USUARIO,ACCION,DETALLE FROM dbo.CRM_AUDITORIA
                WHERE ENTIDAD=@entidad AND ENTIDAD_ID=@id
                  AND (@empresa IS NULL OR EMPRESA=@empresa)
                ORDER BY FECHA DESC,ID DESC;";
            var lista = new List<ClienteCrmEvento>();
            using (var cn = new SqlConnection(_conexion))
            using (var cmd = new SqlCommand(sql, cn))
            {
                P(cmd, "@entidad", entidad); P(cmd, "@id", id);
                P(cmd, "@empresa", empresa); cn.Open();
                using (var r = cmd.ExecuteReader()) while (r.Read())
                    lista.Add(new ClienteCrmEvento { Id = Convert.ToInt64(r["ID"]),
                        Fecha = Convert.ToDateTime(r["FECHA"]), Usuario = Convert.ToString(r["USUARIO"]),
                        Accion = Convert.ToString(r["ACCION"]), Detalle = Convert.ToString(r["DETALLE"]) });
            }
            return lista;
        }

        public void RegistrarEvento(string entidad, long? id, string empresa, string usuario,
            string accion, string detalle, string ip)
        {
            using (var cn = new SqlConnection(_conexion))
            {
                cn.Open();
                Auditar(cn, null, entidad, id, empresa, usuario, accion, detalle, null, null, ip);
            }
        }

        private static long UpsertCliente(SqlConnection cn, SqlTransaction tx, string empresa,
            ClienteCrmFicha ficha, string json, string codigoSap, string usuario, long? id,
            bool activo, bool preservarCodigoSap)
        {
            string clave = NormalizarNit(ficha.NitDpi);
            if (!id.HasValue)
            {
                const string encontrar = @"SELECT ID FROM dbo.CRM_CLIENTE WITH (UPDLOCK,HOLDLOCK)
                    WHERE NIT_CLAVE=@nit;";
                using (var cmd = new SqlCommand(encontrar, cn, tx))
                {
                    P(cmd, "@nit", clave);
                    object existente = cmd.ExecuteScalar();
                    if (existente != null) id = Convert.ToInt64(existente);
                }
            }
            if (!id.HasValue)
            {
                const string crear = @"INSERT dbo.CRM_CLIENTE
                    (NIT_CLAVE,RAZON_SOCIAL,NOMBRE_COMERCIAL,CREADO_POR,ACTUALIZADO_POR)
                    OUTPUT INSERTED.ID VALUES (@nit,@razon,@comercial,@usuario,@usuario);";
                using (var cmd = new SqlCommand(crear, cn, tx))
                {
                    P(cmd, "@nit", clave); P(cmd, "@razon", ficha.RazonSocial);
                    P(cmd, "@comercial", ficha.NombreComercial); P(cmd, "@usuario", usuario);
                    id = Convert.ToInt64(cmd.ExecuteScalar());
                }
            }
            else
            {
                const string actualizar = @"UPDATE dbo.CRM_CLIENTE SET
                    NIT_CLAVE=@nit,RAZON_SOCIAL=@razon,NOMBRE_COMERCIAL=@comercial,
                    VERSION=VERSION+1,ACTUALIZADO_POR=@usuario,ACTUALIZADO_EN=SYSDATETIME()
                    WHERE ID=@id;";
                using (var cmd = new SqlCommand(actualizar, cn, tx))
                {
                    P(cmd, "@nit", clave); P(cmd, "@razon", ficha.RazonSocial);
                    P(cmd, "@comercial", ficha.NombreComercial); P(cmd, "@usuario", usuario);
                    P(cmd, "@id", id.Value); cmd.ExecuteNonQuery();
                }
            }
            const string upsert = @"UPDATE dbo.CRM_CLIENTE_EMPRESA SET
                FICHA_JSON=@ficha,CODIGO_SAP=CASE WHEN @preservarCodigo=1 THEN CODIGO_SAP ELSE @codigo END,
                ACTIVO=@activo,
                COMPRA_ACTUAL=@compra,MONEDA=@moneda,VENTA_12M=@venta,POTENCIAL_ANUAL=@potencial,
                OBJETIVO_12M=@objetivo,VERSION=VERSION+1,
                ACTUALIZADO_POR=@usuario,ACTUALIZADO_EN=SYSDATETIME()
                WHERE CLIENTE_ID=@id AND EMPRESA=@empresa;
                IF @@ROWCOUNT=0 INSERT dbo.CRM_CLIENTE_EMPRESA
                (CLIENTE_ID,EMPRESA,CODIGO_SAP,FICHA_JSON,ACTIVO,COMPRA_ACTUAL,MONEDA,VENTA_12M,
                 POTENCIAL_ANUAL,OBJETIVO_12M,ACTUALIZADO_POR)
                VALUES (@id,@empresa,@codigo,@ficha,@activo,@compra,@moneda,@venta,@potencial,@objetivo,@usuario);";
            using (var cmd = new SqlCommand(upsert, cn, tx))
            {
                P(cmd, "@id", id.Value); P(cmd, "@empresa", empresa);
                P(cmd, "@codigo", string.IsNullOrWhiteSpace(codigoSap) ? null : codigoSap.Trim());
                P(cmd, "@preservarCodigo", preservarCodigoSap);
                P(cmd, "@ficha", json); P(cmd, "@activo", activo);
                P(cmd, "@compra", ficha.CompraActualmente); P(cmd, "@venta", ficha.Venta12Meses);
                P(cmd, "@moneda", ficha.MonedaIndicadores);
                P(cmd, "@potencial", ficha.PotencialAnual); P(cmd, "@objetivo", ficha.Objetivo12Meses);
                P(cmd, "@usuario", usuario); cmd.ExecuteNonQuery();
            }
            return id.Value;
        }

        private static void VincularUsuario(SqlConnection cn, SqlTransaction tx, long clienteId,
            string empresa, string usuario, string origen)
        {
            const string sql = @"INSERT dbo.CRM_CLIENTE_USUARIO (CLIENTE_ID,EMPRESA,USUARIO,ORIGEN)
                SELECT @id,@empresa,@usuario,@origen WHERE NOT EXISTS
                (SELECT 1 FROM dbo.CRM_CLIENTE_USUARIO WITH (UPDLOCK,HOLDLOCK)
                 WHERE CLIENTE_ID=@id AND EMPRESA=@empresa AND USUARIO=@usuario);";
            using (var cmd = new SqlCommand(sql, cn, tx))
            {
                P(cmd, "@id", clienteId); P(cmd, "@empresa", empresa);
                P(cmd, "@usuario", usuario); P(cmd, "@origen", origen);
                cmd.ExecuteNonQuery();
            }
        }

        private static void CopiarArchivosOrigen(SqlConnection cn, SqlTransaction tx,
            ClienteCrmSolicitud solicitud, string usuario, string ip)
        {
            const string sql = @"WITH Ultimos AS (
                SELECT A.TIPO,A.NOMBRE,A.CONTENT_TYPE,A.TAMANO,A.CONTENIDO,
                    ROW_NUMBER() OVER (PARTITION BY A.TIPO ORDER BY A.CREADO_EN DESC,A.ID DESC) AS FILA
                FROM dbo.CRM_ARCHIVO A
                WHERE A.EMPRESA=@empresa AND
                    (A.CLIENTE_ID=@cliente OR A.SOLICITUD_ID IN
                        (SELECT S.ID FROM dbo.CRM_SOLICITUD S
                         WHERE S.CLIENTE_ID=@cliente AND S.EMPRESA=@empresa AND S.ESTADO='APROBADA'))
            )
            INSERT dbo.CRM_ARCHIVO
                (SOLICITUD_ID,EMPRESA,TIPO,NOMBRE,CONTENT_TYPE,TAMANO,CONTENIDO,CREADO_POR)
            SELECT @solicitud,@empresa,TIPO,NOMBRE,CONTENT_TYPE,TAMANO,CONTENIDO,@usuario
            FROM Ultimos WHERE FILA=1;";
            int copiados;
            using (var cmd = new SqlCommand(sql, cn, tx))
            {
                P(cmd, "@empresa", solicitud.Empresa);
                P(cmd, "@cliente", solicitud.OrigenClienteId);
                P(cmd, "@solicitud", solicitud.Id);
                P(cmd, "@usuario", usuario);
                copiados = cmd.ExecuteNonQuery();
            }
            if (copiados > 0)
                Auditar(cn, tx, "SOLICITUD", solicitud.Id, solicitud.Empresa, usuario,
                    "COPIAR_DOCUMENTOS_ORIGEN", "Ficha #" + solicitud.OrigenClienteId + "; Archivos=" + copiados,
                    null, null, ip);
        }

        private static void GuardarArchivo(SqlConnection cn, SqlTransaction tx, string columna,
            long id, string empresa, ClienteCrmArchivo archivo, string usuario, string ip)
        {
            string anterior = null;
            using (var buscar = new SqlCommand("SELECT NOMBRE FROM dbo.CRM_ARCHIVO WHERE " + columna + "=@id AND EMPRESA=@empresa AND TIPO=@tipo", cn, tx))
            {
                P(buscar, "@id", id); P(buscar, "@empresa", empresa); P(buscar, "@tipo", archivo.Tipo);
                anterior = Convert.ToString(buscar.ExecuteScalar());
            }
            using (var borrar = new SqlCommand("DELETE FROM dbo.CRM_ARCHIVO WHERE " + columna + "=@id AND EMPRESA=@empresa AND TIPO=@tipo", cn, tx))
            {
                P(borrar, "@id", id); P(borrar, "@empresa", empresa); P(borrar, "@tipo", archivo.Tipo); borrar.ExecuteNonQuery();
            }
            string sql = "INSERT dbo.CRM_ARCHIVO (" + columna + @",EMPRESA,TIPO,NOMBRE,CONTENT_TYPE,TAMANO,CONTENIDO,CREADO_POR)
                VALUES (@id,@empresa,@tipo,@nombre,@mime,@tamano,@contenido,@usuario);";
            using (var cmd = new SqlCommand(sql, cn, tx))
            {
                P(cmd, "@id", id); P(cmd, "@empresa", empresa);
                P(cmd, "@tipo", archivo.Tipo); P(cmd, "@nombre", archivo.Nombre);
                P(cmd, "@mime", archivo.ContentType); P(cmd, "@tamano", archivo.Tamano);
                cmd.Parameters.Add("@contenido", SqlDbType.VarBinary, -1).Value = archivo.Contenido;
                P(cmd, "@usuario", usuario); cmd.ExecuteNonQuery();
            }
            Auditar(cn, tx, columna == "SOLICITUD_ID" ? "SOLICITUD" : "CLIENTE", id,
                empresa, usuario, anterior == "" ? "ADJUNTAR_ARCHIVO" : "REEMPLAZAR_ARCHIVO",
                archivo.Tipo, anterior, archivo.Nombre, ip);
        }

        private static void ValidarArchivos(SqlConnection cn, SqlTransaction tx, long id,
            bool esSolicitud, string empresa)
        {
            var tipos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string sql = esSolicitud
                ? "SELECT TIPO FROM dbo.CRM_ARCHIVO WHERE SOLICITUD_ID=@id AND EMPRESA=@empresa"
                : @"SELECT A.TIPO FROM dbo.CRM_ARCHIVO A WHERE A.EMPRESA=@empresa
                    AND (A.CLIENTE_ID=@id OR A.SOLICITUD_ID IN
                    (SELECT S.ID FROM dbo.CRM_SOLICITUD S WHERE S.CLIENTE_ID=@id
                     AND S.EMPRESA=@empresa AND S.ESTADO='APROBADA'))";
            using (var cmd = new SqlCommand(sql, cn, tx))
            {
                P(cmd, "@id", id); P(cmd, "@empresa", empresa);
                using (var r = cmd.ExecuteReader()) while (r.Read()) tipos.Add(Convert.ToString(r[0]));
            }
            if (!tipos.Contains("RTU") || !tipos.Contains("DPI"))
                throw new InvalidOperationException("Adjunte RTU actualizado y DPI de ambos lados para enviar.");
        }

        private static void ParamsSolicitud(SqlCommand cmd, ClienteCrmSolicitud s, bool enviar, string usuario)
        {
            P(cmd, "@tipo", s.TipoSolicitud); P(cmd, "@origenId", s.OrigenClienteId);
            P(cmd, "@origenVersion", s.OrigenVersion); P(cmd, "@origenSap", s.OrigenCodigoSap);
            P(cmd, "@empresa", s.Empresa); P(cmd, "@operador", s.CodigoOperador);
            P(cmd, "@agente", s.Agente); P(cmd, "@razon", s.Ficha.RazonSocial);
            P(cmd, "@nit", NormalizarNit(s.Ficha.NitDpi));
            P(cmd, "@estado", enviar ? EstadosSolicitudCliente.Enviada : EstadosSolicitudCliente.Borrador);
            P(cmd, "@ficha", s.FichaJson); P(cmd, "@compra", s.Ficha.CompraActualmente);
            P(cmd, "@moneda", s.Ficha.MonedaIndicadores);
            P(cmd, "@venta", s.Ficha.Venta12Meses); P(cmd, "@potencial", s.Ficha.PotencialAnual);
            P(cmd, "@objetivo", s.Ficha.Objetivo12Meses); P(cmd, "@usuario", usuario);
            P(cmd, "@enviar", enviar);
        }

        private static string NormalizarNit(string nit)
        {
            return (nit ?? "").Trim().ToUpperInvariant().Replace(" ", "").Replace("-", "");
        }

        private static void Auditar(SqlConnection cn, SqlTransaction tx, string entidad, long? id,
            string empresa, string usuario, string accion, string detalle, string antes, string despues, string ip)
        {
            const string sql = @"INSERT dbo.CRM_AUDITORIA
                (ENTIDAD,ENTIDAD_ID,EMPRESA,USUARIO,ACCION,DETALLE,ANTES_JSON,DESPUES_JSON,IP)
                VALUES (@entidad,@id,@empresa,@usuario,@accion,@detalle,@antes,@despues,@ip);";
            using (var cmd = new SqlCommand(sql, cn, tx))
            {
                P(cmd, "@entidad", entidad); P(cmd, "@id", id); P(cmd, "@empresa", empresa);
                P(cmd, "@usuario", usuario); P(cmd, "@accion", accion); P(cmd, "@detalle", detalle);
                P(cmd, "@antes", antes); P(cmd, "@despues", despues); P(cmd, "@ip", ip);
                cmd.ExecuteNonQuery();
            }
        }

        private static ClienteCrmSolicitud MapSolicitud(SqlDataReader r)
        {
            return new ClienteCrmSolicitud {
                Id = Convert.ToInt64(r["ID"]), ClienteId = NLong(r, "CLIENTE_ID"),
                TipoSolicitud = Convert.ToString(r["TIPO_SOLICITUD"]),
                OrigenClienteId = NLong(r, "ORIGEN_CLIENTE_ID"),
                OrigenVersion = r["ORIGEN_VERSION"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["ORIGEN_VERSION"]),
                OrigenCodigoSap = r["ORIGEN_CODIGO_SAP"] == DBNull.Value
                    ? null : Convert.ToString(r["ORIGEN_CODIGO_SAP"]),
                Empresa = Convert.ToString(r["EMPRESA"]), CodigoOperador = Convert.ToString(r["CODIGO_OPERADOR"]),
                Agente = Convert.ToString(r["AGENTE"]), Estado = Convert.ToString(r["ESTADO"]),
                FichaJson = Convert.ToString(r["FICHA_JSON"]), CreadoPor = Convert.ToString(r["CREADO_POR"]),
                CreadoEn = Convert.ToDateTime(r["CREADO_EN"]), EnviadoEn = NDate(r, "ENVIADO_EN"),
                ResueltoEn = NDate(r, "RESUELTO_EN"), ResueltoPor = Convert.ToString(r["RESUELTO_POR"]),
                MotivoRechazo = Convert.ToString(r["MOTIVO_RECHAZO"]), Version = Convert.ToInt32(r["VERSION"])
            };
        }

        private static ClienteCrmCliente MapCliente(SqlDataReader r)
        {
            return new ClienteCrmCliente {
                Id = Convert.ToInt64(r["ID"]), Empresa = Convert.ToString(r["EMPRESA"]),
                CodigoSap = Convert.ToString(r["CODIGO_SAP"]), FichaJson = Convert.ToString(r["FICHA_JSON"]),
                Activo = Convert.ToBoolean(r["ACTIVO"]), Version = Convert.ToInt32(r["VERSION"]),
                ActualizadoEn = Convert.ToDateTime(r["ACTUALIZADO_EN"])
            };
        }

        private static long? NLong(SqlDataReader r, string columna)
        {
            return r[columna] == DBNull.Value ? (long?)null : Convert.ToInt64(r[columna]);
        }

        private static DateTime? NDate(SqlDataReader r, string columna)
        {
            return r[columna] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r[columna]);
        }

        private static void P(SqlCommand cmd, string nombre, object valor)
        {
            cmd.Parameters.AddWithValue(nombre, valor ?? DBNull.Value);
        }
    }
}
