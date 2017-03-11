using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class MovimientoTipoBL
    {

        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public MovimientoTipoBL()
            {
                this.db = new GiveContext();
            }

        #endregion

        #region Metodos Privados
        #endregion

        #region Metodos Publicos

            public List<MovimientoTipo> ObtenerListado()
            {
                List<MovimientoTipo> Tipos = new List<MovimientoTipo>();

                try
                {
                    Tipos = db.Set<MovimientoTipo>().ToList();
                }
                catch (Exception)
                {
                }

                return Tipos;
            }

        #endregion

    }
}
