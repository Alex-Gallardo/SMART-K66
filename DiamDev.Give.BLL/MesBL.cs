using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class MesBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public MesBL()
            {
                this.db = new GiveContext();
            }

        #endregion

        #region Metodos Privados
        #endregion

        #region Metodos Publicos

            public List<Mes> ObtenerListado()
            {
                List<Mes> Meses = new List<Mes>();

                try
                {
                    Meses = db.Set<Mes>().AsNoTracking().ToList();
                }
                catch (Exception)
                {
                }

                return Meses;
            }

        #endregion
    }
}
