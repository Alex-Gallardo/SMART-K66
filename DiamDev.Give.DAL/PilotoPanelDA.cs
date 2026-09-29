using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using DiamDev.Give.Entities;

namespace DiamDev.Give.DAL
{
    // Consultas globales de distribución. No reutiliza ni suplanta el acceso operativo del piloto.
    public sealed class PilotoPanelDA
    {
        private readonly string conexion,apk;
        private bool eventosClientes;
        public PilotoPanelDA() { var admin=new PilotoAdministracionDA();conexion=admin.ConexionPanel;apk=admin.CatalogoPanel; }
        private static SqlCommand Cmd(SqlConnection cn,SqlTransaction tx,string sql) { return new SqlCommand(sql,cn,tx){CommandTimeout=20}; }
        private static void Param(SqlCommand c,string nombre,SqlDbType tipo,object valor,int largo=0) { var p=largo==0 ? c.Parameters.Add(nombre,tipo) : c.Parameters.Add(nombre,tipo,largo);p.Value=valor??DBNull.Value; }
        private static string Texto(SqlDataReader r,string col) { return r[col]==DBNull.Value ? null : Convert.ToString(r[col],CultureInfo.InvariantCulture); }
        private static bool Bit(SqlDataReader r,string col) { return r[col]!=DBNull.Value && Convert.ToBoolean(r[col],CultureInfo.InvariantCulture); }
        private static DateTime? Fecha(SqlDataReader r,string col) { return r[col]==DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r[col],CultureInfo.InvariantCulture); }
        internal static bool Permiso(SqlConnection cn,SqlTransaction tx,string login,string permiso)
        {
            if(string.IsNullOrWhiteSpace(login) || login.Length>50) throw new PilotoException(403,"Acceso no autorizado.");
            using(var c=Cmd(cn,tx,PilotoAdministracionDA.SqlAutorizacion)) {
                Param(c,"@login",SqlDbType.NVarChar,login,50);Param(c,"@permiso",SqlDbType.NVarChar,permiso,100);
                using(var r=c.ExecuteReader()) {
                    if(!r.Read()) throw new PilotoException(403,"La cuenta no está disponible.");
                    var activo=Bit(r,"Disponible");var tiene=Bit(r,"TienePermiso");
                    if(r.Read() || !activo) throw new PilotoException(403,"La cuenta debe ser única, estar activa y habilitada para el sitio.");
                    return tiene;
                }
            }
        }
        private static void Autorizar(SqlConnection cn,SqlTransaction tx,string login) { PilotoPanelReglas.ExigirAcceso(true,Permiso(cn,tx,login,PilotoPanelReglas.Permiso)); }
        internal const string SqlInstalacion=@"SELECT CASE WHEN
OBJECT_ID(N'dbo.PilotoVinculo',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.PilotoCentro',N'U') IS NOT NULL
AND OBJECT_ID(N'dbo.PilotoVehiculo',N'U') IS NOT NULL AND COL_LENGTH(N'dbo.PilotoBorradorCliente',N'Completado') IS NOT NULL
AND OBJECT_ID(N'dbo.PilotoDocumentoImagen',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.PilotoDocumentoImagenEvento',N'U') IS NOT NULL
AND OBJECT_ID(N'dbo.PilotoClienteImagen',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.PilotoClienteImagenEvento',N'U') IS NOT NULL
AND OBJECT_ID(N'dbo.PilotoCierre',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.PilotoVinculoHistorial',N'U') IS NOT NULL
AND OBJECT_ID(N'dbo.PilotoCentroHistorial',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.PilotoVehiculoHistorial',N'U') IS NOT NULL
THEN 1 ELSE 0 END;";
        internal string SqlRutas { get { return @"SELECT TOP (2001) r.ID_RUTA,r.FECHA_RUTA,r.STATUS,r.PILOTO,r.PLACA,r.CENTRO_DIST,r.LIQUIDADO,r.USR_MON,r.FECHA_MON,
CAST(CASE WHEN EXISTS(SELECT 1 FROM "+apk+@".dbo.RT_VEHICULOS v WHERE v.PLACA=r.PLACA AND v.ESTADO=1) THEN 1 ELSE 0 END AS bit) VehiculoActivo
INTO #PanelRutas FROM "+apk+@".dbo.RT_RUTAS r
WHERE (@id IS NOT NULL AND r.ID_RUTA=@id) OR (@id IS NULL AND r.FECHA_RUTA>=@desde AND r.FECHA_RUTA<@fin
AND (@estado=N'todos' OR r.STATUS=@estado) AND r.STATUS IN(N'A',N'E',N'C',N'X')
AND (@liquidacion=N'todas' OR @liquidacion=N'si' AND ISNULL(r.LIQUIDADO,0)=1 OR @liquidacion=N'no' AND ISNULL(r.LIQUIDADO,0)=0)
AND (@piloto=N'' OR r.PILOTO=@piloto) AND (@centro=N'' OR r.CENTRO_DIST=@centro) AND (@placa=N'' OR r.PLACA=@placa))
ORDER BY r.FECHA_RUTA DESC,r.ID_RUTA DESC;
CREATE UNIQUE CLUSTERED INDEX IX_PanelRutas ON #PanelRutas(ID_RUTA);
SELECT * FROM #PanelRutas ORDER BY CASE WHEN STATUS=N'E' THEN 0 ELSE 1 END,FECHA_RUTA DESC,ID_RUTA DESC;"; } }
        internal string SqlDocumentos { get { return @"SELECT TOP (100001) d.ID_RUTA,d.ROWID,d.TIPO,d.ID_EMPRESA,d.ID_DOCUMENTO,d.CLIENTE,d.DIR_DESPACHO,d.NO_BULTOS,
d.MO_VISITO,d.MO_ENTREGA,d.MO_MOTIVO,d.MO_OBSER,d.MO_HR_ENTRADA,d.MO_HR_SALIDA
FROM "+apk+@".dbo.RT_RUTAS_DET d JOIN #PanelRutas r ON r.ID_RUTA=d.ID_RUTA ORDER BY d.ID_RUTA,d.ROWID;"; } }
        internal string SqlPilotos { get { return @"SELECT TOP (5001) u.Usuario_Id,u.Login,u.Nombre,u.Activo,u.Autenticar_Site,p.Activo VinculoActivo,p.Codigo_Operador,e.NOMBRE Empleado,
CAST(CASE WHEN NOT EXISTS(SELECT 1 FROM dbo.Usuario otro WHERE otro.Login=u.Login AND otro.Usuario_Id<>u.Usuario_Id) THEN 1 ELSE 0 END AS bit) CuentaUnica,
CAST(CASE WHEN e.ESTADO=1 AND e.CARGO=N'PILOTO' THEN 1 ELSE 0 END AS bit) EmpleadoActivo,
CAST(CASE WHEN e.ROWID IS NOT NULL AND NOT EXISTS(SELECT 1 FROM "+apk+@".dbo.RT_EMPLEADOS otro WHERE otro.CARGO=N'PILOTO' AND otro.NOMBRE=e.NOMBRE AND otro.ROWID<>e.ROWID) THEN 1 ELSE 0 END AS bit) EmpleadoUnico,
CAST(CASE WHEN EXISTS(SELECT 1 FROM dbo.Usuario_Rol ur JOIN dbo.Rol rol ON rol.Rol_Id=ur.Rol_Id WHERE ur.Usuario_Id=u.Usuario_Id AND rol.Nombre=N'PILOTO') THEN 1 ELSE 0 END AS bit) RolPiloto,
CAST(CASE WHEN EXISTS(SELECT 1 FROM dbo.Usuario_Rol ur JOIN dbo.Rol_Permiso rp ON rp.Rol_Id=ur.Rol_Id WHERE ur.Usuario_Id=u.Usuario_Id AND rp.Permiso_Id=N'Pilotos.Rutas.Ver') THEN 1 ELSE 0 END AS bit) PermisoVer,
CAST(CASE WHEN EXISTS(SELECT 1 FROM dbo.Usuario_Rol ur JOIN dbo.Rol_Permiso rp ON rp.Rol_Id=ur.Rol_Id WHERE ur.Usuario_Id=u.Usuario_Id AND rp.Permiso_Id=N'Pilotos.Rutas.Confirmar') THEN 1 ELSE 0 END AS bit) PermisoConfirmar
FROM dbo.Usuario u LEFT JOIN dbo.PilotoVinculo p ON p.Usuario_Id=u.Usuario_Id LEFT JOIN "+apk+@".dbo.RT_EMPLEADOS e ON e.ROWID=p.Empleado_RowId
WHERE p.Usuario_Id IS NOT NULL OR EXISTS(SELECT 1 FROM dbo.Usuario_Rol ur JOIN dbo.Rol rol ON rol.Rol_Id=ur.Rol_Id WHERE ur.Usuario_Id=u.Usuario_Id AND rol.Nombre=N'PILOTO')
ORDER BY u.Nombre,u.Usuario_Id;
SELECT Usuario_Id,Centro_Dist FROM dbo.PilotoCentro;
SELECT pv.Usuario_Id,pv.Placa,CAST(CASE WHEN EXISTS(SELECT 1 FROM "+apk+@".dbo.RT_VEHICULOS v WHERE v.PLACA=pv.Placa COLLATE DATABASE_DEFAULT AND v.ESTADO=1) THEN 1 ELSE 0 END AS bit) Disponible FROM dbo.PilotoVehiculo pv WHERE pv.Activo=1;"; } }
        internal string SqlAsignaciones { get { return @"SELECT r.ID_RUTA,u.Usuario_Id FROM #PanelRutas r
JOIN "+apk+@".dbo.RT_EMPLEADOS e ON e.NOMBRE=r.PILOTO
JOIN dbo.PilotoVinculo p ON p.Empleado_RowId=e.ROWID JOIN dbo.Usuario u ON u.Usuario_Id=p.Usuario_Id
WHERE u.Activo=1 AND u.Autenticar_Site=1 AND p.Activo=1 AND NULLIF(LTRIM(RTRIM(p.Codigo_Operador)),N'') IS NOT NULL
AND NOT EXISTS(SELECT 1 FROM dbo.Usuario otro WHERE otro.Login=u.Login AND otro.Usuario_Id<>u.Usuario_Id)
AND e.ESTADO=1 AND e.CARGO=N'PILOTO' AND NULLIF(LTRIM(RTRIM(e.NOMBRE)),N'') IS NOT NULL
AND NOT EXISTS(SELECT 1 FROM "+apk+@".dbo.RT_EMPLEADOS otro WHERE otro.CARGO=N'PILOTO' AND otro.NOMBRE=e.NOMBRE AND otro.ROWID<>e.ROWID)
AND EXISTS(SELECT 1 FROM dbo.Usuario_Rol ur JOIN dbo.Rol rol ON rol.Rol_Id=ur.Rol_Id WHERE ur.Usuario_Id=u.Usuario_Id AND rol.Nombre=N'PILOTO')
AND EXISTS(SELECT 1 FROM dbo.Usuario_Rol ur JOIN dbo.Rol_Permiso rp ON rp.Rol_Id=ur.Rol_Id WHERE ur.Usuario_Id=u.Usuario_Id AND rp.Permiso_Id=N'Pilotos.Rutas.Ver')
AND EXISTS(SELECT 1 FROM dbo.PilotoCentro c WHERE c.Usuario_Id=u.Usuario_Id AND c.Centro_Dist=r.CENTRO_DIST COLLATE DATABASE_DEFAULT)
AND EXISTS(SELECT 1 FROM dbo.PilotoVehiculo v WHERE v.Usuario_Id=u.Usuario_Id AND v.Activo=1 AND v.Placa=r.PLACA COLLATE DATABASE_DEFAULT)
AND EXISTS(SELECT 1 FROM "+apk+@".dbo.RT_VEHICULOS v WHERE v.PLACA=r.PLACA AND v.ESTADO=1);"; } }
        internal string SqlArtefactos { get { return @"SELECT b.*,"+(eventosClientes ? @"CAST(CASE WHEN EXISTS(SELECT 1 FROM dbo.PilotoClienteEvento e WHERE e.CatalogoRutas=@catalogo AND e.Usuario_Id=b.Usuario_Id AND e.Ruta_Id=b.Ruta_Id AND e.Primer_RowId=b.Primer_RowId AND e.Version=b.Version) THEN 1 ELSE 0 END AS bit)"
            : "CAST(0 AS bit)")+@" OrigenVerificado FROM dbo.PilotoBorradorCliente b JOIN #PanelRutas r ON r.ID_RUTA COLLATE DATABASE_DEFAULT=b.Ruta_Id;
SELECT i.Ruta_Id,i.Detalle_RowId,i.Usuario_Id,i.FechaUtc FROM dbo.PilotoDocumentoImagen i JOIN #PanelRutas r ON r.ID_RUTA COLLATE DATABASE_DEFAULT=i.Ruta_Id;
SELECT i.Ruta_Id,i.Primer_RowId,i.Usuario_Id,i.FechaUtc FROM dbo.PilotoClienteImagen i JOIN #PanelRutas r ON r.ID_RUTA COLLATE DATABASE_DEFAULT=i.Ruta_Id;"; } }
        internal string SqlActividad { get { return @"WITH Eventos AS (
"+(eventosClientes ? @"SELECT N'Cliente' Tipo,e.Ruta_Id Ruta,e.Usuario_Id Usuario,CONVERT(nvarchar(128),e.Actor) Actor,N'Cliente guardado y bloqueado' Descripcion,e.FechaUtc,CONVERT(nvarchar(80),e.Id) Clave FROM dbo.PilotoClienteEvento e WHERE e.CatalogoRutas=@catalogo
UNION ALL SELECT N'Cliente',b.Ruta_Id,b.Usuario_Id,CONVERT(nvarchar(128),u.Login),N'Borrador histórico vigente en POS · sin catálogo de origen',b.FechaUtc,CONVERT(nvarchar(80),b.Primer_RowId)
FROM dbo.PilotoBorradorCliente b JOIN dbo.Usuario u ON u.Usuario_Id=b.Usuario_Id WHERE b.Completado=1 AND NOT EXISTS(SELECT 1 FROM dbo.PilotoClienteEvento e WHERE e.CatalogoRutas=@catalogo AND e.Ruta_Id=b.Ruta_Id AND e.Usuario_Id=b.Usuario_Id AND e.Primer_RowId=b.Primer_RowId AND e.Version=b.Version)"
: @"SELECT N'Cliente' Tipo,b.Ruta_Id Ruta,b.Usuario_Id Usuario,CONVERT(nvarchar(128),u.Login) Actor,N'Borrador histórico vigente en POS · sin catálogo de origen' Descripcion,b.FechaUtc,CONVERT(nvarchar(80),b.Primer_RowId) Clave
FROM dbo.PilotoBorradorCliente b JOIN dbo.Usuario u ON u.Usuario_Id=b.Usuario_Id WHERE b.Completado=1")+@"
UNION ALL SELECT N'Foto documento',e.Ruta_Id,e.Usuario_Id,CONVERT(nvarchar(128),u.Login),CONVERT(nvarchar(250),e.Accion),e.FechaUtc,CONVERT(nvarchar(80),e.Id) FROM dbo.PilotoDocumentoImagenEvento e JOIN dbo.Usuario u ON u.Usuario_Id=e.Usuario_Id
UNION ALL SELECT N'Foto cliente',e.Ruta_Id,e.Usuario_Id,CONVERT(nvarchar(128),u.Login),CONVERT(nvarchar(250),e.Accion),e.FechaUtc,CONVERT(nvarchar(80),e.Id) FROM dbo.PilotoClienteImagenEvento e JOIN dbo.Usuario u ON u.Usuario_Id=e.Usuario_Id
UNION ALL SELECT N'Cierre POS',e.Ruta_Id,e.Usuario_Id,CONVERT(nvarchar(128),e.Login),N'Registro de cierre en POS · origen histórico sin catálogo',e.FechaUtc,CONVERT(nvarchar(80),e.Solicitud) FROM dbo.PilotoCierre e
UNION ALL SELECT N'Vínculo',NULL,e.Usuario_Id,CONVERT(nvarchar(128),e.Actor),e.Motivo,e.FechaUtc,CONVERT(nvarchar(80),e.Id) FROM dbo.PilotoVinculoHistorial e
UNION ALL SELECT N'Centro',NULL,e.Usuario_Id,CONVERT(nvarchar(128),e.Actor),CONVERT(nvarchar(250),e.Centro_Dist+N' · '+e.Motivo),e.FechaUtc,CONVERT(nvarchar(80),e.Id) FROM dbo.PilotoCentroHistorial e
UNION ALL SELECT N'Vehículo',NULL,e.Usuario_Id,CONVERT(nvarchar(128),e.Actor),CONVERT(nvarchar(250),e.Placa+N' · '+e.Motivo),e.FechaUtc,CONVERT(nvarchar(80),e.Id) FROM dbo.PilotoVehiculoHistorial e
) SELECT TOP (5001) * FROM Eventos e WHERE e.FechaUtc>=@utcDesde AND e.FechaUtc<@utcFin
AND (@evento=N'todos' OR e.Tipo=@evento) AND (@usuario IS NULL OR e.Usuario=@usuario)
AND (EXISTS(SELECT 1 FROM #PanelRutas r WHERE r.ID_RUTA COLLATE DATABASE_DEFAULT=e.Ruta) OR e.Ruta IS NULL)
ORDER BY e.FechaUtc DESC,e.Tipo,e.Clave;"; } }
        public PilotoPanelModelo Leer(string login,PilotoPanelFiltro filtro,string id=null)
        {
            filtro=filtro??new PilotoPanelFiltro();filtro.Validar(PilotoPanelReglas.HoraGuatemala(DateTime.UtcNow).Date);
            if(id!=null && (string.IsNullOrWhiteSpace(id) || id.Length>15)) throw new PilotoException(404,"Ruta no disponible.");
            var modelo=new PilotoPanelModelo{Filtro=filtro,Catalogo=apk.Substring(1,apk.Length-2).Replace("]]","]"),ActualizadoUtc=DateTime.UtcNow};
            if(!PilotoRutaDA.CierreHabilitado) modelo.Avisos.Add("La operación del piloto está deshabilitada en la configuración: no se permiten nuevos guardados, fotografías ni cierres. El panel permanece disponible para consulta.");
            using(var cn=new SqlConnection(conexion)) {
                cn.Open();using(var c=Cmd(cn,null,"SET LOCK_TIMEOUT 3000;")) c.ExecuteNonQuery();
                using(var tx=cn.BeginTransaction(IsolationLevel.ReadCommitted)) {
                    Autorizar(cn,tx,login);modelo.PuedeAdministrar=Permiso(cn,tx,login,PilotoAdminReglas.PermisoAdministrar);
                    using(var c=Cmd(cn,tx,SqlInstalacion)) if((int)c.ExecuteScalar()!=1) throw new PilotoException(503,"Faltan objetos POS del módulo. Revisa las migraciones 18, 19, 20 y 22 antes de consultar el panel.");
                    using(var c=Cmd(cn,tx,"SELECT CASE WHEN OBJECT_ID(N'dbo.PilotoClienteEvento',N'U') IS NULL THEN 0 ELSE 1 END;")) eventosClientes=(int)c.ExecuteScalar()==1;
                    if(!eventosClientes) modelo.Avisos.Add("Falta instalar la migración 25 de eventos de cliente. Los guardados se observan desde el borrador vigente y desaparecerán del historial al cerrar la ruta.");
                    CargarPilotos(cn,tx,modelo);
                    using(var c=Cmd(cn,tx,SqlRutas)) {
                        Param(c,"@id",SqlDbType.NVarChar,id,15);Param(c,"@desde",SqlDbType.Date,filtro.Inicio);Param(c,"@fin",SqlDbType.Date,filtro.Fin);
                        Param(c,"@estado",SqlDbType.NVarChar,filtro.Estado,10);Param(c,"@liquidacion",SqlDbType.NVarChar,filtro.Liquidacion,10);
                        Param(c,"@piloto",SqlDbType.NVarChar,filtro.Piloto,90);Param(c,"@centro",SqlDbType.NVarChar,filtro.Centro,15);Param(c,"@placa",SqlDbType.NVarChar,filtro.Placa,15);
                        using(var r=c.ExecuteReader()) while(r.Read()) modelo.Rutas.Add(new PilotoPanelRuta { Liquidada=Bit(r,"LIQUIDADO"),VehiculoActivo=Bit(r,"VehiculoActivo"),OperadorCierre=Texto(r,"USR_MON"),FechaCierre=Fecha(r,"FECHA_MON"),Datos=new PilotoRuta { Id=Texto(r,"ID_RUTA"),Fecha=(DateTime)r["FECHA_RUTA"],Estado=Texto(r,"STATUS"),Piloto=Texto(r,"PILOTO"),Centro=Texto(r,"CENTRO_DIST"),Placa=Texto(r,"PLACA") } });
                    }
                    if(modelo.Rutas.Count>2000) throw new PilotoException(400,"Hay más de 2.000 rutas. Acota las fechas, el centro o el piloto; no se mostraron totales parciales.");
                    var rutas=modelo.Rutas.ToDictionary(r=>r.Datos.Id,StringComparer.OrdinalIgnoreCase);
                    var personas=modelo.Pilotos.ToDictionary(p=>p.Id);
                    using(var c=Cmd(cn,tx,SqlAsignaciones)) using(var r=c.ExecuteReader()) while(r.Read()) rutas[Texto(r,"ID_RUTA")].Pilotos.Add(personas[Convert.ToInt64(r["Usuario_Id"])]);
                    using(var c=Cmd(cn,tx,SqlDocumentos)) using(var r=c.ExecuteReader()) {
                        var n=0;while(r.Read()) {
                            if(++n>100000) throw new PilotoException(400,"Hay más de 100.000 documentos. Acota los filtros; no se mostraron totales parciales.");
                            var d=new PilotoDocumento {RowId=(int)r["ROWID"],Tipo=Texto(r,"TIPO"),Empresa=Texto(r,"ID_EMPRESA"),Documento=Texto(r,"ID_DOCUMENTO"),Cliente=Texto(r,"CLIENTE"),Direccion=Texto(r,"DIR_DESPACHO"),Bultos=r["NO_BULTOS"]==DBNull.Value ? 0 : Convert.ToDecimal(r["NO_BULTOS"]),Visito=r["MO_VISITO"]==DBNull.Value ? (bool?)null : Bit(r,"MO_VISITO"),Entrega=Texto(r,"MO_ENTREGA"),Motivo=Texto(r,"MO_MOTIVO"),Observaciones=Texto(r,"MO_OBSER"),Entrada=r["MO_HR_ENTRADA"]==DBNull.Value ? (TimeSpan?)null : (TimeSpan)r["MO_HR_ENTRADA"],Salida=r["MO_HR_SALIDA"]==DBNull.Value ? (TimeSpan?)null : (TimeSpan)r["MO_HR_SALIDA"]};
                            rutas[Texto(r,"ID_RUTA")].Datos.Documentos.Add(d);
                        }
                    }
                    foreach(var ruta in modelo.Rutas) {
                        ruta.Datos.Version=PilotoReglas.Version(ruta.Datos);
                        ruta.Clientes=ruta.Datos.Documentos.GroupBy(PilotoPanelReglas.Grupo).Select(g=>new PilotoPanelCliente {Primero=g.Min(d=>d.RowId),Nombre=g.First().Cliente,Direccion=g.First().Direccion,Documentos=g.Select(d=>new PilotoPanelDocumento {Ruta=ruta.Datos.Id,Datos=d,Fuente=modelo.Catalogo+" · resultado registrado"}).ToList()}).ToList();
                        foreach(var cliente in ruta.Clientes) cliente.Completado=ruta.Datos.Estado=="C" && cliente.Documentos.All(d=>d.Resultado!="PENDIENTE");
                        if(!ruta.VehiculoActivo) ruta.Alertas.Add("Vehículo inactivo o no disponible.");
                        if(ruta.Pilotos.Count==0 && ruta.Datos.Estado=="E") ruta.Alertas.Add("Ningún usuario POS tiene acceso operativo a esta combinación de empleado, centro y placa.");
                        if(ruta.Pilotos.Count>1) ruta.Alertas.Add("Más de un usuario coincide con la asignación. Revisar vínculos.");
                        if(ruta.Datos.Documentos.Count==0) ruta.Alertas.Add("Ruta sin documentos.");
                        if(ruta.Datos.Documentos.Count>PilotoReglas.MaxDocumentos) ruta.Alertas.Add("Supera 200 documentos; el cierre corresponde a Distribución.");
                    }
                    CargarArtefactos(cn,tx,modelo,rutas);
                    using(var c=Cmd(cn,tx,SqlActividad)) {
                        Param(c,"@utcDesde",SqlDbType.DateTime2,filtro.Inicio.AddHours(6));Param(c,"@utcFin",SqlDbType.DateTime2,filtro.Fin.AddHours(6));Param(c,"@evento",SqlDbType.NVarChar,filtro.Evento,30);Param(c,"@usuario",SqlDbType.BigInt,filtro.Usuario);
                        Param(c,"@catalogo",SqlDbType.NVarChar,modelo.Catalogo,128);
                        using(var r=c.ExecuteReader()) while(r.Read()) modelo.Actividad.Add(new PilotoPanelActividad {Tipo=Texto(r,"Tipo"),Ruta=Texto(r,"Ruta"),Usuario=Convert.ToInt64(r["Usuario"]),Actor=Texto(r,"Actor"),Descripcion=Texto(r,"Descripcion"),FechaUtc=(DateTime)r["FechaUtc"],Clave=Texto(r,"Clave")});
                    }
                    if(modelo.Actividad.Count>5000) throw new PilotoException(400,"Hay más de 5.000 eventos. Acota el período, usuario o tipo de evento; no se mostraron registros parciales.");
                    tx.Commit();
                }
            }
            foreach(var ruta in modelo.Rutas) {
                var ultima=modelo.Actividad.Where(a=>a.Ruta==ruta.Datos.Id && a.Tipo!="Cierre POS").Select(a=>(DateTime?)a.FechaUtc).Max();
                if(ultima.HasValue && (!ruta.ActividadUtc.HasValue || ruta.ActividadUtc<ultima)) ruta.ActividadUtc=ultima;
                if(ruta.ListaParaCierre) ruta.Alertas.Add("Todos los clientes están guardados; falta confirmar el cierre.");
                foreach(var p in ruta.Pilotos) p.Rutas++;
            }
            modelo.Centros=modelo.Rutas.Select(r=>r.Datos.Centro).Where(x=>!string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x=>x).ToList();
            modelo.Placas=modelo.Rutas.Select(r=>r.Datos.Placa).Where(x=>!string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x=>x).ToList();
            modelo.NombresPiloto=modelo.Pilotos.Select(p=>p.Empleado).Concat(modelo.Rutas.Select(r=>r.Datos.Piloto)).Where(x=>!string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x=>x).ToList();
            if(id!=null) { modelo.Detalle=modelo.Rutas.SingleOrDefault(r=>string.Equals(r.Datos.Id,id,StringComparison.OrdinalIgnoreCase));if(modelo.Detalle==null) throw new PilotoException(404,"Ruta no disponible en el catálogo configurado."); }
            else modelo.Rutas=modelo.Rutas.Where(r=>PilotoPanelReglas.Coincide(r,filtro)).ToList();
            modelo.Pilotos=modelo.Pilotos.Where(p=>(!filtro.Usuario.HasValue || p.Id==filtro.Usuario) && (string.IsNullOrEmpty(filtro.Piloto) || string.Equals(p.Empleado,filtro.Piloto,StringComparison.OrdinalIgnoreCase)) && (string.IsNullOrEmpty(filtro.Centro) || p.Centros.Contains(filtro.Centro,StringComparer.OrdinalIgnoreCase)) && (string.IsNullOrEmpty(filtro.Placa) || p.Placas.Contains(filtro.Placa,StringComparer.OrdinalIgnoreCase))).ToList();
            foreach(var p in modelo.Pilotos) p.Rutas=modelo.Rutas.Count(r=>r.Pilotos.Any(x=>x.Id==p.Id));
            var ids=new HashSet<string>(modelo.Rutas.Select(r=>r.Datos.Id),StringComparer.OrdinalIgnoreCase);var usuarios=new HashSet<long>(modelo.Pilotos.Select(p=>p.Id));
            modelo.Actividad=modelo.Actividad.Where(a=>a.Ruta==null ? id==null && usuarios.Contains(a.Usuario) && string.IsNullOrEmpty(filtro.Buscar) : ids.Contains(a.Ruta)).ToList();
            if(modelo.Actividad.Any(a=>a.Tipo=="Cierre POS")) modelo.Avisos.Add("La auditoría histórica de POS no registra el catálogo de origen. Un ID coincidente no demuestra un cierre de producción; verifica el estado y los resultados actuales en "+modelo.Catalogo+".");
            return modelo;
        }
        private void CargarPilotos(SqlConnection cn,SqlTransaction tx,PilotoPanelModelo modelo)
        {
            using(var c=Cmd(cn,tx,SqlPilotos)) using(var r=c.ExecuteReader()) {
                while(r.Read()) modelo.Pilotos.Add(new PilotoPanelPiloto {Id=Convert.ToInt64(r["Usuario_Id"]),Login=Texto(r,"Login"),Nombre=Texto(r,"Nombre"),Empleado=Texto(r,"Empleado"),Operador=Texto(r,"Codigo_Operador"),CuentaActiva=Bit(r,"Activo") && Bit(r,"Autenticar_Site") && Bit(r,"CuentaUnica"),RolPiloto=Bit(r,"RolPiloto"),PermisoVer=Bit(r,"PermisoVer"),PermisoConfirmar=Bit(r,"PermisoConfirmar"),VinculoActivo=Bit(r,"VinculoActivo"),EmpleadoActivo=Bit(r,"EmpleadoActivo"),EmpleadoUnico=Bit(r,"EmpleadoUnico")});
                if(modelo.Pilotos.Count>5000) throw new PilotoException(400,"La consulta supera 5.000 usuarios piloto. Requiere revisar el alcance de la instalación.");
                var personas=modelo.Pilotos.ToDictionary(p=>p.Id);r.NextResult();
                while(r.Read()) {PilotoPanelPiloto p;if(personas.TryGetValue(Convert.ToInt64(r["Usuario_Id"]),out p)) p.Centros.Add(Texto(r,"Centro_Dist"));}
                r.NextResult();while(r.Read()) {PilotoPanelPiloto p;if(personas.TryGetValue(Convert.ToInt64(r["Usuario_Id"]),out p)) {p.Placas.Add(Texto(r,"Placa"));if(Bit(r,"Disponible")) p.PlacasDisponibles.Add(Texto(r,"Placa"));}}
                foreach(var p in modelo.Pilotos) p.Revisar();
            }
        }
        private void CargarArtefactos(SqlConnection cn,SqlTransaction tx,PilotoPanelModelo modelo,Dictionary<string,PilotoPanelRuta> rutas)
        {
            using(var c=Cmd(cn,tx,SqlArtefactos)) {
            Param(c,"@catalogo",SqlDbType.NVarChar,modelo.Catalogo,128);
            using(var r=c.ExecuteReader()) {
                while(r.Read()) {
                    var ruta=rutas[Texto(r,"Ruta_Id")];
                    if(!PilotoPanelReglas.AplicarBorrador(ruta,Convert.ToInt64(r["Usuario_Id"]),(int)r["Primer_RowId"],Texto(r,"Version"),Texto(r,"Resultados"),Bit(r,"Completado"),(DateTime)r["FechaUtc"],Bit(r,"OrigenVerificado")))
                        if(!ruta.Alertas.Contains("Hay borradores de origen no verificable o incompatibles; no se contabilizan como avance vigente.")) ruta.Alertas.Add("Hay borradores de origen no verificable o incompatibles; no se contabilizan como avance vigente.");
                }
                r.NextResult();while(r.Read()) {var ruta=rutas[Texto(r,"Ruta_Id")];var d=ruta.Documentos.SingleOrDefault(x=>x.Datos.RowId==(int)r["Detalle_RowId"]);if(d!=null) {d.Foto=true;var fecha=(DateTime)r["FechaUtc"];if(!ruta.ActividadUtc.HasValue || ruta.ActividadUtc<fecha) ruta.ActividadUtc=fecha;}}
                r.NextResult();while(r.Read()) {var ruta=rutas[Texto(r,"Ruta_Id")];var cl=ruta.Clientes.SingleOrDefault(x=>x.Primero==(int)r["Primer_RowId"]);if(cl!=null) {cl.Foto=true;var fecha=(DateTime)r["FechaUtc"];if(!ruta.ActividadUtc.HasValue || ruta.ActividadUtc<fecha) ruta.ActividadUtc=fecha;}}
            }
            }
        }
        public PilotoImagen Imagen(string login,string id,int row,bool cliente)
        {
            if(string.IsNullOrWhiteSpace(id) || id.Length>15 || row<=0) throw new PilotoException(404,"Imagen no disponible.");
            using(var cn=new SqlConnection(conexion)) {
                cn.Open();using(var c=Cmd(cn,null,"SET LOCK_TIMEOUT 3000;")) c.ExecuteNonQuery();
                using(var tx=cn.BeginTransaction(IsolationLevel.ReadCommitted)) {
                    Autorizar(cn,tx,login);
                    var tabla=cliente ? "PilotoClienteImagen" : "PilotoDocumentoImagen";var columna=cliente ? "Primer_RowId" : "Detalle_RowId";
                    using(var c=Cmd(cn,tx,"SELECT i.Nombre,i.ContentType,i.Contenido FROM dbo."+tabla+" i JOIN "+apk+".dbo.RT_RUTAS r ON r.ID_RUTA COLLATE DATABASE_DEFAULT=i.Ruta_Id JOIN "+apk+".dbo.RT_RUTAS_DET d ON d.ID_RUTA=r.ID_RUTA AND d.ROWID=i."+columna+" WHERE i.Ruta_Id=@id AND i."+columna+"=@row;")) {
                        Param(c,"@id",SqlDbType.NVarChar,id,15);Param(c,"@row",SqlDbType.Int,row);
                        using(var r=c.ExecuteReader()) {
                            if(!r.Read()) throw new PilotoException(404,"Imagen no disponible en la ruta actual.");
                            var img=new PilotoImagen {RutaId=id,RowId=row,Nombre=Texto(r,"Nombre"),ContentType=Texto(r,"ContentType"),Contenido=(byte[])r["Contenido"]};
                            if(!new[]{"image/jpeg","image/png","image/webp"}.Contains(img.ContentType)) throw new PilotoException(409,"El formato guardado no está permitido.");
                            r.Close();tx.Commit();return img;
                        }
                    }
                }
            }
        }
    }
}
