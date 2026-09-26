using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using DiamDev.Give.Entities;

namespace DiamDev.Give.DAL
{
    public sealed class PilotoAdministracionDA
    {
        private readonly string conexion;
        private readonly string apk;
        internal const string SqlAutorizacion = @"SELECT TOP (2)
CAST(CASE WHEN u.Activo=1 AND u.Autenticar_Site=1 THEN 1 ELSE 0 END AS bit) AS Disponible,
CAST(CASE WHEN EXISTS(
    SELECT 1 FROM dbo.Usuario_Rol ur
    JOIN dbo.Rol_Permiso rp ON rp.Rol_Id=ur.Rol_Id
    JOIN dbo.Permiso p ON p.Nombre=rp.Permiso_Id
    WHERE ur.Usuario_Id=u.Usuario_Id AND p.Nombre=@permiso
) THEN 1 ELSE 0 END AS bit) AS TienePermiso
FROM dbo.Usuario u WHERE u.Login=@login;";

        public PilotoAdministracionDA()
        {
            var posConfig = ConfigurationManager.ConnectionStrings["GiveContext"];
            var apkConfig = ConfigurationManager.ConnectionStrings["APK66Context"];
            if (posConfig == null) throw new ConfigurationErrorsException("Falta la conexión POS de pilotos.");
            var pos = new SqlConnectionStringBuilder(posConfig.ConnectionString);
            var catalogo = ConfigurationManager.AppSettings["Pilotos.CatalogoRutas"];
            if (string.IsNullOrWhiteSpace(catalogo))
            {
                if (apkConfig == null) throw new ConfigurationErrorsException("Falta el catálogo de rutas.");
                var origen = new SqlConnectionStringBuilder(apkConfig.ConnectionString);
                if (!string.Equals(pos.DataSource, origen.DataSource, StringComparison.OrdinalIgnoreCase))
                    throw new PilotoException(503, "Configura el catálogo de rutas en la misma instancia POS.");
                catalogo = origen.InitialCatalog;
            }
            catalogo = (catalogo ?? "").Trim();
            if (string.IsNullOrWhiteSpace(pos.InitialCatalog) || string.IsNullOrWhiteSpace(catalogo) || catalogo.Length > 128 ||
                string.Equals(pos.InitialCatalog, catalogo, StringComparison.OrdinalIgnoreCase))
                throw new ConfigurationErrorsException("Pilotos requiere dos catálogos diferentes en la misma instancia SQL.");
            conexion = pos.ConnectionString;
            using (var builder = new SqlCommandBuilder()) apk = builder.QuoteIdentifier(catalogo);
        }

        private SqlConnection Abrir()
        {
            var cn = new SqlConnection(conexion);
            try { cn.Open(); using (var cmd = Comando(cn, null, "SET LOCK_TIMEOUT 3000;")) cmd.ExecuteNonQuery(); return cn; }
            catch { cn.Dispose(); throw; }
        }

        private static SqlCommand Comando(SqlConnection cn, SqlTransaction tx, string sql)
        { return new SqlCommand(sql, cn, tx) { CommandTimeout = 20 }; }

        private static void Param(SqlCommand cmd, string nombre, SqlDbType tipo, object valor, int longitud = 0)
        {
            var p = longitud == 0 ? cmd.Parameters.Add(nombre, tipo) : cmd.Parameters.Add(nombre, tipo, longitud);
            p.Value = valor ?? DBNull.Value;
        }

        private static string Texto(SqlDataReader r, string nombre)
        { return r[nombre] == DBNull.Value ? null : Convert.ToString(r[nombre], CultureInfo.InvariantCulture); }

        private static void ExigirInstalacion(SqlConnection cn, SqlTransaction tx)
        {
            const string sql = @"SELECT CASE WHEN OBJECT_ID(N'dbo.PilotoVinculo',N'U') IS NOT NULL
 AND OBJECT_ID(N'dbo.PilotoCentro',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.PilotoVehiculo',N'U') IS NOT NULL
 AND OBJECT_ID(N'dbo.PilotoVehiculoHistorial',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.PilotoVinculoHistorial',N'U') IS NOT NULL
 AND OBJECT_ID(N'dbo.PilotoCentroHistorial',N'U') IS NOT NULL THEN 1 ELSE 0 END;";
            using (var cmd = Comando(cn, tx, sql))
                if ((int)cmd.ExecuteScalar() != 1) throw new PilotoException(503, "Falta instalar la administración de vínculos de pilotos.");
        }

        private static void Autorizar(SqlConnection cn, SqlTransaction tx, string login)
        {
            if (string.IsNullOrWhiteSpace(login) || login.Length > 50) throw new PilotoException(403, "Acceso no autorizado.");
            using (var cmd = Comando(cn, tx, SqlAutorizacion))
            {
                Param(cmd, "@login", SqlDbType.NVarChar, login, 50);
                Param(cmd, "@permiso", SqlDbType.NVarChar, PilotoAdminReglas.PermisoAdministrar, 100);
                using(var r=cmd.ExecuteReader())
                {
                    if(!r.Read()) { PilotoAdminReglas.ValidarAcceso(false,false); return; }
                    var disponible=(bool)r["Disponible"];
                    var permiso=(bool)r["TienePermiso"];
                    if(r.Read()) disponible=false;
                    PilotoAdminReglas.ValidarAcceso(disponible,permiso);
                }
            }
        }

        public PilotoAdminLista Listar(string actor, string buscar)
        {
            buscar = string.IsNullOrWhiteSpace(buscar) ? null : buscar.Trim();
            if (buscar != null && buscar.Length > 80) throw new PilotoException(400, "La búsqueda es demasiado larga.");
            var resultado = new PilotoAdminLista { Buscar = buscar };
            using (var cn = Abrir())
            using (var tx = cn.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                Autorizar(cn, tx, actor); ExigirInstalacion(cn, tx);
                const string sql = @"SELECT TOP (100) u.Usuario_Id,u.Login,u.Nombre,u.Activo,u.Autenticar_Site,
 CAST(CASE WHEN EXISTS(SELECT 1 FROM dbo.Usuario_Rol ur JOIN dbo.Rol r ON r.Rol_Id=ur.Rol_Id
  WHERE ur.Usuario_Id=u.Usuario_Id AND r.Nombre=N'PILOTO') THEN 1 ELSE 0 END AS bit) TieneRolPiloto,
 p.Activo VinculoActivo,p.Empleado_RowId,p.Codigo_Operador,e.NOMBRE Piloto,
 (SELECT COUNT(*) FROM dbo.PilotoCentro c WHERE c.Usuario_Id=u.Usuario_Id) Centros,
 (SELECT COUNT(*) FROM dbo.PilotoVehiculo v WHERE v.Usuario_Id=u.Usuario_Id AND v.Activo=1) Vehiculos
FROM dbo.Usuario u
LEFT JOIN dbo.PilotoVinculo p ON p.Usuario_Id=u.Usuario_Id
LEFT JOIN {APK}.dbo.RT_EMPLEADOS e ON e.ROWID=p.Empleado_RowId
WHERE (p.Usuario_Id IS NOT NULL OR EXISTS(SELECT 1 FROM dbo.Usuario_Rol ur JOIN dbo.Rol r ON r.Rol_Id=ur.Rol_Id
       WHERE ur.Usuario_Id=u.Usuario_Id AND r.Nombre=N'PILOTO'))
AND (@buscar IS NULL OR u.Login LIKE N'%'+@buscar+N'%' OR u.Nombre LIKE N'%'+@buscar+N'%')
ORDER BY CASE WHEN p.Activo=1 THEN 0 WHEN p.Usuario_Id IS NULL THEN 1 ELSE 2 END,u.Nombre,u.Login;";
                using (var cmd = Comando(cn, tx, sql.Replace("{APK}", apk)))
                {
                    Param(cmd, "@buscar", SqlDbType.NVarChar, buscar, 80);
                    using (var r = cmd.ExecuteReader()) while (r.Read()) resultado.Usuarios.Add(Usuario(r));
                }
                tx.Commit();
            }
            return resultado;
        }

        public PilotoAdminEdicion Obtener(string actor, long usuarioId)
        {
            if (usuarioId <= 0) throw new PilotoException(404, "Usuario no disponible.");
            var resultado = new PilotoAdminEdicion();
            using (var cn = Abrir())
            using (var tx = cn.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                Autorizar(cn, tx, actor); ExigirInstalacion(cn, tx);
                resultado.Usuario = LeerUsuario(cn, tx, usuarioId);
                if (resultado.Usuario == null) throw new PilotoException(404, "Usuario no disponible.");
                resultado.Formulario = new PilotoAdminGuardar {
                    UsuarioId = usuarioId, EmpleadoRowId = resultado.Usuario.EmpleadoRowId,
                    CodigoOperador = resultado.Usuario.CodigoOperador, Activo = resultado.Usuario.VinculoActivo == true,
                    Centros = LeerCodigos(cn, tx, "SELECT Centro_Dist Codigo FROM dbo.PilotoCentro WHERE Usuario_Id=@u ORDER BY Centro_Dist;", usuarioId),
                    Placas = LeerCodigos(cn, tx, "SELECT Placa Codigo FROM dbo.PilotoVehiculo WHERE Usuario_Id=@u AND Activo=1 ORDER BY Placa;", usuarioId)
                };
                CargarCatalogos(cn, tx, resultado);
                CargarRutas(cn, tx, resultado);
                tx.Commit();
            }
            return resultado;
        }

        private PilotoAdminUsuario LeerUsuario(SqlConnection cn, SqlTransaction tx, long usuarioId)
        {
            var sql = @"SELECT u.Usuario_Id,u.Login,u.Nombre,u.Activo,u.Autenticar_Site,
 CAST(CASE WHEN EXISTS(SELECT 1 FROM dbo.Usuario_Rol ur JOIN dbo.Rol r ON r.Rol_Id=ur.Rol_Id WHERE ur.Usuario_Id=u.Usuario_Id AND r.Nombre=N'PILOTO') THEN 1 ELSE 0 END AS bit) TieneRolPiloto,
 p.Activo VinculoActivo,p.Empleado_RowId,p.Codigo_Operador,e.NOMBRE Piloto,
 (SELECT COUNT(*) FROM dbo.PilotoCentro c WHERE c.Usuario_Id=u.Usuario_Id) Centros,
 (SELECT COUNT(*) FROM dbo.PilotoVehiculo v WHERE v.Usuario_Id=u.Usuario_Id AND v.Activo=1) Vehiculos
FROM dbo.Usuario u LEFT JOIN dbo.PilotoVinculo p ON p.Usuario_Id=u.Usuario_Id
LEFT JOIN " + apk + @".dbo.RT_EMPLEADOS e ON e.ROWID=p.Empleado_RowId WHERE u.Usuario_Id=@u;";
            using (var cmd = Comando(cn, tx, sql))
            {
                Param(cmd, "@u", SqlDbType.BigInt, usuarioId);
                using (var r = cmd.ExecuteReader()) return r.Read() ? Usuario(r) : null;
            }
        }

        private static PilotoAdminUsuario Usuario(SqlDataReader r)
        {
            return new PilotoAdminUsuario {
                UsuarioId = (long)r["Usuario_Id"], Login = Texto(r, "Login"), Nombre = Texto(r, "Nombre"),
                UsuarioActivo = (bool)r["Activo"], AutenticarSite = (bool)r["Autenticar_Site"], TieneRolPiloto = (bool)r["TieneRolPiloto"],
                VinculoActivo = r["VinculoActivo"] == DBNull.Value ? (bool?)null : (bool)r["VinculoActivo"],
                EmpleadoRowId = r["Empleado_RowId"] == DBNull.Value ? (int?)null : (int)r["Empleado_RowId"],
                CodigoOperador = Texto(r, "Codigo_Operador"), Piloto = Texto(r, "Piloto"),
                Centros = (int)r["Centros"], Vehiculos = (int)r["Vehiculos"]
            };
        }

        private static string[] LeerCodigos(SqlConnection cn, SqlTransaction tx, string sql, long usuarioId)
        {
            var valores = new List<string>();
            using (var cmd = Comando(cn, tx, sql))
            {
                Param(cmd, "@u", SqlDbType.BigInt, usuarioId);
                using (var r = cmd.ExecuteReader()) while (r.Read()) valores.Add(Texto(r, "Codigo"));
            }
            return valores.ToArray();
        }

        private void CargarCatalogos(SqlConnection cn, SqlTransaction tx, PilotoAdminEdicion modelo)
        {
            var empleadoSql = @"SELECT TOP (500) e.ROWID,e.NOMBRE,e.ESTADO FROM " + apk + @".dbo.RT_EMPLEADOS e
WHERE e.CARGO=N'PILOTO' AND NULLIF(LTRIM(RTRIM(e.NOMBRE)),N'') IS NOT NULL
AND (e.ROWID=@actual OR (e.ESTADO=1 AND NOT EXISTS(SELECT 1 FROM " + apk + @".dbo.RT_EMPLEADOS otro WHERE otro.CARGO=N'PILOTO' AND otro.NOMBRE=e.NOMBRE AND otro.ROWID<>e.ROWID)))
ORDER BY CASE WHEN e.ROWID=@actual THEN 0 ELSE 1 END,e.NOMBRE;";
            using (var cmd = Comando(cn, tx, empleadoSql))
            {
                Param(cmd, "@actual", SqlDbType.Int, modelo.Formulario.EmpleadoRowId);
                using (var r = cmd.ExecuteReader()) while (r.Read()) modelo.Empleados.Add(new PilotoAdminEmpleado {
                    RowId = (int)r["ROWID"], Nombre = Texto(r, "NOMBRE"), Activo = r["ESTADO"] != DBNull.Value && Convert.ToBoolean(r["ESTADO"], CultureInfo.InvariantCulture), Seleccionado = modelo.Formulario.EmpleadoRowId == (int)r["ROWID"] });
            }
            using (var cmd = Comando(cn, tx, "SELECT DISTINCT TOP (200) CENTRO_DIST Codigo FROM " + apk + ".dbo.RT_RUTAS WHERE NULLIF(LTRIM(RTRIM(CENTRO_DIST)),N'') IS NOT NULL ORDER BY CENTRO_DIST;"))
            using (var r = cmd.ExecuteReader()) while (r.Read())
            {
                var codigo = Texto(r, "Codigo"); modelo.Centros.Add(new PilotoAdminOpcion { Codigo = codigo, Nombre = codigo, Activo = true, Seleccionado = modelo.Formulario.Centros.Contains(codigo, StringComparer.OrdinalIgnoreCase) });
            }
            var vehiculoSql = @"SELECT TOP (500) v.PLACA,v.ESTADO,v.MARCA,v.LINEA,v.TIPO,v.EMPRESA FROM " + apk + @".dbo.RT_VEHICULOS v
WHERE v.ESTADO=1 OR EXISTS(SELECT 1 FROM dbo.PilotoVehiculo pv WHERE pv.Usuario_Id=@u AND pv.Placa=v.PLACA COLLATE DATABASE_DEFAULT)
ORDER BY CASE WHEN EXISTS(SELECT 1 FROM dbo.PilotoVehiculo pv WHERE pv.Usuario_Id=@u AND pv.Placa=v.PLACA COLLATE DATABASE_DEFAULT AND pv.Activo=1) THEN 0 ELSE 1 END,v.PLACA;";
            using (var cmd = Comando(cn, tx, vehiculoSql))
            {
                Param(cmd, "@u", SqlDbType.BigInt, modelo.Usuario.UsuarioId);
                using (var r = cmd.ExecuteReader()) while (r.Read())
                {
                    var placa = Texto(r, "PLACA");
                    var detalle = string.Join(" · ", new[] { Texto(r, "MARCA"), Texto(r, "LINEA"), Texto(r, "TIPO"), Texto(r, "EMPRESA") }.Where(v => !string.IsNullOrWhiteSpace(v)));
                    modelo.Vehiculos.Add(new PilotoAdminOpcion { Codigo = placa, Nombre = detalle, Activo = r["ESTADO"] != DBNull.Value && Convert.ToBoolean(r["ESTADO"], CultureInfo.InvariantCulture), Seleccionado = modelo.Formulario.Placas.Contains(placa, StringComparer.OrdinalIgnoreCase) });
                }
            }
        }

        private void CargarRutas(SqlConnection cn, SqlTransaction tx, PilotoAdminEdicion modelo)
        {
            if (!modelo.Usuario.Listo || !modelo.Usuario.EmpleadoRowId.HasValue || string.IsNullOrWhiteSpace(modelo.Usuario.Piloto)) return;
            var sql = @"SELECT TOP (10) r.ID_RUTA,r.FECHA_RUTA,r.PLACA,r.PILOTO,r.CENTRO_DIST,r.STATUS
FROM " + apk + @".dbo.RT_RUTAS r WHERE r.PILOTO=@piloto AND r.STATUS IN(N'A',N'E')
AND EXISTS(SELECT 1 FROM " + apk + @".dbo.RT_EMPLEADOS e WHERE e.ROWID=@empleado AND e.ESTADO=1 AND e.CARGO=N'PILOTO' AND e.NOMBRE=r.PILOTO
           AND NOT EXISTS(SELECT 1 FROM " + apk + @".dbo.RT_EMPLEADOS otro WHERE otro.CARGO=N'PILOTO' AND otro.NOMBRE=e.NOMBRE AND otro.ROWID<>e.ROWID))
AND EXISTS(SELECT 1 FROM dbo.PilotoCentro c WHERE c.Usuario_Id=@u AND c.Centro_Dist=r.CENTRO_DIST COLLATE DATABASE_DEFAULT)
AND EXISTS(SELECT 1 FROM dbo.PilotoVehiculo v WHERE v.Usuario_Id=@u AND v.Activo=1 AND v.Placa=r.PLACA COLLATE DATABASE_DEFAULT)
AND EXISTS(SELECT 1 FROM " + apk + @".dbo.RT_VEHICULOS rv WHERE rv.PLACA=r.PLACA AND rv.ESTADO=1)
ORDER BY CASE WHEN r.STATUS=N'E' THEN 0 ELSE 1 END,r.FECHA_RUTA DESC,r.ID_RUTA DESC;";
            using (var cmd = Comando(cn, tx, sql))
            {
                Param(cmd, "@piloto", SqlDbType.NVarChar, modelo.Usuario.Piloto, 90); Param(cmd, "@empleado", SqlDbType.Int, modelo.Usuario.EmpleadoRowId); Param(cmd, "@u", SqlDbType.BigInt, modelo.Usuario.UsuarioId);
                using (var r = cmd.ExecuteReader()) while (r.Read()) modelo.Rutas.Add(new PilotoRuta {
                    Id = Texto(r, "ID_RUTA"), Fecha = (DateTime)r["FECHA_RUTA"], Placa = Texto(r, "PLACA"), Piloto = Texto(r, "PILOTO"), Centro = Texto(r, "CENTRO_DIST"), Estado = Texto(r, "STATUS") });
            }
        }

        public void Guardar(string actor, PilotoAdminGuardar modelo)
        {
            PilotoAdminReglas.Validar(modelo);
            using (var cn = Abrir())
            using (var tx = cn.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    Autorizar(cn, tx, actor); ExigirInstalacion(cn, tx);
                    if (!modelo.Activo) PrepararDesactivacion(cn, tx, modelo);
                    ValidarDestino(cn, tx, modelo);
                    GuardarVinculo(cn, tx, actor, modelo);
                    GuardarCentros(cn, tx, actor, modelo);
                    GuardarVehiculos(cn, tx, actor, modelo);
                    tx.Commit();
                }
                catch (SqlException e)
                {
                    if (e.Number == 2601 || e.Number == 2627) throw new PilotoException(409, "El empleado o código de operador ya está activo para otro usuario.");
                    throw;
                }
            }
        }

        private void ValidarDestino(SqlConnection cn, SqlTransaction tx, PilotoAdminGuardar m)
        {
            const string usuarioSql = @"SELECT COUNT(*) FROM dbo.Usuario u WHERE u.Usuario_Id=@u
AND (@activo=0 OR ((u.Activo=1 AND u.Autenticar_Site=1) AND EXISTS(SELECT 1 FROM dbo.Usuario_Rol ur JOIN dbo.Rol r ON r.Rol_Id=ur.Rol_Id WHERE ur.Usuario_Id=u.Usuario_Id AND r.Nombre=N'PILOTO')));";
            using (var cmd = Comando(cn, tx, usuarioSql))
            {
                Param(cmd, "@u", SqlDbType.BigInt, m.UsuarioId); Param(cmd, "@activo", SqlDbType.Bit, m.Activo);
                if ((int)cmd.ExecuteScalar() != 1) throw new PilotoException(409, "El usuario debe tener rol PILOTO y, para activarlo, acceso web vigente.");
            }
            if (m.Activo)
            {
                var empleadoSql = @"SELECT COUNT(*) FROM " + apk + @".dbo.RT_EMPLEADOS e WHERE e.ROWID=@e AND e.CARGO=N'PILOTO'
AND NULLIF(LTRIM(RTRIM(e.NOMBRE)),N'') IS NOT NULL AND e.ESTADO=1
AND NOT EXISTS(SELECT 1 FROM " + apk + @".dbo.RT_EMPLEADOS otro WHERE otro.CARGO=N'PILOTO' AND otro.NOMBRE=e.NOMBRE AND otro.ROWID<>e.ROWID);";
                using (var cmd = Comando(cn, tx, empleadoSql))
                {
                    Param(cmd, "@e", SqlDbType.Int, m.EmpleadoRowId);
                    if ((int)cmd.ExecuteScalar() != 1) throw new PilotoException(409, "El empleado no está disponible, está inactivo o su nombre es ambiguo.");
                }
            }
            foreach (var centro in m.Activo ? m.Centros : new string[0])
                using (var cmd = Comando(cn, tx, "SELECT COUNT(*) FROM " + apk + ".dbo.RT_RUTAS WHERE CENTRO_DIST=@valor;"))
                { Param(cmd, "@valor", SqlDbType.NVarChar, centro, 15); if ((int)cmd.ExecuteScalar() == 0) throw new PilotoException(409, "Uno de los centros ya no está disponible."); }
            foreach (var placa in m.Activo ? m.Placas : new string[0])
                using (var cmd = Comando(cn, tx, "SELECT COUNT(*) FROM " + apk + ".dbo.RT_VEHICULOS WHERE PLACA=@valor AND (@activo=0 OR ESTADO=1);"))
                { Param(cmd, "@valor", SqlDbType.NVarChar, placa, 15); Param(cmd, "@activo", SqlDbType.Bit, m.Activo); if ((int)cmd.ExecuteScalar() != 1) throw new PilotoException(409, "Uno de los vehículos ya no está disponible."); }
        }

        private static void PrepararDesactivacion(SqlConnection cn, SqlTransaction tx, PilotoAdminGuardar m)
        {
            using (var cmd = Comando(cn, tx, "SELECT Empleado_RowId,Codigo_Operador FROM dbo.PilotoVinculo WITH(UPDLOCK,HOLDLOCK) WHERE Usuario_Id=@u;"))
            {
                Param(cmd, "@u", SqlDbType.BigInt, m.UsuarioId);
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) throw new PilotoException(409, "El usuario no tiene un vínculo que pueda desactivarse.");
                    m.EmpleadoRowId = (int)r["Empleado_RowId"];
                    m.CodigoOperador = Texto(r, "Codigo_Operador");
                }
            }
            m.Centros = LeerCodigos(cn, tx, "SELECT Centro_Dist Codigo FROM dbo.PilotoCentro WITH(UPDLOCK,HOLDLOCK) WHERE Usuario_Id=@u;", m.UsuarioId);
            m.Placas = LeerCodigos(cn, tx, "SELECT Placa Codigo FROM dbo.PilotoVehiculo WITH(UPDLOCK,HOLDLOCK) WHERE Usuario_Id=@u AND Activo=1;", m.UsuarioId);
        }

        private static void GuardarVinculo(SqlConnection cn, SqlTransaction tx, string actor, PilotoAdminGuardar m)
        {
            int? anteriorEmpleado = null; string anteriorOperador = null; bool? anteriorActivo = null;
            using (var cmd = Comando(cn, tx, "SELECT Empleado_RowId,Codigo_Operador,Activo FROM dbo.PilotoVinculo WITH(UPDLOCK,HOLDLOCK) WHERE Usuario_Id=@u;"))
            {
                Param(cmd, "@u", SqlDbType.BigInt, m.UsuarioId);
                using (var r = cmd.ExecuteReader()) if (r.Read()) { anteriorEmpleado = (int)r["Empleado_RowId"]; anteriorOperador = Texto(r, "Codigo_Operador"); anteriorActivo = (bool)r["Activo"]; }
            }
            var cambio = !anteriorEmpleado.HasValue || anteriorEmpleado != m.EmpleadoRowId || !string.Equals(anteriorOperador, m.CodigoOperador, StringComparison.Ordinal) || anteriorActivo != m.Activo;
            if (!cambio) return;
            if (!anteriorEmpleado.HasValue)
                using (var cmd = Comando(cn, tx, "INSERT dbo.PilotoVinculo(Usuario_Id,Empleado_RowId,Codigo_Operador,Activo,ModificadoUtc,ModificadoPor) VALUES(@u,@e,@op,@activo,SYSUTCDATETIME(),@actor);"))
                { ParametrosVinculo(cmd, actor, m); cmd.ExecuteNonQuery(); }
            else
                using (var cmd = Comando(cn, tx, "UPDATE dbo.PilotoVinculo SET Empleado_RowId=@e,Codigo_Operador=@op,Activo=@activo,ModificadoUtc=SYSUTCDATETIME(),ModificadoPor=@actor WHERE Usuario_Id=@u;"))
                { ParametrosVinculo(cmd, actor, m); cmd.ExecuteNonQuery(); }
            using (var cmd = Comando(cn, tx, @"INSERT dbo.PilotoVinculoHistorial(Usuario_Id,EmpleadoAnterior,EmpleadoNuevo,OperadorAnterior,OperadorNuevo,ActivoAnterior,ActivoNuevo,Motivo,Actor)
VALUES(@u,@ea,@en,@oa,@on,@aa,@an,@motivo,@actor);"))
            {
                Param(cmd, "@u", SqlDbType.BigInt, m.UsuarioId); Param(cmd, "@ea", SqlDbType.Int, anteriorEmpleado); Param(cmd, "@en", SqlDbType.Int, m.EmpleadoRowId);
                Param(cmd, "@oa", SqlDbType.NVarChar, anteriorOperador, 15); Param(cmd, "@on", SqlDbType.NVarChar, m.CodigoOperador, 15);
                Param(cmd, "@aa", SqlDbType.Bit, anteriorActivo); Param(cmd, "@an", SqlDbType.Bit, m.Activo); Param(cmd, "@motivo", SqlDbType.NVarChar, m.Motivo, 250); Param(cmd, "@actor", SqlDbType.NVarChar, actor, 128); cmd.ExecuteNonQuery();
            }
        }

        private static void ParametrosVinculo(SqlCommand cmd, string actor, PilotoAdminGuardar m)
        {
            Param(cmd, "@u", SqlDbType.BigInt, m.UsuarioId); Param(cmd, "@e", SqlDbType.Int, m.EmpleadoRowId);
            Param(cmd, "@op", SqlDbType.NVarChar, m.CodigoOperador, 15); Param(cmd, "@activo", SqlDbType.Bit, m.Activo); Param(cmd, "@actor", SqlDbType.NVarChar, actor, 128);
        }

        private static void GuardarCentros(SqlConnection cn, SqlTransaction tx, string actor, PilotoAdminGuardar m)
        {
            var actuales = LeerCodigos(cn, tx, "SELECT Centro_Dist Codigo FROM dbo.PilotoCentro WITH(UPDLOCK,HOLDLOCK) WHERE Usuario_Id=@u;", m.UsuarioId);
            foreach (var centro in actuales.Where(v => !m.Centros.Contains(v, StringComparer.OrdinalIgnoreCase)))
            {
                using (var cmd = Comando(cn, tx, "DELETE dbo.PilotoCentro WHERE Usuario_Id=@u AND Centro_Dist=@codigo;"))
                { Param(cmd, "@u", SqlDbType.BigInt, m.UsuarioId); Param(cmd, "@codigo", SqlDbType.NVarChar, centro, 15); cmd.ExecuteNonQuery(); }
                HistorialCentro(cn, tx, actor, m, centro, false);
            }
            foreach (var centro in m.Centros.Where(v => !actuales.Contains(v, StringComparer.OrdinalIgnoreCase)))
            {
                using (var cmd = Comando(cn, tx, "INSERT dbo.PilotoCentro(Usuario_Id,Centro_Dist) VALUES(@u,@codigo);"))
                { Param(cmd, "@u", SqlDbType.BigInt, m.UsuarioId); Param(cmd, "@codigo", SqlDbType.NVarChar, centro, 15); cmd.ExecuteNonQuery(); }
                HistorialCentro(cn, tx, actor, m, centro, true);
            }
        }

        private static void HistorialCentro(SqlConnection cn, SqlTransaction tx, string actor, PilotoAdminGuardar m, string centro, bool activo)
        {
            using (var cmd = Comando(cn, tx, "INSERT dbo.PilotoCentroHistorial(Usuario_Id,Centro_Dist,ActivoNuevo,Motivo,Actor) VALUES(@u,@centro,@activo,@motivo,@actor);"))
            { Param(cmd, "@u", SqlDbType.BigInt, m.UsuarioId); Param(cmd, "@centro", SqlDbType.NVarChar, centro, 15); Param(cmd, "@activo", SqlDbType.Bit, activo); Param(cmd, "@motivo", SqlDbType.NVarChar, m.Motivo, 250); Param(cmd, "@actor", SqlDbType.NVarChar, actor, 128); cmd.ExecuteNonQuery(); }
        }

        private static void GuardarVehiculos(SqlConnection cn, SqlTransaction tx, string actor, PilotoAdminGuardar m)
        {
            var actuales = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            using (var cmd = Comando(cn, tx, "SELECT Placa,Activo FROM dbo.PilotoVehiculo WITH(UPDLOCK,HOLDLOCK) WHERE Usuario_Id=@u;"))
            {
                Param(cmd, "@u", SqlDbType.BigInt, m.UsuarioId);
                using (var r = cmd.ExecuteReader()) while (r.Read()) actuales[Texto(r, "Placa")] = (bool)r["Activo"];
            }
            var codigos = actuales.Keys.Union(m.Placas, StringComparer.OrdinalIgnoreCase).ToArray();
            foreach (var placa in codigos)
            {
                bool antes; var existe = actuales.TryGetValue(placa, out antes); var nuevo = m.Activo && m.Placas.Contains(placa, StringComparer.OrdinalIgnoreCase);
                if (existe && antes == nuevo) continue;
                if (!existe)
                    using (var cmd = Comando(cn, tx, "INSERT dbo.PilotoVehiculo(Usuario_Id,Placa,Activo,ModificadoUtc,ModificadoPor) VALUES(@u,@placa,@activo,SYSUTCDATETIME(),@actor);"))
                    { ParametrosVehiculo(cmd, actor, m.UsuarioId, placa, nuevo); cmd.ExecuteNonQuery(); }
                else
                    using (var cmd = Comando(cn, tx, "UPDATE dbo.PilotoVehiculo SET Activo=@activo,ModificadoUtc=SYSUTCDATETIME(),ModificadoPor=@actor WHERE Usuario_Id=@u AND Placa=@placa;"))
                    { ParametrosVehiculo(cmd, actor, m.UsuarioId, placa, nuevo); cmd.ExecuteNonQuery(); }
                using (var cmd = Comando(cn, tx, "INSERT dbo.PilotoVehiculoHistorial(Usuario_Id,Placa,ActivoAnterior,ActivoNuevo,Motivo,Actor) VALUES(@u,@placa,@antes,@nuevo,@motivo,@actor);"))
                {
                    Param(cmd, "@u", SqlDbType.BigInt, m.UsuarioId); Param(cmd, "@placa", SqlDbType.NVarChar, placa, 15); Param(cmd, "@antes", SqlDbType.Bit, existe ? (object)antes : null);
                    Param(cmd, "@nuevo", SqlDbType.Bit, nuevo); Param(cmd, "@motivo", SqlDbType.NVarChar, m.Motivo, 250); Param(cmd, "@actor", SqlDbType.NVarChar, actor, 128); cmd.ExecuteNonQuery();
                }
            }
        }

        private static void ParametrosVehiculo(SqlCommand cmd, string actor, long usuarioId, string placa, bool activo)
        {
            Param(cmd, "@u", SqlDbType.BigInt, usuarioId); Param(cmd, "@placa", SqlDbType.NVarChar, placa, 15);
            Param(cmd, "@activo", SqlDbType.Bit, activo); Param(cmd, "@actor", SqlDbType.NVarChar, actor, 128);
        }
    }
}
