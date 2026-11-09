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
        internal string SqlActualizarRuta { get { return @"UPDATE "+apk+@".dbo.RT_RUTAS SET FECHA_RUTA=@fecha,PILOTO=@piloto,CENTRO_DIST=@centro,PLACA=@placa,STATUS=@estado,OBSR_GENERAL=@observacion,
TIPO_RT=@tipo,AYUDANTE=@ayudante,AYUDANTE2=@ayudante2,DESPACHO=@despacho,REVISOR=@revisor,CUSTODIO=@custodio,TIPO_CUSTODIO=@tipoCustodio,
OBSERVACIONES=@observaciones,HR_ENCENDIDO=@encendido,HR_SALIDA=@salida,HR_ENTRADA=@entrada,PLANTA_DESTINO=@planta,
USR_MOD=@actor,FECHA_MOD=GETDATE()
WHERE ID_RUTA=@id;"; } }
        private static TimeSpan? Hora(SqlDataReader r,string nombre)
        { return r[nombre]==DBNull.Value ? (TimeSpan?)null : (TimeSpan)r[nombre]; }
        private static void ExigirHistorialRutas(SqlConnection cn,SqlTransaction tx)
        {
            using(var cmd=Comando(cn,tx,"SELECT CASE WHEN OBJECT_ID(N'dbo.PilotoRutaHistorial',N'U') IS NULL THEN 0 ELSE 1 END;"))
                if((int)cmd.ExecuteScalar()!=1) throw new PilotoException(503,"Falta instalar la migración 26 de auditoría de rutas en POS.");
        }

        private PilotoAdminRutaEdicion LeerRutaAdmin(SqlConnection cn,SqlTransaction tx,string id,bool bloquear)
        {
            var hint=bloquear ? " WITH(UPDLOCK,HOLDLOCK)" : "";
            var sql=@"SELECT r.ID_RUTA,r.FECHA_RUTA,r.PILOTO,r.CENTRO_DIST,r.PLACA,r.STATUS,r.OBSR_GENERAL,r.LIQUIDADO,
 r.TIPO_RT,r.AYUDANTE,r.AYUDANTE2,r.DESPACHO,r.REVISOR,r.CUSTODIO,r.TIPO_CUSTODIO,r.OBSERVACIONES,
 r.HR_ENCENDIDO,r.HR_SALIDA,r.HR_ENTRADA,r.PLANTA_DESTINO,r.FECHA_GEN,r.ID_USR,r.FECHA_MON,r.USR_MON,r.FECHA_CON,r.USR_CON,r.FECHA_LIQ,r.USR_LIQ,r.FECHA_MOD,r.USR_MOD,
 (SELECT COUNT(*) FROM "+apk+@".dbo.RT_RUTAS_DET d WHERE d.ID_RUTA=r.ID_RUTA) Documentos
FROM "+apk+".dbo.RT_RUTAS r"+hint+" WHERE r.ID_RUTA=@id;";
            using(var cmd=Comando(cn,tx,sql))
            {
                Param(cmd,"@id",SqlDbType.NVarChar,id,15);
                using(var r=cmd.ExecuteReader())
                {
                    if(!r.Read()) throw new PilotoException(404,"Ruta no disponible.");
                    var modelo=new PilotoAdminRutaEdicion {
                        Ruta=new PilotoRuta {Id=Texto(r,"ID_RUTA"),Fecha=(DateTime)r["FECHA_RUTA"],Piloto=Texto(r,"PILOTO"),Centro=Texto(r,"CENTRO_DIST"),Placa=Texto(r,"PLACA"),Estado=Texto(r,"STATUS"),ObservacionGeneral=Texto(r,"OBSR_GENERAL"),
                            TipoRuta=Texto(r,"TIPO_RT"),Ayudante=Texto(r,"AYUDANTE"),Ayudante2=Texto(r,"AYUDANTE2"),Despacho=Texto(r,"DESPACHO"),Revisor=Texto(r,"REVISOR"),Custodio=r["CUSTODIO"]!=DBNull.Value && Convert.ToBoolean(r["CUSTODIO"]),TipoCustodio=Texto(r,"TIPO_CUSTODIO"),Observaciones=Texto(r,"OBSERVACIONES"),
                            HoraEncendido=Hora(r,"HR_ENCENDIDO"),HoraSalida=Hora(r,"HR_SALIDA"),HoraEntrada=Hora(r,"HR_ENTRADA"),PlantaDestino=Texto(r,"PLANTA_DESTINO")},
                        Liquidada=r["LIQUIDADO"]!=DBNull.Value && Convert.ToBoolean(r["LIQUIDADO"],CultureInfo.InvariantCulture),Documentos=(int)r["Documentos"]
                    };
                    modelo.CamposTecnicos=string.Join(" · ",new[]{"Creada "+Texto(r,"FECHA_GEN"),"por "+Texto(r,"ID_USR"),"Cierre "+Texto(r,"FECHA_MON"),"por "+Texto(r,"USR_MON"),"Liquidación "+Texto(r,"FECHA_LIQ"),"por "+Texto(r,"USR_LIQ"),"Último cambio "+Texto(r,"FECHA_MOD"),"por "+Texto(r,"USR_MOD")});
                    modelo.Formulario=new PilotoAdminRutaGuardar {RutaId=modelo.Ruta.Id,Fecha=modelo.Ruta.Fecha,Piloto=modelo.Ruta.Piloto,Centro=modelo.Ruta.Centro,Placa=modelo.Ruta.Placa,Estado=modelo.Ruta.Estado,ObservacionGeneral=modelo.Ruta.ObservacionGeneral,
                        TipoRuta=modelo.Ruta.TipoRuta,Ayudante=modelo.Ruta.Ayudante,Ayudante2=modelo.Ruta.Ayudante2,Despacho=modelo.Ruta.Despacho,Revisor=modelo.Ruta.Revisor,Custodio=modelo.Ruta.Custodio,TipoCustodio=modelo.Ruta.TipoCustodio,
                        Observaciones=modelo.Ruta.Observaciones,HoraEncendido=modelo.Ruta.HoraEncendido,HoraSalida=modelo.Ruta.HoraSalida,HoraEntrada=modelo.Ruta.HoraEntrada,PlantaDestino=modelo.Ruta.PlantaDestino};
                    modelo.Formulario.Version=PilotoAdminRutaReglas.Version(modelo.Ruta,modelo.Liquidada,modelo.Documentos);
                    return modelo;
                }
            }
        }

        public PilotoAdminRutaEdicion ObtenerRuta(string actor,string id,int paginaDocumento=1,string buscarDocumento=null)
        {
            if(string.IsNullOrWhiteSpace(id) || id.Length>15) throw new PilotoException(404,"Ruta no disponible.");
            if(paginaDocumento<1 || paginaDocumento>100000) throw new PilotoException(400,"La página de documentos no es válida.");
            buscarDocumento=(buscarDocumento??"").Trim();if(buscarDocumento.Length>80) throw new PilotoException(400,"La búsqueda de documentos es demasiado larga.");
            using(var cn=Abrir()) using(var tx=cn.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                Autorizar(cn,tx,actor);ExigirInstalacion(cn,tx);ExigirHistorialRutas(cn,tx);
                var modelo=LeerRutaAdmin(cn,tx,id,false);
                var empleados=@"SELECT TOP (500) e.ROWID,e.NOMBRE,e.ESTADO FROM "+apk+@".dbo.RT_EMPLEADOS e
WHERE e.CARGO=N'PILOTO' AND (e.NOMBRE=@actual OR (e.ESTADO=1 AND NOT EXISTS(SELECT 1 FROM "+apk+@".dbo.RT_EMPLEADOS otro WHERE otro.CARGO=N'PILOTO' AND otro.NOMBRE=e.NOMBRE AND otro.ROWID<>e.ROWID)))
ORDER BY CASE WHEN e.NOMBRE=@actual THEN 0 WHEN EXISTS(SELECT 1 FROM dbo.PilotoVinculo pv JOIN dbo.PilotoEmpresa pe ON pe.Usuario_Id=pv.Usuario_Id AND pe.Activo=1 WHERE pv.Empleado_RowId=e.ROWID) THEN 1 ELSE 2 END,e.NOMBRE;";
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
                CargarConductor(cn,tx,modelo);
                var filtro=" WHERE ID_RUTA=@id AND (@buscar=N'' OR ID_DOCUMENTO LIKE N'%'+@buscar+N'%' OR CLIENTE LIKE N'%'+@buscar+N'%' OR DIR_DESPACHO LIKE N'%'+@buscar+N'%')";
                using(var cmd=Comando(cn,tx,"SELECT COUNT(*) FROM "+apk+".dbo.RT_RUTAS_DET"+filtro+";"))
                {Param(cmd,"@id",SqlDbType.NVarChar,id,15);Param(cmd,"@buscar",SqlDbType.NVarChar,buscarDocumento,80);modelo.TotalDocumentosFiltrados=(int)cmd.ExecuteScalar();}
                modelo.PaginaDocumentos=paginaDocumento;modelo.BuscarDocumento=buscarDocumento;
                using(var cmd=Comando(cn,tx,"SELECT "+ColumnasDocumento+" FROM "+apk+".dbo.RT_RUTAS_DET"+filtro+" ORDER BY ROWID OFFSET @offset ROWS FETCH NEXT 50 ROWS ONLY;"))
                {
                    Param(cmd,"@id",SqlDbType.NVarChar,id,15);
                    Param(cmd,"@buscar",SqlDbType.NVarChar,buscarDocumento,80);Param(cmd,"@offset",SqlDbType.Int,(paginaDocumento-1)*50);
                    using(var r=cmd.ExecuteReader()) while(r.Read()) modelo.Detalles.Add(LeerDocumento(r));
                }
                tx.Commit();return modelo;
            }
        }

        private void CargarConductor(SqlConnection cn,SqlTransaction tx,PilotoAdminRutaEdicion modelo)
        {
            var ruta=modelo.Ruta;
            using(var cmd=Comando(cn,tx,@"SELECT TOP (2) pe.Usuario_Id FROM dbo.PilotoEmpresa pe
JOIN dbo.PilotoVinculo pv ON pv.Usuario_Id=pe.Usuario_Id AND pv.Activo=1
JOIN dbo.Usuario u ON u.Usuario_Id=pe.Usuario_Id AND u.Activo=1 AND u.Autenticar_Site=1 AND u.Login=pe.Nit
JOIN dbo.PilotoEmpresaPlaca ep ON ep.Usuario_Id=pe.Usuario_Id AND ep.Placa=@placa
JOIN dbo.PilotoVehiculo v ON v.Usuario_Id=pe.Usuario_Id AND v.Placa=ep.Placa AND v.Activo=1
WHERE pe.Activo=1;"))
            {Param(cmd,"@placa",SqlDbType.NVarChar,ruta.Placa,15);
             using(var lector=cmd.ExecuteReader()) if(lector.Read()) {modelo.EsRutaEmpresa=true;modelo.EmpresaUsuarioId=Convert.ToInt64(lector[0]);if(lector.Read()) throw new PilotoException(409,"La ruta coincide con varias empresas. Revisa sus vínculos.");}}
            if(!modelo.EsRutaEmpresa) return;
            using(var cmd=Comando(cn,tx,@"SELECT Nombre,Documento,Version FROM dbo.PilotoConductorRuta
WHERE CatalogoRutas=@catalogo AND Ruta_Id=@ruta AND Usuario_Id=@usuario AND Placa=@placa AND Piloto=@piloto;"))
            {Param(cmd,"@catalogo",SqlDbType.NVarChar,apk.Substring(1,apk.Length-2).Replace("]]","]"),128);Param(cmd,"@ruta",SqlDbType.NVarChar,ruta.Id,15);
             Param(cmd,"@usuario",SqlDbType.BigInt,modelo.EmpresaUsuarioId);Param(cmd,"@placa",SqlDbType.NVarChar,ruta.Placa,15);Param(cmd,"@piloto",SqlDbType.NVarChar,ruta.Piloto,90);
             using(var lector=cmd.ExecuteReader()) if(lector.Read()) {modelo.ConductorReal=Texto(lector,"Nombre");modelo.DocumentoConductor=Texto(lector,"Documento");modelo.VersionConductor=Convert.ToString(lector["Version"],CultureInfo.InvariantCulture);}}
            using(var cmd=Comando(cn,tx,@"SELECT TOP (20) FechaUtc,Actor,Motivo,NombreAnterior,NombreNuevo FROM dbo.PilotoConductorHistorial
WHERE CatalogoRutas=@catalogo AND Ruta_Id=@ruta ORDER BY Id DESC;"))
            {Param(cmd,"@catalogo",SqlDbType.NVarChar,apk.Substring(1,apk.Length-2).Replace("]]","]"),128);Param(cmd,"@ruta",SqlDbType.NVarChar,ruta.Id,15);
             using(var lector=cmd.ExecuteReader()) while(lector.Read()) modelo.HistorialConductor.Add(new PilotoAdminRutaCambio{FechaUtc=(DateTime)lector["FechaUtc"],Actor=Texto(lector,"Actor"),Motivo=Texto(lector,"Motivo"),Antes=Texto(lector,"NombreAnterior"),Despues=Texto(lector,"NombreNuevo")});}
        }

        private static XElement EstadoRuta(PilotoRuta r)
        {
            return new XElement("ruta",new XAttribute("id",r.Id),new XElement("fecha",r.Fecha.ToString("o",CultureInfo.InvariantCulture)),
                new XElement("piloto",r.Piloto),new XElement("centro",r.Centro),new XElement("placa",r.Placa),
                new XElement("estado",r.Estado),new XElement("observacion",r.ObservacionGeneral),new XElement("tipo",r.TipoRuta),
                new XElement("ayudante",r.Ayudante),new XElement("ayudante2",r.Ayudante2),new XElement("despacho",r.Despacho),new XElement("revisor",r.Revisor),
                new XElement("custodio",r.Custodio),new XElement("tipoCustodio",r.TipoCustodio),new XElement("observaciones",r.Observaciones),
                new XElement("encendido",r.HoraEncendido),new XElement("salida",r.HoraSalida),new XElement("entrada",r.HoraEntrada),new XElement("planta",r.PlantaDestino));
        }

        public void GuardarConductor(string actor,PilotoConductorGuardar m)
        {
            if(m==null || string.IsNullOrWhiteSpace(m.RutaId) || m.RutaId.Length>15 || m.EmpresaUsuarioId<=0)
                throw new PilotoException(400,"Selecciona una ruta empresarial válida.");
            var nombre=PilotoEmpresaReglas.Conductor(m.Nombre);
            var documento=PilotoEmpresaReglas.Documento(m.Documento);
            var motivo=(m.Motivo??"").Trim();
            if(motivo.Length<5 || motivo.Length>250 || motivo.Any(char.IsControl)) throw new PilotoException(400,"Describe el motivo del cambio (5 a 250 caracteres).");
            using(var cn=Abrir()) using(var tx=cn.BeginTransaction(IsolationLevel.Serializable))
            {
                Autorizar(cn,tx,actor);ExigirInstalacion(cn,tx);ExigirHistorialRutas(cn,tx);
                var ruta=LeerRutaAdmin(cn,tx,m.RutaId,true);
                if(!ruta.PuedeEditar) throw new PilotoException(409,"La ruta ya no admite asignación de conductor.");
                CargarConductor(cn,tx,ruta);
                if(!ruta.EsRutaEmpresa || ruta.EmpresaUsuarioId!=m.EmpresaUsuarioId) throw new PilotoException(409,"La ruta ya no pertenece a esta empresa o placa.");
                if((ruta.VersionConductor??"")!=(m.Version??"")) throw new PilotoException(409,"El conductor cambió. Recarga la ruta antes de guardar.");
                if(ruta.ConductorReal==nombre && ruta.DocumentoConductor==documento) throw new PilotoException(400,"No hay cambios en el conductor.");
                // Una salida histórica sin conductor admite solo su primera asignación;
                // las comprobaciones siguientes impiden hacerlo después de actividad operativa.
                using(var cmd=Comando(cn,tx,"SELECT COUNT(*) FROM dbo.GaritaRutaEvento WHERE CatalogoRutas=@catalogo AND Ruta_Id=@ruta AND Tipo=N'S';"))
                {Param(cmd,"@catalogo",SqlDbType.NVarChar,apk.Substring(1,apk.Length-2).Replace("]]","]"),128);Param(cmd,"@ruta",SqlDbType.NVarChar,m.RutaId,15);
                 if(!PilotoEmpresaReglas.PuedeAsignarConductor((int)cmd.ExecuteScalar()>0,ruta.ConductorReal,ruta.DocumentoConductor))
                     throw new PilotoException(409,"Garita ya registró la salida. No cambies el conductor.");}
                using(var cmd=Comando(cn,tx,"SELECT COUNT(*) FROM dbo.PilotoBorradorCliente WHERE Ruta_Id=@ruta;"))
                {Param(cmd,"@ruta",SqlDbType.NVarChar,m.RutaId,15);if((int)cmd.ExecuteScalar()>0) throw new PilotoException(409,"Hay clientes guardados. No cambies el conductor.");}
                using(var cmd=Comando(cn,tx,"SELECT (SELECT COUNT(*) FROM dbo.PilotoDocumentoImagen WHERE Ruta_Id=@ruta)+(SELECT COUNT(*) FROM dbo.PilotoClienteImagen WHERE Ruta_Id=@ruta);"))
                {Param(cmd,"@ruta",SqlDbType.NVarChar,m.RutaId,15);if((int)cmd.ExecuteScalar()>0) throw new PilotoException(409,"La ruta ya tiene fotografías. No cambies el conductor.");}
                using(var cmd=Comando(cn,tx,"SELECT COUNT(*) FROM "+apk+".dbo.RT_RUTAS_DET WHERE ID_RUTA=@ruta AND (MO_VISITO IS NOT NULL OR NULLIF(LTRIM(RTRIM(MO_ENTREGA)),N'') IS NOT NULL);"))
                {Param(cmd,"@ruta",SqlDbType.NVarChar,m.RutaId,15);if((int)cmd.ExecuteScalar()>0) throw new PilotoException(409,"La ruta ya tiene resultados. No cambies el conductor.");}
                string anteriorNombre=null,anteriorDocumento=null;
                using(var anterior=Comando(cn,tx,"SELECT Nombre,Documento FROM dbo.PilotoConductorRuta WITH(UPDLOCK,HOLDLOCK) WHERE CatalogoRutas=@catalogo AND Ruta_Id=@ruta;"))
                {Param(anterior,"@catalogo",SqlDbType.NVarChar,apk.Substring(1,apk.Length-2).Replace("]]","]"),128);Param(anterior,"@ruta",SqlDbType.NVarChar,m.RutaId,15);
                 using(var lector=anterior.ExecuteReader()) if(lector.Read()) {anteriorNombre=Texto(lector,"Nombre");anteriorDocumento=Texto(lector,"Documento");}}
                using(var cmd=Comando(cn,tx,@"UPDATE dbo.PilotoConductorRuta SET Usuario_Id=@usuario,Placa=@placa,Piloto=@piloto,Nombre=@nombre,Documento=@documento,Version=NEWID(),ModificadoUtc=SYSUTCDATETIME(),ModificadoPor=@actor
WHERE CatalogoRutas=@catalogo AND Ruta_Id=@ruta;
IF @@ROWCOUNT=0 INSERT dbo.PilotoConductorRuta(CatalogoRutas,Ruta_Id,Usuario_Id,Placa,Piloto,Nombre,Documento,ModificadoPor)
VALUES(@catalogo,@ruta,@usuario,@placa,@piloto,@nombre,@documento,@actor);"))
                {Param(cmd,"@catalogo",SqlDbType.NVarChar,apk.Substring(1,apk.Length-2).Replace("]]","]"),128);Param(cmd,"@ruta",SqlDbType.NVarChar,m.RutaId,15);Param(cmd,"@usuario",SqlDbType.BigInt,ruta.EmpresaUsuarioId);
                 Param(cmd,"@placa",SqlDbType.NVarChar,ruta.Ruta.Placa,15);Param(cmd,"@piloto",SqlDbType.NVarChar,ruta.Ruta.Piloto,90);Param(cmd,"@nombre",SqlDbType.NVarChar,nombre,120);Param(cmd,"@documento",SqlDbType.NVarChar,documento,30);Param(cmd,"@actor",SqlDbType.NVarChar,actor,128);cmd.ExecuteNonQuery();}
                using(var cmd=Comando(cn,tx,@"INSERT dbo.PilotoConductorHistorial(CatalogoRutas,Ruta_Id,Usuario_Id,Placa,NombreAnterior,DocumentoAnterior,NombreNuevo,DocumentoNuevo,Actor,Motivo)
VALUES(@catalogo,@ruta,@usuario,@placa,@anterior,@docAnterior,@nombre,@documento,@actor,@motivo);"))
                {Param(cmd,"@catalogo",SqlDbType.NVarChar,apk.Substring(1,apk.Length-2).Replace("]]","]"),128);Param(cmd,"@ruta",SqlDbType.NVarChar,m.RutaId,15);Param(cmd,"@usuario",SqlDbType.BigInt,ruta.EmpresaUsuarioId);
                 Param(cmd,"@placa",SqlDbType.NVarChar,ruta.Ruta.Placa,15);Param(cmd,"@anterior",SqlDbType.NVarChar,anteriorNombre,120);Param(cmd,"@docAnterior",SqlDbType.NVarChar,anteriorDocumento,30);
                 Param(cmd,"@nombre",SqlDbType.NVarChar,nombre,120);Param(cmd,"@documento",SqlDbType.NVarChar,documento,30);Param(cmd,"@actor",SqlDbType.NVarChar,actor,128);Param(cmd,"@motivo",SqlDbType.NVarChar,motivo,250);cmd.ExecuteNonQuery();}
                tx.Commit();
            }
        }

        public void GuardarRuta(string actor,PilotoAdminRutaGuardar modelo)
        {
            PilotoAdminRutaReglas.Validar(modelo);
            using(var cn=Abrir()) using(var tx=cn.BeginTransaction(IsolationLevel.Serializable))
            {
                Autorizar(cn,tx,actor);ExigirInstalacion(cn,tx);ExigirHistorialRutas(cn,tx);
                var antes=LeerRutaAdmin(cn,tx,modelo.RutaId,true);
                if(!antes.PuedeEditar) throw new PilotoException(409,"Esta ruta no admite edición administrativa.");
                if(modelo.Version!=antes.Formulario.Version) throw new PilotoException(409,"La ruta cambió desde que abriste el formulario. Recarga y revisa los datos.");
                if(antes.Ruta.Estado!=modelo.Estado && (antes.Liquidada || modelo.Estado=="C" || modelo.Estado=="L" || modelo.Estado=="X" || antes.Ruta.Estado=="C" || antes.Ruta.Estado=="L" || antes.Ruta.Estado=="X"))
                    throw new PilotoException(409,"El cierre, la anulación y la liquidación se realizan desde sus flujos específicos. Puedes corregir los demás datos de esta ruta.");
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
                    using(var cmd=Comando(cn,tx,"SELECT (SELECT COUNT(*) FROM dbo.PilotoDocumentoImagen WHERE Ruta_Id=@id)+(SELECT COUNT(*) FROM dbo.PilotoClienteImagen WHERE Ruta_Id=@id);"))
                    {Param(cmd,"@id",SqlDbType.NVarChar,modelo.RutaId,15);if((int)cmd.ExecuteScalar()>0) throw new PilotoException(409,"La ruta ya tiene fotografías POS. Solo se permite cambiar la observación general.");}
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
                var propuesta=new PilotoRuta {Id=antes.Ruta.Id,Fecha=fechaNueva,Piloto=modelo.Piloto,Centro=modelo.Centro,Placa=modelo.Placa,Estado=modelo.Estado,ObservacionGeneral=modelo.ObservacionGeneral,
                    TipoRuta=modelo.TipoRuta,Ayudante=modelo.Ayudante,Ayudante2=modelo.Ayudante2,Despacho=modelo.Despacho,Revisor=modelo.Revisor,Custodio=modelo.Custodio,TipoCustodio=modelo.TipoCustodio,
                    Observaciones=modelo.Observaciones,HoraEncendido=modelo.HoraEncendido,HoraSalida=modelo.HoraSalida,HoraEntrada=modelo.HoraEntrada,PlantaDestino=modelo.PlantaDestino};
                if(XNode.DeepEquals(EstadoRuta(antes.Ruta),EstadoRuta(propuesta)))
                    throw new PilotoException(400,"No hay cambios para guardar.");
                using(var cmd=Comando(cn,tx,SqlActualizarRuta))
                {
                    Param(cmd,"@fecha",SqlDbType.DateTime,fechaNueva);Param(cmd,"@piloto",SqlDbType.NVarChar,modelo.Piloto,90);
                    Param(cmd,"@centro",SqlDbType.NVarChar,modelo.Centro,15);Param(cmd,"@placa",SqlDbType.NVarChar,modelo.Placa,15);
                    Param(cmd,"@estado",SqlDbType.NVarChar,modelo.Estado,1);Param(cmd,"@observacion",SqlDbType.NVarChar,modelo.ObservacionGeneral,500);
                    Param(cmd,"@tipo",SqlDbType.NVarChar,modelo.TipoRuta,50);Param(cmd,"@ayudante",SqlDbType.NVarChar,modelo.Ayudante,90);
                    Param(cmd,"@ayudante2",SqlDbType.NVarChar,modelo.Ayudante2,90);Param(cmd,"@despacho",SqlDbType.NVarChar,modelo.Despacho,90);
                    Param(cmd,"@revisor",SqlDbType.NVarChar,modelo.Revisor,90);Param(cmd,"@custodio",SqlDbType.Bit,modelo.Custodio);
                    Param(cmd,"@tipoCustodio",SqlDbType.NVarChar,modelo.TipoCustodio,50);Param(cmd,"@observaciones",SqlDbType.NVarChar,modelo.Observaciones,-1);
                    Param(cmd,"@encendido",SqlDbType.Time,modelo.HoraEncendido);Param(cmd,"@salida",SqlDbType.Time,modelo.HoraSalida);
                    Param(cmd,"@entrada",SqlDbType.Time,modelo.HoraEntrada);Param(cmd,"@planta",SqlDbType.NVarChar,modelo.PlantaDestino,100);
                    Param(cmd,"@actor",SqlDbType.NVarChar,actor.Length<=15?actor:actor.Substring(0,15),15);
                    Param(cmd,"@id",SqlDbType.NVarChar,modelo.RutaId,15);
                    if(cmd.ExecuteNonQuery()!=1) throw new PilotoException(409,"No se pudo guardar la ruta. Revisa su estado actual.");
                }
                if(cambioOperativo)
                using(var cmd=Comando(cn,tx,@"INSERT dbo.PilotoConductorHistorial(CatalogoRutas,Ruta_Id,Usuario_Id,Placa,NombreAnterior,DocumentoAnterior,NombreNuevo,DocumentoNuevo,Actor,Motivo)
SELECT CatalogoRutas,Ruta_Id,Usuario_Id,Placa,Nombre,Documento,NULL,NULL,@actor,@motivo
FROM dbo.PilotoConductorRuta WHERE CatalogoRutas=@catalogo AND Ruta_Id=@id;
DELETE dbo.PilotoConductorRuta WHERE CatalogoRutas=@catalogo AND Ruta_Id=@id;"))
                {Param(cmd,"@catalogo",SqlDbType.NVarChar,apk.Substring(1,apk.Length-2).Replace("]]","]"),128);Param(cmd,"@id",SqlDbType.NVarChar,modelo.RutaId,15);
                 Param(cmd,"@actor",SqlDbType.NVarChar,actor,128);Param(cmd,"@motivo",SqlDbType.NVarChar,modelo.Motivo,250);cmd.ExecuteNonQuery();}
                var despues=LeerRutaAdmin(cn,tx,modelo.RutaId,true);
                if(!XNode.DeepEquals(EstadoRuta(despues.Ruta),EstadoRuta(propuesta)) || despues.Documentos!=antes.Documentos)
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
