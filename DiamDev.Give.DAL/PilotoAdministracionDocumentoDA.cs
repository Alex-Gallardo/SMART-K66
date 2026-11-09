using System;
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
        private const string ColumnasDocumento="ROWID,ID_RUTA,TIPO,ID_EMPRESA,F_DOCTO,ID_DOCUMENTO,FEL_SERIE,FEL_NUM,MONTO_DOC,CLIENTE,AGENTE,NOM_DESPACHO,DIR_DESPACHO,NO_BULTOS,PREPARO,MULTIENTREGA,MO_VISITO,MO_ENTREGA,MO_HR_SALIDA,MO_HR_ENTRADA,MO_OBSER,MO_MOTIVO,U_Usuario_liquida,U_U_FechaLiquidacion,U_Observa_Liquida_Fac";

        private static PilotoAdminDocumentoGuardar LeerDocumento(SqlDataReader r)
        {
            return new PilotoAdminDocumentoGuardar {
                RowId=(int)r["ROWID"],RutaId=Texto(r,"ID_RUTA"),Tipo=Texto(r,"TIPO"),Empresa=Texto(r,"ID_EMPRESA"),
                FechaDocumento=r["F_DOCTO"]==DBNull.Value?(DateTime?)null:(DateTime)r["F_DOCTO"],Documento=Texto(r,"ID_DOCUMENTO"),
                FelSerie=Texto(r,"FEL_SERIE"),FelNumero=Texto(r,"FEL_NUM"),Monto=r["MONTO_DOC"]==DBNull.Value?(decimal?)null:Convert.ToDecimal(r["MONTO_DOC"],CultureInfo.InvariantCulture),
                Cliente=Texto(r,"CLIENTE"),Agente=Texto(r,"AGENTE"),NombreDespacho=Texto(r,"NOM_DESPACHO"),Direccion=Texto(r,"DIR_DESPACHO"),
                Bultos=Convert.ToDecimal(r["NO_BULTOS"],CultureInfo.InvariantCulture),Preparo=Texto(r,"PREPARO"),
                Multientrega=r["MULTIENTREGA"]==DBNull.Value?(bool?)null:Convert.ToBoolean(r["MULTIENTREGA"]),
                Visito=r["MO_VISITO"]==DBNull.Value?(bool?)null:Convert.ToBoolean(r["MO_VISITO"]),Entrega=Texto(r,"MO_ENTREGA"),
                HoraSalida=Hora(r,"MO_HR_SALIDA"),HoraEntrada=Hora(r,"MO_HR_ENTRADA"),Observacion=Texto(r,"MO_OBSER"),
                MotivoNoEntrega=Texto(r,"MO_MOTIVO"),UsuarioLiquida=Texto(r,"U_Usuario_liquida"),
                FechaLiquidacion=r["U_U_FechaLiquidacion"]==DBNull.Value?(DateTime?)null:(DateTime)r["U_U_FechaLiquidacion"],
                ObservacionLiquidacion=Texto(r,"U_Observa_Liquida_Fac")
            };
        }

        private PilotoAdminDocumentoGuardar LeerDocumento(SqlConnection cn,SqlTransaction tx,string rutaId,int rowId,bool bloquear)
        {
            using(var cmd=Comando(cn,tx,"SELECT "+ColumnasDocumento+" FROM "+apk+".dbo.RT_RUTAS_DET"+(bloquear?" WITH(UPDLOCK,HOLDLOCK)":"")+" WHERE ID_RUTA=@ruta AND ROWID=@row;"))
            {
                Param(cmd,"@ruta",SqlDbType.NVarChar,rutaId,15);Param(cmd,"@row",SqlDbType.Int,rowId);
                using(var r=cmd.ExecuteReader()) return r.Read()?LeerDocumento(r):null;
            }
        }

        private static XElement EstadoDocumento(PilotoAdminDocumentoGuardar d)
        {
            if(d==null) return new XElement("sinDocumento");
            return new XElement("documento",new XAttribute("rowid",d.RowId),new XElement("ruta",d.RutaId),new XElement("tipo",d.Tipo),new XElement("empresa",d.Empresa),
                new XElement("fecha",d.FechaDocumento),new XElement("numero",d.Documento),new XElement("serieFel",d.FelSerie),new XElement("numeroFel",d.FelNumero),
                new XElement("monto",d.Monto),new XElement("cliente",d.Cliente),new XElement("agente",d.Agente),new XElement("despacho",d.NombreDespacho),
                new XElement("direccion",d.Direccion),new XElement("bultos",d.Bultos),new XElement("preparo",d.Preparo),new XElement("multientrega",d.Multientrega),
                new XElement("visito",d.Visito),new XElement("entrega",d.Entrega),new XElement("salida",d.HoraSalida),new XElement("entrada",d.HoraEntrada),
                new XElement("observacion",d.Observacion),new XElement("motivo",d.MotivoNoEntrega),new XElement("liquido",d.UsuarioLiquida),
                new XElement("fechaLiquidacion",d.FechaLiquidacion),new XElement("observacionLiquidacion",d.ObservacionLiquidacion));
        }

        private void AuditarDocumento(SqlConnection cn,SqlTransaction tx,string actor,string ruta,string motivo,PilotoAdminDocumentoGuardar antes,PilotoAdminDocumentoGuardar despues)
        {
            using(var cmd=Comando(cn,tx,@"INSERT dbo.PilotoRutaHistorial(CatalogoRutas,Ruta_Id,Actor,Motivo,Antes,Despues)
VALUES(@catalogo,@ruta,@actor,@motivo,@antes,@despues);"))
            {
                Param(cmd,"@catalogo",SqlDbType.NVarChar,apk.Substring(1,apk.Length-2).Replace("]]","]"),128);
                Param(cmd,"@ruta",SqlDbType.NVarChar,ruta,15);Param(cmd,"@actor",SqlDbType.NVarChar,actor,128);
                Param(cmd,"@motivo",SqlDbType.NVarChar,motivo,250);
                Param(cmd,"@antes",SqlDbType.Xml,EstadoDocumento(antes).ToString(SaveOptions.DisableFormatting));
                Param(cmd,"@despues",SqlDbType.Xml,EstadoDocumento(despues).ToString(SaveOptions.DisableFormatting));
                cmd.ExecuteNonQuery();
            }
        }

        public PilotoAdminDocumentoEdicion ObtenerDocumento(string actor,string rutaId,int rowId)
        {
            if(string.IsNullOrWhiteSpace(rutaId) || rutaId.Length>15 || rowId<=0) throw new PilotoException(404,"Documento no disponible.");
            using(var cn=Abrir()) using(var tx=cn.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                Autorizar(cn,tx,actor);ExigirInstalacion(cn,tx);ExigirHistorialRutas(cn,tx);
                var ruta=LeerRutaAdmin(cn,tx,rutaId,false);
                var documento=LeerDocumento(cn,tx,rutaId,rowId,false);
                if(documento==null) throw new PilotoException(404,"Documento no disponible en esta ruta.");
                documento.Version=PilotoAdminDocumentoReglas.Version(documento,ruta.Formulario.Version);
                tx.Commit();
                return new PilotoAdminDocumentoEdicion{Formulario=documento,EstadoRuta=ruta.Ruta.Estado,RutaLiquidada=ruta.Liquidada};
            }
        }

        private static void ParametrosDocumento(SqlCommand cmd,PilotoAdminDocumentoGuardar d)
        {
            Param(cmd,"@ruta",SqlDbType.NVarChar,d.RutaId,15);Param(cmd,"@row",SqlDbType.Int,d.RowId);
            Param(cmd,"@fecha",SqlDbType.DateTime,d.FechaDocumento);
            Param(cmd,"@serie",SqlDbType.NVarChar,d.FelSerie,150);Param(cmd,"@numeroFel",SqlDbType.NVarChar,d.FelNumero,150);
            Param(cmd,"@monto",SqlDbType.Decimal,d.Monto);Param(cmd,"@cliente",SqlDbType.NVarChar,d.Cliente,350);
            Param(cmd,"@agente",SqlDbType.NVarChar,d.Agente,50);Param(cmd,"@despacho",SqlDbType.NVarChar,d.NombreDespacho,250);
            Param(cmd,"@direccion",SqlDbType.NVarChar,d.Direccion,350);Param(cmd,"@bultos",SqlDbType.Decimal,d.Bultos);
            Param(cmd,"@preparo",SqlDbType.NVarChar,d.Preparo,150);Param(cmd,"@multi",SqlDbType.Bit,d.Multientrega);
            Param(cmd,"@visito",SqlDbType.Bit,d.Visito);Param(cmd,"@entrega",SqlDbType.NVarChar,d.Entrega,50);
            Param(cmd,"@salida",SqlDbType.Time,d.HoraSalida);Param(cmd,"@entrada",SqlDbType.Time,d.HoraEntrada);
            Param(cmd,"@observacion",SqlDbType.NVarChar,d.Observacion,-1);Param(cmd,"@motivo",SqlDbType.NVarChar,d.MotivoNoEntrega,150);
            Param(cmd,"@observacionLiq",SqlDbType.NVarChar,d.ObservacionLiquidacion,-1);
        }

        private void SinBorradores(SqlConnection cn,SqlTransaction tx,string ruta)
        {
            using(var cmd=Comando(cn,tx,"SELECT COUNT(*) FROM dbo.PilotoBorradorCliente WITH(UPDLOCK,HOLDLOCK) WHERE Ruta_Id=@ruta;"))
            {Param(cmd,"@ruta",SqlDbType.NVarChar,ruta,15);if((int)cmd.ExecuteScalar()>0) throw new PilotoException(409,"Hay clientes guardados en POS. Completa o revisa esos borradores antes de cambiar documentos.");}
        }

        public int GuardarDocumento(string actor,PilotoAdminDocumentoGuardar d)
        {
            if(d==null || string.IsNullOrWhiteSpace(d.RutaId) || d.RutaId.Length>15 || d.RowId<=0) throw new PilotoException(400,"Selecciona un documento existente.");
            using(var cn=Abrir()) using(var tx=cn.BeginTransaction(IsolationLevel.Serializable))
            {
                Autorizar(cn,tx,actor);ExigirInstalacion(cn,tx);ExigirHistorialRutas(cn,tx);
                var ruta=LeerRutaAdmin(cn,tx,d.RutaId,true);
                PilotoAdminDocumentoReglas.Validar(d,ruta.Ruta.Estado=="C" || ruta.Ruta.Estado=="L" || ruta.Liquidada);
                SinBorradores(cn,tx,d.RutaId);
                var anterior=LeerDocumento(cn,tx,d.RutaId,d.RowId,true);
                if(anterior==null) throw new PilotoException(404,"Documento no disponible en esta ruta.");
                var version=PilotoAdminDocumentoReglas.Version(anterior,ruta.Formulario.Version);
                if(d.Version!=version) throw new PilotoException(409,"La ruta o el documento cambió. Recarga antes de guardar.");
                if(d.Tipo!=anterior.Tipo || d.Empresa!=anterior.Empresa || !string.Equals(d.Documento??"",anterior.Documento??"",StringComparison.Ordinal))
                    throw new PilotoException(409,"La identidad del documento no se puede cambiar.");
                d.UsuarioLiquida=anterior.UsuarioLiquida;
                d.FechaLiquidacion=anterior.FechaLiquidacion;
                if(XNode.DeepEquals(EstadoDocumento(anterior),EstadoDocumento(d))) throw new PilotoException(400,"No hay cambios en el documento.");
                var sql="UPDATE "+apk+@".dbo.RT_RUTAS_DET SET F_DOCTO=@fecha,FEL_SERIE=@serie,FEL_NUM=@numeroFel,MONTO_DOC=@monto,CLIENTE=@cliente,AGENTE=@agente,NOM_DESPACHO=@despacho,DIR_DESPACHO=@direccion,NO_BULTOS=@bultos,PREPARO=@preparo,MULTIENTREGA=@multi,MO_VISITO=@visito,MO_ENTREGA=@entrega,MO_HR_SALIDA=@salida,MO_HR_ENTRADA=@entrada,MO_OBSER=@observacion,MO_MOTIVO=@motivo,U_Observa_Liquida_Fac=@observacionLiq WHERE ID_RUTA=@ruta AND ROWID=@row; SELECT @@ROWCOUNT;";
                using(var cmd=Comando(cn,tx,sql))
                {
                    ParametrosDocumento(cmd,d);
                    var numero=Convert.ToInt32(cmd.ExecuteScalar(),CultureInfo.InvariantCulture);
                    if(numero!=1) throw new PilotoException(409,"No se guardó el documento.");
                }
                var despues=LeerDocumento(cn,tx,d.RutaId,d.RowId,true);
                if(despues==null || !XNode.DeepEquals(EstadoDocumento(despues),EstadoDocumento(d))) throw new PilotoException(409,"No se pudo verificar el documento. Se cancelaron los cambios.");
                AuditarDocumento(cn,tx,actor,d.RutaId,d.MotivoCambio,anterior,despues);
                tx.Commit();return d.RowId;
            }
        }

        public void EliminarDocumento(string actor,string rutaId,int rowId,string version,string motivo)
        {
            motivo=(motivo??"").Trim();
            if(string.IsNullOrWhiteSpace(rutaId) || rutaId.Length>15 || rowId<=0 || string.IsNullOrWhiteSpace(version) || motivo.Length<5 || motivo.Length>250)
                throw new PilotoException(400,"Indica el documento y el motivo de eliminación.");
            using(var cn=Abrir()) using(var tx=cn.BeginTransaction(IsolationLevel.Serializable))
            {
                Autorizar(cn,tx,actor);ExigirInstalacion(cn,tx);ExigirHistorialRutas(cn,tx);
                var ruta=LeerRutaAdmin(cn,tx,rutaId,true);
                SinBorradores(cn,tx,rutaId);
                var antes=LeerDocumento(cn,tx,rutaId,rowId,true);
                if(antes==null) throw new PilotoException(404,"Documento no disponible en esta ruta.");
                if(PilotoAdminDocumentoReglas.Version(antes,ruta.Formulario.Version)!=version) throw new PilotoException(409,"El documento cambió. Recarga antes de eliminar.");
                if(ruta.Documentos==1 && (ruta.Ruta.Estado=="C" || ruta.Ruta.Estado=="L" || ruta.Liquidada)) throw new PilotoException(409,"Una ruta cerrada o liquidada no puede quedar sin documentos.");
                using(var cmd=Comando(cn,tx,"DELETE FROM "+apk+".dbo.RT_RUTAS_DET WHERE ID_RUTA=@ruta AND ROWID=@row;"))
                {Param(cmd,"@ruta",SqlDbType.NVarChar,rutaId,15);Param(cmd,"@row",SqlDbType.Int,rowId);if(cmd.ExecuteNonQuery()!=1) throw new PilotoException(409,"No se eliminó el detalle de la ruta.");}
                // El documento original y sus tablas fuente no forman parte de esta operación.
                if(LeerDocumento(cn,tx,rutaId,rowId,false)!=null) throw new PilotoException(409,"El detalle todavía existe. Se canceló la eliminación.");
                AuditarDocumento(cn,tx,actor,rutaId,"Documento eliminado: "+motivo,antes,null);
                tx.Commit();
            }
        }
    }
}
