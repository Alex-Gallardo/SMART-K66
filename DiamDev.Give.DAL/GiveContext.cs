using System.Data.Entity;
using DiamDev.Give.Entities;

namespace DiamDev.Give.DAL
{
    public class GiveContext : DbContext
    {

        // Constructor por defecto: usa "GiveContext" (producción, toda la app).
        public GiveContext() : base("name=GiveContext") { }

        // Constructor para apuntar a otra BD (recibos lo llama con "RecibosContext").
        public GiveContext(string nombreConnectionString) : base("name=" + nombreConnectionString) { }

        public DbSet<Agencia> Agencias { get; set; }

        public DbSet<OfertaDelivery> OfertasDelivery { get; set; }

        public DbSet<Municipio> Municipios { get; set; }

        public DbSet<Localidad> Localidades { get; set; }

        public DbSet<DireccionCliente> DireccionesCliente { get; set; }

        public DbSet<Permiso> Permisos { get; set; }

        public DbSet<Menu> Menus { get; set; }

        public DbSet<Rol> Roles { get; set; }

        public DbSet<RolPermiso> RolPermisos { get; set; }

        public DbSet<Usuario> Usuarios { get; set; }

        public DbSet<UsuarioRol> UsuarioRoles { get; set; }

        public DbSet<UsuarioAgencia> UsuarioAgencias { get; set; }

        public DbSet<UsuarioAgenciaConsulta> UsuarioAgenciaConsultas { get; set; }
        
        public DbSet<FormaPago> FormaPagos { get; set; }

        public DbSet<Configuracion> Configuracions { get; set; }

        public DbSet<Unidad> Unidades { get; set; }

        public DbSet<Marca> Marcas { get; set; }

        public DbSet<ProductoCategoria> ProductoCategorias { get; set; }

        public DbSet<Producto> Productos { get; set; }

        public DbSet<ProductoInventario> ProductoInventarios { get; set; }

        public DbSet<ProductoInventarioID> ProductoInventarioIDs { get; set; }
        
        public DbSet<ProductoFotografia> ProductoFotografias { get; set; }
        
        public DbSet<Precio> Precios { get; set; }

        public DbSet<ProductoPrecioCosto> ProductoPrecioCostos { get; set; }

        public DbSet<ClienteTipo> ClienteTipos { get; set; }
        
        public DbSet<Cliente> Clientes { get; set; }

        public DbSet<Proveedor> Proveedores { get; set; }

        public DbSet<ProveedorProducto> ProveedorProductos { get; set; }

        public DbSet<MovimientoTipo> MovimientoTipos { get; set; }

        public DbSet<Movimiento> Movimientos { get; set; }

        public DbSet<MovimientoDetalle> MovimientoDetalles { get; set; }

        public DbSet<MovimientoFormaPago> MovimientoFormaPagos { get; set; }

        public DbSet<Traslado> Traslados { get; set; }

        public DbSet<TrasladoDetalle> TrasladoDetalles { get; set; }

        public DbSet<CuentaContable> CuentaContables { get; set; }

        public DbSet<CuentaContableTipo> CuentaContableTipos { get; set; }

        public DbSet<Diario> Diarios { get; set; }

        public DbSet<DiarioAgencia> DiarioAgencias { get; set; }
        
        public DbSet<DiarioDetalle> DiarioDetalles { get; set; }

        public DbSet<FacturaTipo> FacturaTipos { get; set; }
        
        public DbSet<Factura> Facturas { get; set; }

        public DbSet<FacturaDetalle> FacturaDetalles { get; set; }

        public DbSet<FacturaFormaPago> FacturaFormaPagos { get; set; }

        public DbSet<Vendedor> Vendedors { get; set; }

        public DbSet<VendedorAgencia> VendedorAgencias { get; set; }

        public DbSet<VendedorEscala> VendedorEscalas { get; set; }
        
        public DbSet<Serie> Series { get; set; }

        public DbSet<SerieAgencia> SerieAgencias { get; set; }

        public DbSet<SerieAgenciaFactura> SerieAgenciaFacturas { get; set; }

        public DbSet<Personal> Personals { get; set; }

        public DbSet<PersonalHorario> PersonalHorarios { get; set; }

        public DbSet<RegistroKardex> RegistrosKardex { get; set; }

        public DbSet<NotaCredito> NotaCreditos { get; set; }

        public DbSet<NotaCreditoFormaPago> Pagos { get; set; }

        public DbSet<MovimientoCategoria> MovimientoCategorias { get; set; }

        public DbSet<CreditoTipo> CreditoTipos { get; set; }

        public DbSet<Credito> Creditos { get; set; }

        public DbSet<CreditoDetalle> CreditoDetalles { get; set; }

        public DbSet<CreditoAnotacion> CreditoAnotacions { get; set; }

        public DbSet<CreditoPago> CreditoPagos { get; set; }

        public DbSet<Laboratorio> Laboratorios { get; set; }

        public DbSet<Pedido> Pedidos { get; set; }

        public DbSet<PedidoDetalle> PedidoDetalles { get; set; }

        public DbSet<UnidadOperacion> UnidadOperacions { get; set; }

        public DbSet<UnidadConversion> UnidadConversions { get; set; }

        public DbSet<Gasto> Gastos { get; set; }

        public DbSet<GastoFotografia> GastoFotografias { get; set; }

        public DbSet<ServicioClienteTipo> ServicioClienteTipos { get; set; }

        public DbSet<ServicioCliente> ServicioClientes { get; set; }

        public DbSet<ClienteFotografia> ClienteFotografias { get; set; }

        public DbSet<ProveedorMovimientoTipo> ProveedorMovimientoTipos { get; set; }

        public DbSet<ProveedorMovimiento> ProveedorMovimientos { get; set; }

        public DbSet<ProveedorMovimientoFotografia> ProveedorMovimientoFotografias { get; set; }

        public DbSet<Banco> Bancos { get; set; }

        public DbSet<ProveedorCuentaBancaria> ProveedorCuentaBancarias { get; set; }

        public DbSet<MovimientoEstado> MovimientoEstados { get; set; }

        public DbSet<Transporte> Transportes { get; set; }

        public DbSet<Departamento> Departamentos { get; set; }
        
        public DbSet<Servicio> Servicios { get; set; }

        public DbSet<ReparacionEstado> ReparacionEstados { get; set; }

        public DbSet<ReparacionTipo> ReparacionTipos { get; set; }

        public DbSet<ReparacionServicio> ReparacionServicios { get; set; }

        public DbSet<ReparacionPieza> ReparacionPiezas { get; set; }

        public DbSet<ReparacionAnotacion> ReparacionAnotacions { get; set; }

        public DbSet<ReparacionFotografia> ReparacionFotografias { get; set; }

        public DbSet<ReparacionFormaPago> ReparacionFormaPagos { get; set; }

        public DbSet<ReparacionPoliticaCategoria> ReparacionPoliticaCategorias { get; set; }
        
        public DbSet<Reparacion> Reparacions { get; set; }

        public DbSet<PoliticaTipo> PoliticaTipos { get; set; }

        public DbSet<Politica> Politicas { get; set; }

        public DbSet<PoliticaCategoria> PoliticaCategorias { get; set; }

        public DbSet<PoliticaCategoriaPolitica> PoliticaCategoriaPoliticas { get; set; }

        public DbSet<Reserva> Reservas { get; set; }

        public DbSet<ReservaDetalle> ReservaDetalles { get; set; }

        public DbSet<ReservaPago> ReservaPagos { get; set; }

        public DbSet<ReciboTipo> ReciboTipos { get; set; }

        public DbSet<Recibo> Recibos { get; set; }

        public DbSet<ReciboDetalle> ReciboDetalles { get; set; }

        public DbSet<ReciboFormaPago> ReciboFormaPagos { get; set; }

        public DbSet<GarantiaDocumento> GarantiaDocumentos { get; set; }

        public DbSet<Garantia> Garantias { get; set; }

        public DbSet<GarantiaDetalle> GarantiaDetalles { get; set; }

        public DbSet<AnotacionTipo> AnotacionTipos { get; set; }

        public DbSet<Anotacion> Anotacions { get; set; }

        public DbSet<Puesto> Puestos { get; set; }

        public DbSet<NominaTipo> NominaTipos { get; set; }

        public DbSet<Nomina> Nominas { get; set; }

        public DbSet<NominaDetalle> NominaDetalles { get; set; }

        public DbSet<ProductoPrecioCostoHistorial> ProductoPrecioCostoHistorials { get; set; }

        public DbSet<CategoriaGasto> CategoriaGastos { get; set; }

        public DbSet<CorteCaja> CorteCajas { get; set; }

        public DbSet<Cierre> Cierres { get; set; }

        public DbSet<CierreDetalle> CierreDetalles { get; set; }

        public DbSet<ReciboEnvase> ReciboEnvases { get; set; }

        public DbSet<ReciboEnvaseDetalle> ReciboEnvaseDetalles { get; set; }

        public DbSet<ProductoNivelPrecio> ProductoNivelPrecios { get; set; }

        public DbSet<Egreso> Egresos { get; set; }

        public DbSet<EgresoDetalle> EgresoDetalles { get; set; }

        public DbSet<KardexMovimientoTipo> KardexMovimientoTipos { get; set; }

        public DbSet<KardexMovimiento> KardexMovimientos { get; set; }

        public DbSet<TrasladoDetalleDestino> TrasladoDetalleDestinos { get; set; }

        public DbSet<ContrasenaPago> ContrasenaPagos { get; set; }

        public DbSet<ProductoLote> ProductoLotes { get; set; }

        public DbSet<Region> Regions { get; set; }

        public DbSet<ReciboLote> ReciboLotes { get; set; }

        public DbSet<FacturaLote> FacturaLotes { get; set; }

        public DbSet<Mes> Mes { get; set; }

        public DbSet<VendedorMeta> VendedorMetas { get; set; }

        public DbSet<ProveedorTipo> ProveedorTipos { get; set; }

        public DbSet<TipoUbicacion> TipoUbicacions { get; set; }

        public DbSet<Mesa> Mesas { get; set; }

        public DbSet<MesaRecibo> MesaRecibos { get; set; }

        public DbSet<Token> Tokens { get; set; }

        public DbSet<ReciboDelivery> ReciboDeliverys { get; set; }

        public DbSet<FacturaNotaCredito> FacturaNotaCreditos { get; set; }

        public DbSet<FacturaNotaCreditoDetalle> FacturaNotaCreditoDetalles { get; set; }

        public DbSet<TipoCompra> TipoCompra { get; set; }

        public DbSet<Moneda> Moneda { get; set; }

        public DbSet<ClienteContacto> ClienteContacto { get; set; }

        public DbSet<OrdenCompra> OrdenCompra { get; set; }

        public DbSet<OrdenCompraDetalle> OrdenCompraDetalle { get; set; }

        public DbSet<Empresa> Empresa { get; set; }

        public DbSet<Paquete> Paquete { get; set; }

        public DbSet<PaqueteEmpresa> PaqueteEmpresa { get; set; }

        public DbSet<VisitaTipo> VisitaTipo { get; set; }

        public DbSet<Visita> Visita { get; set; }

        public DbSet<PedidoK66> PedidoK66 { get; set; }

        public DbSet<PedidoDetalleK66> PedidoDetalleK66 { get; set; }

        public DbSet<ProductoAlertaK66> ProductoAlertaK66 { get; set; }

        public DbSet<DescuentoK66> DescuentoK66 { get; set; }

        public DbSet<PedidoTipoK66> PedidoTipoK66 { get; set; }

        public DbSet<EstadoSmartK66> EstadoSmartK66 { get; set; }

        public DbSet<UsuarioEmpresa> UsuarioEmpresas { get; set; }
        // ── NUEVO: mapeo usuario POS → DEPTO de serie de recibos ──
        public DbSet<RecibosCajaUsuarioDepto> RecibosCajaUsuarioDeptos { get; set; }


        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MovimientoDetalle>().HasRequired(u => u.Unidad).WithMany().HasForeignKey(e => e.UnidadId).WillCascadeOnDelete(false);           
            modelBuilder.Entity<Factura>().HasRequired(u => u.UsuarioCreo).WithMany().HasForeignKey(e => e.UsrCreo).WillCascadeOnDelete(false);
            modelBuilder.Entity<FacturaDetalle>().HasRequired(u => u.Unidad).WithMany().HasForeignKey(e => e.UnidadId).WillCascadeOnDelete(false);  
            modelBuilder.Entity<Traslado>().HasRequired(u => u.AgenciaOrigen).WithMany().HasForeignKey(e => e.AgenciaOrigenId).WillCascadeOnDelete(false);
            modelBuilder.Entity<TrasladoDetalle>().HasRequired(u => u.Unidad).WithMany().HasForeignKey(e => e.UnidadId).WillCascadeOnDelete(false);
            modelBuilder.Entity<Movimiento>().HasRequired(u => u.UsuarioCreo).WithMany().HasForeignKey(e => e.UsrCreo).WillCascadeOnDelete(false);
            modelBuilder.Entity<NotaCredito>().HasRequired(u => u.UsuarioCreo).WithMany().HasForeignKey(e => e.UsrCreo).WillCascadeOnDelete(false);
            modelBuilder.Entity<Credito>().HasRequired(u => u.UsuarioInicial).WithMany().HasForeignKey(e => e.UsrInicial).WillCascadeOnDelete(false);
            modelBuilder.Entity<Laboratorio>().HasRequired(u => u.ProductoBase).WithMany().HasForeignKey(e => e.ProductoBaseId).WillCascadeOnDelete(false);
            modelBuilder.Entity<Pedido>().HasRequired(u => u.UsuarioCreo).WithMany().HasForeignKey(e => e.UsrCreo).WillCascadeOnDelete(false);
            modelBuilder.Entity<UnidadConversion>().HasRequired(u => u.UnidadBase).WithMany().HasForeignKey(e => e.UnidadBaseId).WillCascadeOnDelete(false);
            modelBuilder.Entity<ProveedorMovimiento>().HasRequired(u => u.UsuarioCreo).WithMany().HasForeignKey(e => e.UsrCreo).WillCascadeOnDelete(false);
            modelBuilder.Entity<Reparacion>().HasRequired(u => u.UsuarioCreo).WithMany().HasForeignKey(e => e.UsrCreo).WillCascadeOnDelete(false);
            modelBuilder.Entity<PoliticaCategoria>().HasRequired(u => u.Tipo).WithMany().HasForeignKey(e => e.TipoId).WillCascadeOnDelete(false);
            modelBuilder.Entity<Reserva>().HasRequired(u => u.UsuarioCreo).WithMany().HasForeignKey(e => e.UsrCreo).WillCascadeOnDelete(false);
            modelBuilder.Entity<Recibo>().HasRequired(u => u.UsuarioCreo).WithMany().HasForeignKey(e => e.UsrCreo).WillCascadeOnDelete(false);
            modelBuilder.Entity<ReciboDetalle>().HasRequired(u => u.Unidad).WithMany().HasForeignKey(e => e.UnidadId).WillCascadeOnDelete(false);
            modelBuilder.Entity<Garantia>().HasRequired(u => u.UsuarioCreo).WithMany().HasForeignKey(e => e.UsrCreo).WillCascadeOnDelete(false);
            modelBuilder.Entity<ReciboEnvase>().HasRequired(u => u.UsuarioCreo).WithMany().HasForeignKey(e => e.UsrCreo).WillCascadeOnDelete(false);
            modelBuilder.Entity<ReciboEnvaseDetalle>().HasRequired(u => u.Unidad).WithMany().HasForeignKey(e => e.UnidadId).WillCascadeOnDelete(false);
            modelBuilder.Entity<ReciboEnvase>().HasRequired(u => u.Recibo).WithMany().HasForeignKey(e => e.ReciboId).WillCascadeOnDelete(false);
         
            //Precision de Decimales
            modelBuilder.Entity<EgresoDetalle>().Property(x => x.PrecioCosto).HasPrecision(18, 4);
            modelBuilder.Entity<ProductoPrecio>().Property(x => x.Valor).HasPrecision(18, 4);
            modelBuilder.Entity<ProductoNivelPrecio>().Property(x => x.Precio).HasPrecision(18, 4);
            modelBuilder.Entity<ReciboDetalle>().Property(x => x.Descuento).HasPrecision(18, 4);
            modelBuilder.Entity<ReciboDetalle>().Property(x => x.PrecioCosto).HasPrecision(18, 4);
            modelBuilder.Entity<ReciboDetalle>().Property(x => x.Precio).HasPrecision(18, 4);
            modelBuilder.Entity<ReciboFormaPago>().Property(x => x.Valor).HasPrecision(18, 4);
            modelBuilder.Entity<CierreDetalle>().Property(x => x.MontoSistema).HasPrecision(18, 4);
            modelBuilder.Entity<CierreDetalle>().Property(x => x.MontoCajero).HasPrecision(18, 4);
            modelBuilder.Entity<CorteCaja>().Property(x => x.Monto).HasPrecision(18, 4);
            modelBuilder.Entity<CorteCaja>().Property(x => x.Gasto).HasPrecision(18, 4);
            modelBuilder.Entity<PedidoDetalle>().Property(x => x.PrecioCosto).HasPrecision(18, 4);
            modelBuilder.Entity<PedidoDetalle>().Property(x => x.Precio).HasPrecision(18, 4);

            modelBuilder.Entity<MovimientoDetalle>().Property(x => x.PrecioCosto).HasPrecision(18, 4);
            modelBuilder.Entity<MovimientoDetalle>().Property(x => x.Precio).HasPrecision(18, 4);

            modelBuilder.Entity<KardexMovimiento>().Property(x => x.Precio).HasPrecision(18, 4);

            modelBuilder.Entity<DescuentoK66>().Property(x => x.Descuento).HasPrecision(18, 4);

            modelBuilder.Entity<ProductoPrecioCostoHistorial>().Property(x => x.PrecioCostoActual).HasPrecision(18, 4);
            modelBuilder.Entity<ProductoPrecioCostoHistorial>().Property(x => x.PrecioCostoNuevo).HasPrecision(18, 4);
            modelBuilder.Entity<ProductoPrecioCostoHistorial>().Property(x => x.PrecioCostoPromedio).HasPrecision(18, 4);

            modelBuilder.Entity<ProductoPrecioCosto>().Property(x => x.PrecioCosto).HasPrecision(18, 4);

            modelBuilder.Entity<VendedorEscala>().Property(x => x.Porcentaje).HasPrecision(18, 5);
        }       
    }
}
