using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.BLL
{
    public class PrecioBL
    {

        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public PrecioBL()
            {
                this.db = new GiveContext();
            }

        #endregion

        #region Metodos Privados
        #endregion

        #region Metodos Publicos

            public List<Precio> ObtenerListado()
            {
                List<Precio> Precios = new List<Precio>();

                try
                {
                    Precios = db.Set<Precio>().Where(x => x.Activo == true).ToList();
                }
                catch (Exception)
                {
                }

                return Precios;
            }

        #endregion

    }
}
