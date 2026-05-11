using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class CreditoTipoBL
    {

        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public CreditoTipoBL()
            {
                this.db = new GiveContext();
            }

        #endregion

        #region Metodos Privados
        #endregion

        #region Metodos Publicos

            public List<CreditoTipo> ObtenerListado()
            {
                List<CreditoTipo> CreditoTipos = new List<CreditoTipo>();

                try
                {
                    CreditoTipos = db.Set<CreditoTipo>().AsNoTracking().Where(x => x.CreditoTipoId < 5).ToList();
                }
                catch (Exception)
                {
                }

                return CreditoTipos;
            }

        #endregion

    }
}
