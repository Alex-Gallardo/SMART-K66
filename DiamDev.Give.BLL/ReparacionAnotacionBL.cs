using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class ReparacionAnotacionBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public ReparacionAnotacionBL()
            {
                this.db = new GiveContext();
            }

        #endregion

        #region Metodos Privados

            private int Correlativo(long ReparacionId)
            {
                int Id = 0;

                try
                {
                    ReparacionAnotacion ReparacionAnotacionActual = db.Set<ReparacionAnotacion>().AsNoTracking().Where(x => x.ReparacionId == ReparacionId).OrderByDescending(x => x.AnotacionId).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (ReparacionAnotacionActual != null)
                    {
                        Inicial_Id = ReparacionAnotacionActual.AnotacionId + 1;
                    }

                    Id = Inicial_Id;

                }
                catch (Exception)
                {
                }

                return Id;
            }

            private bool Agregar(ReparacionAnotacion entidad)
            {
                bool AnotacionAgregar = false;

                try
                {
                    int Id = Correlativo(entidad.ReparacionId);

                    if (Id > 0)
                    {
                        entidad.AnotacionId = Id;
                        entidad.FechaAnotacion = DateTime.Now;

                        db.Set<ReparacionAnotacion>().Add(entidad);
                        db.SaveChanges();
                        AnotacionAgregar = true;
                    }

                }
                catch (Exception)
                {
                }

                return AnotacionAgregar;
            }

        #endregion

        #region Metodos Publicos

            public bool Guardar(ReparacionAnotacion entidad)
            {
                bool OperacionExitosa = false;

                if (entidad.AnotacionId > 0)
                {
                }
                else
                {
                    OperacionExitosa = Agregar(entidad);
                }

                return OperacionExitosa;
            }

        #endregion
    }
}
