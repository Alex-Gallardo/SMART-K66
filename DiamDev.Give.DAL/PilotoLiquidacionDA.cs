using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using DiamDev.Give.Entities;

namespace DiamDev.Give.DAL
{
    public sealed class PilotoLiquidacionDA
    {
        private readonly string conexion,apk;
        public PilotoLiquidacionDA() { var baseRutas=new PilotoAdministracionDA();conexion=baseRutas.ConexionPanel;apk=baseRutas.CatalogoPanel; }
        private SqlConnection Abrir() { var cn=new SqlConnection(conexion);try { cn.Open();using(var c=Cmd(cn,null,"SET LOCK_TIMEOUT 3000;")) c.ExecuteNonQuery();return cn; } catch {cn.Dispose();throw;} }
        private static SqlCommand Cmd(SqlConnection cn,SqlTransaction tx,string sql) {return new SqlCommand(sql,cn,tx){CommandTimeout=30};}
        private static void P(SqlCommand c,string nombre,SqlDbType tipo,object valor,int largo=0) {var p=largo==0?c.Parameters.Add(nombre,tipo):c.Parameters.Add(nombre,tipo,largo);p.Value=valor??DBNull.Value;}
        private static string S(SqlDataReader r,string columna) {return r[columna]==DBNull.Value?null:Convert.ToString(r[columna],CultureInfo.InvariantCulture);}
        private static int N(SqlDataReader r,string columna) {return Convert.ToInt32(r[columna],CultureInfo.InvariantCulture);}
        private static bool B(SqlDataReader r,string columna) {return r[columna]!=DBNull.Value && Convert.ToBoolean(r[columna],CultureInfo.InvariantCulture);}
        private static DateTime? F(SqlDataReader r,string columna) {return r[columna]==DBNull.Value?(DateTime?)null:Convert.ToDateTime(r[columna],CultureInfo.InvariantCulture);}
        private static TimeSpan? H(SqlDataReader r,string columna) {return r[columna]==DBNull.Value?(TimeSpan?)null:(TimeSpan)r[columna];}
        private static void Autorizar(SqlConnection cn,SqlTransaction tx,string actor)
        {if(!PilotoPanelDA.Permiso(cn,tx,actor,PilotoLiquidacionReglas.Permiso)) throw new PilotoException(403,"Necesitas el permiso Pilotos.Liquidar para acceder a todas las rutas y liquidarlas.");}

        private string ResumenSql(string bloqueo="") {return @"SELECT r.ID_RUTA,r.FECHA_RUTA,r.STATUS,r.LIQUIDADO,r.PILOTO,r.PLACA,r.CENTRO_DIST,r.HR_SALIDA,r.HR_ENTRADA,r.USR_LIQ,r.FECHA_LIQ,
 d.TotalDocumentos,d.DocumentosLiquidados,d.DocumentosConResultado,d.DocumentosInconsistentes
FROM "+apk+@".dbo.RT_RUTAS r "+bloqueo+@"
OUTER APPLY (SELECT COUNT(*) TotalDocumentos,
 ISNULL(SUM(CASE WHEN NULLIF(LTRIM(RTRIM(d.U_Usuario_liquida)),N'') IS NOT NULL AND d.U_U_FechaLiquidacion IS NOT NULL THEN 1 ELSE 0 END),0) DocumentosLiquidados,
 ISNULL(SUM(CASE WHEN NULLIF(LTRIM(RTRIM(d.MO_ENTREGA)),N'') IS NOT NULL THEN 1 ELSE 0 END),0) DocumentosConResultado,
 ISNULL(SUM(CASE WHEN (NULLIF(LTRIM(RTRIM(d.U_Usuario_liquida)),N'') IS NULL AND d.U_U_FechaLiquidacion IS NOT NULL)
 OR (NULLIF(LTRIM(RTRIM(d.U_Usuario_liquida)),N'') IS NOT NULL AND d.U_U_FechaLiquidacion IS NULL) THEN 1 ELSE 0 END),0) DocumentosInconsistentes
 FROM "+apk+@".dbo.RT_RUTAS_DET d WHERE d.ID_RUTA=r.ID_RUTA) d";}
        private static PilotoLiquidacionRuta Ruta(SqlDataReader r)
        {
            return new PilotoLiquidacionRuta{Id=S(r,"ID_RUTA"),Fecha=(DateTime)r["FECHA_RUTA"],Estado=S(r,"STATUS"),Liquidada=B(r,"LIQUIDADO"),Piloto=S(r,"PILOTO"),Placa=S(r,"PLACA"),Centro=S(r,"CENTRO_DIST"),HoraSalida=H(r,"HR_SALIDA"),HoraEntrada=H(r,"HR_ENTRADA"),UsuarioLiquida=S(r,"USR_LIQ"),FechaLiquida=F(r,"FECHA_LIQ"),TotalDocumentos=N(r,"TotalDocumentos"),DocumentosLiquidados=N(r,"DocumentosLiquidados"),DocumentosConResultado=N(r,"DocumentosConResultado"),DocumentosInconsistentes=N(r,"DocumentosInconsistentes")};
        }
        private static string Pendientes="((r.STATUS=N'C' AND ISNULL(r.LIQUIDADO,0)=0) OR (@ver=1 AND r.STATUS IN(N'C',N'L') AND ISNULL(r.LIQUIDADO,0)=1))";
        internal string SqlContar {get{return "SELECT COUNT(*) FROM "+apk+".dbo.RT_RUTAS r WHERE r.FECHA_RUTA>=@desde AND r.FECHA_RUTA<@fin AND "+Pendientes+BuscarSql+";";}}
        internal string SqlListar {get{return @"SELECT * FROM ("+ResumenSql()+@") r WHERE r.FECHA_RUTA>=@desde AND r.FECHA_RUTA<@fin AND "+Pendientes+BuscarSql+@"
ORDER BY CASE WHEN r.STATUS=N'C' AND ISNULL(r.LIQUIDADO,0)=0 THEN 0 ELSE 1 END,r.FECHA_RUTA DESC,r.ID_RUTA DESC OFFSET @offset ROWS FETCH NEXT 50 ROWS ONLY;";}}
        private static void Filtros(SqlCommand c,PilotoLiquidacionFiltro f)
        {P(c,"@desde",SqlDbType.Date,f.FechaDesde);P(c,"@fin",SqlDbType.Date,f.FechaFin);P(c,"@buscar",SqlDbType.NVarChar,f.Buscar,80);P(c,"@ver",SqlDbType.Bit,f.VerLiquidadas);}
        private const string BuscarSql=@" AND (@buscar=N'' OR r.ID_RUTA LIKE N'%'+@buscar+N'%' OR r.PLACA LIKE N'%'+@buscar+N'%' OR r.PILOTO LIKE N'%'+@buscar+N'%' OR r.CENTRO_DIST LIKE N'%'+@buscar+N'%')";
        public PilotoLiquidacionLista Listar(string actor,PilotoLiquidacionFiltro filtro)
        {
            filtro=filtro??new PilotoLiquidacionFiltro();filtro.Validar(DateTime.UtcNow.AddHours(-6).Date);
            var m=new PilotoLiquidacionLista{Filtro=filtro};
            using(var cn=Abrir()) using(var tx=cn.BeginTransaction(IsolationLevel.ReadCommitted)) {
                Autorizar(cn,tx,actor);
                using(var c=Cmd(cn,tx,SqlContar))
                {Filtros(c,filtro);m.Total=Convert.ToInt32(c.ExecuteScalar(),CultureInfo.InvariantCulture);}
                if(filtro.Pagina>1 && (long)(filtro.Pagina-1)*50>=m.Total) throw new PilotoException(404,"Esa página ya no tiene rutas. Vuelve a buscar.");
                using(var c=Cmd(cn,tx,SqlListar))
                {Filtros(c,filtro);P(c,"@offset",SqlDbType.Int,(filtro.Pagina-1)*50);using(var r=c.ExecuteReader()) while(r.Read()) m.Rutas.Add(Ruta(r));}
                tx.Commit();return m;
            }
        }
        private PilotoLiquidacionRuta Leer(SqlConnection cn,SqlTransaction tx,string id,bool bloquear,bool documentos)
        {
            var sql="SELECT * FROM ("+ResumenSql(bloquear?"WITH(UPDLOCK,HOLDLOCK)":"")+") r WHERE r.ID_RUTA=@id AND ((r.STATUS=N'C' AND ISNULL(r.LIQUIDADO,0)=0) OR (r.STATUS IN(N'C',N'L') AND ISNULL(r.LIQUIDADO,0)=1));";
            PilotoLiquidacionRuta ruta;
            using(var c=Cmd(cn,tx,sql)) {P(c,"@id",SqlDbType.NVarChar,id,15);using(var r=c.ExecuteReader()) {if(!r.Read()) throw new PilotoException(404,"La ruta no está disponible para liquidación.");ruta=Ruta(r);}}
            if(documentos) using(var c=Cmd(cn,tx,@"SELECT ID_RUTA,ROWID,TIPO,ID_DOCUMENTO,CLIENTE,DIR_DESPACHO,NO_BULTOS,MO_ENTREGA,MO_MOTIVO,MO_OBSER,U_Usuario_liquida,U_U_FechaLiquidacion
FROM "+apk+@".dbo.RT_RUTAS_DET WHERE ID_RUTA=@id ORDER BY ROWID;"))
            {P(c,"@id",SqlDbType.NVarChar,id,15);using(var r=c.ExecuteReader()) while(r.Read()) ruta.Documentos.Add(new PilotoLiquidacionDocumento{RutaId=S(r,"ID_RUTA"),RowId=N(r,"ROWID"),Tipo=S(r,"TIPO"),Numero=S(r,"ID_DOCUMENTO"),Cliente=S(r,"CLIENTE"),Direccion=S(r,"DIR_DESPACHO"),Bultos=r["NO_BULTOS"]==DBNull.Value?0:Convert.ToDecimal(r["NO_BULTOS"],CultureInfo.InvariantCulture),Resultado=S(r,"MO_ENTREGA"),Motivo=S(r,"MO_MOTIVO"),Observacion=S(r,"MO_OBSER"),UsuarioLiquida=S(r,"U_Usuario_liquida"),FechaLiquida=F(r,"U_U_FechaLiquidacion")});}
            return ruta;
        }
        public PilotoLiquidacionRuta Detalle(string actor,string id)
        {
            id=PilotoLiquidacionReglas.Id(id);
            using(var cn=Abrir()) using(var tx=cn.BeginTransaction(IsolationLevel.ReadCommitted)) {Autorizar(cn,tx,actor);var ruta=Leer(cn,tx,id,false,true);tx.Commit();return ruta;}
        }
        private static DateTime Hora(SqlConnection cn,SqlTransaction tx)
        {using(var c=Cmd(cn,tx,"SELECT CONVERT(datetime2(0),DATEADD(HOUR,-6,SYSUTCDATETIME()));")) return (DateTime)c.ExecuteScalar();}
        internal string SqlLiquidarDocumento {get{return @"UPDATE "+apk+@".dbo.RT_RUTAS_DET SET U_Usuario_liquida=@actor,U_U_FechaLiquidacion=@fecha
WHERE ID_RUTA=@id AND ROWID=@rowid AND NULLIF(LTRIM(RTRIM(U_Usuario_liquida)),N'') IS NULL
AND U_U_FechaLiquidacion IS NULL AND NULLIF(LTRIM(RTRIM(MO_ENTREGA)),N'') IS NOT NULL; SELECT @@ROWCOUNT;";}}
        internal string SqlLiquidarTodos {get{return @"UPDATE "+apk+@".dbo.RT_RUTAS_DET SET U_Usuario_liquida=@actor,U_U_FechaLiquidacion=@fecha
WHERE ID_RUTA=@id AND NULLIF(LTRIM(RTRIM(U_Usuario_liquida)),N'') IS NULL
AND U_U_FechaLiquidacion IS NULL AND NULLIF(LTRIM(RTRIM(MO_ENTREGA)),N'') IS NOT NULL; SELECT @@ROWCOUNT;";}}
        internal string SqlLiquidarRuta {get{return @"UPDATE "+apk+@".dbo.RT_RUTAS SET STATUS=N'L',LIQUIDADO=1,USR_LIQ=@actor,FECHA_LIQ=@fecha
WHERE ID_RUTA=@id AND STATUS=N'C' AND ISNULL(LIQUIDADO,0)=0 AND HR_ENTRADA IS NOT NULL
AND EXISTS(SELECT 1 FROM "+apk+@".dbo.RT_RUTAS_DET WHERE ID_RUTA=@id)
AND NOT EXISTS(SELECT 1 FROM "+apk+@".dbo.RT_RUTAS_DET WHERE ID_RUTA=@id AND
 (NULLIF(LTRIM(RTRIM(U_Usuario_liquida)),N'') IS NULL OR U_U_FechaLiquidacion IS NULL OR NULLIF(LTRIM(RTRIM(MO_ENTREGA)),N'') IS NULL)); SELECT @@ROWCOUNT;";}}
        public int LiquidarDocumentos(string actor,string id,int? rowId)
        {
            id=PilotoLiquidacionReglas.Id(id);if(rowId.HasValue && rowId.Value<=0) throw new PilotoException(400,"Selecciona un documento válido.");
            using(var cn=Abrir()) using(var tx=cn.BeginTransaction(IsolationLevel.Serializable)) {
                Autorizar(cn,tx,actor);var ruta=Leer(cn,tx,id,true,false);PilotoLiquidacionReglas.ValidarDocumentos(ruta);
                if(!rowId.HasValue && ruta.DocumentosConResultado!=ruta.TotalDocumentos) throw new PilotoException(409,"Registra primero el resultado de todos los documentos para liquidarlos juntos.");
                var pendientes=ruta.TotalDocumentos-ruta.DocumentosLiquidados;
                if(pendientes==0) throw new PilotoException(409,"Todos los documentos ya están liquidados.");
                var hora=Hora(cn,tx);int actualizados;
                using(var c=Cmd(cn,tx,rowId.HasValue?SqlLiquidarDocumento:SqlLiquidarTodos)) {
                    P(c,"@id",SqlDbType.NVarChar,id,15);P(c,"@actor",SqlDbType.NVarChar,actor,50);P(c,"@fecha",SqlDbType.DateTime2,hora);
                    if(rowId.HasValue) P(c,"@rowid",SqlDbType.Int,rowId.Value);
                    actualizados=Convert.ToInt32(c.ExecuteScalar(),CultureInfo.InvariantCulture);
                }
                if(actualizados!=(rowId.HasValue?1:pendientes)) throw new PilotoException(409,"Los documentos cambiaron. Vuelve a revisar la ruta.");
                tx.Commit();return actualizados;
            }
        }
        public void LiquidarRuta(string actor,string id)
        {
            id=PilotoLiquidacionReglas.Id(id);
            using(var cn=Abrir()) using(var tx=cn.BeginTransaction(IsolationLevel.Serializable)) {
                Autorizar(cn,tx,actor);var ruta=Leer(cn,tx,id,true,false);PilotoLiquidacionReglas.ValidarRuta(ruta);
                using(var c=Cmd(cn,tx,SqlLiquidarRuta)) {P(c,"@id",SqlDbType.NVarChar,id,15);P(c,"@actor",SqlDbType.NVarChar,actor,50);P(c,"@fecha",SqlDbType.DateTime2,Hora(cn,tx));
                    if(Convert.ToInt32(c.ExecuteScalar(),CultureInfo.InvariantCulture)!=1) throw new PilotoException(409,"La ruta cambió. Vuelve a revisarla.");}
                tx.Commit();
            }
        }
        public PilotoLiquidacionReporte Reporte(string actor,string[] ids)
        {
            ids=PilotoLiquidacionReglas.Rutas(ids);var m=new PilotoLiquidacionReporte();
            using(var cn=Abrir()) using(var tx=cn.BeginTransaction(IsolationLevel.ReadCommitted)) {
                Autorizar(cn,tx,actor);foreach(var id in ids) m.Rutas.Add(Leer(cn,tx,id,false,true));tx.Commit();return m;
            }
        }
    }
}
