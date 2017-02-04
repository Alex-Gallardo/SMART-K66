using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiamDev.Give.Entities;

namespace DiamDev.Give.DAL
{
    public class GiveContext : DbContext
    {
        public DbSet<Agencia> Agencias { get; set; }
        
        public DbSet<Permiso> Permisos { get; set; }

        public DbSet<Menu> Menus { get; set; }

        public DbSet<Rol> Roles { get; set; }

        public DbSet<RolPermiso> RolPermisos { get; set; }

        public DbSet<Usuario> Usuarios { get; set; }

        public DbSet<UsuarioRol> UsuarioRoles { get; set; }

        public DbSet<UsuarioAgencia> UsuarioAgencias { get; set; }

        public DbSet<FormaPago> FormaPagos { get; set; }

        public DbSet<Configuracion> Configuracions { get; set; }

        public DbSet<Unidad> Unidades { get; set; }

        public DbSet<Marca> Marcas { get; set; }

        public DbSet<ProductoCategoria> ProductoCategorias { get; set; }

        public DbSet<Producto> Productos { get; set; }

        public DbSet<ProductoInventario> ProductoInventarios { get; set; }

        public DbSet<ProductoFotografia> ProductoFotografias { get; set; }
        
        public DbSet<Precio> Precios { get; set; }

        public DbSet<ProductoPrecioCosto> ProductoPrecioCostos { get; set; }

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

        public DbSet<Factura> Facturas { get; set; }

        public DbSet<FacturaDetalle> FacturaDetalles { get; set; }

        public DbSet<FacturaFormaPago> FacturaFormaPagos { get; set; }

        public DbSet<Vendedor> Vendedors { get; set; }

        public DbSet<VendedorAgencia> VendedorAgencias { get; set; }

        public DbSet<Serie> Series { get; set; }

        public DbSet<SerieAgencia> SerieAgencias { get; set; }

        public DbSet<SerieAgenciaFactura> SerieAgenciaFacturas { get; set; }

        public DbSet<Personal> Personals { get; set; }

        public DbSet<PersonalHorario> PersonalHorarios { get; set; }
        
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MovimientoDetalle>().HasRequired(u => u.Unidad).WithMany().HasForeignKey(e => e.UnidadId).WillCascadeOnDelete(false);           
            modelBuilder.Entity<Factura>().HasRequired(u => u.UsuarioCreo).WithMany().HasForeignKey(e => e.UsrCreo).WillCascadeOnDelete(false);
            modelBuilder.Entity<FacturaDetalle>().HasRequired(u => u.Unidad).WithMany().HasForeignKey(e => e.UnidadId).WillCascadeOnDelete(false);  
            modelBuilder.Entity<Traslado>().HasRequired(u => u.AgenciaOrigen).WithMany().HasForeignKey(e => e.AgenciaOrigenId).WillCascadeOnDelete(false);
            modelBuilder.Entity<TrasladoDetalle>().HasRequired(u => u.Unidad).WithMany().HasForeignKey(e => e.UnidadId).WillCascadeOnDelete(false);
            //modelBuilder.Entity<NotaCredito>().HasRequired(u => u.UsuarioCreo).WithMany().HasForeignKey(e => e.UsrCreo).WillCascadeOnDelete(false);
        }
    }
}
