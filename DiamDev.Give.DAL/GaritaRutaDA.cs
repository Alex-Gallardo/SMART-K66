using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using DiamDev.Give.Entities;

namespace DiamDev.Give.DAL
{
    public sealed class GaritaRutaDA
    {
        internal const string SqlRegistrarSalida = @"INSERT dbo.GaritaRutaEvento(CatalogoRutas,Ruta_Id,Tipo,Solicitud,Usuario_Id,Actor,Centro_Dist,Piloto,Placa,Documentos,Bultos,Version,Precinto,Observacion)
OUTPUT INSERTED.Id
VALUES(@catalogo,@id,N'S',@solicitud,@usuario,@actor,@centro,@piloto,@placa,@documentos,@bultos,@version,@precinto,@observacion);";
        private readonly string conexion,apk,catalogo;
        public GaritaRutaDA()
        {
            var a=new PilotoAdministracionDA();conexion=a.ConexionPanel;apk=a.CatalogoPanel;
            catalogo=apk.Substring(1,apk.Length-2).Replace("]]","]");
        }
        private static SqlCommand Cmd(SqlConnection cn,SqlTransaction tx,string sql) { return new SqlCommand(sql,cn,tx){CommandTimeout=20}; }
        private static void P(SqlCommand c,string n,SqlDbType t,object v,int size=0) { var p=size==0?c.Parameters.Add(n,t):c.Parameters.Add(n,t,size);p.Value=v??DBNull.Value; }
        private static string S(SqlDataReader r,string col) { return r[col]==DBNull.Value?null:Convert.ToString(r[col],CultureInfo.InvariantCulture); }
        private SqlConnection Abrir()
        {
            var cn=new SqlConnection(conexion);
            try {cn.Open();using(var c=Cmd(cn,null,"SET LOCK_TIMEOUT 3000;")) c.ExecuteNonQuery();return cn;}
            catch {cn.Dispose();throw;}
        }
        private static void Instalada(SqlConnection cn,SqlTransaction tx)
        {
            using(var c=Cmd(cn,tx,"SELECT CASE WHEN OBJECT_ID(N'dbo.GaritaCentro',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.GaritaRutaEvento',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.GaritaSalidaImagen',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.PilotoEmpresa',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.PilotoConductorRuta',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.PilotoConductorHistorial',N'U') IS NOT NULL THEN 1 ELSE 0 END;"))
                if((int)c.ExecuteScalar()!=1) throw new PilotoException(503,"Falta instalar la migración de garita o transportistas en POS.");
        }
        private static long Autorizar(SqlConnection cn,SqlTransaction tx,string login,string permiso)
        {
            if(!PilotoPanelDA.Permiso(cn,tx,login,permiso)) throw new PilotoException(403,"Tu cuenta no tiene acceso a esta acción de garita.");
            using(var c=Cmd(cn,tx,"SELECT TOP (2) Usuario_Id FROM dbo.Usuario WHERE Login=@login AND Activo=1 AND Autenticar_Site=1;"))
            {P(c,"@login",SqlDbType.NVarChar,login,50);using(var r=c.ExecuteReader()) {if(!r.Read()) throw new PilotoException(403,"Cuenta no disponible.");var id=Convert.ToInt64(r[0]);if(r.Read()) throw new PilotoException(403,"Cuenta ambigua.");return id;}}
        }
        private GaritaRuta Leer(SqlConnection cn,SqlTransaction tx,long usuario,string id,bool bloqueo)
        {
            var hint=bloqueo?" WITH(UPDLOCK,HOLDLOCK)":"";
            GaritaRuta ruta;
            var sql=@"SELECT r.ID_RUTA,r.FECHA_RUTA,r.PILOTO,r.PLACA,r.CENTRO_DIST,r.STATUS,r.LIQUIDADO,r.OBSR_GENERAL
FROM "+apk+@".dbo.RT_RUTAS r"+hint+@" WHERE r.ID_RUTA=@id AND EXISTS
(SELECT 1 FROM dbo.GaritaCentro g WHERE g.Usuario_Id=@usuario AND g.Centro_Dist=r.CENTRO_DIST COLLATE DATABASE_DEFAULT);";
            using(var c=Cmd(cn,tx,sql))
            {P(c,"@id",SqlDbType.NVarChar,id,15);P(c,"@usuario",SqlDbType.BigInt,usuario);
                using(var r=c.ExecuteReader()) {if(!r.Read()) throw new PilotoException(404,"Ruta no encontrada en tu centro autorizado.");
                    ruta=new GaritaRuta{Id=S(r,"ID_RUTA"),Fecha=(DateTime)r["FECHA_RUTA"],Piloto=S(r,"PILOTO"),Placa=S(r,"PLACA"),Centro=S(r,"CENTRO_DIST"),Estado=(S(r,"STATUS")??"").Trim(),Liquidada=r["LIQUIDADO"]!=DBNull.Value && Convert.ToBoolean(r["LIQUIDADO"]),Observacion=S(r,"OBSR_GENERAL")};
                }
            }
            using(var c=Cmd(cn,tx,"SELECT TOP (2) MARCA,TIPO,LINEA,EMPRESA,ESTADO FROM "+apk+".dbo.RT_VEHICULOS WHERE PLACA=@placa;"))
            {P(c,"@placa",SqlDbType.NVarChar,ruta.Placa,15);using(var r=c.ExecuteReader()) if(r.Read()) {ruta.Transporte=S(r,"EMPRESA");ruta.Vehiculo=string.Join(" · ",new[]{S(r,"MARCA"),S(r,"LINEA"),S(r,"TIPO")}.Where(x=>!string.IsNullOrWhiteSpace(x)));ruta.VehiculoDisponible=r["ESTADO"]!=DBNull.Value && Convert.ToBoolean(r["ESTADO"]);if(r.Read()) {ruta.Vehiculo=null;ruta.VehiculoDisponible=false;}}}
            using(var c=Cmd(cn,tx,@"SELECT TOP (2) pe.Usuario_Id,cr.Nombre,cr.Documento FROM dbo.PilotoEmpresa pe
JOIN dbo.PilotoVinculo pv ON pv.Usuario_Id=pe.Usuario_Id
JOIN "+apk+@".dbo.RT_EMPLEADOS e ON e.ROWID=pv.Empleado_RowId AND e.NOMBRE=@piloto
LEFT JOIN dbo.PilotoConductorRuta cr ON cr.CatalogoRutas=@catalogo AND cr.Ruta_Id=@id AND cr.Usuario_Id=pe.Usuario_Id AND cr.Piloto=@piloto AND cr.Placa=@placa
 AND pe.Activo=1 AND pv.Activo=1 AND e.ESTADO=1
 AND EXISTS(SELECT 1 FROM dbo.Usuario u WHERE u.Usuario_Id=pe.Usuario_Id AND u.Activo=1 AND u.Autenticar_Site=1 AND u.Login=pe.Nit)
 AND EXISTS(SELECT 1 FROM dbo.PilotoCentro pc WHERE pc.Usuario_Id=pe.Usuario_Id AND pc.Centro_Dist=@centro)
 AND EXISTS(SELECT 1 FROM dbo.PilotoVehiculo v WHERE v.Usuario_Id=pe.Usuario_Id AND v.Placa=@placa AND v.Activo=1);"))
            {P(c,"@piloto",SqlDbType.NVarChar,ruta.Piloto,90);P(c,"@centro",SqlDbType.NVarChar,ruta.Centro,15);P(c,"@placa",SqlDbType.NVarChar,ruta.Placa,15);
             P(c,"@catalogo",SqlDbType.NVarChar,catalogo,128);P(c,"@id",SqlDbType.NVarChar,ruta.Id,15);
             using(var r=c.ExecuteReader()) if(r.Read()) {ruta.EsRutaEmpresa=true;ruta.ConductorReal=S(r,"Nombre");ruta.DocumentoConductor=S(r,"Documento");if(r.Read()) throw new PilotoException(409,"La ruta coincide con varias empresas. Consulta a Distribución.");}}
            using(var c=Cmd(cn,tx,@"SELECT TOP (10001) ROWID,TIPO,ID_EMPRESA,ID_DOCUMENTO,CLIENTE,DIR_DESPACHO,NO_BULTOS,MO_VISITO,MO_ENTREGA
FROM "+apk+".dbo.RT_RUTAS_DET"+hint+" WHERE ID_RUTA=@id ORDER BY ROWID;"))
            {P(c,"@id",SqlDbType.NVarChar,id,15);using(var r=c.ExecuteReader()) while(r.Read()) ruta.Documentos.Add(new PilotoDocumento{RowId=(int)r["ROWID"],Tipo=S(r,"TIPO"),Empresa=S(r,"ID_EMPRESA"),Documento=S(r,"ID_DOCUMENTO"),Cliente=S(r,"CLIENTE"),Direccion=S(r,"DIR_DESPACHO"),Bultos=r["NO_BULTOS"]==DBNull.Value?0:Convert.ToDecimal(r["NO_BULTOS"]),Visito=r["MO_VISITO"]==DBNull.Value?(bool?)null:Convert.ToBoolean(r["MO_VISITO"]),Entrega=S(r,"MO_ENTREGA")});}
            if(ruta.Documentos.Count>10000) throw new PilotoException(409,"Ruta demasiado grande para verificar en garita. Consulta a Distribución.");
            using(var c=Cmd(cn,tx,@"SELECT TOP (1) Tipo,FechaUtc,Actor,Precinto FROM dbo.GaritaRutaEvento
WHERE CatalogoRutas=@catalogo AND Ruta_Id=@id AND Tipo=N'S' ORDER BY Id DESC;"))
            {P(c,"@catalogo",SqlDbType.NVarChar,catalogo,128);P(c,"@id",SqlDbType.NVarChar,id,15);using(var r=c.ExecuteReader()) if(r.Read()) {ruta.SalidaUtc=(DateTime)r["FechaUtc"];ruta.GuardDecision=S(r,"Tipo");ruta.Agente=S(r,"Actor");ruta.Precinto=S(r,"Precinto");}}
            if(ruta.SalidaUtc.HasValue) using(var c=Cmd(cn,tx,@"SELECT COUNT(*) FROM dbo.GaritaSalidaImagen i
JOIN dbo.GaritaRutaEvento e ON e.Id=i.Evento_Id WHERE e.CatalogoRutas=@catalogo AND e.Ruta_Id=@id AND e.Tipo=N'S';"))
            {P(c,"@catalogo",SqlDbType.NVarChar,catalogo,128);P(c,"@id",SqlDbType.NVarChar,id,15);ruta.TieneFotoSalida=(int)c.ExecuteScalar()>0;}
            using(var c=Cmd(cn,tx,@"SELECT TOP (1) Tipo,Observacion FROM dbo.GaritaRutaEvento
WHERE CatalogoRutas=@catalogo AND Ruta_Id=@id AND Tipo IN(N'D',N'R') ORDER BY Id DESC;"))
            {P(c,"@catalogo",SqlDbType.NVarChar,catalogo,128);P(c,"@id",SqlDbType.NVarChar,id,15);using(var r=c.ExecuteReader()) if(r.Read() && S(r,"Tipo")=="D") ruta.Diferencia=S(r,"Observacion");}
            ruta.Version=GaritaReglas.Version(ruta);
            return ruta;
        }
        public GaritaRuta Buscar(string actor,string id)
        {
            id=GaritaReglas.Id(id);
            using(var cn=Abrir()) using(var tx=cn.BeginTransaction(IsolationLevel.ReadCommitted))
            {Instalada(cn,tx);var usuario=Autorizar(cn,tx,actor,GaritaReglas.Ver);var ruta=Leer(cn,tx,usuario,id,false);tx.Commit();return ruta;}
        }
        public GaritaRuta BuscarPorPlaca(string actor,string placa,int pagina)
        {
            placa=GaritaReglas.Placa(placa);
            if(pagina<1 || pagina>100000) throw new PilotoException(400,"Selecciona una página válida.");
            var modelo=new GaritaRuta{PlacaBusqueda=placa,Pagina=pagina,BusquedaRealizada=true};
            using(var cn=Abrir()) using(var tx=cn.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                Instalada(cn,tx);var usuario=Autorizar(cn,tx,actor,GaritaReglas.Ver);
                var origen=@" FROM "+apk+@".dbo.RT_RUTAS r";
                var filtro=@" WHERE r.PLACA=@placa AND r.STATUS=N'E' AND EXISTS
(SELECT 1 FROM dbo.GaritaCentro g WHERE g.Usuario_Id=@usuario AND g.Centro_Dist=r.CENTRO_DIST COLLATE DATABASE_DEFAULT)";
                using(var c=Cmd(cn,tx,"SELECT COUNT(*)"+origen+filtro))
                {P(c,"@placa",SqlDbType.NVarChar,placa,15);P(c,"@usuario",SqlDbType.BigInt,usuario);modelo.TotalRutas=(int)c.ExecuteScalar();}
                if(pagina>1 && (long)(pagina-1)*50>=modelo.TotalRutas) throw new PilotoException(404,"La página de rutas no existe. Vuelve a buscar la placa.");
                using(var c=Cmd(cn,tx,SqlBuscarPorPlaca(apk)))
                {P(c,"@placa",SqlDbType.NVarChar,placa,15);P(c,"@usuario",SqlDbType.BigInt,usuario);P(c,"@catalogo",SqlDbType.NVarChar,catalogo,128);P(c,"@offset",SqlDbType.Int,(pagina-1)*50);
                    using(var r=c.ExecuteReader()) while(r.Read()) modelo.Resultados.Add(new GaritaRutaResumen{Id=S(r,"ID_RUTA"),Fecha=(DateTime)r["FECHA_RUTA"],Placa=S(r,"PLACA"),Piloto=S(r,"PILOTO"),ConductorReal=S(r,"ConductorReal"),Centro=S(r,"CENTRO_DIST"),Documentos=Convert.ToInt32(r["Documentos"]),Clientes=Convert.ToInt32(r["Clientes"]),Bultos=Convert.ToDecimal(r["Bultos"]),SalidaRegistrada=Convert.ToInt32(r["SalidaRegistrada"])==1,DiferenciaPendiente=Convert.ToInt32(r["DiferenciaPendiente"])==1});}
                tx.Commit();return modelo;
            }
        }
        internal static string SqlBuscarPorPlaca(string catalogoRutas)
        {
            var origen=@" FROM "+catalogoRutas+@".dbo.RT_RUTAS r";
            var filtro=@" WHERE r.PLACA=@placa AND r.STATUS=N'E' AND EXISTS
(SELECT 1 FROM dbo.GaritaCentro g WHERE g.Usuario_Id=@usuario AND g.Centro_Dist=r.CENTRO_DIST COLLATE DATABASE_DEFAULT)";
            return @"SELECT r.ID_RUTA,r.FECHA_RUTA,r.PLACA,r.PILOTO,r.CENTRO_DIST,cr.Nombre ConductorReal,
ISNULL(c.Documentos,0) Documentos,ISNULL(c.Clientes,0) Clientes,ISNULL(c.Bultos,0) Bultos,
CASE WHEN s.Id IS NULL THEN 0 ELSE 1 END SalidaRegistrada,
CASE WHEN ultimo.Tipo=N'D' THEN 1 ELSE 0 END DiferenciaPendiente"+origen+@"
OUTER APPLY (SELECT COUNT(*) Documentos,COUNT(DISTINCT ISNULL(d.CLIENTE,N'')) Clientes,
ISNULL(SUM(ISNULL(d.NO_BULTOS,0)),0) Bultos FROM "+catalogoRutas+@".dbo.RT_RUTAS_DET d WHERE d.ID_RUTA=r.ID_RUTA) c
OUTER APPLY (SELECT TOP (1) e.Id FROM dbo.GaritaRutaEvento e WHERE e.CatalogoRutas=@catalogo AND e.Ruta_Id=r.ID_RUTA COLLATE DATABASE_DEFAULT AND e.Tipo=N'S') s
OUTER APPLY (SELECT TOP (1) e.Tipo FROM dbo.GaritaRutaEvento e WHERE e.CatalogoRutas=@catalogo AND e.Ruta_Id=r.ID_RUTA COLLATE DATABASE_DEFAULT AND e.Tipo IN(N'D',N'R') ORDER BY e.Id DESC) ultimo
OUTER APPLY (SELECT TOP (1) pc.Nombre FROM dbo.PilotoConductorRuta pc JOIN dbo.PilotoEmpresa pe ON pe.Usuario_Id=pc.Usuario_Id AND pe.Activo=1
 JOIN dbo.PilotoVinculo pv ON pv.Usuario_Id=pe.Usuario_Id AND pv.Activo=1
 JOIN "+catalogoRutas+@".dbo.RT_EMPLEADOS em ON em.ROWID=pv.Empleado_RowId AND em.NOMBRE=r.PILOTO AND em.ESTADO=1
 WHERE pc.CatalogoRutas=@catalogo AND pc.Ruta_Id=r.ID_RUTA COLLATE DATABASE_DEFAULT AND pc.Placa=r.PLACA COLLATE DATABASE_DEFAULT AND pc.Piloto=r.PILOTO COLLATE DATABASE_DEFAULT
 AND EXISTS(SELECT 1 FROM dbo.Usuario u WHERE u.Usuario_Id=pe.Usuario_Id AND u.Activo=1 AND u.Autenticar_Site=1 AND u.Login=pe.Nit)
 AND EXISTS(SELECT 1 FROM dbo.PilotoCentro c WHERE c.Usuario_Id=pe.Usuario_Id AND c.Centro_Dist=r.CENTRO_DIST COLLATE DATABASE_DEFAULT)
 AND EXISTS(SELECT 1 FROM dbo.PilotoVehiculo v WHERE v.Usuario_Id=pe.Usuario_Id AND v.Placa=r.PLACA COLLATE DATABASE_DEFAULT AND v.Activo=1)) cr
"+filtro+@" ORDER BY r.FECHA_RUTA DESC,r.ID_RUTA DESC OFFSET @offset ROWS FETCH NEXT 50 ROWS ONLY;";
        }
        public GaritaRuta Detalle(string actor,string placa,string id)
        {
            placa=GaritaReglas.Placa(placa);id=GaritaReglas.Id(id);
            var ruta=Buscar(actor,id);
            if(ruta.Estado!="E" || !string.Equals(ruta.Placa,placa,StringComparison.OrdinalIgnoreCase)) throw new PilotoException(404,"Ruta no disponible para esa placa.");
            ruta.PlacaBusqueda=placa;return ruta;
        }
        public GaritaFoto ObtenerFoto(string actor,string placa,string id)
        {
            placa=GaritaReglas.Placa(placa);id=GaritaReglas.Id(id);
            using(var cn=Abrir()) using(var tx=cn.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                Instalada(cn,tx);var usuario=Autorizar(cn,tx,actor,GaritaReglas.Ver);var ruta=Leer(cn,tx,usuario,id,false);
                if(!string.Equals(ruta.Placa,placa,StringComparison.OrdinalIgnoreCase) || !ruta.TieneFotoSalida) throw new PilotoException(404,"Foto no disponible.");
                using(var c=Cmd(cn,tx,@"SELECT TOP (1) i.ContentType,i.Contenido FROM dbo.GaritaSalidaImagen i
JOIN dbo.GaritaRutaEvento e ON e.Id=i.Evento_Id WHERE e.CatalogoRutas=@catalogo AND e.Ruta_Id=@id AND e.Tipo=N'S';"))
                {P(c,"@catalogo",SqlDbType.NVarChar,catalogo,128);P(c,"@id",SqlDbType.NVarChar,id,15);using(var r=c.ExecuteReader()) {if(!r.Read()) throw new PilotoException(404,"Foto no disponible.");var foto=new GaritaFoto{ContentType=S(r,"ContentType"),Contenido=(byte[])r["Contenido"]};r.Close();tx.Commit();return foto;}}
            }
        }
        public void Preparar(string actor)
        {
            using(var cn=Abrir()) using(var tx=cn.BeginTransaction(IsolationLevel.ReadCommitted))
            {Instalada(cn,tx);var usuario=Autorizar(cn,tx,actor,GaritaReglas.Ver);
                using(var c=Cmd(cn,tx,"SELECT COUNT(*) FROM dbo.GaritaCentro WHERE Usuario_Id=@usuario;"))
                {P(c,"@usuario",SqlDbType.BigInt,usuario);if((int)c.ExecuteScalar()==0) throw new PilotoException(403,"No tienes un centro de garita asignado. Solicita acceso a Distribución.");}
                tx.Commit();
            }
        }
        public void RegistrarSalida(string actor,GaritaRegistroSalida modelo)
        {
            if(modelo==null) throw new PilotoException(400,"Vuelve a abrir la ruta.");
            var id=GaritaReglas.Id(modelo.RutaId);
            using(var cn=Abrir()) using(var tx=cn.BeginTransaction(IsolationLevel.Serializable))
            {
                Instalada(cn,tx);Autorizar(cn,tx,actor,GaritaReglas.Ver);var usuario=Autorizar(cn,tx,actor,GaritaReglas.RegistrarSalida);
                var ruta=Leer(cn,tx,usuario,id,true);
                if(ruta.SalidaUtc.HasValue) {
                    using(var c=Cmd(cn,tx,"SELECT COUNT(*) FROM dbo.GaritaRutaEvento WHERE CatalogoRutas=@catalogo AND Ruta_Id=@id AND Tipo=N'S' AND Solicitud=@solicitud AND Usuario_Id=@usuario;"))
                    {P(c,"@catalogo",SqlDbType.NVarChar,catalogo,128);P(c,"@id",SqlDbType.NVarChar,id,15);P(c,"@solicitud",SqlDbType.UniqueIdentifier,modelo.Solicitud);P(c,"@usuario",SqlDbType.BigInt,usuario);if((int)c.ExecuteScalar()==1) {tx.Commit();return;}}
                    throw new PilotoException(409,"La salida ya fue registrada. Consulta a Distribución.");
                }
                GaritaReglas.ValidarSalida(modelo,ruta);
                if(!string.Equals(GaritaReglas.Placa(modelo.PlacaBusqueda),ruta.Placa,StringComparison.OrdinalIgnoreCase))
                    throw new PilotoException(409,"La placa cambió. Vuelve a buscarla.");
                using(var c=Cmd(cn,tx,@"SELECT COUNT(*) FROM "+apk+@".dbo.RT_EMPLEADOS e WHERE e.NOMBRE=@piloto AND e.CARGO=N'PILOTO' AND e.ESTADO=1
AND NOT EXISTS(SELECT 1 FROM "+apk+@".dbo.RT_EMPLEADOS otro WHERE otro.CARGO=N'PILOTO' AND otro.NOMBRE=e.NOMBRE AND otro.ROWID<>e.ROWID);"))
                {P(c,"@piloto",SqlDbType.NVarChar,ruta.Piloto,90);if((int)c.ExecuteScalar()!=1) throw new PilotoException(409,"El piloto no está activo o su asignación es ambigua. Consulta a Distribución.");}
                long evento;
                using(var c=Cmd(cn,tx,SqlRegistrarSalida))
                {EventoParametros(c,ruta,usuario,actor,modelo.Solicitud,null,modelo.Observacion);evento=Convert.ToInt64(c.ExecuteScalar());}
                if(modelo.FotoContenido!=null)
                {
                    byte[] hash;using(var sha=SHA256.Create()) hash=sha.ComputeHash(modelo.FotoContenido);
                    using(var c=Cmd(cn,tx,@"INSERT dbo.GaritaSalidaImagen(Evento_Id,Nombre,ContentType,Tamano,Contenido,HashSha256)
VALUES(@evento,@nombre,@tipo,@tamano,@contenido,@hash);"))
                    {P(c,"@evento",SqlDbType.BigInt,evento);P(c,"@nombre",SqlDbType.NVarChar,modelo.FotoNombre,255);P(c,"@tipo",SqlDbType.NVarChar,PilotoReglas.ValidarImagen(modelo.FotoNombre,modelo.FotoContenido),50);P(c,"@tamano",SqlDbType.Int,modelo.FotoContenido.Length);P(c,"@contenido",SqlDbType.VarBinary,modelo.FotoContenido,-1);P(c,"@hash",SqlDbType.Binary,hash,32);c.ExecuteNonQuery();}
                }
                tx.Commit();
            }
        }
        public void Reportar(string actor,GaritaDiferencia modelo)
        {
            if(modelo==null) throw new PilotoException(400,"Vuelve a abrir la ruta.");
            var id=GaritaReglas.Id(modelo.RutaId);var nota=GaritaReglas.Limpiar(modelo.Descripcion,250);
            if(nota.Length<10 || modelo.Solicitud==Guid.Empty) throw new PilotoException(400,"Describe la diferencia con al menos 10 caracteres.");
            using(var cn=Abrir()) using(var tx=cn.BeginTransaction(IsolationLevel.Serializable))
            {Instalada(cn,tx);Autorizar(cn,tx,actor,GaritaReglas.Ver);var usuario=Autorizar(cn,tx,actor,GaritaReglas.RegistrarSalida);var ruta=Leer(cn,tx,usuario,id,true);
                using(var repetida=Cmd(cn,tx,"SELECT COUNT(*) FROM dbo.GaritaRutaEvento WHERE Solicitud=@solicitud AND Tipo=N'D' AND Usuario_Id=@usuario AND CatalogoRutas=@catalogo AND Ruta_Id=@id;"))
                {P(repetida,"@solicitud",SqlDbType.UniqueIdentifier,modelo.Solicitud);P(repetida,"@usuario",SqlDbType.BigInt,usuario);P(repetida,"@catalogo",SqlDbType.NVarChar,catalogo,128);P(repetida,"@id",SqlDbType.NVarChar,id,15);
                 if((int)repetida.ExecuteScalar()==1) {tx.Commit();return;}}
                if(!ruta.PuedeReportarDiferencia || ruta.Version!=modelo.Version) throw new PilotoException(409,"La ruta cambió. Vuelve a revisarla.");
                using(var c=Cmd(cn,tx,@"INSERT dbo.GaritaRutaEvento(CatalogoRutas,Ruta_Id,Tipo,Solicitud,Usuario_Id,Actor,Centro_Dist,Piloto,Placa,Documentos,Bultos,Version,Observacion)
VALUES(@catalogo,@id,N'D',@solicitud,@usuario,@actor,@centro,@piloto,@placa,@documentos,@bultos,@version,@observacion);"))
                {EventoParametros(c,ruta,usuario,actor,modelo.Solicitud,null,nota);c.ExecuteNonQuery();}tx.Commit();}
        }
        private void EventoParametros(SqlCommand c,GaritaRuta r,long usuario,string actor,Guid solicitud,string precinto,string observacion)
        {
            P(c,"@catalogo",SqlDbType.NVarChar,catalogo,128);P(c,"@id",SqlDbType.NVarChar,r.Id,15);P(c,"@solicitud",SqlDbType.UniqueIdentifier,solicitud);
            P(c,"@usuario",SqlDbType.BigInt,usuario);P(c,"@actor",SqlDbType.NVarChar,actor,50);P(c,"@centro",SqlDbType.NVarChar,r.Centro,15);
            P(c,"@piloto",SqlDbType.NVarChar,r.Piloto,90);P(c,"@placa",SqlDbType.NVarChar,r.Placa,15);
            P(c,"@documentos",SqlDbType.Int,r.Documentos.Count);P(c,"@bultos",SqlDbType.Decimal,r.Bultos);
            P(c,"@version",SqlDbType.NVarChar,r.Version,44);P(c,"@precinto",SqlDbType.NVarChar,precinto,40);P(c,"@observacion",SqlDbType.NVarChar,observacion,250);
        }
        public GaritaAdministracion Administrar(string actor,string login)
        {
            login=GaritaReglas.Limpiar(login,50);
            var m=new GaritaAdministracion{Login=login};
            using(var cn=Abrir()) using(var tx=cn.BeginTransaction(IsolationLevel.ReadCommitted))
            {Instalada(cn,tx);Autorizar(cn,tx,actor,GaritaReglas.Administrar);
                using(var c=Cmd(cn,tx,"SELECT DISTINCT TOP (500) CENTRO_DIST FROM "+apk+".dbo.RT_RUTAS WHERE NULLIF(LTRIM(RTRIM(CENTRO_DIST)),N'') IS NOT NULL ORDER BY CENTRO_DIST;"))
                using(var r=c.ExecuteReader()) while(r.Read()) m.Disponibles.Add(S(r,"CENTRO_DIST"));
                if(login.Length>0) {
                    using(var c=Cmd(cn,tx,"SELECT TOP (2) Usuario_Id,Nombre FROM dbo.Usuario WHERE Login=@login AND Activo=1 AND Autenticar_Site=1;"))
                    {P(c,"@login",SqlDbType.NVarChar,login,50);using(var r=c.ExecuteReader()) {if(!r.Read()) throw new PilotoException(404,"No se encontró una cuenta POS activa con ese login.");m.UsuarioId=Convert.ToInt64(r["Usuario_Id"]);m.Nombre=S(r,"Nombre");if(r.Read()) throw new PilotoException(409,"El login está duplicado.");}}
                    using(var c=Cmd(cn,tx,"SELECT Centro_Dist FROM dbo.GaritaCentro WHERE Usuario_Id=@usuario ORDER BY Centro_Dist;"))
                    {P(c,"@usuario",SqlDbType.BigInt,m.UsuarioId);using(var r=c.ExecuteReader()) while(r.Read()) m.Centros.Add(S(r,"Centro_Dist"));}
                }
                using(var c=Cmd(cn,tx,@"SELECT TOP (50) d.Ruta_Id,d.Centro_Dist,d.Observacion,d.FechaUtc FROM dbo.GaritaRutaEvento d
WHERE d.CatalogoRutas=@catalogo AND d.Tipo=N'D' AND NOT EXISTS
(SELECT 1 FROM dbo.GaritaRutaEvento e WHERE e.CatalogoRutas=d.CatalogoRutas AND e.Ruta_Id=d.Ruta_Id AND e.Id>d.Id AND e.Tipo IN(N'R',N'S',N'D'))
ORDER BY d.Id DESC;"))
                {P(c,"@catalogo",SqlDbType.NVarChar,catalogo,128);using(var r=c.ExecuteReader()) while(r.Read()) m.Incidencias.Add(new GaritaIncidencia{RutaId=S(r,"Ruta_Id"),Centro=S(r,"Centro_Dist"),Descripcion=S(r,"Observacion"),FechaUtc=(DateTime)r["FechaUtc"]});}
                tx.Commit();return m;
            }
        }
        public void Centro(string actor,string login,string centro,bool asignar)
        {
            login=GaritaReglas.Limpiar(login,50);centro=GaritaReglas.Limpiar(centro,15);
            if(login.Length==0 || centro.Length==0) throw new PilotoException(400,"Indica el usuario y centro.");
            using(var cn=Abrir()) using(var tx=cn.BeginTransaction(IsolationLevel.Serializable))
            {Instalada(cn,tx);Autorizar(cn,tx,actor,GaritaReglas.Administrar);
                long usuario;
                using(var c=Cmd(cn,tx,"SELECT TOP (2) Usuario_Id FROM dbo.Usuario WHERE Login=@login AND Activo=1 AND Autenticar_Site=1;"))
                {P(c,"@login",SqlDbType.NVarChar,login,50);using(var r=c.ExecuteReader()) {if(!r.Read()) throw new PilotoException(404,"Cuenta no disponible.");usuario=Convert.ToInt64(r[0]);if(r.Read()) throw new PilotoException(409,"Login duplicado.");}}
                if(asignar) using(var c=Cmd(cn,tx,"SELECT COUNT(*) FROM "+apk+".dbo.RT_RUTAS WHERE CENTRO_DIST=@centro;"))
                {P(c,"@centro",SqlDbType.NVarChar,centro,15);if((int)c.ExecuteScalar()==0) throw new PilotoException(400,"El centro no existe en las rutas.");}
                using(var c=Cmd(cn,tx,asignar?@"IF NOT EXISTS(SELECT 1 FROM dbo.GaritaCentro WITH(UPDLOCK,HOLDLOCK) WHERE Usuario_Id=@usuario AND Centro_Dist=@centro)
INSERT dbo.GaritaCentro(Usuario_Id,Centro_Dist) VALUES(@usuario,@centro);":@"DELETE dbo.GaritaCentro WHERE Usuario_Id=@usuario AND Centro_Dist=@centro;"))
                {P(c,"@usuario",SqlDbType.BigInt,usuario);P(c,"@centro",SqlDbType.NVarChar,centro,15);c.ExecuteNonQuery();}tx.Commit();
            }
        }
        public void Resolver(string actor,string id,string nota)
        {
            id=GaritaReglas.Id(id);nota=GaritaReglas.Limpiar(nota,250);
            if(nota.Length<10) throw new PilotoException(400,"Describe la solución con al menos 10 caracteres.");
            using(var cn=Abrir()) using(var tx=cn.BeginTransaction(IsolationLevel.Serializable))
            {Instalada(cn,tx);var usuario=Autorizar(cn,tx,actor,GaritaReglas.Administrar);
                // El administrador puede revisar una ruta de cualquier centro; no se le concede acceso operativo a la garita.
                using(var c=Cmd(cn,tx,"SELECT TOP (1) STATUS,LIQUIDADO FROM "+apk+".dbo.RT_RUTAS WITH(UPDLOCK,HOLDLOCK) WHERE ID_RUTA=@id;"))
                {P(c,"@id",SqlDbType.NVarChar,id,15);using(var r=c.ExecuteReader()) {if(!r.Read()) throw new PilotoException(404,"Ruta no disponible.");
                    if(!new[]{"A","E"}.Contains((S(r,"STATUS")??"").Trim()) || r["LIQUIDADO"]!=DBNull.Value && Convert.ToBoolean(r["LIQUIDADO"])) throw new PilotoException(409,"Solo se resuelven diferencias de rutas abiertas o en ruta sin liquidar.");}}
                using(var c=Cmd(cn,tx,"SELECT COUNT(*) FROM dbo.GaritaRutaEvento WHERE CatalogoRutas=@catalogo AND Ruta_Id=@id AND Tipo=N'S';"))
                {P(c,"@catalogo",SqlDbType.NVarChar,catalogo,128);P(c,"@id",SqlDbType.NVarChar,id,15);if((int)c.ExecuteScalar()>0) throw new PilotoException(409,"La salida ya está registrada.");}
                using(var c=Cmd(cn,tx,@"SELECT TOP (1) Id FROM dbo.GaritaRutaEvento
WHERE CatalogoRutas=@catalogo AND Ruta_Id=@id AND Tipo=N'D' AND Id>
ISNULL((SELECT MAX(Id) FROM dbo.GaritaRutaEvento WHERE CatalogoRutas=@catalogo AND Ruta_Id=@id AND Tipo=N'R'),0) ORDER BY Id DESC;"))
                {P(c,"@catalogo",SqlDbType.NVarChar,catalogo,128);P(c,"@id",SqlDbType.NVarChar,id,15);
                    if(c.ExecuteScalar()==null) throw new PilotoException(409,"No hay una diferencia pendiente para esta ruta.");
                }
                using(var c=Cmd(cn,tx,@"INSERT dbo.GaritaRutaEvento(CatalogoRutas,Ruta_Id,Tipo,Solicitud,Usuario_Id,Actor,Centro_Dist,Piloto,Placa,Documentos,Bultos,Version,Observacion)
SELECT TOP (1) CatalogoRutas,Ruta_Id,N'R',@solicitud,@usuario,@actor,Centro_Dist,Piloto,Placa,Documentos,Bultos,Version,@nota
FROM dbo.GaritaRutaEvento WHERE CatalogoRutas=@catalogo AND Ruta_Id=@id AND Tipo=N'D' ORDER BY Id DESC;"))
                {P(c,"@solicitud",SqlDbType.UniqueIdentifier,Guid.NewGuid());P(c,"@usuario",SqlDbType.BigInt,usuario);P(c,"@actor",SqlDbType.NVarChar,actor,50);P(c,"@catalogo",SqlDbType.NVarChar,catalogo,128);P(c,"@id",SqlDbType.NVarChar,id,15);P(c,"@nota",SqlDbType.NVarChar,nota,250);c.ExecuteNonQuery();}tx.Commit();
            }
        }
    }
}
