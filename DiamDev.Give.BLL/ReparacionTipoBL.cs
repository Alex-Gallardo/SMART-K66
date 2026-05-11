using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class ReparacionTipoBL
    {

        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public ReparacionTipoBL()
            {
                this.db = new GiveContext();
            }

        #endregion

        #region Metodos Privados
        #endregion

        #region Metodos Publicos

            public List<ReparacionTipo> ObtenerListado()
            {
                List<ReparacionTipo> Tipos = new List<ReparacionTipo>();

                try
                {
                    Tipos = db.Set<ReparacionTipo>().AsNoTracking().ToList();
                }
                catch (Exception)
                {
                }

                return Tipos;
            }

        #endregion

    }
}
