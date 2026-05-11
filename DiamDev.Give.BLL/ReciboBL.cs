using DiamDev.Give.DAL;
using DiamDev.Give.DAL.Migrations;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace DiamDev.Give.BLL
{
    public class ReciboBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public ReciboBL()
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
                    Recibo ReciboActual = db.Set<Recibo>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (ReciboActual != null)
                    {
                        Inicial_Id = ReciboActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;
                }
                catch (Exception)
                {}

                return Id;
            }

            private int CorrelativoReciboEnvase()
            {
                int Id = 0;

                try
                {
                    ReciboEnvase ReciboEnvaseActual = db.Set<ReciboEnvase>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (ReciboEnvaseActual != null)
                    {
                        Inicial_Id = ReciboEnvaseActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;
                }
                catch (Exception)
                {}

                return Id;
            }


            public string PagarReciboPagadito(long reciboid,string token) 
            {
                string Mensaje = "OK";

                try
                {
                    Recibo esactual = ObtenerPorId(reciboid, true, true,true);

                    esactual.Pagada = true;

                    esactual.Pagos = new List<ReciboFormaPago>();
                    ReciboFormaPago nuevo = new ReciboFormaPago();
                    nuevo.ReciboId = reciboid;
                    nuevo.Valor = esactual.Total;
                    nuevo.Fecha = DateTime.Today;
                    nuevo.Nota = token;
                    nuevo.UsrOperacionId = 20200506001;// USUARIO PEDIDOSAPP
                    nuevo.FormaPagoId = 20200517001;//FORMA PAGO PAGADITO
                    esactual.Pagos.Add(nuevo);

                    db.SaveChanges();
                }
                catch (Exception) 
                {
                    Mensaje = "NoOk";
                }

                return Mensaje;
            }

            private string Agregar(Recibo entidad)
            {
                string Mensaje = "OK";

                try
                {
                    //Se verifica disponibilidad de pago de cliente
                    if (!entidad.Pagada)
                    {
                        if (entidad.MesaId == 0)
                        {
                            Cliente ClienteActual = db.Set<Cliente>().AsNoTracking().Where(x => x.ClienteId == entidad.ClienteId).FirstOrDefault();
                            if (ClienteActual == null)
                            {
                                return "Se le informa que no contiene cliente asignado en el recibo";
                            }

                            decimal TotalRecibo = 0;
                            decimal LimiteCreditoCliente = ClienteActual.LimiteCredito == null ? 0 : ClienteActual.LimiteCredito.Value;

                            if (entidad.Detalles != null && entidad.Detalles.Count() > 0)
                            {
                                TotalRecibo = entidad.Detalles.Sum(x => x.Cantidad * x.Precio);
                            }

                            List<Recibo> RecibosNoPagados = db.Set<Recibo>().Include("Detalles").AsNoTracking().Where(x => x.ClienteId == ClienteActual.ClienteId && !x.Pagada && !x.Anulada).ToList();
                            if (RecibosNoPagados != null && RecibosNoPagados.Count() > 0)
                            {
                                TotalRecibo += RecibosNoPagados.Sum(x => x.Detalles.Sum(y => y.Cantidad * y.Precio));
                            }

                            if (TotalRecibo > LimiteCreditoCliente)
                            {
                                return "Se le informa que no se puede registrar el recibo no cuenta con el credito suficiente";
                            }
                        }                       
                    }

                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngReciboId = new Herramienta().Formato_Correlativo(Id);

                        if (lngReciboId > 0)
                        {
                            entidad.ReciboId = lngReciboId;
                            entidad.Correlativo = Id;

                            entidad.Fecha = DateTime.Today;                       
                            entidad.FechaHoraRecibo = DateTime.Now;                   
                            
                            if (entidad.DiaCredito > 0)
                            {
                                entidad.Credito = true;
                            }
                            else if (entidad.DiaCredito == 0)
                            {
                                entidad.Credito = false;
                            }                       

                            if (entidad.Detalles != null && entidad.Detalles.Count() > 0)
                            {
                                int DetalleId = 1;
                                foreach (var Producto in entidad.Detalles)
                                {
                                    Producto.DetalleId = DetalleId;
                                    Producto.ReciboId = entidad.ReciboId;

                                    if (entidad.MesaId == 0)
                                    {
                                        //Se obtiene el producto para convercion
                                        Producto ProductoPadreActual = new Producto();
                                        Producto ProductoHijoActual = new Producto();

                                        bool UnidadPadre = false;
                                        decimal Cantidad = Producto.Cantidad;
                                        decimal CantidadOriginal = 0;

                                        decimal KardexPrecio = Producto.Precio;
                                        decimal KardexExistenciaActual = 0;
                                        decimal KardexExistenciaFinal = 0;

                                        string ProductoPadreId = string.Empty;

                                        ProductoPadreActual = db.Set<Producto>().Where(x => x.ProductoId == Producto.ProductoId).FirstOrDefault();

                                        if (ProductoPadreActual != null)
                                        {
                                            if (!string.IsNullOrWhiteSpace(ProductoPadreActual.ProductoPadreId))
                                            {                                                
                                                ProductoPadreId = ProductoPadreActual.ProductoPadreId;
                                                ProductoPadreActual = db.Set<Producto>().Where(x => x.ProductoId == ProductoPadreId).FirstOrDefault();
                                            }
                                        }                                    

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
                                            ProductoHijoActual = db.Set<Producto>().Where(x => x.ProductoId == Producto.ProductoId && x.UnidadId == Producto.UnidadId).FirstOrDefault();

                                            if (ProductoHijoActual != null)
                                            {
                                                Cantidad *= ProductoHijoActual.Cantidad;
                                                CantidadOriginal = ProductoHijoActual.Cantidad;
                                            }
                                        }

                                        ProductoInventario InventarioActual = new ProductoInventario();

                                        if (!string.IsNullOrWhiteSpace(ProductoPadreId))
                                        {
                                            InventarioActual = db.Set<ProductoInventario>().Where(x => x.ProductoId == ProductoPadreId && x.AgenciaId == entidad.AgenciaId).FirstOrDefault();
                                        }
                                        else
                                        {
                                            InventarioActual = db.Set<ProductoInventario>().Where(x => x.ProductoId == Producto.ProductoId && x.AgenciaId == entidad.AgenciaId).FirstOrDefault();
                                        }
                                        
                                        if (InventarioActual != null)
                                        {
                                            KardexExistenciaActual = InventarioActual.Cantidad;
                                            KardexExistenciaFinal = InventarioActual.Cantidad - Cantidad;

                                            InventarioActual.Cantidad -= Cantidad;
                                        }

                                        //Agrega el precio costo al producto
                                        ProductoPrecioCosto CostoActual = db.Set<ProductoPrecioCosto>().Where(x => x.ProductoId == Producto.ProductoId).FirstOrDefault();
                                        if (CostoActual != null)
                                        {
                                            Producto.PrecioCosto = CostoActual.PrecioCosto;
                                        }

                                        if (!string.IsNullOrWhiteSpace(Producto.ID))
                                        {
                                            ProductoInventarioID InventarioIDActual = db.Set<ProductoInventarioID>().Where(x => x.ProductoId == Producto.ProductoId && x.ID.Equals(Producto.ID) && !x.Operado).FirstOrDefault();
                                            if (InventarioIDActual != null)
                                            {
                                                InventarioIDActual.Operado = true;
                                            }
                                        }

                                        //Se agrega la informacion al Kardex
                                        db.Set<KardexMovimiento>().Add(new KardexMovimiento() { AgenciaId = entidad.AgenciaId, TipoId = 3, Fecha = DateTime.Today, FechaHora = DateTime.Now, ProductoId = Producto.ProductoId, UnidadId = Producto.UnidadId, DocumentoId = entidad.ReciboId, Cantidad = Producto.Cantidad, Precio = KardexPrecio, ExistenciaActual = KardexExistenciaActual, ExistenciaFinal = KardexExistenciaFinal, ResponsableId = entidad.UsrCreo });
                                    }
                                    else
                                    {
                                        //Agrega el precio costo al producto
                                        ProductoPrecioCosto CostoActual = db.Set<ProductoPrecioCosto>().Where(x => x.ProductoId == Producto.ProductoId).FirstOrDefault();
                                        if (CostoActual != null)
                                        {
                                            Producto.PrecioCosto = CostoActual.PrecioCosto;
                                        }
                                    }

                                    DetalleId += 1;
                                }
                            }

                            if (entidad.Pagos != null && entidad.Pagos.Count() > 0)
                            {
                                int i = 1;
                                foreach (var Pago in entidad.Pagos)
                                {
                                    Pago.DetalleId = i;
                                    Pago.ReciboId = entidad.ReciboId;
                                    Pago.Fecha = DateTime.Today;
                                    Pago.UsrOperacionId = entidad.UsrCreo;

                                    i++;
                                }
                            }

                            if (entidad.PedidoId != null && entidad.PedidoId > 0)
                            {
                                Pedido PedidoActual = db.Set<Pedido>().Where(x => x.PedidoId == entidad.PedidoId.Value).FirstOrDefault();
                                if (PedidoActual != null)
                                {
                                    PedidoActual.Operada = true;
                                    PedidoActual.FechaHoraOpero = DateTime.Now;
                                    PedidoActual.UsrOpero = entidad.UsrCreo;

                                    //Agregar anotacion del pedido al recibo
                                    entidad.ComentarioPedido = PedidoActual.Descripcion;
                                }
                            }

                            if (entidad.PedidoId == 0)
                            {
                                entidad.PedidoId = null;                                
                            }

                            if (entidad.ReservaId != null)
                            {
                                Reserva ReservaActual = db.Set<Reserva>().Where(x => x.ReservaId == entidad.ReservaId.Value).FirstOrDefault();
                                if (ReservaActual != null)
                                {
                                    ReservaActual.Operado = true;                                   
                                }
                            }

                            //Se verifica que productos tiene configurado lote
                            if (entidad.Detalles != null && entidad.Detalles.Count() > 0)
                            {
                                List<string> ProductoIDs = entidad.Detalles.Select(x => x.ProductoId).ToList();
                                if (ProductoIDs != null && ProductoIDs.Count() > 0)
                                {
                                    entidad.ProductoLote = db.Set<Producto>().AsNoTracking().Where(x => ProductoIDs.Contains(x.ProductoId) && x.TieneLote).Count() > 0;
                                }
                            }

                            db.Set<Recibo>().Add(entidad);

                            if (entidad.MesaId != 0)
                            {
                                if (entidad.MesaId != 1)
                                {
                                    db.Set<MesaRecibo>().Add(new MesaRecibo() { MesaId = entidad.MesaId, ReciboId = entidad.ReciboId, PendientePago = true });
                                }
                                else if (entidad.MesaId == 1)
                                {
                                    db.Set<ReciboDelivery>().Add(new ReciboDelivery() { ReciboId = entidad.ReciboId, Fecha = DateTime.Today });
                                }
                            }

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

        #endregion

        #region Metodos Publicos

            public string Guardar(Recibo entidad, bool tienda = false)
            {
                string Mensaje = "OK";
                int Delivery = 0;

                if (!tienda)
                {
                    //Se obtiene la configuracion de Delivery Activado
                    Configuracion ConfiguracionActual = db.Set<Configuracion>().AsNoTracking().Where(x => x.ConfiguracionId == 20200722003).FirstOrDefault();
                    if (ConfiguracionActual != null)
                    {
                        Delivery = int.Parse(ConfiguracionActual.Valor);
                    }

                    if (Delivery == 0)
                    {
                        entidad.Reparto = false;
                    }
                    else if (Delivery == 1)
                    {
                        entidad.Reparto = true;
                    }
                }
                else if (tienda)
                {
                    DireccionCliente DireccionActual = db.Set<DireccionCliente>().AsNoTracking().Where(x => x.ClienteId == entidad.ClienteId).FirstOrDefault();
                    if (DireccionActual != null)
                    {
                        entidad.DireccionClienteId = DireccionActual.DireccionId;
                    }
                }

                if (entidad.ReciboId == 0)
                {
                    Mensaje = Agregar(entidad);
                }
                
                return Mensaje;
            }

            public string GuardarCliente(Recibo entidad)
            {
                string Mensaje = "OK";

                try
                {
                    Recibo ReciboActual = db.Set<Recibo>().Where(x => x.ReciboId == entidad.ReciboId).FirstOrDefault();
                    if (ReciboActual == null)
                    {
                        return "El recibo que selecciono no se encuentra disponible";
                    }
                  
                    ReciboActual.ClienteId = entidad.ClienteId;
                    db.SaveChanges();                   
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            public string GenerarFactura(long reciboId, long usuarioId, bool cambiaria)
            {
                string Mensaje = "OK";

                try
                {
                    Recibo ReciboActual = db.Set<Recibo>().Include("Tipo").Include("Agencia").Include("Cliente").Include("Vendedor").Include("Detalles").Include("Pagos").Where(x => x.ReciboId == reciboId).FirstOrDefault();
                    if (ReciboActual == null)
                    {
                        return "El recibo que selecciono no se encuentra disponible";
                    }

                    //Se identifica que ya se genero una factura
                    ReciboActual.Factura = true;
                    db.SaveChanges();

                    Factura NuevaFactura = new Factura();
                    NuevaFactura.AgenciaId = ReciboActual.AgenciaId;
                    NuevaFactura.SerieId = 20200520001;
                    NuevaFactura.VendedorId = ReciboActual.VendedorId;
                    NuevaFactura.ClienteId = ReciboActual.ClienteId;
                    NuevaFactura.Descuento = 0;
                    NuevaFactura.NoFactura = 0;
                    NuevaFactura.UsrCreo = usuarioId;
                    NuevaFactura.FacturaElectronica = true;
                    NuevaFactura.Empleado = false;
                    NuevaFactura.DiaCredito = 0;
                    NuevaFactura.TipoId = 1;
                    NuevaFactura.Reparto = false;
                    NuevaFactura.Pagada = ReciboActual.Pagada;
                    NuevaFactura.ServicioCliente = false;
                    NuevaFactura.EntregadoTransporte = false;
                    NuevaFactura.Credito = cambiaria;
                    NuevaFactura.ReciboId = ReciboActual.ReciboId;

                    NuevaFactura.Detalles = new List<FacturaDetalle>();

                    if (ReciboActual.Detalles != null && ReciboActual.Detalles.Count() > 0)
                    {
                        ReciboActual.Detalles.ForEach(x => 
                        {
                            NuevaFactura.Detalles.Add(new FacturaDetalle() { ProductoId = x.ProductoId, UnidadId = x.UnidadId, Cantidad = x.Cantidad, PrecioCosto = x.PrecioCosto, Precio = x.Precio, Descuento = 0, Nombre = x.Nombre });
                        });
                    }

                    NuevaFactura.Pagos = new List<FacturaFormaPago>();

                    if (ReciboActual.Pagos != null && ReciboActual.Pagos.Count() > 0)
                    {
                        ReciboActual.Pagos.ForEach(x =>
                        {
                            NuevaFactura.Pagos.Add(new FacturaFormaPago() { FormaPagoId = x.FormaPagoId, Valor = x.Valor, Nota = x.Nota, Fecha = x.Fecha, UsrOperacionId = x.UsrOperacionId });
                        });
                    }

                    Mensaje = new FacturaBL().GuardarLocal(NuevaFactura);
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            public string GuardarLote(Recibo entidad)
            {
                string Mensaje = "OK";

                try
                {
                    Recibo ReciboActual = db.Set<Recibo>().Where(x => x.ReciboId == entidad.ReciboId).FirstOrDefault();
                    if (ReciboActual != null)
                    {
                        ReciboActual.ProductoLote = false;

                        if (entidad.Lotes != null && entidad.Lotes.Count() > 0)
                        {
                             int DetalleId = 1;
                             foreach (var LoteActual in entidad.Lotes)
                             {
                                 LoteActual.DetalleId = DetalleId;
                                 LoteActual.ReciboId = entidad.ReciboId;
                                 
                                 //Se obtiene el lote actual
                                 ProductoLote LoteProductoActual = db.Set<ProductoLote>().Where(x => x.ProductoId == LoteActual.ProductoId && x.AgenciaId == ReciboActual.AgenciaId && x.Lote == LoteActual.Lote).FirstOrDefault();
                                 if (LoteProductoActual != null)
                                 {
                                     if (LoteActual.Cantidad > LoteProductoActual.Cantidad)
                                     {
                                         return string.Format("Se le informa que la cantidad solicitada es mayor a la existencia que contiene el #lote: {0}", LoteActual.Lote);
                                     }
                                     else
                                     {
                                         LoteActual.FechaVencimiento = LoteProductoActual.FechaVencimiento;
                                         LoteProductoActual.Cantidad -= LoteActual.Cantidad;
                                     }
                                 }
                                 else
                                 {
                                     return "Se le informa que el #lote ingresado no se encuentra registrado en el sistema";
                                 }

                                 db.Set<ReciboLote>().Add(LoteActual);
                                 DetalleId++;
                             }

                             db.SaveChanges();
                        }
                        else
                        {
                            return "Se le informa que el recibo ingresado no contiene lotes asignados";
                        }
                    }
                    else
                    {
                        return "Se le informa que el recibo seleccionado no se encuentra registrado en el sistema";
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }   

                return Mensaje;
            }

            public string GuardarFechaPagoEstimada(Recibo entidad) 
            {
                string Mensaje = "OK";

                try
                {
                    Recibo ReciboActual = db.Set<Recibo>().Where(x => x.ReciboId == entidad.ReciboId).FirstOrDefault();
                    if (ReciboActual != null)
                    {
                        ReciboActual.FechaPagoEstimada = entidad.FechaPagoEstimada;

                        db.SaveChanges();
                    }
                    else
                    {
                        Mensaje = "El recibo que selecciono no se encuentra registrado en el sistema";
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
                    Recibo ReciboActual = db.Set<Recibo>().Where(x => x.ReciboId == id).FirstOrDefault();
                    if (ReciboActual != null)
                    {
                        ReciboActual.Despachado = true;
                        ReciboActual.UsrDespacho = usuarioId;
                        ReciboActual.FechaHoraDespacho = DateTime.Now;

                        //Se verifica que los productos que contengan el recibo no tengan envases
                        List<ReciboDetalle> DetalleIDs = db.Set<ReciboDetalle>().AsNoTracking().Where(x => x.ReciboId == ReciboActual.ReciboId).ToList();
                        if (DetalleIDs != null && DetalleIDs.Count() > 0)
                        {
                            List<string> ProductoIDs = DetalleIDs.Select(x => x.ProductoId).ToList();
                            if (ProductoIDs != null && ProductoIDs.Count() > 0)
                            {
                                List<Producto> ProductosConEnvase = db.Set<Producto>().AsNoTracking().Where(x => ProductoIDs.Contains(x.ProductoId) && x.TieneEnvase).ToList();
                                if (ProductosConEnvase != null && ProductosConEnvase.Count() > 0)
                                {
                                    ProductoIDs = new List<string>();
                                    ProductoIDs = ProductosConEnvase.Select(x => x.ProductoId).ToList();

                                    DetalleIDs = DetalleIDs.Where(x => ProductoIDs.Contains(x.ProductoId)).ToList();
                                    if (DetalleIDs != null && DetalleIDs.Count() > 0)
                                    {
                                        int EnvaseId = CorrelativoReciboEnvase();

                                        if (EnvaseId > 0)
                                        {
                                            long lngReciboEnvaseId = new Herramienta().Formato_Correlativo(EnvaseId);

                                            if (lngReciboEnvaseId > 0)
                                            {
                                                ReciboEnvase ReciboEnvaseActual = new ReciboEnvase();
                                                ReciboEnvaseActual.ReciboEnvaseId = lngReciboEnvaseId;
                                                ReciboEnvaseActual.ReciboId = ReciboActual.ReciboId;
                                                ReciboEnvaseActual.AgenciaId = ReciboActual.AgenciaId;
                                                ReciboEnvaseActual.UsrCreo = usuarioId;
                                                ReciboEnvaseActual.Fecha = DateTime.Today;
                                                ReciboEnvaseActual.Correlativo = EnvaseId;

                                                ReciboEnvaseActual.Detalles = new List<ReciboEnvaseDetalle>();
                                                int DetalleId = 1;
                                                foreach (ReciboDetalle Detalle in DetalleIDs)
                                                {
                                                    ReciboEnvaseDetalle DetalleActual = new ReciboEnvaseDetalle();
                                                    DetalleActual.DetalleId = DetalleId;
                                                    DetalleActual.ReciboEnvaseId = ReciboEnvaseActual.ReciboEnvaseId;
                                                    DetalleActual.ProductoId = Detalle.ProductoId;
                                                    DetalleActual.UnidadId = Detalle.UnidadId;
                                                    DetalleActual.Cantidad = Detalle.Cantidad;

                                                    Producto ProductoActual = ProductosConEnvase.Where(x => x.ProductoId.Equals(Detalle.ProductoId)).FirstOrDefault();
                                                    if (ProductoActual != null)
                                                    {
                                                        DetalleActual.CantidadEnvase = ProductoActual.CantidadEnvase == null ? 0 : ProductoActual.CantidadEnvase.Value;                                            
                                                    }

                                                    ReciboEnvaseActual.Detalles.Add(DetalleActual);
                                                    DetalleId++;
                                                }

                                                db.Set<ReciboEnvase>().Add(ReciboEnvaseActual);
                                            }
                                        }           
                                    }
                                }                                   
                            }
                        }

                        db.SaveChanges();
                    }
                    else
                    {
                        Mensaje = "El recibo no se encuentra disponible";
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            public string Pagar(long id)
            {
                string Mensaje = "OK";

                try
                {
                    Recibo ReciboActual = db.Set<Recibo>().Where(x => x.ReciboId == id).FirstOrDefault();
                    if (ReciboActual != null)
                    {
                        ReciboActual.Pagada = true;
                        db.SaveChanges();
                    }
                    else
                    {
                        Mensaje = "El recibo no se encuentra disponible";
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            public string Envases(long id, long usuarioId)
            {
                string Mensaje = "OK";

                try
                {
                    ReciboEnvase ReciboEnvaseActual = db.Set<ReciboEnvase>().Where(x => x.ReciboEnvaseId == id).FirstOrDefault();
                    if (ReciboEnvaseActual != null)
                    {
                        ReciboEnvaseActual.UsrRecibe = usuarioId;
                        ReciboEnvaseActual.FechaRecibe = DateTime.Now;
                        db.SaveChanges();
                    }
                    else
                    {
                        Mensaje = "El recibo no se encuentra disponible";
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            public Recibo ObtenerPorId(long id, bool todo, bool recibo, bool totalizar = false)
            {
                Recibo ReciboActual = new Recibo();

                try
                {
                    if (todo)
                    {
                        if (recibo)
                        {
                            ReciboActual = db.Set<Recibo>().Include("Tipo").Include("Agencia").Include("Cliente").Include("Vendedor").Include("Pedido").Include("Pedido.UsuarioCreo").Include("Pedido.UsuarioOpero").Include("UsuarioCreo").Include("UsuarioDespacho").Include("Transporte").Include("Detalles").Include("Detalles.Producto").Include("Lotes").Include("Lotes.Producto").Include("Detalles.Unidad").Include("Pagos").Include("Pagos.FormaPago").Include("Pagos.UsuarioOperacion").Where(x => x.ReciboId == id).FirstOrDefault();
                        if (totalizar)
                        {
                            if (ReciboActual != null)
                            {
                                ReciboActual.DescuentoTotal = ReciboActual.Descuento == 0 ? 0 : (Convert.ToDecimal(ReciboActual.Descuento) / Convert.ToDecimal(100) * ReciboActual.Detalles.Sum(x => x.Cantidad * x.Precio));
                                ReciboActual.Total = ReciboActual.Detalles.Sum(x => x.Cantidad * x.Precio) - ReciboActual.DescuentoTotal;
                            }
                        }

                        //Se valida que exista informacion sobre el recibo y se agrega el numero de documento(Recibo) y las formas de pago como fue pagada el recibo
                        if (ReciboActual != null)
                            {
                                ReciboActual.Documento = string.Format("{0} - {1}", "RECIBO", ReciboActual.ReciboId);

                                if (ReciboActual.Pagos != null && ReciboActual.Pagos.Count() > 0) 
                                {
                                    foreach (var Pago in ReciboActual.Pagos)
                                    {
                                        ReciboActual.FormaPago += string.Format("{0} - {1:C},", Pago.FormaPago.Nombre, Pago.Valor);                                       
                                    }

                                    if (!string.IsNullOrWhiteSpace(ReciboActual.FormaPago))
                                    {
                                        ReciboActual.FormaPago = ReciboActual.FormaPago.Substring(0, ReciboActual.FormaPago.Length - 1);
                                        ReciboActual.FormaPago = ReciboActual.FormaPago.ToUpper();
                                    }
                                }
                            }
                        }                                         
                    }
                    else 
                    {
                        ReciboActual = db.Set<Recibo>().Where(x => x.ReciboId == id).FirstOrDefault();
                    if (totalizar)
                    {
                        if (ReciboActual != null)
                        {
                          Recibo  ReciboActual2 = db.Set<Recibo>().AsNoTracking().Include("Detalles").Where(x => x.ReciboId == id).FirstOrDefault();

                            ReciboActual.DescuentoTotal = ReciboActual.Descuento == 0 ? 0 : (Convert.ToDecimal(ReciboActual.Descuento) / Convert.ToDecimal(100) * ReciboActual.Detalles.Sum(x => x.Cantidad * x.Precio));
                            ReciboActual.Total = ReciboActual2.Detalles.Sum(x => x.Cantidad * x.Precio) - ReciboActual.DescuentoTotal;
                        }
                    }
                }
                   
                }
                catch (Exception)
                {
                }

                return ReciboActual;
            }

            public ReciboEnvase ObtenerEnvasePorId(long id)
            {
                ReciboEnvase ReciboEnvaseActual = new ReciboEnvase();

                try
                {
                    ReciboEnvaseActual = db.Set<ReciboEnvase>().Include("Agencia").Include("Recibo").Include("Recibo.Cliente").Include("Recibo.Transporte").Include("UsuarioCreo").Include("Detalles").Include("Detalles.Producto").Include("Detalles.Unidad").AsNoTracking().Where(x => x.ReciboEnvaseId == id).FirstOrDefault();
                }
                catch (Exception)
                {
                }

                return ReciboEnvaseActual;
            }

            public List<Recibo> BuscarRecibo(string recibo, long usuarioId, bool supervisor)
            {
                List<Recibo> Recibos = new List<Recibo>();
                List<long> AgenciasIds = new List<long>();

                try
                {
                    if (supervisor)
                    {
                        AgenciasIds = db.Set<UsuarioAgencia>().AsNoTracking().Where(x => x.UsuarioId == usuarioId).Join(db.Set<Agencia>().AsNoTracking().Where(x => x.EsDeliveryDomicilio), UA => UA.AgenciaId, A => A.AgenciaId, (UA, A) => new { A }).Select(x => x.A.AgenciaId).ToList();
                    }
                    else
                    {
                        AgenciasIds = db.Set<UsuarioAgencia>().AsNoTracking().Where(x => x.UsuarioId == usuarioId).AsEnumerable().Select(x => x.AgenciaId).ToList();
                    }

                    if (AgenciasIds != null && AgenciasIds.Count() > 0)
                    {                        
                        long NoReciboActual = 0;
                        bool EsNumero = long.TryParse(recibo, out NoReciboActual);
                        if (EsNumero)
                        {
                            Recibos = db.Set<Recibo>().Include("Tipo").Include("Agencia").Include("Cliente").Include("UsuarioCreo").Include("UsuarioDespacho").Include("Transporte").Include("Detalles").Where(x => x.ReciboId == NoReciboActual && AgenciasIds.Contains(x.AgenciaId)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ReciboId).ToList();
                        }
                        else {
                            Recibos = db.Set<Recibo>().Include("Tipo").Include("Agencia").Include("Cliente").Include("UsuarioCreo").Include("UsuarioDespacho").Include("Transporte").Include("Detalles").Where(x => x.Cliente.Nombre.Contains( recibo) && AgenciasIds.Contains(x.AgenciaId)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ReciboId).ToList();
                        }                      
                    }

                    if (Recibos != null && Recibos.Count() > 0)
                    {
                        Recibos.ForEach(x =>
                        {
                            if (x.Factura)
                            {
                                Factura FacturaActual = db.Set<Factura>().AsNoTracking().Where(y => y.ReciboId == x.ReciboId).FirstOrDefault();
                                if (FacturaActual != null)
                                {
                                    if (!string.IsNullOrWhiteSpace(FacturaActual.SerieFEL) && !string.IsNullOrWhiteSpace(FacturaActual.NumeroFEL))
                                    {
                                        x.FEL = true;
                                    }
                                }
                            }
                        });
                    }
                }
                catch (Exception)
                {}

                return Recibos;
            }

            public List<Recibo> ObtenerListadoPorFecha(DateTime fechaInicial, DateTime fechaFinal, long usuarioId, bool supervisor)
            {
                List<Recibo> Recibos = new List<Recibo>();
                List<long> AgenciasIds = new List<long>();

                try
                {
                    if (supervisor)
                    {
                        AgenciasIds = db.Set<UsuarioAgencia>().AsNoTracking().Where(x => x.UsuarioId == usuarioId).Join(db.Set<Agencia>().AsNoTracking().Where(x => x.EsDeliveryDomicilio), UA => UA.AgenciaId, A => A.AgenciaId, (UA, A) => new { A }).Select(x => x.A.AgenciaId).ToList();
                    }
                    else
                    {
                        AgenciasIds = db.Set<UsuarioAgencia>().Where(x => x.UsuarioId == usuarioId).AsEnumerable().Select(x => x.AgenciaId).ToList();
                    }
                    
                    if (AgenciasIds != null && AgenciasIds.Count() > 0)
                    {
                        Recibos = db.Set<Recibo>().Include("Tipo").Include("Agencia").Include("Cliente").Include("UsuarioCreo").Include("UsuarioDespacho").Include("Transporte").Include("Detalles").Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && AgenciasIds.Contains(x.AgenciaId)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ReciboId).ToList();
                    }

                    if (Recibos != null && Recibos.Count() > 0)
                    {
                        Recibos.ForEach(x => 
                        {
                            if (x.Factura)
                            {
                                Factura FacturaActual = db.Set<Factura>().AsNoTracking().Where(y => y.ReciboId == x.ReciboId).FirstOrDefault();
                                if (FacturaActual != null)
                                {
                                    if (!string.IsNullOrWhiteSpace(FacturaActual.SerieFEL) && !string.IsNullOrWhiteSpace(FacturaActual.NumeroFEL))
                                    {
                                        x.FEL = true;
                                    }
                                }
                            }
                        });
                    }
                }
                catch (Exception)
                {}

                return Recibos;
            }

            public Recibo ObtenerUltimoCliente(long ClienteId) 
            {
                Recibo Recibos = db.Set<Recibo>().Where(x => x.ClienteId==ClienteId&&!x.Reparto && !x.Despachado && !x.Anulada).FirstOrDefault();
                return Recibos;
            }

            public List<Recibo> Buscar(string search, long usuarioId)
            {
                List<Recibo> Recibos = new List<Recibo>();
                long ReciboId = 0;

                try
                {
                    long.TryParse(search, out ReciboId);

                    var AgenciasIds = db.Set<UsuarioAgencia>().Where(x => x.UsuarioId == usuarioId).AsEnumerable().Select(x => x.AgenciaId).ToList();
                    if (AgenciasIds != null && AgenciasIds.Count() > 0)
                    {
                        if (ReciboId > 0)
                        {
                            Recibos = db.Set<Recibo>().Include("Tipo").Include("Agencia").Include("Cliente").Include("Detalles").AsNoTracking().Where(x => x.ReciboId == ReciboId && AgenciasIds.Contains(x.AgenciaId)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ReciboId).ToList();
                        }
                        else
                        {
                            Recibos = db.Set<Recibo>().Include("Tipo").Include("Agencia").Include("Cliente").Include("Detalles").AsNoTracking().Where(x => (x.Agencia.Nombre.ToLower().Contains(search.ToLower()) || x.Cliente.Nombre.ToLower().Contains(search.ToLower())) && AgenciasIds.Contains(x.AgenciaId)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ReciboId).ToList();
                        }
                    }
                }
                catch (Exception)
                {}

                return Recibos;
            }           
           
            public List<ReciboEnvase> BuscarEnvasexRecibir(string search, long agenciaId)
            {
                List<ReciboEnvase> Recibos = new List<ReciboEnvase>();
                long ReciboId = 0;

                try
                {
                    long.TryParse(search, out ReciboId);

                    if (ReciboId > 0)
                    {
                        Recibos = db.Set<ReciboEnvase>().Include("Agencia").Include("Recibo").Include("Recibo.Cliente").Include("Recibo.Transporte").Include("UsuarioCreo").Include("Detalles").AsNoTracking().Where(x => x.ReciboId == ReciboId && x.AgenciaId == agenciaId).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ReciboId).ToList();
                    }
                    else
                    {
                        Recibos = db.Set<ReciboEnvase>().Include("Agencia").Include("Recibo").Include("Recibo.Cliente").Include("Recibo.Transporte").Include("UsuarioCreo").Include("Detalles").AsNoTracking().Where(x => (x.Agencia.Nombre.ToLower().Contains(search.ToLower()) || x.Recibo.Cliente.Nombre.ToLower().Contains(search.ToLower())) && x.AgenciaId == agenciaId).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ReciboId).ToList();
                    }
                }
                catch (Exception)
                {}

                return Recibos;
            }

            public List<Recibo> ObtenerListadoSinDespachar(long agenciaId)
            {
                List<Recibo> Recibos = new List<Recibo>();

                try
                {
                    Recibos = db.Set<Recibo>().Include("Tipo").Include("Agencia").Include("Vendedor").Include("Cliente").Include("Detalles").Include("Lotes").AsNoTracking().Where(x => !x.Anulada && !x.Despachado && x.Programada == true  && x.Reparto && x.AgenciaId == agenciaId).OrderBy(x => x.Fecha).ThenBy(x => x.ReciboId).ToList();
                }
                catch (Exception)
                {
                }

                return Recibos;
            }
            public List<Recibo> ObtenerListadoSinDespacharCocina(long agenciaId)
            {
                List<Recibo> Recibos = new List<Recibo>();

                try
                {
                    Recibos = db.Set<Recibo>().Include("Tipo").Include("Agencia").Include("Vendedor").Include("Cliente").Include("Detalles").Include("Lotes").AsNoTracking().Where(x => !x.Anulada && !x.Despachado && !x.Reparto && x.Programada==true && x.AgenciaId == agenciaId ).OrderBy(x => x.Fecha).ThenBy(x => x.ReciboId).ToList();
                }
                catch (Exception)
                {
                }

                return Recibos;
            }

            public List<ReciboEnvase> ObtenerListadoEnvasexRecibir(long agenciaId)
            {
                List<ReciboEnvase> Recibos = new List<ReciboEnvase>();

                try
                {
                    Recibos = db.Set<ReciboEnvase>().Include("Agencia").Include("Recibo").Include("Recibo.Cliente").Include("Recibo.Transporte").Include("UsuarioCreo").Include("Detalles").AsNoTracking().Where(x => x.UsrRecibe == null && x.AgenciaId == agenciaId).OrderBy(x => x.Fecha).ThenBy(x => x.ReciboId).ToList();
                }
                catch (Exception)
                {
                }

                return Recibos;
            }

            public string Anular(long reciboId, string comentario, long usuarioId)
            {
                string Mensaje = "OK";

                try
                {
                    Recibo ReciboActual = db.Set<Recibo>().Include("Tipo").Include("Cliente").Include("Detalles").Include("Detalles.Producto").Include("Detalles.Unidad").Include("Lotes").Where(x => x.ReciboId == reciboId).FirstOrDefault();
                    if (ReciboActual == null)
                    {
                        return "El recibo que selecciono no se encuentra disponible";
                    }                   
                                        
                    ReciboActual.Comentario = comentario;
                    ReciboActual.Anulada = true;
                    ReciboActual.UsrAnular = usuarioId;
                    ReciboActual.FechaAnular = DateTime.Now;

                    //Se verifica si contiene factura el recibo
                    bool FacturaExiste = false;

                    Factura FacturaActual = db.Set<Factura>().AsNoTracking().Where(x => x.ReciboId == ReciboActual.ReciboId).FirstOrDefault();
                    if (FacturaActual != null)
                    {
                        FacturaExiste = true;
                    }

                    if (FacturaExiste)
                    {
                        Mensaje = new FacturaBL().Anular(FacturaActual.FacturaId, comentario, usuarioId);
                    }

                    if (!Mensaje.Equals("OK"))
                    {
                        return Mensaje;
                    }

                    if (!FacturaExiste)
                    {
                        foreach (var Producto in ReciboActual.Detalles)
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

                            ProductoInventario InventarioActual = db.Set<ProductoInventario>().Where(x => x.ProductoId == Producto.ProductoId && x.AgenciaId == ReciboActual.AgenciaId).FirstOrDefault();
                            if (InventarioActual != null)
                            {
                                KardexExistenciaActual = InventarioActual.Cantidad;
                                KardexExistenciaFinal = InventarioActual.Cantidad + Cantidad;

                                InventarioActual.Cantidad += Cantidad;
                            }

                            if (!string.IsNullOrWhiteSpace(Producto.ID))
                            {
                                ProductoInventarioID InventarioIDActual = db.Set<ProductoInventarioID>().Where(x => x.ProductoId == Producto.ProductoId && x.ID.Equals(Producto.ID)).FirstOrDefault();
                                if (InventarioIDActual != null)
                                {
                                    InventarioIDActual.Operado = false;
                                }
                            }

                            //Se agrega la informacion al Kardex
                            db.Set<KardexMovimiento>().Add(new KardexMovimiento() { AgenciaId = ReciboActual.AgenciaId, TipoId = 10, Fecha = DateTime.Today, FechaHora = DateTime.Now, ProductoId = Producto.ProductoId, UnidadId = Producto.UnidadId, DocumentoId = ReciboActual.ReciboId, Cantidad = Producto.Cantidad, Precio = KardexPrecio, ExistenciaActual = KardexExistenciaActual, ExistenciaFinal = KardexExistenciaFinal, ResponsableId = ReciboActual.UsrAnular.Value });
                        }

                        if (ReciboActual.Lotes != null && ReciboActual.Lotes.Count() > 0)
                        {
                            foreach (ReciboLote LoteActual in ReciboActual.Lotes)
                            {
                                ProductoLote ProductoLoteActual = db.Set<ProductoLote>().Where(x => x.ProductoId == LoteActual.ProductoId && x.AgenciaId == ReciboActual.AgenciaId && x.Lote == LoteActual.Lote).FirstOrDefault();
                                if (ProductoLoteActual != null)
                                {
                                    ProductoLoteActual.Cantidad += LoteActual.Cantidad;
                                }
                            }
                        }
                    }

                    db.SaveChanges();                   
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            public string AsignarTransporte(long reciboId, long transporteId)
            {
                string Mensaje = "OK";

                try
                {
                    Recibo ReciboActual = db.Set<Recibo>().Where(x => x.ReciboId == reciboId).FirstOrDefault();
                    if (ReciboActual == null)
                    {
                        return "El recibo que selecciono no se encuentra disponible";
                    }

                    ReciboActual.TransporteId = transporteId;
                                    
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }
        public string FinalizarCocina(long reciboId,long UsuarioId)
        {
            string Mensaje = "OK";

            try
            {
                Recibo ReciboActual = db.Set<Recibo>().Where(x => x.ReciboId == reciboId).FirstOrDefault();
                if (ReciboActual == null)
                {
                    return "El recibo que selecciono no se encuentra disponible";
                }

                ReciboActual.Reparto = true;
                ReciboActual.UsrCocina = UsuarioId;
                ReciboActual.FechaHoraCocina = DateTime.Now;

                db.SaveChanges();
            }
            catch (Exception ex)
            {
                Mensaje = string.Format("Descripción del Error {0}", ex.Message);
            }

            return Mensaje;
        }
      
            public FacturaGarantia ObtenerProductosRecibo(long recibo)
            {
                FacturaGarantia FacturaActual = new FacturaGarantia();

                try
                {
                    Recibo Recibo = db.Set<Recibo>().Include("Cliente").Include("Detalles").Include("Detalles.Producto").AsNoTracking().Where(x => x.ReciboId == recibo).FirstOrDefault();
                    if (Recibo != null)
                    {
                        if (Recibo.Anulada)
                        {
                            FacturaActual.MensajeId = -2;
                        }
                        else
                        {
                            FacturaActual.MensajeId = 1;
                            FacturaActual.FacturaId = Recibo.ReciboId;
                            FacturaActual.Cliente = Recibo.Cliente.Nombre;

                            if (Recibo.Detalles != null && Recibo.Detalles.Count() > 0)
                            {
                                FacturaActual.Productos = new List<Producto>();
                                foreach (var item in Recibo.Detalles)
                                {
                                    FacturaActual.Productos.Add(item.Producto);
                                }
                            }
                        }
                    }
                    else
                    {
                        FacturaActual.MensajeId = -1;
                    }
                }
                catch (Exception)
                {
                }

                return FacturaActual;
            }

            public List<ReporteAbonoxCliente> ReporteAbonoxCliente(long clienteId, DateTime fechaInicial, DateTime fechaFinal)
            {
                List<ReporteAbonoxCliente> Abonos = new List<ReporteAbonoxCliente>();

                try
                {
                    if (clienteId == 0)
                    {
                        Abonos = db.Database.SqlQuery<ReporteAbonoxCliente>("dbo.sp_reporte_abonos_x_cliente @ClienteId, @FechaInicial, @FechaFinal", new SqlParameter("@ClienteId", DBNull.Value), new SqlParameter("@FechaInicial", fechaInicial), new SqlParameter("@FechaFinal", fechaFinal)).ToList();
                    }
                    else if (clienteId != 0)
                    {
                        Abonos = db.Database.SqlQuery<ReporteAbonoxCliente>("dbo.sp_reporte_abonos_x_cliente @ClienteId, @FechaInicial, @FechaFinal", new SqlParameter("@ClienteId", clienteId), new SqlParameter("@FechaInicial", fechaInicial), new SqlParameter("@FechaFinal", fechaFinal)).ToList();
                    }
                }
                catch (Exception)
                {
                }

                return Abonos;
            }

            public List<ReporteVentaxProductoDiaVendedor> ReporteVentaxProductoDiaVendedor(long agenciaId, long vendedorId, DateTime fechaInicial, DateTime fechaFinal)
            {
                List<ReporteVentaxProductoDiaVendedor> Ventas = new List<ReporteVentaxProductoDiaVendedor>();

                try
                {
                    if (agenciaId == 0 && vendedorId == 0)
                    {
                        Ventas = db.Database.SqlQuery<ReporteVentaxProductoDiaVendedor>("dbo.sp_reporte_venta_x_producto_dia_vendedor @AgenciaId, @VendedorId, @FechaInicial, @FechaFinal", new SqlParameter("@AgenciaId", DBNull.Value), new SqlParameter("@VendedorId", DBNull.Value), new SqlParameter("@FechaInicial", fechaInicial), new SqlParameter("@FechaFinal", fechaFinal)).ToList();
                    }
                    else if (agenciaId != 0 && vendedorId == 0)
                    {
                        Ventas = db.Database.SqlQuery<ReporteVentaxProductoDiaVendedor>("dbo.sp_reporte_venta_x_producto_dia_vendedor @AgenciaId, @VendedorId, @FechaInicial, @FechaFinal", new SqlParameter("@AgenciaId", agenciaId), new SqlParameter("@VendedorId", DBNull.Value), new SqlParameter("@FechaInicial", fechaInicial), new SqlParameter("@FechaFinal", fechaFinal)).ToList();
                    }
                    else if (agenciaId == 0 && vendedorId != 0)
                    {
                        Ventas = db.Database.SqlQuery<ReporteVentaxProductoDiaVendedor>("dbo.sp_reporte_venta_x_producto_dia_vendedor @AgenciaId, @VendedorId, @FechaInicial, @FechaFinal", new SqlParameter("@AgenciaId", DBNull.Value), new SqlParameter("@VendedorId", vendedorId), new SqlParameter("@FechaInicial", fechaInicial), new SqlParameter("@FechaFinal", fechaFinal)).ToList();
                    }
                    else if (agenciaId != 0 && vendedorId != 0)
                    {
                        Ventas = db.Database.SqlQuery<ReporteVentaxProductoDiaVendedor>("dbo.sp_reporte_venta_x_producto_dia_vendedor @AgenciaId, @VendedorId, @FechaInicial, @FechaFinal", new SqlParameter("@AgenciaId", agenciaId), new SqlParameter("@VendedorId", vendedorId), new SqlParameter("@FechaInicial", fechaInicial), new SqlParameter("@FechaFinal", fechaFinal)).ToList();
                    }
                }
                catch (Exception)
                {
                }

                return Ventas;
            }

            public decimal ObtenerTotalxMesaId(long mesaId)
            {
                decimal Total = 0;

                try
                {
                    List<long> ReciboIDs = db.Set<MesaRecibo>().AsNoTracking().Where(x => x.MesaId == mesaId && x.PendientePago).Select(x => x.ReciboId).ToList();
                    if (ReciboIDs != null && ReciboIDs.Count() > 0)
                    {
                        foreach (long ReciboActualId in ReciboIDs)
                        {
                            List<ReciboDetalle> Productos = db.Set<ReciboDetalle>().AsNoTracking().Where(x => x.ReciboId == ReciboActualId).ToList();
                            if (Productos != null && Productos.Count() > 0)
                            {
                                Productos.ForEach(x => 
                                {
                                    Total += x.Cantidad * x.Precio;
                                });
                            }
                        }
                    }
                }
                catch (Exception)
                {}

                return Total;
            }

            public CuentaGeneral ObtenerCuentaTotalxMesaId(long mesaId)
            {
                CuentaGeneral CuentaActual = new CuentaGeneral();
                List<CuentaModel> Cuentas = new List<CuentaModel>();

                try
                {
                    List<long> ReciboIDs = db.Set<MesaRecibo>().AsNoTracking().Where(x => x.MesaId == mesaId && x.PendientePago).Select(x => x.ReciboId).ToList();
                    if (ReciboIDs != null && ReciboIDs.Count() > 0)
                    {
                        foreach (long ReciboActualId in ReciboIDs)
                        {
                            List<ReciboDetalle> Productos = db.Set<ReciboDetalle>().AsNoTracking().Where(x => x.ReciboId == ReciboActualId).ToList();
                            if (Productos != null && Productos.Count() > 0)
                            {
                                Productos.ForEach(x =>
                                {
                                    Cuentas.Add(new CuentaModel() { ProductoId = x.ProductoId, Producto = x.Nombre, UnidadId = x.UnidadId, Cantidad = x.Cantidad, Precio = x.Precio });
                                });                                
                            }                           
                        }
                    }

                    if (Cuentas != null && Cuentas.Count() > 0)
                    {
                        Cuentas = Cuentas.GroupBy(x => new { x.ProductoId, x.UnidadId, x.Producto, x.Precio }).Select(g => new CuentaModel() { ProductoId = g.Key.ProductoId, UnidadId = g.Key.UnidadId, Producto = g.Key.Producto, Cantidad = g.Sum(y => y.Cantidad), Precio = g.Key.Precio }).ToList();
                    }

                    CuentaActual.Cuentas = new List<CuentaModel>();
                    CuentaActual.Cuentas = Cuentas;
                }
                catch (Exception)
                { }

                return CuentaActual;
            }

            public string GenerarReciboCuentaGeneral(CuentaGeneral modelo) 
            {
                string Mensaje = "OK";
                List<CuentaModel> Cuentas = new List<CuentaModel>();

                try
                {
                    List<long> ReciboIDs = db.Set<MesaRecibo>().AsNoTracking().Where(x => x.MesaId == modelo.MesaId && x.PendientePago).Select(x => x.ReciboId).ToList();
                    if (ReciboIDs != null && ReciboIDs.Count() > 0)
                    {
                        foreach (long ReciboActualId in ReciboIDs)
                        {
                            List<ReciboDetalle> Productos = db.Set<ReciboDetalle>().AsNoTracking().Where(x => x.ReciboId == ReciboActualId).ToList();
                            if (Productos != null && Productos.Count() > 0)
                            {
                                Productos.ForEach(x =>
                                {
                                    Cuentas.Add(new CuentaModel() { ProductoId = x.ProductoId, Producto = x.Nombre, UnidadId = x.UnidadId, Cantidad = x.Cantidad, Precio = x.Precio });
                                });
                            }
                        }
                    }

                    if (Cuentas != null && Cuentas.Count() > 0)
                    {
                        Cuentas = Cuentas.GroupBy(x => new { x.ProductoId, x.UnidadId, x.Producto, x.Precio }).Select(g => new CuentaModel() { ProductoId = g.Key.ProductoId, UnidadId = g.Key.UnidadId, Producto = g.Key.Producto, Cantidad = g.Sum(y => y.Cantidad), Precio = g.Key.Precio }).ToList();
                    }

                    if (Cuentas != null && Cuentas.Count() > 0)
                    {
                        int? DireccionId = null; 

                        DireccionCliente DireccionActual = db.Set<DireccionCliente>().AsNoTracking().Where(x => x.ClienteId == modelo.ClienteId).OrderByDescending(x => x.DireccionId).FirstOrDefault();
                        if (DireccionActual != null)
                        {
                            DireccionId = DireccionActual.DireccionId;
                        }

                        Recibo ReciboActual = new Recibo();
                        ReciboActual.TipoId = 1;
                        ReciboActual.AgenciaId = modelo.AgenciaId;
                        ReciboActual.VendedorId = 20200506001;
                        ReciboActual.ClienteId = modelo.ClienteId;
                        ReciboActual.Anulada = false;
                        ReciboActual.Empleado = false;
                        ReciboActual.Reparto = true;
                        ReciboActual.Pagada = false;
                        ReciboActual.Credito = false;
                        ReciboActual.DiaCredito = 0;
                        ReciboActual.Despachado = true;
                        ReciboActual.Programada = false;
                        ReciboActual.EntregadoTransporte = false;
                        ReciboActual.Factura = false;
                        ReciboActual.UsrCreo = 20200506001;
                        ReciboActual.MesaId = 1;

                        if (DireccionId != null)
                        {
                            ReciboActual.DireccionClienteId = DireccionId.Value;
                        }

                        //Se agrega el detalle del recibo
                        ReciboActual.Detalles = new List<ReciboDetalle>();
                        foreach (CuentaModel Cuenta in Cuentas)
                        {
                            ReciboDetalle Detalle = new ReciboDetalle();
                            Detalle.ProductoId = Cuenta.ProductoId;
                            Detalle.UnidadId = Cuenta.UnidadId;
                            Detalle.Nombre = Cuenta.Producto;
                            Detalle.Cantidad = Cuenta.Cantidad;
                            Detalle.Descuento = 0;
                            Detalle.Precio = Cuenta.Precio;

                            ReciboActual.Detalles.Add(Detalle);
                        }                       

                        string Observaciones = string.Format("#Mesa: {0} - Mesa: {1}\n\n **CUENTA GENERAL**", modelo.MesaId, modelo.Mesa);
                    
                        ReciboActual.ComentarioPedido = Observaciones;

                        Mensaje = Guardar(ReciboActual);

                        if (Mensaje.Equals("OK"))
                        {
                            //Se eliminan los recibos de la mesa
                            List<MesaRecibo> Recibos = db.Set<MesaRecibo>().Where(x => x.MesaId == modelo.MesaId && x.PendientePago).ToList();
                            db.Set<MesaRecibo>().RemoveRange(Recibos);

                            //Se anulan los recibos 
                            List<Recibo> AnularRecibos = db.Set<Recibo>().Where(x => ReciboIDs.Contains(x.ReciboId)).ToList();
                            if (AnularRecibos != null && AnularRecibos.Count() > 0)
                            {
                                foreach (Recibo AnularRecibo in AnularRecibos)
                                {
                                    AnularRecibo.Comentario = "**MOTIVO DE ANULACION X CUENTA GENERAL**";
                                    AnularRecibo.Anulada = true;
                                    AnularRecibo.UsrAnular = 20200506001;
                                    AnularRecibo.FechaAnular = DateTime.Now;
                                }
                            }

                            //Se inactiva el token
                            Token TokenActual = db.Set<Token>().Where(x => x.TokenValido.Equals(modelo.Token) && !x.Administrativo).FirstOrDefault();
                            if (TokenActual != null)
                            {
                                TokenActual.Activo = false;
                            }

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

            public List<Recibo> ObtenerPendientesCancelarDelivery(long agenciaId) 
            {
                List<Recibo> Recibos = new List<Recibo>();

                try
                {
                    List<long> ReciboIDs = db.Set<ReciboDelivery>().AsNoTracking().Where(x => !x.Operado).Select(x => x.ReciboId).ToList();
                    if (ReciboIDs != null && ReciboIDs.Count() > 0)
                    {
                        Recibos = db.Set<Recibo>().Include("Tipo").Include("Cliente").Include("Detalles").AsNoTracking().Where(x => x.AgenciaId == agenciaId && ReciboIDs.Contains(x.ReciboId) && !x.Anulada).ToList();
                    }
                }
                catch (Exception)
                {}

                return Recibos;
            }

            public string LiquidarDelivery(long id)
            {
                string Mensaje = "OK";

                try
                {
                    ReciboDelivery ReciboActual = db.Set<ReciboDelivery>().Where(x => x.ReciboId == id && !x.Operado).FirstOrDefault();
                    if (ReciboActual != null)
                    {
                        ReciboActual.Operado = true;
                        ReciboActual.FechaOperado = DateTime.Now;
                        db.SaveChanges();
                    }
                    else
                    {
                        Mensaje = "El recibo no se encuentra disponible";
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            public string EnviarCorreo(long id)
            {
                string Mensaje = "OK";
                string CorreoNotificacion = "";
                string MensajeNotificacion = "";

                try
                {
                    //Se obtiene la configuracion del correo
                    Configuracion ConfiguracionActual = db.Set<Configuracion>().AsNoTracking().Where(x => x.ConfiguracionId == 20170611001).FirstOrDefault();
                    if (ConfiguracionActual != null)
                    {
                        CorreoNotificacion = ConfiguracionActual.Valor;
                    }

                    //Se obtiene el cliente del recibo para verificar su correo
                    Recibo ReciboActual = db.Set<Recibo>().Include("Cliente").AsNoTracking().Where(x => x.ReciboId == id).FirstOrDefault();
                    if (ReciboActual != null)
                    {
                        if (ReciboActual.Cliente != null)
                        {
                            if (!string.IsNullOrWhiteSpace(ReciboActual.Cliente.EmailCliente))
                            {
                                if (!ReciboActual.Cliente.EmailCliente.Equals("sincorreo@sincorreo.com"))
                                {
                                    CorreoNotificacion = ReciboActual.Cliente.EmailCliente;
                                }
                            }
                        }
                    }

                    //Se obtiene la factura
                    Factura FacturaActual = db.Set<Factura>().AsNoTracking().Where(x => x.ReciboId == id).FirstOrDefault();
                    if (FacturaActual != null)
                    {
                        MensajeNotificacion = string.Format("FACTURA ELECTRONICA: SERIE: {0} - NUMERO: {1}", FacturaActual.SerieFEL, FacturaActual.NumeroFEL);
                    }

                    Herramienta.EnviarCorreoAsync(string.Format(@"<html><body><b>FERRETERIA JIREH</b><hr /><p>{0} <br /><br />Le enviamos el enlance de su factura electronica para descargar <a href='{1}'>aqui</a></p></body></html>", MensajeNotificacion, string.Format("http://104.225.140.235/ServiciosRAGA/SmartJirehReportes/Factura?factid={0}", id)), CorreoNotificacion);
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            public List<ProductosxCliente> ProductosxCliente(long agenciaId, long clienteId, string productoId)
            {
                List<ProductosxCliente> Ventas = new List<ProductosxCliente>();

                try
                {
                    Ventas = db.Database.SqlQuery<ProductosxCliente>("dbo.sp_consulta_productos_x_cliente @AgenciaId, @ClienteId, @ProductoId", new SqlParameter("@AgenciaId", agenciaId), new SqlParameter("@ClienteId", clienteId), new SqlParameter("@ProductoId", productoId)).ToList();
                }
                catch (Exception)
                {}

                return Ventas;
            }

            //Morosidad
            public List<ReciboMorosidad> ConsultaMorosidadCritica(long agenciaId)
            {
                List<ReciboMorosidad> Recibos = new List<ReciboMorosidad>();

                try
                {
                    Recibos = db.Database.SqlQuery<ReciboMorosidad>("dbo.sp_consulta_morosidad_critica @AgenciaId", new SqlParameter("@AgenciaId", agenciaId)).ToList();
                }
                catch (Exception)
                { }

                return Recibos;
            }

            public List<ReciboMorosidad> ConsultaMorosidadAlta(long agenciaId)
            {
                List<ReciboMorosidad> Recibos = new List<ReciboMorosidad>();

                try
                {
                    Recibos = db.Database.SqlQuery<ReciboMorosidad>("dbo.sp_consulta_morosidad_alta @AgenciaId", new SqlParameter("@AgenciaId", agenciaId)).ToList();
                }
                catch (Exception)
                { }

                return Recibos;
            }

            public List<ReciboMorosidad> ConsultaMorosidadMedia(long agenciaId)
            {
                List<ReciboMorosidad> Recibos = new List<ReciboMorosidad>();

                try
                {
                    Recibos = db.Database.SqlQuery<ReciboMorosidad>("dbo.sp_consulta_morosidad_media @AgenciaId", new SqlParameter("@AgenciaId", agenciaId)).ToList();
                }
                catch (Exception)
                { }

                return Recibos;
            }

            public List<ReciboMorosidad> ConsultaMorosidadBaja(long agenciaId)
            {
                List<ReciboMorosidad> Recibos = new List<ReciboMorosidad>();

                try
                {
                    Recibos = db.Database.SqlQuery<ReciboMorosidad>("dbo.sp_consulta_morosidad_baja @AgenciaId", new SqlParameter("@AgenciaId", agenciaId)).ToList();
                }
                catch (Exception)
                { }

                return Recibos;
            }

            public int CantidadConsultaMorosidadCritica(long agenciaId)
            {
                List<ReciboMorosidad> Recibos = new List<ReciboMorosidad>();
                int Cantidad = 0;

                try
                {
                    Recibos = ConsultaMorosidadCritica(agenciaId);
                    if (Recibos != null && Recibos.Count() > 0)
                    {
                        Cantidad = Recibos.Count();
                    }
                }
                catch (Exception)
                { }

                return Cantidad;
            }

            public int CantidadConsultaMorosidadAlta(long agenciaId)
            {
                List<ReciboMorosidad> Recibos = new List<ReciboMorosidad>();
                int Cantidad = 0;

                try
                {
                    Recibos = ConsultaMorosidadAlta(agenciaId);
                    if (Recibos != null && Recibos.Count() > 0)
                    {
                        Cantidad = Recibos.Count();
                    }
                }
                catch (Exception)
                { }

                return Cantidad;
            }

            public int CantidadConsultaMorosidadMedia(long agenciaId)
            {
                List<ReciboMorosidad> Recibos = new List<ReciboMorosidad>();
                int Cantidad = 0;

                try
                {
                    Recibos = ConsultaMorosidadMedia(agenciaId);
                    if (Recibos != null && Recibos.Count() > 0)
                    {
                        Cantidad = Recibos.Count();
                    }
                }
                catch (Exception)
                { }

                return Cantidad;
            }

            public int CantidadConsultaMorosidadBaja(long agenciaId)
            {
                List<ReciboMorosidad> Recibos = new List<ReciboMorosidad>();
                int Cantidad = 0;

                try
                {
                    Recibos = ConsultaMorosidadBaja(agenciaId);
                    if (Recibos != null && Recibos.Count() > 0)
                    {
                        Cantidad = Recibos.Count();
                    }
                }
                catch (Exception)
                { }

                return Cantidad;
            }

            public List<ReciboFechaPagoEstimadaModel> ObtenerReciboNoPagadoxFechaEstimada(DateTime fechaInicial, DateTime fechaFinal, long agenciaId)
            {
                List<ReciboFechaPagoEstimadaModel> Recibos = new List<ReciboFechaPagoEstimadaModel>();

                try
                {
                    Recibos = db.Database.SqlQuery<ReciboFechaPagoEstimadaModel>("dbo.sp_recibo_fecha_pago_estimada @FechaInicial,@FechaFinal,@AgenciaId", new SqlParameter("@FechaInicial", fechaInicial), new SqlParameter("@FechaFinal", fechaFinal), new SqlParameter("@AgenciaId", agenciaId)).ToList();
                }
                catch (Exception)
                { }

                return Recibos;
            }

            public string EliminarPago(long reciboId, int id)
            {
                string Mensaje = "OK";              

                try
                {
                    ReciboFormaPago ReciboFormaPagoActual = db.Set<ReciboFormaPago>().Where(x => x.ReciboId == reciboId && x.DetalleId == id).FirstOrDefault();
                    if (ReciboFormaPagoActual != null)
                    {
                        db.Set<ReciboFormaPago>().Remove(ReciboFormaPagoActual);

                        //RECIBO
                        Recibo ReciboActual = db.Set<Recibo>().Where(x => x.ReciboId == reciboId).FirstOrDefault();
                        if (ReciboActual != null)
                        {
                            ReciboActual.Despachado = true;
                            ReciboActual.Pagada = false;
                        }

                        db.SaveChanges();
                    }
                    else
                    {
                        Mensaje = "No se elimino la forma de pago del recibo";
                    }
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
