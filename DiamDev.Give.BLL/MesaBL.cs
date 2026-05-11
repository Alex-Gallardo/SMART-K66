using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class MesaBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public MesaBL()
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
                    Mesa MesaActual = db.Set<Mesa>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (MesaActual != null)
                    {
                        Inicial_Id = MesaActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;
                }
                catch (Exception)
                {}

                return Id;
            }

            private string Agregar(Mesa entidad)
            {
                string Mensaje = "OK";

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngMesaId = new Herramienta().Formato_Correlativo(Id);

                        if (lngMesaId > 0)
                        {
                            entidad.MesaId = lngMesaId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            db.Set<Mesa>().Add(entidad);
                            db.SaveChanges();                           
                        }
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            private string Actualizar(Mesa entidad)
            {
                string Mensaje = "OK";

                try
                {
                    Mesa MesaActual = ObtenerPorId(entidad.MesaId);

                    if (MesaActual.MesaId > 0)
                    {
                        MesaActual.TipoUbicacionId = entidad.TipoUbicacionId;
                        MesaActual.Nombre = entidad.Nombre;
                        MesaActual.Descripcion = entidad.Descripcion;                        
                        MesaActual.Activo = entidad.Activo;

                        db.SaveChanges();
                    }
                    else
                    {
                        Mensaje = "La mesa seleccionada no se encuentra con ID valido";
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

            public string Guardar(Mesa entidad)
            {
                string Mensaje = "OK";
               
                if (entidad.MesaId > 0)
                {
                    Mensaje = Actualizar(entidad);
                }
                else
                {
                    Mensaje = Agregar(entidad);
                }            

                return Mensaje;
            }

            public Mesa ObtenerPorId(long id)
            {
                Mesa MesaActual = new Mesa();

                try
                {
                    MesaActual = db.Set<Mesa>().Where(x => x.MesaId == id).FirstOrDefault();
                }
                catch (Exception)
                {}

                return MesaActual;
            }

            public List<Mesa> ObtenerListado(bool todos, long agenciaId)
            {
                List<Mesa> Mesas = new List<Mesa>();

                try
                {
                    if (todos)
                    {
                        Mesas = db.Set<Mesa>().Include("TipoUbicacion").AsNoTracking().Where(x => x.AgenciaId == agenciaId).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.MesaId).Take(200).ToList();
                    }
                    else
                    {
                        Mesas = db.Set<Mesa>().AsNoTracking().Where(x => x.Activo).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.MesaId).ToList();
                    }
                }
                catch (Exception)
                {}

                return Mesas;
            }

            public List<Mesa> Buscar(string search, long agenciaId)
            {
                List<Mesa> Mesas = new List<Mesa>();

                try
                {
                    Mesas = db.Set<Mesa>().Include("TipoUbicacion").AsNoTracking().Where(x => x.Nombre.Contains(search) && x.AgenciaId == agenciaId).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.MesaId).Take(200).ToList();
                }
                catch (Exception)
                {}

                return Mesas;
            }

        #endregion
    }
}
