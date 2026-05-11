using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class PoliticaTipoBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public PoliticaTipoBL()
            {
                this.db = new GiveContext();
            }

        #endregion

        #region Metodos Privados
        #endregion

        #region Metodos Publicos

            public List<PoliticaTipo> ObtenerListado()
            {
                List<PoliticaTipo> Tipos = new List<PoliticaTipo>();

                try
                {
                    Tipos = db.Set<PoliticaTipo>().AsNoTracking().ToList();
                }
                catch (Exception)
                {
                }

                return Tipos;
            }
        #endregion
    }
}
