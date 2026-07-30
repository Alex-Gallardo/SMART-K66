using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class RegionBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public RegionBL()
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
                    Region RegionActual = db.Set<Region>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (RegionActual != null)
                    {
                        Inicial_Id = RegionActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;
                }
                catch (Exception)
                {
                }

                return Id;
            }

            private string Agregar(Region entidad)
            {
                string Mensaje = "OK";

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngRegionId = new Herramienta().Formato_Correlativo(Id);

                        if (lngRegionId > 0)
                        {
                            entidad.RegionId = lngRegionId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            db.Set<Region>().Add(entidad);
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

            private string Actualizar(Region entidad)
            {
                string Mensaje = "OK";

                try
                {
                    Region RegionActual = ObtenerPorId(entidad.RegionId);

                    if (RegionActual.RegionId > 0)
                    {
                        RegionActual.Nombre = entidad.Nombre;
                      
                        db.SaveChanges();
                    }
                    else
                    {
                        Mensaje = "La region seleccionada no se encuentra con ID valido";
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

            public string Guardar(Region entidad)
            {
                string Mensaje = "OK";
               
                if (entidad.RegionId > 0)
                {
                    Mensaje = Actualizar(entidad);
                }
                else
                {
                    Mensaje = Agregar(entidad);
                }            

                return Mensaje;
            }

            public Region ObtenerPorId(long id)
            {
                Region RegionActual = new Region();

                try
                {
                    RegionActual = db.Set<Region>().Where(x => x.RegionId == id).FirstOrDefault();
                }
                catch (Exception)
                {
                }

                return RegionActual;
            }

            public List<Region> ObtenerListado()
            {
                List<Region> Regiones = new List<Region>();

                try
                {
                    Regiones = db.Set<Region>().AsNoTracking().OrderByDescending(x => x.Fecha).ThenByDescending(x => x.RegionId).ToList();
                }
                catch (Exception)
                {
                }

                return Regiones;
            }

            public List<Region> Buscar(string search)
            {
                List<Region> Regiones = new List<Region>();

                try
                {
                    Regiones = db.Set<Region>().AsNoTracking().Where(x => x.Nombre.Contains(search)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.RegionId).Take(200).ToList();
                }
                catch (Exception)
                {
                }

                return Regiones;
            }

        #endregion
    }
}
