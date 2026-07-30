using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class UnidadOperacionBL
    {        
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public UnidadOperacionBL()
            {
                this.db = new GiveContext();
            }

        #endregion

        #region Metodos Privados
        #endregion

        #region Metodos Publicos

            public List<UnidadOperacion> ObtenerListado()
            {
                List<UnidadOperacion> UnidadOperaciones = new List<UnidadOperacion>();

                try
                {
                    UnidadOperaciones = db.Set<UnidadOperacion>().AsNoTracking().ToList();
                }
                catch (Exception)
                {
                }

                return UnidadOperaciones;
            }

        #endregion
    }
}
