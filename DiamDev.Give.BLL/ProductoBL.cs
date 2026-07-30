using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class ProductoBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public ProductoBL()
            {
                this.db = new GiveContext();
            }

            public ProductoBL(GiveContext db)
            {
                this.db = db;
            }

        #endregion

        #region Metodos Privados

            private int Correlativo()
            {
                int Id = 0;

                try
                {

                    Producto ProductoActual = db.Set<Producto>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (ProductoActual != null)
                    {
                        Inicial_Id = ProductoActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;

                }
                catch (Exception)
                {
                }

                return Id;
            }

            private string Agregar(Producto entidad)
            {
                string Mensaje = "OK";

                string PathFotografia = ConfigurationManager.AppSettings["Path_FotografiaApp"].ToString();               
                string UrlFotografia = ConfigurationManager.AppSettings["Url_FotografiaApp"].ToString();               

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngProductoId = new Herramienta().Formato_Correlativo(Id);

                        if (lngProductoId > 0)
                        {
                            entidad.ProductoId = lngProductoId.ToString();
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;
                            entidad.FotografiaApp = string.Format(@"{0}{1}/{2}.png", UrlFotografia, entidad.ProductoId, entidad.ProductoId);

                            if (entidad.Precios != null && entidad.Precios.Count() > 0)
                            {
                                foreach (var Precio in entidad.Precios)
                                {
                                    Precio.ProductoId = entidad.ProductoId;
                                }
                            }

                            if (entidad.Imagenes != null && entidad.Imagenes.Count() > 0)
                            {
                                int imagenId = 1;
                                foreach (var Imagen in entidad.Imagenes)
                                {
                                    Imagen.FotografiaId = imagenId;
                                    Imagen.ProductoId = entidad.ProductoId;
                                    imagenId++;
                                }
                            }

                            if (entidad.Niveles != null && entidad.Niveles.Count() > 0)
                            {
                                int i = 1;
                                foreach (var Detalle in entidad.Niveles)
                                {
                                    Detalle.NivelId = i;
                                    Detalle.ProductoId = entidad.ProductoId;
                                    i++;
                                }
                            }

                            if (entidad.ProductoPadreId == "0")
                            {
                                entidad.ProductoPadreId = null;
                            }
                            
                            db.Set<Producto>().Add(entidad);
                            db.SaveChanges();

                            if (Mensaje.Equals("OK"))
                            {
                                //Se crea carpeta por producto
                                string Path_Producto = string.Format(@"{0}\{1}", PathFotografia, entidad.ProductoId);

                                if (!(Directory.Exists(Path_Producto)))
                                {
                                    Directory.CreateDirectory(Path_Producto);
                                }

                                if (entidad.Fotografia != null)
                                {
                                    ConvetirbyteAImage(entidad.Fotografia.Content).Save(string.Format(@"{0}\{1}.png", Path_Producto, entidad.ProductoId));
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

            private string Actualizar(Producto entidad)
            {
                string Mensaje = "OK";

                string PathFotografia = ConfigurationManager.AppSettings["Path_FotografiaApp"].ToString();
                
                string UrlFotografia = ConfigurationManager.AppSettings["Url_FotografiaApp"].ToString();
                
                try
                {
                    Producto ProductoActual = ObtenerPorId(entidad.ProductoId);

                    if (!string.IsNullOrWhiteSpace(ProductoActual.ProductoId))
                    {
                        ProductoActual.EmpresaId = entidad.EmpresaId;
                        ProductoActual.CategoriaId = entidad.CategoriaId;
                        ProductoActual.MarcaId = entidad.MarcaId;
                        ProductoActual.UnidadId = entidad.UnidadId;
                        ProductoActual.Codigo = entidad.Codigo;
                        ProductoActual.Nombre = entidad.Nombre;
                        ProductoActual.NombreAlternativo1 = entidad.NombreAlternativo1;
                        ProductoActual.NombreAlternativo2 = entidad.NombreAlternativo2;
                        ProductoActual.Descripcion = entidad.Descripcion;
                        ProductoActual.Minimo = entidad.Minimo;
                        ProductoActual.Maximo = entidad.Maximo;
                        ProductoActual.Cantidad = entidad.Cantidad;
                        ProductoActual.TieneIdentificador = entidad.TieneIdentificador;
                        ProductoActual.Activo = entidad.Activo;
                        ProductoActual.TieneEnvase = entidad.TieneEnvase;
                        ProductoActual.CantidadEnvase = entidad.CantidadEnvase;
                        ProductoActual.TieneLote = entidad.TieneLote;

                        if (!string.IsNullOrWhiteSpace(entidad.FotografiaApp))
                        {
                            ProductoActual.FotografiaApp = string.Format(@"{0}{1}/{2}.png", UrlFotografia, ProductoActual.ProductoId, ProductoActual.ProductoId);
                        }


                        if (entidad.Precios != null && entidad.Precios.Count() > 0)
                        {
                            //Eliminar por productoId                          
                            var Precios = db.Set<ProductoPrecio>().Where(x => x.ProductoId == ProductoActual.ProductoId).ToList();
                            db.Set<ProductoPrecio>().RemoveRange(Precios);

                            //Agregar los nuevos precios
                            ProductoActual.Precios = new List<ProductoPrecio>();

                            foreach (var Precio in entidad.Precios)
                            {
                                if (!ProductoActual.Precios.Any(x => x.PrecioId == Precio.PrecioId))
                                {
                                    ProductoActual.Precios.Add(new ProductoPrecio() { ProductoId = ProductoActual.ProductoId, PrecioId = Precio.PrecioId, Valor = Precio.Valor });
                                }
                            }
                        }

                        if (entidad.Imagenes != null && entidad.Imagenes.Count() > 0)
                        {
                            int i = 1;

                            ProductoFotografia DocumentoFinal = db.Set<ProductoFotografia>().Where(x => x.ProductoId == ProductoActual.ProductoId).OrderByDescending(x => x.FotografiaId).FirstOrDefault();
                            if (DocumentoFinal != null)
                            {
                                i = DocumentoFinal.FotografiaId + 1;
                            }

                            foreach (var item in entidad.Imagenes)
                            {
                                item.FotografiaId = i++;
                                item.ProductoId = ProductoActual.ProductoId;

                                db.Set<ProductoFotografia>().Add(item);
                            }
                        }

                        if (entidad.Niveles != null && entidad.Niveles.Count() > 0)
                        {
                            //Eliminar por productoId                          
                            var Niveles = db.Set<ProductoNivelPrecio>().Where(x => x.ProductoId == ProductoActual.ProductoId).ToList();
                            db.Set<ProductoNivelPrecio>().RemoveRange(Niveles);

                            //Agregar los nuevos precios
                            ProductoActual.Niveles = new List<ProductoNivelPrecio>();

                            int i = 1;
                            foreach (var Precio in entidad.Niveles)
                            {
                                ProductoActual.Niveles.Add(new ProductoNivelPrecio() { NivelId = i, ProductoId = ProductoActual.ProductoId, Inicial = Precio.Inicial, Final = Precio.Final, Precio = Precio.Precio });
                                i++;
                            }
                        }

                        if (ProductoActual.Costo != entidad.Costo)
                        {
                            ProductoPrecioCosto productoCosto = db.Set<ProductoPrecioCosto>().Where(x => x.ProductoId == ProductoActual.ProductoId).FirstOrDefault();
                            productoCosto.PrecioCosto = entidad.Costo;
                        }

                        db.SaveChanges();

                        if (Mensaje.Equals("OK"))
                        {
                            //Se crea carpeta por producto
                            string Path_Producto = string.Format(@"{0}\{1}", PathFotografia, entidad.ProductoId);

                            if (!(Directory.Exists(Path_Producto)))
                            {
                                Directory.CreateDirectory(Path_Producto);
                            }

                            if (entidad.Fotografia != null)
                            {
                                ConvetirbyteAImage(entidad.Fotografia.Content).Save(string.Format(@"{0}\{1}.png", Path_Producto, entidad.ProductoId));
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

            public string Guardar(Producto entidad)
            {
                string Mensaje = "OK";
               
                if (!string.IsNullOrWhiteSpace(entidad.ProductoId))
                {
                    Mensaje = Actualizar(entidad);
                }
                else
                {
                    Mensaje = Agregar(entidad);    
                }
            
                return Mensaje;
            }

            public string Eliminar(Producto entidad) 
            {
                string Mensaje = "OK";

                try
                {
                    Producto ProductoActual = db.Set<Producto>().Where(x => x.ProductoId == entidad.ProductoId).FirstOrDefault();
                    if (ProductoActual != null)
                    {
                        db.Set<Producto>().Remove(ProductoActual);
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }
                
                return Mensaje;
            }

            public bool ExisteBarra(string barra)
            {
                return db.Set<Producto>().AsNoTracking().Where(x => x.Codigo.ToLower().Equals(barra.ToLower())).Count() > 0;
            }

            public Producto ObtenerPorId(string id, bool todos = true, bool existencia = false, bool imagen = false)
            {
                Producto ProductoActual = new Producto();

                 var costoProducto = ObtenerCosto(id);

            
                try
                {
                    if (todos)
                    {
                        if (imagen)
                        {
                            ProductoActual = db.Set<Producto>().Include("Categoria").Include("Marca").Include("Unidad").Include("Precios").Include("Niveles").Include("Imagenes").Where(x => x.ProductoId == id).FirstOrDefault();
                        }
                        else 
                        {
                            ProductoActual = db.Set<Producto>().Include("Categoria").Include("Marca").Include("Unidad").Include("Precios").Include("Niveles").Where(x => x.ProductoId == id).FirstOrDefault();
                        }
                    ProductoActual.PrecioActual = ObtenerPrecioActualPorProductoId(ProductoActual.ProductoId, ProductoActual.UnidadId).Valor;
                }
                    else
                    {
                        if (existencia)
                        {
                            ProductoActual = db.Set<Producto>().Where(x => x.ProductoId == id).FirstOrDefault();
                            if (ProductoActual != null)
                            {
                                decimal Existencia = db.Set<ProductoInventario>().Where(x => x.ProductoId == id).Sum(x => x.Cantidad);                               
                            }
                        }
                        else
                        {
                            ProductoActual = db.Set<Producto>().Where(x => x.ProductoId == id).FirstOrDefault();

                       
                    }
                    ProductoActual.PrecioActual = ObtenerPrecioActualPorProductoId(ProductoActual.ProductoId, ProductoActual.UnidadId).Valor;
                }
                }
                catch (Exception)
                {               
                }

                ProductoActual.Costo = costoProducto;
                return ProductoActual;
            }

            public Producto ObtenerPorId(long agenciaId, string id, bool todos = true, bool existencia = false, bool imagen = false)
            {
                Producto ProductoActual = new Producto();

                try
                {
                    if (todos)
                    {
                        if (imagen)
                        {
                            ProductoActual = db.Set<Producto>().Include("Categoria").Include("Marca").Include("Unidad").Include("Niveles").Include("Imagenes").Where(x => x.ProductoId == id).FirstOrDefault();
                        }
                        else
                        {
                            ProductoActual = db.Set<Producto>().Include("Categoria").Include("Marca").Include("Unidad").Include("Niveles").Where(x => x.ProductoId == id).FirstOrDefault();
                        }
                    }
                    else
                    {
                        if (existencia)
                        {
                            ProductoActual = db.Set<Producto>().Where(x => x.ProductoId == id).FirstOrDefault();
                            if (ProductoActual != null && !string.IsNullOrWhiteSpace(ProductoActual.ProductoId))
                            {
                                decimal Existencia = db.Set<ProductoInventario>().Where(x => x.AgenciaId == agenciaId && x.ProductoId == id).Sum(x => x.Cantidad);
                                ProductoActual.Existencia = Existencia;
                            }
                        }
                        else
                        {
                            ProductoActual = db.Set<Producto>().Where(x => x.ProductoId == id).FirstOrDefault();
                        }
                    }
                }
                catch (Exception)
                {
                }

                return ProductoActual;
            }
        public Decimal ObtenerCosto(string productoId)
        {
            var Costo = db.ProductoPrecioCostos.Where(x => x.ProductoId == productoId).Select(x => x.PrecioCosto).FirstOrDefault();
            return Costo;
        }

        public Producto ObtenerExistenciaPorAgenciaYProducto(long agenciaId, string productoId, long unidadId, bool precioVigente = false, bool empleado = false)
        {
                Producto ProductoActual = new Producto();

                try
                {
                    ProductoActual = db.Set<Producto>().AsNoTracking().Where(x => x.ProductoId == productoId && x.Activo == true).FirstOrDefault();
                    if (ProductoActual != null)
                    {
                        bool UnidadPadre = false;
                        decimal Existencia = 0;

                        ProductoInventario ExistenciaActual = db.Set<ProductoInventario>().AsNoTracking().Where(x => x.ProductoId == productoId && x.AgenciaId == agenciaId).FirstOrDefault();
                        if (ExistenciaActual != null)
                        {
                            Existencia = ExistenciaActual.Cantidad;                            
                        }
                      
                        if (ProductoActual.UnidadId == unidadId)
                        {
                            UnidadPadre = true;
                        }

                        if (!UnidadPadre)
                        {
                            Producto ProductoHijoActual = db.Set<Producto>().AsNoTracking().Where(x => x.ProductoPadreId == productoId && x.UnidadId == unidadId).FirstOrDefault();
                            if (ProductoHijoActual != null)
                            {
                                if (Existencia > 0)
                                {
                                    Existencia = decimal.Round(Existencia / ProductoHijoActual.Cantidad, 2);
                                }
                            }
                        }

                        decimal PrecioCosto = 0;
                        decimal PrecioVentaMinimo = 0;
                        decimal Precio = 0;

                        if (precioVigente)
                        {
                            if (empleado)
                            {
                                ProductoPrecioCosto PrecioActual = db.Set<ProductoPrecioCosto>().AsNoTracking().Where(x => x.ProductoId == productoId).FirstOrDefault();
                                if (PrecioActual != null)
                                {
                                    decimal IncrementoCompraEmpleado = 1;
                                    Configuracion ConfiguracionActual = db.Set<Configuracion>().AsNoTracking().Where(x => x.Identificador.Equals("CompraColaborador")).FirstOrDefault();
                                    if (ConfiguracionActual != null)
                                    {
                                        IncrementoCompraEmpleado = decimal.Parse(ConfiguracionActual.Valor);                                       
                                    }

                                    Precio = PrecioActual.PrecioCosto + IncrementoCompraEmpleado;
                                }
                            }
                            else
                            {
                                ProductoPrecioCosto PrecioCostoActual = db.Set<ProductoPrecioCosto>().AsNoTracking().Where(x => x.ProductoId == productoId).FirstOrDefault();
                                if (PrecioCostoActual != null)
                                {
                                    PrecioCosto = PrecioCostoActual.PrecioCosto;
                                }

                                ProductoPrecio PrecioActual = db.Set<ProductoPrecio>().AsNoTracking().Where(x => x.ProductoId == productoId && x.PrecioId == 5).FirstOrDefault();
                                if (PrecioActual != null)
                                {
                                    Precio = PrecioActual.Valor;
                                }
                            }
                            
                            ProductoActual.Precios = new List<ProductoPrecio>();
                        }

                        //Se obtiene el precio venta minimo     
                        ProductoNivelPrecio PrecioEscalaActual = db.Set<ProductoNivelPrecio>().AsNoTracking().Where(x => x.ProductoId == productoId && x.Inicial == 1).FirstOrDefault();
                        if (PrecioEscalaActual != null)
                        {
                            Precio = PrecioEscalaActual.Precio;  
                            PrecioVentaMinimo = PrecioEscalaActual.Precio;                             
                        }

                        if (PrecioVentaMinimo == 0)
                        {
                            PrecioVentaMinimo = Precio;
                        }

                        ProductoActual.Costo = PrecioCosto;
                        ProductoActual.PrecioCostoDescuento = PrecioVentaMinimo;
                        ProductoActual.PrecioActual = Precio;
                        ProductoActual.Existencia = Existencia;
                    }
                }
                catch (Exception)
                {
                }

                return ProductoActual;
            }

        public Producto ObtenerExistenciaPorAgenciaYProductoSinEscala(long agenciaId, string productoId, long unidadId, bool precioVigente = false, bool empleado = false)
        {
            Producto ProductoActual = new Producto();
            Producto ProductoFinalActual = new Producto();
            string productoBaseId = string.Empty;

            try
            {                
                ProductoActual = db.Set<Producto>().AsNoTracking().Where(x => x.ProductoId == productoId && x.Activo).FirstOrDefault();             

                if (ProductoActual != null)
                {
                    bool UnidadPadre = false;
                    decimal Existencia = 0;

                    //Se obtiene el codigo padre
                    if (!string.IsNullOrWhiteSpace(ProductoActual.ProductoPadreId))
                    {
                        productoBaseId = ProductoActual.ProductoPadreId;
                    }

                    ProductoInventario ExistenciaActual = new ProductoInventario();

                    if (!string.IsNullOrWhiteSpace(productoBaseId))
                    {
                        ExistenciaActual = db.Set<ProductoInventario>().AsNoTracking().Where(x => x.ProductoId == productoBaseId && x.AgenciaId == agenciaId).FirstOrDefault();
                    }
                    else
                    {
                        ExistenciaActual = db.Set<ProductoInventario>().AsNoTracking().Where(x => x.ProductoId == productoId && x.AgenciaId == agenciaId).FirstOrDefault();
                    }
                    
                    if (ExistenciaActual != null)
                    {
                        Existencia = ExistenciaActual.Cantidad;
                    }

                    if (ProductoActual.UnidadId != unidadId && !string.IsNullOrWhiteSpace(productoBaseId))
                    {
                        ProductoActual = db.Set<Producto>().AsNoTracking().Where(x => x.ProductoId == productoBaseId && x.UnidadId == unidadId && x.Activo).FirstOrDefault();
                        if (ProductoActual != null)
                        {
                            productoId = ProductoActual.ProductoId;
                            unidadId = ProductoActual.UnidadId;
                        }
                    }

                    if (ProductoActual.UnidadId == unidadId)
                    {
                        UnidadPadre = true;
                    }                

                    if (!UnidadPadre)
                    {
                        Producto ProductoHijoActual = db.Set<Producto>().AsNoTracking().Where(x => x.ProductoPadreId == productoId && x.UnidadId == unidadId).FirstOrDefault();
                        if (ProductoHijoActual != null)
                        {
                            if (Existencia > 0)
                            {
                                Existencia = decimal.Round(Existencia / ProductoHijoActual.Cantidad, 2);
                            }

                            ProductoFinalActual = ProductoHijoActual;
                            productoId = ProductoFinalActual.ProductoId;
                        }
                        else
                        {
                            ProductoFinalActual = ProductoActual;
                        }
                    }
                    else
                    {
                        ProductoFinalActual = ProductoActual;
                    }

                    decimal PrecioCosto = 0;
                    decimal PrecioVentaMinimo = 0;
                    decimal Precio = 0;

                    if (precioVigente)
                    {
                        if (empleado)
                        {
                            ProductoPrecioCosto PrecioActual = db.Set<ProductoPrecioCosto>().AsNoTracking().Where(x => x.ProductoId == productoId).FirstOrDefault();
                            if (PrecioActual != null)
                            {
                                decimal IncrementoCompraEmpleado = 1;
                                Configuracion ConfiguracionActual = db.Set<Configuracion>().AsNoTracking().Where(x => x.Identificador.Equals("CompraColaborador")).FirstOrDefault();
                                if (ConfiguracionActual != null)
                                {
                                    IncrementoCompraEmpleado = decimal.Parse(ConfiguracionActual.Valor);
                                }

                                Precio = PrecioActual.PrecioCosto + IncrementoCompraEmpleado;
                            }
                        }
                        else
                        {
                            ProductoPrecioCosto PrecioCostoActual = db.Set<ProductoPrecioCosto>().AsNoTracking().Where(x => x.ProductoId == productoId).FirstOrDefault();
                            if (PrecioCostoActual != null)
                            {
                                PrecioCosto = PrecioCostoActual.PrecioCosto;
                            }

                            ProductoPrecio PrecioActual = db.Set<ProductoPrecio>().AsNoTracking().Where(x => x.ProductoId == productoId && x.PrecioId == 5).FirstOrDefault();
                            if (PrecioActual != null)
                            {
                                Precio = PrecioActual.Valor;
                            }
                        }

                        ProductoActual.Precios = new List<ProductoPrecio>();
                    }

                    //Se obtiene el precio venta minimo     
                    ProductoPrecio PrecioMinimoActual = db.Set<ProductoPrecio>().AsNoTracking().Where(x => x.ProductoId == productoId && x.PrecioId == 4).FirstOrDefault();
                    if (PrecioMinimoActual != null)
                    {                      
                        PrecioVentaMinimo = PrecioMinimoActual.Valor;
                    }

                    if (PrecioVentaMinimo == 0)
                    {
                        PrecioVentaMinimo = Precio;
                    }

                    ProductoFinalActual.Costo = PrecioCosto;
                    ProductoFinalActual.PrecioCostoDescuento = PrecioVentaMinimo;
                    ProductoFinalActual.PrecioActual = Precio;
                    ProductoFinalActual.Existencia = Existencia;
                }
            }
            catch (Exception)
            {}

            return ProductoFinalActual;
        }

        public Producto ObtenerPrecioPorAgenciaYProducto(long agenciaId, string productoId, long unidadId, int cantidad, bool precioVigente = false, bool empleado = false)
        {
            Producto ProductoActual = new Producto();

            try
            {
                ProductoActual = db.Set<Producto>().AsNoTracking().Where(x => x.ProductoId == productoId && x.Activo == true).FirstOrDefault();
                if (ProductoActual != null)
                {  
                    decimal PrecioCosto = 0;
                    decimal PrecioVentaMinimo = 0;
                    decimal Precio = 0;

                    if (precioVigente)
                    {
                        if (empleado)
                        {
                            ProductoPrecioCosto PrecioActual = db.Set<ProductoPrecioCosto>().AsNoTracking().Where(x => x.ProductoId == productoId).FirstOrDefault();
                            if (PrecioActual != null)
                            {
                                decimal IncrementoCompraEmpleado = 1;
                                Configuracion ConfiguracionActual = db.Set<Configuracion>().AsNoTracking().Where(x => x.Identificador.Equals("CompraColaborador")).FirstOrDefault();
                                if (ConfiguracionActual != null)
                                {
                                    IncrementoCompraEmpleado = decimal.Parse(ConfiguracionActual.Valor);
                                }

                                Precio = PrecioActual.PrecioCosto + IncrementoCompraEmpleado;
                            }
                        }
                        else
                        {
                            ProductoPrecioCosto PrecioCostoActual = db.Set<ProductoPrecioCosto>().AsNoTracking().Where(x => x.ProductoId == productoId).FirstOrDefault();
                            if (PrecioCostoActual != null)
                            {
                                PrecioCosto = PrecioCostoActual.PrecioCosto;
                            }

                            ProductoPrecio PrecioActual = db.Set<ProductoPrecio>().AsNoTracking().Where(x => x.ProductoId == productoId && x.PrecioId == 5).FirstOrDefault();
                            if (PrecioActual != null)
                            {
                                Precio = PrecioActual.Valor;
                            }
                        }

                        ProductoActual.Precios = new List<ProductoPrecio>();
                    }

                    ProductoNivelPrecio PrecioEscalaActual = db.Set<ProductoNivelPrecio>().AsNoTracking().Where(x => x.ProductoId == productoId && cantidad >= x.Inicial && cantidad <= x.Final).FirstOrDefault();
                    if (PrecioEscalaActual != null)
                    {
                        Precio = PrecioEscalaActual.Precio;
                        PrecioVentaMinimo = PrecioEscalaActual.Precio;
                    }

                    if (PrecioVentaMinimo == 0)
                    {
                        PrecioVentaMinimo = Precio;
                    }

                    ProductoActual.Costo = PrecioCosto;
                    ProductoActual.PrecioCostoDescuento = PrecioVentaMinimo;
                    ProductoActual.PrecioActual = Precio;                   
                }
            }
            catch (Exception)
            {
            }

            return ProductoActual;
        }

            public Producto ObtenerProductoxBarra(string barra) 
            {
                Producto ProductoActual = new Producto();

                try
                {
                    if (barra.Contains("-"))
                    {
                        barra = barra.Substring(0, barra.LastIndexOf("-"));
                        barra = barra.Trim();
                    }

                    ProductoActual = db.Set<Producto>().Where(x => (x.ProductoId.Equals(barra) || x.Codigo.Equals(barra) || x.Nombre.Equals(barra) || x.NombreAlternativo1.Equals(barra) || x.NombreAlternativo2.Equals(barra)) && (x.Activo == true)).AsEnumerable().Select(x => new Producto() { ProductoId = x.ProductoId, Nombre = string.Format("{0} - {1}", x.Codigo, x.Nombre) }).FirstOrDefault();

                    List<Producto> Productos = BuscarProductoxTextoLibre(barra);
                }
                catch (Exception)
                {
                }

                return ProductoActual;
            }

            public List<Producto> BuscarProductoxTextoLibre(string search, long empresaId = 0)
            {
                List<Producto> Productos = new List<Producto>();

                try
                {
                    if (search.Contains("-"))
                    {
                        search = search.Substring(0, search.LastIndexOf("-"));
                        search = search.Trim();
                    }

                    List<ProductoConsultaModel> Consultas = db.Database.SqlQuery<ProductoConsultaModel>("dbo.sp_busqueda_libre_de_producto @Buscar, @EmpresaId", new SqlParameter("@Buscar", search), new SqlParameter("@EmpresaId", empresaId)).ToList();
                    if (Consultas != null && Consultas.Count() > 0)
                    {
                        Productos = Consultas.Select(x => new Producto() { ProductoId = x.ProductoId, Nombre = x.Nombre }).ToList();                       
                    }
                }
                catch (Exception)
                {
                }

                return Productos;
            }

            public List<Producto> BuscarProductoxAutocompletar(string search, bool? id = null)
            {
                List<Producto> Productos = new List<Producto>();

                try
                {
                    List<ProductoConsultaModel> Consultas = new List<ProductoConsultaModel>();

                    if (id == null)
                    {
                        Consultas = db.Database.SqlQuery<ProductoConsultaModel>("dbo.sp_busqueda_libre_producto @Buscar, @ID", new SqlParameter("@Buscar", search), new SqlParameter("@ID", DBNull.Value)).ToList();
                    }
                    else
                    {
                        Consultas = db.Database.SqlQuery<ProductoConsultaModel>("dbo.sp_busqueda_libre_producto @Buscar, @ID", new SqlParameter("@Buscar", search), new SqlParameter("@ID", id)).ToList();
                    }

                    if (Consultas != null && Consultas.Count() > 0)
                    {
                        Productos = Consultas.Select(x => new Producto() { ProductoId = x.ProductoId, Nombre = x.Nombre }).ToList();
                    }
                }
                catch (Exception)
                {
                }

                return Productos;
            }

            public List<Producto> BuscarProductoxAutocompletarExistencia(string search, long agenciaId, long empresaId, bool? id = null)
            {
                List<Producto> Productos = new List<Producto>();

                try
                {
                    List<ProductoConsultaModel> Consultas = new List<ProductoConsultaModel>();

                    if (id == null)
                    {
                        Consultas = db.Database.SqlQuery<ProductoConsultaModel>("dbo.sp_busqueda_libre_producto_existencia @Buscar, @ID, @AgenciaId, @EmpresaId", new SqlParameter("@Buscar", search), new SqlParameter("@ID", DBNull.Value), new SqlParameter("@AgenciaId", agenciaId), new SqlParameter("@EmpresaId", empresaId)).ToList();
                    }
                    else
                    {
                        Consultas = db.Database.SqlQuery<ProductoConsultaModel>("dbo.sp_busqueda_libre_producto_existencia @Buscar, @ID, @AgenciaId, @EmpresaId", new SqlParameter("@Buscar", search), new SqlParameter("@ID", id), new SqlParameter("@AgenciaId", agenciaId), new SqlParameter("@EmpresaId", empresaId)).ToList();
                    }

                    if (Consultas != null && Consultas.Count() > 0)
                    {
                        Productos = Consultas.AsEnumerable().Select(x => new Producto() { ProductoId = x.ProductoId, Nombre = string.Format("{0} - ({1})", x.Nombre, x.Existencia) }).ToList();
                    }
                }
                catch (Exception)
                {
                }

                return Productos;
            }

            public List<Producto> ObtenerProductoPorCategoriaIdYMarcaId(long categoriaId, long marcaId)
            {
                List<Producto> Productos = new List<Producto>();

                try
                {
                    Productos = db.Set<Producto>().Include("Unidad").AsNoTracking().Where(x => x.CategoriaId == categoriaId && x.MarcaId == marcaId && x.ProductoPadreId == null).AsEnumerable().Select(x => new Producto() { ProductoId = x.ProductoId, Nombre = string.Format("{0} - {1} - {2}", x.Codigo, x.Nombre, x.Unidad.Nombre) }).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ProductoId).ToList();
                }
                catch (Exception)
                {
                }

                return Productos;
            }

            public List<Producto> ObtenerProductoPorCategoriaId(long categoriaId)
            {
                List<Producto> Productos = new List<Producto>();

                try
                {
                    Productos = db.Set<Producto>().AsNoTracking().Where(x => x.CategoriaId == categoriaId).AsEnumerable().Select(x => new Producto() { ProductoId = x.ProductoId, Nombre = string.Format("{0} - {1}", x.Codigo, x.Nombre) }).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ProductoId).ToList();                   
                }
                catch (Exception)
                {}

                return Productos;
            }
        public List<Producto> ObtenerProductoPorCategoriaIdConPrecio(long categoriaId)
        {
            List<Producto> Productos = new List<Producto>();

            try
            {
                Productos = db.Set<Producto>().AsNoTracking().Where(x => x.CategoriaId == categoriaId&&x.Activo).AsEnumerable().Select(x => new Producto() { ProductoId = x.ProductoId, Nombre =  x.Nombre, UnidadId=x.UnidadId,FotografiaApp= x.FotografiaApp,Descripcion= x.Descripcion }).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ProductoId).ToList();
                foreach (Producto item in Productos) 
                {
                    item.PrecioActual = ObtenerPrecioActualPorProductoId(item.ProductoId,item.UnidadId).Valor;
                }
            }
            catch (Exception)
            {}

            return Productos;
        }

        public List<Producto> ObtenerProductoPorCategoriaIdConPrecioExistenciaAgencia(long categoriaId,long AgenciaId)
        {
            List<Producto> Productos = new List<Producto>();
            List<Producto> Productosv = new List<Producto>();
            
                Productosv = db.Set<Producto>().AsNoTracking().Where(x => x.CategoriaId == categoriaId && x.Activo).AsEnumerable().Select(x => new Producto() { ProductoId = x.ProductoId, Nombre = x.Nombre, UnidadId = x.UnidadId, FotografiaApp = x.FotografiaApp, Descripcion = x.Descripcion }).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ProductoId).ToList();                
                foreach (Producto item in Productosv)
                {
                    try
                    {
                        item.PrecioActual = ObtenerPrecioActualPorProductoId(item.ProductoId, item.UnidadId).Valor;
                        ProductoInventario inv= db.ProductoInventarios.Where(x => x.ProductoId == item.ProductoId && x.AgenciaId == AgenciaId).FirstOrDefault();
                        if (inv != null) {
                            if (inv.Cantidad > 0) {
                                item.Existencia = inv.Cantidad;
                                Productos.Add(item);
                            }
                        }
                    }
                    catch (Exception)
                    {}
                }           

            return Productos;
        }

        public List<Producto> ObtenerProductoPorCategoriaIdConPrecioExistenciaAgenciaCorto(long categoriaId, long AgenciaId)
        {
            List<Producto> Productos = new List<Producto>();
            List<Producto> Productosv = new List<Producto>();

            Productosv = db.Set<Producto>().AsNoTracking().Where(x => x.CategoriaId == categoriaId && x.Activo).AsEnumerable().Select(x => new Producto() { ProductoId = x.ProductoId, Nombre = x.Nombre, UnidadId = x.UnidadId, FotografiaApp = x.FotografiaApp, Descripcion = x.Descripcion }).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ProductoId).ToList();
            
            foreach (Producto item in Productosv)
            {
                try
                {
                    item.PrecioActual = ObtenerPrecioActualPorProductoId(item.ProductoId, item.UnidadId).Valor;
                    ProductoInventario inv = db.ProductoInventarios.Where(x => x.ProductoId == item.ProductoId && x.AgenciaId == AgenciaId).FirstOrDefault();
                    if (inv != null)
                    {
                        if (inv.Cantidad > 0)
                        {
                            item.Existencia = inv.Cantidad;
                            Productos.Add(item);
                        }
                    }
                }
                catch (Exception)
                {}
            }



            return Productos;
        }
        public List<ProductoPrecio> ObtenerPrecioPorProductoId(string productoId, long presentacionId)
            {
                List<ProductoPrecio> Precios = new List<ProductoPrecio>();

                try
                {
                    Producto ProductoActual = db.Set<Producto>().Where(x => (x.ProductoId == productoId || x.ProductoPadreId == productoId) && (x.UnidadId == presentacionId)).FirstOrDefault();
                    if (ProductoActual != null)
                    {
                        Precios = db.Set<ProductoPrecio>().Include("Precio").Where(x => x.ProductoId == ProductoActual.ProductoId).AsEnumerable().Select(x => new ProductoPrecio() { PrecioId = x.PrecioId, Nombre = string.Format("{0} - {1}", x.Precio.Nombre, x.Valor.ToString("C")), Valor = x.Valor }).OrderByDescending(x => x.PrecioId).ToList();
                    }
                }
                catch (Exception)
                {}

                return Precios;
            }

            public ProductoPrecio ObtenerPrecioActualPorProductoId(string productoId, long presentacionId)
            {
                ProductoPrecio PrecioActual = new ProductoPrecio();

                try
                {
                    Producto ProductoActual = db.Set<Producto>().Where(x => (x.ProductoId == productoId || x.ProductoPadreId == productoId) && (x.UnidadId == presentacionId)).FirstOrDefault();
                    if (ProductoActual != null)
                    {
                        PrecioActual = db.Set<ProductoPrecio>().Where(x => x.ProductoId == ProductoActual.ProductoId && x.PrecioId == 5).FirstOrDefault();
                    }
                }
                catch (Exception)
                {
                }

                return PrecioActual;
            }

            public List<Producto> ObtenerListado(bool formato = false, bool todos = true, bool todosPadre = false)
            {
                List<Producto> Productos = new List<Producto>();

                try
                {
                    if (formato)
                    {
                        if (todos)
                        {
                            Productos = db.Set<Producto>().Include("Categoria").Include("Marca").Include("Unidad").AsEnumerable().Select(x => new Producto() { ProductoId = x.ProductoId, Nombre = x.Nombre }).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ProductoId).ToList();
                        }
                        else
                        {
                            if (todosPadre)
                            {
                                Productos = db.Set<Producto>().Include("Categoria").Include("Marca").Include("Unidad").Where(x => x.Activo == true && x.ProductoPadreId == null).AsEnumerable().Select(x => new Producto() { ProductoId = x.ProductoId, Nombre = string.Format("{0} - {1}", x.Codigo, x.Nombre) }).OrderBy(x => x.Nombre).ToList();
                            }
                            else
                            {
                                Productos = db.Set<Producto>().Include("Categoria").Include("Marca").Include("Unidad").Where(x => x.Activo == true).AsEnumerable().Select(x => new Producto() { ProductoId = x.ProductoId, Nombre = x.Nombre }).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ProductoId).ToList();
                            }
                        }
                    }
                    else
                    {
                        Productos = db.Set<Producto>().Include("Categoria").Include("Marca").Include("Unidad").Where(x => x.ProductoPadreId == null).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ProductoId).ToList();
                    }
                }
                catch (Exception)
                {
                }

                return Productos;
            }

            public List<Producto> ObtenerProductos(long empresaId)
            {
                List<Producto> Productos = new List<Producto>();

                try
                {
                    Productos = db.Set<Producto>().Include("Categoria").Include("Marca").Include("Unidad").Include("Precios").AsNoTracking().Where(x => x.EmpresaId == empresaId).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ProductoId).Take(200).ToList();
                    if (Productos != null && Productos.Count() > 0)
                    {
                        Productos.ForEach(x => 
                        {
                            x.PrecioActual = x.Precios.Where(y => y.PrecioId == 5).Sum(z => z.Valor);

                             //Se verifica que el codigo del producto no tenga ningun tipo de movimiento
                            bool TieneMovimientos = db.Set<MovimientoDetalle>().AsNoTracking().Where(y => y.ProductoId == x.ProductoId).Count() > 0;
                            bool TieneTraslados = db.Set<TrasladoDetalle>().AsNoTracking().Where(y => y.ProductoId == x.ProductoId).Count() > 0;
                            bool TieneCreditos = db.Set<CreditoDetalle>().AsNoTracking().Where(y => y.ProductoId == x.ProductoId).Count() > 0;
                            bool TieneFacturas = db.Set<FacturaDetalle>().AsNoTracking().Where(y => y.ProductoId == x.ProductoId).Count() > 0;
                            bool TienePedidos = db.Set<PedidoDetalle>().AsNoTracking().Where(y => y.ProductoId == x.ProductoId).Count() > 0;

                            if (!TieneMovimientos && !TieneTraslados && !TieneCreditos && !TieneFacturas && !TienePedidos)
                            {
                                x.Eliminar = true;                                
                            }
                        });                      
                    }
                }
                catch (Exception)
                {
                }

                return Productos;
            }

            public List<InventarioModel> ObtenerProductosExistenciaxAgencia(long usuarioId)
            {
                List<InventarioModel> Productos = new List<InventarioModel>();
                List<long> AgenciaIds = new List<long>();

                try
                {
                    AgenciaIds = db.Set<UsuarioAgenciaConsulta>().Where(x => x.UsuarioId == usuarioId).Select(x => x.AgenciaId).ToList();
                    if (AgenciaIds != null && AgenciaIds.Count() > 0)
                    {
                        Productos = db.Set<Producto>().Include("Marca").Include("Unidad").Include("Precios").AsNoTracking().AsEnumerable().Select(x => new InventarioModel() { ProductoId = x.ProductoId, Codigo = x.Codigo, Nombre = x.Nombre, Unidad = x.Unidad.Nombre, Marca = x.Marca.Nombre, PrecioVenta = x.Precios.Where(y => y.ProductoId == x.ProductoId && y.PrecioId == 5).Sum(y => y.Valor), PrecioValidar = x.Precios.Where(y => y.ProductoId == x.ProductoId && y.PrecioId == 6).Count() > 0 ? x.Precios.Where(y => y.ProductoId == x.ProductoId && y.PrecioId == 6).Sum(y => y.Valor) : 0, Activo = x.Activo, TieneLote = x.TieneLote }).Join(db.Set<ProductoInventario>().Include("Agencia").AsNoTracking().Where(x => AgenciaIds.Contains(x.AgenciaId)), P => P.ProductoId, A => A.ProductoId, (P, A) => new InventarioModel() { AgenciaId = A.AgenciaId, Agencia = A.Agencia.Nombre, ProductoId = P.ProductoId, Codigo = P.Codigo, Nombre = P.Nombre, Unidad = P.Unidad, Marca = P.Marca, PrecioVenta = P.PrecioVenta, PrecioValidar = P.PrecioValidar, Activo = P.Activo, TieneLote = P.TieneLote, Existencia = A.Cantidad }).Take(200).ToList();
                    } 
                }
                catch (Exception)
                {
                }

                return Productos;
            }

            public List<InventarioModel> ConsultaAdministrativaProductosExistenciaxAgencia(long usuarioId)
            {
                List<InventarioModel> Productos = new List<InventarioModel>();
                List<long> AgenciaIds = new List<long>();

                try
                {
                    AgenciaIds = db.Set<UsuarioAgenciaConsulta>().Where(x => x.UsuarioId == usuarioId).Select(x => x.AgenciaId).ToList();
                    if (AgenciaIds != null && AgenciaIds.Count() > 0)
                    {
                        Productos = db.Set<Producto>().Include("Marca").Include("Unidad").Include("Precios").AsNoTracking().AsEnumerable().Select(x => new InventarioModel() { ProductoId = x.ProductoId, Codigo = x.Codigo, Nombre = x.Nombre, Unidad = x.Unidad.Nombre, Marca = x.Marca.Nombre, PrecioVenta = x.Precios.Where(y => y.ProductoId == x.ProductoId && y.PrecioId == 5).Sum(y => y.Valor), PrecioValidar = x.Precios.Where(y => y.ProductoId == x.ProductoId && y.PrecioId == 6).Count() > 0 ? x.Precios.Where(y => y.ProductoId == x.ProductoId && y.PrecioId == 6).Sum(y => y.Valor) : 0, Activo = x.Activo }).Join(db.Set<ProductoInventario>().Include("Agencia").AsNoTracking().Where(x => AgenciaIds.Contains(x.AgenciaId)), P => P.ProductoId, A => A.ProductoId, (P, A) => new InventarioModel() { AgenciaId = A.AgenciaId, Agencia = A.Agencia.Nombre, ProductoId = P.ProductoId, Codigo = P.Codigo, Nombre = P.Nombre, Unidad = P.Unidad, Marca = P.Marca, PrecioVenta = P.PrecioVenta, PrecioValidar = P.PrecioValidar, Activo = P.Activo, Existencia = A.Cantidad }).Take(200).ToList();
                        if (Productos != null && Productos.Count() > 0)
                        {
                            Productos.ForEach(x => 
                            {
                                //Se obtiene el precio costo
                                ProductoPrecioCosto PrecioCostoActual = db.Set<ProductoPrecioCosto>().AsNoTracking().Where(y => y.ProductoId.Equals(x.ProductoId)).FirstOrDefault();
                                if (PrecioCostoActual != null)
                                {
                                    x.PrecioCosto = PrecioCostoActual.PrecioCosto;                                    
                                }

                                //Se obtiene al ultimo proveedor
                                Proveedor UltimoIngreso = db.Set<MovimientoDetalle>().AsNoTracking().Where(y => y.ProductoId.Equals(x.ProductoId)).Join(db.Set<Movimiento>().Include("Proveedor").AsNoTracking().Where(y => !y.Anulada), MD => MD.MovimientoId, M => M.MovimientoId, (MD, M) => new { M }).OrderByDescending(y => y.M.Fecha).ThenByDescending(y => y.M.MovimientoId).Select(y => y.M.Proveedor).FirstOrDefault();
                                if (UltimoIngreso != null)
                                {
                                    x.Proveedor = UltimoIngreso.Nombre;
                                }
                                else
                                {
                                    x.Proveedor = "Sin Proveedor";
                                }
                            });                            
                        }
                    }
                }
                catch (Exception)
                {
                }

                return Productos;
            }

            public List<Producto> Buscar(string search, long empresaId)
            {
                List<Producto> Productos = new List<Producto>();

                try
                {
                    Productos = db.Set<Producto>().Include("Categoria").Include("Marca").Include("Unidad").Include("Precios").AsNoTracking().Where(x => (x.Codigo.Contains(search) || x.Nombre.Contains(search) || x.NombreAlternativo1.Contains(search) || x.NombreAlternativo2.Contains(search) || x.Marca.Nombre.Contains(search)) && x.EmpresaId == empresaId).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ProductoId).Take(200).ToList();
                    if (Productos != null && Productos.Count() > 0)
                    {
                        Productos.ForEach(x =>
                        {
                            x.PrecioActual = x.Precios.Where(y => y.PrecioId == 5).Sum(z => z.Valor);

                            //Se verifica que el codigo del producto no tenga ningun tipo de movimiento
                            bool TieneMovimientos = db.Set<MovimientoDetalle>().AsNoTracking().Where(y => y.ProductoId == x.ProductoId).Count() > 0;
                            bool TieneTraslados = db.Set<TrasladoDetalle>().AsNoTracking().Where(y => y.ProductoId == x.ProductoId).Count() > 0;
                            bool TieneCreditos = db.Set<CreditoDetalle>().AsNoTracking().Where(y => y.ProductoId == x.ProductoId).Count() > 0;
                            bool TieneFacturas = db.Set<FacturaDetalle>().AsNoTracking().Where(y => y.ProductoId == x.ProductoId).Count() > 0;
                            bool TienePedidos = db.Set<PedidoDetalle>().AsNoTracking().Where(y => y.ProductoId == x.ProductoId).Count() > 0;

                            if (!TieneMovimientos && !TieneTraslados && !TieneCreditos && !TieneFacturas && !TienePedidos)
                            {
                                x.Eliminar = true;
                            }
                        });                           
                    }
                }
                catch (Exception)
                {
                }

                return Productos;
            }

            public List<InventarioModel> BuscarExistenciaxAgencia(string search, long usuarioId)
            {
                List<InventarioModel> Productos = new List<InventarioModel>();
                List<long> AgenciaIds = new List<long>();

                try
                {
                    AgenciaIds = db.Set<UsuarioAgenciaConsulta>().Where(x => x.UsuarioId == usuarioId).Select(x => x.AgenciaId).ToList();
                    if (AgenciaIds != null && AgenciaIds.Count() > 0)
                    {
                        Productos = db.Set<Producto>().Include("Marca").Include("Unidad").Include("Precios").AsNoTracking().Where(x => x.Codigo.Contains(search) || x.Nombre.Contains(search) || x.NombreAlternativo1.Contains(search) || x.NombreAlternativo2.Contains(search) || x.Marca.Nombre.Contains(search)).AsEnumerable().Select(x => new InventarioModel() { ProductoId = x.ProductoId, Codigo = x.Codigo, Nombre = x.Nombre, Unidad = x.Unidad.Nombre, Marca = x.Marca.Nombre,
                            PrecioVenta = x.Precios.Where(y => y.ProductoId == x.ProductoId && y.PrecioId == 5).Sum(y => y.Valor), PrecioValidar = x.Precios.Where(y => y.ProductoId == x.ProductoId && y.PrecioId == 6).Count() > 0 ? x.Precios.Where(y => y.ProductoId == x.ProductoId && y.PrecioId == 6).Sum(y => y.Valor) : 0, Activo = x.Activo, TieneLote = x.TieneLote }).Join(db.Set<ProductoInventario>().Include("Agencia").AsNoTracking().Where(x => AgenciaIds.Contains(x.AgenciaId)), P => P.ProductoId, A => A.ProductoId, (P, A) => new InventarioModel() { AgenciaId = A.AgenciaId, Agencia = A.Agencia.Nombre, ProductoId = P.ProductoId, Codigo = P.Codigo, Nombre = P.Nombre, Unidad = P.Unidad, Marca = P.Marca, PrecioVenta = P.PrecioVenta, PrecioValidar = P.PrecioValidar, Activo = P.Activo, TieneLote = P.TieneLote, Existencia = A.Cantidad }).Take(200).ToList();                         
                    }                    
                }
                catch (Exception)
                {
                }

                return Productos;
            }

            public List<InventarioModel> ConsultaAdministrativaExistenciaxAgencia(string search, long usuarioId)
            {
                List<InventarioModel> Productos = new List<InventarioModel>();
                List<long> AgenciaIds = new List<long>();

                try
                {
                    AgenciaIds = db.Set<UsuarioAgenciaConsulta>().Where(x => x.UsuarioId == usuarioId).Select(x => x.AgenciaId).ToList();
                    if (AgenciaIds != null && AgenciaIds.Count() > 0)
                    {
                        Productos = db.Set<Producto>().Include("Marca").Include("Unidad").Include("Precios").AsNoTracking().Where(x => x.Codigo.Contains(search) || x.Nombre.Contains(search) || x.NombreAlternativo1.Contains(search) || x.NombreAlternativo2.Contains(search) || x.Marca.Nombre.Contains(search)).AsEnumerable().Select(x => new InventarioModel() { ProductoId = x.ProductoId, Codigo = x.Codigo, Nombre = x.Nombre, Unidad = x.Unidad.Nombre, Marca = x.Marca.Nombre, PrecioVenta = x.Precios.Where(y => y.ProductoId == x.ProductoId && y.PrecioId == 5).Sum(y => y.Valor), PrecioValidar = x.Precios.Where(y => y.ProductoId == x.ProductoId && y.PrecioId == 6).Count() > 0 ? x.Precios.Where(y => y.ProductoId == x.ProductoId && y.PrecioId == 6).Sum(y => y.Valor) : 0, Activo = x.Activo }).Join(db.Set<ProductoInventario>().Include("Agencia").AsNoTracking().Where(x => AgenciaIds.Contains(x.AgenciaId)), P => P.ProductoId, A => A.ProductoId, (P, A) => new InventarioModel() { AgenciaId = A.AgenciaId, Agencia = A.Agencia.Nombre, ProductoId = P.ProductoId, Codigo = P.Codigo, Nombre = P.Nombre, Unidad = P.Unidad, Marca = P.Marca, PrecioVenta = P.PrecioVenta, PrecioValidar = P.PrecioValidar, Activo = P.Activo, Existencia = A.Cantidad }).Take(200).ToList();
                        if (Productos != null && Productos.Count() > 0)
                        {
                            Productos.ForEach(x =>
                            {
                                //Se obtiene el precio costo
                                ProductoPrecioCosto PrecioCostoActual = db.Set<ProductoPrecioCosto>().AsNoTracking().Where(y => y.ProductoId.Equals(x.ProductoId)).FirstOrDefault();
                                if (PrecioCostoActual != null)
                                {
                                    x.PrecioCosto = PrecioCostoActual.PrecioCosto;
                                }

                                //Se obtiene al ultimo proveedor
                                Proveedor UltimoIngreso = db.Set<MovimientoDetalle>().AsNoTracking().Where(y => y.ProductoId.Equals(x.ProductoId)).Join(db.Set<Movimiento>().Include("Proveedor").AsNoTracking().Where(y => !y.Anulada), MD => MD.MovimientoId, M => M.MovimientoId, (MD, M) => new { M }).OrderByDescending(y => y.M.Fecha).ThenByDescending(y => y.M.MovimientoId).Select(y => y.M.Proveedor).FirstOrDefault();
                                if (UltimoIngreso != null)
                                {
                                    x.Proveedor = UltimoIngreso.Nombre;
                                }
                                else
                                {
                                    x.Proveedor = "Sin Proveedor";
                                }
                            });
                        }
                    }
                }
                catch (Exception)
                {
                }

                return Productos;
            }
            
            public Producto HistorialPorProductoId(string productoId, bool imagen = false)
            {
                Producto ProductoActual = new Producto();

                try
                {
                    if (imagen)
                    {
                        ProductoActual = ObtenerPorId(productoId, true, false, true);
                    }
                    else
                    {
                        ProductoActual = ObtenerPorId(productoId);
                    }

                    if (ProductoActual != null)
                    {
                        ProductoActual.Productos = new List<Producto>();
                        ProductoActual.Productos = db.Set<Producto>().Include("Categoria").Include("Marca").Include("Unidad").Where(x => x.ProductoPadreId == productoId).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ProductoId).ToList();

                        //Se agrega el historial del precio costo del producto
                        ProductoActual.Compras = new List<ProductoPrecioCostoHistorial>();
                        ProductoActual.Compras = db.Set<ProductoPrecioCostoHistorial>().Include("Proveedor").AsNoTracking().Where(x => x.ProductoId == productoId).OrderByDescending(x => x.HistorialId).ToList();
                    }
                }
                catch (Exception)
                {
                }

                return ProductoActual;
            }

            public List<Unidad> ObtenerPresentacionPorProductoId(string productoId)
            {
                List<Unidad> Unidades = new List<Unidad>();

                try
                {
                    Unidades = db.Set<Producto>().Include("Unidad").Where(x => x.ProductoId == productoId || x.ProductoPadreId == productoId).AsEnumerable().Select(x => x.Unidad).ToList();
                }
                catch (Exception)
                {
                }

                return Unidades;
            }

            public List<ProductoInventarioModel> ObtenerExistenciaPorPresentacion(long agenciaId, long usuarioId, long precioId, bool todo, bool transito, bool venta)
            {
                List<ProductoInventarioModel> Productos = new List<ProductoInventarioModel>();
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

                    if (todo)
                    {

                        if (venta)
                        {
                            Productos = db.Set<Producto>().Include("Unidad").Where(x => x.ProductoPadreId == null && x.Activo == true).Join(db.Set<ProductoInventario>().Where(x => AgenciaIds.Contains(x.AgenciaId)), P => P.ProductoId, PI => PI.ProductoId, (P, PI) => new ProductoInventarioModel() { ProductoId = P.ProductoId, Codigo = P.Codigo, Nombre = P.Nombre, Unidad = P.Unidad.Nombre, Existencia = transito ? PI.Transito : PI.Cantidad, AgenciaId = PI.AgenciaId }).AsEnumerable().Join(db.Set<Agencia>(), P => P.AgenciaId, C => C.AgenciaId, (P, C) => new ProductoInventarioModel() { ProductoId = P.ProductoId, Codigo = P.Codigo, Nombre = P.Nombre, Unidad = P.Unidad, Agencia = C.Nombre, Existencia = P.Existencia }).Join(db.Set<ProductoPrecio>().Where(x => x.PrecioId == precioId), P => P.ProductoId, PC => PC.ProductoId, (P, PC) => new ProductoInventarioModel() { ProductoId = P.ProductoId, Codigo = P.Codigo, Nombre = P.Nombre, Agencia = P.Agencia, Unidad = P.Unidad, Existencia = P.Existencia, Precio = PC.Valor }).ToList();
                        }
                        else
                        {
                            Productos = db.Set<Producto>().Include("Unidad").Where(x => x.ProductoPadreId == null && x.Activo == true).Join(db.Set<ProductoInventario>().Where(x => AgenciaIds.Contains(x.AgenciaId)), P => P.ProductoId, PI => PI.ProductoId, (P, PI) => new ProductoInventarioModel() { ProductoId = P.ProductoId, Codigo = P.Codigo, Nombre = P.Nombre, Unidad = P.Unidad.Nombre, Existencia = transito ? PI.Transito : PI.Cantidad, AgenciaId = PI.AgenciaId }).AsEnumerable().Join(db.Set<Agencia>(), P => P.AgenciaId, C => C.AgenciaId, (P, C) => new ProductoInventarioModel() { ProductoId = P.ProductoId, Codigo = P.Codigo, Nombre = P.Nombre, Unidad = P.Unidad, Agencia = C.Nombre, Existencia = P.Existencia }).Join(db.Set<ProductoPrecioCosto>(), P => P.ProductoId, PC => PC.ProductoId, (P, PC) => new ProductoInventarioModel() { ProductoId = P.ProductoId, Codigo = P.Codigo, Nombre = P.Nombre, Agencia = P.Agencia, Unidad = P.Unidad, Existencia = P.Existencia, Precio = PC.PrecioCosto }).ToList();
                        }

                        if (Productos != null && Productos.Count() > 0)
                        {
                            List<ProductoInventarioModel> Presentaciones = Productos.Join(db.Set<Producto>().Include("Unidad"), P => P.ProductoId, PU => PU.ProductoPadreId, (P, PU) => new ProductoInventarioModel() { ProductoId = PU.ProductoId, Codigo = PU.Codigo, Nombre = PU.Nombre, Unidad = PU.Unidad.Nombre, Agencia = P.Agencia, Existencia = P.Existencia == 0 ? 0 : decimal.Round(P.Existencia / PU.Cantidad, 2), Precio = P.Precio * PU.Cantidad }).ToList();
                            if (Presentaciones != null && Presentaciones.Count() > 0)
                            {
                                Productos.AddRange(Presentaciones);
                            }
                        }

                        if (transito)
                        {
                            Productos = Productos.Where(x => x.Existencia > 0).ToList();
                        }
                    }
                    else
                    {
                        Productos = db.Set<Producto>().Include("Unidad").Where(x => x.ProductoPadreId == null && x.Activo == true).Join(db.Set<ProductoInventario>().Where(x => AgenciaIds.Contains(x.AgenciaId)), P => P.ProductoId, PI => PI.ProductoId, (P, PI) => new ProductoInventarioModel() { ProductoId = P.ProductoId, Codigo = P.Codigo, Nombre = P.Nombre, Unidad = P.Unidad.Nombre, Existencia = PI.Cantidad, AgenciaId = PI.AgenciaId }).AsEnumerable().Join(db.Set<Agencia>(), P => P.AgenciaId, C => C.AgenciaId, (P, C) => new ProductoInventarioModel() { ProductoId = P.ProductoId, Codigo = P.Codigo, Nombre = P.Nombre, Unidad = P.Unidad, Agencia = C.Nombre, Existencia = P.Existencia }).Join(db.Set<ProductoPrecioCosto>(), P => P.ProductoId, PC => PC.ProductoId, (P, PC) => new ProductoInventarioModel() { ProductoId = P.ProductoId, Codigo = P.Codigo, Nombre = P.Nombre, Agencia = P.Agencia, Unidad = P.Unidad, Existencia = P.Existencia, Precio = PC.PrecioCosto }).ToList();
                    }
                }
                catch (Exception)
                {
                }

                return Productos;
            }

            public List<ProductoModel> ObtenerGananciaPorProductoVenta(DateTime fechaInicial, DateTime fechaFinal, long agenciaId, long usuarioId)
            {
                List<ProductoModel> ProductoVentas = new List<ProductoModel>();
                List<ProductoModel> ProductoFacturas = new List<ProductoModel>();
                List<ProductoModel> ProductoRecibos = new List<ProductoModel>();

                List<long> CentroIds = new List<long>();

                try
                {

                    if (agenciaId == 0)
                    {
                        CentroIds = db.Set<UsuarioAgencia>().Where(x => x.UsuarioId == usuarioId).Select(x => x.AgenciaId).ToList();
                    }
                    else
                    {
                        CentroIds.Add(agenciaId);
                    }

                  //  ProductoFacturas = db.Set<Factura>().Include("Agencia").Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && !x.Anulada && CentroIds.Contains(x.AgenciaId)).AsEnumerable().Select( x => new ProductoModel() { SolicitudId = x.FacturaId, Agencia = x.Agencia.Nombre, Fecha = x.Fecha }).AsEnumerable().Select(x => x).Join(db.Set<FacturaDetalle>().Include("Producto"), E => E.SolicitudId, FD => FD.FacturaId, (E, FD) => new ProductoModel() { ProductoId = FD.ProductoId, Agencia = E.Agencia, Fecha = E.Fecha, Nombre = FD.Producto.Nombre, PrecioCosto = FD.PrecioCosto, PrecioVenta = FD.Precio, Cantidad = FD.Cantidad }).ToList();
                    ProductoRecibos = db.Set<Recibo>().Include("Agencia").Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && CentroIds.Contains(x.AgenciaId) && !x.Anulada && x.Pagada).Join(db.Set<ReciboDetalle>().Include("Producto"), M => M.ReciboId, MD => MD.ReciboId, (M, MD) => new ProductoModel() { ProductoId = MD.ProductoId, Agencia = M.Agencia.Nombre, Nombre = MD.Producto.Nombre, PrecioVenta = MD.Precio, PrecioCosto = MD.PrecioCosto, Fecha = M.Fecha, Cantidad = MD.Cantidad }).ToList();

                    if (ProductoFacturas != null && ProductoFacturas.Count() > 0)
                    {
                    //    ProductoVentas.AddRange(ProductoFacturas);
                    }

                    if (ProductoRecibos != null && ProductoRecibos.Count() > 0)
                    {
                        ProductoVentas.AddRange(ProductoRecibos);
                    }

                    if (ProductoVentas != null && ProductoVentas.Count() > 0)
                    {
                        var GanaciaTotales = ProductoVentas.AsEnumerable().GroupBy(r => new { r.ProductoId, r.Agencia, r.Fecha, r.PrecioCosto, r.PrecioVenta }).Select(g => new { g.Key, Cantidad = g.Sum(X => X.Cantidad) }).ToList();
                        if (GanaciaTotales != null && GanaciaTotales.Count() > 0)
                        {
                            ProductoVentas = GanaciaTotales.Join(db.Set<Producto>(), G => G.Key.ProductoId, P => P.ProductoId, (G, P) => new ProductoModel() { ProductoId = P.ProductoId, Agencia = G.Key.Agencia, Nombre = P.Nombre, Fecha = G.Key.Fecha, Cantidad = G.Cantidad, PrecioCosto = G.Key.PrecioCosto, PrecioVenta = G.Key.PrecioVenta }).ToList();
                        }
                    }

                    if (ProductoVentas != null && ProductoVentas.Count() > 0)
                    {
                        ProductoVentas = ProductoVentas.OrderBy(x => x.Nombre).ToList();                        
                    }
                }
                catch (Exception)
                {
                }

                return ProductoVentas;
            }

            public List<ProductoModel> ObtenerGananciaConsolidadaxVenta(DateTime fechaInicial, DateTime fechaFinal, long agenciaId, long usuarioId)
            {
                List<ProductoModel> ProductoVentas = new List<ProductoModel>();
               // List<ProductoModel> ProductoFacturas = new List<ProductoModel>();
                List<ProductoModel> ProductoRecibos = new List<ProductoModel>();

                List<long> CentroIds = new List<long>();

                try
                {

                    if (agenciaId == 0)
                    {
                        CentroIds = db.Set<UsuarioAgencia>().Where(x => x.UsuarioId == usuarioId).Select(x => x.AgenciaId).ToList();
                    }
                    else
                    {
                        CentroIds.Add(agenciaId);
                    }

                   // ProductoFacturas = db.Set<Factura>().Include("Agencia").Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && !x.Anulada && CentroIds.Contains(x.AgenciaId)).AsEnumerable().Select(x => new ProductoModel() { SolicitudId = x.FacturaId, Agencia = x.Agencia.Nombre, Fecha = x.Fecha }).AsEnumerable().Select(x => x).Join(db.Set<FacturaDetalle>().Include("Producto"), E => E.SolicitudId, FD => FD.FacturaId, (E, FD) => new ProductoModel() { ProductoId = FD.ProductoId, Agencia = E.Agencia, Fecha = E.Fecha, Nombre = FD.Producto.Nombre, PrecioCosto = FD.PrecioCosto, PrecioVenta = FD.Precio, Cantidad = FD.Cantidad }).ToList();
                    ProductoRecibos = db.Set<Recibo>().Include("Agencia").Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && CentroIds.Contains(x.AgenciaId) && !x.Anulada && x.Pagada).Join(db.Set<ReciboDetalle>().Include("Producto"), M => M.ReciboId, MD => MD.ReciboId, (M, MD) => new ProductoModel() { ProductoId = MD.ProductoId, Agencia = M.Agencia.Nombre, Nombre = MD.Producto.Nombre, PrecioVenta = MD.Precio, PrecioCosto = MD.PrecioCosto, Fecha = M.Fecha, Cantidad = MD.Cantidad }).ToList();

                    //if (ProductoFacturas != null && ProductoFacturas.Count() > 0)
                    //{
                    //    ProductoVentas.AddRange(ProductoFacturas);
                    //}

                    if (ProductoRecibos != null && ProductoRecibos.Count() > 0)
                    {
                        ProductoVentas.AddRange(ProductoRecibos);
                    }

                    if (ProductoVentas != null && ProductoVentas.Count() > 0)
                    {
                        var GanaciaTotales = ProductoVentas.AsEnumerable().GroupBy(r => new { r.Agencia, r.Fecha }).Select(g => new { g.Key, Cantidad = g.Sum(x => x.Cantidad), Costo = g.Sum(x => x.Cantidad * x.PrecioCosto), Venta = g.Sum(x => x.Cantidad * x.PrecioVenta) }).ToList();
                        if (GanaciaTotales != null && GanaciaTotales.Count() > 0)
                        {
                            ProductoVentas = GanaciaTotales.Select(x => new ProductoModel() { Agencia = x.Key.Agencia, Fecha = x.Key.Fecha, Cantidad = x.Cantidad, PrecioCosto = x.Costo, PrecioVenta = x.Venta }).ToList();
                        }
                    }

                    if (ProductoVentas != null && ProductoVentas.Count() > 0)
                    {
                        ProductoVentas = ProductoVentas.OrderBy(x => x.Nombre).ToList();
                    }
                }
                catch (Exception)
                {
                }

                return ProductoVentas;
            }

            public List<ProductoModel> ObtenerGananciaConsolidadaxProductoVenta(DateTime fechaInicial, DateTime fechaFinal, long agenciaId, string productoId, long usuarioId)
            {
                List<ProductoModel> ProductoVentas = new List<ProductoModel>();
              //  List<ProductoModel> ProductoFacturas = new List<ProductoModel>();
                List<ProductoModel> ProductoRecibos = new List<ProductoModel>();

                List<long> CentroIds = new List<long>();

                try
                {

                    if (agenciaId == 0)
                    {
                        CentroIds = db.Set<UsuarioAgencia>().Where(x => x.UsuarioId == usuarioId).Select(x => x.AgenciaId).ToList();
                    }
                    else
                    {
                        CentroIds.Add(agenciaId);
                    }

                    if (string.IsNullOrWhiteSpace(productoId))
                    {
                    //    ProductoFacturas = db.Set<Factura>().Include("Agencia").Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && !x.Anulada && CentroIds.Contains(x.AgenciaId)).AsEnumerable().Select(x => new ProductoModel() { SolicitudId = x.FacturaId, Agencia = x.Agencia.Nombre, Fecha = x.Fecha }).AsEnumerable().Select(x => x).Join(db.Set<FacturaDetalle>().Include("Producto"), E => E.SolicitudId, FD => FD.FacturaId, (E, FD) => new ProductoModel() { ProductoId = FD.ProductoId, Agencia = E.Agencia, Fecha = E.Fecha, Nombre = FD.Producto.Nombre, PrecioCosto = FD.PrecioCosto, PrecioVenta = FD.Precio, Cantidad = FD.Cantidad }).ToList();
                        ProductoRecibos = db.Set<Recibo>().Include("Agencia").Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && CentroIds.Contains(x.AgenciaId) && !x.Anulada && x.Pagada).Join(db.Set<ReciboDetalle>().Include("Producto"), M => M.ReciboId, MD => MD.ReciboId, (M, MD) => new ProductoModel() { ProductoId = MD.ProductoId, Agencia = M.Agencia.Nombre, Nombre = MD.Producto.Nombre, PrecioVenta = MD.Precio, PrecioCosto = MD.PrecioCosto, Fecha = M.Fecha, Cantidad = MD.Cantidad }).ToList();
                    }
                    else
                    {
                     //   ProductoFacturas = db.Set<Factura>().Include("Agencia").Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && !x.Anulada && CentroIds.Contains(x.AgenciaId)).AsEnumerable().Select(x => new ProductoModel() { SolicitudId = x.FacturaId, Agencia = x.Agencia.Nombre, Fecha = x.Fecha }).AsEnumerable().Select(x => x).Join(db.Set<FacturaDetalle>().Include("Producto").Where(x => x.ProductoId.Equals(productoId)), E => E.SolicitudId, FD => FD.FacturaId, (E, FD) => new ProductoModel() { ProductoId = FD.ProductoId, Agencia = E.Agencia, Fecha = E.Fecha, Nombre = FD.Producto.Nombre, PrecioCosto = FD.PrecioCosto, PrecioVenta = FD.Precio, Cantidad = FD.Cantidad }).ToList();
                        ProductoRecibos = db.Set<Recibo>().Include("Agencia").Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && CentroIds.Contains(x.AgenciaId) && !x.Anulada && x.Pagada).Join(db.Set<ReciboDetalle>().Include("Producto").Where(x => x.ProductoId.Equals(productoId)), M => M.ReciboId, MD => MD.ReciboId, (M, MD) => new ProductoModel() { ProductoId = MD.ProductoId, Agencia = M.Agencia.Nombre, Nombre = MD.Producto.Nombre, PrecioVenta = MD.Precio, PrecioCosto = MD.PrecioCosto, Fecha = M.Fecha, Cantidad = MD.Cantidad }).ToList();
                    }

                    //if (ProductoFacturas != null && ProductoFacturas.Count() > 0)
                    //{
                    //    ProductoVentas.AddRange(ProductoFacturas);
                    //}

                    if (ProductoRecibos != null && ProductoRecibos.Count() > 0)
                    {
                        ProductoVentas.AddRange(ProductoRecibos);
                    }

                    if (ProductoVentas != null && ProductoVentas.Count() > 0)
                    {
                        var GanaciaTotales = ProductoVentas.AsEnumerable().GroupBy(r => new { r.ProductoId, r.Agencia, r.Fecha }).Select(g => new { g.Key, Cantidad = g.Sum(x => x.Cantidad), Costo = g.Sum(x => x.Cantidad * x.PrecioCosto), Venta = g.Sum(x => x.Cantidad * x.PrecioVenta) }).ToList();
                        if (GanaciaTotales != null && GanaciaTotales.Count() > 0)
                        {
                            ProductoVentas = GanaciaTotales.Join(db.Set<Producto>(), G => G.Key.ProductoId, P => P.ProductoId, (G, P) => new ProductoModel() { ProductoId = P.ProductoId, Agencia = G.Key.Agencia, Nombre = P.Nombre, Fecha = G.Key.Fecha, Cantidad = G.Cantidad, PrecioCosto = G.Costo, PrecioVenta = G.Venta }).ToList();
                        }
                    }

                    if (ProductoVentas != null && ProductoVentas.Count() > 0)
                    {
                        ProductoVentas = ProductoVentas.OrderBy(x => x.Nombre).ToList();
                    }
                }
                catch (Exception)
                {
                }

                return ProductoVentas;
            }

            public ProductoFotografia Fotografia(int fotografiaId, string productoId)
            {
                ProductoFotografia FotografiaActual = new ProductoFotografia();

                try
                {
                    FotografiaActual = db.Set<ProductoFotografia>().Where(x => x.FotografiaId == fotografiaId && x.ProductoId == productoId).FirstOrDefault();
                }
                catch (Exception)
                {
                }

                return FotografiaActual;
            }

        public List<ProductoExistenciaModel> ObtenerExistenciaPorTienda(long marcaId, long agenciaId, long usuarioId, long categoriaId = -1, bool pedido = false, bool existencia = false)
        {
            List<ProductoExistenciaModel> Existencias = new List<ProductoExistenciaModel>();
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
                             
                if (marcaId == 0)
                {
                    if (categoriaId == -1)
                    {
                        Existencias = db.Set<Producto>().Include("Marca").Select(x => new ProductoExistenciaModel() { ID = x.ProductoId, Codigo = x.Codigo, Marca = x.Marca.Nombre, Descripcion = x.Nombre, Minimo = x.Minimo, Maximo = x.Maximo, Estado = x.Activo ? "Activo" : "Inactivo" }).AsEnumerable().Join(db.Set<ProductoInventario>().Where(x => AgenciaIds.Contains(x.AgenciaId)), E => E.ID, PI => PI.ProductoId, (E, PI) => new ProductoExistenciaModel() { ID = E.ID, Codigo = E.Codigo, Marca = E.Marca, Descripcion = E.Descripcion, AgenciaId = PI.AgenciaId, Cantidad = PI.Cantidad >= E.Maximo ? 0 : E.Maximo - PI.Cantidad, Total = PI.Cantidad, Minimo = E.Minimo, Maximo = E.Maximo, Costo = 0, Precio = 0, Estado = E.Estado }).Join(db.Set<Agencia>(), E => E.AgenciaId, A => A.AgenciaId, (E, A) => new ProductoExistenciaModel() { ID = E.ID, Codigo = E.Codigo, Marca = E.Marca, Descripcion = E.Descripcion, AgenciaId = E.AgenciaId, Agencia = A.Nombre, Cantidad = E.Cantidad, Total = E.Total, Minimo = E.Minimo, Maximo = E.Maximo, Costo = E.Costo, Precio = E.Precio, Estado = E.Estado }).Join(db.Set<ProductoPrecio>().Where(x => x.PrecioId == 5), E => E.ID, PP => PP.ProductoId, (E, PP) => new ProductoExistenciaModel() { ID = E.ID, Codigo = E.Codigo, Marca = E.Marca, Descripcion = E.Descripcion, AgenciaId = E.AgenciaId, Agencia = E.Agencia, Cantidad = E.Cantidad, Total = E.Total, Minimo = E.Minimo, Maximo = E.Maximo, Costo = E.Costo, Precio = PP.Valor, Estado = E.Estado }).Join(db.Set<ProductoPrecioCosto>(), E => E.ID, PP => PP.ProductoId, (E, PP) => new ProductoExistenciaModel() { ID = E.ID, Codigo = E.Codigo, Marca = E.Marca, Descripcion = E.Descripcion, AgenciaId = E.AgenciaId, Agencia = E.Agencia, Cantidad = E.Cantidad, Total = E.Total, Minimo = E.Minimo, Maximo = E.Maximo, Costo = PP.PrecioCosto, Precio = E.Precio, Estado = E.Estado }).ToList();
                    }
                    else
                    {
                        Existencias = db.Set<Producto>().Include("Marca").Where(x => x.CategoriaId == categoriaId).Select(x => new ProductoExistenciaModel() { ID = x.ProductoId, Codigo = x.Codigo, Marca = x.Marca.Nombre, Descripcion = x.Nombre, Minimo = x.Minimo, Maximo = x.Maximo, Estado = x.Activo ? "Activo" : "Inactivo" }).AsEnumerable().Join(db.Set<ProductoInventario>().Where(x => AgenciaIds.Contains(x.AgenciaId)), E => E.ID, PI => PI.ProductoId, (E, PI) => new ProductoExistenciaModel() { ID = E.ID, Codigo = E.Codigo, Marca = E.Marca, Descripcion = E.Descripcion, AgenciaId = PI.AgenciaId, Cantidad = PI.Cantidad >= E.Maximo ? 0 : E.Maximo - PI.Cantidad, Total = PI.Cantidad, Minimo = E.Minimo, Maximo = E.Maximo, Costo = 0, Precio = 0, Estado = E.Estado }).Join(db.Set<Agencia>(), E => E.AgenciaId, A => A.AgenciaId, (E, A) => new ProductoExistenciaModel() { ID = E.ID, Codigo = E.Codigo, Marca = E.Marca, Descripcion = E.Descripcion, AgenciaId = E.AgenciaId, Agencia = A.Nombre, Cantidad = E.Cantidad, Total = E.Total, Minimo = E.Minimo, Maximo = E.Maximo, Costo = E.Costo, Precio = E.Precio, Estado = E.Estado }).Join(db.Set<ProductoPrecio>().Where(x => x.PrecioId == 5), E => E.ID, PP => PP.ProductoId, (E, PP) => new ProductoExistenciaModel() { ID = E.ID, Codigo = E.Codigo, Marca = E.Marca, Descripcion = E.Descripcion, AgenciaId = E.AgenciaId, Agencia = E.Agencia, Cantidad = E.Cantidad, Total = E.Total, Minimo = E.Minimo, Maximo = E.Maximo, Costo = E.Costo, Precio = PP.Valor, Estado = E.Estado }).Join(db.Set<ProductoPrecioCosto>(), E => E.ID, PP => PP.ProductoId, (E, PP) => new ProductoExistenciaModel() { ID = E.ID, Codigo = E.Codigo, Marca = E.Marca, Descripcion = E.Descripcion, AgenciaId = E.AgenciaId, Agencia = E.Agencia, Cantidad = E.Cantidad, Total = E.Total, Minimo = E.Minimo, Maximo = E.Maximo, Costo = PP.PrecioCosto, Precio = E.Precio, Estado = E.Estado }).ToList();
                    }
                }
                else
                {
                    Existencias = db.Set<Producto>().Include("Marca").Where(x => x.MarcaId == marcaId).Select(x => new ProductoExistenciaModel() { ID = x.ProductoId, Codigo = x.Codigo, Marca = x.Marca.Nombre, Descripcion = x.Nombre, Minimo = x.Minimo, Maximo = x.Maximo, Estado = x.Activo ? "Activo" : "Inactivo" }).AsEnumerable().Join(db.Set<ProductoInventario>().Where(x => AgenciaIds.Contains(x.AgenciaId)), E => E.ID, PI => PI.ProductoId, (E, PI) => new ProductoExistenciaModel() { ID = E.ID, Codigo = E.Codigo, Marca = E.Marca, Descripcion = E.Descripcion, AgenciaId = PI.AgenciaId, Cantidad = PI.Cantidad >= E.Maximo ? 0 : E.Maximo - PI.Cantidad, Total = PI.Cantidad, Minimo = E.Minimo, Maximo = E.Maximo, Costo = 0, Precio = 0, Estado = E.Estado }).Join(db.Set<Agencia>(), E => E.AgenciaId, A => A.AgenciaId, (E, A) => new ProductoExistenciaModel() { ID = E.ID, Codigo = E.Codigo, Marca = E.Marca, Descripcion = E.Descripcion, AgenciaId = E.AgenciaId, Agencia = A.Nombre, Cantidad = E.Cantidad, Total = E.Total, Minimo = E.Minimo, Maximo = E.Maximo, Costo = E.Costo, Precio = E.Precio, Estado = E.Estado }).Join(db.Set<ProductoPrecio>().Where(x => x.PrecioId == 5), E => E.ID, PP => PP.ProductoId, (E, PP) => new ProductoExistenciaModel() { ID = E.ID, Codigo = E.Codigo, Marca = E.Marca, Descripcion = E.Descripcion, AgenciaId = E.AgenciaId, Agencia = E.Agencia, Cantidad = E.Cantidad, Total = E.Total, Minimo = E.Minimo, Maximo = E.Maximo, Costo = E.Costo, Precio = PP.Valor, Estado = E.Estado }).Join(db.Set<ProductoPrecioCosto>(), E => E.ID, PP => PP.ProductoId, (E, PP) => new ProductoExistenciaModel() { ID = E.ID, Codigo = E.Codigo, Marca = E.Marca, Descripcion = E.Descripcion, AgenciaId = E.AgenciaId, Agencia = E.Agencia, Cantidad = E.Cantidad, Total = E.Total, Minimo = E.Minimo, Maximo = E.Maximo, Costo = PP.PrecioCosto, Precio = E.Precio, Estado = E.Estado }).ToList();
                }

                if (pedido)
                {
                    if (Existencias != null && Existencias.Count() > 0)
                    {
                        Existencias = Existencias.Where(x => x.Cantidad > 0).ToList();        
                    }
                }

                if (existencia)
                {
                    if (Existencias != null && Existencias.Count() > 0)
                    {
                        Existencias = Existencias.Where(x => x.Total > 0).ToList();
                    }
                }

                if (Existencias != null && Existencias.Count() > 0)
                {
                    Existencias = Existencias.OrderBy(x => x.Descripcion).ToList();                    
                }
            }
            catch (Exception)
            {
            }

            return Existencias;
        }

        public bool EliminarFotografia(string productoId, int fotografiaId)
        {
            bool Eliminar = false;

            try
            {
                ProductoFotografia FotografiaActual = db.Set<ProductoFotografia>().Where(x => x.FotografiaId == fotografiaId && x.ProductoId == productoId).FirstOrDefault();
                if (FotografiaActual != null)
                {
                    db.Set<ProductoFotografia>().Remove(FotografiaActual);
                    db.SaveChanges();

                    Eliminar = true;
                }
            }
            catch (Exception)
            {
            }

            return Eliminar;
        }

        public List<ReporteProductoControladoModel> ReporteProductoControlado(long agenciaId, long categoriaId, DateTime fechaInicial, DateTime fechaFinal)
        {
            List<ReporteProductoControladoModel> Ventas = new List<ReporteProductoControladoModel>();
            
            try
            {
                if (agenciaId == 0 && categoriaId == 0)
                {
                    Ventas = db.Database.SqlQuery<ReporteProductoControladoModel>("dbo.sp_reporte_producto_controlado @AgenciaId, @CategoriaId, @FechaInicial, @FechaFinal", new SqlParameter("@AgenciaId", DBNull.Value), new SqlParameter("@CategoriaId", DBNull.Value), new SqlParameter("@FechaInicial", fechaInicial), new SqlParameter("@FechaFinal", fechaFinal)).ToList();                    
                }
                else if (agenciaId != 0 && categoriaId == 0)
                {
                    Ventas = db.Database.SqlQuery<ReporteProductoControladoModel>("dbo.sp_reporte_producto_controlado @AgenciaId, @CategoriaId, @FechaInicial, @FechaFinal", new SqlParameter("@AgenciaId", agenciaId), new SqlParameter("@CategoriaId", DBNull.Value), new SqlParameter("@FechaInicial", fechaInicial), new SqlParameter("@FechaFinal", fechaFinal)).ToList();
                }
                else if (agenciaId != 0 && categoriaId != 0)
                {
                    Ventas = db.Database.SqlQuery<ReporteProductoControladoModel>("dbo.sp_reporte_producto_controlado @AgenciaId, @CategoriaId, @FechaInicial, @FechaFinal", new SqlParameter("@AgenciaId", agenciaId), new SqlParameter("@CategoriaId", categoriaId), new SqlParameter("@FechaInicial", fechaInicial), new SqlParameter("@FechaFinal", fechaFinal)).ToList();
                } 
            }
            catch (Exception)
            {
            }

            return Ventas;
        }

        public List<ReporteMinimoCategoriaModel> ReporteProductoMinimoCategoria(long agenciaId, long categoriaId)
        {
            List<ReporteMinimoCategoriaModel> Productos = new List<ReporteMinimoCategoriaModel>();

            try
            {
                if (agenciaId == 0 && categoriaId == 0)
                {
                    Productos = db.Database.SqlQuery<ReporteMinimoCategoriaModel>("dbo.sp_reporte_producto_minimo_categoria @AgenciaId, @CategoriaId", new SqlParameter("@AgenciaId", DBNull.Value), new SqlParameter("@CategoriaId", DBNull.Value)).ToList();
                }
                else if (agenciaId != 0 && categoriaId == 0)
                {
                    Productos = db.Database.SqlQuery<ReporteMinimoCategoriaModel>("dbo.sp_reporte_producto_minimo_categoria @AgenciaId, @CategoriaId", new SqlParameter("@AgenciaId", agenciaId), new SqlParameter("@CategoriaId", DBNull.Value)).ToList();
                }
                else if (agenciaId == 0 && categoriaId != 0)
                {
                    Productos = db.Database.SqlQuery<ReporteMinimoCategoriaModel>("dbo.sp_reporte_producto_minimo_categoria @AgenciaId, @CategoriaId", new SqlParameter("@AgenciaId", DBNull.Value), new SqlParameter("@CategoriaId", categoriaId)).ToList();
                }
                else if (agenciaId != 0 && categoriaId != 0)
                {
                    Productos = db.Database.SqlQuery<ReporteMinimoCategoriaModel>("dbo.sp_reporte_producto_minimo_categoria @AgenciaId, @CategoriaId", new SqlParameter("@AgenciaId", agenciaId), new SqlParameter("@CategoriaId", categoriaId)).ToList();
                }
            }
            catch (Exception)
            {
            }

            return Productos;
        }

        public List<ProductoInventarioID> ObtenerProductoID(string productoId, long agenciaId)
        {
            List<ProductoInventarioID> IDs = new List<ProductoInventarioID>();

            try
            {
                IDs = db.Set<ProductoInventarioID>().AsNoTracking().Where(x => x.ProductoId == productoId && x.AgenciaId == agenciaId && !x.Operado).ToList();
            }
            catch (Exception)
            {
            }

            return IDs;
        }

        public List<Producto> ObtenerProductosConIDs()         
        {
            List<Producto> Productos = new List<Producto>();

            try
            {
                Productos = db.Set<Producto>().AsNoTracking().Where(x => x.TieneIdentificador == true && x.Activo).ToList();
            }
            catch (Exception)
            {
            }

            return Productos;
        }

        public List<ReporteInventarioIDsModel> ReporteProductoIDs(long agenciaId, long productoId)
        {
            List<ReporteInventarioIDsModel> Productos = new List<ReporteInventarioIDsModel>();

            try
            {
                if (agenciaId == 0 && productoId == 0)
                {
                    Productos = db.Database.SqlQuery<ReporteInventarioIDsModel>("dbo.sp_reporte_inventario_producto_ids @AgenciaId, @ProductoId", new SqlParameter("@AgenciaId", DBNull.Value), new SqlParameter("@ProductoId", DBNull.Value)).ToList();
                }
                else if (agenciaId != 0 && productoId == 0)
                {
                    Productos = db.Database.SqlQuery<ReporteInventarioIDsModel>("dbo.sp_reporte_inventario_producto_ids @AgenciaId, @ProductoId", new SqlParameter("@AgenciaId", agenciaId), new SqlParameter("@ProductoId", DBNull.Value)).ToList();
                }
                else if (agenciaId == 0 && productoId != 0)
                {
                    Productos = db.Database.SqlQuery<ReporteInventarioIDsModel>("dbo.sp_reporte_inventario_producto_ids @AgenciaId, @ProductoId", new SqlParameter("@AgenciaId", DBNull.Value), new SqlParameter("@ProductoId", productoId)).ToList();
                }
                else if (agenciaId != 0 && productoId != 0)
                {
                    Productos = db.Database.SqlQuery<ReporteInventarioIDsModel>("dbo.sp_reporte_inventario_producto_ids @AgenciaId, @ProductoId", new SqlParameter("@AgenciaId", agenciaId), new SqlParameter("@ProductoId", productoId)).ToList();
                }
            }
            catch (Exception)
            {
            }

            return Productos;
        }

        public List<ReporteProductoReservaModel> ReporteProductoReserva(long agenciaId, long categoriaId)
        {
            List<ReporteProductoReservaModel> Ventas = new List<ReporteProductoReservaModel>();

            try
            {
                if (agenciaId != 0 && categoriaId == 0)
                {
                    Ventas = db.Database.SqlQuery<ReporteProductoReservaModel>("dbo.sp_reporte_producto_reserva @AgenciaId, @CategoriaId", new SqlParameter("@AgenciaId", agenciaId), new SqlParameter("@CategoriaId", DBNull.Value)).ToList();
                }               
                else if (agenciaId != 0 && categoriaId != 0)
                {
                    Ventas = db.Database.SqlQuery<ReporteProductoReservaModel>("dbo.sp_reporte_producto_reserva @AgenciaId, @CategoriaId", new SqlParameter("@AgenciaId", agenciaId), new SqlParameter("@CategoriaId", categoriaId)).ToList();
                }
                else if (agenciaId == 0 && categoriaId == 0)
                {
                    Ventas = db.Database.SqlQuery<ReporteProductoReservaModel>("dbo.sp_reporte_producto_reserva @AgenciaId, @CategoriaId", new SqlParameter("@AgenciaId", DBNull.Value), new SqlParameter("@CategoriaId", DBNull.Value)).ToList();
                }
            }
            catch (Exception)
            {
            }

            return Ventas;
        }

        public List<ReporteProductoReservado> ReporteProductoReservado(long agenciaId, long categoriaId, string productoId, bool estadoId, DateTime fechaInicial, DateTime fechaFinal)
        {
            List<ReporteProductoReservado> Ventas = new List<ReporteProductoReservado>();

            try
            {
                if (agenciaId == 0 && categoriaId == 0 && productoId == "0")
                {
                    Ventas = db.Database.SqlQuery<ReporteProductoReservado>("dbo.sp_reporte_producto_reservado @AgenciaId, @CategoriaId, @ProductoId, @EstadoId, @FechaInicial, @FechaFinal", new SqlParameter("@AgenciaId", DBNull.Value), new SqlParameter("@CategoriaId", DBNull.Value), new SqlParameter("@ProductoId", DBNull.Value), new SqlParameter("@EstadoId", estadoId), new SqlParameter("@FechaInicial", fechaInicial), new SqlParameter("@FechaFinal", fechaFinal)).ToList();
                }
                else if (agenciaId == 0 && categoriaId != 0 && productoId == "0")
                {
                    Ventas = db.Database.SqlQuery<ReporteProductoReservado>("dbo.sp_reporte_producto_reservado @AgenciaId, @CategoriaId, @ProductoId, @EstadoId, @FechaInicial, @FechaFinal", new SqlParameter("@AgenciaId", DBNull.Value), new SqlParameter("@CategoriaId", categoriaId), new SqlParameter("@ProductoId", DBNull.Value), new SqlParameter("@EstadoId", estadoId), new SqlParameter("@FechaInicial", fechaInicial), new SqlParameter("@FechaFinal", fechaFinal)).ToList();
                }
                else if (agenciaId == 0 && categoriaId != 0 && productoId != "0")
                {
                    Ventas = db.Database.SqlQuery<ReporteProductoReservado>("dbo.sp_reporte_producto_reservado @AgenciaId, @CategoriaId, @ProductoId, @EstadoId, @FechaInicial, @FechaFinal", new SqlParameter("@AgenciaId", DBNull.Value), new SqlParameter("@CategoriaId", categoriaId), new SqlParameter("@ProductoId", productoId), new SqlParameter("@EstadoId", estadoId), new SqlParameter("@FechaInicial", fechaInicial), new SqlParameter("@FechaFinal", fechaFinal)).ToList();
                }
                else if (agenciaId != 0 && categoriaId == 0 && productoId == "0")
                {
                    Ventas = db.Database.SqlQuery<ReporteProductoReservado>("dbo.sp_reporte_producto_reservado @AgenciaId, @CategoriaId, @ProductoId, @EstadoId, @FechaInicial, @FechaFinal", new SqlParameter("@AgenciaId", agenciaId), new SqlParameter("@CategoriaId", DBNull.Value), new SqlParameter("@ProductoId", DBNull.Value), new SqlParameter("@EstadoId", estadoId), new SqlParameter("@FechaInicial", fechaInicial), new SqlParameter("@FechaFinal", fechaFinal)).ToList();
                }                
                else if (agenciaId != 0 && categoriaId != 0 && productoId == "0")
                {
                    Ventas = db.Database.SqlQuery<ReporteProductoReservado>("dbo.sp_reporte_producto_reservado @AgenciaId, @CategoriaId, @ProductoId, @EstadoId, @FechaInicial, @FechaFinal", new SqlParameter("@AgenciaId", agenciaId), new SqlParameter("@CategoriaId", categoriaId), new SqlParameter("@ProductoId", DBNull.Value), new SqlParameter("@EstadoId", estadoId), new SqlParameter("@FechaInicial", fechaInicial), new SqlParameter("@FechaFinal", fechaFinal)).ToList();
                }
                else if (agenciaId != 0 && categoriaId != 0 && productoId != "0")
                {
                    Ventas = db.Database.SqlQuery<ReporteProductoReservado>("dbo.sp_reporte_producto_reservado @AgenciaId, @CategoriaId, @ProductoId, @EstadoId, @FechaInicial, @FechaFinal", new SqlParameter("@AgenciaId", agenciaId), new SqlParameter("@CategoriaId", categoriaId), new SqlParameter("@ProductoId", productoId), new SqlParameter("@EstadoId", estadoId), new SqlParameter("@FechaInicial", fechaInicial), new SqlParameter("@FechaFinal", fechaFinal)).ToList();
                }
            }
            catch (Exception)
            {
            }

            return Ventas;
        }

        public List<ProductoPrecioCostoHistorial> ObtenerHistorialPrecioCostoxProducto(string id) 
        {
            List<ProductoPrecioCostoHistorial> Historial = new List<ProductoPrecioCostoHistorial>();

            try
            {
                Historial = db.Set<ProductoPrecioCostoHistorial>().Include("Proveedor").AsNoTracking().Where(x => x.ProductoId.Equals(id)).OrderByDescending(x => x.HistorialId).Take(5).ToList();
            }
            catch (Exception)
            {
            }

            return Historial;
        }

        public List<ProductoNivelPrecio> ObtenerEscalaPreciosxProducto(string id)
        {
            List<ProductoNivelPrecio> Escalas = new List<ProductoNivelPrecio>();

            try
            {
                Escalas = db.Set<ProductoNivelPrecio>().AsNoTracking().Where(x => x.ProductoId.Equals(id)).ToList();
            }
            catch (Exception)
            {
            }

            return Escalas;
        }

        public List<KardexMovimientoModel> KardexMovimientoModel(long agenciaId, string productoId, DateTime fechaInicial, DateTime fechaFinal)
        {
            List<KardexMovimientoModel> Ventas = new List<KardexMovimientoModel>();

            try
            {
                if (agenciaId == 0 && productoId == "0")
                {
                    Ventas = db.Database.SqlQuery<KardexMovimientoModel>("dbo.sp_reporte_kardex_movimiento @AgenciaId, @ProductoId, @FechaInicial, @FechaFinal", new SqlParameter("@AgenciaId", DBNull.Value), new SqlParameter("@ProductoId", DBNull.Value), new SqlParameter("@FechaInicial", fechaInicial), new SqlParameter("@FechaFinal", fechaFinal)).ToList();
                }               
                else if (agenciaId == 0 && productoId != "0")
                {
                    Ventas = db.Database.SqlQuery<KardexMovimientoModel>("dbo.sp_reporte_kardex_movimiento @AgenciaId, @ProductoId, @FechaInicial, @FechaFinal", new SqlParameter("@AgenciaId", DBNull.Value), new SqlParameter("@ProductoId", productoId), new SqlParameter("@FechaInicial", fechaInicial), new SqlParameter("@FechaFinal", fechaFinal)).ToList();
                }
                else if (agenciaId != 0 && productoId == "0")
                {
                    Ventas = db.Database.SqlQuery<KardexMovimientoModel>("dbo.sp_reporte_kardex_movimiento @AgenciaId, @ProductoId, @FechaInicial, @FechaFinal", new SqlParameter("@AgenciaId", agenciaId), new SqlParameter("@ProductoId", DBNull.Value), new SqlParameter("@FechaInicial", fechaInicial), new SqlParameter("@FechaFinal", fechaFinal)).ToList();
                }               
                else if (agenciaId != 0 && productoId != "0")
                {
                    Ventas = db.Database.SqlQuery<KardexMovimientoModel>("dbo.sp_reporte_kardex_movimiento @AgenciaId, @ProductoId, @FechaInicial, @FechaFinal", new SqlParameter("@AgenciaId", agenciaId), new SqlParameter("@ProductoId", productoId), new SqlParameter("@FechaInicial", fechaInicial), new SqlParameter("@FechaFinal", fechaFinal)).ToList();
                }

                if (Ventas != null && Ventas.Count() > 0)
                {
                    Ventas = Ventas.OrderByDescending(x => x.FechaHora).ToList();                    
                }
            }
            catch (Exception)
            {
            }

            return Ventas;
        }

        public List<ReporteProductoReservaPendienteCompra> ReporteProductoReservaPendienteCompra()
        {
            List<ReporteProductoReservaPendienteCompra> Ventas = new List<ReporteProductoReservaPendienteCompra>();

            try
            {
                Ventas = db.Database.SqlQuery<ReporteProductoReservaPendienteCompra>("dbo.sp_reporte_producto_reserva_pendiente_compra").ToList();
            }
            catch (Exception)
            {
            }

            return Ventas;
        }

        public List<ProductoStock> ConsultaStockMaximo(long agenciaId)
        {
            List<ProductoStock> Productos = new List<ProductoStock>();

            try
            {
                Productos = db.Database.SqlQuery<ProductoStock>("dbo.sp_consulta_producto_maximo_stock @AgenciaId", new SqlParameter("@AgenciaId", agenciaId)).ToList();
            }
            catch (Exception)
            {
            }

            return Productos;
        }

        public List<ProductoStock> ConsultaStockMinimo(long agenciaId)
        {
            List<ProductoStock> Productos = new List<ProductoStock>();

            try
            {
                Productos = db.Database.SqlQuery<ProductoStock>("dbo.sp_consulta_producto_minimo_stock @AgenciaId", new SqlParameter("@AgenciaId", agenciaId)).ToList();
            }
            catch (Exception)
            {
            }

            return Productos;
        }

        public decimal CantidadReservaPendienteCompra()
        {
            List<ReporteProductoReservaPendienteCompra> Ventas = new List<ReporteProductoReservaPendienteCompra>();
            decimal CantidadTotal = 0;

            try
            {
                Ventas = ReporteProductoReservaPendienteCompra();
                if (Ventas != null && Ventas.Count() > 0)
                {
                    CantidadTotal = Ventas.Sum(x => x.Cantidad);
                }
            }
            catch (Exception)
            {
            }

            return CantidadTotal;
        }

        public int CantidadConsultaStockMaximo(long agenciaId)
        {
            List<ProductoStock> Productos = new List<ProductoStock>();
            int Cantidad = 0;

            try
            {
                Productos = ConsultaStockMaximo(agenciaId);
                if (Productos != null && Productos.Count() > 0)
                {
                    Cantidad = Productos.Count();
                }
            }
            catch (Exception)
            {
            }

            return Cantidad;
        }

        public int CantidadConsultaStockMinimo(long agenciaId)
        {
            List<ProductoStock> Productos = new List<ProductoStock>();
            int Cantidad = 0;

            try
            {
                Productos = ConsultaStockMinimo(agenciaId);
                if (Productos != null && Productos.Count() > 0)
                {
                    Cantidad = Productos.Count();
                }
            }
            catch (Exception)
            {
            }

            return Cantidad;
        }

        public List<ProductoLote> ObtenerLotesxProductoId(string productoId, long agenciaId)
        {
            List<ProductoLote> Lotes = new List<ProductoLote>();

            try
            {
                Lotes = db.Set<ProductoLote>().AsNoTracking().Where(x => x.ProductoId == productoId && x.AgenciaId == agenciaId && x.Cantidad > 0).ToList();
                if (Lotes != null && Lotes.Count() > 0)
                {
                    Lotes.ForEach(x => 
                    {
                        x.Nombre = string.Format("#Lote: {0} - Fecha Vencimiento: {1} - Cantidad: {2}", x.Lote, x.FechaVencimiento.ToString("yyyy-MM-dd"), x.Cantidad);
                    });                    
                }
            }
            catch (Exception)
            {
            }

            return Lotes;
        }

        public ProductoLote ObtenerLotexId(string productoId, long agenciaId, string loteId) 
        {
            ProductoLote Lote = new ProductoLote();

            try
            {
                Lote = db.Set<ProductoLote>().AsNoTracking().Where(x => x.ProductoId == productoId && x.AgenciaId == agenciaId && x.Lote == loteId).FirstOrDefault();
                if (Lote != null)
                {
                    Lote.Fecha = Lote.FechaVencimiento.ToString("yyyy-MM-dd");                    
                }
            }
            catch (Exception)
            {
            }

            return Lote;
        }

        public string ObtenerLotes(string productoId, long agenciaId) 
        {
            string Mensaje = "<tr><td colspan='3'>No contiene lotes disponibles</td></tr>";
            List<ProductoLote> Lotes = new List<ProductoLote>();

            try
            {
                Lotes = db.Set<ProductoLote>().AsNoTracking().Where(x => x.ProductoId == productoId && x.AgenciaId == agenciaId && x.Cantidad > 0).ToList();
                if (Lotes != null && Lotes.Count() > 0)
                {
                    Mensaje = string.Empty;

                    Lotes.ForEach(x => 
                    {
                        Mensaje += string.Format("<tr><td>{0}</td><td>{1}</td><td>{2}</td></tr>", x.Lote, x.FechaVencimiento.ToString("dd/MM/yyyy"), x.Cantidad);
                    });                    
                }
            }
            catch (Exception)
            {
            }

            return Mensaje;
        }

        public List<ReporteProductoLote> ReporteProductoLote(long agenciaId, long productoId)
        {
            List<ReporteProductoLote> Productos = new List<ReporteProductoLote>();

            try
            {
                if (agenciaId == 0 && productoId == 0)
                {
                    Productos = db.Database.SqlQuery<ReporteProductoLote>("dbo.sp_reporte_producto_controlado_lote_x_producto @AgenciaId, @ProductoId", new SqlParameter("@AgenciaId", DBNull.Value), new SqlParameter("@ProductoId", DBNull.Value)).ToList();
                }
                else if (agenciaId != 0 && productoId == 0)
                {
                    Productos = db.Database.SqlQuery<ReporteProductoLote>("dbo.sp_reporte_producto_controlado_lote_x_producto @AgenciaId, @ProductoId", new SqlParameter("@AgenciaId", agenciaId), new SqlParameter("@ProductoId", DBNull.Value)).ToList();
                }
                else if (agenciaId == 0 && productoId != 0)
                {
                    Productos = db.Database.SqlQuery<ReporteProductoLote>("dbo.sp_reporte_producto_controlado_lote_x_producto @AgenciaId, @ProductoId", new SqlParameter("@AgenciaId", DBNull.Value), new SqlParameter("@ProductoId", productoId)).ToList();
                }
                else if (agenciaId != 0 && productoId != 0)
                {
                    Productos = db.Database.SqlQuery<ReporteProductoLote>("dbo.sp_reporte_producto_controlado_lote_x_producto @AgenciaId, @ProductoId", new SqlParameter("@AgenciaId", agenciaId), new SqlParameter("@ProductoId", productoId)).ToList();
                }
            }
            catch (Exception)
            {
            }

            return Productos;
        }

        public Producto ObtenerProductoxCodigo(string codigo) 
        {
            Producto ProductoActual = new Producto();
            long ProductoId = 0;
            
            try
            {
                long.TryParse(codigo, out ProductoId);

                if (ProductoId > 0)
                {     
                    ProductoActual = db.Set<Producto>().AsNoTracking().Where(x => x.ProductoId == codigo).FirstOrDefault();
                }
                else
                {
                    ProductoActual = db.Set<Producto>().AsNoTracking().Where(x => x.Codigo == codigo).FirstOrDefault();
                }

                if (ProductoActual != null)
                {
                    ProductoPrecio PrecioActual = db.Set<ProductoPrecio>().AsNoTracking().Where(x => x.ProductoId == ProductoActual.ProductoId && x.PrecioId == 5).FirstOrDefault();
                    if (PrecioActual != null)
                    {
                        ProductoActual.PrecioActual = PrecioActual.Valor;
                    }
                }
            }
            catch (Exception)
            {}

            return ProductoActual;
        }

        #endregion
    }
}
