using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class ProveedorMovimientoTipoBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public ProveedorMovimientoTipoBL()
            {
                this.db = new GiveContext();
            }

        #endregion

        #region Metodos Privados
        #endregion

        #region Metodos Publicos

            public List<ProveedorMovimientoTipo> ObtenerListado()
            {
                List<ProveedorMovimientoTipo> Tipos = new List<ProveedorMovimientoTipo>();

                try
                {
                    Tipos = db.Set<ProveedorMovimientoTipo>().ToList();
                }
                catch (Exception)
                {
                }

                return Tipos;
            }

        #endregion
    }
}
