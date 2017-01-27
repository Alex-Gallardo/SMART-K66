using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.BLL
{
    public class CuentaContableTipoBL
    {

        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public CuentaContableTipoBL()
            {
                this.db = new GiveContext();
            }

        #endregion

        #region Metodos Privados
        #endregion

        #region Metodos Publicos

            public List<CuentaContableTipo> ObtenerListado()
            {
                List<CuentaContableTipo> Tipos = new List<CuentaContableTipo>();

                try
                {
                    Tipos = db.Set<CuentaContableTipo>().Where(x => x.Activo == true).ToList();
                }
                catch (Exception)
                {
                }

                return Tipos;
            }

        #endregion

    }
}
