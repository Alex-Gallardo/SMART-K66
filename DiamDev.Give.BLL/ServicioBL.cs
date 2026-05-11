using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class ServicioBL
    {

        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public ServicioBL()
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

                    Servicio ServicioActual = db.Set<Servicio>().AsNoTracking().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (ServicioActual != null)
                    {
                        Inicial_Id = ServicioActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;

                }
                catch (Exception)
                {
                }

                return Id;
            }

            private bool Agregar(Servicio entidad)
            {
                bool ServicioAgregar = false;

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {

                        long lngServicioId = new Herramienta().Formato_Correlativo(Id);

                        if (lngServicioId > 0)
                        {
                            entidad.ServicioId = lngServicioId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            db.Set<Servicio>().Add(entidad);
                            db.SaveChanges();
                            ServicioAgregar = true;
                        }
                    }

                }
                catch (Exception)
                {
                }

                return ServicioAgregar;
            }

            private bool Actualizar(Servicio entidad)
            {
                bool ServicioActualizar = false;

                try
                {

                    Servicio ServicioActual = ObtenerPorId(entidad.ServicioId);

                    if (ServicioActual.ServicioId > 0)
                    {

                        ServicioActual.Nombre = entidad.Nombre;
                        ServicioActual.Activo = entidad.Activo;

                        db.SaveChanges();
                        ServicioActualizar = true;
                    }

                }
                catch (Exception)
                {
                }

                return ServicioActualizar;
            }


        #endregion

        #region Metodos Publicos

            public string Guardar(Servicio entidad)
            {
                string Mensaje = "OK";
                bool OperacionExitosa = false;

                if (entidad.ServicioId > 0)
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

            public Servicio ObtenerPorId(long id)
            {
                Servicio ServicioActual = new Servicio();

                try
                {
                    ServicioActual = db.Set<Servicio>().Where(x => x.ServicioId == id).FirstOrDefault();
                }
                catch (Exception)
                {
                }

                return ServicioActual;
            }

            public List<Servicio> ObtenerListado(bool todos)
            {
                List<Servicio> Servicios = new List<Servicio>();

                try
                {
                    if (todos)
                    {
                        Servicios = db.Set<Servicio>().AsNoTracking().OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ServicioId).ToList();
                    }
                    else
                    {
                        Servicios = db.Set<Servicio>().AsNoTracking().Where(x => x.Activo == true).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ServicioId).ToList();
                    }
                }
                catch (Exception)
                {
                }

                return Servicios;
            }

        #endregion

    }
}
