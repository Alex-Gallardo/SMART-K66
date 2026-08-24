using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class RolBL
    {

        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public RolBL()
            {
                this.db = new GiveContext();
            }

        #endregion

        #region Metodos Privados

            private string Agregar(Rol entidad)
            {
                string Mensaje = "OK";

                try
                {
                    db.Set<Rol>().Add(entidad);
                    db.SaveChanges();                   
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            private string Actualizar(Rol entidad)
            {
                string Mensaje = "OK";

                try
                {

                    Rol RolActual = ObtenerPorId(entidad.RolId);

                    if (RolActual.RolId > 0)
                    {

                        RolActual.Nombre = entidad.Nombre;

                        if (RolActual.Permisos != null && RolActual.Permisos.Count() > 0)
                        {
                            //Eliminar permiso por rolId
                            var Permisos = db.Set<RolPermiso>().Where(x => x.RolId == RolActual.RolId).ToList();
                            db.Set<RolPermiso>().RemoveRange(Permisos);

                            //Agregar los nuevos permisos
                            RolActual.Permisos = new List<RolPermiso>();
                            foreach (var Permiso in entidad.Permisos)
                            {
                                db.Set<RolPermiso>().Add(new RolPermiso() { RolId = RolActual.RolId, PermisoId = Permiso.PermisoId });
                            }

                        }

                        db.SaveChanges();                       
                    }

                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

        #endregion

        #region Metodos Publicos

            public string Guardar(Rol entidad)
            {
                string Mensaje = "OK";
              
                if (entidad.RolId > 0)
                {
                    Mensaje = Actualizar(entidad);
                }
                else
                {
                    Mensaje = Agregar(entidad);
                }

                return Mensaje;
            }

            public Rol ObtenerPorId(int id)
            {
                Rol RolActual = new Rol();

                try
                {
                    RolActual = db.Set<Rol>().Include("Permisos").Where(x => x.RolId == id).FirstOrDefault();
                }
                catch (Exception)
                {
                }

                return RolActual;
            }

            public List<Rol> ObtenerListado()
            {
                List<Rol> Roles = new List<Rol>();

                try
                {
                    Roles = db.Set<Rol>().Include("Permisos").OrderByDescending(x => x.RolId).ToList();
                }
                catch (Exception)
                {
                }

                return Roles;
            }

            public List<RolPermiso> ObtenerPermisoPorRolId(int id)
            {
                List<RolPermiso> Permisos = new List<RolPermiso>();

                try
                {
                    Permisos = db.Set<RolPermiso>().Where(x => x.RolId == id).ToList();
                }
                catch (Exception)
                {
                }

                return Permisos;
            }

            public List<RolPermiso> ObtenerPermisoPorUsuario(string usuario)
            {
                List<RolPermiso> Permisos = new List<RolPermiso>();

                try
                {
                    Permisos = db.Set<Usuario>().Where(x => x.Login.Equals(usuario)).Join(db.Set<UsuarioRol>(), U => U.UsuarioId, UR => UR.UsuarioId, (U, UR) => new { Roles = UR }).Join(db.Set<RolPermiso>(), R => R.Roles.RolId, RP => RP.RolId, (R, RP) => new { Permisos = RP }).Select(x => x.Permisos).ToList();
                }
                catch (Exception)
                {
                }

                return Permisos;
            }

            public string ObtenerPermisoPorUsuario(long usuario)
            {
                Rol RolActual = new Rol();
                string Descripcion = string.Empty;

                try
                {
                    RolActual = db.Set<Usuario>().Where(x => x.UsuarioId == usuario).Join(db.Set<UsuarioRol>(), U => U.UsuarioId, UR => UR.UsuarioId, (U, UR) => new { Roles = UR }).Join(db.Set<Rol>(), R => R.Roles.RolId, RP => RP.RolId, (R, RP) => new { Permisos = RP }).Select(x => x.Permisos).FirstOrDefault();
                    if (RolActual != null)
                    {
                        Descripcion = RolActual.Nombre;                        
                    }
                }
                catch (Exception)
                {
                }

                return Descripcion;
            }

            public bool AutorizacionPermisoPorUsuario(string usuario, string permiso)
            {
                bool Autorizacion = false;

                try
                {
                    Autorizacion = db.Set<Usuario>().Where(x => x.Login.Equals(usuario)).Join(db.Set<UsuarioRol>(), U => U.UsuarioId, UR => UR.UsuarioId, (U, UR) => new { Roles = UR }).Join(db.Set<RolPermiso>(), R => R.Roles.RolId, RP => RP.RolId, (R, RP) => new { Permisos = RP }).Select(x => x.Permisos).Any(x => x.PermisoId.Equals(permiso));
                }
                catch (Exception)
                {
                }

                return Autorizacion;
            }

            /// <summary>
            /// Indica si el usuario pertenece al rol solicitado. A diferencia de
            /// ObtenerPermisoPorUsuario(long), revisa todos los roles asignados y no
            /// depende del orden en que SQL Server los devuelva.
            /// </summary>
            public bool UsuarioTieneRol(string usuario, string rol)
            {
                if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(rol))
                {
                    return false;
                }

                return db.Set<Usuario>()
                    .Where(x => x.Login.Equals(usuario))
                    .Join(db.Set<UsuarioRol>(), u => u.UsuarioId, ur => ur.UsuarioId,
                          (u, ur) => ur)
                    .Join(db.Set<Rol>(), ur => ur.RolId, r => r.RolId,
                          (ur, r) => r)
                    .Any(r => r.Nombre.Equals(rol));
            }

            public List<Rol> Buscar(string Buscar)
            {
                List<Rol> Roles = new List<Rol>();

                try
                {
                    Roles = db.Set<Rol>().Include("Permisos").Where(x => x.Nombre.Contains(Buscar)).ToList();
                }
                catch (Exception)
                {
                }

                return Roles;
            }

        #endregion

    }
}
