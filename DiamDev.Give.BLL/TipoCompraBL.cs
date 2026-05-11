using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class TipoCompraBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public TipoCompraBL()
            {
                this.db = new GiveContext();
            }

        #endregion

        #region Metodos Privados

        #endregion

        #region Metodos Publicos

            public List<TipoCompra> ObtenerListado()
            {
                List<TipoCompra> Tipos = new List<TipoCompra>();

                try
                {
                    Tipos = db.Set<TipoCompra>().AsNoTracking().Where(x => x.Activo).ToList();
                }
                catch (Exception)
                {}

                return Tipos;
            }        

        #endregion
    }
}
