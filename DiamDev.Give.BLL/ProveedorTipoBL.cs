using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class ProveedorTipoBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public ProveedorTipoBL()
            {
                this.db = new GiveContext();
            }

        #endregion

        #region Metodos Privados
        #endregion

        #region Metodos Publicos

            public List<ProveedorTipo> ObtenerListado()
            {
                List<ProveedorTipo> Tipos = new List<ProveedorTipo>();

                try
                {
                    Tipos = db.Set<ProveedorTipo>().AsNoTracking().ToList();
                }
                catch (Exception)
                {
                }

                return Tipos;
            }

        #endregion
    }
}
