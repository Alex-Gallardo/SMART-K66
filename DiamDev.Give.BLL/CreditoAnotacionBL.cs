using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class CreditoAnotacionBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public CreditoAnotacionBL()
            {
                this.db = new GiveContext();
            }

        #endregion

        #region Metodos Privados

            private int Correlativo(long CreditoId)
            {
                int Id = 0;

                try
                {
                    CreditoAnotacion CreditoAnotacionActual = db.Set<CreditoAnotacion>().AsNoTracking().Where(x => x.CreditoId == CreditoId).OrderByDescending(x => x.AnotacionId).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (CreditoAnotacionActual != null)
                    {
                        Inicial_Id = CreditoAnotacionActual.AnotacionId + 1;
                    }

                    Id = Inicial_Id;

                }
                catch (Exception)
                {
                }

                return Id;
            }

            private bool Agregar(CreditoAnotacion entidad)
            {
                bool AnotacionAgregar = false;

                try
                {
                    int Id = Correlativo(entidad.CreditoId);

                    if (Id > 0)
                    {
                        entidad.AnotacionId = Id;
                        entidad.FechaAnotacion = DateTime.Now;

                        db.Set<CreditoAnotacion>().Add(entidad);
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

            public bool Guardar(CreditoAnotacion entidad)
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
