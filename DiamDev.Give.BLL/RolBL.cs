using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            private bool Agregar(Rol entidad)
            {
                bool RolAgregar = false;

                try
                {
                    db.Set<Rol>().Add(entidad);
                    db.SaveChanges();

                    RolAgregar = true;
                }
                catch (Exception)
                {
                }

                return RolAgregar;
            }

            private bool Actualizar(Rol entidad)
            {
                bool RolActualizar = false;

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
                                if (!RolActual.Permisos.Any(x => x.PermisoId.Equals(Permiso.PermisoId)))
                                {
                                    RolActual.Permisos.Add(new RolPermiso() { RolId = RolActual.RolId, PermisoId = Permiso.PermisoId });
                                }
                            }

                        }

                        db.SaveChanges();
                        RolActualizar = true;
                    }

                }
                catch (Exception)
                {
                }

                return RolActualizar;
            }

        #endregion

        #region Metodos Publicos

            public string Guardar(Rol entidad)
            {
                string Mensaje = "OK";
                bool OperacionExitosa = false;

                if (entidad.RolId > 0)
                {
                    OperacionExitosa = Actualizar(entidad);
                }
                else
                {
                    OperacionExitosa = Agregar(entidad);
                }

                if (!OperacionExitosa)
                {
                    Mensaje = "La información ingresada no es valida";
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
