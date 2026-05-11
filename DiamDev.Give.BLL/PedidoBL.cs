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
    public class PedidoBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public PedidoBL()
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
                    Pedido PedidoActual = db.Set<Pedido>().AsNoTracking().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (PedidoActual != null)
                    {
                        Inicial_Id = PedidoActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;
                }
                catch (Exception)
                {}

                return Id;
            }

            private int CorrelativoRecibo()
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
        
            private bool Agregar(Pedido entidad)
            {
                bool PedidoAgregar = false;

                string PathFotografia = ConfigurationManager.AppSettings["Path_Fotografia_Cotizacion"].ToString();
                string UrlFotografia = ConfigurationManager.AppSettings["Url_Fotografia_Cotizacion"].ToString();

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngPedidoId = new Herramienta().Formato_Correlativo(Id);

                        if (lngPedidoId > 0)
                        {
                            entidad.PedidoId = lngPedidoId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;
                            entidad.FechaHoraCreacion = DateTime.Now;

                            if (!string.IsNullOrWhiteSpace(entidad.FotografiaCotizacion))
                            {
                                entidad.FotografiaCotizacion = string.Format(@"{0}{1}/{2}", UrlFotografia, entidad.PedidoId, entidad.FotografiaCotizacion);
                            }

                            if (entidad.Detalles != null && entidad.Detalles.Count() > 0)
                            {
                                int i = 1;
                                foreach (var Detalle in entidad.Detalles)
                                {
                                    Detalle.DetalleId = i;
                                    Detalle.PedidoId = entidad.PedidoId;
                                    i++;
                                }
                            }

                            db.Set<Pedido>().Add(entidad);
                            db.SaveChanges();
                            PedidoAgregar = true;

                            if (PedidoAgregar)
                            {
                                //Se crea carpeta de la fotografia
                                string Path_Fotografia_Pedido = string.Format(@"{0}\{1}", PathFotografia, entidad.PedidoId);

                                if (!(Directory.Exists(Path_Fotografia_Pedido)))
                                {
                                    Directory.CreateDirectory(Path_Fotografia_Pedido);
                                }

                                if (entidad.Fotografia != null)
                                {
                                    ConvetirbyteAImage(entidad.Fotografia.Content).Save(string.Format(@"{0}\{1}", Path_Fotografia_Pedido, "cotizacion.png"));
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {}

                return PedidoAgregar;
            }

            public bool Actualizar(Pedido entidad) 
            {
                bool PedidoEditar = false;

                string PathFotografia = ConfigurationManager.AppSettings["Path_Fotografia_Cotizacion"].ToString();
                string UrlFotografia = ConfigurationManager.AppSettings["Url_Fotografia_Cotizacion"].ToString();

                try
                {
                    Pedido PedidoActual = db.Set<Pedido>().Where(x => x.PedidoId == entidad.PedidoId).FirstOrDefault();
                    if (PedidoActual != null)
                    {
                        PedidoActual.ClienteId = entidad.ClienteId;
                        PedidoActual.Descripcion = entidad.Descripcion;
                        PedidoActual.Cotizacion = entidad.Cotizacion;
                        PedidoActual.FormaPago = entidad.FormaPago;
                        PedidoActual.TiempoEntrega = entidad.TiempoEntrega;
                        PedidoActual.VendedorId = entidad.VendedorId;

                        if (!string.IsNullOrWhiteSpace(entidad.FotografiaCotizacion))
                        {
                            PedidoActual.FotografiaCotizacion = string.Format(@"{0}{1}/{2}", UrlFotografia, PedidoActual.PedidoId, entidad.FotografiaCotizacion);
                        }

                        var Detalles = db.Set<PedidoDetalle>().Where(x => x.PedidoId == entidad.PedidoId).ToList();
                        db.Set<PedidoDetalle>().RemoveRange(Detalles);

                        if (entidad.Detalles != null && entidad.Detalles.Count() > 0)
                        {
                            int i = 1;
                            foreach (var Detalle in entidad.Detalles)
                            {
                                Detalle.DetalleId = i;
                                Detalle.PedidoId = entidad.PedidoId;
                                db.Set<PedidoDetalle>().Add(Detalle);
                                i++;
                            }
                        }

                        db.SaveChanges();
                        PedidoEditar = true;

                        if (PedidoEditar)
                        {
                            //Se crea carpeta de la fotografia
                            string Path_Fotografia_Pedido = string.Format(@"{0}\{1}", PathFotografia, entidad.PedidoId);

                            if (!(Directory.Exists(Path_Fotografia_Pedido)))
                            {
                                Directory.CreateDirectory(Path_Fotografia_Pedido);
                            }

                            if (entidad.Fotografia != null)
                            {
                                ConvetirbyteAImage(entidad.Fotografia.Content).Save(string.Format(@"{0}\{1}", Path_Fotografia_Pedido, "cotizacion.png"));
                            }
                        }
                    }
                }
                catch (Exception)
                {}

                return PedidoEditar;
            }

            private Image ConvetirbyteAImage(byte[] byteArrayIn)
            {
                return Image.FromStream(new MemoryStream(byteArrayIn));
            }

        #endregion

        #region Metodos Publicos

            public string Guardar(Pedido entidad)
            {
                string Mensaje = "OK";
                bool OperacionExitosa = false;

                if (entidad.PedidoId > 0)
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

            public string Duplicar(Pedido entidad, long usuarioId)
            {
                string Mensaje = "OK";

                try
                {
                    Pedido PedidoActual = db.Set<Pedido>().Include("Detalles").AsNoTracking().Where(x => x.PedidoId == entidad.PedidoId).FirstOrDefault();
                    if (PedidoActual != null)
                    {
                        Pedido NuevoPedido = new Pedido();
                        NuevoPedido.AgenciaId = PedidoActual.AgenciaId;
                        NuevoPedido.ClienteId = PedidoActual.ClienteId;
                        NuevoPedido.VendedorId = PedidoActual.VendedorId;
                        NuevoPedido.Descripcion = PedidoActual.Descripcion;
                        NuevoPedido.FormaPago = PedidoActual.FormaPago;
                        NuevoPedido.TiempoEntrega = PedidoActual.TiempoEntrega;
                        NuevoPedido.Operada = false;
                        NuevoPedido.Anulada = false;
                        NuevoPedido.UsrCreo = usuarioId;
                        NuevoPedido.Cotizacion = PedidoActual.Cotizacion;

                        NuevoPedido.Detalles = new List<PedidoDetalle>();

                        int DetalleId = 1;
                        foreach (var Producto in PedidoActual.Detalles)
                        {
                            PedidoDetalle DetalleActual = new PedidoDetalle();
                            DetalleActual.DetalleId = DetalleId;                            
                            DetalleActual.ProductoId = Producto.ProductoId;
                            DetalleActual.UnidadId = Producto.UnidadId;
                            DetalleActual.Nombre = Producto.Nombre;
                            DetalleActual.Descuento = Producto.Descuento;
                            DetalleActual.Cantidad = Producto.Cantidad;
                            DetalleActual.PrecioCosto = Producto.PrecioCosto;
                            DetalleActual.Precio = Producto.Precio;

                            DetalleId += 1;
                            NuevoPedido.Detalles.Add(DetalleActual);
                        }

                        Mensaje = Agregar(NuevoPedido) ? "OK" : "No se registro el pedido";
                    }
                    else
                    {
                        return "Se le informa que el pedido no se encuentra registrado en el sistema";
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            public long AgregarAnonimo(Pedido entidad)
            {
                long ID = 0;

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngPedidoId = new Herramienta().Formato_Correlativo(Id);

                        if (lngPedidoId > 0)
                        {
                            entidad.PedidoId = lngPedidoId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            if (entidad.Detalles != null && entidad.Detalles.Count() > 0)
                            {
                                int i = 1;
                                foreach (var Detalle in entidad.Detalles)
                                {
                                    Detalle.DetalleId = i;
                                    Detalle.PedidoId = entidad.PedidoId;
                                    i++;
                                }
                            }

                            db.Set<Pedido>().Add(entidad);
                            db.SaveChanges();

                            ID = lngPedidoId;
                        }
                    }
                }
                catch (Exception)
                {}

                return ID;
            }

            public string Operar(long pedidoId, long usuarioId) 
            {
                string Mensaje = string.Empty;

                try
                {
                    Pedido PedidoActual = db.Set<Pedido>().Include("Detalles").Where(x => x.PedidoId == pedidoId).FirstOrDefault();
                    if (PedidoActual != null)
                    {
                        long VendedorId = 0;

                        Usuario UsuarioPedido = db.Set<Usuario>().AsNoTracking().Where(x => x.UsuarioId == PedidoActual.UsrCreo).FirstOrDefault();
                        if (UsuarioPedido != null)
	                    {
                            Vendedor VendedorPedido = db.Set<Vendedor>().AsNoTracking().Where(x => x.VendedorId == UsuarioPedido.VendedorId).FirstOrDefault();
                            if (VendedorPedido != null)
                            {
                                VendedorId = VendedorPedido.VendedorId;                              
                            }
	                    }

                        if (VendedorId == 0)
                        {
                            return "Se le informa que el usuario que realizo el pedido no tiene registrado a un vendedor";                            
                        }

                        int Id = CorrelativoRecibo();

                        if (Id > 0)
                        {
                            long lngReciboId = new Herramienta().Formato_Correlativo(Id);

                            if (lngReciboId > 0)
                            {
                                Recibo ReciboActual = new Recibo();
                                ReciboActual.ReciboId = lngReciboId;
                                ReciboActual.TipoId = 1;
                                ReciboActual.AgenciaId = PedidoActual.AgenciaId;
                                ReciboActual.VendedorId = VendedorId;
                                ReciboActual.ClienteId = PedidoActual.ClienteId;
                                ReciboActual.PedidoId = PedidoActual.PedidoId;
                                ReciboActual.Descuento = 0;
                                ReciboActual.Anulada = false;
                                ReciboActual.Empleado = false;
                                ReciboActual.Reparto = false;
                                ReciboActual.Pagada = false;
                                ReciboActual.UsrCreo = usuarioId;
                                ReciboActual.Correlativo = Id;
                                ReciboActual.Fecha = DateTime.Today;
                                ReciboActual.FechaHoraRecibo = DateTime.Now;
                                ReciboActual.Credito = false;
                                ReciboActual.Despachado = false;
                                ReciboActual.ComentarioPedido = PedidoActual.Descripcion;

                                ReciboActual.Detalles = new List<ReciboDetalle>();

                                if (PedidoActual.Detalles != null && PedidoActual.Detalles.Count() > 0)
                                {
                                    int DetalleId = 1;
                                    foreach (var Producto in PedidoActual.Detalles)
                                    {
                                        ReciboDetalle DetalleActual = new ReciboDetalle();
                                        DetalleActual.DetalleId = DetalleId;
                                        DetalleActual.ReciboId = ReciboActual.ReciboId;
                                        DetalleActual.ProductoId = Producto.ProductoId;
                                        DetalleActual.UnidadId = Producto.UnidadId;
                                        DetalleActual.Nombre = Producto.Nombre;
                                        DetalleActual.Descuento = Producto.Descuento;
                                        DetalleActual.Cantidad = Producto.Cantidad;
                                        DetalleActual.PrecioCosto = Producto.PrecioCosto;
                                        DetalleActual.Precio = Producto.Precio;

                                        //Se obtiene el producto para convercion
                                        Producto ProductoPadreActual = new Producto();
                                        Producto ProductoHijoActual = new Producto();

                                        bool UnidadPadre = false;
                                        decimal Cantidad = Producto.Cantidad;
                                        decimal CantidadOriginal = 0;

                                        decimal KardexPrecio = Producto.Precio;
                                        decimal KardexExistenciaActual = 0;
                                        decimal KardexExistenciaFinal = 0;

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

                                        ProductoInventario InventarioActual = db.Set<ProductoInventario>().Where(x => x.ProductoId == Producto.ProductoId && x.AgenciaId == ReciboActual.AgenciaId).FirstOrDefault();
                                        if (InventarioActual != null)
                                        {
                                            if (Cantidad > InventarioActual.Cantidad)
                                            {
                                                return string.Format("Se le informa que el producto con ID: {0} no cuenta con existencia", Producto.ProductoId);
                                            }

                                            if (InventarioActual.Cantidad > 0)
                                            {
                                                KardexExistenciaActual = InventarioActual.Cantidad;
                                                KardexExistenciaFinal = InventarioActual.Cantidad - Cantidad;

                                                InventarioActual.Cantidad -= Cantidad;
                                            }
                                            else
                                            {
                                                return string.Format("Se le informa que el producto con ID: {0} no cuenta con existencia", Producto.ProductoId);
                                            }                                            
                                        }

                                        //Se agrega la informacion al Kardex
                                        db.Set<KardexMovimiento>().Add(new KardexMovimiento() { AgenciaId = ReciboActual.AgenciaId, TipoId = 3, Fecha = DateTime.Today, FechaHora = DateTime.Now, ProductoId = Producto.ProductoId, UnidadId = Producto.UnidadId, DocumentoId = ReciboActual.ReciboId, Cantidad = Producto.Cantidad, Precio = KardexPrecio, ExistenciaActual = KardexExistenciaActual, ExistenciaFinal = KardexExistenciaFinal, ResponsableId = ReciboActual.UsrCreo });

                                        DetalleId += 1;
                                        ReciboActual.Detalles.Add(DetalleActual);
                                    }
                                }

                                PedidoActual.Operada = true;
                                PedidoActual.FechaHoraOpero = DateTime.Now;
                                PedidoActual.UsrOpero = usuarioId;

                                db.Set<Recibo>().Add(ReciboActual);
                                db.SaveChanges();

                                Mensaje = string.Format("OK;{0}", ReciboActual.ReciboId);
                            }
                        }                        
                    }
                    else
                    {
                        return "El pedido que ingreso no se encuentra registrado en el sistema";
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }    

                return Mensaje;
            }

            public string Convertir(long id)
            {
                string Mensaje = "OK";

                try
                {
                    Pedido PedidoActual = db.Set<Pedido>().Where(x => x.PedidoId == id).FirstOrDefault();
                    if (PedidoActual != null)
                    {
                        PedidoActual.Cotizacion = false;
                        db.SaveChanges();
                    }
                    else
                    {
                        Mensaje = "La cotización no se encuentra disponible";
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            public string Revivir(long id)
            {
                string Mensaje = "OK";

                try
                {
                    Pedido PedidoActual = db.Set<Pedido>().Where(x => x.PedidoId == id).FirstOrDefault();
                    if (PedidoActual != null)
                    {
                        PedidoActual.Operada = false;
                        PedidoActual.UsrOpero = null;
                        PedidoActual.FechaHoraOpero = null;
                        db.SaveChanges();
                    }
                    else
                    {
                        Mensaje = "El pedido no se encuentra disponible";
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            public Pedido ObtenerPorId(long id, bool todo = false, bool incluirDescuento = false)
            {
                Pedido PedidoActual = new Pedido();

                try
                {
                    if (todo)
                    {
                        PedidoActual = db.Set<Pedido>().Include("Agencia").Include("Cliente").Include("Detalles").Include("Detalles.Producto").Include("Detalles.Unidad").Include("UsuarioCreo").Where(x => x.PedidoId == id).FirstOrDefault();
                    }
                    else
                    {
                        PedidoActual = db.Set<Pedido>().Where(x => x.PedidoId == id).FirstOrDefault();
                    }

                    if (incluirDescuento)
                    {
                        if (PedidoActual != null)
                        {
                            if (PedidoActual.Detalles != null && PedidoActual.Detalles.Count() > 0)
                            {
                                foreach (PedidoDetalle Detalle in PedidoActual.Detalles)
                                {
                                    Detalle.Precio = Detalle.Precio + (Detalle.Descuento == null ? 0 : Detalle.Descuento.Value);                                 
                                }                                
                            }                            
                        }            
                    }
                }
                catch (Exception)
                {
                }

                return PedidoActual;
            }

            public List<Pedido> ObtenerListadoPorFecha(DateTime fechaInicial, DateTime fechaFinal, long agenciaId)
            {
                List<Pedido> Pedidos = new List<Pedido>();

                try
                {
                    Pedidos = db.Set<Pedido>().Include("Agencia").Include("Cliente").Include("UsuarioCreo").Include("Detalles").AsNoTracking().Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && x.AgenciaId == agenciaId).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.PedidoId).ToList();
                }
                catch (Exception)
                {}

                return Pedidos;
            }

            public List<Pedido> Buscar(string search, long agenciaId)
            {
                List<Pedido> Pedidos = new List<Pedido>();
                long PedidoId = 0;

                try
                {
                    long.TryParse(search, out PedidoId);

                    if (PedidoId > 0)
                    {
                        Pedidos = db.Set<Pedido>().Include("Agencia").Include("Cliente").Include("UsuarioCreo").Include("Detalles").AsNoTracking().Where(x => x.PedidoId == PedidoId && x.AgenciaId == agenciaId).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.PedidoId).ToList();
                    }
                    else
                    {
                        Pedidos = db.Set<Pedido>().Include("Agencia").Include("Cliente").Include("UsuarioCreo").Include("Detalles").AsNoTracking().Where(x => (x.Agencia.Nombre.ToLower().Contains(search.ToLower()) || x.Cliente.Nombre.ToLower().Contains(search.ToLower())) && x.AgenciaId == agenciaId).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.PedidoId).ToList();
                    }
                }
                catch (Exception)
                {}

                return Pedidos;
            }

            public List<Pedido> ObtenerListadoSinOperar(long agenciaId)
            {
                List<Pedido> Pedidos = new List<Pedido>();

                try
                {
                    Pedidos = db.Set<Pedido>().Include("Agencia").Include("Cliente").Include("UsuarioCreo").Include("Detalles").AsNoTracking().Where(x => x.AgenciaId == agenciaId && !x.Operada && !x.Anulada).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.PedidoId).ToList();
                }
                catch (Exception)
                {
                }

                return Pedidos;
            }

            public List<Pedido> ObtenerListadoSinOperarxAgencia(long agenciaId)
            {
                List<Pedido> Pedidos = new List<Pedido>();

                try
                {
                    Pedidos = db.Set<Pedido>().Include("Cliente").Include("UsuarioCreo").AsNoTracking().Where(x => x.AgenciaId == agenciaId && !x.Operada && !x.Anulada && !x.Cotizacion).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.PedidoId).ToList();
                    if (Pedidos != null && Pedidos.Count() > 0)
                    {
                        Pedidos.ForEach(x => 
                        {
                            x.Nombre = string.Format("{0} --- {1} - {2} - {3} - {4}", x.Correlativo, x.PedidoId, x.Cliente == null ? "No Disponible" : x.Cliente.Nombre, x.UsuarioCreo == null ? "No Disponible" : x.UsuarioCreo.Nombre, x.FechaHoraCreacion == null ? "00:00" : x.FechaHoraCreacion.Value.ToString("hh:mm tt"));
                        });                        
                    }
                }
                catch (Exception)
                {
                }

                return Pedidos;
            }

            public MensajePedido ObtenerPedido(long pedidoId) 
            {
                MensajePedido PedidoActual = new MensajePedido();
                string Productos = string.Empty;

                try
                {
                    Pedido Pedido = db.Set<Pedido>().Include("Detalles").Include("Detalles.Producto").Include("Detalles.Unidad").AsNoTracking().Where(x => x.PedidoId == pedidoId).FirstOrDefault();
                    if (Pedido != null)
                    {
                        if (Pedido.Operada)
                        {
                            PedidoActual.MensajeId = -2;
                            PedidoActual.Mensaje = "El pedido ya se encuentra operado";
                        }
                        else
                        {
                            //Se asigna al cliente del pedido
                            PedidoActual.ClienteId = Pedido.ClienteId;

                            //Se agrega el vendedor que viene del pedido
                            PedidoActual.VendedorId = 0;
                            Vendedor VendedorActual = db.Set<Usuario>().AsNoTracking().Where(x => x.UsuarioId == Pedido.UsrCreo).Join(db.Set<Vendedor>().AsNoTracking(), U => U.VendedorId, V => V.VendedorId, (U, V) => new  { V }).Select(x => x.V).FirstOrDefault();
                            if (VendedorActual != null)
                            {
                                PedidoActual.VendedorId = VendedorActual.VendedorId;                                
                            }

                            if (Pedido.Detalles != null && Pedido.Detalles.Count() > 0)
                            {
                                bool PedidoIncompleto = false;

                                foreach (var Producto in Pedido.Detalles)
                                {                                    
                                    //Se obtiene el producto para convercion
                                    Producto ProductoPadreActual = new Producto();
                                    Producto ProductoHijoActual = new Producto();

                                    bool UnidadPadre = false;
                                    decimal Cantidad = Producto.Cantidad;
                                    decimal CantidadOriginal = 0;

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

                                    ProductoInventario InventarioActual = db.Set<ProductoInventario>().Where(x => x.ProductoId == Producto.ProductoId && x.AgenciaId == Pedido.AgenciaId).FirstOrDefault();
                                    if (InventarioActual != null)
                                    {
                                        if (InventarioActual.Cantidad >= Cantidad)
                                        {
                                            string NombreProducto = string.IsNullOrWhiteSpace(Producto.Nombre) ? Producto.Producto.Nombre : Producto.Nombre;

                                            Productos += string.Format("<tr><td>{0}</td><td>{1}</td><td>{2}</td><td></td><td>{3}</td><td>{4}</td><td>{5}</td><td>{6}</td></tr>", NombreProducto, Producto.Unidad.Nombre, Producto.Cantidad, Producto.Precio.ToString("C"), 0, (Producto.Cantidad * Producto.Precio).ToString("C"), "<input type='hidden' name='productoIds' value='" + Producto.ProductoId + "' /><input type='hidden' name='nombreProductoIds' value='" + NombreProducto + "' /><input type='hidden' name='presentacionIds' value='" + Producto.UnidadId + "' /><input type='hidden' name='nombrePresentacionIds' value='" + Producto.Unidad.Nombre + "' /><input type='hidden' name='existenciaIds' value='" + Cantidad + "' /><input type='hidden' name='cantidadIds' value='" + Producto.Cantidad + "' /><input type='hidden' name='precioIds' value='" + Producto.Precio + "' /><input type='hidden' name='descuentoIds' value='0' /><input type='hidden' name='dIds' value='' />");
                                        }
                                        else
                                        {
                                            PedidoIncompleto = true;
                                            break;
                                        }
                                    }                                  
                                }

                                if (PedidoIncompleto)
                                {
                                    PedidoActual.MensajeId = -4;
                                    PedidoActual.Mensaje = "El pedido no contiene suficientes existencias para ser despachado";
                                }
                                else
                                {
                                    PedidoActual.MensajeId = -5;
                                    PedidoActual.Mensaje = Productos;
                                }
                            }
                            else
                            {
                                PedidoActual.MensajeId = -3;
                                PedidoActual.Mensaje = "El pedido no contiene productos";
                            }
                        }
                    }
                    else
                    {
                        PedidoActual.MensajeId = -1;
                        PedidoActual.Mensaje = "El pedido no se encuentra registrado en el sistema";
                    }
                }
                catch (Exception)
                {
                }

                return PedidoActual;
            }

            public string Anular(long pedidoId, string comentario, long usuarioId)
            {
                string Mensaje = "OK";

                try
                {

                    Pedido PedidoActual = db.Set<Pedido>().Where(x => x.PedidoId == pedidoId).FirstOrDefault();
                    if (PedidoActual == null)
                    {
                        return "El pedido que selecciono no se encuentra disponible";
                    }

                    PedidoActual.Comentario = comentario;
                    PedidoActual.Anulada = true;
                    PedidoActual.UsrAnular = usuarioId;
                    PedidoActual.FechaAnular = DateTime.Now;

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
