using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class MovimientoEstadoBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public MovimientoEstadoBL()
            {
                this.db = new GiveContext();
            }

        #endregion

        #region Metodos Privados
        #endregion

        #region Metodos Publicos

            public List<MovimientoEstado> ObtenerListado()
            {
                List<MovimientoEstado> Estados = new List<MovimientoEstado>();

                try
                {
                    Estados = db.Set<MovimientoEstado>().ToList();
                }
                catch (Exception)
                {
                }

                return Estados;
            }

        #endregion
    }
}
