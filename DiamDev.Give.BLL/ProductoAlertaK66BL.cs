using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class ProductoAlertaK66BL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public ProductoAlertaK66BL()
            {
                this.db = new GiveContext();
            }

        #endregion

        #region Metodos Privados

            private int Correlativo()
            {
                int Id = 0;

                try
                {
                    ProductoAlertaK66 ProductoAlertaK66Actual = db.Set<ProductoAlertaK66>().AsNoTracking().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (ProductoAlertaK66Actual != null)
                    {
                        Inicial_Id = ProductoAlertaK66Actual.Correlativo + 1;
                    }

                    Id = Inicial_Id;
                }
                catch (Exception)
                {}

                return Id;
            }

            private string Agregar(ProductoAlertaK66 entidad)
            {
                string Mensaje = "OK";

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngAlertaId = new Herramienta().Formato_Correlativo(Id);

                        if (lngAlertaId > 0)
                        {
                            entidad.AlertaId = lngAlertaId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            db.Set<ProductoAlertaK66>().Add(entidad);
                            db.SaveChanges();                           
                        }
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            private string Actualizar(ProductoAlertaK66 entidad)
            {
                string Mensaje = "OK";

                try
                {
                    ProductoAlertaK66 ProductoAlertaK66Actual = ObtenerPorId(entidad.AlertaId);

                    if (ProductoAlertaK66Actual.AlertaId > 0)
                    {
                        ProductoAlertaK66Actual.Nombre = entidad.Nombre;
                        ProductoAlertaK66Actual.Mensaje = entidad.Mensaje;
                        ProductoAlertaK66Actual.RangoInicial = entidad.RangoInicial;
                        ProductoAlertaK66Actual.RangoFinal = entidad.RangoFinal;

                        db.SaveChanges();
                    }
                    else
                    {
                        Mensaje = "La alerta de producto seleccionada no se encuentra con ID valido";
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

        #endregion

        #region Metodos Publicos

            public string Guardar(ProductoAlertaK66 entidad)
            {
                string Mensaje = "OK";

                if (entidad.RangoFinal <  entidad.RangoInicial)
                {
                    return "El rango final debe de ser mayor al rango inicial";
                }

                if (entidad.AlertaId > 0)
                {
                    Mensaje = Actualizar(entidad);
                }
                else
                {
                    Mensaje = Agregar(entidad);
                }            

                return Mensaje;
            }

            public ProductoAlertaK66 ObtenerPorId(long id)
            {
                ProductoAlertaK66 ProductoAlertaK66Actual = new ProductoAlertaK66();

                try
                {
                    ProductoAlertaK66Actual = db.Set<ProductoAlertaK66>().Where(x => x.AlertaId == id).FirstOrDefault();
                }
                catch (Exception)
                {}

                return ProductoAlertaK66Actual;
            }

            public List<ProductoAlertaK66> ObtenerListado()
            {
                List<ProductoAlertaK66> Alertas = new List<ProductoAlertaK66>();

                try
                {
                    Alertas = db.Set<ProductoAlertaK66>().AsNoTracking().OrderByDescending(x => x.Fecha).ThenByDescending(x => x.AlertaId).Take(200).ToList();
                }
                catch (Exception)
                {}

                return Alertas;
            }          

            public List<ProductoAlertaK66> Buscar(string search)
            {
                List<ProductoAlertaK66> Alertas = new List<ProductoAlertaK66>();

                try
                {
                    Alertas = db.Set<ProductoAlertaK66>().AsNoTracking().Where(x => x.Nombre.Contains(search)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.AlertaId).Take(200).ToList();
                }
                catch (Exception)
                {}

                return Alertas;
            }

        #endregion
    }
}
