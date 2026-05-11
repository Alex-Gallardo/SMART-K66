using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class VisitaTipoBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public VisitaTipoBL()
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
                    VisitaTipo VisitaTipoActual = db.Set<VisitaTipo>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (VisitaTipoActual != null)
                    {
                        Inicial_Id = VisitaTipoActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;
                }
                catch (Exception)
                {}

                return Id;
            }

            private string Agregar(VisitaTipo entidad)
            {
                string Mensaje = "OK";

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngTipoId = new Herramienta().Formato_Correlativo(Id);

                        if (lngTipoId > 0)
                        {
                            entidad.TipoId = lngTipoId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            db.Set<VisitaTipo>().Add(entidad);
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

            private string Actualizar(VisitaTipo entidad)
            {
                string Mensaje = "OK";

                try
                {
                    VisitaTipo VisitaTipoActual = ObtenerPorId(entidad.TipoId);

                    if (VisitaTipoActual.TipoId > 0)
                    {
                        VisitaTipoActual.Nombre = entidad.Nombre;
                        VisitaTipoActual.Activo = entidad.Activo;

                        db.SaveChanges();
                    }
                    else
                    {
                        Mensaje = "El tipo de visita seleccionada no se encuentra con ID valido";
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

            public string Guardar(VisitaTipo entidad)
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

            public VisitaTipo ObtenerPorId(long id)
            {
                VisitaTipo VisitaTipoActual = new VisitaTipo();

                try
                {
                    VisitaTipoActual = db.Set<VisitaTipo>().Where(x => x.TipoId == id).FirstOrDefault();
                }
                catch (Exception)
                {}

                return VisitaTipoActual;
            }

            public List<VisitaTipo> ObtenerListado(bool todos)
            {
                List<VisitaTipo> VisitaTipos = new List<VisitaTipo>();

                try
                {
                    if (todos)
                    {
                        VisitaTipos = db.Set<VisitaTipo>().AsNoTracking().OrderByDescending(x => x.Fecha).ThenByDescending(x => x.TipoId).Take(200).ToList();
                    }
                    else
                    {
                        VisitaTipos = db.Set<VisitaTipo>().AsNoTracking().Where(x => x.Activo).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.TipoId).ToList();
                    }
                }
                catch (Exception)
                {}

                return VisitaTipos;
            }          

            public List<VisitaTipo> Buscar(string search)
            {
                List<VisitaTipo> VisitaTipos = new List<VisitaTipo>();

                try
                {
                    VisitaTipos = db.Set<VisitaTipo>().AsNoTracking().Where(x => x.Nombre.Contains(search)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.TipoId).Take(200).ToList();
                }
                catch (Exception)
                {}

                return VisitaTipos;
            }

        #endregion
    }
}
