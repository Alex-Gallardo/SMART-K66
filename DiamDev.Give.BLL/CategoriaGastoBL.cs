using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class CategoriaGastoBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public CategoriaGastoBL()
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
                    CategoriaGasto CategoriaGastoActual = db.Set<CategoriaGasto>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (CategoriaGastoActual != null)
                    {
                        Inicial_Id = CategoriaGastoActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;
                }
                catch (Exception)
                {
                }

                return Id;
            }

            private string Agregar(CategoriaGasto entidad)
            {
                string Mensaje = "OK";

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngCategoriaGastoId = new Herramienta().Formato_Correlativo(Id);

                        if (lngCategoriaGastoId > 0)
                        {
                            entidad.CategoriaId = lngCategoriaGastoId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            db.Set<CategoriaGasto>().Add(entidad);
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

            private string Actualizar(CategoriaGasto entidad)
            {
                string Mensaje = "OK";

                try
                {
                    CategoriaGasto CategoriaGastoActual = ObtenerPorId(entidad.CategoriaId);

                    if (CategoriaGastoActual.CategoriaId > 0)
                    {
                        CategoriaGastoActual.Nombre = entidad.Nombre;
                        CategoriaGastoActual.Descripcion = entidad.Descripcion;
                        CategoriaGastoActual.Activo = entidad.Activo;

                        db.SaveChanges();
                    }
                    else
                    {
                        Mensaje = "La categoria seleccionada no se encuentra con ID valido";
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

            public string Guardar(CategoriaGasto entidad)
            {
                string Mensaje = "OK";
               
                if (entidad.CategoriaId > 0)
                {
                    Mensaje = Actualizar(entidad);
                }
                else
                {
                    Mensaje = Agregar(entidad);
                }            

                return Mensaje;
            }

            public CategoriaGasto ObtenerPorId(long id)
            {
                CategoriaGasto CategoriaGastoActual = new CategoriaGasto();

                try
                {
                    CategoriaGastoActual = db.Set<CategoriaGasto>().Where(x => x.CategoriaId == id).FirstOrDefault();
                }
                catch (Exception)
                {
                }

                return CategoriaGastoActual;
            }

            public List<CategoriaGasto> ObtenerListado(bool todos)
            {
                List<CategoriaGasto> CategoriaGastos = new List<CategoriaGasto>();

                try
                {
                    if (todos)
                    {
                        CategoriaGastos = db.Set<CategoriaGasto>().AsNoTracking().OrderByDescending(x => x.Fecha).ThenByDescending(x => x.CategoriaId).Take(200).ToList();
                    }
                    else
                    {
                        CategoriaGastos = db.Set<CategoriaGasto>().AsNoTracking().Where(x => x.Activo == true).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.CategoriaId).ToList();
                    }
                }
                catch (Exception)
                {
                }

                return CategoriaGastos;
            }

            public List<CategoriaGasto> Buscar(string search)
            {
                List<CategoriaGasto> CategoriaGastos = new List<CategoriaGasto>();

                try
                {
                    CategoriaGastos = db.Set<CategoriaGasto>().AsNoTracking().Where(x => x.Nombre.Contains(search)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.CategoriaId).Take(200).ToList();
                }
                catch (Exception)
                {
                }

                return CategoriaGastos;
            }
        
        #endregion
    }
}
