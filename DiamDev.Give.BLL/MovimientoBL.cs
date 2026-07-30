using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class MovimientoBL
    {

        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public MovimientoBL()
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
                    Movimiento MovimientoActual = db.Set<Movimiento>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (MovimientoActual != null)
                    {
                        Inicial_Id = MovimientoActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;
                }
                catch (Exception)
                {}

                return Id;
            }

            private int CorrelativoCredito()
            {
                int Id = 0;

                try
                {
                    ProveedorMovimiento ProveedorMovimientoActual = db.Set<ProveedorMovimiento>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (ProveedorMovimientoActual != null)
                    {
                        Inicial_Id = ProveedorMovimientoActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;
                }
                catch (Exception)
                {}

                return Id;
            }

            private string Agregar(Movimiento entidad)
            {
                string Mensaje = "OK";

                string PathFotografia = ConfigurationManager.AppSettings["Path_Fotografia_Movimiento"].ToString();
                string UrlFotografia = ConfigurationManager.AppSettings["Url_Fotografia_Movimiento"].ToString();

                try
                {
                    if (!string.IsNullOrWhiteSpace(entidad.Documento))
                    {
                        bool ExisteDocumento = db.Set<Movimiento>().AsNoTracking().Where(x => x.ProveedorId == entidad.ProveedorId && x.Documento.Equals(entidad.Documento)).Count() > 0;
                        if (ExisteDocumento)
                        {
                            return "Se le informa que el documento que ingreso ya se encuentra registrado en el sistema";
                        }                         
                    }

                    if (entidad.Detalles != null && entidad.Detalles.Count() > 0)
                    {
                        foreach (var Producto in entidad.Detalles)
                        {
                            if (!string.IsNullOrWhiteSpace(Producto.Lote))
                            {
                                bool ExisteLote = db.Set<MovimientoDetalle>().AsNoTracking().Where(x => x.Lote.Equals(Producto.Lote)).Count() > 0;
                                if (ExisteLote)
                                {
                                    return string.Format("Se le informa que el #lote '{0}' ya se encuentra registrado en el sistema", Producto.Lote);
                                }                                   
                            }                           
                        }
                    }
                    else
                    {
                        return "El ingreso no contiene productos asignados";
                    }                 

                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngMovimientoId = new Herramienta().Formato_Correlativo(Id);

                        if (lngMovimientoId > 0)
                        {
                            entidad.MovimientoId = lngMovimientoId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            if (!string.IsNullOrWhiteSpace(entidad.FotografiaMovimiento))
                            {
                                entidad.FotografiaMovimiento = string.Format(@"{0}{1}/{2}", UrlFotografia, entidad.MovimientoId, entidad.FotografiaMovimiento);
                            }

                            if (entidad.Detalles != null && entidad.Detalles.Count() > 0)
                            {
                                int DetalleId = 1;
                                foreach (var Producto in entidad.Detalles)
                                {
                                    Producto.DetalleId = DetalleId;
                                    Producto.MovimientoId = entidad.MovimientoId;

                                    if (entidad.Operado)
                                    {
                                        //Se obtiene el producto para convercion
                                        Producto ProductoPadreActual = new Producto();
                                        Producto ProductoHijoActual = new Producto();

                                        decimal Cantidad = Producto.Cantidad;
                                        decimal PrecioCosto = Producto.Precio;
                                        decimal CantidadOriginal = 0;

                                        decimal KardexPrecio = 0;
                                        decimal KardexExistenciaActual = 0;
                                        decimal KardexExistenciaFinal = 0;

                                        bool UnidadPadre = false;

                                        ProductoPadreActual = db.Set<Producto>().Where(x => x.ProductoId == Producto.ProductoId).FirstOrDefault();

                                        if (ProductoPadreActual != null)
                                        {
                                            if (ProductoPadreActual.UnidadId == Producto.UnidadId)
                                            {
                                                UnidadPadre = true;
                                                CantidadOriginal = ProductoPadreActual.Cantidad;
                                            }
                                        }

                                        if (!UnidadPadre)
                                        {
                                            ProductoHijoActual = db.Set<Producto>().Where(x => x.ProductoPadreId == Producto.ProductoId && x.UnidadId == Producto.UnidadId).FirstOrDefault();

                                            if (ProductoHijoActual != null)
                                            {
                                                Cantidad *= ProductoHijoActual.Cantidad;
                                                PrecioCosto = decimal.Round(PrecioCosto / ProductoHijoActual.Cantidad, 4);
                                                CantidadOriginal = ProductoHijoActual.Cantidad;
                                            }
                                        }

                                        if (entidad.MovimientoTipoId == 1 || entidad.MovimientoTipoId  == 3)
                                        {
                                            //Se verifica que exista el producto en la tabla de inventario
                                            bool Existe = db.Set<ProductoInventario>().Where(x => x.ProductoId == Producto.ProductoId && x.AgenciaId == entidad.AgenciaId).Count() > 0;
                                            if (Existe)
                                            {
                                                ProductoInventario InventarioActual = db.Set<ProductoInventario>().Where(x => x.ProductoId == Producto.ProductoId && x.AgenciaId == entidad.AgenciaId).FirstOrDefault();
                                                if (InventarioActual != null)
                                                {
                                                    InventarioActual.Cantidad += Cantidad;
                                                }
                                            }
                                            else
                                            {
                                                db.Set<ProductoInventario>().Add(new ProductoInventario() { ProductoId = Producto.ProductoId, AgenciaId = entidad.AgenciaId, Cantidad = Cantidad, Transito = 0 });
                                            }

                                            //Se agrega el precio costo al producto
                                            Existe = db.Set<ProductoPrecioCosto>().Where(x => x.ProductoId == Producto.ProductoId).Count() > 0;
                                            if (Existe)
                                            {
                                                ProductoPrecioCosto CostoActual = db.Set<ProductoPrecioCosto>().Where(x => x.ProductoId == Producto.ProductoId).FirstOrDefault();
                                                if (CostoActual != null)
                                                {
                                                    CostoActual.PrecioCosto = PrecioCosto;
                                                }
                                            }
                                            else
                                            {
                                                db.Set<ProductoPrecioCosto>().Add(new ProductoPrecioCosto() { ProductoId = Producto.ProductoId, PrecioCosto = PrecioCosto });
                                            }
                                        }
                                        else if (entidad.MovimientoTipoId == 2)
                                        {
                                            ProductoInventario InventarioActual = db.Set<ProductoInventario>().Where(x => x.ProductoId == Producto.ProductoId && x.AgenciaId == entidad.AgenciaId).FirstOrDefault();
                                            if (InventarioActual != null)
                                            {
                                                KardexExistenciaActual = InventarioActual.Cantidad;
                                                KardexExistenciaFinal = InventarioActual.Cantidad - Producto.Cantidad;

                                                InventarioActual.Cantidad -= Producto.Cantidad;
                                            }

                                            ProductoPrecioCosto CostoActual = db.Set<ProductoPrecioCosto>().Where(x => x.ProductoId == Producto.ProductoId).FirstOrDefault();
                                            if (CostoActual != null)
                                            {
                                                Producto.PrecioCosto = decimal.Round(CostoActual.PrecioCosto * CantidadOriginal, 4);
                                                KardexPrecio = Producto.PrecioCosto;
                                            }

                                            //Se agrega la informacion al Kardex
                                            db.Set<KardexMovimiento>().Add(new KardexMovimiento() { AgenciaId = entidad.AgenciaId, TipoId = 2, Fecha = DateTime.Today, FechaHora = DateTime.Now, ProductoId = Producto.ProductoId, UnidadId = Producto.UnidadId, DocumentoId = entidad.MovimientoId, Cantidad = Producto.Cantidad, Precio = KardexPrecio, ExistenciaActual = KardexExistenciaActual, ExistenciaFinal = KardexExistenciaFinal, ResponsableId = entidad.UsrCreo });
                                        }
                                    }                                   

                                    DetalleId += 1;
                                }
                            }

                            if (entidad.Pagos != null && entidad.Pagos.Count() > 0)
                            {
                                foreach (var Pago in entidad.Pagos)
                                {
                                    Pago.MovimientoId = entidad.MovimientoId;
                                }
                            }

                            db.Set<Movimiento>().Add(entidad);
                            db.SaveChanges();

                            if (Mensaje.Equals("OK"))
                            {
                                //Se crea carpeta de la fotografia
                                string Path_Fotografia_Movimiento = string.Format(@"{0}\{1}", PathFotografia, entidad.MovimientoId);

                                if (!(Directory.Exists(Path_Fotografia_Movimiento)))
                                {
                                    Directory.CreateDirectory(Path_Fotografia_Movimiento);
                                }

                                if (entidad.Fotografia != null)
                                {
                                    ConvetirbyteAImage(entidad.Fotografia.Content).Save(string.Format(@"{0}\{1}", Path_Fotografia_Movimiento, "movimiento.png"));
                                }
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            private string Actualizar(Movimiento entidad) 
            {
                string Mensaje = "OK";

                string PathFotografia = ConfigurationManager.AppSettings["Path_Fotografia_Movimiento"].ToString();
                string UrlFotografia = ConfigurationManager.AppSettings["Url_Fotografia_Movimiento"].ToString();

                try
                {
                    Movimiento MovimientoActual = ObtenerPorId(entidad.MovimientoId, true);
                    if (MovimientoActual != null)
                    {
                        MovimientoActual.MovimientoCategoriaId = entidad.MovimientoCategoriaId;
                        MovimientoActual.ProveedorId = entidad.ProveedorId;
                        MovimientoActual.AgenciaId = entidad.AgenciaId;
                        MovimientoActual.Descripcion = entidad.Descripcion;

                        MovimientoActual.MovimientoEstadoId = entidad.MovimientoEstadoId;
                        MovimientoActual.FechaDocumento = entidad.FechaDocumento;
                        MovimientoActual.FechaVencimiento = entidad.FechaVencimiento;
                        MovimientoActual.Documento = entidad.Documento;
                        MovimientoActual.DiasCredito = entidad.DiasCredito;

                        if (!string.IsNullOrWhiteSpace(entidad.FotografiaMovimiento))
                        {
                            MovimientoActual.FotografiaMovimiento = string.Format(@"{0}{1}/{2}", UrlFotografia, MovimientoActual.MovimientoId, entidad.FotografiaMovimiento);
                        }

                        db.SaveChanges();

                        if (Mensaje.Equals("OK"))
                        {
                            //Se crea carpeta de la fotografia
                            string Path_Fotografia_Movimiento = string.Format(@"{0}\{1}", PathFotografia, entidad.MovimientoId);

                            if (!(Directory.Exists(Path_Fotografia_Movimiento)))
                            {
                                Directory.CreateDirectory(Path_Fotografia_Movimiento);
                            }

                            if (entidad.Fotografia != null)
                            {
                                ConvetirbyteAImage(entidad.Fotografia.Content).Save(string.Format(@"{0}\{1}", Path_Fotografia_Movimiento, "movimiento.png"));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            private Image ConvetirbyteAImage(byte[] byteArrayIn)
            {
                return Image.FromStream(new MemoryStream(byteArrayIn));
            }

        #endregion

        #region Metodos Publicos

        public string Guardar(Movimiento entidad)
            {
                string Mensaje = "OK";
               
                if (entidad.MovimientoTipoId == 2)
                {
                    //Se valida que concuerde con el monto a cancelar
                    var Detalles = entidad.Detalles;
                    if (Detalles != null && Detalles.Count() > 0)
                    {
                        decimal TotalProducto = Detalles.Sum(x => x.Cantidad * x.Precio);
                        decimal TotalLiquido = entidad.Descuento == 0 ? TotalProducto : (TotalProducto - (Convert.ToDecimal(entidad.Descuento) / Convert.ToDecimal(100)) * TotalProducto);
                        decimal TotalFormas = entidad.Pagos.Sum(x => x.Valor);

                        if (TotalLiquido != TotalFormas)
                        {
                            return string.Format("El monto correcto a cancelar es de: {0}", TotalLiquido.ToString("C"));
                        }
                    }
                    else
                    {
                        return "El egreso no contiene detalle de productos";
                    }
                }

                if (entidad.MovimientoId > 0)
                {
                    Mensaje = Actualizar(entidad);
                }
                else
                {
                    Mensaje = Agregar(entidad);
                }

                return Mensaje;
            }

            public string Anular(long movimientoId, string comentario, long usuarioId, int tipoMovimientoId)
            {
                string Mensaje = "OK";

                try
                {
                    Movimiento MovimientoActual = new Movimiento();
                    if (tipoMovimientoId == 1 || tipoMovimientoId == 3)
                    {
                        MovimientoActual = ObtenerPorId(movimientoId, true);
                    }
                    else if (tipoMovimientoId == 2)
                    {
                        MovimientoActual = ObtenerPorId(movimientoId, false);
                    }

                    if (MovimientoActual == null)
                    {
                        if (tipoMovimientoId == 1 || tipoMovimientoId == 3)
                        {
                            return "El ingreso que selecciono no se encuentra disponible";
                        }
                        else if (tipoMovimientoId == 2)
                        {
                            return "El egreso que selecciono no se encuentra disponible";
                        }
                    }

                    MovimientoActual.Comentario = comentario;
                    MovimientoActual.Anulada = true;
                    MovimientoActual.UsrAnular = usuarioId;
                    MovimientoActual.FechaAnular = DateTime.Now;

                    if (MovimientoActual.Detalles != null && MovimientoActual.Detalles.Count() > 0)
                    {
                        if (MovimientoActual.Operado)
                        {
                            foreach (var Producto in MovimientoActual.Detalles)
                            {
                                //Se obtiene el producto para convercion
                                Producto ProductoPadreActual = new Producto();
                                Producto ProductoHijoActual = new Producto();
                                bool UnidadPadre = false;
                                decimal Cantidad = Producto.Cantidad;

                                decimal KardexPrecio = Producto.Precio;
                                decimal KardexExistenciaActual = 0;
                                decimal KardexExistenciaFinal = 0;

                                ProductoPadreActual = db.Set<Producto>().Where(x => x.ProductoId == Producto.ProductoId).FirstOrDefault();

                                if (ProductoPadreActual != null)
                                {
                                    if (ProductoPadreActual.UnidadId == Producto.UnidadId)
                                    {
                                        UnidadPadre = true;
                                    }
                                }

                                if (!UnidadPadre)
                                {
                                    ProductoHijoActual = db.Set<Producto>().Where(x => x.ProductoPadreId == Producto.ProductoId && x.UnidadId == Producto.UnidadId).FirstOrDefault();

                                    if (ProductoHijoActual != null)
                                    {
                                        Cantidad *= ProductoHijoActual.Cantidad;
                                    }
                                }

                                ProductoInventario InventarioActual = db.Set<ProductoInventario>().Where(x => x.ProductoId == Producto.ProductoId && x.AgenciaId == MovimientoActual.AgenciaId).FirstOrDefault();
                                if (InventarioActual != null)
                                {
                                    if (tipoMovimientoId == 1 || tipoMovimientoId == 3)
                                    {
                                        if (Cantidad >= InventarioActual.Cantidad)
                                        {
                                            InventarioActual.Cantidad = 0;
                                        }
                                        else
                                        {
                                            KardexExistenciaActual = InventarioActual.Cantidad;
                                            KardexExistenciaFinal = InventarioActual.Cantidad - Cantidad;

                                            InventarioActual.Cantidad -= Cantidad;
                                        }

                                        //Se agrega la informacion al Kardex
                                        db.Set<KardexMovimiento>().Add(new KardexMovimiento() { AgenciaId = MovimientoActual.AgenciaId, TipoId = 8, Fecha = DateTime.Today, FechaHora = DateTime.Now, ProductoId = Producto.ProductoId, UnidadId = Producto.UnidadId, DocumentoId = MovimientoActual.MovimientoId, Cantidad = Producto.Cantidad, Precio = KardexPrecio, ExistenciaActual = KardexExistenciaActual, ExistenciaFinal = KardexExistenciaFinal, ResponsableId = MovimientoActual.UsrAnular.Value });
                                    }
                                    else if (tipoMovimientoId == 2)
                                    {
                                        KardexExistenciaActual = InventarioActual.Cantidad;
                                        KardexExistenciaFinal = InventarioActual.Cantidad + Cantidad;

                                        InventarioActual.Cantidad += Cantidad;

                                        //Se agrega la informacion al Kardex
                                        db.Set<KardexMovimiento>().Add(new KardexMovimiento() { AgenciaId = MovimientoActual.AgenciaId, TipoId = 9, Fecha = DateTime.Today, FechaHora = DateTime.Now, ProductoId = Producto.ProductoId, UnidadId = Producto.UnidadId, DocumentoId = MovimientoActual.MovimientoId, Cantidad = Producto.Cantidad, Precio = KardexPrecio, ExistenciaActual = KardexExistenciaActual, ExistenciaFinal = KardexExistenciaFinal, ResponsableId = MovimientoActual.UsrAnular.Value });
                                    }
                                }
                            }                                                         
                        }

                        db.SaveChanges(); 
                    }
                    else
                    {
                        if (tipoMovimientoId == 1 || tipoMovimientoId == 3)
                        {
                            return "El ingreso que selecciono no contiene productos";
                        }
                        else if (tipoMovimientoId == 2)
                        {
                            return "El egreso que selecciono no contiene productos";                         
                        }
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            public String Aprobar(Movimiento entidad, long usuarioId)
            {
                string Mensaje = "OK";

                try
                {
                    Movimiento MovimientoActual = ObtenerPorId(entidad.MovimientoId, true);
                    if (MovimientoActual != null)
                    {
                        MovimientoActual.Operado = true;
                        if (MovimientoActual.Detalles != null && MovimientoActual.Detalles.Count() > 0)
                        {
                            foreach (var Producto in MovimientoActual.Detalles)
                            {
                                //Se obtiene el producto para convercion
                                Producto ProductoPadreActual = new Producto();
                                Producto ProductoHijoActual = new Producto();

                                decimal Cantidad = Producto.Cantidad;
                                decimal PrecioCosto = Producto.PrecioCosto;
                                decimal CantidadOriginal = 0;

                                decimal KardexExistenciaActual = 0;
                                decimal KardexExistenciaFinal = 0;
                                decimal KardexPrecio = 0;

                                bool UnidadPadre = false;

                                ProductoPadreActual = db.Set<Producto>().Where(x => x.ProductoId == Producto.ProductoId).FirstOrDefault();

                                if (ProductoPadreActual != null)
                                {
                                    if (ProductoPadreActual.UnidadId == Producto.UnidadId)
                                    {
                                        UnidadPadre = true;
                                        CantidadOriginal = ProductoPadreActual.Cantidad;
                                    }
                                }

                                if (!UnidadPadre)
                                {
                                    ProductoHijoActual = db.Set<Producto>().Where(x => x.ProductoPadreId == Producto.ProductoId && x.UnidadId == Producto.UnidadId).FirstOrDefault();

                                    if (ProductoHijoActual != null)
                                    {
                                        Cantidad *= ProductoHijoActual.Cantidad;                                        
                                        CantidadOriginal = ProductoHijoActual.Cantidad;
                                    }
                                }

                                if (MovimientoActual.MovimientoTipoId == 1 || MovimientoActual.MovimientoTipoId == 3)
                                {
                                    //Se verifica que exista el producto en la tabla de inventario
                                    bool Existe = db.Set<ProductoInventario>().Where(x => x.ProductoId == Producto.ProductoId && x.AgenciaId == MovimientoActual.AgenciaId).Count() > 0;
                                    if (Existe)
                                    {
                                        ProductoInventario InventarioActual = db.Set<ProductoInventario>().Where(x => x.ProductoId == Producto.ProductoId && x.AgenciaId == MovimientoActual.AgenciaId).FirstOrDefault();
                                        if (InventarioActual != null)
                                        {
                                            //Informacion del Kardex
                                            KardexExistenciaActual = InventarioActual.Cantidad;
                                            KardexExistenciaFinal = InventarioActual.Cantidad + Cantidad;

                                            InventarioActual.Cantidad += Cantidad;
                                        }
                                    }
                                    else
                                    {
                                        //Informacion del Kardex
                                        KardexExistenciaActual = Cantidad;
                                        KardexExistenciaFinal = Cantidad;

                                        db.Set<ProductoInventario>().Add(new ProductoInventario() { ProductoId = Producto.ProductoId, AgenciaId = MovimientoActual.AgenciaId, Cantidad = Cantidad, Transito = 0 });
                                    }

                                    decimal PrecioCostoActual = 0;
                                    decimal PrecioCostoNuevo = PrecioCosto;
                                    decimal PrecioCostoPromedio = 0;

                                    //Se agrega el precio costo al producto
                                    Existe = db.Set<ProductoPrecioCosto>().Where(x => x.ProductoId == Producto.ProductoId).Count() > 0;
                                    if (Existe)
                                    {
                                        ProductoPrecioCosto CostoActual = db.Set<ProductoPrecioCosto>().Where(x => x.ProductoId == Producto.ProductoId).FirstOrDefault();
                                        if (CostoActual != null)
                                        {
                                            PrecioCostoActual = CostoActual.PrecioCosto;
                                            PrecioCostoPromedio = decimal.Round((CostoActual.PrecioCosto + PrecioCosto)/2,4);

                                            KardexPrecio = PrecioCostoPromedio;
                                            CostoActual.PrecioCosto = PrecioCostoPromedio;
                                        }
                                    }
                                    else
                                    {
                                        KardexPrecio = PrecioCosto;

                                        db.Set<ProductoPrecioCosto>().Add(new ProductoPrecioCosto() { ProductoId = Producto.ProductoId, PrecioCosto = PrecioCosto });
                                    }

                                    //Se agrega el historial de precio de costo x producto
                                    db.Set<ProductoPrecioCostoHistorial>().Add(new ProductoPrecioCostoHistorial() { ProveedorId = MovimientoActual.ProveedorId.Value, ProductoId = Producto.ProductoId , PrecioCostoActual = PrecioCostoActual, PrecioCostoNuevo = PrecioCostoNuevo, PrecioCostoPromedio = PrecioCostoPromedio, Cantidad = Producto.Cantidad, IngresoId = MovimientoActual.MovimientoId, Fecha = DateTime.Today });

                                    //Se agrega el precio venta al producto
                                    Existe = db.Set<ProductoPrecio>().Where(x => x.ProductoId == Producto.ProductoId && x.PrecioId == 5).Count() > 0;
                                    if (Existe)
                                    {
                                        ProductoPrecio PrecioActual = db.Set<ProductoPrecio>().Where(x => x.ProductoId == Producto.ProductoId && x.PrecioId == 5).FirstOrDefault();
                                        if (PrecioActual != null)
                                        {
                                            PrecioActual.Valor = Producto.Precio;
                                        }
                                    }
                                    else
                                    {
                                        db.Set<ProductoPrecio>().Add(new ProductoPrecio() { ProductoId = Producto.ProductoId, PrecioId = 5,  Valor = Producto.Precio });
                                    }

                                    //Se actualiza el minimo y el maximo del producto
                                    Producto ProductoActual = db.Set<Producto>().Where(x => x.ProductoId == Producto.ProductoId).FirstOrDefault();
                                    if (ProductoActual != null)
                                    {
                                        ProductoActual.Minimo = Producto.Minimo;
                                        ProductoActual.Maximo = Producto.Maximo;
                                    }

                                    //Se agrega a la tabla de administracion de lotes
                                    if (!string.IsNullOrWhiteSpace(Producto.Lote) && Producto.FechaVencimientoLote != null)
                                    {
                                        db.Set<ProductoLote>().Add(new ProductoLote() { ProductoId = Producto.ProductoId, AgenciaId = MovimientoActual.AgenciaId, Lote = Producto.Lote, FechaVencimiento = Producto.FechaVencimientoLote.Value, Cantidad = Cantidad });                                                                                
                                    }
                                    
                                    if (MovimientoActual.MovimientoTipoId == 3)
                                    {
                                        if (!string.IsNullOrWhiteSpace(Producto.ID))
                                        {
                                            string[] IDs = Producto.ID.Split(',');
                                            if (IDs != null && IDs.Count() > 0)
                                            {
                                                for (int i = 0; i < IDs.Length; i++)
                                                {
                                                    db.Set<ProductoInventarioID>().Add(new ProductoInventarioID() { ProductoId = Producto.ProductoId, AgenciaId  = MovimientoActual.AgenciaId, ID = IDs[i], Operado = false });                                                    
                                                }
                                            }                                            
                                        }                                        
                                    }

                                    //Se agrega la informacion al Kardex
                                    db.Set<KardexMovimiento>().Add(new KardexMovimiento() { AgenciaId = MovimientoActual.AgenciaId, TipoId = 1, Fecha = DateTime.Today, FechaHora = DateTime.Now, ProductoId = Producto.ProductoId, UnidadId = Producto.UnidadId, DocumentoId = MovimientoActual.MovimientoId, Cantidad = Producto.Cantidad, Precio = KardexPrecio, ExistenciaActual = KardexExistenciaActual, ExistenciaFinal = KardexExistenciaFinal, ResponsableId = usuarioId });
                                }                                                     
                            }                         
                        }

                        //Se verifica que el ingreso sea de estado Credito
                        if (MovimientoActual.MovimientoEstadoId == 2)
                        {
                            int Id = CorrelativoCredito();
                            long lngMovimientoId = new Herramienta().Formato_Correlativo(Id);

                            if (lngMovimientoId > 0)
                            {
                                decimal MontoActual = MovimientoActual.Detalles.Sum(x => x.Cantidad * x.PrecioCosto);

                                ProveedorMovimiento CreditoActual = new ProveedorMovimiento();
                                CreditoActual.MovimientoId = lngMovimientoId;
                                CreditoActual.ProveedorId = MovimientoActual.ProveedorId.Value;
                                CreditoActual.TipoId = 1;
                                CreditoActual.Monto = MontoActual;
                                CreditoActual.Documento = MovimientoActual.Documento;
                                CreditoActual.DiasCredito = MovimientoActual.DiasCredito;
                                CreditoActual.FechaVencimiento = MovimientoActual.FechaVencimiento;
                                CreditoActual.Correlativo = Id;
                                CreditoActual.Fecha = DateTime.Today;
                                CreditoActual.FechaMovimiento = DateTime.Today;
                                CreditoActual.UsrCreo = MovimientoActual.UsrCreo;

                                //Se actualiza el saldo al proveedor
                                Proveedor ProveedorActual = db.Set<Proveedor>().Where(x => x.ProveedorId == MovimientoActual.ProveedorId).FirstOrDefault();
                                if (ProveedorActual != null)
                                {
                                    ProveedorActual.Credito += MontoActual;
                                }

                                db.Set<ProveedorMovimiento>().Add(CreditoActual);
                            }
                        }

                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }


            public Movimiento ObtenerPorId(long id, bool proveedor = true)
            {
                Movimiento MovimientoActual = new Movimiento();

                try
                {
                    if (proveedor)
                    {
                        MovimientoActual = db.Set<Movimiento>().Include("MovimientoCategoria").Include("MovimientoEstado").Include("Agencia").Include("Proveedor").Include("Detalles").Include("Detalles.Producto").Include("Detalles.Producto.Marca").Include("Detalles.Unidad").Where(x => x.MovimientoId == id).FirstOrDefault();
                    }
                    else
                    {
                        MovimientoActual = db.Set<Movimiento>().Include("MovimientoCategoria").Include("MovimientoEstado").Include("Agencia").Include("Cliente").Include("Detalles").Include("Detalles.Producto").Include("Detalles.Producto.Marca").Include("Detalles.Unidad").Include("Pagos").Include("Pagos.FormaPago").Where(x => x.MovimientoId == id).FirstOrDefault();
                        if (MovimientoActual != null)
                        {
                            MovimientoActual.DescuentoTotal = MovimientoActual.Descuento == 0 ? 0 : (Convert.ToDecimal(MovimientoActual.Descuento) / Convert.ToDecimal(100) * MovimientoActual.Detalles.Sum(x => x.Cantidad * x.Precio));
                            MovimientoActual.Total = MovimientoActual.Detalles.Sum(x => x.Cantidad * x.Precio) - MovimientoActual.DescuentoTotal;
                        }
                    }
                }
                catch (Exception)
                {
                }

                return MovimientoActual;
            }

            public List<Movimiento> ObtenerListado()
            {
                List<Movimiento> Movimientos = new List<Movimiento>();

                try
                {
                    Movimientos = db.Set<Movimiento>().OrderByDescending(x => x.Fecha).ThenByDescending(x => x.MovimientoId).ToList();
                }
                catch (Exception)
                {
                }

                return Movimientos;
            }

            public List<Movimiento> ObtenerListadoPorFecha(DateTime fechaInicial, DateTime fechaFinal, int tipoId, long usuarioId, bool proveedor = true)
            {
                List<Movimiento> Movimientos = new List<Movimiento>();

                try
                {
                    var AgenciasIds = db.Set<UsuarioAgencia>().Where(x => x.UsuarioId == usuarioId).AsEnumerable().Select(x => x.AgenciaId).ToList();
                    if (AgenciasIds != null && AgenciasIds.Count() > 0)
                    {
                        if (proveedor)
                        {
                            Movimientos = db.Set<Movimiento>().Include("Agencia").Include("MovimientoCategoria").Include("MovimientoEstado").Include("Proveedor").Include("Detalles").Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && x.MovimientoTipoId == tipoId && AgenciasIds.Contains(x.AgenciaId)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.MovimientoId).ToList();
                        }
                        else
                        {
                            Movimientos = db.Set<Movimiento>().Include("Agencia").Include("MovimientoCategoria").Include("MovimientoEstado").Include("Cliente").Include("Detalles").Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && x.MovimientoTipoId == tipoId && AgenciasIds.Contains(x.AgenciaId)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.MovimientoId).ToList();
                        }
                    }
                }
                catch (Exception)
                {
                }

                return Movimientos;
            }

            public List<MovimientoModel> ObtenerMovimientoPorTipo(DateTime fechaInicial, DateTime fechaFinal, int tipoId, long agenciaId, long usuarioId, long proveedorId, string productoId)
            {
                List<MovimientoModel> Movimientos = new List<MovimientoModel>();
                List<long> AgenciaIds = new List<long>();

                try
                {

                    if (agenciaId == 0)
                    {
                        AgenciaIds = db.Set<UsuarioAgencia>().Where(x => x.UsuarioId == usuarioId).Select(x => x.AgenciaId).ToList();
                    }
                    else
                    {
                        AgenciaIds.Add(agenciaId);
                    }

                    if (tipoId == 1 || tipoId == 3)
                    {
                        if (proveedorId == 0)
                        {
                            if (string.IsNullOrWhiteSpace(productoId))
                            {
                                Movimientos = db.Set<Movimiento>().Include("MovimientoCategoria").Include("MovimientoEstado").Include("Agencia").Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && x.MovimientoTipoId == tipoId && AgenciaIds.Contains(x.AgenciaId)).Join(db.Set<MovimientoDetalle>(), M => M.MovimientoId, MD => MD.MovimientoId, (M, MD) => new MovimientoModel() { MovimientoId = M.MovimientoId, Categoria = M.MovimientoCategoria.Nombre, Agencia = M.Agencia.Nombre, Descripcion = M.Descripcion, Id = M.ProveedorId.Value, Total = MD.Cantidad * MD.Precio, Descuento = M.Descuento, UsuarioId = M.UsrCreo }).AsEnumerable().Select(x => x).Join(db.Set<Proveedor>(), M => M.Id, P => P.ProveedorId, (M, P) => new MovimientoModel() { MovimientoId = M.MovimientoId, Categoria = M.Categoria, Agencia = M.Agencia, Nombre = P.Nombre, Descripcion = M.Descripcion, Total = M.Total, UsuarioId = M.UsuarioId }).Join(db.Set<Usuario>(), M => M.UsuarioId, U => U.UsuarioId, (M, U) => new MovimientoModel() { MovimientoId = M.MovimientoId, Categoria = M.Categoria, Agencia = M.Agencia, Nombre = M.Nombre, Descripcion = M.Descripcion, Total = M.Total, Usuario = U.Nombre }).ToList();
                            }
                            else
                            {
                                Movimientos = db.Set<Movimiento>().Include("MovimientoCategoria").Include("MovimientoEstado").Include("Agencia").Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && x.MovimientoTipoId == tipoId && AgenciaIds.Contains(x.AgenciaId)).Join(db.Set<MovimientoDetalle>().Where(x => x.ProductoId == productoId), M => M.MovimientoId, MD => MD.MovimientoId, (M, MD) => new MovimientoModel() { MovimientoId = M.MovimientoId, Categoria = M.MovimientoCategoria.Nombre, Agencia = M.Agencia.Nombre, Descripcion = M.Descripcion, Id = M.ProveedorId.Value, Fecha = M.Fecha, Cantidad = MD.Cantidad, Precio = MD.Precio, Total = MD.Cantidad * MD.Precio, Descuento = M.Descuento, UsuarioId = M.UsrCreo }).AsEnumerable().Select(x => x).Join(db.Set<Proveedor>(), M => M.Id, P => P.ProveedorId, (M, P) => new MovimientoModel() { MovimientoId = M.MovimientoId, Categoria = M.Categoria, Agencia = M.Agencia, Nombre = P.Nombre, Descripcion = M.Descripcion, Fecha = M.Fecha, Cantidad = M.Cantidad, Precio = M.Precio, Total = M.Total, UsuarioId = M.UsuarioId }).Join(db.Set<Usuario>(), M => M.UsuarioId, U => U.UsuarioId, (M, U) => new MovimientoModel() { MovimientoId = M.MovimientoId, Categoria = M.Categoria, Agencia = M.Agencia, Nombre = M.Nombre, Descripcion = M.Descripcion, Fecha = M.Fecha, Cantidad = M.Cantidad, Precio = M.Precio, Total = M.Total, Usuario = U.Nombre }).ToList();
                            }
                        }
                        else
                        {
                            Movimientos = db.Set<Movimiento>().Include("MovimientoCategoria").Include("MovimientoEstado").Include("Agencia").Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && x.MovimientoTipoId == tipoId && AgenciaIds.Contains(x.AgenciaId) && x.ProveedorId == proveedorId).Join(db.Set<MovimientoDetalle>(), M => M.MovimientoId, MD => MD.MovimientoId, (M, MD) => new MovimientoModel() { MovimientoId = M.MovimientoId, Categoria = M.MovimientoCategoria.Nombre, Agencia = M.Agencia.Nombre, Descripcion = M.Descripcion, Id = M.ProveedorId.Value, Total = MD.Cantidad * MD.Precio, Descuento = M.Descuento, UsuarioId = M.UsrCreo }).AsEnumerable().Select(x => x).Join(db.Set<Proveedor>(), M => M.Id, P => P.ProveedorId, (M, P) => new MovimientoModel() { MovimientoId = M.MovimientoId, Categoria = M.Categoria, Agencia = M.Agencia, Nombre = P.Nombre, Descripcion = M.Descripcion, Total = M.Total, UsuarioId = M.UsuarioId }).Join(db.Set<Usuario>(), M => M.UsuarioId, U => U.UsuarioId, (M, U) => new MovimientoModel() { MovimientoId = M.MovimientoId, Categoria = M.Categoria, Agencia = M.Agencia, Nombre = M.Nombre, Descripcion = M.Descripcion, Total = M.Total, Usuario = U.Nombre }).ToList();
                        }
                    }
                    else if (tipoId == 2)
                    {
                        Movimientos = db.Set<Movimiento>().Include("MovimientoCategoria").Include("MovimientoEstado").Include("Agencia").Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && x.MovimientoTipoId == tipoId && AgenciaIds.Contains(x.AgenciaId)).Join(db.Set<MovimientoDetalle>(), M => M.MovimientoId, MD => MD.MovimientoId, (M, MD) => new MovimientoModel() { MovimientoId = M.MovimientoId, Categoria = M.MovimientoCategoria.Nombre, Agencia = M.Agencia.Nombre, Descripcion = M.Descripcion, Id = M.ClienteId.Value, Total = MD.Cantidad * MD.Precio, Descuento = M.Descuento, UsuarioId = M.UsrCreo }).AsEnumerable().Select(x => x).Join(db.Set<Cliente>(), M => M.Id, C => C.ClienteId, (M, C) => new MovimientoModel() { MovimientoId = M.MovimientoId, Categoria = M.Categoria, Agencia = M.Agencia, Nombre = C.Nombre, Descripcion = M.Descripcion, Total = M.Total, Descuento = M.Descuento, UsuarioId = M.UsuarioId }).AsEnumerable().Select(x => new MovimientoModel() { MovimientoId = x.MovimientoId, Categoria = x.Categoria, Agencia = x.Agencia, Nombre = x.Nombre, Descripcion = x.Descripcion, Total = x.Descuento == 0 ? x.Total : x.Total - ((Convert.ToDecimal(x.Descuento) / Convert.ToDecimal(100)) * x.Total), UsuarioId = x.UsuarioId }).Join(db.Set<Usuario>(), M => M.UsuarioId, U => U.UsuarioId, (M, U) => new MovimientoModel() { MovimientoId = M.MovimientoId, Categoria = M.Categoria, Agencia = M.Agencia, Nombre = M.Nombre, Descripcion = M.Descripcion, Total = M.Total, Usuario = U.Nombre }).ToList();
                    }

                    if (Movimientos != null && Movimientos.Count() > 0)
                    {
                        var MovimientosIds = Movimientos.GroupBy(m => new { m.MovimientoId, m.Nombre, Categoria = m.Categoria, Centro = m.Agencia, m.Descripcion, m.Descuento, m.Usuario, m.Fecha, m.Precio }).Select(g => new { g.Key, Cantidad = g.Sum(x => x.Cantidad), Total = g.Sum(x => x.Total) }).ToList();
                        if (MovimientosIds != null && MovimientosIds.Count() > 0)
                        {
                            Movimientos = new List<MovimientoModel>();
                            Movimientos = MovimientosIds.Select(x => new MovimientoModel() { MovimientoId = x.Key.MovimientoId, Categoria = x.Key.Categoria, Agencia = x.Key.Centro, Nombre = x.Key.Nombre, Descuento = x.Key.Descuento, Descripcion = x.Key.Descripcion, Fecha = x.Key.Fecha, Cantidad = x.Cantidad, Precio = x.Key.Precio, Total = x.Total, Usuario = x.Key.Usuario }).ToList();
                        }
                    }
                }
                catch (Exception)
                {
                }

                return Movimientos;
            }

            public List<FormaPago> ObtenerMovimientoPorFormaPago(DateTime fechaInicial, DateTime fechaFinal, long agenciaId, long usuarioId)
            {
                List<FormaPago> Formas = new List<FormaPago>();
                List<long> AgenciaIds = new List<long>();

                try
                {

                    if (agenciaId == 0)
                    {
                        AgenciaIds = db.Set<UsuarioAgencia>().Where(x => x.UsuarioId == usuarioId).Select(x => x.AgenciaId).ToList();
                    }
                    else
                    {
                        AgenciaIds.Add(agenciaId);
                    }

                    List<FormaModel> MovimientoIds = db.Set<Movimiento>().Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && x.MovimientoTipoId == 2 && AgenciaIds.Contains(x.AgenciaId)).Join(db.Set<MovimientoFormaPago>(), R => R.MovimientoId, F => F.MovimientoId, (R, F) => new { R, F }).GroupBy(r => r.F.FormaPagoId).Select(x => new FormaModel { FacturaId = x.Key, Total = x.Sum(g => g.F.Valor) }).ToList();
                    if (MovimientoIds != null && MovimientoIds.Count() > 0)
                    {
                        Formas = MovimientoIds.Join(db.Set<FormaPago>(), R => R.FacturaId, F => F.FormaPagoId, (R, F) => new FormaPago() { FormaPagoId = F.FormaPagoId, Nombre = F.Nombre, Valor = R.Total }).ToList();
                    }
                }
                catch (Exception)
                {
                }

                return Formas;
            }

            public List<EtiquetaModel> GenerarEtiquetas(long movimientoId) 
            {
                List<EtiquetaModel> Etiquetas = new List<EtiquetaModel>();

                try
                {
                    Etiquetas = db.Set<MovimientoDetalle>().Where(x => x.MovimientoId == movimientoId).Join(db.Set<Producto>(), MD => MD.ProductoId, P => P.ProductoId, (MD, P) => new EtiquetaModel() { ProductoId = MD.ProductoId, Codigo = MD.ProductoId, Barra = P.Codigo, Descripcion = P.Descripcion, Precio = 0, Copia = MD.Cantidad }).AsEnumerable().Join(db.Set<ProductoPrecio>().Where(x => x.PrecioId == 5), E => E.ProductoId, PC => PC.ProductoId, (E,PC) => new EtiquetaModel(){ ProductoId = E.ProductoId, Codigo = E.Codigo, Barra = E.Barra, Descripcion = E.Descripcion, Precio = PC.Valor, Copia = E.Copia }).ToList();
                }
                catch (Exception)
                {
                }

                return Etiquetas;
            }

            public bool Eliminar(long MovimientoId, string ProductoId)
            {
                try
                {
                    MovimientoDetalle DetalleActual = db.Set<MovimientoDetalle>().Where(x => x.MovimientoId == MovimientoId && x.ProductoId == ProductoId).FirstOrDefault();
                    if (DetalleActual != null && DetalleActual.MovimientoId > 0)
                    {
                        db.Set<MovimientoDetalle>().Remove(DetalleActual);
                    }

                    db.SaveChanges();
                }
                catch (Exception)
                {
                    return false;
                }

                return true;
            }

            public bool NuevoProducto(MovimientoDetalle detalle)
            {
                bool OperacionExitosa = false;

                try
                {
                    int DetalleId = 0;
                    MovimientoDetalle DetalleActual = db.Set<MovimientoDetalle>().Where(x => x.MovimientoId == detalle.MovimientoId).OrderByDescending(x => x.DetalleId).FirstOrDefault();
                    if (DetalleActual != null)
                    {
                        DetalleId = DetalleActual.DetalleId + 1;   
                    }

                    db.Set<MovimientoDetalle>().Add(new MovimientoDetalle() { DetalleId = DetalleId, MovimientoId = detalle.MovimientoId, ProductoId = detalle.ProductoId, UnidadId = detalle.UnidadId, Cantidad = detalle.Cantidad, Minimo = detalle.Minimo, Maximo = detalle.Maximo, PrecioCosto = detalle.PrecioCosto, Precio = detalle.Precio, ID = detalle.ID });
                    db.SaveChanges();
                    OperacionExitosa = true;
                }
                catch (Exception)
                {
                }

                return OperacionExitosa;
            }

            public List<VentaModel> ObtenerMovimientosxTienda(DateTime fechaInicial, DateTime fechaFinal, long agenciaId, int tipoId, long usuarioId)
            {
                List<VentaModel> Movimientos = new List<VentaModel>();
                List<long> AgenciaIds = new List<long>();

                try
                {
                    if (agenciaId == 0)
                    {
                        AgenciaIds = db.Set<UsuarioAgencia>().Where(x => x.UsuarioId == usuarioId).Select(x => x.AgenciaId).ToList();
                    }
                    else
                    {
                        AgenciaIds.Add(agenciaId);
                    }
                
                    Movimientos = db.Set<Movimiento>().Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && !x.Anulada && AgenciaIds.Contains(x.AgenciaId) && x.MovimientoTipoId == tipoId).Join(db.Set<MovimientoDetalle>(), F => F.MovimientoId, FD => FD.MovimientoId, (F, FD) => new VentaModel() { Id = FD.ProductoId, NoFactura = F.MovimientoId, AgenciaId = F.AgenciaId, Cantidad = FD.Cantidad, Total = FD.Cantidad * FD.Precio, CostoIva = FD.PrecioCosto, PrecioIva = FD.Precio, Descuento = 0, FacturaId = F.MovimientoId, Dias = 0, Fecha = F.Fecha }).AsEnumerable().Join(db.Set<Producto>(), V => V.Id, P => P.ProductoId, (V, P) => new VentaModel() { Id = V.Id, Codigo = P.Codigo, MarcaId = P.MarcaId, Descripcion = P.Descripcion, SerieId = V.SerieId, NoFactura = V.NoFactura, Cantidad = V.Cantidad, Total = V.Total, CostoIva = V.CostoIva, PrecioIva = V.PrecioIva, AgenciaId = V.AgenciaId, Descuento = V.Descuento, FacturaId = V.FacturaId, Dias = V.Dias, Fecha = V.Fecha }).AsEnumerable().Join(db.Set<Agencia>(), V => V.AgenciaId, A => A.AgenciaId, (V, A) => new VentaModel() { Id = V.Id, Codigo = V.Codigo, MarcaId = V.MarcaId, Descripcion = V.Descripcion, SerieId = V.SerieId, NoFactura = V.NoFactura, AgenciaId = V.AgenciaId, Agencia = A.Nombre, Cantidad = V.Cantidad, Total = V.Total, CostoIva = V.CostoIva, PrecioIva = V.PrecioIva, Descuento = V.Descuento, FacturaId = V.FacturaId, Dias = V.Dias, Fecha = V.Fecha }).AsEnumerable().Join(db.Set<Marca>(), V => V.MarcaId, M => M.MarcaId, (V, M) => new VentaModel() { Id = V.Id, Codigo = V.Codigo, MarcaId = V.MarcaId, Marca = M.Nombre, Descripcion = V.Descripcion, SerieId = V.SerieId, Serie = V.Serie, NoFactura = V.NoFactura, AgenciaId = V.AgenciaId, Agencia = V.Agencia, Cantidad = V.Cantidad, Total = V.Total, CostoIva = V.CostoIva, PrecioIva = V.PrecioIva, Descuento = V.Descuento, FacturaId = V.FacturaId, Dias = V.Dias, Fecha = V.Fecha }).OrderBy(x => x.Fecha).ThenBy(x => x.NoFactura).ToList();
                }
                catch (Exception)
                {
                }
                return Movimientos;
            }

            public List<MovimientoCreditoModel> ObtenerCreditosNoCancelados(long proveedorId) 
            {
                List<MovimientoCreditoModel> Creditos = new List<MovimientoCreditoModel>();

                try
                {
                    Creditos = db.Set<Movimiento>().Include("Detalles").AsNoTracking().Where(x => x.ProveedorId == proveedorId && x.MovimientoEstadoId == 2 && x.MovimientoTipoId == 1 && !x.Cancelado).AsEnumerable().Select(x => new MovimientoCreditoModel() { MovimientoId = x.MovimientoId, Documento = string.Format("#Documento: {0}, Monto: {1:C}", x.Documento, x.Detalles.Sum(y => y.Cantidad * y.PrecioCosto)) }).ToList();
                }
                catch (Exception)
                {
                }

                return Creditos;
            }

            public decimal ObtenerTotalCreditoPendiente(long proveedorId, long movimientoId)
            {
                decimal Total = 0;

                try
                {
                    Total = db.Set<Movimiento>().Include("Detalles").AsNoTracking().Where(x => x.ProveedorId == proveedorId && x.MovimientoId == movimientoId).Sum(x => x.Detalles.Sum(y => y.Cantidad * y.PrecioCosto));
                }
                catch (Exception)
                {
                }

                return Total;
            }

            public bool ValidarID(string ID) 
            {
                return db.Set<ProductoInventarioID>().AsNoTracking().Where(x => x.ID.Equals(ID)).Count() > 0;
            }

            public ConteoIngresos ObtenerConteoIngresos(long agenciaId) 
            {
                ConteoIngresos Conteo = new ConteoIngresos();

                try
                {
                    Conteo.CantidadIngresos = db.Set<Movimiento>().AsNoTracking().Where(x => x.AgenciaId == agenciaId && x.MovimientoTipoId == 1 && !x.Operado && !x.Anulada).Count();
                    Conteo.CantidadIngresosxID = db.Set<Movimiento>().AsNoTracking().Where(x => x.AgenciaId == agenciaId && x.MovimientoTipoId == 3 && !x.Operado && !x.Anulada).Count();
                    Conteo.CantidadPedidosSinOperar = db.Set<Pedido>().AsNoTracking().Where(x => x.AgenciaId == agenciaId && !x.Operada).Count();
                    Conteo.CantidadRecibosSinDespachar = db.Set<Recibo>().AsNoTracking().Where(x => x.AgenciaId == agenciaId && !x.Anulada && !x.Despachado).Count();
                    Conteo.CantidadFacturasSinDespachar = db.Set<Factura>().AsNoTracking().Where(x => x.AgenciaId == agenciaId && !x.Anulada && !x.Despachado).Count();
                    Conteo.CantidadCuentaxCobrar = db.Set<Recibo>().AsNoTracking().Where(x => x.AgenciaId == agenciaId && !x.Anulada && !x.Pagada && x.Despachado).Count() + db.Set<Factura>().AsNoTracking().Where(x => x.AgenciaId == agenciaId && !x.Anulada && !x.Pagada && x.Despachado).Count();
                    Conteo.CantidadEnvasesxRecibir = db.Set<ReciboEnvase>().AsNoTracking().Where(x => x.AgenciaId == agenciaId && x.UsrRecibe == null).Count();
                    Conteo.CantidadPedidosCotizacion = db.Set<Pedido>().AsNoTracking().Where(x => x.AgenciaId == agenciaId && !x.Cotizacion && !x.Operada).Count();
                }
                catch (Exception)
                {
                }

                return Conteo;
            }

            public List<Movimiento> BuscarxDocumento(string documento, int tipoId, long usuarioId, bool proveedor = true)
            {
                List<Movimiento> Movimientos = new List<Movimiento>();

                try
                {
                    var AgenciasIds = db.Set<UsuarioAgencia>().Where(x => x.UsuarioId == usuarioId).AsEnumerable().Select(x => x.AgenciaId).ToList();
                    if (AgenciasIds != null && AgenciasIds.Count() > 0)
                    {
                        if (proveedor)
                        {
                            Movimientos = db.Set<Movimiento>().Include("Agencia").Include("MovimientoCategoria").Include("MovimientoEstado").Include("Proveedor").Include("Detalles").Where(x => x.Documento.ToLower().Contains(documento) && x.MovimientoTipoId == tipoId && AgenciasIds.Contains(x.AgenciaId)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.MovimientoId).ToList();
                        }
                        else
                        {
                            Movimientos = db.Set<Movimiento>().Include("Agencia").Include("MovimientoCategoria").Include("MovimientoEstado").Include("Cliente").Include("Detalles").Where(x => x.Documento.ToLower().Contains(documento) && x.MovimientoTipoId == tipoId && AgenciasIds.Contains(x.AgenciaId)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.MovimientoId).ToList();
                        }
                    }
                }
                catch (Exception)
                {}

                return Movimientos;
            }

            public List<Movimiento> BuscarxProveedor(long proveedorId, int tipoId, long usuarioId, bool proveedor = true)
            {
                List<Movimiento> Movimientos = new List<Movimiento>();

                try
                {
                    var AgenciasIds = db.Set<UsuarioAgencia>().Where(x => x.UsuarioId == usuarioId).AsEnumerable().Select(x => x.AgenciaId).ToList();
                    if (AgenciasIds != null && AgenciasIds.Count() > 0)
                    {
                        if (proveedor)
                        {
                            Movimientos = db.Set<Movimiento>().Include("Agencia").Include("MovimientoCategoria").Include("MovimientoEstado").Include("Proveedor").Include("Detalles").Where(x => x.ProveedorId == proveedorId && x.MovimientoTipoId == tipoId && AgenciasIds.Contains(x.AgenciaId)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.MovimientoId).ToList();
                        }
                    }
                }
                catch (Exception)
                { }

                return Movimientos;
            }

            public bool ValidarDocumentoxProveedor(long proveedorId, string Documento)
            {
                return db.Set<Movimiento>().AsNoTracking().Where(x => x.ProveedorId == proveedorId && x.Documento.Equals(Documento)).Count() > 0;
            }

            public List<MovimientoxProveedorModel> ObtenerMovimientoAlCreditoNoCancelados(long proveedorId)
            {
                List<MovimientoxProveedorModel> Creditos = new List<MovimientoxProveedorModel>();

                try
                {
                    Creditos = db.Set<Movimiento>().Include("Detalles").AsNoTracking().Where(x => x.ProveedorId == proveedorId && x.MovimientoEstadoId == 2 && x.MovimientoTipoId == 1 && !x.Cancelado).AsEnumerable().Select(x => new MovimientoxProveedorModel() { MovimientoId = x.MovimientoId, Documento = x.Documento, Fecha = x.Fecha, DiasCredito = x.DiasCredito, FechaVencimiento = x.FechaVencimiento, Monto =  x.Detalles.Sum(y => y.Cantidad * y.PrecioCosto) }).ToList();
                }
                catch (Exception)
                {
                }

                return Creditos;
            }
        
        #endregion

    }
}
