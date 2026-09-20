using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using DiamDev.Give.Entities;

namespace DiamDev.Give.DAL
{
    public sealed class PilotoRutaDA
    {
        private readonly string conexion;
        private readonly string apk;
        private sealed class Acceso { public long Usuario; public string Nombre; public string Operador; public string PlacaPrueba; public string RutaPrueba; }

        public static bool Habilitado { get { return EsTrue("Pilotos.Habilitado"); } }
        public static bool PruebasSoloLectura { get { return EsTrue("Pilotos.PruebasSoloLectura"); } }
        public static bool CierreHabilitado { get { return !PruebasSoloLectura && EsTrue("Pilotos.PermitirCierre"); } }
        private static bool EsTrue(string key) { return string.Equals(ConfigurationManager.AppSettings[key], "true", StringComparison.OrdinalIgnoreCase); }
        public static bool PermiteUsuarioPrueba(string login)
        {
            var permitido=ConfigurationManager.AppSettings["Pilotos.UsuarioPrueba"];
            return PruebasSoloLectura && !string.IsNullOrWhiteSpace(login) && !string.IsNullOrWhiteSpace(permitido)
                && string.Equals(login,permitido.Trim(),StringComparison.OrdinalIgnoreCase);
        }

        public PilotoRutaDA()
        {
            if (!Habilitado && !PruebasSoloLectura) throw new PilotoException(503, "El acceso a rutas no esta disponible. Contacta a Distribucion.");
            var posConfig = ConfigurationManager.ConnectionStrings["GiveContext"];
            var apkConfig = ConfigurationManager.ConnectionStrings["APK66Context"];
            if (posConfig == null) throw new ConfigurationErrorsException("Falta la conexion POS de pilotos.");
            var pos = new SqlConnectionStringBuilder(posConfig.ConnectionString);
            // El catalogo explicito siempre se consulta en la instancia de GiveContext.
            // No modifica ni reutiliza credenciales de conexiones de otros modulos.
            var catalogo = ConfigurationManager.AppSettings["Pilotos.CatalogoRutas"];
            if (string.IsNullOrWhiteSpace(catalogo))
            {
                if (apkConfig == null) throw new ConfigurationErrorsException("Falta el catalogo de rutas.");
                var origen = new SqlConnectionStringBuilder(apkConfig.ConnectionString);
                if (!string.Equals(pos.DataSource, origen.DataSource, StringComparison.OrdinalIgnoreCase))
                    throw new PilotoException(503,"Configura Pilotos.CatalogoRutas con la base de rutas de la misma instancia POS.");
                catalogo = origen.InitialCatalog;
            }
            catalogo = (catalogo ?? "").Trim();
            if (string.IsNullOrWhiteSpace(pos.InitialCatalog) || string.IsNullOrWhiteSpace(catalogo) || catalogo.Length>128 ||
                string.Equals(pos.InitialCatalog, catalogo, StringComparison.OrdinalIgnoreCase))
                throw new ConfigurationErrorsException("Pilotos requiere dos catalogos diferentes en la misma instancia SQL.");
            conexion = pos.ConnectionString;
            using (var builder = new SqlCommandBuilder()) apk = builder.QuoteIdentifier(catalogo);
        }

        private SqlCommand Command(SqlConnection cn, SqlTransaction tx, string sql)
        { return new SqlCommand(sql, cn, tx) { CommandTimeout = 15 }; }
        private static void Param(SqlCommand cmd, string name, SqlDbType type, object value, int size = 0)
        {
            var p = size == 0 ? cmd.Parameters.Add(name, type) : cmd.Parameters.Add(name, type, size);
            p.Value = value ?? DBNull.Value;
        }
        private static string Texto(SqlDataReader r, string name) { return r[name] == DBNull.Value ? null : Convert.ToString(r[name], CultureInfo.InvariantCulture); }

        private Acceso Autorizar(SqlConnection cn, SqlTransaction tx, string login, string permiso)
        {
            if (string.IsNullOrWhiteSpace(login) || login.Length > 50) throw new PilotoException(403, "Acceso no autorizado.");
            if (PruebasSoloLectura) return AutorizarPrueba(cn,tx,login,permiso);
            var sql = @"SELECT TOP (2) u.Usuario_Id, e.NOMBRE, p.Codigo_Operador
FROM dbo.Usuario u JOIN dbo.PilotoVinculo p ON p.Usuario_Id=u.Usuario_Id
JOIN " + apk + @".dbo.RT_EMPLEADOS e ON e.ROWID=p.Empleado_RowId
WHERE u.Login=@login AND u.Activo=1 AND u.Autenticar_Site=1 AND p.Activo=1
AND NOT EXISTS (SELECT 1 FROM dbo.Usuario otroUsuario WHERE otroUsuario.Login=u.Login AND otroUsuario.Usuario_Id<>u.Usuario_Id)
AND e.ESTADO=1 AND e.CARGO=N'PILOTO' AND NULLIF(LTRIM(RTRIM(e.NOMBRE)),N'') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM " + apk + @".dbo.RT_EMPLEADOS otro
                WHERE otro.CARGO=N'PILOTO' AND otro.NOMBRE=e.NOMBRE AND otro.ROWID<>e.ROWID)
AND EXISTS (SELECT 1 FROM dbo.Usuario_Rol ur JOIN dbo.Rol r ON r.Rol_Id=ur.Rol_Id
            WHERE ur.Usuario_Id=u.Usuario_Id AND r.Nombre=N'PILOTO')
AND EXISTS (SELECT 1 FROM dbo.Usuario_Rol ur JOIN dbo.Rol_Permiso rp ON rp.Rol_Id=ur.Rol_Id
            WHERE ur.Usuario_Id=u.Usuario_Id AND rp.Permiso_Id=@permiso);";
            using (var cmd = Command(cn, tx, sql))
            {
                Param(cmd, "@login", SqlDbType.NVarChar, login, 50);
                Param(cmd, "@permiso", SqlDbType.NVarChar, permiso, 100);
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) throw new PilotoException(403, "Tu acceso de piloto no esta configurado o esta inactivo. Contacta a Distribucion.");
                    var a = new Acceso { Usuario = (long)r["Usuario_Id"], Nombre = Texto(r, "NOMBRE"), Operador = Texto(r, "Codigo_Operador") };
                    if (r.Read() || string.IsNullOrWhiteSpace(a.Operador)) throw new PilotoException(403, "El vinculo de piloto es ambiguo o incompleto.");
                    return a;
                }
            }
        }

        private Acceso AutorizarPrueba(SqlConnection cn,SqlTransaction tx,string login,string permiso)
        {
            // Excepcion temporal: solo la consulta del usuario/vehiculo configurados.
            // No requiere rol PILOTO ni tablas de vinculos; conserva el login POS activo.
            if(permiso!="Pilotos.Rutas.Ver" || !PermiteUsuarioPrueba(login))
                throw new PilotoException(403,"La consulta temporal no esta habilitada para tu usuario.");
            var placa=ConfigurationManager.AppSettings["Pilotos.PlacaPrueba"];
            var ruta=ConfigurationManager.AppSettings["Pilotos.RutaPrueba"];
            placa=string.IsNullOrWhiteSpace(placa) ? null : placa.Trim();
            ruta=string.IsNullOrWhiteSpace(ruta) ? null : ruta.Trim();
            if((placa==null)==(ruta==null) || (placa??ruta).Length>15)
                throw new PilotoException(503,"Configura solo una opcion: Pilotos.PlacaPrueba o Pilotos.RutaPrueba, con un identificador de hasta 15 caracteres.");
            using(var cmd=Command(cn,tx,@"SELECT u.Usuario_Id FROM dbo.Usuario u
WHERE u.Login=@login AND u.Activo=1 AND u.Autenticar_Site=1
AND NOT EXISTS(SELECT 1 FROM dbo.Usuario otro WHERE otro.Login=u.Login AND otro.Usuario_Id<>u.Usuario_Id);"))
            {
                Param(cmd,"@login",SqlDbType.NVarChar,login,50);
                using(var r=cmd.ExecuteReader())
                {
                    if(!r.Read()) throw new PilotoException(403,"El usuario de prueba debe estar activo y habilitado para web.");
                    return new Acceso { Usuario=(long)r["Usuario_Id"], PlacaPrueba=placa, RutaPrueba=ruta };
                }
            }
        }

        private static string Alcance(Acceso a)
        {
            return a.PlacaPrueba!=null ? "r.PLACA=@placa" : a.RutaPrueba!=null ? "r.ID_RUTA=@rutaPrueba" : @"r.PILOTO=@piloto
AND EXISTS (SELECT 1 FROM dbo.PilotoCentro c WHERE c.Usuario_Id=@usuario AND c.Centro_Dist=r.CENTRO_DIST)";
        }
        private static void ParametrosAcceso(SqlCommand cmd,Acceso a)
        {
            if(a.PlacaPrueba!=null) Param(cmd,"@placa",SqlDbType.NVarChar,a.PlacaPrueba,15);
            else if(a.RutaPrueba!=null) Param(cmd,"@rutaPrueba",SqlDbType.NVarChar,a.RutaPrueba,15);
            else { Param(cmd,"@piloto",SqlDbType.NVarChar,a.Nombre,90); Param(cmd,"@usuario",SqlDbType.BigInt,a.Usuario); }
        }

        private static PilotoRuta Cabecera(SqlDataReader r)
        {
            return new PilotoRuta { Id = Texto(r, "ID_RUTA"), Fecha = (DateTime)r["FECHA_RUTA"],
                Placa = Texto(r, "PLACA"), Piloto = Texto(r, "PILOTO"), Centro = Texto(r, "CENTRO_DIST"),
                Estado = Texto(r, "STATUS"), Solicitud = Guid.NewGuid() };
        }

        public PilotoLista Listar(string login, DateTime desde, DateTime hasta, int pagina)
        {
            desde = desde.Date; hasta = hasta.Date;
            if (hasta < desde || (hasta-desde).TotalDays >= 31 || hasta == DateTime.MaxValue.Date || pagina < 1 || pagina > 1000)
                throw new PilotoException(400, "Selecciona un periodo de hasta 31 dias y una pagina valida.");
            var result = new PilotoLista { Desde=desde, Hasta=hasta, Pagina=pagina, Rutas=new List<PilotoRuta>() };
            using (var cn = new SqlConnection(conexion))
            {
                cn.Open();
                using (var tx = cn.BeginTransaction(IsolationLevel.Serializable))
                {
                    var a = Autorizar(cn, tx, login, "Pilotos.Rutas.Ver");
                    result.RutaFija=a.RutaPrueba!=null;
                    var sql = @"SELECT r.ID_RUTA,r.FECHA_RUTA,r.PLACA,r.PILOTO,r.CENTRO_DIST,r.STATUS
FROM " + apk + @".dbo.RT_RUTAS r
WHERE " + Alcance(a) + (result.RutaFija ? "" : " AND r.FECHA_RUTA>=@desde AND r.FECHA_RUTA<@fin") + @"
AND r.STATUS IN (N'A',N'E',N'C',N'X')
ORDER BY r.FECHA_RUTA DESC,r.ID_RUTA DESC OFFSET @offset ROWS FETCH NEXT 26 ROWS ONLY;";
                    using (var cmd = Command(cn, tx, sql))
                    {
                        ParametrosAcceso(cmd,a);
                        Param(cmd,"@desde",SqlDbType.Date,desde); Param(cmd,"@fin",SqlDbType.Date,hasta.AddDays(1));
                        Param(cmd,"@offset",SqlDbType.Int,(pagina-1)*25);
                        using (var r=cmd.ExecuteReader()) while(r.Read()) result.Rutas.Add(Cabecera(r));
                    }
                    tx.Commit();
                }
            }
            result.HayMas=result.Rutas.Count>25;
            if(result.HayMas) result.Rutas.RemoveAt(25);
            return result;
        }

        private PilotoRuta LeerRuta(SqlConnection cn, SqlTransaction tx, Acceso a, string id, bool bloquear)
        {
            if(string.IsNullOrWhiteSpace(id) || id.Length>15) throw new PilotoException(404,"Ruta no disponible.");
            PilotoRuta ruta;
            var hint = bloquear ? " WITH (UPDLOCK,HOLDLOCK) " : " ";
            var sql = @"SELECT r.ID_RUTA,r.FECHA_RUTA,r.PLACA,r.PILOTO,r.CENTRO_DIST,r.STATUS,r.LIQUIDADO
FROM " + apk + ".dbo.RT_RUTAS r" + hint + "WHERE r.ID_RUTA=@id AND " + Alcance(a) + ";";
            using(var cmd=Command(cn,tx,sql))
            {
                Param(cmd,"@id",SqlDbType.NVarChar,id,15); ParametrosAcceso(cmd,a);
                using(var r=cmd.ExecuteReader())
                {
                    if(!r.Read()) throw new PilotoException(404,"Ruta no disponible o reasignada.");
                    ruta=Cabecera(r);
                    ruta.PuedeCerrar=CierreHabilitado && ruta.Estado=="E" && (r["LIQUIDADO"]==DBNull.Value || !(bool)r["LIQUIDADO"]);
                }
            }
            sql=@"SELECT TOP (@limite) ROWID,TIPO,ID_EMPRESA,ID_DOCUMENTO,CLIENTE,DIR_DESPACHO,NO_BULTOS,
MO_VISITO,MO_ENTREGA,MO_MOTIVO,MO_OBSER,MO_HR_ENTRADA,MO_HR_SALIDA
FROM " + apk + ".dbo.RT_RUTAS_DET" + hint + "WHERE ID_RUTA=@id ORDER BY ROWID;";
            using(var cmd=Command(cn,tx,sql))
            {
                Param(cmd,"@id",SqlDbType.NVarChar,id,15);
                Param(cmd,"@limite",SqlDbType.Int,PilotoReglas.MaxDocumentos+1);
                using(var r=cmd.ExecuteReader()) while(r.Read())
                {
                    ruta.Documentos.Add(new PilotoDocumento { RowId=(int)r["ROWID"], Tipo=Texto(r,"TIPO"), Empresa=Texto(r,"ID_EMPRESA"),
                        Documento=Texto(r,"ID_DOCUMENTO"), Cliente=Texto(r,"CLIENTE"), Direccion=Texto(r,"DIR_DESPACHO"), Bultos=(decimal)r["NO_BULTOS"],
                        Visito=r["MO_VISITO"]==DBNull.Value ? (bool?)null : (bool)r["MO_VISITO"], Entrega=Texto(r,"MO_ENTREGA"), Motivo=Texto(r,"MO_MOTIVO"),
                        Observaciones=Texto(r,"MO_OBSER"), Entrada=r["MO_HR_ENTRADA"]==DBNull.Value ? (TimeSpan?)null : (TimeSpan)r["MO_HR_ENTRADA"],
                        Salida=r["MO_HR_SALIDA"]==DBNull.Value ? (TimeSpan?)null : (TimeSpan)r["MO_HR_SALIDA"] });
                }
            }
            if(ruta.Documentos.Count>PilotoReglas.MaxDocumentos) throw new PilotoException(409,"Esta ruta supera el limite de documentos del portal. Contacta a Distribucion.");
            ruta.Version=PilotoReglas.Version(ruta);
            return ruta;
        }

        public PilotoRuta Detalle(string login,string id)
        {
            using(var cn=new SqlConnection(conexion))
            {
                cn.Open(); using(var tx=cn.BeginTransaction(IsolationLevel.Serializable))
                {
                    var a=Autorizar(cn,tx,login,"Pilotos.Rutas.Ver"); var ruta=LeerRuta(cn,tx,a,id,false); tx.Commit(); return ruta;
                }
            }
        }

        private void VerificarTrigger(SqlConnection cn,SqlTransaction tx)
        {
            var esperado=ConfigurationManager.AppSettings["Pilotos.TriggerSha256"];
            if(string.IsNullOrWhiteSpace(esperado) || esperado.Length!=64)
                throw new PilotoException(503,"El cierre no esta disponible. Contacta a Distribucion.");
            var sql=@"SELECT t.name,t.is_disabled,CONVERT(varchar(64),HASHBYTES('SHA2_256',m.definition),2) AS Huella,
o.name AS Tabla FROM " + apk + @".sys.triggers t
JOIN " + apk + @".sys.objects o ON o.object_id=t.parent_id
JOIN " + apk + @".sys.schemas s ON s.schema_id=o.schema_id
LEFT JOIN " + apk + @".sys.sql_modules m ON m.object_id=t.object_id
WHERE s.name=N'dbo' AND o.name IN(N'RT_RUTAS',N'RT_RUTAS_DET');";
            bool encontrado=false;
            using(var cmd=Command(cn,tx,sql)) using(var r=cmd.ExecuteReader()) while(r.Read())
            {
                bool esRevisado=Texto(r,"Tabla")=="RT_RUTAS" && Texto(r,"name")=="INSERT_MA_CUST_ORDER_LISTAS";
                if(esRevisado)
                {
                    encontrado=!(bool)r["is_disabled"] && string.Equals(Texto(r,"Huella"),esperado,StringComparison.OrdinalIgnoreCase);
                    if(!encontrado) throw new PilotoException(503,"El cierre no esta disponible. Contacta a Distribucion.");
                }
                else if(!(bool)r["is_disabled"]) throw new PilotoException(503,"El cierre requiere revision de Distribucion.");
            }
            if(!encontrado) throw new PilotoException(503,"El cierre no esta disponible. Contacta a Distribucion.");
        }

        public void Cerrar(string login,PilotoCierre cierre)
        {
            if(PruebasSoloLectura) throw new PilotoException(403,"El modo de pruebas permite consultar. El cierre de rutas esta desactivado.");
            if(!CierreHabilitado) throw new PilotoException(503,"El cierre no esta disponible. Contacta a Distribucion.");
            var huella=PilotoReglas.HuellaSolicitud(cierre);
            if(cierre.Solicitud==Guid.Empty || string.IsNullOrWhiteSpace(cierre.RutaId) || cierre.RutaId.Length>15)
                throw new PilotoException(400,"La solicitud de cierre no es valida.");
            using(var cn=new SqlConnection(conexion))
            {
                cn.Open(); using(var tx=cn.BeginTransaction(IsolationLevel.Serializable))
                {
                    var a=Autorizar(cn,tx,login,"Pilotos.Rutas.Confirmar");
                    // Serializa duplicados por usuario/solicitud incluso antes de que exista auditoria.
                    using(var cmd=Command(cn,tx,"SELECT Ruta_Id,Huella FROM dbo.PilotoCierre WITH(UPDLOCK,HOLDLOCK) WHERE Usuario_Id=@u AND Solicitud=@s;"))
                    {
                        Param(cmd,"@u",SqlDbType.BigInt,a.Usuario); Param(cmd,"@s",SqlDbType.UniqueIdentifier,cierre.Solicitud);
                        using(var r=cmd.ExecuteReader()) if(r.Read())
                        {
                            if(Texto(r,"Ruta_Id")!=cierre.RutaId || Texto(r,"Huella")!=huella) throw new PilotoException(409,"La solicitud ya se uso con otro contenido.");
                            // No se devuelve informacion comercial en un reintento ya confirmado.
                            r.Close(); tx.Commit(); return;
                        }
                    }
                    var ruta=LeerRuta(cn,tx,a,cierre.RutaId,true);
                    PilotoReglas.ValidarCierre(ruta,cierre);
                    if(!ruta.PuedeCerrar) throw new PilotoException(409,"La ruta no admite cierre desde el portal.");
                    VerificarTrigger(cn,tx);
                    var antes=new XElement("documentos",ruta.Documentos.Select(d=>EstadoXml(d.RowId,d.Visito,d.Entrega,d.Motivo)));
                    foreach(var resultado in cierre.Documentos)
                    {
                        var original=ruta.Documentos.Single(d=>d.RowId==resultado.RowId);
                        if(original.Visito==resultado.Visito && original.Entrega==resultado.Entrega && original.Motivo==resultado.Motivo) continue;
                        using(var cmd=Command(cn,tx,apk+".dbo.portal_piloto_guardar_resultado"))
                        {
                            cmd.CommandType=CommandType.StoredProcedure;
                            Param(cmd,"@rowid",SqlDbType.Int,resultado.RowId); Param(cmd,"@idruta",SqlDbType.NVarChar,ruta.Id,15);
                            Param(cmd,"@motivo",SqlDbType.NVarChar,resultado.Motivo,150);
                            Param(cmd,"@visito",SqlDbType.Bit,resultado.Visito); Param(cmd,"@entrega",SqlDbType.NVarChar,resultado.Entrega,50);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    using(var cmd=Command(cn,tx,apk+".dbo.rutas_cerrar"))
                    {
                        cmd.CommandType=CommandType.StoredProcedure; Param(cmd,"@idruta",SqlDbType.NVarChar,ruta.Id,15);
                        Param(cmd,"@usr",SqlDbType.NVarChar,a.Operador,15); cmd.ExecuteNonQuery();
                    }
                    // Verificar postcondicion; un procedimiento que deja de aplicar los cambios no es exito.
                    var final=LeerRuta(cn,tx,a,ruta.Id,true);
                    if(final.Estado!="C" || final.Documentos.Count!=cierre.Documentos.Count || cierre.Documentos.Any(d=>!final.Documentos.Any(f=>
                        f.RowId==d.RowId && f.Visito==d.Visito && f.Entrega==d.Entrega && (f.Motivo??"")== (d.Motivo??""))) ||
                        final.Documentos.Any(f=>!ruta.Documentos.Any(o=>o.RowId==f.RowId && o.Entrada==f.Entrada && o.Salida==f.Salida && o.Observaciones==f.Observaciones)))
                        throw new PilotoException(409,"No se pudo verificar el cierre. No se guardaron los cambios.");
                    var despues=new XElement("documentos",cierre.Documentos.Select(d=>EstadoXml(d.RowId,d.Visito,d.Entrega,d.Motivo)));
                    using(var cmd=Command(cn,tx,@"INSERT dbo.PilotoCierre(Usuario_Id,Solicitud,Ruta_Id,Login,Operador,Huella,Antes,Despues)
VALUES(@u,@s,@ruta,@login,@operador,@huella,@antes,@despues);"))
                    {
                        Param(cmd,"@u",SqlDbType.BigInt,a.Usuario); Param(cmd,"@s",SqlDbType.UniqueIdentifier,cierre.Solicitud);
                        Param(cmd,"@ruta",SqlDbType.NVarChar,ruta.Id,15); Param(cmd,"@login",SqlDbType.NVarChar,login,50);
                        Param(cmd,"@operador",SqlDbType.NVarChar,a.Operador,15); Param(cmd,"@huella",SqlDbType.VarChar,huella,44);
                        Param(cmd,"@antes",SqlDbType.Xml,antes.ToString(SaveOptions.DisableFormatting));
                        Param(cmd,"@despues",SqlDbType.Xml,despues.ToString(SaveOptions.DisableFormatting)); cmd.ExecuteNonQuery();
                    }
                    tx.Commit();
                }
            }
        }

        private static XElement EstadoXml(int id,bool? visito,string entrega,string motivo)
        { return new XElement("documento",new XAttribute("rowid",id),new XElement("visito",visito),new XElement("entrega",entrega),new XElement("motivo",motivo)); }
    }
}
