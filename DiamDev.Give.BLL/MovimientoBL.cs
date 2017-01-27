using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                {
                }

                return Id;
            }

            private bool Agregar(Movimiento entidad)
            {
                bool MovimientoAgregar = false;

                try
                {

                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngMovimientoId = new Herramienta().Formato_Correlativo(Id);

                        if (lngMovimientoId > 0)
                        {
                            entidad.MovimientoId = lngMovimientoId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            if (entidad.Detalles != null && entidad.Detalles.Count() > 0)
                            {
                                int DetalleId = 1;
                                foreach (var Producto in entidad.Detalles)
                                {
                                    Producto.DetalleId = DetalleId;
                                    Producto.MovimientoId = entidad.MovimientoId;

                                    //Se obtiene el producto para convercion
                                    Producto ProductoPadreActual = new Producto();
                                    Producto ProductoHijoActual = new Producto();

                                    decimal Cantidad = Producto.Cantidad;
                                    decimal PrecioCosto = Producto.Precio;
                                    decimal CantidadOriginal = 0;

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
                                            PrecioCosto = decimal.Round(PrecioCosto / ProductoHijoActual.Cantidad, 2);
                                            CantidadOriginal = ProductoHijoActual.Cantidad;
                                        }
                                    }

                                    if (entidad.MovimientoTipoId == 1)
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
                                    else if (entidad.MovimientoTipoId == 2 || entidad.MovimientoTipoId == 3)
                                    {
                                        ProductoInventario InventarioActual = db.Set<ProductoInventario>().Where(x => x.ProductoId == Producto.ProductoId && x.AgenciaId == entidad.AgenciaId).FirstOrDefault();
                                        if (InventarioActual != null)
                                        {
                                            InventarioActual.Cantidad -= Producto.Cantidad;
                                        }

                                        ProductoPrecioCosto CostoActual = db.Set<ProductoPrecioCosto>().Where(x => x.ProductoId == Producto.ProductoId).FirstOrDefault();
                                        if (CostoActual != null)
                                        {
                                            Producto.PrecioCosto = decimal.Round(CostoActual.PrecioCosto * CantidadOriginal, 2);
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
                            MovimientoAgregar = true;
                        }
                    }

                }
                catch (Exception)
                {
                }

                return MovimientoAgregar;
            }

        #endregion

        #region Metodos Publicos

            public string Guardar(Movimiento entidad)
            {
                string Mensaje = "OK";
                bool OperacionExitosa = false;

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

            public Movimiento ObtenerPorId(long id, bool proveedor = true)
            {
                Movimiento MovimientoActual = new Movimiento();

                try
                {
                    if (proveedor)
                    {
                        MovimientoActual = db.Set<Movimiento>().Include("Agencia").Include("Proveedor").Include("Detalles").Include("Detalles.Producto").Include("Detalles.Unidad").Where(x => x.MovimientoId == id).FirstOrDefault();
                    }
                    else
                    {
                        MovimientoActual = db.Set<Movimiento>().Include("Agencia").Include("Cliente").Include("Detalles").Include("Detalles.Producto").Include("Detalles.Unidad").Include("Pagos").Include("Pagos.FormaPago").Where(x => x.MovimientoId == id).FirstOrDefault();
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
                            Movimientos = db.Set<Movimiento>().Include("Proveedor").Include("Detalles").Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && x.MovimientoTipoId == tipoId && AgenciasIds.Contains(x.AgenciaId)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.MovimientoId).ToList();
                        }
                        else
                        {
                            Movimientos = db.Set<Movimiento>().Include("Cliente").Include("Detalles").Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && x.MovimientoTipoId == tipoId && AgenciasIds.Contains(x.AgenciaId)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.MovimientoId).ToList();
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
                                Movimientos = db.Set<Movimiento>().Include("Agencia").Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && x.MovimientoTipoId == tipoId && AgenciaIds.Contains(x.AgenciaId)).Join(db.Set<MovimientoDetalle>(), M => M.MovimientoId, MD => MD.MovimientoId, (M, MD) => new MovimientoModel() { MovimientoId = M.MovimientoId, Agencia = M.Agencia.Nombre, Descripcion = M.Descripcion, Id = M.ProveedorId.Value, Total = MD.Cantidad * MD.Precio, Descuento = M.Descuento, UsuarioId = M.UsrCreo }).AsEnumerable().Select(x => x).Join(db.Set<Proveedor>(), M => M.Id, P => P.ProveedorId, (M, P) => new MovimientoModel() { MovimientoId = M.MovimientoId, Agencia = M.Agencia, Nombre = P.Nombre, Descripcion = M.Descripcion, Total = M.Total, UsuarioId = M.UsuarioId }).Join(db.Set<Usuario>(), M => M.UsuarioId, U => U.UsuarioId, (M, U) => new MovimientoModel() { MovimientoId = M.MovimientoId, Agencia = M.Agencia, Nombre = M.Nombre, Descripcion = M.Descripcion, Total = M.Total, Usuario = U.Nombre }).ToList();
                            }
                            else
                            {
                                Movimientos = db.Set<Movimiento>().Include("Agencia").Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && x.MovimientoTipoId == tipoId && AgenciaIds.Contains(x.AgenciaId)).Join(db.Set<MovimientoDetalle>().Where(x => x.ProductoId == productoId), M => M.MovimientoId, MD => MD.MovimientoId, (M, MD) => new MovimientoModel() { MovimientoId = M.MovimientoId, Agencia = M.Agencia.Nombre, Descripcion = M.Descripcion, Id = M.ProveedorId.Value, Total = MD.Cantidad * MD.Precio, Descuento = M.Descuento, UsuarioId = M.UsrCreo }).AsEnumerable().Select(x => x).Join(db.Set<Proveedor>(), M => M.Id, P => P.ProveedorId, (M, P) => new MovimientoModel() { MovimientoId = M.MovimientoId, Agencia = M.Agencia, Nombre = P.Nombre, Descripcion = M.Descripcion, Total = M.Total, UsuarioId = M.UsuarioId }).Join(db.Set<Usuario>(), M => M.UsuarioId, U => U.UsuarioId, (M, U) => new MovimientoModel() { MovimientoId = M.MovimientoId, Agencia = M.Agencia, Nombre = M.Nombre, Descripcion = M.Descripcion, Total = M.Total, Usuario = U.Nombre }).ToList();
                            }
                        }
                        else
                        {
                            Movimientos = db.Set<Movimiento>().Include("Agencia").Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && x.MovimientoTipoId == tipoId && AgenciaIds.Contains(x.AgenciaId) && x.ProveedorId == proveedorId).Join(db.Set<MovimientoDetalle>(), M => M.MovimientoId, MD => MD.MovimientoId, (M, MD) => new MovimientoModel() { MovimientoId = M.MovimientoId, Agencia = M.Agencia.Nombre, Descripcion = M.Descripcion, Id = M.ProveedorId.Value, Total = MD.Cantidad * MD.Precio, Descuento = M.Descuento, UsuarioId = M.UsrCreo }).AsEnumerable().Select(x => x).Join(db.Set<Proveedor>(), M => M.Id, P => P.ProveedorId, (M, P) => new MovimientoModel() { MovimientoId = M.MovimientoId, Agencia = M.Agencia, Nombre = P.Nombre, Descripcion = M.Descripcion, Total = M.Total, UsuarioId = M.UsuarioId }).Join(db.Set<Usuario>(), M => M.UsuarioId, U => U.UsuarioId, (M, U) => new MovimientoModel() { MovimientoId = M.MovimientoId, Agencia = M.Agencia, Nombre = M.Nombre, Descripcion = M.Descripcion, Total = M.Total, Usuario = U.Nombre }).ToList();
                        }
                    }
                    else if (tipoId == 2)
                    {
                        Movimientos = db.Set<Movimiento>().Include("Agencia").Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && x.MovimientoTipoId == tipoId && AgenciaIds.Contains(x.AgenciaId)).Join(db.Set<MovimientoDetalle>(), M => M.MovimientoId, MD => MD.MovimientoId, (M, MD) => new MovimientoModel() { MovimientoId = M.MovimientoId, Agencia = M.Agencia.Nombre, Descripcion = M.Descripcion, Id = M.ClienteId.Value, Total = MD.Cantidad * MD.Precio, Descuento = M.Descuento, UsuarioId = M.UsrCreo }).AsEnumerable().Select(x => x).Join(db.Set<Cliente>(), M => M.Id, C => C.ClienteId, (M, C) => new MovimientoModel() { MovimientoId = M.MovimientoId, Agencia = M.Agencia, Nombre = C.Nombre, Descripcion = M.Descripcion, Total = M.Total, Descuento = M.Descuento, UsuarioId = M.UsuarioId }).AsEnumerable().Select(x => new MovimientoModel() { MovimientoId = x.MovimientoId, Agencia = x.Agencia, Nombre = x.Nombre, Descripcion = x.Descripcion, Total = x.Descuento == 0 ? x.Total : x.Total - ((Convert.ToDecimal(x.Descuento) / Convert.ToDecimal(100)) * x.Total), UsuarioId = x.UsuarioId }).Join(db.Set<Usuario>(), M => M.UsuarioId, U => U.UsuarioId, (M, U) => new MovimientoModel() { MovimientoId = M.MovimientoId, Agencia = M.Agencia, Nombre = M.Nombre, Descripcion = M.Descripcion, Total = M.Total, Usuario = U.Nombre }).ToList();
                    }

                    if (Movimientos != null && Movimientos.Count() > 0)
                    {
                        var MovimientosIds = Movimientos.GroupBy(m => new { m.MovimientoId, m.Nombre, Centro = m.Agencia, m.Descripcion, m.Descuento, m.Usuario }).Select(g => new { g.Key, Total = g.Sum(x => x.Total) }).ToList();
                        if (MovimientosIds != null && MovimientosIds.Count() > 0)
                        {
                            Movimientos = new List<MovimientoModel>();
                            Movimientos = MovimientosIds.Select(x => new MovimientoModel() { MovimientoId = x.Key.MovimientoId, Agencia = x.Key.Centro, Nombre = x.Key.Nombre, Descuento = x.Key.Descuento, Descripcion = x.Key.Descripcion, Total = x.Total, Usuario = x.Key.Usuario }).ToList();
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

        #endregion

    }
}
