using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class NominaTipoBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public NominaTipoBL()
            {
                this.db = new GiveContext();
            }

        #endregion

        #region Metodos Privados
        #endregion

        #region Metodos Publicos

            public List<NominaTipo> ObtenerListado()
            {
                List<NominaTipo> Tipos = new List<NominaTipo>();

                try
                {
                    Tipos = db.Set<NominaTipo>().AsNoTracking().ToList();
                }
                catch (Exception)
                {
                }

                return Tipos;
            }

        #endregion
    }
}
