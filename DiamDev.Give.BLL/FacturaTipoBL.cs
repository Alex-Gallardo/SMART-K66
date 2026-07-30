using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class FacturaTipoBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public FacturaTipoBL()
            {
                this.db = new GiveContext();
            }

        #endregion

        #region Metodos Privados
        #endregion

        #region Metodos Publicos

            public List<FacturaTipo> ObtenerListado()
            {
                List<FacturaTipo> FacturaTipos = new List<FacturaTipo>();

                try
                {
                    FacturaTipos = db.Set<FacturaTipo>().AsNoTracking().ToList();
                }
                catch (Exception)
                {
                }

                return FacturaTipos;
            }

        #endregion
    }
}
