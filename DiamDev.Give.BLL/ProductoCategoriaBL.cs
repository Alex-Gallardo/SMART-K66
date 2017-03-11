using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class ProductoCategoriaBL
    {

        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public ProductoCategoriaBL()
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

                    ProductoCategoria ProductoCategoriaActual = db.Set<ProductoCategoria>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (ProductoCategoriaActual != null)
                    {
                        Inicial_Id = ProductoCategoriaActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;

                }
                catch (Exception)
                {
                }

                return Id;
            }

            private bool Agregar(ProductoCategoria entidad)
            {
                bool ProductoCategoriaAgregar = false;

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngProductoCategoriaId = new Herramienta().Formato_Correlativo(Id);

                        if (lngProductoCategoriaId > 0)
                        {
                            entidad.ProductoCategoriaId = lngProductoCategoriaId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            db.Set<ProductoCategoria>().Add(entidad);
                            db.SaveChanges();
                            ProductoCategoriaAgregar = true;
                        }
                    }

                }
                catch (Exception)
                {
                }

                return ProductoCategoriaAgregar;
            }

            private bool Actualizar(ProductoCategoria entidad)
            {
                bool ProductoCategoriaActualizar = false;

                try
                {

                    ProductoCategoria ProductoCategoriaActual = ObtenerPorId(entidad.ProductoCategoriaId);

                    if (ProductoCategoriaActual.ProductoCategoriaId > 0)
                    {
                        ProductoCategoriaActual.Nombre = entidad.Nombre;
                        ProductoCategoriaActual.Activo = entidad.Activo;

                        db.SaveChanges();
                        ProductoCategoriaActualizar = true;
                    }

                }
                catch (Exception)
                {
                }

                return ProductoCategoriaActualizar;
            }

        #endregion

        #region Metodos Publicos

            public string Guardar(ProductoCategoria entidad)
            {
                string Mensaje = "OK";
                bool OperacionExitosa = false;

                if (entidad.ProductoCategoriaId > 0)
                {
                    OperacionExitosa = Actualizar(entidad);
                }
                else
                {
                    OperacionExitosa = Agregar(entidad);
                }

                if (!OperacionExitosa)
                {
                    Mensaje = "La información ingresada no es valida";
                }

                return Mensaje;
            }

            public ProductoCategoria ObtenerPorId(long id)
            {
                ProductoCategoria ProductoCategoriaActual = new ProductoCategoria();

                try
                {
                    ProductoCategoriaActual = db.Set<ProductoCategoria>().Where(x => x.ProductoCategoriaId == id).FirstOrDefault();
                }
                catch (Exception)
                {
                }

                return ProductoCategoriaActual;
            }

            public List<ProductoCategoria> ObtenerListado(bool todo = true)
            {
                List<ProductoCategoria> ProductoCategorias = new List<ProductoCategoria>();

                try
                {
                    if (todo)
                    {
                        ProductoCategorias = db.Set<ProductoCategoria>().OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ProductoCategoriaId).ToList();
                    }
                    else
                    {
                        ProductoCategorias = db.Set<ProductoCategoria>().Where(x => x.Activo == true).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ProductoCategoriaId).ToList();
                    }
                }
                catch (Exception)
                {
                }

                return ProductoCategorias;
            }

            public List<ProductoCategoria> Buscar(string search)
            {
                List<ProductoCategoria> ProductoCategorias = new List<ProductoCategoria>();

                try
                {
                    ProductoCategorias = db.Set<ProductoCategoria>().Where(x => x.Nombre.Contains(search)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ProductoCategoriaId).ToList();
                }
                catch (Exception)
                {
                }

                return ProductoCategorias;
            }

        #endregion

    }
}
