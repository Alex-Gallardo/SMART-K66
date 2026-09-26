using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Xml.Linq;
using DiamDev.Give.Entities;

namespace DiamDev.Give.DAL
{
    public sealed class PilotoRutaDA
    {
        private readonly string conexion;
        private readonly string apk;
        private sealed class Acceso { public long Usuario; public string Nombre; public string Operador; public string PlacaPrueba; public string RutaPrueba; public string PilotoPrueba; }

        public static bool Habilitado { get { return EsTrue("Pilotos.Habilitado"); } }
        public static bool PruebasSoloLectura { get { return EsTrue("Pilotos.PruebasSoloLectura"); } }
        public static bool CierreHabilitado { get { return !PruebasSoloLectura && EsTrue("Pilotos.PermitirCierre"); } }
        private static bool EsTrue(string key) { return string.Equals((ConfigurationManager.AppSettings[key] ?? "").Trim(), "true", StringComparison.OrdinalIgnoreCase); }
        public static bool ConsultaRutaFija { get { return PruebasSoloLectura && !string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["Pilotos.RutaPrueba"]); } }

        public static void ValidarConfiguracionPrueba()
        {
            if (!PruebasSoloLectura) return;
            if (string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["Pilotos.UsuarioPrueba"]))
                throw new PilotoException(503, "Falta configurar el usuario de consulta temporal. Contacta al administrador.");
            var placa = (ConfigurationManager.AppSettings["Pilotos.PlacaPrueba"] ?? "").Trim();
            var ruta = (ConfigurationManager.AppSettings["Pilotos.RutaPrueba"] ?? "").Trim();
            var piloto = (ConfigurationManager.AppSettings["Pilotos.PilotoPrueba"] ?? "").Trim();
            if (piloto.Length > 90 || (piloto.Length > 0 && placa.Length == 0))
                throw new PilotoException(503,"El piloto de prueba debe acompañar a una placa válida. Contacta al administrador.");
            if ((placa.Length == 0) == (ruta.Length == 0) || placa.Length > 15 || ruta.Length > 15)
                throw new PilotoException(503, "La consulta temporal necesita una sola placa o ruta válida. Contacta al administrador.");
        }
        public static bool PermiteUsuarioPrueba(string login)
        {
            var permitido=ConfigurationManager.AppSettings["Pilotos.UsuarioPrueba"];
            return PruebasSoloLectura && !string.IsNullOrWhiteSpace(login) && !string.IsNullOrWhiteSpace(permitido)
                && string.Equals(login,permitido.Trim(),StringComparison.OrdinalIgnoreCase);
        }

        public PilotoRutaDA()
        {
            ValidarConfiguracionPrueba();
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

        private SqlConnection AbrirConexion()
        {
            var cn = new SqlConnection(conexion);
            try
            {
                cn.Open();
                // Se limita la espera por bloqueos sin leer datos sin confirmar.
                using (var cmd = Command(cn, null, "SET LOCK_TIMEOUT 3000;")) cmd.ExecuteNonQuery();
                return cn;
            }
            catch { cn.Dispose(); throw; }
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
            using (var readiness = Command(cn, tx, "SELECT CASE WHEN OBJECT_ID(N'dbo.PilotoVinculo',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.PilotoCentro',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.PilotoVehiculo',N'U') IS NOT NULL THEN 1 ELSE 0 END;"))
                if ((int)readiness.ExecuteScalar() != 1) throw new PilotoException(503, "Falta preparar los vínculos de pilotos y vehículos. Contacta al administrador.");
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
                    var piloto=(ConfigurationManager.AppSettings["Pilotos.PilotoPrueba"] ?? "").Trim();
                    return new Acceso { Usuario=(long)r["Usuario_Id"], PlacaPrueba=placa, RutaPrueba=ruta, PilotoPrueba=piloto.Length==0 ? null : piloto };
                }
            }
        }

        private string Alcance(Acceso a)
        {
            return a.PlacaPrueba!=null ? "r.PLACA=@placa" + (a.PilotoPrueba==null ? "" : " AND r.PILOTO=@pilotoPrueba") : a.RutaPrueba!=null ? "r.ID_RUTA=@rutaPrueba" : @"r.PILOTO=@piloto
AND EXISTS (SELECT 1 FROM dbo.PilotoCentro c WHERE c.Usuario_Id=@usuario AND c.Centro_Dist=r.CENTRO_DIST COLLATE DATABASE_DEFAULT)
AND EXISTS (SELECT 1 FROM dbo.PilotoVehiculo pv WHERE pv.Usuario_Id=@usuario AND pv.Activo=1 AND pv.Placa=r.PLACA COLLATE DATABASE_DEFAULT)
AND EXISTS (SELECT 1 FROM " + apk + @".dbo.RT_VEHICULOS v WHERE v.PLACA=r.PLACA AND v.ESTADO=1)";
        }
        private static void ParametrosAcceso(SqlCommand cmd,Acceso a)
        {
            if(a.PlacaPrueba!=null) {
                Param(cmd,"@placa",SqlDbType.NVarChar,a.PlacaPrueba,15);
                if(a.PilotoPrueba!=null) Param(cmd,"@pilotoPrueba",SqlDbType.NVarChar,a.PilotoPrueba,90);
            }
            else if(a.RutaPrueba!=null) Param(cmd,"@rutaPrueba",SqlDbType.NVarChar,a.RutaPrueba,15);
            else { Param(cmd,"@piloto",SqlDbType.NVarChar,a.Nombre,90); Param(cmd,"@usuario",SqlDbType.BigInt,a.Usuario); }
        }

        private static PilotoRuta Cabecera(SqlDataReader r)
        {
            return new PilotoRuta { Id = Texto(r, "ID_RUTA"), Fecha = (DateTime)r["FECHA_RUTA"],
                Placa = Texto(r, "PLACA"), Piloto = Texto(r, "PILOTO"), Centro = Texto(r, "CENTRO_DIST"),
                Estado = Texto(r, "STATUS"), Solicitud = Guid.NewGuid() };
        }

        public PilotoLista Listar(string login, DateTime desde, DateTime hasta, int pagina, string vista = null)
        {
            vista=PilotoReglas.VistaRutas(vista);
            desde = desde.Date; hasta = hasta.Date;
            if (hasta < desde || (hasta-desde).TotalDays >= 31 || hasta == DateTime.MaxValue.Date || pagina < 1 || pagina > 1000)
                throw new PilotoException(400, "Selecciona un periodo de hasta 31 dias y una pagina valida.");
            var result = new PilotoLista { Desde=desde, Hasta=hasta, Pagina=pagina, Vista=vista, Rutas=new List<PilotoRuta>() };
            using (var cn = AbrirConexion())
            {
                using (var tx = cn.BeginTransaction(IsolationLevel.Serializable))
                {
                    var a = Autorizar(cn, tx, login, "Pilotos.Rutas.Ver");
                    result.RutaFija=a.RutaPrueba!=null;
                    var sql = @"SELECT r.ID_RUTA,r.FECHA_RUTA,r.PLACA,r.PILOTO,r.CENTRO_DIST,r.STATUS
FROM " + apk + @".dbo.RT_RUTAS r
WHERE " + Alcance(a) + (result.RutaFija ? " AND r.STATUS IN (N'A',N'E',N'C',N'X')" : " AND r.FECHA_RUTA>=@desde AND r.FECHA_RUTA<@fin AND r.STATUS IN (@estado1,@estado2)") + @"
ORDER BY CASE WHEN r.STATUS=N'E' THEN 0 ELSE 1 END,r.FECHA_RUTA DESC,r.ID_RUTA DESC OFFSET @offset ROWS FETCH NEXT 26 ROWS ONLY;";
                    using (var cmd = Command(cn, tx, sql))
                    {
                        ParametrosAcceso(cmd,a);
                        Param(cmd,"@desde",SqlDbType.Date,desde); Param(cmd,"@fin",SqlDbType.Date,hasta.AddDays(1));
                        Param(cmd,"@offset",SqlDbType.Int,(pagina-1)*25);
                        Param(cmd,"@estado1",SqlDbType.NVarChar,vista=="activas" ? "A" : "C",1);
                        Param(cmd,"@estado2",SqlDbType.NVarChar,vista=="activas" ? "E" : "X",1);
                        using (var r=cmd.ExecuteReader()) while(r.Read()) result.Rutas.Add(Cabecera(r));
                    }
                    tx.Commit();
                }
            }
            result.HayMas=result.Rutas.Count>25;
            if(result.HayMas) result.Rutas.RemoveAt(25);
            return result;
        }

        private PilotoRuta LeerRuta(SqlConnection cn, SqlTransaction tx, Acceso a, string id, bool bloquear, int pagina = 1, bool consulta = false)
        {
            if (pagina < 1 || pagina > 100000) throw new PilotoException(400, "Selecciona una página de documentos válida.");
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
            using (var cmd = Command(cn, tx, "SELECT COUNT(*) FROM " + apk + ".dbo.RT_RUTAS_DET WHERE ID_RUTA=@id;"))
            {
                Param(cmd,"@id",SqlDbType.NVarChar,id,15); ruta.TotalDocumentos=(int)cmd.ExecuteScalar();
            }
            if (!consulta && ruta.TotalDocumentos > PilotoReglas.MaxDocumentos)
                throw new PilotoException(409,"Esta ruta supera el límite de documentos para cierre. Puedes consultarla; solicita el cierre a Distribución.");
            if (consulta) PilotoReglas.PrepararConsulta(ruta,pagina);
            var paginada = consulta && !ruta.PuedeCerrar;
            if (!consulta) { ruta.TamanoPaginaDocumentos=PilotoReglas.MaxDocumentos; ruta.PaginaDocumentos=1; }
            using (var cmd = Command(cn, tx, "SELECT MARCA,TIPO,LINEA,EMPRESA FROM " + apk + ".dbo.RT_VEHICULOS WHERE PLACA=@placa;"))
            {
                Param(cmd,"@placa",SqlDbType.NVarChar,ruta.Placa,15);
                using (var r = cmd.ExecuteReader()) if(r.Read())
                {
                    ruta.Transporte=Texto(r,"EMPRESA");
                    ruta.Vehiculo=string.Join(" · ",new[]{Texto(r,"MARCA"),Texto(r,"LINEA"),Texto(r,"TIPO")}.Where(v=>!string.IsNullOrWhiteSpace(v)));
                }
            }
            sql=@"SELECT ROWID,TIPO,ID_EMPRESA,ID_DOCUMENTO,CLIENTE,DIR_DESPACHO,NO_BULTOS,
MO_VISITO,MO_ENTREGA,MO_MOTIVO,MO_OBSER,MO_HR_ENTRADA,MO_HR_SALIDA
FROM " + apk + ".dbo.RT_RUTAS_DET" + hint + "WHERE ID_RUTA=@id ORDER BY ROWID OFFSET @offset ROWS FETCH NEXT @limite ROWS ONLY;";
            using(var cmd=Command(cn,tx,sql))
            {
                Param(cmd,"@id",SqlDbType.NVarChar,id,15);
                Param(cmd,"@limite",SqlDbType.Int,ruta.TamanoPaginaDocumentos);
                Param(cmd,"@offset",SqlDbType.Int,(ruta.PaginaDocumentos-1)*ruta.TamanoPaginaDocumentos);
                using(var r=cmd.ExecuteReader()) while(r.Read())
                {
                    ruta.Documentos.Add(new PilotoDocumento { RowId=(int)r["ROWID"], Tipo=Texto(r,"TIPO"), Empresa=Texto(r,"ID_EMPRESA"),
                        Documento=Texto(r,"ID_DOCUMENTO"), Cliente=Texto(r,"CLIENTE"), Direccion=Texto(r,"DIR_DESPACHO"), Bultos=(decimal)r["NO_BULTOS"],
                        Visito=r["MO_VISITO"]==DBNull.Value ? (bool?)null : (bool)r["MO_VISITO"], Entrega=Texto(r,"MO_ENTREGA"), Motivo=Texto(r,"MO_MOTIVO"),
                        Observaciones=Texto(r,"MO_OBSER"), Entrada=r["MO_HR_ENTRADA"]==DBNull.Value ? (TimeSpan?)null : (TimeSpan)r["MO_HR_ENTRADA"],
                        Salida=r["MO_HR_SALIDA"]==DBNull.Value ? (TimeSpan?)null : (TimeSpan)r["MO_HR_SALIDA"] });
                }
            }
            using (var cmd = Command(cn, tx, "SELECT CASE WHEN OBJECT_ID(N'dbo.PilotoDocumentoImagen',N'U') IS NULL OR OBJECT_ID(N'dbo.PilotoDocumentoImagenEvento',N'U') IS NULL THEN 0 ELSE 1 END;"))
                ruta.ImagenesDisponibles = (int)cmd.ExecuteScalar() == 1;
            ruta.PuedeAdjuntarImagen = ruta.ImagenesDisponibles && CierreHabilitado && ruta.Estado == "E";
            if (ruta.ImagenesDisponibles && ruta.Documentos.Count > 0)
            {
                var documentos = ruta.Documentos.ToDictionary(d => d.RowId);
                using (var cmd = Command(cn, tx, @"SELECT Detalle_RowId, Nombre, FechaUtc
FROM dbo.PilotoDocumentoImagen WHERE Ruta_Id=@id;"))
                {
                    Param(cmd,"@id",SqlDbType.NVarChar,id,15);
                    using (var r=cmd.ExecuteReader()) while(r.Read())
                    {
                        PilotoDocumento d;
                        if (!documentos.TryGetValue((int)r["Detalle_RowId"], out d)) continue;
                        d.TieneImagen=true;
                        d.ImagenNombre=Texto(r,"Nombre");
                        d.ImagenFechaUtc=(DateTime)r["FechaUtc"];
                    }
                }
            }
            if (paginada && pagina > 1 && ruta.Documentos.Count == 0) throw new PilotoException(404,"La página de documentos no está disponible. Vuelve al inicio de la ruta.");
            ruta.Version=paginada ? null : PilotoReglas.Version(ruta);
            using (var cmd=Command(cn,tx,"SELECT CASE WHEN OBJECT_ID(N'dbo.PilotoBorradorCliente',N'U') IS NULL THEN 0 ELSE 1 END;"))
                ruta.BorradoresDisponibles=(int)cmd.ExecuteScalar()==1;
            using (var cmd=Command(cn,tx,"SELECT CASE WHEN COL_LENGTH(N'dbo.PilotoBorradorCliente',N'Completado') IS NULL OR OBJECT_ID(N'dbo.PilotoClienteImagen',N'U') IS NULL OR OBJECT_ID(N'dbo.PilotoClienteImagenEvento',N'U') IS NULL THEN 0 ELSE 1 END;"))
                ruta.ClientesCompletadosDisponibles=(int)cmd.ExecuteScalar()==1;
            if(ruta.ClientesCompletadosDisponibles)
            {
                using(var cmd=Command(cn,tx,"SELECT Primer_RowId FROM dbo.PilotoClienteImagen WHERE Ruta_Id=@ruta;"))
                {
                    Param(cmd,"@ruta",SqlDbType.NVarChar,ruta.Id,15);
                    using(var r=cmd.ExecuteReader()) while(r.Read()) ruta.ClientesConImagen.Add((int)r["Primer_RowId"]);
                }
            }
            ruta.PuedeAdjuntarImagen = ruta.PuedeAdjuntarImagen && ruta.ClientesCompletadosDisponibles;
            return ruta;
        }

        public PilotoRuta Detalle(string login,string id,int pagina = 1)
        {
            using(var cn=AbrirConexion())
            {
                using(var tx=cn.BeginTransaction(IsolationLevel.Serializable))
                {
                    var a=Autorizar(cn,tx,login,"Pilotos.Rutas.Ver"); var ruta=LeerRuta(cn,tx,a,id,false,pagina,true);
                    if(ruta.PuedeCerrar && ruta.ClientesCompletadosDisponibles) CargarBorradores(cn,tx,a,ruta);
                    if(ruta.PuedeCerrar && !ruta.ClientesCompletadosDisponibles) ruta.PuedeCerrar=false;
                    tx.Commit(); return ruta;
                }
            }
        }

        private static string Grupo(PilotoDocumento d)
        { return (d.Cliente??"").Trim().ToUpperInvariant()+"\u001f"+(d.Direccion??"").Trim().ToUpperInvariant(); }

        private static List<PilotoDocumento> DocumentosGrupo(PilotoRuta ruta,int primero)
        {
            var principal=ruta.Documentos.SingleOrDefault(d=>d.RowId==primero);
            if(principal==null) throw new PilotoException(409,"El cliente ya no pertenece a la ruta. Recarga los detalles.");
            var clave=Grupo(principal);
            var docs=ruta.Documentos.Where(d=>Grupo(d)==clave).OrderBy(d=>d.RowId).ToList();
            if(docs[0].RowId!=primero) throw new PilotoException(409,"El grupo de cliente cambió. Recarga los detalles.");
            return docs;
        }

        private static XElement ResultadosXml(IEnumerable<PilotoResultado> resultados)
        {
            return new XElement("documentos",resultados.Select(d=>new XElement("d",new XAttribute("id",d.RowId),
                new XElement("visito",d.Visito),new XElement("entrega",d.Entrega),
                new XElement("motivo",d.Motivo),new XElement("observaciones",d.Observaciones))));
        }

        private static List<PilotoResultado> LeerResultados(string xml)
        {
            if(string.IsNullOrEmpty(xml)) return new List<PilotoResultado>();
            var root=XElement.Parse(xml);
            if(root.Name!="documentos") throw new PilotoException(503,"El borrador guardado no tiene el formato esperado.");
            return root.Elements("d").Select(x=>new PilotoResultado {
                RowId=int.Parse((string)x.Attribute("id"),CultureInfo.InvariantCulture),
                Visito=string.IsNullOrEmpty((string)x.Element("visito")) ? (bool?)null : bool.Parse((string)x.Element("visito")),
                Entrega=(string)x.Element("entrega"),Motivo=(string)x.Element("motivo"),Observaciones=(string)x.Element("observaciones")
            }).ToList();
        }

        private void CargarBorradores(SqlConnection cn,SqlTransaction tx,Acceso a,PilotoRuta ruta,bool aplicar=true)
        {
            using(var cmd=Command(cn,tx,@"SELECT b.Primer_RowId,b.Version,b.MasivoActivo,b.Resultados,b.Anteriores,b.Completado,
CASE WHEN i.Ruta_Id IS NULL THEN 0 ELSE 1 END AS TieneImagenCliente
FROM dbo.PilotoBorradorCliente b LEFT JOIN dbo.PilotoClienteImagen i
ON i.Ruta_Id=b.Ruta_Id AND i.Primer_RowId=b.Primer_RowId
WHERE b.Usuario_Id=@usuario AND b.Ruta_Id=@ruta;"))
            {
                Param(cmd,"@usuario",SqlDbType.BigInt,a.Usuario); Param(cmd,"@ruta",SqlDbType.NVarChar,ruta.Id,15);
                using(var r=cmd.ExecuteReader()) while(r.Read())
                {
                    if(Texto(r,"Version")!=ruta.Version) continue;
                    var b=new PilotoBorradorCliente { RutaId=ruta.Id,Version=ruta.Version,
                        PrimerRowId=(int)r["Primer_RowId"],MasivoActivo=(bool)r["MasivoActivo"],
                        Completado=(bool)r["Completado"],TieneImagenCliente=(int)r["TieneImagenCliente"]==1,
                        Documentos=LeerResultados(Texto(r,"Resultados")),Anteriores=LeerResultados(Texto(r,"Anteriores")) };
                    var grupo=DocumentosGrupo(ruta,b.PrimerRowId);
                    if(b.Documentos.Count!=grupo.Count || !b.Documentos.Select(d=>d.RowId).OrderBy(x=>x).SequenceEqual(grupo.Select(d=>d.RowId)))
                        throw new PilotoException(409,"El borrador no coincide con los documentos actuales. Contacta a Distribución.");
                    if(aplicar) foreach(var d in grupo)
                    {
                        var guardado=b.Documentos.Single(x=>x.RowId==d.RowId);
                        d.Visito=guardado.Visito; d.Entrega=guardado.Entrega; d.Motivo=guardado.Motivo;
                        d.ObservacionPiloto=guardado.Observaciones;
                        d.AnteriorMasivo=b.Anteriores.SingleOrDefault(x=>x.RowId==d.RowId);
                    }
                    ruta.Borradores.Add(b);
                }
            }
        }

        public void GuardarBorrador(string login,PilotoBorradorCliente borrador)
        {
            if(PruebasSoloLectura || !CierreHabilitado) throw new PilotoException(403,"El guardado de resultados no está habilitado.");
            if(borrador==null || borrador.PrimerRowId<=0 || string.IsNullOrWhiteSpace(borrador.RutaId) || borrador.RutaId.Length>15)
                throw new PilotoException(400,"El borrador no es válido.");
            using(var cn=AbrirConexion()) using(var tx=cn.BeginTransaction(IsolationLevel.Serializable))
            {
                var a=Autorizar(cn,tx,login,"Pilotos.Rutas.Confirmar");
                var ruta=LeerRuta(cn,tx,a,borrador.RutaId,true);
                if(!ruta.PuedeCerrar || !ruta.ClientesCompletadosDisponibles || ruta.Version!=borrador.Version)
                    throw new PilotoException(409,"La ruta cambió o el guardado no está instalado. Recarga los detalles.");
                var grupo=DocumentosGrupo(ruta,borrador.PrimerRowId);
                using(var lockCmd=Command(cn,tx,@"SELECT Completado FROM dbo.PilotoBorradorCliente WITH(UPDLOCK,HOLDLOCK)
WHERE Usuario_Id=@usuario AND Ruta_Id=@ruta AND Primer_RowId=@primero;"))
                {
                    Param(lockCmd,"@usuario",SqlDbType.BigInt,a.Usuario); Param(lockCmd,"@ruta",SqlDbType.NVarChar,ruta.Id,15);
                    Param(lockCmd,"@primero",SqlDbType.Int,borrador.PrimerRowId);
                    var completado=lockCmd.ExecuteScalar();
                    if(completado is bool && (bool)completado)
                        throw new PilotoException(409,"Este cliente ya está completado y no admite cambios.");
                }
                if(borrador.Documentos==null || borrador.Documentos.Count!=grupo.Count ||
                    !borrador.Documentos.Select(d=>d.RowId).OrderBy(x=>x).SequenceEqual(grupo.Select(d=>d.RowId)))
                    throw new PilotoException(400,"Guarda todos los documentos de este cliente.");
                PilotoReglas.NormalizarCierre(new PilotoCierre { Documentos=borrador.Documentos });
                foreach(var d in borrador.Documentos) PilotoReglas.ValidarResultado(d);
                if(borrador.MasivoActivo)
                {
                    if(borrador.Anteriores==null || borrador.Anteriores.Count!=grupo.Count ||
                       !borrador.Anteriores.Select(d=>d.RowId).OrderBy(x=>x).SequenceEqual(grupo.Select(d=>d.RowId)))
                        throw new PilotoException(400,"Faltan las respuestas anteriores del marcado masivo.");
                    foreach(var d in borrador.Anteriores)
                        if(d==null || (d.Entrega??"").Length>50 || (d.Motivo??"").Length>150 || (d.Observaciones??"").Length>PilotoReglas.MaxObservacion)
                            throw new PilotoException(400,"Las respuestas anteriores no son válidas.");
                }
                else if(borrador.Anteriores!=null && borrador.Anteriores.Count>0)
                    throw new PilotoException(400,"El marcado masivo no coincide con las respuestas anteriores.");
                var resultados=ResultadosXml(borrador.Documentos).ToString(SaveOptions.DisableFormatting);
                var anteriores=borrador.MasivoActivo ? ResultadosXml(borrador.Anteriores).ToString(SaveOptions.DisableFormatting) : null;
                using(var cmd=Command(cn,tx,@"UPDATE dbo.PilotoBorradorCliente WITH(UPDLOCK,HOLDLOCK)
SET Version=@version,MasivoActivo=@masivo,Resultados=@resultados,Anteriores=@anteriores,Completado=1,FechaUtc=SYSUTCDATETIME()
WHERE Usuario_Id=@usuario AND Ruta_Id=@ruta AND Primer_RowId=@primero;
IF @@ROWCOUNT=0 INSERT dbo.PilotoBorradorCliente(Usuario_Id,Ruta_Id,Primer_RowId,Version,MasivoActivo,Resultados,Anteriores,Completado)
VALUES(@usuario,@ruta,@primero,@version,@masivo,@resultados,@anteriores,1);"))
                {
                    Param(cmd,"@usuario",SqlDbType.BigInt,a.Usuario); Param(cmd,"@ruta",SqlDbType.NVarChar,ruta.Id,15);
                    Param(cmd,"@primero",SqlDbType.Int,borrador.PrimerRowId); Param(cmd,"@version",SqlDbType.VarChar,borrador.Version,44);
                    Param(cmd,"@masivo",SqlDbType.Bit,borrador.MasivoActivo); Param(cmd,"@resultados",SqlDbType.NVarChar,resultados,-1);
                    Param(cmd,"@anteriores",SqlDbType.NVarChar,anteriores,-1); cmd.ExecuteNonQuery();
                }
                tx.Commit();
            }
        }

        private static void ValidarImagenId(string rutaId, int rowId)
        {
            if (string.IsNullOrWhiteSpace(rutaId) || rutaId.Length > 15 || rowId <= 0)
                throw new PilotoException(404,"Documento no disponible.");
        }

        private void VerificarTablaImagen(SqlConnection cn, SqlTransaction tx)
        {
            using (var cmd=Command(cn,tx,"SELECT CASE WHEN OBJECT_ID(N'dbo.PilotoDocumentoImagen',N'U') IS NULL OR OBJECT_ID(N'dbo.PilotoDocumentoImagenEvento',N'U') IS NULL THEN 0 ELSE 1 END;"))
                if ((int)cmd.ExecuteScalar()!=1)
                    throw new PilotoException(503,"Falta instalar las imágenes de documentos en POS.");
        }

        public PilotoImagen ObtenerImagen(string login, string rutaId, int rowId)
        {
            ValidarImagenId(rutaId,rowId);
            using(var cn=AbrirConexion()) using(var tx=cn.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                var acceso=Autorizar(cn,tx,login,"Pilotos.Rutas.Ver");
                VerificarTablaImagen(cn,tx);
                var sql=@"SELECT i.Nombre,i.ContentType,i.Contenido
FROM " + apk + @".dbo.RT_RUTAS r
JOIN " + apk + @".dbo.RT_RUTAS_DET d ON d.ID_RUTA=r.ID_RUTA
JOIN dbo.PilotoDocumentoImagen i ON i.Ruta_Id=r.ID_RUTA COLLATE DATABASE_DEFAULT AND i.Detalle_RowId=d.ROWID
WHERE r.ID_RUTA=@id AND d.ROWID=@row AND " + Alcance(acceso) + ";";
                using(var cmd=Command(cn,tx,sql))
                {
                    Param(cmd,"@id",SqlDbType.NVarChar,rutaId,15);
                    Param(cmd,"@row",SqlDbType.Int,rowId);
                    ParametrosAcceso(cmd,acceso);
                    using(var r=cmd.ExecuteReader())
                    {
                        if(!r.Read()) throw new PilotoException(404,"Imagen no disponible.");
                        var imagen=new PilotoImagen { RutaId=rutaId,RowId=rowId,Nombre=Texto(r,"Nombre"),
                            ContentType=Texto(r,"ContentType"),Contenido=(byte[])r["Contenido"] };
                        r.Close(); tx.Commit(); return imagen;
                    }
                }
            }
        }

        public void GuardarImagen(string login, PilotoImagen imagen)
        {
            if (!CierreHabilitado) throw new PilotoException(403,"La carga de imágenes no está habilitada.");
            if (imagen==null) throw new PilotoException(400,"Selecciona una imagen.");
            ValidarImagenId(imagen.RutaId,imagen.RowId);
            imagen.ContentType=PilotoReglas.ValidarImagen(imagen.Nombre,imagen.Contenido);
            byte[] huella;
            using(var sha=SHA256.Create()) huella=sha.ComputeHash(imagen.Contenido);
            using(var cn=AbrirConexion()) using(var tx=cn.BeginTransaction(IsolationLevel.Serializable))
            {
                var acceso=Autorizar(cn,tx,login,"Pilotos.Rutas.Ver");
                VerificarTablaImagen(cn,tx);
                var sql=@"SELECT TOP (1) r.STATUS
FROM " + apk + @".dbo.RT_RUTAS r WITH(UPDLOCK,HOLDLOCK)
JOIN " + apk + @".dbo.RT_RUTAS_DET d ON d.ID_RUTA=r.ID_RUTA
WHERE r.ID_RUTA=@id AND d.ROWID=@row AND " + Alcance(acceso) + ";";
                string estado;
                using(var cmd=Command(cn,tx,sql))
                {
                    Param(cmd,"@id",SqlDbType.NVarChar,imagen.RutaId,15);
                    Param(cmd,"@row",SqlDbType.Int,imagen.RowId);
                    ParametrosAcceso(cmd,acceso);
                    estado=cmd.ExecuteScalar() as string;
                }
                if(estado==null) throw new PilotoException(404,"Documento no disponible o reasignado.");
                if(estado!="E") throw new PilotoException(409,"Solo puedes subir imágenes mientras la ruta está En ruta.");
                if(imagen.Entrega!="NO ENTREGADO" && imagen.Entrega!="INCIDENCIA")
                    throw new PilotoException(400,"Selecciona No entregado o Incidencia antes de subir la imagen.");
                var ruta=LeerRuta(cn,tx,acceso,imagen.RutaId,true);
                if(!ruta.ClientesCompletadosDisponibles) throw new PilotoException(503,"Falta instalar la finalización de clientes en POS.");
                var documento=ruta.Documentos.SingleOrDefault(d=>d.RowId==imagen.RowId);
                if(documento==null) throw new PilotoException(404,"Documento no disponible.");
                var primero=DocumentosGrupo(ruta,ruta.Documentos.Where(d=>Grupo(d)==Grupo(documento)).Min(d=>d.RowId))[0].RowId;
                using(var cmd=Command(cn,tx,@"SELECT Completado FROM dbo.PilotoBorradorCliente WITH(UPDLOCK,HOLDLOCK)
WHERE Usuario_Id=@usuario AND Ruta_Id=@ruta AND Primer_RowId=@primero;"))
                {
                    Param(cmd,"@usuario",SqlDbType.BigInt,acceso.Usuario); Param(cmd,"@ruta",SqlDbType.NVarChar,ruta.Id,15);
                    Param(cmd,"@primero",SqlDbType.Int,primero);
                    var completado=cmd.ExecuteScalar();
                    if(completado is bool && (bool)completado) throw new PilotoException(409,"Este cliente ya está completado y no admite cambios.");
                }
                byte[] anterior=null;
                using(var cmd=Command(cn,tx,@"SELECT HashSha256 FROM dbo.PilotoDocumentoImagen WITH(UPDLOCK,HOLDLOCK)
WHERE Ruta_Id=@id AND Detalle_RowId=@row;"))
                {
                    Param(cmd,"@id",SqlDbType.NVarChar,imagen.RutaId,15);
                    Param(cmd,"@row",SqlDbType.Int,imagen.RowId);
                    anterior=cmd.ExecuteScalar() as byte[];
                }
                sql=anterior==null
                    ? @"INSERT dbo.PilotoDocumentoImagen(Ruta_Id,Detalle_RowId,Usuario_Id,Nombre,ContentType,Tamano,Contenido,HashSha256)
VALUES(@id,@row,@usuario,@nombre,@tipo,@tamano,@contenido,@huella);"
                    : @"UPDATE dbo.PilotoDocumentoImagen
SET Usuario_Id=@usuario,Nombre=@nombre,ContentType=@tipo,Tamano=@tamano,Contenido=@contenido,HashSha256=@huella,FechaUtc=SYSUTCDATETIME()
WHERE Ruta_Id=@id AND Detalle_RowId=@row;";
                using(var cmd=Command(cn,tx,sql))
                {
                    Param(cmd,"@id",SqlDbType.NVarChar,imagen.RutaId,15);
                    Param(cmd,"@row",SqlDbType.Int,imagen.RowId);
                    Param(cmd,"@usuario",SqlDbType.BigInt,acceso.Usuario);
                    Param(cmd,"@nombre",SqlDbType.NVarChar,imagen.Nombre,255);
                    Param(cmd,"@tipo",SqlDbType.NVarChar,imagen.ContentType,50);
                    Param(cmd,"@tamano",SqlDbType.Int,imagen.Contenido.Length);
                    Param(cmd,"@contenido",SqlDbType.VarBinary,imagen.Contenido,-1);
                    Param(cmd,"@huella",SqlDbType.Binary,huella,32);
                    cmd.ExecuteNonQuery();
                }
                using(var cmd=Command(cn,tx,@"INSERT dbo.PilotoDocumentoImagenEvento
(Ruta_Id,Detalle_RowId,Usuario_Id,Accion,HashAnterior,HashNueva)
VALUES(@id,@row,@usuario,@accion,@anterior,@nueva);"))
                {
                    Param(cmd,"@id",SqlDbType.NVarChar,imagen.RutaId,15);
                    Param(cmd,"@row",SqlDbType.Int,imagen.RowId);
                    Param(cmd,"@usuario",SqlDbType.BigInt,acceso.Usuario);
                    Param(cmd,"@accion",SqlDbType.NVarChar,anterior==null ? "AGREGADA" : "REEMPLAZADA",12);
                    Param(cmd,"@anterior",SqlDbType.Binary,anterior,32);
                    Param(cmd,"@nueva",SqlDbType.Binary,huella,32);
                    cmd.ExecuteNonQuery();
                }
                tx.Commit();
            }
        }

        public PilotoClienteImagen ObtenerImagenCliente(string login,string rutaId,int primero)
        {
            ValidarImagenId(rutaId,primero);
            using(var cn=AbrirConexion()) using(var tx=cn.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                var a=Autorizar(cn,tx,login,"Pilotos.Rutas.Ver");
                var ruta=LeerRuta(cn,tx,a,rutaId,false);
                if(!ruta.ClientesCompletadosDisponibles) throw new PilotoException(503,"Falta instalar las imágenes de clientes en POS.");
                DocumentosGrupo(ruta,primero);
                using(var cmd=Command(cn,tx,@"SELECT Nombre,ContentType,Contenido FROM dbo.PilotoClienteImagen
WHERE Ruta_Id=@ruta AND Primer_RowId=@primero;"))
                {
                    Param(cmd,"@ruta",SqlDbType.NVarChar,rutaId,15); Param(cmd,"@primero",SqlDbType.Int,primero);
                    using(var r=cmd.ExecuteReader())
                    {
                        if(!r.Read()) throw new PilotoException(404,"Foto final no disponible.");
                        var imagen=new PilotoClienteImagen { RutaId=rutaId,PrimerRowId=primero,Nombre=Texto(r,"Nombre"),
                            ContentType=Texto(r,"ContentType"),Contenido=(byte[])r["Contenido"] };
                        r.Close(); tx.Commit(); return imagen;
                    }
                }
            }
        }

        public void GuardarImagenCliente(string login,PilotoClienteImagen imagen)
        {
            if(PruebasSoloLectura || !CierreHabilitado) throw new PilotoException(403,"La carga de imágenes no está habilitada.");
            if(imagen==null) throw new PilotoException(400,"Selecciona una foto final.");
            ValidarImagenId(imagen.RutaId,imagen.PrimerRowId);
            imagen.ContentType=PilotoReglas.ValidarImagen(imagen.Nombre,imagen.Contenido);
            byte[] huella;
            using(var sha=SHA256.Create()) huella=sha.ComputeHash(imagen.Contenido);
            using(var cn=AbrirConexion()) using(var tx=cn.BeginTransaction(IsolationLevel.Serializable))
            {
                var a=Autorizar(cn,tx,login,"Pilotos.Rutas.Confirmar");
                var ruta=LeerRuta(cn,tx,a,imagen.RutaId,true);
                if(!ruta.PuedeCerrar || !ruta.ClientesCompletadosDisponibles || ruta.Version!=imagen.Version)
                    throw new PilotoException(409,"La ruta cambió o la foto final no está instalada. Recarga los detalles.");
                var grupo=DocumentosGrupo(ruta,imagen.PrimerRowId);
                if(imagen.Documentos==null || imagen.Documentos.Count!=grupo.Count ||
                    !imagen.Documentos.Select(d=>d.RowId).OrderBy(x=>x).SequenceEqual(grupo.Select(d=>d.RowId)))
                    throw new PilotoException(400,"Resuelve todos los documentos del cliente antes de subir la foto final.");
                PilotoReglas.NormalizarCierre(new PilotoCierre { Documentos=imagen.Documentos });
                foreach(var resultado in imagen.Documentos) PilotoReglas.ValidarResultado(resultado);
                using(var cmd=Command(cn,tx,@"SELECT Completado FROM dbo.PilotoBorradorCliente WITH(UPDLOCK,HOLDLOCK)
WHERE Usuario_Id=@usuario AND Ruta_Id=@ruta AND Primer_RowId=@primero;"))
                {
                    Param(cmd,"@usuario",SqlDbType.BigInt,a.Usuario); Param(cmd,"@ruta",SqlDbType.NVarChar,ruta.Id,15);
                    Param(cmd,"@primero",SqlDbType.Int,imagen.PrimerRowId);
                    var completado=cmd.ExecuteScalar();
                    if(completado is bool && (bool)completado) throw new PilotoException(409,"Este cliente ya está completado y no admite cambios.");
                }
                byte[] anterior;
                using(var cmd=Command(cn,tx,@"SELECT HashSha256 FROM dbo.PilotoClienteImagen WITH(UPDLOCK,HOLDLOCK)
WHERE Ruta_Id=@ruta AND Primer_RowId=@primero;"))
                {
                    Param(cmd,"@ruta",SqlDbType.NVarChar,ruta.Id,15); Param(cmd,"@primero",SqlDbType.Int,imagen.PrimerRowId);
                    anterior=cmd.ExecuteScalar() as byte[];
                }
                using(var cmd=Command(cn,tx,anterior==null
                    ? @"INSERT dbo.PilotoClienteImagen(Ruta_Id,Primer_RowId,Usuario_Id,Nombre,ContentType,Tamano,Contenido,HashSha256)
VALUES(@ruta,@primero,@usuario,@nombre,@tipo,@tamano,@contenido,@huella);"
                    : @"UPDATE dbo.PilotoClienteImagen SET Usuario_Id=@usuario,Nombre=@nombre,ContentType=@tipo,Tamano=@tamano,
Contenido=@contenido,HashSha256=@huella,FechaUtc=SYSUTCDATETIME() WHERE Ruta_Id=@ruta AND Primer_RowId=@primero;"))
                {
                    Param(cmd,"@ruta",SqlDbType.NVarChar,ruta.Id,15); Param(cmd,"@primero",SqlDbType.Int,imagen.PrimerRowId);
                    Param(cmd,"@usuario",SqlDbType.BigInt,a.Usuario); Param(cmd,"@nombre",SqlDbType.NVarChar,imagen.Nombre,255);
                    Param(cmd,"@tipo",SqlDbType.NVarChar,imagen.ContentType,50); Param(cmd,"@tamano",SqlDbType.Int,imagen.Contenido.Length);
                    Param(cmd,"@contenido",SqlDbType.VarBinary,imagen.Contenido,-1); Param(cmd,"@huella",SqlDbType.Binary,huella,32);
                    cmd.ExecuteNonQuery();
                }
                using(var cmd=Command(cn,tx,@"INSERT dbo.PilotoClienteImagenEvento(Ruta_Id,Primer_RowId,Usuario_Id,Accion,HashAnterior,HashNueva)
VALUES(@ruta,@primero,@usuario,@accion,@anterior,@nueva);"))
                {
                    Param(cmd,"@ruta",SqlDbType.NVarChar,ruta.Id,15); Param(cmd,"@primero",SqlDbType.Int,imagen.PrimerRowId);
                    Param(cmd,"@usuario",SqlDbType.BigInt,a.Usuario); Param(cmd,"@accion",SqlDbType.NVarChar,anterior==null?"AGREGADA":"REEMPLAZADA",12);
                    Param(cmd,"@anterior",SqlDbType.Binary,anterior,32); Param(cmd,"@nueva",SqlDbType.Binary,huella,32);
                    cmd.ExecuteNonQuery();
                }
                tx.Commit();
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
            using(var cn=AbrirConexion())
            {
                using(var tx=cn.BeginTransaction(IsolationLevel.Serializable))
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
                    if(!ruta.ClientesCompletadosDisponibles) throw new PilotoException(503,"Falta instalar la finalización de clientes en POS.");
                    CargarBorradores(cn,tx,a,ruta,false);
                    var grupos=ruta.Documentos.GroupBy(Grupo).ToList();
                    if(ruta.Borradores.Count!=grupos.Count)
                        throw new PilotoException(409,"Guarda cada cliente antes de cerrar la ruta.");
                    foreach(var grupo in grupos)
                    {
                        var ids=grupo.Select(d=>d.RowId).OrderBy(x=>x).ToArray();
                        var guardado=ruta.Borradores.SingleOrDefault(b=>b.PrimerRowId==ids[0]);
                        if(guardado==null || !guardado.Completado || guardado.Documentos.Count!=ids.Length ||
                           !guardado.Documentos.Select(d=>d.RowId).OrderBy(x=>x).SequenceEqual(ids))
                            throw new PilotoException(409,"Guarda cada cliente antes de cerrar la ruta.");
                        var esperado=guardado.Documentos.OrderBy(d=>d.RowId).ToArray();
                        var recibido=cierre.Documentos.Where(d=>ids.Contains(d.RowId)).OrderBy(d=>d.RowId).ToArray();
                        if(recibido.Length!=esperado.Length || recibido.Where((d,i)=>d.RowId!=esperado[i].RowId ||
                           d.Visito!=esperado[i].Visito || d.Entrega!=esperado[i].Entrega ||
                           (d.Motivo??"")!=(esperado[i].Motivo??"") || (d.Observaciones??"")!=(esperado[i].Observaciones??"")).Any())
                            throw new PilotoException(409,"Hay cambios sin guardar en un cliente. Guarda el cliente y vuelve a revisar.");
                    }
                    VerificarTrigger(cn,tx);
                    var antes=new XElement("documentos",ruta.Documentos.Select(d=>EstadoXml(d.RowId,d.Visito,d.Entrega,d.Motivo,d.Observaciones)));
                    foreach(var resultado in cierre.Documentos)
                    {
                        var original=ruta.Documentos.Single(d=>d.RowId==resultado.RowId);
                        if(original.Visito==resultado.Visito && original.Entrega==resultado.Entrega && original.Motivo==resultado.Motivo && string.IsNullOrEmpty(resultado.Observaciones)) continue;
                        using(var cmd=Command(cn,tx,apk+".dbo.portal_piloto_guardar_resultado"))
                        {
                            cmd.CommandType=CommandType.StoredProcedure;
                            Param(cmd,"@rowid",SqlDbType.Int,resultado.RowId); Param(cmd,"@idruta",SqlDbType.NVarChar,ruta.Id,15);
                            Param(cmd,"@motivo",SqlDbType.NVarChar,resultado.Motivo,150);
                            Param(cmd,"@observacion",SqlDbType.NVarChar,resultado.Observaciones,PilotoReglas.MaxObservacion);
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
                        f.RowId==d.RowId && f.Visito==d.Visito && f.Entrega==d.Entrega && (f.Motivo??"")== (d.Motivo??"") &&
                        (f.Observaciones??"")==ObservacionEsperada(ruta.Documentos.Single(o=>o.RowId==d.RowId).Observaciones,d.Observaciones))) ||
                        final.Documentos.Any(f=>!ruta.Documentos.Any(o=>o.RowId==f.RowId && o.Entrada==f.Entrada && o.Salida==f.Salida)))
                        throw new PilotoException(409,"No se pudo verificar el cierre. No se guardaron los cambios.");
                    var despues=new XElement("documentos",cierre.Documentos.Select(d=>EstadoXml(d.RowId,d.Visito,d.Entrega,d.Motivo,
                        ObservacionEsperada(ruta.Documentos.Single(o=>o.RowId==d.RowId).Observaciones,d.Observaciones))));
                    using(var cmd=Command(cn,tx,@"INSERT dbo.PilotoCierre(Usuario_Id,Solicitud,Ruta_Id,Login,Operador,Huella,Antes,Despues)
VALUES(@u,@s,@ruta,@login,@operador,@huella,@antes,@despues);"))
                    {
                        Param(cmd,"@u",SqlDbType.BigInt,a.Usuario); Param(cmd,"@s",SqlDbType.UniqueIdentifier,cierre.Solicitud);
                        Param(cmd,"@ruta",SqlDbType.NVarChar,ruta.Id,15); Param(cmd,"@login",SqlDbType.NVarChar,login,50);
                        Param(cmd,"@operador",SqlDbType.NVarChar,a.Operador,15); Param(cmd,"@huella",SqlDbType.VarChar,huella,44);
                        Param(cmd,"@antes",SqlDbType.Xml,antes.ToString(SaveOptions.DisableFormatting));
                        Param(cmd,"@despues",SqlDbType.Xml,despues.ToString(SaveOptions.DisableFormatting)); cmd.ExecuteNonQuery();
                    }
                    using(var cmd=Command(cn,tx,"DELETE dbo.PilotoBorradorCliente WHERE Usuario_Id=@u AND Ruta_Id=@ruta;"))
                    {
                        Param(cmd,"@u",SqlDbType.BigInt,a.Usuario); Param(cmd,"@ruta",SqlDbType.NVarChar,ruta.Id,15);
                        cmd.ExecuteNonQuery();
                    }
                    tx.Commit();
                }
            }
        }

        private static string ObservacionEsperada(string anterior,string nueva)
        {
            if(string.IsNullOrWhiteSpace(nueva)) return anterior ?? "";
            if(string.IsNullOrWhiteSpace(anterior)) return nueva;
            return anterior+"\r\n"+nueva;
        }

        private static XElement EstadoXml(int id,bool? visito,string entrega,string motivo,string observaciones)
        { return new XElement("documento",new XAttribute("rowid",id),new XElement("visito",visito),new XElement("entrega",entrega),new XElement("motivo",motivo),new XElement("observaciones",observaciones)); }
    }
}
