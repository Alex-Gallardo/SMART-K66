using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class UnidadConversionBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public UnidadConversionBL()
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
                    UnidadConversion UnidadConversionActual = db.Set<UnidadConversion>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (UnidadConversionActual != null)
                    {
                        Inicial_Id = UnidadConversionActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;
                }
                catch (Exception)
                {
                }

                return Id;
            }

            private string Agregar(UnidadConversion entidad)
            {
                string Mensaje = "OK";

                try
                {
                    bool Existe = db.Set<UnidadConversion>().AsNoTracking().Where(x => x.OperacionId == entidad.OperacionId && x.UnidadBaseId == entidad.UnidadBaseId && x.UnidadDestinoId == entidad.UnidadDestinoId).Count() > 0;
                    if (Existe)
                    {
                        return "Se le informa que el tipo de conversión ingresado ya se encuentra registrado en el sistema.";
                    }
                    
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngUnidadConversionId = new Herramienta().Formato_Correlativo(Id);

                        if (lngUnidadConversionId > 0)
                        {
                            entidad.ConversionId = lngUnidadConversionId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            db.Set<UnidadConversion>().Add(entidad);
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

            private string Actualizar(UnidadConversion entidad)
            {
                string Mensaje = "OK";

                try
                {
                    UnidadConversion UnidadConversionActual = ObtenerPorId(entidad.ConversionId);

                    if (UnidadConversionActual.ConversionId > 0)
                    {
                        UnidadConversionActual.CantidadBase = entidad.CantidadBase;
                        UnidadConversionActual.CantidadDestino = entidad.CantidadDestino;

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

            public string Guardar(UnidadConversion entidad)
            {
                string Mensaje = "OK";
             
                if (entidad.ConversionId > 0)
                {
                    Mensaje = Actualizar(entidad);
                }
                else
                {
                    Mensaje = Agregar(entidad);
                }

                return Mensaje;
            }

            public UnidadConversion ObtenerPorId(long id)
            {
                UnidadConversion UnidadConversionActual = new UnidadConversion();

                try
                {
                    UnidadConversionActual = db.Set<UnidadConversion>().Where(x => x.ConversionId == id).FirstOrDefault();
                }
                catch (Exception)
                {
                }

                return UnidadConversionActual;
            }

            public List<UnidadConversion> ObtenerListado()
            {
                List<UnidadConversion> UnidadConversions = new List<UnidadConversion>();

                try
                {
                    UnidadConversions = db.Set<UnidadConversion>().Include("Operacion").Include("UnidadBase").Include("UnidadDestino").AsNoTracking().OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ConversionId).ToList();
                }
                catch (Exception)
                {
                }

                return UnidadConversions;
            }

            public List<UnidadConversion> Buscar(string search)
            {
                List<UnidadConversion> UnidadConversions = new List<UnidadConversion>();

                try
                {
                    UnidadConversions = db.Set<UnidadConversion>().Include("Operacion").Include("UnidadBase").Include("UnidadDestino").AsNoTracking().Where(x => x.Operacion.Nombre.ToLower().Contains(search.ToLower()) || x.UnidadBase.Nombre.ToLower().Contains(search.ToLower()) || x.UnidadDestino.Nombre.ToLower().Contains(search.ToLower())).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ConversionId).ToList();
                }
                catch (Exception)
                {
                }

                return UnidadConversions;
            }

        #endregion
    }
}
