using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class AnotacionBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public AnotacionBL()
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

                    Anotacion AnotacionActual = db.Set<Anotacion>().AsNoTracking().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (AnotacionActual != null)
                    {
                        Inicial_Id = AnotacionActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;

                }
                catch (Exception)
                {
                }

                return Id;
            }

            private bool Agregar(Anotacion entidad)
            {
                bool AnotacionAgregar = false;

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngAnotacionId = new Herramienta().Formato_Correlativo(Id);

                        if (lngAnotacionId > 0)
                        {
                            entidad.AnotacionId = lngAnotacionId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            db.Set<Anotacion>().Add(entidad);
                            db.SaveChanges();
                            AnotacionAgregar = true;
                        }
                    }

                }
                catch (Exception)
                {
                }

                return AnotacionAgregar;
            }

        #endregion

        #region Metodos Publicos

            public string Guardar(Anotacion entidad)
            {
                string Mensaje = "OK";
                bool OperacionExitosa = false;

                if (entidad.AnotacionId > 0)
                {

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

        #endregion
    }
}
