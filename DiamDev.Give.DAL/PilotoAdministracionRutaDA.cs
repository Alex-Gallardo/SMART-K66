using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using DiamDev.Give.Entities;

namespace DiamDev.Give.DAL
{
    public sealed partial class PilotoAdministracionDA
    {
        internal string SqlActualizarRuta { get { return @"UPDATE "+apk+@".dbo.RT_RUTAS SET FECHA_RUTA=@fecha,PILOTO=@piloto,CENTRO_DIST=@centro,PLACA=@placa,STATUS=@estado,OBSR_GENERAL=@observacion
WHERE ID_RUTA=@id AND STATUS IN(N'A',N'E') AND ISNULL(LIQUIDADO,0)=0;"; } }
        private static void ExigirHistorialRutas(SqlConnection cn,SqlTransaction tx)
        {
            using(var cmd=Comando(cn,tx,"SELECT CASE WHEN OBJECT_ID(N'dbo.PilotoRutaHistorial',N'U') IS NULL THEN 0 ELSE 1 END;"))
                if((int)cmd.ExecuteScalar()!=1) throw new PilotoException(503,"Falta instalar la migración 26 de auditoría de rutas en POS.");
        }

        private PilotoAdminRutaEdicion LeerRutaAdmin(SqlConnection cn,SqlTransaction tx,string id,bool bloquear)
        {
            var hint=bloquear ? " WITH(UPDLOCK,HOLDLOCK)" : "";
            var sql=@"SELECT r.ID_RUTA,r.FECHA_RUTA,r.PILOTO,r.CENTRO_DIST,r.PLACA,r.STATUS,r.OBSR_GENERAL,r.LIQUIDADO,
 (SELECT COUNT(*) FROM "+apk+@".dbo.RT_RUTAS_DET d WHERE d.ID_RUTA=r.ID_RUTA) Documentos
FROM "+apk+".dbo.RT_RUTAS r"+hint+" WHERE r.ID_RUTA=@id;";
            using(var cmd=Comando(cn,tx,sql))
            {
                Param(cmd,"@id",SqlDbType.NVarChar,id,15);
                using(var r=cmd.ExecuteReader())
                {
                    if(!r.Read()) throw new PilotoException(404,"Ruta no disponible.");
                    var modelo=new PilotoAdminRutaEdicion {
                        Ruta=new PilotoRuta {Id=Texto(r,"ID_RUTA"),Fecha=(DateTime)r["FECHA_RUTA"],Piloto=Texto(r,"PILOTO"),Centro=Texto(r,"CENTRO_DIST"),Placa=Texto(r,"PLACA"),Estado=Texto(r,"STATUS"),ObservacionGeneral=Texto(r,"OBSR_GENERAL")},
                        Liquidada=r["LIQUIDADO"]!=DBNull.Value && Convert.ToBoolean(r["LIQUIDADO"],CultureInfo.InvariantCulture),Documentos=(int)r["Documentos"]
                    };
                    modelo.Formulario=new PilotoAdminRutaGuardar {RutaId=modelo.Ruta.Id,Fecha=modelo.Ruta.Fecha,Piloto=modelo.Ruta.Piloto,Centro=modelo.Ruta.Centro,Placa=modelo.Ruta.Placa,Estado=modelo.Ruta.Estado,ObservacionGeneral=modelo.Ruta.ObservacionGeneral};
                    modelo.Formulario.Version=PilotoAdminRutaReglas.Version(modelo.Ruta,modelo.Liquidada,modelo.Documentos);
                    return modelo;
                }
            }
        }

        public PilotoAdminRutaEdicion ObtenerRuta(string actor,string id)
        {
            if(string.IsNullOrWhiteSpace(id) || id.Length>15) throw new PilotoException(404,"Ruta no disponible.");
            using(var cn=Abrir()) using(var tx=cn.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                Autorizar(cn,tx,actor);ExigirHistorialRutas(cn,tx);
                var modelo=LeerRutaAdmin(cn,tx,id,false);
                var empleados=@"SELECT TOP (500) e.ROWID,e.NOMBRE,e.ESTADO FROM "+apk+@".dbo.RT_EMPLEADOS e
WHERE e.CARGO=N'PILOTO' AND (e.NOMBRE=@actual OR (e.ESTADO=1 AND NOT EXISTS(SELECT 1 FROM "+apk+@".dbo.RT_EMPLEADOS otro WHERE otro.CARGO=N'PILOTO' AND otro.NOMBRE=e.NOMBRE AND otro.ROWID<>e.ROWID)))
ORDER BY e.NOMBRE;";
                using(var cmd=Comando(cn,tx,empleados))
                {
                    Param(cmd,"@actual",SqlDbType.NVarChar,modelo.Ruta.Piloto,90);
                    using(var r=cmd.ExecuteReader()) while(r.Read()) modelo.Pilotos.Add(new PilotoAdminEmpleado {RowId=(int)r["ROWID"],Nombre=Texto(r,"NOMBRE"),Activo=r["ESTADO"]!=DBNull.Value && Convert.ToBoolean(r["ESTADO"],CultureInfo.InvariantCulture),Seleccionado=string.Equals(Texto(r,"NOMBRE"),modelo.Ruta.Piloto,StringComparison.OrdinalIgnoreCase)});
                }
                using(var cmd=Comando(cn,tx,"SELECT DISTINCT TOP (200) CENTRO_DIST Codigo FROM "+apk+".dbo.RT_RUTAS WHERE NULLIF(LTRIM(RTRIM(CENTRO_DIST)),N'') IS NOT NULL ORDER BY CENTRO_DIST;"))
                using(var r=cmd.ExecuteReader()) while(r.Read()) {var codigo=Texto(r,"Codigo");modelo.Centros.Add(new PilotoAdminOpcion{Codigo=codigo,Nombre=codigo,Activo=true,Seleccionado=string.Equals(codigo,modelo.Ruta.Centro,StringComparison.OrdinalIgnoreCase)});}
                using(var cmd=Comando(cn,tx,"SELECT TOP (500) PLACA,ESTADO,MARCA,LINEA,TIPO,EMPRESA FROM "+apk+".dbo.RT_VEHICULOS WHERE ESTADO=1 OR PLACA=@actual ORDER BY PLACA;"))
                {
                    Param(cmd,"@actual",SqlDbType.NVarChar,modelo.Ruta.Placa,15);
                    using(var r=cmd.ExecuteReader()) while(r.Read()) {var placa=Texto(r,"PLACA");modelo.Vehiculos.Add(new PilotoAdminOpcion{Codigo=placa,Nombre=string.Join(" · ",new[]{Texto(r,"MARCA"),Texto(r,"LINEA"),Texto(r,"TIPO"),Texto(r,"EMPRESA")}.Where(x=>!string.IsNullOrWhiteSpace(x))),Activo=r["ESTADO"]!=DBNull.Value && Convert.ToBoolean(r["ESTADO"],CultureInfo.InvariantCulture),Seleccionado=string.Equals(placa,modelo.Ruta.Placa,StringComparison.OrdinalIgnoreCase)});}
                }
                using(var cmd=Comando(cn,tx,@"SELECT FechaUtc,Actor,Motivo,CONVERT(nvarchar(max),Antes) Antes,CONVERT(nvarchar(max),Despues) Despues
FROM dbo.PilotoRutaHistorial WHERE CatalogoRutas=@catalogo AND Ruta_Id=@id ORDER BY FechaUtc DESC,Id DESC;"))
                {
                    Param(cmd,"@catalogo",SqlDbType.NVarChar,apk.Substring(1,apk.Length-2).Replace("]]","]"),128);
                    Param(cmd,"@id",SqlDbType.NVarChar,id,15);
                    using(var r=cmd.ExecuteReader()) while(r.Read()) modelo.Historial.Add(new PilotoAdminRutaCambio{FechaUtc=(DateTime)r["FechaUtc"],Actor=Texto(r,"Actor"),Motivo=Texto(r,"Motivo"),Antes=Texto(r,"Antes"),Despues=Texto(r,"Despues")});
                }
                tx.Commit();return modelo;
            }
        }

        private static XElement EstadoRuta(PilotoRuta r)
        {
            return new XElement("ruta",new XAttribute("id",r.Id),new XElement("fecha",r.Fecha.ToString("o",CultureInfo.InvariantCulture)),
                new XElement("piloto",r.Piloto),new XElement("centro",r.Centro),new XElement("placa",r.Placa),
                new XElement("estado",r.Estado),new XElement("observacion",r.ObservacionGeneral));
        }

        public void GuardarRuta(string actor,PilotoAdminRutaGuardar modelo)
        {
            PilotoAdminRutaReglas.Validar(modelo);
            using(var cn=Abrir()) using(var tx=cn.BeginTransaction(IsolationLevel.Serializable))
            {
                Autorizar(cn,tx,actor);ExigirHistorialRutas(cn,tx);
                var antes=LeerRutaAdmin(cn,tx,modelo.RutaId,true);
                if(!antes.PuedeEditar) throw new PilotoException(409,"Solo se pueden editar rutas abiertas o en ruta que no estén liquidadas.");
                if(modelo.Version!=antes.Formulario.Version) throw new PilotoException(409,"La ruta cambió desde que abriste el formulario. Recarga y revisa los datos.");
                var fechaNueva=modelo.Fecha.Date==antes.Ruta.Fecha.Date ? antes.Ruta.Fecha : modelo.Fecha.Date.Add(antes.Ruta.Fecha.TimeOfDay);
                var cambioOperativo=fechaNueva!=antes.Ruta.Fecha || antes.Ruta.Piloto!=modelo.Piloto || antes.Ruta.Centro!=modelo.Centro || antes.Ruta.Placa!=modelo.Placa || antes.Ruta.Estado!=modelo.Estado;
                if(cambioOperativo)
                {
                    using(var cmd=Comando(cn,tx,"SELECT CASE WHEN OBJECT_ID(N'dbo.GaritaRutaEvento',N'U') IS NULL THEN 0 ELSE 1 END;"))
                    if((int)cmd.ExecuteScalar()==1) using(var salida=Comando(cn,tx,@"SELECT COUNT(*) FROM dbo.GaritaRutaEvento
WHERE CatalogoRutas=@catalogo AND Ruta_Id=@id AND Tipo=N'S';"))
                    {Param(salida,"@catalogo",SqlDbType.NVarChar,apk.Substring(1,apk.Length-2).Replace("]]","]"),128);Param(salida,"@id",SqlDbType.NVarChar,modelo.RutaId,15);
                     if((int)salida.ExecuteScalar()>0) throw new PilotoException(409,"Garita ya registró la salida. Solo se permite cambiar la observación general.");}
                    using(var cmd=Comando(cn,tx,"SELECT COUNT(*) FROM dbo.PilotoBorradorCliente WHERE Ruta_Id=@id;"))
                    {Param(cmd,"@id",SqlDbType.NVarChar,modelo.RutaId,15);if((int)cmd.ExecuteScalar()>0) throw new PilotoException(409,"Hay clientes guardados en POS. Completa o revisa la ruta antes de modificar su asignación.");}
                    using(var cmd=Comando(cn,tx,"SELECT COUNT(*) FROM "+apk+".dbo.RT_RUTAS_DET WHERE ID_RUTA=@id AND (MO_VISITO IS NOT NULL OR NULLIF(LTRIM(RTRIM(MO_ENTREGA)),N'') IS NOT NULL);"))
                    {Param(cmd,"@id",SqlDbType.NVarChar,modelo.RutaId,15);if((int)cmd.ExecuteScalar()>0) throw new PilotoException(409,"La ruta ya tiene resultados registrados. Solo se permite cambiar la observación general.");}
                }
                if(cambioOperativo)
                {
                using(var cmd=Comando(cn,tx,@"SELECT COUNT(*) FROM "+apk+@".dbo.RT_EMPLEADOS e WHERE e.NOMBRE=@piloto AND e.CARGO=N'PILOTO' AND e.ESTADO=1
AND NOT EXISTS(SELECT 1 FROM "+apk+@".dbo.RT_EMPLEADOS otro WHERE otro.CARGO=N'PILOTO' AND otro.NOMBRE=e.NOMBRE AND otro.ROWID<>e.ROWID);"))
                {Param(cmd,"@piloto",SqlDbType.NVarChar,modelo.Piloto,90);if((int)cmd.ExecuteScalar()!=1) throw new PilotoException(409,"El piloto no está activo o su nombre es ambiguo.");}
                using(var cmd=Comando(cn,tx,"SELECT COUNT(*) FROM "+apk+".dbo.RT_RUTAS WHERE CENTRO_DIST=@centro;"))
                {Param(cmd,"@centro",SqlDbType.NVarChar,modelo.Centro,15);if((int)cmd.ExecuteScalar()==0) throw new PilotoException(409,"El centro no está disponible.");}
                using(var cmd=Comando(cn,tx,"SELECT COUNT(*) FROM "+apk+".dbo.RT_VEHICULOS WHERE PLACA=@placa AND ESTADO=1;"))
                {Param(cmd,"@placa",SqlDbType.NVarChar,modelo.Placa,15);if((int)cmd.ExecuteScalar()!=1) throw new PilotoException(409,"El vehículo no está disponible.");}
                }
                if(!cambioOperativo && (antes.Ruta.ObservacionGeneral??"")==modelo.ObservacionGeneral)
                    throw new PilotoException(400,"No hay cambios para guardar.");
                using(var cmd=Comando(cn,tx,SqlActualizarRuta))
                {
                    Param(cmd,"@fecha",SqlDbType.DateTime,fechaNueva);Param(cmd,"@piloto",SqlDbType.NVarChar,modelo.Piloto,90);
                    Param(cmd,"@centro",SqlDbType.NVarChar,modelo.Centro,15);Param(cmd,"@placa",SqlDbType.NVarChar,modelo.Placa,15);
                    Param(cmd,"@estado",SqlDbType.NVarChar,modelo.Estado,1);Param(cmd,"@observacion",SqlDbType.NVarChar,modelo.ObservacionGeneral,500);
                    Param(cmd,"@id",SqlDbType.NVarChar,modelo.RutaId,15);
                    if(cmd.ExecuteNonQuery()!=1) throw new PilotoException(409,"No se pudo guardar la ruta. Revisa su estado actual.");
                }
                var despues=LeerRutaAdmin(cn,tx,modelo.RutaId,true);
                if(despues.Ruta.Fecha!=fechaNueva || despues.Ruta.Piloto!=modelo.Piloto || despues.Ruta.Centro!=modelo.Centro || despues.Ruta.Placa!=modelo.Placa || despues.Ruta.Estado!=modelo.Estado || (despues.Ruta.ObservacionGeneral??"")!=modelo.ObservacionGeneral || despues.Documentos!=antes.Documentos)
                    throw new PilotoException(409,"La ruta no quedó con los datos esperados. No se guardaron los cambios.");
                using(var cmd=Comando(cn,tx,@"INSERT dbo.PilotoRutaHistorial(CatalogoRutas,Ruta_Id,Actor,Motivo,Antes,Despues)
VALUES(@catalogo,@id,@actor,@motivo,@antes,@despues);"))
                {
                    Param(cmd,"@catalogo",SqlDbType.NVarChar,apk.Substring(1,apk.Length-2).Replace("]]","]"),128);
                    Param(cmd,"@id",SqlDbType.NVarChar,modelo.RutaId,15);Param(cmd,"@actor",SqlDbType.NVarChar,actor,128);
                    Param(cmd,"@motivo",SqlDbType.NVarChar,modelo.Motivo,250);
                    Param(cmd,"@antes",SqlDbType.Xml,EstadoRuta(antes.Ruta).ToString(SaveOptions.DisableFormatting));
                    Param(cmd,"@despues",SqlDbType.Xml,EstadoRuta(despues.Ruta).ToString(SaveOptions.DisableFormatting));
                    cmd.ExecuteNonQuery();
                }
                tx.Commit();
            }
        }
    }
}
