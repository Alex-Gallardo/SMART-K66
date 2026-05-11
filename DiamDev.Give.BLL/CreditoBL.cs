using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class CreditoBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public CreditoBL()
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

                    Credito CreditoActual = db.Set<Credito>().AsNoTracking().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (CreditoActual != null)
                    {
                        Inicial_Id = CreditoActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;

                }
                catch (Exception)
                {
                }

                return Id;
            }

            private bool Agregar(Credito entidad)
            {
                bool CreditoAgregar = false;

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngCreditoId = new Herramienta().Formato_Correlativo(Id);

                        if (lngCreditoId > 0)
                        {
                            entidad.CreditoId = lngCreditoId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;
                          
                            if (entidad.Detalles != null && entidad.Detalles.Count() > 0)
                            {
                                int i = 1;
                                foreach (var Detalle in entidad.Detalles)
                                {
                                    Detalle.DetalleId = i;
                                    Detalle.CreditoId = entidad.CreditoId;
                                    i++;

                                    decimal KardexExistenciaActual = 0;
                                    decimal KardexExistenciaFinal = 0;

                                    ProductoInventario InventarioActual = db.Set<ProductoInventario>().Where(x => x.ProductoId == Detalle.ProductoId && x.AgenciaId == entidad.AgenciaId).FirstOrDefault();
                                    if (InventarioActual != null)
                                    {
                                        KardexExistenciaActual = InventarioActual.Cantidad;
                                        KardexExistenciaFinal = InventarioActual.Cantidad - Detalle.Cantidad;

                                        InventarioActual.Cantidad -= Detalle.Cantidad;
                                    }

                                    //Se agrega la informacion al Kardex
                                    db.Set<KardexMovimiento>().Add(new KardexMovimiento() { AgenciaId = entidad.AgenciaId, TipoId = 7, Fecha = DateTime.Today, FechaHora = DateTime.Now, ProductoId = Detalle.ProductoId, UnidadId = Detalle.UnidadId, DocumentoId = entidad.CreditoId, Cantidad = Detalle.Cantidad, Precio = Detalle.Precio, ExistenciaActual = KardexExistenciaActual, ExistenciaFinal = KardexExistenciaFinal, ResponsableId = entidad.UsrInicial });
                                }
                            }

                            db.Set<Credito>().Add(entidad);
                            db.SaveChanges();
                            CreditoAgregar = true;
                        }
                    }
                }
                catch (Exception)
                {
                }

                return CreditoAgregar;
            }

            private bool Actualizar(Credito entidad)
            {
                bool CreditoActualizar = false;

                try
                {

                    Credito CreditoActual = ObtenerPorId(entidad.CreditoId);

                    if (CreditoActual.CreditoId > 0)
                    {
                        CreditoActual.Finalizado = entidad.Finalizado;

                        if (entidad.Finalizado)
                        {
                            CreditoActual.UsrFinal = entidad.UsrFinal;
                        }

                        db.SaveChanges();
                        CreditoActualizar = true;
                    }

                }
                catch (Exception)
                {
                }

                return CreditoActualizar;
            }

        #endregion

        #region Metodos Publicos

            public string Guardar(Credito entidad)
            {
                string Mensaje = "OK";
                bool OperacionExitosa = false;

                if (entidad.CreditoId > 0)
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

            public string GenerarPago(Credito entidad, long usuarioId, List<CreditoPago> pagos)
            {
                string Mensaje = "OK";

                try
                {
                    int Id = 1;

                    if (pagos != null && pagos.Count() > 0)
                    {
                        CreditoPago PagoUltimo = db.Set<CreditoPago>().Where(x => x.CreditoId == entidad.CreditoId).OrderByDescending(x => x.DetalleId).FirstOrDefault();
                        if (PagoUltimo != null)
                        {
                            Id = PagoUltimo.DetalleId + 1;
                        }

                        foreach (var pago in pagos)
                        {
                            pago.DetalleId = Id;
                            pago.Fecha = DateTime.Today;
                            Id++;

                            db.Set<CreditoPago>().Add(pago);
                        }                        

                        decimal TotalCredito = db.Set<CreditoDetalle>().Where(x => x.CreditoId == entidad.CreditoId).Sum(x => x.Cantidad * x.Precio);
                        decimal TotalPago = 0;

                        var Pagos = db.Set<CreditoPago>().Where(x => x.CreditoId == entidad.CreditoId).ToList();
                        if (Pagos != null && Pagos.Count() > 0)
                        {
                            TotalPago = Pagos.Sum(x => x.Valor);
                        }

                        TotalPago += pagos.Sum(x => x.Valor);

                        if (TotalPago == TotalCredito)
                        {
                            Credito CreditoActual = db.Set<Credito>().Where(x => x.CreditoId == entidad.CreditoId).FirstOrDefault();

                            if (CreditoActual != null)
                            {
                                CreditoActual.FechaCancelacion = DateTime.Today;
                                CreditoActual.Finalizado = true;
                                CreditoActual.UsrFinal = usuarioId;
                            }
                        }

                        Credito CreditoDocumentoActual = db.Set<Credito>().Where(x => x.CreditoId == entidad.CreditoId).FirstOrDefault();

                        if (CreditoDocumentoActual != null)
                        {                            
                            CreditoDocumentoActual.Serie = entidad.Serie;
                            CreditoDocumentoActual.Factura = entidad.Factura;
                        }

                        db.SaveChanges();
                    }

                }
                catch (Exception)
                {
                    Mensaje = "La información ingresada no es valida";
                }

                return Mensaje;
            }

            public Credito ObtenerPorId(long id, bool todo = false)
            {
                Credito CreditoActual = new Credito();

                try
                {
                    if (todo)
                    {
                        CreditoActual = db.Set<Credito>().Include("Tipo").Include("Agencia").Include("Cliente").Include("Detalles").Include("Detalles.Producto").Include("Pagos").Include("Pagos.UsuarioOperacion").Include("Pagos.FormaPago").Include("Comentarios").Include("Comentarios.UsuarioAnotacion").Where(x => x.CreditoId == id).FirstOrDefault();
                    }
                    else
                    {
                        CreditoActual = db.Set<Credito>().Where(x => x.CreditoId == id).FirstOrDefault();
                    }
                }
                catch (Exception)
                {
                }

                return CreditoActual;
            }

            public List<Credito> ObtenerListado(long? clienteId = null)
            {
                List<Credito> Creditos = new List<Credito>();

                try
                {
                    if (!clienteId.HasValue)
                    {
                        Creditos = db.Set<Credito>().Include("Tipo").Include("Agencia").Include("Cliente").Include("Pagos").AsNoTracking().Where(x => !x.Finalizado && !x.Anulada).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.CreditoId).ToList();
                    }
                    else
                    {
                        Creditos = db.Set<Credito>().Include("Tipo").Include("Agencia").Include("Cliente").Include("Pagos").AsNoTracking().Where(x => x.ClienteId == clienteId && !x.Anulada).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.CreditoId).Take(20).ToList();
                    }
                }
                catch (Exception)
                {
                }

                return Creditos;
            }

            public List<Credito> Buscar(string search)
            {
                List<Credito> Creditos = new List<Credito>();

                try
                {
                    Creditos = db.Set<Credito>().Include("Tipo").Include("Agencia").Include("Cliente").Include("Pagos").AsNoTracking().Where(x => x.Descripcion.Contains(search) && x.Finalizado == false).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.CreditoId).ToList();
                }
                catch (Exception)
                {
                }

                return Creditos;
            }

            public List<CreditoHistorial> ObtenerCreditosPorAgenciaYFecha(long agenciaId, long usuarioId, DateTime fechaInicial, DateTime fechaFinal)
            {
                List<CreditoHistorial> Creditos = new List<CreditoHistorial>();
                List<long> AgenciaIds = new List<long>();

                try
                {
                    if (agenciaId == 0)
                    {
                        AgenciaIds = db.Set<UsuarioAgencia>().AsNoTracking().Where(x => x.UsuarioId == usuarioId).Select(x => x.AgenciaId).ToList();
                    }
                    else
                    {
                        AgenciaIds.Add(agenciaId);
                    }

                    Creditos = db.Set<Credito>().Include("Cliente").Include("Tipo").Include("Agencia").Include("Detalles").Include("Pagos").AsNoTracking().Where(x => AgenciaIds.Contains(x.AgenciaId) && x.Fecha >= fechaInicial && x.Fecha <= fechaFinal).AsEnumerable().Select(x => new CreditoHistorial() { CreditoId = x.CreditoId, Cliente = x.Cliente.Nombre, Tipo = x.Tipo.Nombre, Agencia = x.Agencia.Nombre, Descripcion = x.Descripcion, FechaInicial = x.FechaInicial, FechaFinal = x.FechaFinal, Fecha = x.Fecha, Finalizado = x.Finalizado, MontoCredito = x.Detalles.Sum(d => d.Cantidad * d.Precio), MontoCancelado = x.Pagos.Count() == 0 ? 0 : x.Pagos.Sum(p => p.Valor) }).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.CreditoId).ToList();
                }
                catch (Exception)
                {
                }

                return Creditos;
            }

            public bool NuevoProductoCredito(CreditoDetalle pieza)
            {
                bool OperacionExitosa = false;

                try
                {
                    Credito CreditoActual = ObtenerPorId(pieza.CreditoId, false);

                    if (CreditoActual != null)
                    {
                        int Id = 1;
                        CreditoDetalle Detalle = db.Set<CreditoDetalle>().Where(x => x.CreditoId == pieza.CreditoId).OrderByDescending(x => x.DetalleId).FirstOrDefault();
                        if (Detalle != null)
                        {
                            Id = Detalle.DetalleId + 1;
                        }

                        pieza.DetalleId = Id;
                        db.Set<CreditoDetalle>().Add(pieza);

                        ProductoInventario InventarioActual = db.Set<ProductoInventario>().Where(x => x.ProductoId == pieza.ProductoId && x.AgenciaId == CreditoActual.AgenciaId).FirstOrDefault();
                        if (InventarioActual != null)
                        {
                            InventarioActual.Cantidad -= pieza.Cantidad;
                        }

                        db.SaveChanges();
                        OperacionExitosa = true;
                    }
                }
                catch (Exception)
                {
                }

                return OperacionExitosa;
            }

            public bool EliminarPieza(long CreditoId, long AgenciaId, string ProductoId)
            {
                try
                {
                    CreditoDetalle PiezaActual = db.Set<CreditoDetalle>().Where(x => x.CreditoId == CreditoId && x.ProductoId == ProductoId).FirstOrDefault();
                    if (PiezaActual != null && PiezaActual.CreditoId > 0)
                    {
                        db.Set<CreditoDetalle>().Remove(PiezaActual);
                    }

                    ProductoInventario InventarioActual = db.Set<ProductoInventario>().Where(x => x.AgenciaId == AgenciaId && x.ProductoId == ProductoId).FirstOrDefault();
                    if (InventarioActual != null)
                    {
                        InventarioActual.Cantidad += PiezaActual.Cantidad;
                    }

                    db.SaveChanges();
                }
                catch (Exception)
                {
                    return false;
                }

                return true;
            }

            public string Anular(long creditoId, string comentario, long usuarioId)
            {
                string Mensaje = "OK";

                try
                {

                    Credito CreditoActual = db.Set<Credito>().Include("Cliente").Include("Detalles").Include("Detalles.Producto").Include("Detalles.Unidad").Where(x => x.CreditoId == creditoId).FirstOrDefault();
                    if (CreditoActual == null)
                    {
                        return "El credito que selecciono no se encuentra disponible";
                    }

                    CreditoActual.Comentario = comentario;
                    CreditoActual.Anulada = true;
                    CreditoActual.UsrAnular = usuarioId;
                    CreditoActual.FechaAnular = DateTime.Now;

                    foreach (var Producto in CreditoActual.Detalles)
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

                        ProductoInventario InventarioActual = db.Set<ProductoInventario>().Where(x => x.ProductoId == Producto.ProductoId && x.AgenciaId == CreditoActual.AgenciaId).FirstOrDefault();
                        if (InventarioActual != null)
                        {
                            KardexExistenciaActual = InventarioActual.Cantidad;
                            KardexExistenciaFinal = InventarioActual.Cantidad + Cantidad;

                            InventarioActual.Cantidad += Cantidad;
                        }

                        //Se agrega la informacion al Kardex
                        db.Set<KardexMovimiento>().Add(new KardexMovimiento() { AgenciaId = CreditoActual.AgenciaId, TipoId = 14, Fecha = DateTime.Today, FechaHora = DateTime.Now, ProductoId = Producto.ProductoId, UnidadId = Producto.UnidadId, DocumentoId = CreditoActual.CreditoId, Cantidad = Producto.Cantidad, Precio = KardexPrecio, ExistenciaActual = KardexExistenciaActual, ExistenciaFinal = KardexExistenciaFinal, ResponsableId = CreditoActual.UsrAnular.Value });
                    }

                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

        #endregion
    }
}
