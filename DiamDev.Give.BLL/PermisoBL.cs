using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class PermisoBL
    {

        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public PermisoBL()
            {
                this.db = new GiveContext();
            }

        #endregion

        #region Metodos Privados

        #endregion

        #region Metodos Publicos

            public List<Permiso> ObtenerListado()
            {
                List<Permiso> Permisos = new List<Permiso>();

                try
                {                    
                    Permisos = db.Set<Permiso>().AsNoTracking().ToList();
                }
                catch (Exception)
                {
                }

                return Permisos;
            }

        #endregion
    }
}
