using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class AgenciaBL
    {

        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public AgenciaBL()
            {
                this.db = new GiveContext();
            }

        #endregion

        #region Metodos Privados

            private int Correlativo()
            {
                int Id = 0;

                try
                {
                    Agencia AgenciaActual = db.Set<Agencia>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (AgenciaActual != null)
                    {
                        Inicial_Id = AgenciaActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;
                }
                catch (Exception)
                {
                }

                return Id;
            }

            private bool Agregar(Agencia entidad)
            {
                bool AgenciaAgregar = false;

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngAgenciaId = new Herramienta().Formato_Correlativo(Id);

                        if (lngAgenciaId > 0)
                        {
                            entidad.AgenciaId = lngAgenciaId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            db.Set<Agencia>().Add(entidad);
                            db.SaveChanges();
                            AgenciaAgregar = true;
                        }
                    }
                }
                catch (Exception)
                {
                }

                return AgenciaAgregar;
            }

            private bool Actualizar(Agencia entidad)
            {
                bool AgenciaActualizar = false;

                try
                {
                    Agencia AgenciaActual = ObtenerPorId(entidad.AgenciaId);

                    if (AgenciaActual.AgenciaId > 0)
                    {
                        AgenciaActual.Nombre = entidad.Nombre;
                        AgenciaActual.Activo = entidad.Activo;

                        db.SaveChanges();
                        AgenciaActualizar = true;
                    }
                }
                catch (Exception)
                {
                }

                return AgenciaActualizar;
            }


        #endregion

        #region Metodos Publicos

            public string Guardar(Agencia entidad)
            {
                string Mensaje = "OK";
                bool OperacionExitosa = false;

                if (entidad.AgenciaId > 0)
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

            public Agencia ObtenerPorId(long id)
            {
                Agencia AgenciaActual = new Agencia();

                try
                {
                    AgenciaActual = db.Set<Agencia>().Where(x => x.AgenciaId == id).FirstOrDefault();
                }
                catch (Exception)
                {
                }

                return AgenciaActual;
            }

            public List<Agencia> ObtenerListado(bool todos, long usuarioId = 0)
            {
                List<Agencia> Agencias = new List<Agencia>();

                try
                {
                    if (todos)
                    {
                        if (usuarioId == 0)
                        {
                            Agencias = db.Set<Agencia>().OrderByDescending(x => x.Fecha).ThenByDescending(x => x.AgenciaId).Take(200).ToList();
                        }
                        else
                        {
                            List<long> AgenciasIds = db.Set<UsuarioAgencia>().Where(x => x.UsuarioId == usuarioId).Select(x => x.AgenciaId).ToList();
                            if (AgenciasIds != null && AgenciasIds.Count() > 0)
                            {
                                Agencias = db.Set<Agencia>().Where(x => AgenciasIds.Contains(x.AgenciaId)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.AgenciaId).Take(200).ToList();
                            }
                        }
                    }
                    else
                    {
                        Agencias = db.Set<Agencia>().Where(x => x.Activo == true).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.AgenciaId).ToList();
                    }
                }
                catch (Exception)
                {
                }

                return Agencias;
            }

            public List<Agencia> Buscar(string search, long usuarioId = 0)
            {
                List<Agencia> Agencias = new List<Agencia>();

                try
                {
                    if (usuarioId == 0)
                    {
                        Agencias = db.Set<Agencia>().Where(x => x.Nombre.Contains(search)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.AgenciaId).Take(200).ToList();
                    }
                    else
                    {
                        List<long> AgenciasIds = db.Set<UsuarioAgencia>().Where(x => x.UsuarioId == usuarioId).Select(x => x.AgenciaId).ToList();
                        if (AgenciasIds != null && AgenciasIds.Count() > 0)
                        {
                            Agencias = db.Set<Agencia>().Where(x => (AgenciasIds.Contains(x.AgenciaId)) && (x.Nombre.Contains(search))).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.AgenciaId).Take(200).ToList();
                        }
                    }
                }
                catch (Exception)
                {
                }

                return Agencias;
            }

            public List<Agencia> ObtenerListadoPorAgencia(long agenciaId)
            {
                List<Agencia> Agencias = new List<Agencia>();

                try
                {
                    Agencias = db.Set<Agencia>().Where(x => (x.Activo == true) && !(x.AgenciaId == agenciaId)).ToList();
                }
                catch (Exception)
                {
                }

                return Agencias;
            }

        #endregion

    }
}
