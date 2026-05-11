using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using DiamDev.Give.BLL;
using DiamDev.Give.Entities;
using DiamDev.Give.UI.App_Start;
using DiamDev.Give.UI.Models;
using PagedList;

namespace DiamDev.Give.UI.Controllers
{
    [Authorize]
    [Seguridad]
    [HandleError]
    public class ReporteController : Controller
    {

        #region Metodos Privados

        private void CargaControles(bool pCargaCentroxUsuario, bool centroGeneral = true)
        {
            var Centros = new AgenciaBL().ObtenerListado(pCargaCentroxUsuario, CustomHelper.getUserId());

            if (centroGeneral)
            {
                if (Centros != null && Centros.Count() > 0)
                {
                    Centros.Insert(0, new Agencia() { AgenciaId = 0, Nombre = "General" });
                }
            }         

            ViewBag.Centros = new SelectList(Centros, "AgenciaId", "Nombre");
        }

        private void CargaPrecios()
        {
            var Precios = new PrecioBL().ObtenerListado();
            ViewBag.Precios = new SelectList(Precios, "PrecioId", "Nombre");
        }

        private void CargaProveedores()
        {
            var Proveedores = new ProveedorBL().ObtenerListado(false);
            ViewBag.Proveedores = new SelectList(Proveedores, "ProveedorId", "Nombre");
        }

        private void CargaProductos()
        {
            var Productos = new ProductoBL().ObtenerListado(true, false, true);
            ViewBag.Productos = new SelectList(Productos, "ProductoId", "Nombre");
        }

        private void CargaMarcas()
        {
            var Marcas = new MarcaBL().ObtenerListado(false);
            ViewBag.Marcas = new SelectList(Marcas, "MarcaId", "Nombre");
        }

        private void CargarPersonal()
        {
            var Personal = new PersonalBL().ObtenerListado(false);
            ViewBag.Personals = new SelectList(Personal, "PersonalId", "Nombre");
        }

        private void CargarProductoCategorias()
        {
            var Categorias = new ProductoCategoriaBL().ObtenerListado(false);
            ViewBag.Categorias = new SelectList(Categorias, "ProductoCategoriaId", "Nombre");
        }

        private void cargaVendedores() 
        {
            var Vendedores = new VendedorBL().ObtenerVendedoresPorAgencia(CustomHelper.getAgenciaId());
            ViewBag.Vendedores = new SelectList(Vendedores, "VendedorId", "Nombre");    
        }

        private void cargaUsuarios()
        {
            var Usuarios = new UsuarioBL().ObtenerUsuarioxAgenciaId(CustomHelper.getAgenciaId());
            ViewBag.Usuarios = new SelectList(Usuarios, "UsuarioId", "Nombre");
        }

        private void cargaTransportes()
        {
            var Transportes = new TransporteBL().ObtenerListado();
            ViewBag.Transportes = new SelectList(Transportes, "TransporteId", "Nombre");
        }

        private void cargaProductosIDs()
        {
            var Productos = new ProductoBL().ObtenerProductosConIDs();
            ViewBag.Productos = new SelectList(Productos, "ProductoId", "Nombre");
        }

        private void cargaTiposDeClientes()
        {
            var Tipos = new ClienteTipoBL().ObtenerListado();
            ViewBag.Tipos = new SelectList(Tipos, "TipoId", "Nombre");
        }

        private void cargaTecnicos()
        {
            var Usuarios = new UsuarioBL().ObtenerTecnicos();
            ViewBag.Usuarios = new SelectList(Usuarios, "UsuarioId", "Nombre");
        }

        private void cargaFormas()
        {
            var Formas = new FormaPagoBL().ObtenerListado(false);
            ViewBag.Formas = new SelectList(Formas, "FormaPagoId", "Nombre");
        }

        private void cargaEstadosReserva()
        {
            var Estados = new List<ComboModel>() { new ComboModel() { ID = 1, Nombre = "Sí" }, new ComboModel() { ID = 2, Nombre = "No" } };
            ViewBag.Estados = new SelectList(Estados, "ID", "Nombre");
        }

        private void cargaCategoriaGastos()
        {
            var Categorias = new CategoriaGastoBL().ObtenerListado(false);
            ViewBag.Categorias = new SelectList(Categorias, "CategoriaId", "Nombre");
        }

        #endregion

        // GET: Reporte
        [Permiso("Control.Reporte.Inventario")]
        public ActionResult Inventario()
        {
            CustomHelper.setTitle("Inventario", "Reporte");

            this.CargaControles(true);
            return View();
        }


        [Permiso("Control.Reporte.kpidel")]
        public ActionResult KpiDelivery()
        {
            CustomHelper.setTitle("KPI Entrega", "Reporte");

            this.CargaControles(true);
            return View();
        }
        [Permiso("Control.Reporte.cxchist")]
        public ActionResult CuentasPorCobrarHistorico()
        {
            CustomHelper.setTitle("Cuentas Por Cobrar Historico", "Reporte");

            this.CargaControles(true);
            return View();
        }
        [Permiso("Control.Reporte.ventasdesp")]
        public ActionResult VentasDespachadas()
        {
            CustomHelper.setTitle("Venta Despachada", "Reporte");

            this.CargaControles(true);
            return View();
        }
        [Permiso("Control.Reporte.recanula")]
        public ActionResult RecibosAnulados()
        {
            CustomHelper.setTitle("Recibos Anulados", "Reporte");

            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.CompVend")]
        public ActionResult VendedoresComparativa()
        {
            CustomHelper.setTitle("Comparativa Vendedores", "Reporte");

            this.CargaControles(true);
            return View();
        }
        [Permiso("Control.Reporte.TransRep")]
        public ActionResult TransporteConsolidado()
        {
            CustomHelper.setTitle("Transporte Consolidado", "Reporte");

            this.CargaControles(true);
            return View();
        }
        [Permiso("Control.Reporte.Ventacom")]
        public ActionResult ComparativaSucursal()
        {
            CustomHelper.setTitle("Comparativa Sucursal", "Reporte");

            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.ProdTop")]
        public ActionResult ProductosTop()
        {
            CustomHelper.setTitle("Ventas Por Producto", "Reporte");

            this.CargaControles(true);
            return View();
        }



        [Permiso("Control.Reporte.topcli")]
        public ActionResult TopClientes()
        {
            CustomHelper.setTitle("Top Clientes", "Reporte");

            
            return View();
        }


        [Permiso("Control.Reporte.Cierre")]
        public ActionResult Cierre()
        {
            CustomHelper.setTitle("Cierre del Día", "Reporte");

            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.Cierre")]
        public ActionResult CierrexUsuario()
        {
            CustomHelper.setTitle("Cierre del Día x Usuario", "Reporte");

            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.Cierre")]
        public ActionResult CierrexUsuarioHora()
        {
            CustomHelper.setTitle("Cierre del Día x Usuario Hora", "Reporte");

            this.cargaUsuarios();
            return View();
        }

        [Permiso("Control.Reporte.Ingreso")]
        public ActionResult Ingreso()
        {
            CustomHelper.setTitle("Ingreso", "Reporte");

            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.IngresoxProveedor")]
        public ActionResult IngresoxProveedor()
        {
            CustomHelper.setTitle("Ingreso x Proveedor", "Reporte");

            this.CargaControles(true);
            this.CargaProveedores();
            return View();
        }

        [Permiso("Control.Reporte.IngresoxProducto")]
        public ActionResult IngresoxProducto()
        {
            CustomHelper.setTitle("Ingreso x Producto", "Reporte");

            this.CargaControles(true);
            this.CargaProductos();
            return View();
        }

        [Permiso("Control.Reporte.Egreso")]
        public ActionResult Egreso()
        {
            CustomHelper.setTitle("Egreso", "Reporte");

            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.Ganancia")]
        public ActionResult Ganancia()
        {
            CustomHelper.setTitle("Ganancia", "Reporte");

            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.Ganancia")]
        public ActionResult Ganancia_Detalle()
        {
            CustomHelper.setTitle("Ganancia", "Reporte");

            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.Ganancia")]
        public ActionResult Ganancia_Consolidada()
        {
            CustomHelper.setTitle("Ganancia Consolidada", "Reporte");

            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.Ganancia")]
        public ActionResult Ganancia_Consolidada_x_Producto()
        {
            CustomHelper.setTitle("Ganancia Consolidada x Producto", "Reporte");

            this.CargaControles(true);
            this.CargaProductos();
            return View();
        }

        [Permiso("Control.Reporte.Diario")]
        public ActionResult Diario()
        {
            CustomHelper.setTitle("Libro Diario", "Reporte");

            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.Mayor")]
        public ActionResult Mayor()
        {
            CustomHelper.setTitle("Libro Mayor", "Reporte");

            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.BalanceSaldo")]
        public ActionResult Balance_Saldo()
        {
            CustomHelper.setTitle("Balance de Saldos", "Reporte");

            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.VentaxTienda")]
        public ActionResult VentaxTienda()
        {
            CustomHelper.setTitle("Venta x Tienda", "Reporte");

            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.VentaxTienda")]
        public ActionResult VentaxTiendaYMarca()
        {
            CustomHelper.setTitle("Venta x Tienda Y Marca", "Reporte");

            this.CargaMarcas();
            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.TomaFisicaxTienda")]
        public ActionResult TomaFisicaxTienda()
        {
            CustomHelper.setTitle("Toma Fisica de Inventario x Tienda", "Reporte");

            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.InventarioxTienda")]
        public ActionResult InventarioxTienda()
        {
            CustomHelper.setTitle("Inventario x Tienda", "Reporte");

            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.InventarioxTienda")]
        public ActionResult InventarioxTiendaYMarca()
        {
            CustomHelper.setTitle("Inventario x Tienda Y Marca", "Reporte");

            this.CargaMarcas();
            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.PedidoxTienda")]
        public ActionResult PedidoxTiendaYMarca()
        {
            CustomHelper.setTitle("Pedido x Tienda Y Marca", "Reporte");

            this.CargaMarcas();
            this.CargaControles(true);
            return View();
        }
        //se cambio el nombre en el Menu y en el encabezado 
        [Permiso("Control.Reporte.VentaResumenxTienda")]
        public ActionResult VentaResumenxTienda()
        {
            CustomHelper.setTitle("Resumen del Mes", "Reporte");

            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.CierreDiarioResumen")]
        public ActionResult CierreDiarioResumen()
        {
            CustomHelper.setTitle("Corte Diario", "Reporte");

            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.IngresoxTienda")]
        public ActionResult IngresoxTienda()
        {
            CustomHelper.setTitle("Ingreso x Tienda", "Reporte");
                       
            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.SalidaxTienda")]
        public ActionResult SalidaxTienda()
        {
            CustomHelper.setTitle("Salida x Tienda", "Reporte");

            this.CargaControles(true);
            return View();
        }

        public ActionResult Horario()
        {
            CustomHelper.setTitle("Horario Personal", "Reporte");

            this.CargaControles(true, false);
            this.CargarPersonal();
            return View();
        }

        public ActionResult Horario_General()
        {
            CustomHelper.setTitle("Horario General", "Reporte");

            this.CargaControles(true, false);
            return View();
        }

        [Permiso("Control.Reporte.LibroVenta")]
        public ActionResult LibroVenta()
        {
            CustomHelper.setTitle("Libro de Venta", "Reporte");

            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.ProductoControlado")]
        public ActionResult Producto_Controlado()
        {
            CustomHelper.setTitle("Producto Controlado", "Reporte");
           
            this.CargarProductoCategorias();
            return View();
        }

        [Permiso("Control.Reporte.ProductoMinimoCategoria")]
        public ActionResult Producto_Minimo_Categoria()
        {
            CustomHelper.setTitle("Producto Minimo x Categoria", "Reporte");

            this.CargaControles(true);
            this.CargarProductoCategorias();
            return View();
        }

        [Permiso("Control.Reporte.VentaComisionVendedor")]
        public ActionResult Venta_Comision_Vendedor()
        {
            CustomHelper.setTitle("Venta Comision x Vendedor", "Reporte");

            this.cargaVendedores();
            return View();
        }

        [Permiso("Control.Reporte.ProveedorProducto")]
        public ActionResult Proveedor_Producto()
        {
            CustomHelper.setTitle("Proveedor Producto", "Reporte");

            this.CargaProveedores();
            return View();
        }

        [Permiso("Control.Reporte.VentaTransporte")]
        public ActionResult Venta_Transporte()
        {
            CustomHelper.setTitle("Venta Transporte", "Reporte");

            this.cargaTransportes();
            return View();
        }

        [Permiso("Control.Reporte.Inventario")]
        public ActionResult Inventario_x_Tienda_Categoria()
        {
            CustomHelper.setTitle("Inventario x Tienda y Categoria", "Reporte");

            this.CargaControles(true);
            this.CargarProductoCategorias();
            return View();
        }

        [Permiso("Control.Reporte.Inventario")]
        public ActionResult Inventario_IDs_x_Tienda_Producto()
        {
            CustomHelper.setTitle("Inventario IDs x Tienda y Producto", "Reporte");

            this.CargaControles(true);
            this.cargaProductosIDs();
            return View();
        }

        [Permiso("Control.Reporte.VentaTransporte")]
        public ActionResult Cierre_Transporte()
        {
            CustomHelper.setTitle("Cierre Transporte", "Reporte");

            this.cargaTransportes();
            return View();
        }

        [Permiso("Control.Reporte.ProductoReserva")]
        public ActionResult Producto_Reservado()
        {
            CustomHelper.setTitle("Producto Reservado", "Reporte");

            this.CargaControles(true);
            this.CargarProductoCategorias();
            return View();
        }

        [Permiso("Control.Reporte.VentaxTipoCliente")]
        public ActionResult Venta_x_Tipo_Cliente()
        {
            CustomHelper.setTitle("Venta x Tipo de Cliente", "Reporte");

            this.CargaControles(true);
            this.cargaTiposDeClientes();
            return View();
        }

        [Permiso("Control.Reporte.VentaxTipoCliente")]
        public ActionResult Grafica_Venta_x_Tipo_Cliente()
        {
            CustomHelper.setTitle("Grafica Venta x Tipo de Cliente", "Reporte");

            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.VentaComisionVendedor")]
        public ActionResult Venta_Comision_x_Vendedor_Configurable()
        {
            CustomHelper.setTitle("Venta Comision x Vendedor Configurable", "Reporte");

            this.cargaVendedores();
            return View();
        }

        [Permiso("Control.Reporte.Reparacion_Pagos_Tecnicos")]
        public ActionResult Reparacion_Pagos_Tecnicos()
        {
            CustomHelper.setTitle("Reparación de Pagos Tecnicos", "Reporte");

            this.cargaTecnicos();
            return View();
        }

        [Permiso("Control.Reporte.Venta_x_Forma_Pago")]
        public ActionResult Venta_x_Forma_Pago()
        {
            CustomHelper.setTitle("Venta x Forma de Pago", "Reporte");

            this.cargaFormas();
            return View();
        }

        [Permiso("Control.Reporte.Producto_Reservado_x_Producto")]
        public ActionResult Producto_Reservado_x_Producto()
        {
            CustomHelper.setTitle("Producto Reservado x Producto", "Reporte");

            this.CargaControles(true);
            this.CargarProductoCategorias();
            this.cargaEstadosReserva();
            return View();
        }

        [Permiso("Control.Reporte.Producto_Reservado_Actual")]
        public ActionResult Producto_Reservado_Actual()
        {
            CustomHelper.setTitle("Producto Reservado Actual", "Reporte");

            this.CargaControles(true);           
            return View();
        }

        [Permiso("Control.Reporte.Egresos_Efectivo")]
        public ActionResult Egresos_Efectivo()
        {
            CustomHelper.setTitle("Egresos de Efectivo", "Reporte");

            this.CargaControles(true);
            this.cargaCategoriaGastos();
            return View();
        }

        [Permiso("Control.Reporte.Abono_x_Cliente")]
        public ActionResult Abono_x_Cliente()
        {
            CustomHelper.setTitle("Abonos x Cliente", "Reporte");
         
            return View();
        }

        [Permiso("Control.Reporte.VentaxProductoDiaVendedor")]
        public ActionResult Venta_x_Producto_Dia_Vendedor()
        {
            CustomHelper.setTitle("Venta x Producto x Dia x Vendedor", "Reporte");

            this.CargaControles(true);
            this.cargaVendedores();
            return View();
        }

        [Permiso("Control.Reporte.ProductoxLote")]
        public ActionResult Producto_x_Lote()
        {
            CustomHelper.setTitle("Productos x Lote", "Reporte");

            this.CargaControles(true);           
            return View();
        }

        [Permiso("Control.Reporte.HistorialVenta")]
        public ActionResult HistorialVenta()
        {
            CustomHelper.setTitle("Historial de Venta", "Reporte");
            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.HistorialEntrega")]
        public ActionResult HistorialEntrega()
        {
            CustomHelper.setTitle("Historial de Entrega", "Reporte");
            this.CargaControles(true);
            return View();
        }
    }


}