using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using Sistema.Seguridad;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class UsuarioBL
    {

        #region Variables Globales

        private GiveContext db;

        #endregion

        #region Constructores

        public UsuarioBL()
        {
            this.db = new GiveContext();
        }

        #endregion

        #region Metodos Privados

        private string Key(string key)
        {
            return Criptografia.Base64StringAHexString(Criptografia.EncriptarSha512(key));
        }

        private string Concat_Usuario(string Usuario, string Password)
        {
            return string.Concat(Usuario, Password, Usuario);
        }

        private int Correlativo()
        {
            int Id = 0;

            try
            {

                Usuario UsuarioActual = db.Set<Usuario>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                int Inicial_Id = 1;

                if (UsuarioActual != null)
                {
                    Inicial_Id = UsuarioActual.Correlativo + 1;
                }

                Id = Inicial_Id;

            }
            catch (Exception)
            {
            }

            return Id;
        }

        private bool ExisteLogin(string Login)
        {
            bool ExisteLogin = false;

            try
            {
                ExisteLogin = db.Set<Usuario>().Where(x => x.Login.Equals(Login)).Count() > 0;
            }
            catch (Exception)
            {
            }

            return ExisteLogin;
        }

        private bool Agregar(Usuario entidad, bool correo)
        {
            bool UsuarioAgregar = false;

            try
            {
                int Id = Correlativo();

                if (Id > 0)
                {
                    string concat = Concat_Usuario(entidad.Login, entidad.Password);
                    long lngUsuarioId = new Herramienta().Formato_Correlativo(Id);

                    if (lngUsuarioId > 0)
                    {
                        entidad.UsuarioId = lngUsuarioId;
                        entidad.Correlativo = Id;
                        entidad.Fecha = DateTime.Today;
                        entidad.NuevoPassword = entidad.Password;
                        entidad.Password = Key(concat);
                        entidad.PasswordAndroid = Herramienta.Key_Android(concat);

                        if (entidad.Roles != null && entidad.Roles.Count() > 0)
                        {
                            foreach (var Rol in entidad.Roles)
                            {
                                Rol.UsuarioId = entidad.UsuarioId;
                            }
                        }

                        if (entidad.Agencias != null && entidad.Agencias.Count() > 0)
                        {
                            foreach (var Centro in entidad.Agencias)
                            {
                                Centro.UsuarioId = entidad.UsuarioId;
                            }
                        }

                        db.Set<Usuario>().Add(entidad);
                        db.SaveChanges();
                        UsuarioAgregar = true;
                    }
                }

            }
            catch (Exception)
            {
            }

            return UsuarioAgregar;
        }

        private bool Actualizar(Usuario entidad)
        {
            bool UsuarioActualizar = false;

            try
            {

                Usuario UsuarioActual = ObtenerPorId(entidad.UsuarioId, false);

                if (UsuarioActual.UsuarioId > 0)
                {

                    string concat = Concat_Usuario(entidad.Login, entidad.Password);

                    UsuarioActual.Nombre = entidad.Nombre;
                    UsuarioActual.Password = Key(concat);
                    UsuarioActual.PasswordAndroid = Herramienta.Key_Android(concat);
                    UsuarioActual.AutenticarSite = entidad.AutenticarSite;
                    UsuarioActual.AutenticarAndroid = entidad.AutenticarAndroid;
                    UsuarioActual.ReiniciarPassword = entidad.ReiniciarPassword;
                    UsuarioActual.Activo = entidad.Activo;

                    if (entidad.Roles != null && entidad.Roles.Count() > 0)
                    {
                        //Eliminar por usuarioId                          
                        var Roles = db.Set<UsuarioRol>().Where(x => x.UsuarioId == UsuarioActual.UsuarioId).ToList();
                        db.Set<UsuarioRol>().RemoveRange(Roles);

                        //Agregar los nuevos roles
                        UsuarioActual.Roles = new List<UsuarioRol>();

                        foreach (var Rol in entidad.Roles)
                        {
                            if (!UsuarioActual.Roles.Any(x => x.RolId == Rol.RolId))
                            {
                                UsuarioActual.Roles.Add(new UsuarioRol() { UsuarioId = UsuarioActual.UsuarioId, RolId = Rol.RolId });
                            }
                        }

                    }

                    if (entidad.Agencias != null && entidad.Agencias.Count() > 0)
                    {
                        //Eliminar por usuarioId                          
                        var Agencias = db.Set<UsuarioAgencia>().Where(x => x.UsuarioId == UsuarioActual.UsuarioId).ToList();
                        db.Set<UsuarioAgencia>().RemoveRange(Agencias);

                        //Agregar las nuevas agencias
                        UsuarioActual.Agencias = new List<UsuarioAgencia>();

                        foreach (var Agencia in entidad.Agencias)
                        {
                            if (!UsuarioActual.Agencias.Any(x => x.AgenciaId == Agencia.AgenciaId))
                            {
                                UsuarioActual.Agencias.Add(new UsuarioAgencia() { UsuarioId = UsuarioActual.UsuarioId, AgenciaId = Agencia.AgenciaId });
                            }
                        }
                    }

                    db.SaveChanges();
                    UsuarioActualizar = true;
                }

            }
            catch (Exception)
            {
            }

            return UsuarioActualizar;
        }

        private void ActualizarUltimaActividad(Usuario entidad)
        {

            try
            {

                Usuario UsuarioActual = ObtenerPorId(entidad.UsuarioId, false);

                if (UsuarioActual.UsuarioId > 0)
                {
                    UsuarioActual.FechaUltimaActividad = entidad.FechaUltimaActividad;
                    db.SaveChanges();
                }

            }
            catch (Exception)
            {
            }

        }

        #endregion

        #region Metodos Publicos

        public string ValidarUsuario(string usuario, string Encriptado_password, string password, bool Servicio = false)
        {

            string Mensaje = "OK";

            var Usuario = ObtenerPorLogin(usuario);

            if (Usuario == null)
            {
                return "El usuario que ingreso no se encuentra registrado";
            }

            if (Usuario.UsuarioId == 0)
            {
                return "El usuario que ingreso no se encuentra registrado";
            }

            if (!Usuario.Activo)
            {
                return "El usuario que ingreso no se encuentra activo";
            }

            bool Password_Invalido = false;

            if (Servicio)
            {
                if (!Usuario.AutenticarAndroid)
                {
                    return "El usuario que ingreso no tiene acceso para autenticarse en el dispositivo";
                }

                if (Usuario.PasswordAndroid == password)
                {
                    Password_Invalido = true;
                }
            }
            else
            {
                if (!Usuario.AutenticarSite)
                {
                    return "El usuario que ingreso no tiene acceso para autenticarse en el site";
                }

                if (Usuario.Password != Encriptado_password)
                {
                    return "El usuario o password están incorrectos";
                }
                else
                {
                    Password_Invalido = true;
                }
            }

            if (!Password_Invalido)
            {
                return "El usuario o password están incorrectos";
            }
            else
            {
                Usuario.FechaUltimaActividad = DateTime.Now;
                ActualizarUltimaActividad(Usuario);
            }

            return Mensaje;
        }

        public string Guardar(Usuario entidad, bool correo = false)
        {
            string Mensaje = "OK";
            bool OperacionExitosa = false;

            if (entidad.UsuarioId > 0)
            {
                OperacionExitosa = Actualizar(entidad);
            }
            else
            {
                if (!ExisteLogin(entidad.Login))
                {
                    OperacionExitosa = Agregar(entidad, correo);
                }
                else
                {
                    Mensaje = "El usuario que ingreso ya existe en el sistema";
                }
            }

            if (!OperacionExitosa)
            {
                Mensaje = "La información ingresada no es valida";
            }

            return Mensaje;
        }

        public string DesactivarUsuario(int usuarioId)
        {
            string Mensaje = "OK";

            try
            {

                Usuario UsuarioActual = ObtenerPorId(usuarioId, false);

                if (UsuarioActual == null)
                {
                    return "El usuario que quiere desactivar no está registrado en el sistema";
                }

                if (UsuarioActual.UsuarioId == 0)
                {
                    return "El usuario que quiere desactivar no está registrado en el sistema";
                }

                UsuarioActual.Activo = false;
                db.SaveChanges();

            }
            catch (Exception)
            {
            }

            return Mensaje;
        }

        public string ActualizarPassword(Usuario entidad)
        {
            string Mensaje = "OK";

            try
            {

                Usuario UsuarioActual = ObtenerPorId(entidad.UsuarioId, false);

                if (UsuarioActual == null)
                {
                    return "El usuario que quiere actualizar el password no está registrado en el sistema";
                }

                if (UsuarioActual.UsuarioId == 0)
                {
                    return "El usuario que quiere actualizar el password no está registrado en el sistema";
                }

                string concat = Concat_Usuario(entidad.Login, entidad.Password);

                UsuarioActual.Password = Key(concat);
                UsuarioActual.ReiniciarPassword = false;

                db.SaveChanges();

            }
            catch (Exception)
            {
            }

            return Mensaje;
        }

        public Usuario ObtenerPorLogin(string usuario)
        {
            Usuario UsuarioActual = new Usuario();

            try
            {
                UsuarioActual = db.Set<Usuario>().Include("Roles").Include("Agencias").Include("Agencias.Agencia").Where(x => x.Login.Equals(usuario)).FirstOrDefault();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Ocurrió un error: {0}", ex);
            }

            return UsuarioActual;
        }

        public Usuario ObtenerPorId(long id, bool todos)
        {
            Usuario UsuarioActual = new Usuario();

            try
            {
                if (todos)
                {
                    UsuarioActual = db.Set<Usuario>().Include("Roles").Include("Agencias").Where(x => x.UsuarioId == id).FirstOrDefault();
                }
                else
                {
                    UsuarioActual = db.Set<Usuario>().Where(x => x.UsuarioId == id).FirstOrDefault();
                }
            }
            catch (Exception)
            {
            }

            return UsuarioActual;
        }

        public Usuario ObtenerUsuarioConRol(string usuario)
        {
            Usuario UsuarioActual = new Usuario();

            try
            {

                UsuarioActual = db.Set<Usuario>().Include("Roles").Include("Agencias").Where(x => x.Login.Equals(usuario)).FirstOrDefault();

                if (UsuarioActual != null)
                {
                    UsuarioActual.RolesPermiso = new RolBL().ObtenerPermisoPorUsuario(UsuarioActual.Login);
                }

            }
            catch (Exception)
            {
            }

            return UsuarioActual;
        }

        public List<Usuario> ObtenerListado()
        {
            List<Usuario> Usuarios = new List<Usuario>();

            try
            {
                Usuarios = db.Set<Usuario>().Include("Roles").Include("Agencias").OrderByDescending(x => x.Fecha).ThenByDescending(x => x.UsuarioId).ToList();
            }
            catch (Exception)
            {
            }

            return Usuarios;
        }

        public List<Usuario> Buscar(string Buscar)
        {
            List<Usuario> Usuarios = new List<Usuario>();

            try
            {
                Usuarios = db.Set<Usuario>().Include("Roles").Include("Agencias").Where(x => x.Nombre.Contains(Buscar)).ToList();
            }
            catch (Exception)
            {
            }

            return Usuarios;
        }

        #endregion

    }
}
