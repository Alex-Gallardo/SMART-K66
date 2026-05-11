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

            private string Agregar(Traslado entidad)
            {
                string Mensaje = "OK";

                try
                {
                    List<TrasladoDetalle> Productos = new List<TrasladoDetalle>();
                    if (entidad.Detalles != null && entidad.Detalles.Count() > 0)
                    {
                        foreach (var item in entidad.Detalles)
                        {
                            bool Existe = Productos.Where(x => x.ProductoId == item.ProductoId).Count() > 0;
                            if (Existe)
                            {
                                foreach (var Producto in Productos)
                                {
                                    if (Producto.ProductoId == item.ProductoId)
                                    {
                                        Producto.Cantidad += item.Cantidad;
                                        if (!string.IsNullOrWhiteSpace(item.ID))
                                        {
                                            Producto.ID += string.Format("{0},", item.ID);                                            
                                        }
                                    }                                    
                                }
                            }
                            else
                            {
                                Productos.Add(new TrasladoDetalle() { ProductoId = item.ProductoId, UnidadId = item.UnidadId, Cantidad = item.Cantidad, ID = string.IsNullOrWhiteSpace(item.ID) ? "" : string.Format("{0},", item.ID) });
                            }
                        }
                    }
                    else
                    {
                        return "No contiene productos";
                    }

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
                                }

                                foreach (var Detalle in Productos)
                                {
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
                                        //Se agrega la informacion al Kardex
                                        db.Set<KardexMovimiento>().Add(new KardexMovimiento() { AgenciaId = entidad.AgenciaOrigenId, TipoId = 6, Fecha = DateTime.Today, FechaHora = DateTime.Now, ProductoId = Detalle.ProductoId, UnidadId = Detalle.UnidadId, DocumentoId = entidad.TrasladoId, Cantidad = Detalle.Cantidad, Precio = 0, ExistenciaActual = InventarioOrigenActual.Cantidad, ExistenciaFinal = InventarioOrigenActual.Cantidad - Cantidad, ResponsableId = entidad.UsrInicial });

                                        InventarioOrigenActual.Cantidad -= Cantidad;
                                    }

                                    //Se verifica que exista el producto en la tabla de inventario
                                    bool Existe = db.Set<ProductoInventario>().Where(x => x.ProductoId == Detalle.ProductoId && x.AgenciaId == entidad.AgenciaDestinoId).Count() > 0;
                                    if (Existe)
                                    {
                                        ProductoInventario InventarioDestinoActual = db.Set<ProductoInventario>().Where(x => x.ProductoId == Detalle.ProductoId && x.AgenciaId == entidad.AgenciaDestinoId).FirstOrDefault();
                                        if (InventarioDestinoActual != null)
                                        {
                                            //Se agrega la informacion al Kardex
                                            db.Set<KardexMovimiento>().Add(new KardexMovimiento() { AgenciaId = entidad.AgenciaDestinoId, TipoId = 6, Fecha = DateTime.Today, FechaHora = DateTime.Now, ProductoId = Detalle.ProductoId, UnidadId = Detalle.UnidadId, DocumentoId = entidad.TrasladoId, Cantidad = Detalle.Cantidad, Precio = 0, ExistenciaActual = InventarioDestinoActual.Cantidad, ExistenciaFinal = InventarioDestinoActual.Cantidad + Cantidad, ResponsableId = entidad.UsrInicial });

                                            InventarioDestinoActual.Cantidad += Cantidad;
                                        }
                                    }
                                    else
                                    {
                                        //Se agrega la informacion al Kardex
                                        db.Set<KardexMovimiento>().Add(new KardexMovimiento() { AgenciaId = entidad.AgenciaDestinoId, TipoId = 6, Fecha = DateTime.Today, FechaHora = DateTime.Now, ProductoId = Detalle.ProductoId, UnidadId = Detalle.UnidadId, DocumentoId = entidad.TrasladoId, Cantidad = Detalle.Cantidad, Precio = 0, ExistenciaActual = 0, ExistenciaFinal = Cantidad, ResponsableId = entidad.UsrInicial });

                                        db.Set<ProductoInventario>().Add(new ProductoInventario() { ProductoId = Detalle.ProductoId, AgenciaId = entidad.AgenciaDestinoId, Cantidad = Cantidad });
                                    }

                                    if (!string.IsNullOrWhiteSpace(Detalle.ID))
                                    {
                                        string[] IDs = Detalle.ID.Split(',');
                                        for (int x = 0; x < IDs.Length; x++)
                                        {
                                            if (!string.IsNullOrWhiteSpace(IDs[x]))
                                            {
                                                //Se elimina de la agencia origen
                                                string IDsActual = IDs[x];
                                                ProductoInventarioID ProductoActualID = db.Set<ProductoInventarioID>().Where(y => y.ProductoId == Detalle.ProductoId && y.AgenciaId == entidad.AgenciaOrigenId && y.ID.Equals(IDsActual)).FirstOrDefault();
                                                if (ProductoActualID != null)
                                                {
                                                    db.Set<ProductoInventarioID>().Remove(ProductoActualID);
                                                }

                                                //Se agrega a la agencia destino
                                                db.Set<ProductoInventarioID>().Add(new ProductoInventarioID() { ProductoId = Detalle.ProductoId, ID = IDs[x], Operado = false, AgenciaId = entidad.AgenciaDestinoId });
                                            }                                                                                        
                                        }
                                    }
                                }
                            }

                            db.Set<Traslado>().Add(entidad);
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

            private string Actualizar(Traslado entidad)
            {
                string Mensaje = "OK";

                try
                {

                    Traslado TrasladoActual = ObtenerPorId(entidad.TrasladoId);

                    if (TrasladoActual.TrasladoId > 0)
                    {

                        db.SaveChanges();                       
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

            public string Guardar(Traslado entidad)
            {
                string Mensaje = "OK";
             
                if (entidad.TrasladoId > 0)
                {
                    Mensaje = Actualizar(entidad);
                }
                else
                {
                    Mensaje = Agregar(entidad);
                }
          
                return Mensaje;
            }

            public string GuardarConDestino(Traslado entidad)
            {
                string Mensaje = "OK";

                try
                {
                    List<TrasladoDetalle> Productos = new List<TrasladoDetalle>();
                    if (entidad.Detalles != null && entidad.Detalles.Count() > 0)
                    {
                        foreach (var item in entidad.Detalles)
                        {
                            bool Existe = Productos.Where(x => x.ProductoId == item.ProductoId).Count() > 0;
                            if (Existe)
                            {
                                foreach (var Producto in Productos)
                                {
                                    if (Producto.ProductoId == item.ProductoId)
                                    {
                                        Producto.Cantidad += item.Cantidad;
                                        if (!string.IsNullOrWhiteSpace(item.ID))
                                        {
                                            Producto.ID += string.Format("{0},", item.ID);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Productos.Add(new TrasladoDetalle() { ProductoId = item.ProductoId, UnidadId = item.UnidadId, Cantidad = item.Cantidad, ID = string.IsNullOrWhiteSpace(item.ID) ? "" : string.Format("{0},", item.ID) });
                            }
                        }
                    }
                    else
                    {
                        return "No contiene productos";
                    }

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
                                }

                                List<string> ProductoIDs = Productos.Select(x => x.ProductoId).ToList();
                                List<Producto> TGeneralProductos = new List<Producto>();                               
                                List<Producto> TCategoriaProductos = new List<Producto>();
                                List<Producto> TNoCategoriaProductos = new List<Producto>();

                                if (ProductoIDs != null && ProductoIDs.Count() > 0)
                                {
                                    TGeneralProductos = db.Set<Producto>().AsNoTracking().Where(x => ProductoIDs.Contains(x.ProductoId)).ToList(); ;                                       
                                }

                                if (TGeneralProductos != null && TGeneralProductos.Count() > 0)
                                {
                                    TCategoriaProductos = TGeneralProductos.Where(x => x.CategoriaId == 20190921001).ToList();
                                    TNoCategoriaProductos = TGeneralProductos.Where(x => x.CategoriaId != 20190921001).ToList();    
                                }

                                if (TCategoriaProductos.Count() == 0)
                                {
                                    foreach (var Detalle in Productos)
                                    {
                                        //Se obtiene el producto para convercion
                                        Producto ProductoPadreActual = new Producto();
                                        Producto ProductoHijoActual = new Producto();
                                        bool UnidadPadre = false;
                                        decimal Cantidad = Detalle.Cantidad;

                                        ProductoPadreActual = TGeneralProductos.Where(x => x.ProductoId == Detalle.ProductoId).FirstOrDefault();

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
                                            //Se agrega la informacion al Kardex
                                            db.Set<KardexMovimiento>().Add(new KardexMovimiento() { AgenciaId = entidad.AgenciaOrigenId, TipoId = 6, Fecha = DateTime.Today, FechaHora = DateTime.Now, ProductoId = Detalle.ProductoId, UnidadId = Detalle.UnidadId, DocumentoId = entidad.TrasladoId, Cantidad = Detalle.Cantidad, Precio = 0, ExistenciaActual = InventarioOrigenActual.Cantidad, ExistenciaFinal = InventarioOrigenActual.Cantidad - Cantidad, ResponsableId = entidad.UsrInicial });

                                            InventarioOrigenActual.Cantidad -= Cantidad;
                                        }

                                        //Se verifica que exista el producto en la tabla de inventario
                                        bool Existe = db.Set<ProductoInventario>().Where(x => x.ProductoId == Detalle.ProductoId && x.AgenciaId == entidad.AgenciaDestinoId).Count() > 0;
                                        if (Existe)
                                        {
                                            ProductoInventario InventarioDestinoActual = db.Set<ProductoInventario>().Where(x => x.ProductoId == Detalle.ProductoId && x.AgenciaId == entidad.AgenciaDestinoId).FirstOrDefault();
                                            if (InventarioDestinoActual != null)
                                            {
                                                //Se agrega la informacion al Kardex
                                                db.Set<KardexMovimiento>().Add(new KardexMovimiento() { AgenciaId = entidad.AgenciaDestinoId, TipoId = 6, Fecha = DateTime.Today, FechaHora = DateTime.Now, ProductoId = Detalle.ProductoId, UnidadId = Detalle.UnidadId, DocumentoId = entidad.TrasladoId, Cantidad = Detalle.Cantidad, Precio = 0, ExistenciaActual = InventarioDestinoActual.Cantidad, ExistenciaFinal = InventarioDestinoActual.Cantidad + Cantidad, ResponsableId = entidad.UsrInicial });

                                                InventarioDestinoActual.Cantidad += Cantidad;
                                            }
                                        }
                                        else
                                        {
                                            //Se agrega la informacion al Kardex
                                            db.Set<KardexMovimiento>().Add(new KardexMovimiento() { AgenciaId = entidad.AgenciaDestinoId, TipoId = 6, Fecha = DateTime.Today, FechaHora = DateTime.Now, ProductoId = Detalle.ProductoId, UnidadId = Detalle.UnidadId, DocumentoId = entidad.TrasladoId, Cantidad = Detalle.Cantidad, Precio = 0, ExistenciaActual = 0, ExistenciaFinal = Cantidad, ResponsableId = entidad.UsrInicial });

                                            db.Set<ProductoInventario>().Add(new ProductoInventario() { ProductoId = Detalle.ProductoId, AgenciaId = entidad.AgenciaDestinoId, Cantidad = Cantidad });
                                        }
                                    }                                   
                                }
                                else if (TCategoriaProductos.Count() > 0) 
                                {
                                    //Se realiza el descuento de existencia en la agencia origen
                                    foreach (var Detalle in Productos)
                                    {
                                        //Se obtiene el producto para convercion
                                        Producto ProductoPadreActual = new Producto();
                                        Producto ProductoHijoActual = new Producto();
                                        bool UnidadPadre = false;
                                        decimal Cantidad = Detalle.Cantidad;

                                        ProductoPadreActual = TGeneralProductos.Where(x => x.ProductoId == Detalle.ProductoId).FirstOrDefault();

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
                                            //Se agrega la informacion al Kardex
                                            db.Set<KardexMovimiento>().Add(new KardexMovimiento() { AgenciaId = entidad.AgenciaOrigenId, TipoId = 6, Fecha = DateTime.Today, FechaHora = DateTime.Now, ProductoId = Detalle.ProductoId, UnidadId = Detalle.UnidadId, DocumentoId = entidad.TrasladoId, Cantidad = Detalle.Cantidad, Precio = 0, ExistenciaActual = InventarioOrigenActual.Cantidad, ExistenciaFinal = InventarioOrigenActual.Cantidad - Cantidad, ResponsableId = entidad.UsrInicial });

                                            InventarioOrigenActual.Cantidad -= Cantidad;
                                        }
                                    }

                                    //Se realiza el proceso de agencia destino
                                    entidad.DetallesDestino = new List<TrasladoDetalleDestino>();

                                    //Productos que no tiene la categoria
                                    foreach (var Detalle in TNoCategoriaProductos)
                                    {
                                        //Se obtiene el producto para convercion
                                        Producto ProductoPadreActual = new Producto();
                                        Producto ProductoHijoActual = new Producto();
                                        bool UnidadPadre = false;
                                        decimal Cantidad = Detalle.Cantidad;

                                        ProductoPadreActual = TGeneralProductos.Where(x => x.ProductoId == Detalle.ProductoId).FirstOrDefault();

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
                                        
                                        //Se verifica que exista el producto en la tabla de inventario
                                        bool Existe = db.Set<ProductoInventario>().Where(x => x.ProductoId == Detalle.ProductoId && x.AgenciaId == entidad.AgenciaDestinoId).Count() > 0;
                                        if (Existe)
                                        {
                                            ProductoInventario InventarioDestinoActual = db.Set<ProductoInventario>().Where(x => x.ProductoId == Detalle.ProductoId && x.AgenciaId == entidad.AgenciaDestinoId).FirstOrDefault();
                                            if (InventarioDestinoActual != null)
                                            {
                                                //Se agrega la informacion al Kardex
                                                db.Set<KardexMovimiento>().Add(new KardexMovimiento() { AgenciaId = entidad.AgenciaDestinoId, TipoId = 6, Fecha = DateTime.Today, FechaHora = DateTime.Now, ProductoId = Detalle.ProductoId, UnidadId = Detalle.UnidadId, DocumentoId = entidad.TrasladoId, Cantidad = Detalle.Cantidad, Precio = 0, ExistenciaActual = InventarioDestinoActual.Cantidad, ExistenciaFinal = InventarioDestinoActual.Cantidad + Cantidad, ResponsableId = entidad.UsrInicial });

                                                InventarioDestinoActual.Cantidad += Cantidad;
                                            }
                                        }
                                        else
                                        {
                                            //Se agrega la informacion al Kardex
                                            db.Set<KardexMovimiento>().Add(new KardexMovimiento() { AgenciaId = entidad.AgenciaDestinoId, TipoId = 6, Fecha = DateTime.Today, FechaHora = DateTime.Now, ProductoId = Detalle.ProductoId, UnidadId = Detalle.UnidadId, DocumentoId = entidad.TrasladoId, Cantidad = Detalle.Cantidad, Precio = 0, ExistenciaActual = 0, ExistenciaFinal = Cantidad, ResponsableId = entidad.UsrInicial });

                                            db.Set<ProductoInventario>().Add(new ProductoInventario() { ProductoId = Detalle.ProductoId, AgenciaId = entidad.AgenciaDestinoId, Cantidad = Cantidad });
                                        }

                                        //Se agrega producto a destino
                                        TrasladoDetalle ProductoTrasladoActual = Productos.Where(x => x.ProductoId == Detalle.ProductoId).FirstOrDefault();
                                        if (ProductoTrasladoActual != null)
                                        {
                                            entidad.DetallesDestino.Add(new TrasladoDetalleDestino() { ProductoId = Detalle.ProductoId, UnidadId = Detalle.UnidadId, Cantidad = ProductoTrasladoActual.Cantidad, ID = "" });                                           
                                        }                                        
                                    }

                                    //Productos que tiene la categoria
                                    List<string> TCProductoIDs = TCategoriaProductos.Select(x => x.ProductoId).ToList();
                                    decimal CantidadTotal = 0;

                                    if (TCProductoIDs != null && TCProductoIDs.Count() > 0)
                                    {
                                        CantidadTotal = Productos.Where(x => TCProductoIDs.Contains(x.ProductoId)).Sum(x => x.Cantidad);                                      
                                    }

                                    //Obtener producto especial
                                    Producto ProductoEspecial = db.Set<Producto>().AsNoTracking().Where(x => x.ProductoId == "20190921001").FirstOrDefault();
                                    if (ProductoEspecial != null)
                                    {
                                        entidad.DetallesDestino.Add(new TrasladoDetalleDestino() { ProductoId = ProductoEspecial.ProductoId, UnidadId = ProductoEspecial.UnidadId, Cantidad = CantidadTotal, ID = "" });

                                        bool ExisteProducto = db.Set<ProductoInventario>().Where(x => x.ProductoId == ProductoEspecial.ProductoId && x.AgenciaId == entidad.AgenciaDestinoId).Count() > 0;
                                        if (ExisteProducto)
                                        {
                                            ProductoInventario InventarioDestinoActual = db.Set<ProductoInventario>().Where(x => x.ProductoId == ProductoEspecial.ProductoId && x.AgenciaId == entidad.AgenciaDestinoId).FirstOrDefault();
                                            if (InventarioDestinoActual != null)
                                            {
                                                //Se agrega la informacion al Kardex
                                                db.Set<KardexMovimiento>().Add(new KardexMovimiento() { AgenciaId = entidad.AgenciaDestinoId, TipoId = 6, Fecha = DateTime.Today, FechaHora = DateTime.Now, ProductoId = ProductoEspecial.ProductoId, UnidadId = ProductoEspecial.UnidadId, DocumentoId = entidad.TrasladoId, Cantidad = CantidadTotal, Precio = 0, ExistenciaActual = InventarioDestinoActual.Cantidad, ExistenciaFinal = InventarioDestinoActual.Cantidad + CantidadTotal, ResponsableId = entidad.UsrInicial });

                                                InventarioDestinoActual.Cantidad += CantidadTotal;
                                            }
                                        }
                                        else
                                        {
                                            //Se agrega la informacion al Kardex
                                            db.Set<KardexMovimiento>().Add(new KardexMovimiento() { AgenciaId = entidad.AgenciaDestinoId, TipoId = 6, Fecha = DateTime.Today, FechaHora = DateTime.Now, ProductoId = ProductoEspecial.ProductoId, UnidadId = ProductoEspecial.UnidadId, DocumentoId = entidad.TrasladoId, Cantidad = CantidadTotal, Precio = 0, ExistenciaActual = 0, ExistenciaFinal = CantidadTotal, ResponsableId = entidad.UsrInicial });

                                            db.Set<ProductoInventario>().Add(new ProductoInventario() { ProductoId = ProductoEspecial.ProductoId, AgenciaId = entidad.AgenciaDestinoId, Cantidad = CantidadTotal });
                                        }                                        
                                    }

                                    if (entidad.DetallesDestino != null && entidad.DetallesDestino.Count() > 0)
                                    {
                                        i = 1;
                                        foreach (var Detalle in entidad.DetallesDestino)
                                        {
                                            Detalle.DetalleId = i;
                                            Detalle.TrasladoId = entidad.TrasladoId;
                                            i++;
                                        }                                     
                                    }
                                }                                
                            }

                            db.Set<Traslado>().Add(entidad);
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

            public string Despachar(long id, long usuarioId)
            {
                string Mensaje = "OK";

                try
                {
                    Traslado TrasladoActual = db.Set<Traslado>().Where(x => x.TrasladoId == id).FirstOrDefault();
                    if (TrasladoActual != null)
                    {
                        TrasladoActual.Despachado = true;
                        TrasladoActual.UsrDespacho = usuarioId;
                        TrasladoActual.FechaHoraDespacho = DateTime.Now;
                        
                        db.SaveChanges();
                    }
                    else
                    {
                        Mensaje = "El traslado no se encuentra disponible";
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
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
                        TrasladoActual = db.Set<Traslado>().Include("AgenciaOrigen").Include("AgenciaDestino").Include("Detalles").Include("Detalles.Producto").Include("Detalles.Unidad").Include("DetallesDestino").Include("DetallesDestino.Producto").Include("DetallesDestino.Unidad").Where(x => x.TrasladoId == id).FirstOrDefault();
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

            public List<Traslado> ObtenerListado(DateTime fechaInicial, DateTime fechaFinal, bool supervisor = false)
            {
                List<Traslado> Traslados = new List<Traslado>();

                try
                {
                    Traslados = db.Set<Traslado>().Include("AgenciaOrigen").Include("AgenciaDestino").Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && x.Supervisor == supervisor).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.TrasladoId).ToList();
                }
                catch (Exception)
                {
                }

                return Traslados;
            }

            public List<Traslado> ObtenerListadoxDespachar(long agenciaId)
            {
                List<Traslado> Traslados = new List<Traslado>();

                try
                {
                    Traslados = db.Set<Traslado>().Include("AgenciaOrigen").Include("AgenciaDestino").Where(x => x.AgenciaOrigenId == agenciaId && !x.Despachado && x.Supervisor).ToList();
                }
                catch (Exception)
                {
                }

                return Traslados;
            }

        #endregion

    }
}
