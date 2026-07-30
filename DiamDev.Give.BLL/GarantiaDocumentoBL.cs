using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class GarantiaDocumentoBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public GarantiaDocumentoBL()
            {
                this.db = new GiveContext();
            }

        #endregion

        #region Metodos Privados
        #endregion

        #region Metodos Publicos

            public List<GarantiaDocumento> ObtenerListado()
            {
                List<GarantiaDocumento> Documentos = new List<GarantiaDocumento>();

                try
                {
                    Documentos = db.Set<GarantiaDocumento>().ToList();
                }
                catch (Exception)
                {
                }

                return Documentos;
            }

        #endregion
    }
}
