using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class TrasladoBL
    {

        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public TrasladoBL()
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

                    Traslado TrasladoActual = db.Set<Traslado>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (TrasladoActual != null)
                    {
                        Inicial_Id = TrasladoActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;

                }
                catch (Exception)
                {
                }

                return Id;
            }

            private bool Agregar(Traslado entidad)
            {
                bool TrasladoAgregar = false;

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngTrasladoId = new Herramienta().Formato_Correlativo(Id);

                        if (lngTrasladoId > 0)
                        {
                            entidad.TrasladoId = lngTrasladoId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            if (entidad.Detalles != null && entidad.Detalles.Count() > 0)
                            {
                                int i = 1;
                                foreach (var Detalle in entidad.Detalles)
                                {
                                    Detalle.DetalleId = i;
                                    Detalle.TrasladoId = entidad.TrasladoId;
                                    i++;

                                    //Se obtiene el producto para convercion
                                    Producto ProductoPadreActual = new Producto();
                                    Producto ProductoHijoActual = new Producto();
                                    bool UnidadPadre = false;
                                    decimal Cantidad = Detalle.Cantidad;

                                    ProductoPadreActual = db.Set<Producto>().Where(x => x.ProductoId == Detalle.ProductoId).FirstOrDefault();

                                    if (ProductoPadreActual != null)
                                    {
                                        if (ProductoPadreActual.UnidadId == Detalle.UnidadId)
                                        {
                                            UnidadPadre = true;
                                        }
                                    }

                                    if (!UnidadPadre)
                                    {
                                        ProductoHijoActual = db.Set<Producto>().Where(x => x.ProductoPadreId == Detalle.ProductoId && x.UnidadId == Detalle.UnidadId).FirstOrDefault();

                                        if (ProductoHijoActual != null)
                                        {
                                            Cantidad *= ProductoHijoActual.Cantidad;
                                        }
                                    }

                                    ProductoInventario InventarioOrigenActual = db.Set<ProductoInventario>().Where(x => x.ProductoId == Detalle.ProductoId && x.AgenciaId == entidad.AgenciaOrigenId).FirstOrDefault();
                                    if (InventarioOrigenActual != null)
                                    {
                                        InventarioOrigenActual.Cantidad -= Cantidad;
                                    }

                                    //Se verifica que exista el producto en la tabla de inventario
                                    bool Existe = db.Set<ProductoInventario>().Where(x => x.ProductoId == Detalle.ProductoId && x.AgenciaId == entidad.AgenciaDestinoId).Count() > 0;
                                    if (Existe)
                                    {
                                        ProductoInventario InventarioDestinoActual = db.Set<ProductoInventario>().Where(x => x.ProductoId == Detalle.ProductoId && x.AgenciaId == entidad.AgenciaDestinoId).FirstOrDefault();
                                        if (InventarioDestinoActual != null)
                                        {
                                            InventarioDestinoActual.Cantidad += Cantidad;
                                        }
                                    }
                                    else
                                    {
                                        db.Set<ProductoInventario>().Add(new ProductoInventario() { ProductoId = Detalle.ProductoId, AgenciaId = entidad.AgenciaDestinoId, Cantidad = Cantidad });
                                    }
                                }
                            }

                            db.Set<Traslado>().Add(entidad);
                            db.SaveChanges();
                            TrasladoAgregar = true;
                        }
                    }

                }
                catch (Exception)
                {
                }

                return TrasladoAgregar;
            }

            private bool Actualizar(Traslado entidad)
            {
                bool TrasladoActualizar = false;

                try
                {

                    Traslado TrasladoActual = ObtenerPorId(entidad.TrasladoId);

                    if (TrasladoActual.TrasladoId > 0)
                    {

                        db.SaveChanges();
                        TrasladoActualizar = true;
                    }

                }
                catch (Exception)
                {
                }

                return TrasladoActualizar;
            }

        #endregion

        #region Metodos Publicos

            public string Guardar(Traslado entidad)
            {
                string Mensaje = "OK";
                bool OperacionExitosa = false;

                if (entidad.TrasladoId > 0)
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

            public Traslado ObtenerPorId(long id, bool todo = false)
            {
                Traslado TrasladoActual = new Traslado();

                try
                {
                    if (todo)
                    {
                        TrasladoActual = db.Set<Traslado>().Include("AgenciaOrigen").Include("AgenciaDestino").Include("Detalles").Include("Detalles.Producto").Include("Detalles.Unidad").Where(x => x.TrasladoId == id).FirstOrDefault();
                    }
                    else
                    {
                        TrasladoActual = db.Set<Traslado>().Where(x => x.TrasladoId == id).FirstOrDefault();
                    }
                }
                catch (Exception)
                {
                }

                return TrasladoActual;
            }

            public List<Traslado> ObtenerListado(DateTime fechaInicial, DateTime fechaFinal)
            {
                List<Traslado> Traslados = new List<Traslado>();

                try
                {
                    Traslados = db.Set<Traslado>().Include("AgenciaOrigen").Include("AgenciaDestino").Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.TrasladoId).ToList();
                }
                catch (Exception)
                {
                }

                return Traslados;
            }

        #endregion

    }
}
