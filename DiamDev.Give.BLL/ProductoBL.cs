using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            private bool Agregar(Producto entidad)
            {
                bool ProductoAgregar = false;

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

                            db.Set<Producto>().Add(entidad);

                            db.SaveChanges();
                            ProductoAgregar = true;
                        }
                    }

                }
                catch (Exception)
                {
                }

                return ProductoAgregar;
            }

            private bool Actualizar(Producto entidad)
            {
                bool ProductoActualizar = false;

                try
                {

                    Producto ProductoActual = ObtenerPorId(entidad.ProductoId);

                    if (!string.IsNullOrWhiteSpace(ProductoActual.ProductoId))
                    {
                        ProductoActual.Codigo = entidad.Codigo;
                        ProductoActual.Nombre = entidad.Nombre;
                        ProductoActual.Descripcion = entidad.Descripcion;
                        ProductoActual.Minimo = entidad.Minimo;
                        ProductoActual.Maximo = entidad.Maximo;
                        ProductoActual.Cantidad = entidad.Cantidad;
                        ProductoActual.Activo = entidad.Activo;

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

                        db.SaveChanges();
                        ProductoActualizar = true;
                    }

                }
                catch (Exception)
                {
                }

                return ProductoActualizar;
            }

        #endregion

        #region Metodos Publicos

            public string Guardar(Producto entidad)
            {
                string Mensaje = "OK";
                bool OperacionExitosa = false;

                if (!string.IsNullOrWhiteSpace(entidad.ProductoId))
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

            public Producto ObtenerPorId(string id, bool todos = true, bool existencia = false, bool imagen = false)
            {
                Producto ProductoActual = new Producto();

                try
                {
                    if (todos)
                    {
                        if (imagen)
                        {
                            ProductoActual = db.Set<Producto>().Include("Categoria").Include("Marca").Include("Unidad").Include("Precios").Include("Imagenes").Where(x => x.ProductoId == id).FirstOrDefault();
                        }
                        else 
                        {
                            ProductoActual = db.Set<Producto>().Include("Categoria").Include("Marca").Include("Unidad").Include("Precios").Where(x => x.ProductoId == id).FirstOrDefault();
                        }
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
                    }
                }
                catch (Exception)
                {
                }

                return ProductoActual;
            }

            public Producto ObtenerExistenciaPorAgenciaYProducto(long agenciaId, string productoId, long unidadId, bool precioVigente = false, bool empleado = false)
            {
                Producto ProductoActual = new Producto();

                try
                {
                    ProductoActual = db.Set<Producto>().Where(x => x.ProductoId == productoId).FirstOrDefault();
                    if (ProductoActual != null)
                    {
                        bool UnidadPadre = false;
                        decimal Existencia = db.Set<ProductoInventario>().Where(x => x.ProductoId == productoId && x.AgenciaId == agenciaId).Sum(x => x.Cantidad); ;

                        if (ProductoActual.UnidadId == unidadId)
                        {
                            UnidadPadre = true;
                        }

                        if (!UnidadPadre)
                        {
                            Producto ProductoHijoActual = db.Set<Producto>().Where(x => x.ProductoPadreId == productoId && x.UnidadId == unidadId).FirstOrDefault();
                            if (ProductoHijoActual != null)
                            {
                                if (Existencia > 0)
                                {
                                    Existencia = decimal.Round(Existencia / ProductoHijoActual.Cantidad, 2);
                                }
                            }
                        }

                        decimal Precio = 0;

                        if (precioVigente)
                        {
                            if (empleado)
                            {
                                ProductoPrecioCosto PrecioActual = db.Set<ProductoPrecioCosto>().Where(x => x.ProductoId == productoId).FirstOrDefault();
                                if (PrecioActual != null)
                                {
                                    decimal IncrementoCompraEmpleado = 1;
                                    Configuracion ConfiguracionActual = db.Set<Configuracion>().Where(x => x.Identificador.Equals("CompraColaborador")).FirstOrDefault();
                                    if (ConfiguracionActual != null)
                                    {
                                        IncrementoCompraEmpleado = decimal.Parse(ConfiguracionActual.Valor);                                       
                                    }

                                    Precio = PrecioActual.PrecioCosto + IncrementoCompraEmpleado;
                                }
                            }
                            else
                            {
                                ProductoPrecio PrecioActual = db.Set<ProductoPrecio>().Where(x => x.ProductoId == productoId && x.PrecioId == 5).FirstOrDefault();
                                if (PrecioActual != null)
                                {
                                    Precio = PrecioActual.Valor;
                                }
                            }

                            ProductoActual.Precios = new List<ProductoPrecio>();
                        }

                        ProductoActual.PrecioActual = Precio;
                        ProductoActual.Existencia = Existencia;
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
                    ProductoActual = db.Set<Producto>().Where(x => x.ProductoId.Equals(barra) || x.Codigo.Equals(barra)).AsEnumerable().Select(x => new Producto() { ProductoId = x.ProductoId, Nombre = string.Format("{0} - {1}",x.Codigo,x.Nombre) }).FirstOrDefault();
                }
                catch (Exception)
                {
                }

                return ProductoActual;
            }

            public List<Producto> ObtenerProductoPorCategoriaIdYMarcaId(long categoriaId, long marcaId)
            {
                List<Producto> Productos = new List<Producto>();

                try
                {
                    Productos = db.Set<Producto>().Include("Unidad").Where(x => x.CategoriaId == categoriaId && x.MarcaId == marcaId && x.ProductoPadreId == null).AsEnumerable().Select(x => new Producto() { ProductoId = x.ProductoId, Nombre = string.Format("{0} - {1} - {2}", x.Codigo, x.Nombre, x.Unidad.Nombre) }).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ProductoId).ToList();
                }
                catch (Exception)
                {
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
                {
                }

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

            public List<Producto> Buscar(string search)
            {
                List<Producto> Productos = new List<Producto>();

                try
                {
                    Productos = db.Set<Producto>().Include("Categoria").Include("Marca").Include("Unidad").Where(x => x.Nombre.Contains(search)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ProductoId).ToList();
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
                List<ProductoModel> ProductoEgresos = new List<ProductoModel>();

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

                    ProductoFacturas = db.Set<Factura>().Include("Agencia").Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && x.Anulada == false && CentroIds.Contains(x.AgenciaId)).AsEnumerable().Select( x => new ProductoModel() { SolicitudId = x.FacturaId, Agencia = x.Agencia.Nombre, Fecha = x.Fecha }).AsEnumerable().Select(x => x).Join(db.Set<FacturaDetalle>().Include("Producto"), E => E.SolicitudId, FD => FD.FacturaId, (E, FD) => new ProductoModel() { ProductoId = FD.ProductoId, Agencia = E.Agencia, Fecha = E.Fecha, Nombre = FD.Producto.Nombre, PrecioCosto = FD.PrecioCosto, PrecioVenta = FD.Precio, Cantidad = FD.Cantidad }).ToList();
                    ProductoEgresos = db.Set<Movimiento>().Include("Agencia").Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && CentroIds.Contains(x.AgenciaId) && x.MovimientoTipoId == 2).Join(db.Set<MovimientoDetalle>().Include("Producto"), M => M.MovimientoId, MD => MD.MovimientoId, (M, MD) => new ProductoModel() { ProductoId = MD.ProductoId, Agencia = M.Agencia.Nombre, Nombre = MD.Producto.Nombre, PrecioVenta = MD.Precio, PrecioCosto = MD.PrecioCosto, Fecha = M.Fecha, Cantidad = MD.Cantidad }).ToList();

                    if (ProductoFacturas != null && ProductoFacturas.Count() > 0)
                    {
                        ProductoVentas.AddRange(ProductoFacturas);
                    }

                    if (ProductoEgresos != null && ProductoEgresos.Count() > 0)
                    {
                        ProductoVentas.AddRange(ProductoEgresos);
                    }

                    if (ProductoVentas != null && ProductoVentas.Count() > 0)
                    {
                        var GanaciaTotales = ProductoVentas.AsEnumerable().GroupBy(r => new { r.ProductoId, r.Agencia, r.Fecha, r.PrecioCosto, r.PrecioVenta }).Select(g => new { g.Key, Cantidad = g.Sum(X => X.Cantidad) }).ToList();
                        if (GanaciaTotales != null && GanaciaTotales.Count() > 0)
                        {
                            ProductoVentas = GanaciaTotales.Join(db.Set<Producto>(), G => G.Key.ProductoId, P => P.ProductoId, (G, P) => new ProductoModel() { ProductoId = P.ProductoId, Agencia = G.Key.Agencia, Nombre = P.Nombre, Fecha = G.Key.Fecha, Cantidad = G.Cantidad, PrecioCosto = G.Key.PrecioCosto, PrecioVenta = G.Key.PrecioVenta }).ToList();
                        }
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

        #endregion

    }
}
