using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class ReciboTipoBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public ReciboTipoBL()
            {
                this.db = new GiveContext();
            }

        #endregion

        #region Metodos Privados
        #endregion

        #region Metodos Publicos

            public List<ReciboTipo> ObtenerListado()
            {
                List<ReciboTipo> ReciboTipos = new List<ReciboTipo>();

                try
                {
                    ReciboTipos = db.Set<ReciboTipo>().AsNoTracking().ToList();
                }
                catch (Exception)
                {
                }

                return ReciboTipos;
            }

        #endregion
    }
}
