using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class MovimientoCategoriaBL
    {

        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public MovimientoCategoriaBL()
            {
                this.db = new GiveContext();
            }

        #endregion

        #region Metodos Privados
        #endregion

        #region Metodos Publicos

            public List<MovimientoCategoria> ObtenerListado(bool ingreso)
            {
                List<MovimientoCategoria> Categorias = new List<MovimientoCategoria>();

                try
                {
                    List<int> Excluir = new List<int>() { 1, 2, 3, 4 };
                    Categorias = db.Set<MovimientoCategoria>().AsNoTracking().Where(x => !Excluir.Contains(x.MovimientoCategoriaId) && x.Ingreso == ingreso).ToList();
                }
                catch (Exception)
                {
                }

                return Categorias;
            }

        #endregion

    }
}
