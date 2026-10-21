using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using DiamDev.Give.Entities;

namespace DiamDev.Give.DAL
{
    public sealed class GaritaRutaDA
    {
        private readonly string conexion,apk,catalogo;
        internal string SqlConfirmarEstado { get { return @"UPDATE "+apk+@".dbo.RT_RUTAS SET STATUS=N'E'
WHERE ID_RUTA=@id AND STATUS=N'A' AND ISNULL(LIQUIDADO,0)=0;"; } }
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
            using(var c=Cmd(cn,tx,"SELECT CASE WHEN OBJECT_ID(N'dbo.GaritaCentro',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.GaritaRutaEvento',N'U') IS NOT NULL THEN 1 ELSE 0 END;"))
                if((int)c.ExecuteScalar()!=1) throw new PilotoException(503,"Falta instalar la migración de garita en POS.");
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
            using(var c=Cmd(cn,tx,@"SELECT TOP (10001) ROWID,TIPO,ID_EMPRESA,ID_DOCUMENTO,CLIENTE,DIR_DESPACHO,NO_BULTOS,MO_VISITO,MO_ENTREGA
FROM "+apk+".dbo.RT_RUTAS_DET"+hint+" WHERE ID_RUTA=@id ORDER BY ROWID;"))
            {P(c,"@id",SqlDbType.NVarChar,id,15);using(var r=c.ExecuteReader()) while(r.Read()) ruta.Documentos.Add(new PilotoDocumento{RowId=(int)r["ROWID"],Tipo=S(r,"TIPO"),Empresa=S(r,"ID_EMPRESA"),Documento=S(r,"ID_DOCUMENTO"),Cliente=S(r,"CLIENTE"),Direccion=S(r,"DIR_DESPACHO"),Bultos=r["NO_BULTOS"]==DBNull.Value?0:Convert.ToDecimal(r["NO_BULTOS"]),Visito=r["MO_VISITO"]==DBNull.Value?(bool?)null:Convert.ToBoolean(r["MO_VISITO"]),Entrega=S(r,"MO_ENTREGA")});}
            if(ruta.Documentos.Count>10000) throw new PilotoException(409,"Ruta demasiado grande para verificar en garita. Consulta a Distribución.");
            using(var c=Cmd(cn,tx,@"SELECT TOP (1) Tipo,FechaUtc,Actor,Precinto FROM dbo.GaritaRutaEvento
WHERE CatalogoRutas=@catalogo AND Ruta_Id=@id AND Tipo=N'S' ORDER BY Id DESC;"))
            {P(c,"@catalogo",SqlDbType.NVarChar,catalogo,128);P(c,"@id",SqlDbType.NVarChar,id,15);using(var r=c.ExecuteReader()) if(r.Read()) {ruta.SalidaUtc=(DateTime)r["FechaUtc"];ruta.GuardDecision=S(r,"Tipo");ruta.Agente=S(r,"Actor");ruta.Precinto=S(r,"Precinto");}}
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
        public void Preparar(string actor)
        {
            using(var cn=Abrir()) using(var tx=cn.BeginTransaction(IsolationLevel.ReadCommitted))
            {Instalada(cn,tx);var usuario=Autorizar(cn,tx,actor,GaritaReglas.Ver);
                using(var c=Cmd(cn,tx,"SELECT COUNT(*) FROM dbo.GaritaCentro WHERE Usuario_Id=@usuario;"))
                {P(c,"@usuario",SqlDbType.BigInt,usuario);if((int)c.ExecuteScalar()==0) throw new PilotoException(403,"No tienes un centro de garita asignado. Solicita acceso a Distribución.");}
                tx.Commit();
            }
        }
        public void Confirmar(string actor,GaritaConfirmacion modelo)
        {
            if(modelo==null) throw new PilotoException(400,"Vuelve a abrir la ruta.");
            var id=GaritaReglas.Id(modelo.RutaId);
            using(var cn=Abrir()) using(var tx=cn.BeginTransaction(IsolationLevel.Serializable))
            {
                Instalada(cn,tx);Autorizar(cn,tx,actor,GaritaReglas.Ver);var usuario=Autorizar(cn,tx,actor,GaritaReglas.Salir);
                var ruta=Leer(cn,tx,usuario,id,true);
                if(ruta.SalidaUtc.HasValue) {
                    using(var c=Cmd(cn,tx,"SELECT COUNT(*) FROM dbo.GaritaRutaEvento WHERE CatalogoRutas=@catalogo AND Ruta_Id=@id AND Tipo=N'S' AND Solicitud=@solicitud AND Usuario_Id=@usuario;"))
                    {P(c,"@catalogo",SqlDbType.NVarChar,catalogo,128);P(c,"@id",SqlDbType.NVarChar,id,15);P(c,"@solicitud",SqlDbType.UniqueIdentifier,modelo.Solicitud);P(c,"@usuario",SqlDbType.BigInt,usuario);if((int)c.ExecuteScalar()==1) {tx.Commit();return;}}
                    throw new PilotoException(409,"La salida ya fue registrada. Consulta a Distribución.");
                }
                GaritaReglas.Validar(modelo,ruta);
                if(string.IsNullOrWhiteSpace(ruta.Piloto) || string.IsNullOrWhiteSpace(ruta.Placa) || string.IsNullOrWhiteSpace(ruta.Centro) || !ruta.VehiculoDisponible)
                    throw new PilotoException(409,"Faltan datos de piloto, vehículo o centro. Consulta a Distribución.");
                using(var c=Cmd(cn,tx,@"SELECT COUNT(*) FROM "+apk+@".dbo.RT_EMPLEADOS e WHERE e.NOMBRE=@piloto AND e.CARGO=N'PILOTO' AND e.ESTADO=1
AND NOT EXISTS(SELECT 1 FROM "+apk+@".dbo.RT_EMPLEADOS otro WHERE otro.CARGO=N'PILOTO' AND otro.NOMBRE=e.NOMBRE AND otro.ROWID<>e.ROWID);"))
                {P(c,"@piloto",SqlDbType.NVarChar,ruta.Piloto,90);if((int)c.ExecuteScalar()!=1) throw new PilotoException(409,"El piloto no está activo o su asignación es ambigua. Consulta a Distribución.");}
                using(var c=Cmd(cn,tx,SqlConfirmarEstado))
                {P(c,"@id",SqlDbType.NVarChar,id,15);if(c.ExecuteNonQuery()!=1) throw new PilotoException(409,"La ruta cambió. Vuelve a consultarla.");}
                using(var c=Cmd(cn,tx,@"INSERT dbo.GaritaRutaEvento(CatalogoRutas,Ruta_Id,Tipo,Solicitud,Usuario_Id,Actor,Centro_Dist,Piloto,Placa,Documentos,Bultos,Version,Precinto,Observacion)
VALUES(@catalogo,@id,N'S',@solicitud,@usuario,@actor,@centro,@piloto,@placa,@documentos,@bultos,@version,@precinto,@observacion);"))
                {EventoParametros(c,ruta,usuario,actor,modelo.Solicitud,modelo.Precinto,modelo.Observacion);c.ExecuteNonQuery();}
                tx.Commit();
            }
        }
        public void Reportar(string actor,GaritaDiferencia modelo)
        {
            if(modelo==null) throw new PilotoException(400,"Vuelve a abrir la ruta.");
            var id=GaritaReglas.Id(modelo.RutaId);var nota=GaritaReglas.Limpiar(modelo.Descripcion,250);
            if(nota.Length<10 || modelo.Solicitud==Guid.Empty) throw new PilotoException(400,"Describe la diferencia con al menos 10 caracteres.");
            using(var cn=Abrir()) using(var tx=cn.BeginTransaction(IsolationLevel.Serializable))
            {Instalada(cn,tx);Autorizar(cn,tx,actor,GaritaReglas.Ver);var usuario=Autorizar(cn,tx,actor,GaritaReglas.Salir);var ruta=Leer(cn,tx,usuario,id,true);
                using(var repetida=Cmd(cn,tx,"SELECT COUNT(*) FROM dbo.GaritaRutaEvento WHERE Solicitud=@solicitud AND Tipo=N'D' AND Usuario_Id=@usuario AND CatalogoRutas=@catalogo AND Ruta_Id=@id;"))
                {P(repetida,"@solicitud",SqlDbType.UniqueIdentifier,modelo.Solicitud);P(repetida,"@usuario",SqlDbType.BigInt,usuario);P(repetida,"@catalogo",SqlDbType.NVarChar,catalogo,128);P(repetida,"@id",SqlDbType.NVarChar,id,15);
                 if((int)repetida.ExecuteScalar()==1) {tx.Commit();return;}}
                if(!ruta.PuedeSalir || ruta.SalidaUtc.HasValue || ruta.Version!=modelo.Version) throw new PilotoException(409,"La ruta cambió. Vuelve a revisarla.");
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
                    if(S(r,"STATUS")!="A" || r["LIQUIDADO"]!=DBNull.Value && Convert.ToBoolean(r["LIQUIDADO"])) throw new PilotoException(409,"Solo se resuelven diferencias de rutas abiertas y sin liquidar.");}}
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
