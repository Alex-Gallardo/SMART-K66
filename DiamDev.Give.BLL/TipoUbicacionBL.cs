using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class TipoUbicacionBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public TipoUbicacionBL()
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
                    TipoUbicacion TipoUbicacionActual = db.Set<TipoUbicacion>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (TipoUbicacionActual != null)
                    {
                        Inicial_Id = TipoUbicacionActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;
                }
                catch (Exception)
                {}

                return Id;
            }

            private string Agregar(TipoUbicacion entidad)
            {
                string Mensaje = "OK";

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngTipoUbicacionId = new Herramienta().Formato_Correlativo(Id);

                        if (lngTipoUbicacionId > 0)
                        {
                            entidad.TipoId = lngTipoUbicacionId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            db.Set<TipoUbicacion>().Add(entidad);
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

            private string Actualizar(TipoUbicacion entidad)
            {
                string Mensaje = "OK";

                try
                {
                    TipoUbicacion TipoUbicacionActual = ObtenerPorId(entidad.TipoId);

                    if (TipoUbicacionActual.TipoId > 0)
                    {
                        TipoUbicacionActual.Nombre = entidad.Nombre;
                        TipoUbicacionActual.Descripcion = entidad.Descripcion;
                        TipoUbicacionActual.Activo = entidad.Activo;

                        db.SaveChanges();
                    }
                    else
                    {
                        Mensaje = "El tipo de ubicacion seleccionada no se encuentra con ID valido";
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

            public string Guardar(TipoUbicacion entidad)
            {
                string Mensaje = "OK";
               
                if (entidad.TipoId > 0)
                {
                    Mensaje = Actualizar(entidad);
                }
                else
                {
                    Mensaje = Agregar(entidad);
                }            

                return Mensaje;
            }

            public TipoUbicacion ObtenerPorId(long id)
            {
                TipoUbicacion TipoUbicacionActual = new TipoUbicacion();

                try
                {
                    TipoUbicacionActual = db.Set<TipoUbicacion>().Where(x => x.TipoId == id).FirstOrDefault();
                }
                catch (Exception)
                {}

                return TipoUbicacionActual;
            }

            public List<TipoUbicacion> ObtenerListado(bool todos)
            {
                List<TipoUbicacion> Tipos = new List<TipoUbicacion>();

                try
                {
                    if (todos)
                    {
                        Tipos = db.Set<TipoUbicacion>().AsNoTracking().OrderByDescending(x => x.Fecha).ThenByDescending(x => x.TipoId).Take(200).ToList();
                    }
                    else
                    {
                        Tipos = db.Set<TipoUbicacion>().AsNoTracking().Where(x => x.Activo).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.TipoId).ToList();
                    }
                }
                catch (Exception)
                {}

                return Tipos;
            }

            public List<TipoUbicacion> Buscar(string search)
            {
                List<TipoUbicacion> Tipos = new List<TipoUbicacion>();

                try
                {
                    Tipos = db.Set<TipoUbicacion>().AsNoTracking().Where(x => x.Nombre.Contains(search)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.TipoId).Take(200).ToList();
                }
                catch (Exception)
                {}

                return Tipos;
            }

        #endregion
    }
}
