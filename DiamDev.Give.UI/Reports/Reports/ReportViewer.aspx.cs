using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.Reporting.WebForms;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using DiamDev.Give.Entities;
using DiamDev.Give.BLL;
using DiamDev.Give.UI;

using DiamDev.Give.UI.App_Start;

namespace RDLCInASPNetMVC3.Reports
{
    public partial class ReportViewer : System.Web.UI.Page
    {

        private DataTable GenerarInventario(List<ProductoInventarioModel> Inventarios)
        {

            DataTable DTInventarios = new DataTable("Inventario");
            DTInventarios.Columns.Add(new DataColumn("ProductoId", typeof(string)));           
            DTInventarios.Columns.Add(new DataColumn("Agencia", typeof(string)));
            DTInventarios.Columns.Add(new DataColumn("Codigo", typeof(string)));
            DTInventarios.Columns.Add(new DataColumn("Nombre", typeof(string)));
            DTInventarios.Columns.Add(new DataColumn("Unidad", typeof(string)));         
            DTInventarios.Columns.Add(new DataColumn("Existencia", typeof(decimal)));
            DTInventarios.Columns.Add(new DataColumn("PrecioCosto", typeof(decimal)));

            if (Inventarios != null && Inventarios.Count() > 0)
            {
                foreach (var Inventario in Inventarios)
                {
                    DTInventarios.Rows.Add(Inventario.ProductoId, Inventario.Agencia, Inventario.Codigo, Inventario.Nombre, Inventario.Unidad, Inventario.Existencia, Inventario.Precio);
                }
            }

            return DTInventarios;
        }

        private DataTable GenerarMovimiento(List<MovimientoModel> Movimientos)
        {

            DataTable DTMovimientos = new DataTable("Inventario");
            DTMovimientos.Columns.Add(new DataColumn("MovimientoId", typeof(string)));
            DTMovimientos.Columns.Add(new DataColumn("Agencia", typeof(string)));           
            DTMovimientos.Columns.Add(new DataColumn("Nombre", typeof(string)));
            DTMovimientos.Columns.Add(new DataColumn("Descripcion", typeof(string)));
            DTMovimientos.Columns.Add(new DataColumn("Total", typeof(decimal)));
            DTMovimientos.Columns.Add(new DataColumn("Usuario", typeof(string)));
            DTMovimientos.Columns.Add(new DataColumn("Forma", typeof(string)));
            DTMovimientos.Columns.Add(new DataColumn("Categoria", typeof(string)));
        
            if (Movimientos != null && Movimientos.Count() > 0)
            {
                foreach (var Movimiento in Movimientos)
                {
                    DTMovimientos.Rows.Add(Movimiento.MovimientoId, Movimiento.Agencia, Movimiento.Nombre, Movimiento.Descripcion, Movimiento.Total, Movimiento.Usuario, Movimiento.Forma, Movimiento.Categoria);
                }
            }

            return DTMovimientos;
        }

        private DataTable GenerarMovimientoProducto(List<MovimientoModel> Movimientos)
        {

            DataTable DTMovimientos = new DataTable("Inventario");
            DTMovimientos.Columns.Add(new DataColumn("MovimientoId", typeof(string)));
            DTMovimientos.Columns.Add(new DataColumn("Agencia", typeof(string)));
            DTMovimientos.Columns.Add(new DataColumn("Nombre", typeof(string)));
            DTMovimientos.Columns.Add(new DataColumn("Descripcion", typeof(string)));
            DTMovimientos.Columns.Add(new DataColumn("Total", typeof(decimal)));
            DTMovimientos.Columns.Add(new DataColumn("Usuario", typeof(string)));
            DTMovimientos.Columns.Add(new DataColumn("Forma", typeof(string)));
            DTMovimientos.Columns.Add(new DataColumn("Categoria", typeof(string)));
            DTMovimientos.Columns.Add(new DataColumn("Fecha", typeof(string)));
            DTMovimientos.Columns.Add(new DataColumn("Cantidad", typeof(decimal)));
            DTMovimientos.Columns.Add(new DataColumn("Precio", typeof(decimal)));

            if (Movimientos != null && Movimientos.Count() > 0)
            {
                foreach (var Movimiento in Movimientos)
                {
                    DTMovimientos.Rows.Add(Movimiento.MovimientoId, Movimiento.Agencia, Movimiento.Nombre, Movimiento.Descripcion, Movimiento.Total, Movimiento.Usuario, Movimiento.Forma, Movimiento.Categoria, Movimiento.Fecha.ToString("dd/MM/yyyy"), Movimiento.Cantidad, Movimiento.Precio);
                }
            }

            return DTMovimientos;
        }

        private DataTable GenerarGanancia(List<ProductoModel> Productos)
        {
            DataTable DTProductos = new DataTable("Inventario");
            DTProductos.Columns.Add(new DataColumn("ProductoId", typeof(string)));
            DTProductos.Columns.Add(new DataColumn("Agencia", typeof(string)));
            DTProductos.Columns.Add(new DataColumn("Nombre", typeof(string)));
            DTProductos.Columns.Add(new DataColumn("Fecha", typeof(DateTime)));
            DTProductos.Columns.Add(new DataColumn("Cantidad", typeof(decimal)));
            DTProductos.Columns.Add(new DataColumn("Costo", typeof(decimal)));
            DTProductos.Columns.Add(new DataColumn("Precio", typeof(decimal)));

            if (Productos != null && Productos.Count() > 0)
            {
                foreach (var Producto in Productos)
                {
                    DTProductos.Rows.Add(Producto.ProductoId, Producto.Agencia, Producto.Nombre, Producto.Fecha, Producto.Cantidad, Producto.PrecioCosto, Producto.PrecioVenta);
                }
            }

            return DTProductos;
        }

        private DataTable GenerarDiario(List<DiarioModel> Cuentas)
        {
            
            DataTable DTCuentas = new DataTable("Inventario");
            DTCuentas.Columns.Add(new DataColumn("DiarioId", typeof(long)));
            DTCuentas.Columns.Add(new DataColumn("PartidaId", typeof(string)));
            DTCuentas.Columns.Add(new DataColumn("Agencia", typeof(string)));
            DTCuentas.Columns.Add(new DataColumn("Descripcion", typeof(string)));
            DTCuentas.Columns.Add(new DataColumn("Fecha", typeof(DateTime)));
            DTCuentas.Columns.Add(new DataColumn("Cuenta", typeof(string)));
            DTCuentas.Columns.Add(new DataColumn("Debe", typeof(decimal)));
            DTCuentas.Columns.Add(new DataColumn("Haber", typeof(decimal)));

            if (Cuentas != null && Cuentas.Count() > 0)
            {
                foreach (var Cuenta in Cuentas)
                {
                    DTCuentas.Rows.Add(Cuenta.DiarioId, Cuenta.PartidaId, Cuenta.Agencia, Cuenta.Descripcion, Cuenta.Fecha, Cuenta.Cuenta, Cuenta.Debe, Cuenta.Haber);
                }
            }

            return DTCuentas;
        }

        private DataTable GenerarVentaxTienda(List<VentaModel> Ventas, bool movimientos = false)
        {

            DataTable DTVentas = new DataTable("Inventario");
            DTVentas.Columns.Add(new DataColumn("Id", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Codigo", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Marca", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Descripcion", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Factura", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Agencia", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Total", typeof(decimal)));
            DTVentas.Columns.Add(new DataColumn("Costo", typeof(decimal)));
            DTVentas.Columns.Add(new DataColumn("Precio", typeof(decimal)));
            DTVentas.Columns.Add(new DataColumn("Descuento", typeof(decimal)));
            DTVentas.Columns.Add(new DataColumn("Concepto", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Fecha", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Cantidad", typeof(decimal)));
            DTVentas.Columns.Add(new DataColumn("Estado", typeof(bool)));
            DTVentas.Columns.Add(new DataColumn("Vendedor", typeof(string)));

            if (Ventas != null && Ventas.Count() > 0)
            {
                foreach (var Venta in Ventas)
                {
                    if (movimientos)
                    {
                        DTVentas.Rows.Add(Venta.Id, Venta.Codigo, Venta.Marca, Venta.Descripcion, Venta.NoFactura, Venta.Agencia, Venta.Total, Venta.CostoIva, Venta.PrecioIva, Venta.Descuento, Venta.Concepto, Venta.Fecha.ToString("dd/MM/yyyy"), Venta.Cantidad, Venta.Estado, Venta.Vendedor);
                    }
                    else
                    {
                        DTVentas.Rows.Add(Venta.Id, Venta.Codigo, Venta.Marca, Venta.Descripcion, string.Format("{0} - {1}", Venta.Serie, Venta.NoFactura), Venta.Agencia, Venta.Total, Venta.CostoIva, Venta.PrecioIva, Venta.Descuento, Venta.Concepto, Venta.Fecha.ToString("dd/MM/yyyy"), Venta.Cantidad, Venta.Estado, Venta.Vendedor);
                    }
                }
            }

            return DTVentas;
        }

        private DataTable GenerarInventarioxTienda(List<ProductoExistenciaModel> Existencias)
        {

            DataTable DTExistencias = new DataTable("Inventario");
            DTExistencias.Columns.Add(new DataColumn("Id", typeof(string)));
            DTExistencias.Columns.Add(new DataColumn("Codigo", typeof(string)));
            DTExistencias.Columns.Add(new DataColumn("Marca", typeof(string)));
            DTExistencias.Columns.Add(new DataColumn("Descripcion", typeof(string)));          
            DTExistencias.Columns.Add(new DataColumn("Agencia", typeof(string)));
            DTExistencias.Columns.Add(new DataColumn("Cantidad", typeof(decimal)));
            DTExistencias.Columns.Add(new DataColumn("Total", typeof(decimal)));
            DTExistencias.Columns.Add(new DataColumn("Costo", typeof(decimal)));
            DTExistencias.Columns.Add(new DataColumn("Precio", typeof(decimal)));
            DTExistencias.Columns.Add(new DataColumn("Minimo", typeof(int)));
            DTExistencias.Columns.Add(new DataColumn("Maximo", typeof(int)));
            DTExistencias.Columns.Add(new DataColumn("Estado", typeof(string)));

            if (Existencias != null && Existencias.Count() > 0)
            {
                foreach (var Existencia in Existencias)
                {
                    DTExistencias.Rows.Add(Existencia.ID, Existencia.Codigo, Existencia.Marca, Existencia.Descripcion, Existencia.Agencia, Existencia.Cantidad, Existencia.Total, Existencia.Costo, Existencia.Precio, Existencia.Minimo, Existencia.Maximo, Existencia.Estado);
                }
            }

            return DTExistencias;
        }

        private DataTable GenerarVentaResumenxTienda(List<VentaResumen> Ventas)
        {
            DataTable DTVentas = new DataTable("Inventario");           
            DTVentas.Columns.Add(new DataColumn("Fecha", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Monto", typeof(decimal)));
            DTVentas.Columns.Add(new DataColumn("TC", typeof(decimal)));
            DTVentas.Columns.Add(new DataColumn("EF", typeof(decimal)));
            DTVentas.Columns.Add(new DataColumn("EFDolar", typeof(decimal)));
            DTVentas.Columns.Add(new DataColumn("Otros", typeof(decimal)));
            DTVentas.Columns.Add(new DataColumn("Factura", typeof(string)));
          
            if (Ventas != null && Ventas.Count() > 0)
            {
                foreach (var Venta in Ventas)
                {
                    DTVentas.Rows.Add(Venta.Fecha.ToString("dd/MM/yyyy"), Venta.Monto, Venta.TC, Venta.Efectivo, Venta.EfectivoDolar, Venta.Otros, Venta.Factura);
                }
            }

            return DTVentas;
        }

        private DataTable GenerarLibroVenta(List<LibroVentaModel> Ventas)
        {

            DataTable DTVentas = new DataTable("Inventario");
            DTVentas.Columns.Add(new DataColumn("Fecha", typeof(DateTime)));
            DTVentas.Columns.Add(new DataColumn("Agencia", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("TipoDocumento", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("TipoTransaccion", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Serie", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("NoFactura", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Nit", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Nombre", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Total", typeof(decimal)));
            DTVentas.Columns.Add(new DataColumn("TotalSinIva", typeof(decimal)));    

            if (Ventas != null && Ventas.Count() > 0)
            {
                foreach (var Venta in Ventas)
                {
                    DTVentas.Rows.Add(Venta.Fecha, Venta.Agencia, Venta.TipoDocumento, Venta.TipoTransaccion, Venta.Serie, Venta.NoFactura, Venta.Nit, Venta.Nombre, Venta.Total, Venta.TotalSinIva);
                }
            }

            return DTVentas;
        }

        private DataTable GenerarHorario(List<HorarioModel> Horarios)
        {
            DataTable DTHorarios = new DataTable("Inventario");
            DTHorarios.Columns.Add(new DataColumn("PersonalId", typeof(long)));
            DTHorarios.Columns.Add(new DataColumn("Nombre", typeof(string)));          
            DTHorarios.Columns.Add(new DataColumn("Fecha", typeof(string)));
            DTHorarios.Columns.Add(new DataColumn("Entrada", typeof(string)));
            DTHorarios.Columns.Add(new DataColumn("Salida", typeof(string)));
            DTHorarios.Columns.Add(new DataColumn("Laborado", typeof(string)));

            if (Horarios != null && Horarios.Count() > 0)
            {
                Horarios.ForEach(x => DTHorarios.Rows.Add(x.PersonaId, x.Nombre, x.Fecha.ToString("dd/MM/yyyy"), x.Entrada.ToString(), x.Salida == null ? "00:00:00" : x.Salida.Value.ToString(), x.Laborado.ToString()));
            }

            return DTHorarios;
        }

        private DataTable GenerarProductoControlado(List<ReporteProductoControladoModel> Ventas)
        {
            DataTable DTVentas = new DataTable("Inventario");
            DTVentas.Columns.Add(new DataColumn("Agencia", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Nit", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Cliente", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Fecha", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Factura", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Codigo", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Producto", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Cantidad", typeof(decimal)));

            if (Ventas != null && Ventas.Count() > 0)
            {
                Ventas.ForEach(x => DTVentas.Rows.Add(x.Agencia, x.Nit, x.Cliente, x.Fecha.ToString("dd/MM/yyyy"), string.Format("{0} - {1}", x.Serie, x.Factura), x.Codigo, x.Producto, x.Cantidad));
            }

            return DTVentas;
        }

        private DataTable GenerarProductoMinimoCategoria(List<ReporteMinimoCategoriaModel> Productos)
        {
            DataTable DTProductos = new DataTable("Inventario");
            DTProductos.Columns.Add(new DataColumn("Agencia", typeof(string)));
            DTProductos.Columns.Add(new DataColumn("Codigo", typeof(string)));
            DTProductos.Columns.Add(new DataColumn("Producto", typeof(string)));
            DTProductos.Columns.Add(new DataColumn("Existencia", typeof(decimal)));
            DTProductos.Columns.Add(new DataColumn("Minimo", typeof(decimal)));

            if (Productos != null && Productos.Count() > 0)
            {
                Productos.ForEach(x => DTProductos.Rows.Add(x.Agencia, x.Codigo, x.Producto, x.Existencia, x.Minimo));
            }

            return DTProductos;
        }

        private DataTable GenerarComisionVendedor(List<ReporteVentaComisionVendedorModel> Ventas)
        {
            DataTable DTVentas = new DataTable("Inventario");
            DTVentas.Columns.Add(new DataColumn("Agencia", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Nit", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Cliente", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Fecha", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Factura", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Vendedor", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("SubTotal", typeof(decimal)));
            DTVentas.Columns.Add(new DataColumn("Total", typeof(decimal)));
            DTVentas.Columns.Add(new DataColumn("Comision", typeof(decimal)));

            string Agencia = CustomHelper.getAgenciaNombre();

            if (Ventas != null && Ventas.Count() > 0)
            {
                Ventas.ForEach(x => DTVentas.Rows.Add(Agencia, x.Nit, x.Cliente, x.Fecha.ToString("dd/MM/yyyy"), string.Format("{0} - {1}", x.Serie, x.Factura), x.Vendedor, x.SubTotal, x.Total, x.Comision));
            }

            return DTVentas;
        }

        private DataTable GenerarProveedorProducto(List<ReporteProveedorProducto> Productos)
        {
            DataTable DTProductos = new DataTable("Inventario");
            DTProductos.Columns.Add(new DataColumn("Proveedor", typeof(string)));
            DTProductos.Columns.Add(new DataColumn("Codigo", typeof(string)));
            DTProductos.Columns.Add(new DataColumn("Categoria", typeof(string)));
            DTProductos.Columns.Add(new DataColumn("Producto", typeof(string)));
            DTProductos.Columns.Add(new DataColumn("Fecha", typeof(string)));
            DTProductos.Columns.Add(new DataColumn("Costo", typeof(decimal)));
            DTProductos.Columns.Add(new DataColumn("Cantidad", typeof(decimal)));
            DTProductos.Columns.Add(new DataColumn("MovimientoId", typeof(string)));
            DTProductos.Columns.Add(new DataColumn("Documento", typeof(string)));

            if (Productos != null && Productos.Count() > 0)
            {
                Productos.ForEach(x => DTProductos.Rows.Add(x.Proveedor, x.Codigo, x.Categoria, x.Producto, x.Fecha.ToString("dd/MM/yyyy"), x.Costo, x.Cantidad, x.MovimientoId, x.Documento));
            }

            return DTProductos;
        }

        private DataTable GenerarVentaTransporte(List<ReporteVentaTransporteModel> Ventas)
        {
            DataTable DTVentas = new DataTable("Inventario");          
            DTVentas.Columns.Add(new DataColumn("Nit", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Cliente", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Direccion", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Fecha", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Hora", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Factura", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Transporte", typeof(string)));           
            DTVentas.Columns.Add(new DataColumn("Total", typeof(decimal)));
            DTVentas.Columns.Add(new DataColumn("Entregado", typeof(string)));
                        
            if (Ventas != null && Ventas.Count() > 0)
            {
                Ventas.ForEach(x => DTVentas.Rows.Add(x.Nit, x.Cliente, x.Direccion, x.Fecha.ToString("dd/MM/yyyy"), x.FechaHoraFactura.ToString("hh:mm tt"), string.Format("{0} - {1}", x.Serie, x.Factura), x.Transporte, x.Total, x.EntregadoTransporte ? "Sí" : "No"));
            }

            return DTVentas;
        }

        private DataTable GenerarProductosIDs(List<ReporteInventarioIDsModel> Productos)
        {
            DataTable DTProductos = new DataTable("Inventario");
            DTProductos.Columns.Add(new DataColumn("Agencia", typeof(string)));
            DTProductos.Columns.Add(new DataColumn("ProductoId", typeof(string)));
            DTProductos.Columns.Add(new DataColumn("Codigo", typeof(string)));
            DTProductos.Columns.Add(new DataColumn("Nombre", typeof(string)));
            DTProductos.Columns.Add(new DataColumn("Marca", typeof(string)));
            DTProductos.Columns.Add(new DataColumn("Existencia", typeof(int)));
            DTProductos.Columns.Add(new DataColumn("ID", typeof(string)));

            if (Productos != null && Productos.Count() > 0)
            {
                Productos.ForEach(x => DTProductos.Rows.Add(x.Agencia, x.ProductoId, x.Codigo, x.Nombre, x.Marca, x.Existencia, x.ID));
            }

            return DTProductos;
        }

        private DataTable GenerarCierreTransporte(List<ReporteCierreTransporteModel> Ventas)
        {
            DataTable DTVentas = new DataTable("Inventario");
            DTVentas.Columns.Add(new DataColumn("Nit", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Cliente", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Direccion", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Fecha", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Hora", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Factura", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Transporte", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Total", typeof(decimal)));
            DTVentas.Columns.Add(new DataColumn("TotalMensajero", typeof(decimal)));

            if (Ventas != null && Ventas.Count() > 0)
            {
                Ventas.ForEach(x => DTVentas.Rows.Add(x.Nit, x.Cliente, x.Direccion, x.Fecha.ToString("dd/MM/yyyy"), x.FechaHoraFactura.ToString("hh:mm tt"), string.Format("{0} - {1}", x.Serie, x.Factura), x.Transporte, x.TotalFactura, x.TotalMensajero));
            }

            return DTVentas;
        }

        private DataTable GenerarProductoReserva(List<ReporteProductoReservaModel> Ventas)
        {
            DataTable DTVentas = new DataTable("Inventario");
            DTVentas.Columns.Add(new DataColumn("Fecha", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("ReservaId", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Cliente", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Categoria", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Producto", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Cantidad", typeof(decimal)));
            DTVentas.Columns.Add(new DataColumn("Total", typeof(decimal)));
            DTVentas.Columns.Add(new DataColumn("TotalPagado", typeof(decimal)));
            DTVentas.Columns.Add(new DataColumn("Agencia", typeof(string)));

            if (Ventas != null && Ventas.Count() > 0)
            {
                Ventas.ForEach(x => DTVentas.Rows.Add(x.Fecha.ToString("dd/MM/yyyy"), x.ReservaId, x.Cliente, x.Categoria, x.Producto, x.Cantidad, x.Total, x.TotalPagado, x.Agencia));
            }

            return DTVentas;
        }

        private string ObtenerFormato24Horas(string hora)
        {
            string Hora = string.Empty;

            switch (hora)
            {
                case "01":
                    Hora = "13";
                    break;
                case "02":
                    Hora = "14";
                    break;
                case "03":
                    Hora = "15";
                    break;
                case "04":
                    Hora = "16";
                    break;
                case "05":
                    Hora = "17";
                    break;
                case "06":
                    Hora = "18";
                    break;
                case "07":
                    Hora = "19";
                    break;
                case "08":
                    Hora = "20";
                    break;
                case "09":
                    Hora = "21";
                    break;
                case "10":
                    Hora = "22";
                    break;
                case "11":
                    Hora = "23";
                    break;
                case "12":
                    Hora = "24";
                    break;
            }

            return Hora;
        }

        private string Hora(string hora)
        {
            string HoraFormato24 = string.Empty;

            try
            {

                string HoraOriginal = hora;

                if (!string.IsNullOrWhiteSpace(hora))
                {
                    HoraOriginal = HoraOriginal.Substring(0, 5);

                    if (hora.Contains("PM"))
                    {
                        string[] Hora24 = HoraOriginal.Split(':');
                        HoraOriginal = string.Format("{0}:{1}", ObtenerFormato24Horas(Hora24[0]), Hora24[1]);
                    }

                    HoraFormato24 = string.Format("{0}:00", HoraOriginal);
                }

            }
            catch (Exception)
            {
            }

            return HoraFormato24;
        }

        private DataTable GenerarVentaxTipoCliente(List<ReporteVentaxTipoCliente> Ventas)
        {
            DataTable DTVentas = new DataTable("Inventario");
            DTVentas.Columns.Add(new DataColumn("Agencia", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Tipo", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Cliente", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Fecha", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Factura", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Formas", typeof(string)));          
            DTVentas.Columns.Add(new DataColumn("Total", typeof(decimal)));
            
            if (Ventas != null && Ventas.Count() > 0)
            {
                Ventas.ForEach(x => DTVentas.Rows.Add(x.Agencia, x.Tipo, x.Cliente, x.Fecha.ToString("dd/MM/yyyy"), x.Factura, x.Formas, x.Total));
            }

            return DTVentas;
        }

        private DataTable GenerarGraficaVentaxTipoCliente(List<ReporteGraficoVentaxTipoCliente> Ventas)
        {
            DataTable DTVentas = new DataTable("Inventario");            
            DTVentas.Columns.Add(new DataColumn("Tipo", typeof(string)));         
            DTVentas.Columns.Add(new DataColumn("Cantidad", typeof(int)));

            if (Ventas != null && Ventas.Count() > 0)
            {
                Ventas.ForEach(x => DTVentas.Rows.Add(x.Tipo, x.Cantidad));
            }

            return DTVentas;
        }

        private DataTable GenerarVentaxVendedorConfigurable(List<ReporteVentaComisionxVendedorConfigurable> Ventas)
        {
            DataTable DTVentas = new DataTable("Inventario");
            DTVentas.Columns.Add(new DataColumn("Vendedor", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Fecha", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Factura", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Producto", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Cantidad", typeof(decimal)));
            DTVentas.Columns.Add(new DataColumn("Precio", typeof(decimal)));
            DTVentas.Columns.Add(new DataColumn("Total", typeof(decimal)));
            DTVentas.Columns.Add(new DataColumn("Comision", typeof(decimal)));

            if (Ventas != null && Ventas.Count() > 0)
            {
                Ventas.ForEach(x => DTVentas.Rows.Add(x.Vendedor, x.Fecha.ToString("dd/MM/yyyy"), x.Factura, x.Producto, x.Cantidad, x.Precio, x.Total, x.Valido >= 1 ? x.Comision : 0));
            }

            return DTVentas;
        }

        private DataTable GenerarReparacionPagosTecnicos(List<HistorialReparacion> Reparaciones)
        {
            DataTable DTVentas = new DataTable("Inventario");
            DTVentas.Columns.Add(new DataColumn("ReparacionId", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Fecha", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("FechaFinalizacion", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Monto", typeof(decimal)));
            DTVentas.Columns.Add(new DataColumn("Tecnico", typeof(string)));
      
            if (Reparaciones != null && Reparaciones.Count() > 0)
            {
                Reparaciones.ForEach(x => DTVentas.Rows.Add(x.ReparacionId, x.Fecha.ToString("dd/MM/yyyy"), x.FechaFinalizacion == null ? "" : x.FechaFinalizacion.Value.ToString("dd/MM/yyyy"), x.Total, x.Tecnico));
            }

            return DTVentas;
        }

        private DataTable GenerarVentaxFormaPago(List<ReporteVentaxFormaPago> Ventas)
        {
            DataTable DTVentas = new DataTable("Inventario");
            DTVentas.Columns.Add(new DataColumn("Agencia", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Fecha", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Documento", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Cliente", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Forma", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Nota", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Monto", typeof(decimal)));

            if (Ventas != null && Ventas.Count() > 0)
            {
                string Agencia = CustomHelper.getAgenciaNombre();

                Ventas.ForEach(x => DTVentas.Rows.Add(Agencia, x.Fecha.ToString("dd/MM/yyyy"), x.Documento, string.Format("{0} - {1}", x.Nit, x.Nombre), x.Forma, x.Nota, x.Monto));
            }

            return DTVentas;
        }

        private DataTable GenerarProductoReservado(List<ReporteProductoReservado> Ventas)
        {
            DataTable DTVentas = new DataTable("Inventario");
            DTVentas.Columns.Add(new DataColumn("Agencia", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Cliente", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("ReservaId", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Fecha", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("FechaPrimerAbono", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Producto", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Cantidad", typeof(decimal)));
            DTVentas.Columns.Add(new DataColumn("MontoAbonado", typeof(decimal)));
            DTVentas.Columns.Add(new DataColumn("Operado", typeof(string)));
           
            if (Ventas != null && Ventas.Count() > 0)
            {
                Ventas.ForEach(x => DTVentas.Rows.Add(x.Agencia, x.Cliente, x.ReservaId, x.Fecha.ToString("dd/MM/yyyy"), x.FechaPrimerAbono.ToString("dd/MM/yyyy"), x.Producto, x.Cantidad, x.MontoAbonado, x.Operado ? "Sí" : "No"));
            }

            return DTVentas;
        }

        private DataTable GenerarEgresosEfectivo(List<ReporteEgresosEfectivo> Egresos)
        {
            DataTable DTEgresos = new DataTable("Inventario");
            DTEgresos.Columns.Add(new DataColumn("GastoId", typeof(string)));
            DTEgresos.Columns.Add(new DataColumn("Fecha", typeof(string)));
            DTEgresos.Columns.Add(new DataColumn("Agencia", typeof(string)));
            DTEgresos.Columns.Add(new DataColumn("Categoria", typeof(string)));
            DTEgresos.Columns.Add(new DataColumn("Concepto", typeof(string)));
            DTEgresos.Columns.Add(new DataColumn("Documento", typeof(string)));
            DTEgresos.Columns.Add(new DataColumn("Responsable", typeof(string)));
            DTEgresos.Columns.Add(new DataColumn("Monto", typeof(decimal)));

            if (Egresos != null && Egresos.Count() > 0)
            {
                Egresos.ForEach(x => DTEgresos.Rows.Add(x.GastoId, x.Fecha.ToString("dd/MM/yyyy"), x.Agencia, x.Categoria, x.Concepto, x.Documento, x.Responsable, x.Monto));
            }

            return DTEgresos;
        }

        private DataTable GenerarAbonoxCliente(List<ReporteAbonoxCliente> Abonos)
        {
            DataTable DTAbonos = new DataTable("Inventario");
            DTAbonos.Columns.Add(new DataColumn("ReciboId", typeof(string)));
            DTAbonos.Columns.Add(new DataColumn("Agencia", typeof(string)));
            DTAbonos.Columns.Add(new DataColumn("Fecha", typeof(string)));
            DTAbonos.Columns.Add(new DataColumn("Cliente", typeof(string)));          
            DTAbonos.Columns.Add(new DataColumn("Responsable", typeof(string)));
            DTAbonos.Columns.Add(new DataColumn("Monto", typeof(decimal)));

            if (Abonos != null && Abonos.Count() > 0)
            {
                Abonos.ForEach(x => DTAbonos.Rows.Add(x.ReciboId, x.Agencia, x.Fecha.ToString("dd/MM/yyyy"), x.Cliente, x.Responsable, x.Monto));
            }

            return DTAbonos;
        }

        private DataTable GenerarVentaxProductoDiaVendedor(List<ReporteVentaxProductoDiaVendedor> Ventas)
        {
            DataTable DTVentas = new DataTable("Inventario");
            DTVentas.Columns.Add(new DataColumn("Agencia", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Vendedor", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("ProductoId", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Codigo", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Producto", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Marca", typeof(string)));
            DTVentas.Columns.Add(new DataColumn("Cantidad", typeof(decimal)));
            DTVentas.Columns.Add(new DataColumn("Costo", typeof(decimal)));
            DTVentas.Columns.Add(new DataColumn("Venta", typeof(decimal)));
            DTVentas.Columns.Add(new DataColumn("Promedio", typeof(decimal)));
            DTVentas.Columns.Add(new DataColumn("Fecha", typeof(string)));

            if (Ventas != null && Ventas.Count() > 0)
            {
                Ventas.ForEach(x => DTVentas.Rows.Add(x.Agencia, x.Vendedor, x.ProductoId, x.Codigo, x.Nombre, x.Marca, x.Cantidad, x.Costo, x.Venta, x.Promedio, x.Fecha.ToString("dd/MM/yyyy")));
            }

            return DTVentas;
        }

        private DataTable GenerarProductoLote(List<ReporteProductoLote> Lotes)
        {
            DataTable DTLotes = new DataTable("Inventario");          
            DTLotes.Columns.Add(new DataColumn("Agencia", typeof(string)));
            DTLotes.Columns.Add(new DataColumn("ProductoId", typeof(string)));
            DTLotes.Columns.Add(new DataColumn("Codigo", typeof(string)));
            DTLotes.Columns.Add(new DataColumn("Producto", typeof(string)));
            DTLotes.Columns.Add(new DataColumn("Lote", typeof(string)));
            DTLotes.Columns.Add(new DataColumn("FechaVecimiento", typeof(string)));
            DTLotes.Columns.Add(new DataColumn("Cantidad", typeof(decimal)));

            if (Lotes != null && Lotes.Count() > 0)
            {
                Lotes.ForEach(x => DTLotes.Rows.Add(x.Agencia, x.ProductoId, x.Codigo, x.Producto, x.Lote, x.FechaVencimiento.ToString("dd/MM/yyyy"), x.Cantidad));
            }

            return DTLotes;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                try
                {                    
                    if (Request.QueryString["ReportID"] != null)
                    {
                  
                        string strReportID = Request.QueryString["ReportID"].ToString();                        
                        string Reports = "~/Reports/ReportKardex.rdlc";
                        
                        string strCentroId = string.Empty;
                        long CentroId = 0;
                        long PrecioId = 0;
                        long ProveedorId = 0;
                        string ProductoId = string.Empty;
                        string Usuario = string.Empty;
                        string Rol = string.Empty;
                        string Fecha = string.Empty;
                        string FechaInicialG = string.Empty;
                        string FechaFinalG = string.Empty;
                        string Agencia = string.Empty;
                        string Param1 = string.Empty;
                        string Param2 = string.Empty;
                        string Param3 = string.Empty;
                        ReportDataSource rd=new ReportDataSource();

                        DataTable dt = new DataTable("Inventario");
                        DataSet Cierre = new DataSet("Inventario");

                        List<ProductoInventarioModel> Inventarios = new List<ProductoInventarioModel>();
                        List<FacturaModel> Facturas = new List<FacturaModel>();
                        List<MovimientoModel> Movimientos = new List<MovimientoModel>();
                        List<ProductoModel> Ganancias = new List<ProductoModel>();
                        List<DiarioModel> Diarios = new List<DiarioModel>();
                        
                        switch (strReportID)
                        {
                            case "ReportInventario":

                                try
                                {

                                    strCentroId = Request.QueryString["CentroId"].ToString();

                                    if (!string.IsNullOrWhiteSpace(strCentroId))
                                    {
                                        CentroId = Convert.ToInt64(strCentroId);
                                    }

                                    List<ProductoExistenciaModel> Existencias = new ProductoBL().ObtenerExistenciaPorTienda(0, CentroId, CustomHelper.getUserId());
                                    dt = GenerarInventarioxTienda(Existencias);

                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                    Reports = "~/Reports/ReportInventario.rdlc";

                                }
                                catch (Exception)
                                {
                                }


                                break;
                            case "ReportInventarioxTiendaCategoria":

                                try
                                {

                                    strCentroId = Request.QueryString["CentroId"].ToString();
                                    string strCategoriaId = Request.QueryString["CategoriaId"].ToString();

                                    if (!string.IsNullOrWhiteSpace(strCentroId))
                                    {
                                        CentroId = Convert.ToInt64(strCentroId);
                                    }

                                    long categoriaId = -1;
                                    if (!string.IsNullOrWhiteSpace(strCategoriaId))
                                    {
                                        categoriaId = Convert.ToInt64(strCategoriaId);
                                    }

                                    List<ProductoExistenciaModel> Existencias = new ProductoBL().ObtenerExistenciaPorTienda(0, CentroId, CustomHelper.getUserId(), categoriaId);
                                    dt = GenerarInventarioxTienda(Existencias);

                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                    Reports = "~/Reports/ReportInventarioxTiendaCategoria.rdlc";

                                }
                                catch (Exception)
                                {
                                }


                                break;                    
                            case "ReportCierre":

                                try
                                {
                                    string strFechaInicial = Request.QueryString["FechaInicial"].ToString();
                                    string strFechaFinal = Request.QueryString["FechaFinal"].ToString();
                                    strCentroId = Request.QueryString["CentroId"].ToString(); 

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                    if (!string.IsNullOrWhiteSpace(strCentroId))
                                    {
                                        CentroId = Convert.ToInt64(strCentroId);
                                    }

                                    //Detalle del Cierre
                                    Facturas = new FacturaBL().ObtenerFactura(FechaInicial, FechaFinal, CentroId, CustomHelper.getUserId()).Select(x =>
                                                         new FacturaModel
                                                         {
                                                            FacturaId = x.FacturaId,
                                                            Documento = x.Documento,
                                                            Fecha = x.Fecha,
                                                            Agencia = x.Agencia, 
                                                            Nombre = x.Nombre,
                                                            Usuario = x.Usuario,
                                                            Descuento = x.Descuento,
                                                            Total = x.Total,
                                                            TotalLiquido = x.TotalLiquido,
                                                            Forma = x.Forma
                                                         }
                                                     ).ToList();

                                    //Resumen de Forma de Pago
                                    List<FormaPago> Formas = new FacturaBL().ObtenerFacturaPorFormaPago(FechaInicial, FechaFinal, CentroId, CustomHelper.getUserId()).ToList();
                                    long FacturaId = 0;

                                    DataTable DTCierre = new DataTable("Cierre");                                    

                                    DTCierre.Columns.Add(new DataColumn("FacturaId", typeof(string)));
                                    DTCierre.Columns.Add(new DataColumn("Documento", typeof(string)));
                                    DTCierre.Columns.Add(new DataColumn("Fecha", typeof(DateTime)));
                                    DTCierre.Columns.Add(new DataColumn("Agencia", typeof(string)));
                                    DTCierre.Columns.Add(new DataColumn("Nombre", typeof(string)));
                                    DTCierre.Columns.Add(new DataColumn("Descuento", typeof(decimal)));
                                    DTCierre.Columns.Add(new DataColumn("Total", typeof(decimal)));
                                    DTCierre.Columns.Add(new DataColumn("TotalLiquido", typeof(decimal)));
                                    DTCierre.Columns.Add(new DataColumn("Forma", typeof(string)));
                                    DTCierre.Columns.Add(new DataColumn("Usuario", typeof(string)));

                                    if (Facturas != null && Facturas.Count() > 0)
                                    {
                                        foreach (var Factura in Facturas)
                                        {

                                            if (FacturaId ==  0)
                                            {
                                                FacturaId = Factura.FacturaId;
                                            }

                                            DTCierre.Rows.Add(Factura.FacturaId, Factura.Documento, Factura.Fecha, Factura.Agencia, Factura.Nombre, Factura.Descuento, Factura.Total, Factura.TotalLiquido, Factura.Forma, Factura.Usuario);
                                        }
                                    }

                                    DataTable DTCierreForma = new DataTable("CierreForma");

                                    DTCierreForma.Columns.Add(new DataColumn("FacturaId", typeof(string)));
                                    DTCierreForma.Columns.Add(new DataColumn("Nombre", typeof(string)));
                                    DTCierreForma.Columns.Add(new DataColumn("Valor", typeof(decimal)));

                                    if (Formas != null && Formas.Count() > 0)
                                    {
                                        foreach (var Forma in Formas)
                                        {
                                            DTCierreForma.Rows.Add(FacturaId, Forma.Nombre, Forma.Valor);
                                        }                                       
                                    }

                                    Cierre.Tables.Add(DTCierre);
                                    Cierre.Tables.Add(DTCierreForma);

                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                    Reports = "~/Reports/ReportCierre.rdlc";
                                }
                                catch (Exception)
                                {
                                }

                                break;
                                
                                     case "ReportTopClientes":

                                try
                                {
                                    string strFechaInicial = Request.QueryString["FechaInicial"].ToString();
                                    string strFechaFinal = Request.QueryString["FechaFinal"].ToString();
                                   
                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                  

                                    DeliveryREntities ddb = new DeliveryREntities();
                                    List<sp_topclientes_Result> cm = new List<sp_topclientes_Result>();
                                    cm = ddb.sp_topclientes( FechaInicial, FechaFinal).ToList();
                                    int cantped = cm.Max(x => x.CantidadPedidos);
                                    sp_topclientes_Result temp = cm.Where(x => x.CantidadPedidos == cantped).FirstOrDefault();
                                    Param1 = temp.Nombre+ " "+temp.No_Telefono;

                                    decimal montoped = cm.Max(x => x.MontoPedido);
                                    sp_topclientes_Result temp2 = cm.Where(x => x.MontoPedido == montoped).FirstOrDefault();
                                    Param2 = temp2.Nombre + " " + temp2.No_Telefono;

                                    FechaFinalG = FechaFinal.ToString();
                                    FechaInicialG = FechaInicial.ToString();

                                    rd = new ReportDataSource("DataSet1", cm);



                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                    Reports = "~/Reports/ReportTopClientes.rdlc";
                                }
                                catch (Exception ess)
                                {
                                }

                                break;
                            case "ReportKpiDelivery":

                                try
                                {
                                    string strFechaInicial = Request.QueryString["FechaInicial"].ToString();
                                    string strFechaFinal = Request.QueryString["FechaFinal"].ToString();
                                    strCentroId = Request.QueryString["AgenciaId"].ToString();

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                    if (!string.IsNullOrWhiteSpace(strCentroId))
                                    {
                                        CentroId = Convert.ToInt64(strCentroId);
                                    }

                                    DeliveryREntities ddb = new DeliveryREntities();
                                    List<KpiDelivery_Result> cm = new List<KpiDelivery_Result>();
                                    cm = ddb.KpiDelivery(CentroId, FechaInicial, FechaFinal).ToList();

                                    Agencia = new AgenciaBL().ObtenerPorId(CentroId).Nombre;
                                    FechaFinalG = FechaFinal.ToString();
                                    FechaInicialG = FechaInicial.ToString();

                                     rd = new ReportDataSource("DataSet1", cm);
                                  
                                   

                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                    Reports = "~/Reports/ReportKpiDelivery.rdlc";
                                }
                                catch (Exception ess)
                                {
                                }

                                break;
                            case "ReportCierrexUsuario":

                                try
                                {
                                    string strFechaInicial = Request.QueryString["FechaInicial"].ToString();
                                    string strFechaFinal = Request.QueryString["FechaFinal"].ToString();
                                    strCentroId = Request.QueryString["CentroId"].ToString();

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                    if (!string.IsNullOrWhiteSpace(strCentroId))
                                    {
                                        CentroId = Convert.ToInt64(strCentroId);
                                    }

                                    //Detalle del Cierre
                                    Facturas = new FacturaBL().ObtenerFactura(FechaInicial, FechaFinal, CentroId, CustomHelper.getUserId()).Select(x =>
                                                         new FacturaModel
                                                         {
                                                             FacturaId = x.FacturaId,
                                                             Documento = x.Documento,
                                                             Fecha = x.Fecha,
                                                             Agencia = x.Agencia,
                                                             Nombre = x.Nombre,
                                                             Usuario = x.Usuario,
                                                             Descuento = x.Descuento,
                                                             Total = x.Total,
                                                             TotalLiquido = x.TotalLiquido,
                                                             Forma = x.Forma
                                                         }
                                                     ).ToList();

                                    //Resumen de Forma de Pago
                                    List<FormaPago> Formas = new FacturaBL().ObtenerFacturaPorFormaPago(FechaInicial, FechaFinal, CentroId, CustomHelper.getUserId()).ToList();
                                    long FacturaId = 0;

                                    DataTable DTCierre = new DataTable("Cierre");

                                    DTCierre.Columns.Add(new DataColumn("FacturaId", typeof(string)));
                                    DTCierre.Columns.Add(new DataColumn("Documento", typeof(string)));
                                    DTCierre.Columns.Add(new DataColumn("Fecha", typeof(DateTime)));
                                    DTCierre.Columns.Add(new DataColumn("Agencia", typeof(string)));
                                    DTCierre.Columns.Add(new DataColumn("Nombre", typeof(string)));
                                    DTCierre.Columns.Add(new DataColumn("Descuento", typeof(decimal)));
                                    DTCierre.Columns.Add(new DataColumn("Total", typeof(decimal)));
                                    DTCierre.Columns.Add(new DataColumn("TotalLiquido", typeof(decimal)));
                                    DTCierre.Columns.Add(new DataColumn("Forma", typeof(string)));
                                    DTCierre.Columns.Add(new DataColumn("Usuario", typeof(string)));

                                    if (Facturas != null && Facturas.Count() > 0)
                                    {
                                        foreach (var Factura in Facturas)
                                        {

                                            if (FacturaId == 0)
                                            {
                                                FacturaId = Factura.FacturaId;
                                            }

                                            DTCierre.Rows.Add(Factura.FacturaId, Factura.Documento, Factura.Fecha, Factura.Agencia, Factura.Nombre, Factura.Descuento, Factura.Total, Factura.TotalLiquido, Factura.Forma, Factura.Usuario);
                                        }
                                    }

                                    DataTable DTCierreForma = new DataTable("CierreForma");

                                    DTCierreForma.Columns.Add(new DataColumn("FacturaId", typeof(string)));
                                    DTCierreForma.Columns.Add(new DataColumn("Nombre", typeof(string)));
                                    DTCierreForma.Columns.Add(new DataColumn("Valor", typeof(decimal)));

                                    if (Formas != null && Formas.Count() > 0)
                                    {
                                        foreach (var Forma in Formas)
                                        {
                                            DTCierreForma.Rows.Add(FacturaId, Forma.Nombre, Forma.Valor);
                                        }
                                    }

                                    Cierre.Tables.Add(DTCierre);
                                    Cierre.Tables.Add(DTCierreForma);

                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                    Reports = "~/Reports/ReportCierrexUsuario.rdlc";
                                }
                                catch (Exception)
                                {
                                }

                                break;
                            case "ReportCierrexUsuarioHora":

                                try
                                {
                                    string strUsuarioId = Request.QueryString["UsuarioId"].ToString();
                                    string strFechaInicial = Request.QueryString["FechaInicial"].ToString();
                                    string strFechaFinal = Request.QueryString["FechaInicial"].ToString();
                                    string strHoraInicial = Request.QueryString["HoraInicial"].ToString();
                                    string strHoraFinal = Request.QueryString["HoraFinal"].ToString();
                                    strCentroId = Request.QueryString["CentroId"].ToString();

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strHoraInicial) && !string.IsNullOrWhiteSpace(strHoraFinal))
                                    {
                                        strHoraInicial = Hora(strHoraInicial);
                                        strHoraFinal = Hora(strHoraFinal);

                                        strFechaInicial = strFechaInicial + ' ' + strHoraInicial;
                                        strFechaFinal = strFechaFinal + ' ' + strHoraFinal;

                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                    if (!string.IsNullOrWhiteSpace(strCentroId))
                                    {
                                        CentroId = Convert.ToInt64(strCentroId);
                                    }

                                    long UsuarioId = 0;
                                    if (!string.IsNullOrWhiteSpace(strUsuarioId))
                                    {
                                        UsuarioId = Convert.ToInt64(strUsuarioId);
                                    }

                                    //Detalle del Cierre
                                    Facturas = new FacturaBL().ObtenerFacturaxUsuario(FechaInicial, FechaFinal, CentroId, UsuarioId).Select(x =>
                                                         new FacturaModel
                                                         {
                                                             FacturaId = x.FacturaId,
                                                             Documento = x.Documento,
                                                             Fecha = x.Fecha,
                                                             Agencia = x.Agencia,
                                                             Nombre = x.Nombre,
                                                             Usuario = x.Usuario,
                                                             Descuento = x.Descuento,
                                                             Total = x.Total,
                                                             TotalLiquido = x.TotalLiquido,
                                                             Forma = x.Forma
                                                         }
                                                     ).ToList();

                                    //Resumen de Forma de Pago
                                    List<FormaPago> Formas = new FacturaBL().ObtenerFacturaPorFormaPagoxUsuario(FechaInicial, FechaFinal, CentroId, UsuarioId).ToList();
                                    long FacturaId = 0;

                                    DataTable DTCierre = new DataTable("Cierre");

                                    DTCierre.Columns.Add(new DataColumn("FacturaId", typeof(string)));
                                    DTCierre.Columns.Add(new DataColumn("Documento", typeof(string)));
                                    DTCierre.Columns.Add(new DataColumn("Fecha", typeof(DateTime)));
                                    DTCierre.Columns.Add(new DataColumn("Agencia", typeof(string)));
                                    DTCierre.Columns.Add(new DataColumn("Nombre", typeof(string)));
                                    DTCierre.Columns.Add(new DataColumn("Descuento", typeof(decimal)));
                                    DTCierre.Columns.Add(new DataColumn("Total", typeof(decimal)));
                                    DTCierre.Columns.Add(new DataColumn("TotalLiquido", typeof(decimal)));
                                    DTCierre.Columns.Add(new DataColumn("Forma", typeof(string)));
                                    DTCierre.Columns.Add(new DataColumn("Usuario", typeof(string)));

                                    if (Facturas != null && Facturas.Count() > 0)
                                    {
                                        foreach (var Factura in Facturas)
                                        {

                                            if (FacturaId == 0)
                                            {
                                                FacturaId = Factura.FacturaId;
                                            }

                                            DTCierre.Rows.Add(Factura.FacturaId, Factura.Documento, Factura.Fecha, Factura.Agencia, Factura.Nombre, Factura.Descuento, Factura.Total, Factura.TotalLiquido, Factura.Forma, Factura.Usuario);
                                        }
                                    }

                                    DataTable DTCierreForma = new DataTable("CierreForma");

                                    DTCierreForma.Columns.Add(new DataColumn("FacturaId", typeof(string)));
                                    DTCierreForma.Columns.Add(new DataColumn("Nombre", typeof(string)));
                                    DTCierreForma.Columns.Add(new DataColumn("Valor", typeof(decimal)));

                                    if (Formas != null && Formas.Count() > 0)
                                    {
                                        foreach (var Forma in Formas)
                                        {
                                            DTCierreForma.Rows.Add(FacturaId, Forma.Nombre, Forma.Valor);
                                        }
                                    }

                                    Cierre.Tables.Add(DTCierre);
                                    Cierre.Tables.Add(DTCierreForma);

                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                    Fecha = string.Format("DEL {0} AL {1}", FechaInicial.ToString("dd/MM/yyyy hh:mm tt"), FechaFinal.ToString("dd/MM/yyyy hh:mm tt"));

                                    Reports = "~/Reports/ReportCierrexUsuarioHora.rdlc";
                                }
                                catch (Exception)
                                {
                                }

                                break;  
                            case "ReportIngreso":

                                try
                                {
                                    string strFechaInicial = Request.QueryString["FechaInicial"].ToString();
                                    string strFechaFinal = Request.QueryString["FechaFinal"].ToString();                                 
                                    strCentroId = Request.QueryString["CentroId"].ToString();

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;
                                 
                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                    if (!string.IsNullOrWhiteSpace(strCentroId))
                                    {
                                        CentroId = Convert.ToInt64(strCentroId);
                                    }

                                    Movimientos = new MovimientoBL().ObtenerMovimientoPorTipo(FechaInicial, FechaFinal, 1, CentroId, CustomHelper.getUserId(), ProveedorId, ProductoId).Select(x =>
                                                        new MovimientoModel
                                                        {
                                                            MovimientoId = x.MovimientoId,
                                                            Categoria = x.Categoria,
                                                            Agencia = x.Agencia,                                                          
                                                            Nombre = x.Nombre,
                                                            Descripcion = x.Descripcion,
                                                            Total = x.Total,
                                                            Usuario = x.Usuario,
                                                            Forma = x.Forma
                                                        }
                                                    ).ToList();

                                   
                                    dt = GenerarMovimiento(Movimientos);

                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                    Reports = "~/Reports/ReportIngreso.rdlc";
                                }
                                catch (Exception)
                                {
                                }

                                break;
                            case "ReportIngresoxProveedor":

                                try
                                {
                                    string strFechaInicial = Request.QueryString["FechaInicial"].ToString();
                                    string strFechaFinal = Request.QueryString["FechaFinal"].ToString();
                                    strCentroId = Request.QueryString["CentroId"].ToString();
                                    string strProveedorId = Request.QueryString["ProveedorId"].ToString();

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                    if (!string.IsNullOrWhiteSpace(strCentroId))
                                    {
                                        CentroId = Convert.ToInt64(strCentroId);
                                    }

                                    if (!string.IsNullOrWhiteSpace(strProveedorId))
                                    {
                                        ProveedorId = Convert.ToInt64(strProveedorId);
                                    }

                                    Movimientos = new MovimientoBL().ObtenerMovimientoPorTipo(FechaInicial, FechaFinal, 1, CentroId, CustomHelper.getUserId(), ProveedorId, ProductoId).Select(x =>
                                                        new MovimientoModel
                                                        {
                                                            MovimientoId = x.MovimientoId,
                                                            Categoria = x.Categoria,
                                                            Agencia = x.Agencia,
                                                            Nombre = x.Nombre,
                                                            Descripcion = x.Descripcion,
                                                            Total = x.Total,
                                                            Usuario = x.Usuario,
                                                            Forma = x.Forma
                                                        }
                                                    ).ToList();
                                   
                                    dt = GenerarMovimiento(Movimientos);

                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                    Reports = "~/Reports/ReportIngresoxProveedor.rdlc";
                                }
                                catch (Exception)
                                {
                                }

                                break;
                            case "ReportIngresoxProducto":

                                try
                                {
                                    string strFechaInicial = Request.QueryString["FechaInicial"].ToString();
                                    string strFechaFinal = Request.QueryString["FechaFinal"].ToString();
                                    strCentroId = Request.QueryString["CentroId"].ToString();
                                    string strProductoId = Request.QueryString["ProductoId"].ToString();

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                    if (!string.IsNullOrWhiteSpace(strCentroId))
                                    {
                                        CentroId = Convert.ToInt64(strCentroId);
                                    }

                                    if (!string.IsNullOrWhiteSpace(strProductoId))
                                    {
                                        ProductoId = strProductoId;
                                    }

                                    Movimientos = new MovimientoBL().ObtenerMovimientoPorTipo(FechaInicial, FechaFinal, 1, CentroId, CustomHelper.getUserId(), ProveedorId, ProductoId).Select(x =>
                                                        new MovimientoModel
                                                        {
                                                            MovimientoId = x.MovimientoId,
                                                            Categoria = x.Categoria,
                                                            Agencia = x.Agencia,
                                                            Nombre = x.Nombre,
                                                            Descripcion = x.Descripcion,
                                                            Total = x.Total,
                                                            Usuario = x.Usuario,
                                                            Forma = x.Forma,
                                                            Fecha = x.Fecha,
                                                            Cantidad = x.Cantidad,
                                                            Precio = x.Precio
                                                        }
                                                    ).ToList();

                                    dt = GenerarMovimientoProducto(Movimientos);

                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                    Reports = "~/Reports/ReportIngresoxProducto.rdlc";
                                }
                                catch (Exception)
                                {
                                }

                                break;
                            case "ReportEgreso":

                                try
                                {
                                    string strFechaInicial = Request.QueryString["FechaInicial"].ToString();
                                    string strFechaFinal = Request.QueryString["FechaFinal"].ToString();
                                    strCentroId = Request.QueryString["CentroId"].ToString();

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                    if (!string.IsNullOrWhiteSpace(strCentroId))
                                    {
                                        CentroId = Convert.ToInt64(strCentroId);
                                    }

                                    Movimientos = new MovimientoBL().ObtenerMovimientoPorTipo(FechaInicial, FechaFinal, 2, CentroId, CustomHelper.getUserId(), ProveedorId, ProductoId).Select(x =>
                                                        new MovimientoModel
                                                        {
                                                            MovimientoId = x.MovimientoId,
                                                            Categoria = x.Categoria,
                                                            Agencia = x.Agencia,
                                                            Nombre = x.Nombre,
                                                            Descripcion = x.Descripcion,
                                                            Total = x.Total,
                                                            Usuario = x.Usuario,
                                                            Forma = x.Forma
                                                        }
                                                    ).ToList();

                                    long MovimientoId = 0;

                                    DataTable DTMovimiento = new DataTable("Movimiento");
                                    DTMovimiento.Columns.Add(new DataColumn("MovimientoId", typeof(string)));
                                    DTMovimiento.Columns.Add(new DataColumn("Agencia", typeof(string)));                                
                                    DTMovimiento.Columns.Add(new DataColumn("Nombre", typeof(string)));
                                    DTMovimiento.Columns.Add(new DataColumn("Descripcion", typeof(string)));
                                    DTMovimiento.Columns.Add(new DataColumn("Total", typeof(decimal)));
                                    DTMovimiento.Columns.Add(new DataColumn("Usuario", typeof(string)));
                                    DTMovimiento.Columns.Add(new DataColumn("Forma", typeof(string)));
                                    DTMovimiento.Columns.Add(new DataColumn("Categoria", typeof(string)));

                                    if (Movimientos != null && Movimientos.Count() > 0)
                                    {
                                        foreach (var Movimiento in Movimientos)
                                        {
                                            if (MovimientoId == 0)
                                            {
                                                MovimientoId = Movimiento.MovimientoId;
                                            }

                                            DTMovimiento.Rows.Add(Movimiento.MovimientoId, Movimiento.Agencia, Movimiento.Nombre, Movimiento.Descripcion, Movimiento.Total, Movimiento.Usuario, Movimiento.Forma, Movimiento.Categoria);
                                        }
                                    }

                                    //Resumen de Forma de Pago
                                    List<FormaPago> Formas = new MovimientoBL().ObtenerMovimientoPorFormaPago(FechaInicial, FechaFinal, CentroId, CustomHelper.getUserId()).ToList();

                                    DataTable DTMovimientoForma = new DataTable("MovimientoForma");

                                    DTMovimientoForma.Columns.Add(new DataColumn("MovimientoId", typeof(string)));
                                    DTMovimientoForma.Columns.Add(new DataColumn("Nombre", typeof(string)));
                                    DTMovimientoForma.Columns.Add(new DataColumn("Valor", typeof(decimal)));

                                    if (Formas != null && Formas.Count() > 0)
                                    {
                                        foreach (var Forma in Formas)
                                        {
                                            DTMovimientoForma.Rows.Add(MovimientoId, Forma.Nombre, Forma.Valor);
                                        }
                                    }

                                    Cierre.Tables.Add(DTMovimiento);
                                    Cierre.Tables.Add(DTMovimientoForma);

                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                    Reports = "~/Reports/ReportEgreso.rdlc";
                                }
                                catch (Exception)
                                {
                                }

                                break;
                            case "ReportGanancia":

                                try
                                {
                                    string strFechaInicial = Request.QueryString["FechaInicial"].ToString();
                                    string strFechaFinal = Request.QueryString["FechaFinal"].ToString();
                                    strCentroId = Request.QueryString["CentroId"].ToString();

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                    if (!string.IsNullOrWhiteSpace(strCentroId))
                                    {
                                        CentroId = Convert.ToInt64(strCentroId);
                                    }

                                    Ganancias = new ProductoBL().ObtenerGananciaPorProductoVenta(FechaInicial, FechaFinal, CentroId, CustomHelper.getUserId()).Select(x =>
                                                        new ProductoModel
                                                        {
                                                            ProductoId = x.ProductoId,
                                                            Agencia = x.Agencia,
                                                            Nombre = x.Nombre,
                                                            Fecha = x.Fecha,
                                                            Cantidad = x.Cantidad,
                                                            PrecioCosto = x.PrecioCosto,
                                                            PrecioVenta = x.PrecioVenta
                                                        }
                                                    ).ToList();

                                    dt = GenerarGanancia(Ganancias);

                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                    Reports = "~/Reports/ReportGanancia.rdlc";
                                }
                                catch (Exception)
                                {
                                }

                                break;
                            case "ReportGananciaConsolidado":

                                try
                                {
                                    string strFechaInicial = Request.QueryString["FechaInicial"].ToString();
                                    string strFechaFinal = Request.QueryString["FechaFinal"].ToString();
                                    strCentroId = Request.QueryString["CentroId"].ToString();

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                    if (!string.IsNullOrWhiteSpace(strCentroId))
                                    {
                                        CentroId = Convert.ToInt64(strCentroId);
                                    }

                                    Ganancias = new ProductoBL().ObtenerGananciaConsolidadaxVenta(FechaInicial, FechaFinal, CentroId, CustomHelper.getUserId()).Select(x =>
                                                        new ProductoModel
                                                        {
                                                            ProductoId = x.ProductoId,
                                                            Agencia = x.Agencia,
                                                            Nombre = x.Nombre,
                                                            Fecha = x.Fecha,
                                                            Cantidad = x.Cantidad,
                                                            PrecioCosto = x.PrecioCosto,
                                                            PrecioVenta = x.PrecioVenta
                                                        }
                                                    ).ToList();

                                    dt = GenerarGanancia(Ganancias);

                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                    Reports = "~/Reports/ReportGananciaConsolidado.rdlc";
                                }
                                catch (Exception)
                                {
                                }

                                break;
                            case "ReportGananciaConsolidadoxProducto":

                                try
                                {
                                    string strFechaInicial = Request.QueryString["FechaInicial"].ToString();
                                    string strFechaFinal = Request.QueryString["FechaFinal"].ToString();
                                    strCentroId = Request.QueryString["CentroId"].ToString();
                                    string strProductoId = Request.QueryString["ProductoId"].ToString();

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                    CentroId = 0;
                                    if (!string.IsNullOrWhiteSpace(strCentroId))
                                    {
                                        CentroId = Convert.ToInt64(strCentroId);
                                    }

                                    Ganancias = new ProductoBL().ObtenerGananciaConsolidadaxProductoVenta(FechaInicial, FechaFinal, CentroId, strProductoId, CustomHelper.getUserId()).Select(x =>
                                                        new ProductoModel
                                                        {
                                                            ProductoId = x.ProductoId,
                                                            Agencia = x.Agencia,
                                                            Nombre = x.Nombre,
                                                            Fecha = x.Fecha,
                                                            Cantidad = x.Cantidad,
                                                            PrecioCosto = x.PrecioCosto,
                                                            PrecioVenta = x.PrecioVenta
                                                        }
                                                    ).ToList();

                                    dt = GenerarGanancia(Ganancias);

                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                    Reports = "~/Reports/ReportGananciaConsolidadoxProducto.rdlc";
                                }
                                catch (Exception)
                                {
                                }

                                break;
                            case "ReportDiario":

                                try
                                {
                                    string strFechaInicial = Request.QueryString["FechaInicial"].ToString();
                                    string strFechaFinal = Request.QueryString["FechaFinal"].ToString();
                                    strCentroId = Request.QueryString["CentroId"].ToString();

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                    if (!string.IsNullOrWhiteSpace(strCentroId))
                                    {
                                        CentroId = Convert.ToInt64(strCentroId);
                                    }

                                    Diarios = new DiarioBL().ObtenerDiarioPorFecha(FechaInicial, FechaFinal, CentroId, CustomHelper.getUserId(), false, false).Select(x =>
                                                        new DiarioModel
                                                        {
                                                           DiarioId = x.DiarioId,
                                                           PartidaId = x.PartidaId,
                                                           Agencia = x.Agencia,
                                                           Descripcion = x.Descripcion,
                                                           Fecha = x.Fecha,
                                                           Cuenta = x.Cuenta,
                                                           Debe = x.Debe,
                                                           Haber = x.Haber
                                                        }
                                                    ).ToList();

                                    dt = GenerarDiario(Diarios);
                                    Reports = "~/Reports/ReportDiario.rdlc";
                                }
                                catch (Exception)
                                {
                                }

                                break;
                            case "ReportMayor":

                                try
                                {
                                    string strFechaInicial = Request.QueryString["FechaInicial"].ToString();
                                    string strFechaFinal = Request.QueryString["FechaFinal"].ToString();
                                    strCentroId = Request.QueryString["CentroId"].ToString();

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                    if (!string.IsNullOrWhiteSpace(strCentroId))
                                    {
                                        CentroId = Convert.ToInt64(strCentroId);
                                    }

                                    Diarios = new DiarioBL().ObtenerDiarioPorFecha(FechaInicial, FechaFinal, CentroId, CustomHelper.getUserId(), true, false).Select(x =>
                                                        new DiarioModel
                                                        {
                                                            DiarioId = x.DiarioId,
                                                            PartidaId = x.PartidaId,
                                                            Agencia = x.Agencia,
                                                            Descripcion = x.Descripcion,
                                                            Fecha = x.Fecha,
                                                            Cuenta = x.Cuenta,
                                                            Debe = x.Debe,
                                                            Haber = x.Haber
                                                        }
                                                    ).ToList();

                                    dt = GenerarDiario(Diarios);
                                    Reports = "~/Reports/ReportMayor.rdlc";
                                }
                                catch (Exception)
                                {
                                }

                                break;
                            case "ReportBalanceSaldo":

                                try
                                {
                                    string strFechaInicial = Request.QueryString["FechaInicial"].ToString();
                                    string strFechaFinal = Request.QueryString["FechaFinal"].ToString();
                                    strCentroId = Request.QueryString["CentroId"].ToString();

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                    if (!string.IsNullOrWhiteSpace(strCentroId))
                                    {
                                        CentroId = Convert.ToInt64(strCentroId);
                                    }

                                    Diarios = new DiarioBL().ObtenerDiarioPorFecha(FechaInicial, FechaFinal, CentroId, CustomHelper.getUserId(), false, true).Select(x =>
                                                        new DiarioModel
                                                        {
                                                            DiarioId = x.DiarioId,
                                                            PartidaId = x.PartidaId,
                                                            Agencia = x.Agencia,
                                                            Descripcion = x.Descripcion,
                                                            Fecha = x.Fecha,
                                                            Cuenta = x.Cuenta,
                                                            Debe = x.Debe,
                                                            Haber = x.Haber
                                                        }
                                                    ).ToList();

                                    dt = GenerarDiario(Diarios);
                                    Reports = "~/Reports/ReportBalanceSaldo.rdlc";
                                }
                                catch (Exception)
                                {
                                }

                                break;
                            case "ReportVentaxTienda":
                                try
                                {
                                    string strFechaInicial = Request.QueryString["FechaInicial"].ToString();
                                    string strFechaFinal = Request.QueryString["FechaFinal"].ToString();
                                    strCentroId = Request.QueryString["CentroId"].ToString();

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                    if (!string.IsNullOrWhiteSpace(strCentroId))
                                    {
                                        CentroId = Convert.ToInt64(strCentroId);
                                    }

                                    List<VentaModel> Ventas = new FacturaBL().ObtenerVentasxTienda(FechaInicial, FechaFinal, 0, CentroId, CustomHelper.getUserId());
                                    dt = GenerarVentaxTienda(Ventas);

                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                    Reports = "~/Reports/ReportVentaxTienda.rdlc";
                                }
                                catch (Exception)
                                {
                                }
                                
                                break;
                            case "ReportVentaxTiendaYMarca":
                                try
                                {
                                    string strFechaInicial = Request.QueryString["FechaInicial"].ToString();
                                    string strFechaFinal = Request.QueryString["FechaFinal"].ToString();
                                    strCentroId = Request.QueryString["CentroId"].ToString();
                                    string strMarcaId = Request.QueryString["MarcaId"].ToString();

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                    if (!string.IsNullOrWhiteSpace(strCentroId))
                                    {
                                        CentroId = Convert.ToInt64(strCentroId);
                                    }

                                    long MarcaId = 0;

                                    if (!string.IsNullOrWhiteSpace(strMarcaId))
                                    {
                                        MarcaId = Convert.ToInt64(strMarcaId);
                                    }

                                    List<VentaModel> Ventas = new FacturaBL().ObtenerVentasxTienda(FechaInicial, FechaFinal, MarcaId, CentroId, CustomHelper.getUserId());
                                    dt = GenerarVentaxTienda(Ventas);

                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                    Reports = "~/Reports/ReportVentaxTiendaYMarca.rdlc";
                                }
                                catch (Exception)
                                {
                                }

                                break;
                            case "ReportTomaFisicaInventarioxTienda":
                                try
                                {
                                    strCentroId = Request.QueryString["CentroId"].ToString();

                                    if (!string.IsNullOrWhiteSpace(strCentroId))
                                    {
                                        CentroId = Convert.ToInt64(strCentroId);
                                    }

                                    List<ProductoExistenciaModel> Existencias = new ProductoBL().ObtenerExistenciaPorTienda(0, CentroId, CustomHelper.getUserId());
                                    dt = GenerarInventarioxTienda(Existencias);

                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                    Reports = "~/Reports/ReportTomaFisicaInventarioxTienda.rdlc";
                                }
                                catch (Exception)
                                {
                                }

                                break;
                            case "ReportInventarioxTienda":
                                try
                                {                                   
                                    strCentroId = Request.QueryString["CentroId"].ToString();
                               
                                    if (!string.IsNullOrWhiteSpace(strCentroId))
                                    {
                                        CentroId = Convert.ToInt64(strCentroId);
                                    }

                                    List<ProductoExistenciaModel> Existencias = new ProductoBL().ObtenerExistenciaPorTienda(0, CentroId, CustomHelper.getUserId());
                                    dt = GenerarInventarioxTienda(Existencias);

                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                    Reports = "~/Reports/ReportInventarioxTienda.rdlc";
                                }
                                catch (Exception)
                                {
                                }

                                break;
                            case "ReportInventarioxTiendaYMarca":
                                try
                                {
                                    strCentroId = Request.QueryString["CentroId"].ToString();
                                    string strMarcaId = Request.QueryString["MarcaId"].ToString();

                                    if (!string.IsNullOrWhiteSpace(strCentroId))
                                    {
                                        CentroId = Convert.ToInt64(strCentroId);
                                    }

                                    long MarcaId = 0;

                                    if (!string.IsNullOrWhiteSpace(strMarcaId))
                                    {
                                        MarcaId = Convert.ToInt64(strMarcaId);
                                    }

                                    List<ProductoExistenciaModel> Existencias = new ProductoBL().ObtenerExistenciaPorTienda(MarcaId, CentroId, CustomHelper.getUserId());
                                    dt = GenerarInventarioxTienda(Existencias);

                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                    Reports = "~/Reports/ReportInventarioxTiendaYMarca.rdlc";
                                }
                                catch (Exception)
                                {
                                }

                                break;
                            case "ReportPedidoxTiendaYMarca":
                                try
                                {
                                    strCentroId = Request.QueryString["CentroId"].ToString();
                                    string strMarcaId = Request.QueryString["MarcaId"].ToString();

                                    if (!string.IsNullOrWhiteSpace(strCentroId))
                                    {
                                        CentroId = Convert.ToInt64(strCentroId);
                                    }

                                    long MarcaId = 0;

                                    if (!string.IsNullOrWhiteSpace(strMarcaId))
                                    {
                                        MarcaId = Convert.ToInt64(strMarcaId);
                                    }

                                    List<ProductoExistenciaModel> Existencias = new ProductoBL().ObtenerExistenciaPorTienda(MarcaId, CentroId, CustomHelper.getUserId(), -1, true);
                                    dt = GenerarInventarioxTienda(Existencias);

                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                    Reports = "~/Reports/ReportPedidoxTiendaYMarca.rdlc";
                                }
                                catch (Exception)
                                {
                                }

                                break;
                            case "ReportVentaResumenxTienda":
                                try
                                {
                                    string strFechaInicial = Request.QueryString["FechaInicial"].ToString();
                                    string strFechaFinal = Request.QueryString["FechaFinal"].ToString();
                                    strCentroId = Request.QueryString["CentroId"].ToString();

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                    if (!string.IsNullOrWhiteSpace(strCentroId))
                                    {
                                        CentroId = Convert.ToInt64(strCentroId);
                                    }

                                    List<VentaResumen> Ventas = new FacturaBL().ObtenerVentasResumenxTienda(FechaInicial, FechaFinal, CentroId);
                                    dt = GenerarVentaResumenxTienda(Ventas);

                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                    Reports = "~/Reports/ReportVentaResumenxTienda.rdlc";
                                }
                                catch (Exception)
                                {
                                }

                                break;
                            case "ReportCierreDiarioResumen":
                                try
                                {
                                    string strFechaInicial = Request.QueryString["FechaInicial"].ToString();
                                    string strFechaFinal = Request.QueryString["FechaFinal"].ToString();
                                    strCentroId = Request.QueryString["CentroId"].ToString();

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                    if (!string.IsNullOrWhiteSpace(strCentroId))
                                    {
                                        CentroId = Convert.ToInt64(strCentroId);
                                    }

                                    List<VentaResumen> Ventas = new FacturaBL().ObtenerCierreResumen(FechaInicial, FechaFinal, CentroId);
                                    dt = GenerarVentaResumenxTienda(Ventas);

                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                    Reports = "~/Reports/ReportCierreDiarioResumen.rdlc";
                                }
                                catch (Exception)
                                {
                                }

                                break;
                            case "ReportIngresoxTienda":
                                try
                                {
                                    string strFechaInicial = Request.QueryString["FechaInicial"].ToString();
                                    string strFechaFinal = Request.QueryString["FechaFinal"].ToString();
                                    strCentroId = Request.QueryString["CentroId"].ToString();

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                    if (!string.IsNullOrWhiteSpace(strCentroId))
                                    {
                                        CentroId = Convert.ToInt64(strCentroId);
                                    }

                                    List<VentaModel> Ventas = new MovimientoBL().ObtenerMovimientosxTienda(FechaInicial, FechaFinal, CentroId, 1, CustomHelper.getUserId());
                                    dt = GenerarVentaxTienda(Ventas, true);

                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                    Reports = "~/Reports/ReportIngresoxTienda.rdlc";
                                }
                                catch (Exception)
                                {
                                }

                                break;
                            case "ReportSalidaxTienda":
                                try
                                {
                                    string strFechaInicial = Request.QueryString["FechaInicial"].ToString();
                                    string strFechaFinal = Request.QueryString["FechaFinal"].ToString();
                                    strCentroId = Request.QueryString["CentroId"].ToString();

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                    if (!string.IsNullOrWhiteSpace(strCentroId))
                                    {
                                        CentroId = Convert.ToInt64(strCentroId);
                                    }

                                    List<VentaModel> Ventas = new MovimientoBL().ObtenerMovimientosxTienda(FechaInicial, FechaFinal, CentroId, 2, CustomHelper.getUserId());
                                    dt = GenerarVentaxTienda(Ventas, true);

                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                    Reports = "~/Reports/ReportSalidaxTienda.rdlc";
                                }
                                catch (Exception)
                                {
                                }

                                break;
                            case "ReportHorarioIndividual":
                                try
                                {
                                    string strFechaInicial = Request.QueryString["FechaInicial"].ToString();
                                    string strFechaFinal = Request.QueryString["FechaFinal"].ToString();
                                    string strPersonalId = Request.QueryString["PersonalId"].ToString();
                                    long PersonalId = 0;

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                    if (!string.IsNullOrWhiteSpace(strPersonalId))
                                    {
                                        PersonalId = Convert.ToInt64(strPersonalId);
                                    }

                                    Fecha = string.Format("DEL {0} AL {1}", FechaInicial.ToString("dd/MM/yyyy"), FechaFinal.ToString("dd/MM/yyyy"));

                                    List<HorarioModel> horario = new PersonalHorarioBL().ObtenerHorarioxPersonalId(FechaInicial, FechaFinal, PersonalId);
                                    dt = GenerarHorario(horario);

                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                    Reports = "~/Reports/ReportHorarioIndividual.rdlc";
                                }
                                catch (Exception)
                                {
                                }

                                break;
                            case "ReportLibroVenta":
                                try
                                {
                                    string strFechaInicial = Request.QueryString["FechaInicial"].ToString();
                                    string strFechaFinal = Request.QueryString["FechaFinal"].ToString();
                                    strCentroId = Request.QueryString["CentroId"].ToString();

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                    if (!string.IsNullOrWhiteSpace(strCentroId))
                                    {
                                        CentroId = Convert.ToInt64(strCentroId);
                                    }

                                    Fecha = string.Format("DEL {0} AL {1}", FechaInicial.ToString("dd/MM/yyyy"), FechaFinal.ToString("dd/MM/yyyy"));

                                    List<LibroVentaModel> Ventas = new FacturaBL().ObtenerLibroVenta(FechaInicial, FechaFinal, CentroId, CustomHelper.getUserId());
                                    dt = GenerarLibroVenta(Ventas);

                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                    Reports = "~/Reports/ReportLibroVenta.rdlc";
                                }
                                catch (Exception)
                                {
                                }

                                break;
                            case "ReportProductoControlado":

                                try
                                {
                                    strCentroId = Request.QueryString["CentroId"].ToString();
                                    string strCategoriaId = Request.QueryString["CategoriaId"].ToString();
                                    string strFechaInicial = Request.QueryString["FechaInicial"].ToString();
                                    string strFechaFinal = Request.QueryString["FechaFinal"].ToString();
                                    
                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                    if (!string.IsNullOrWhiteSpace(strCentroId))
                                    {
                                        CentroId = Convert.ToInt64(strCentroId);
                                    }

                                    long CategoriaId = 0;
                                    if (!string.IsNullOrWhiteSpace(strCategoriaId))
                                    {
                                        CategoriaId = Convert.ToInt64(strCategoriaId);
                                    }

                                    List<ReporteProductoControladoModel> Ventas = new ProductoBL().ReporteProductoControlado(CentroId, CategoriaId, FechaInicial, FechaFinal);
                                    dt = GenerarProductoControlado(Ventas);

                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                    Fecha = string.Format("DEL {0} AL {1}", FechaInicial.ToString("dd/MM/yyyy"), FechaFinal.ToString("dd/MM/yyyy"));

                                    Reports = "~/Reports/ReportProductoControlado.rdlc";
                                }
                                catch (Exception)
                                {
                                }

                                break;
                            case "ReportProductoMinimoCategoria":

                                try
                                {
                                    strCentroId = Request.QueryString["CentroId"].ToString();
                                    string strCategoriaId = Request.QueryString["CategoriaId"].ToString();
                                 
                                    if (!string.IsNullOrWhiteSpace(strCentroId))
                                    {
                                        CentroId = Convert.ToInt64(strCentroId);
                                    }

                                    long CategoriaId = 0;
                                    if (!string.IsNullOrWhiteSpace(strCategoriaId))
                                    {
                                        CategoriaId = Convert.ToInt64(strCategoriaId);
                                    }

                                    List<ReporteMinimoCategoriaModel> Productos = new ProductoBL().ReporteProductoMinimoCategoria(CentroId, CategoriaId);
                                    dt = GenerarProductoMinimoCategoria(Productos);

                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                    Reports = "~/Reports/ReportProductoMinimoCategoria.rdlc";
                                }
                                catch (Exception)
                                {
                                }

                                break;
                            case "ReportVentaComisionVendedor":

                                try
                                {                                   
                                    string strVendedorId = Request.QueryString["VendedorId"].ToString();
                                    string strFechaInicial = Request.QueryString["FechaInicial"].ToString();
                                    string strFechaFinal = Request.QueryString["FechaFinal"].ToString();

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                    if (string.IsNullOrWhiteSpace(strVendedorId))
                                    {
                                        CentroId = CustomHelper.getAgenciaId();
                                    }
                                    else
                                    {
                                        CentroId = 0;
                                    }

                                    long VendedorId = 0;
                                    if (!string.IsNullOrWhiteSpace(strVendedorId))
                                    {
                                        VendedorId = Convert.ToInt64(strVendedorId);
                                    }

                                    List<ReporteVentaComisionVendedorModel> Ventas = new FacturaBL().ReporteVentaComisionVendedor(CentroId, VendedorId, FechaInicial, FechaFinal);
                                    dt = GenerarComisionVendedor(Ventas);

                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                    Fecha = string.Format("DEL {0} AL {1}", FechaInicial.ToString("dd/MM/yyyy"), FechaFinal.ToString("dd/MM/yyyy"));

                                    Reports = "~/Reports/ReportVentaComisionVendedor.rdlc";
                                }
                                catch (Exception)
                                {
                                }

                                break;
                            case "ReportProveedorProducto":

                                try
                                {
                                    string strProveedorId = Request.QueryString["ProveedorId"].ToString();
                                    string strFechaInicial = Request.QueryString["FechaInicial"].ToString();
                                    string strFechaFinal = Request.QueryString["FechaFinal"].ToString();

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }                                    

                                    ProveedorId = 0;
                                    if (!string.IsNullOrWhiteSpace(strProveedorId))
                                    {
                                        ProveedorId = Convert.ToInt64(strProveedorId);
                                    }

                                    List<ReporteProveedorProducto> Productos = new ProveedorBL().ReporteProveedorProducto(ProveedorId, FechaInicial, FechaFinal);
                                    dt = GenerarProveedorProducto(Productos);

                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                    Fecha = string.Format("DEL {0} AL {1}", FechaInicial.ToString("dd/MM/yyyy"), FechaFinal.ToString("dd/MM/yyyy"));

                                    Reports = "~/Reports/ReportProveedorProducto.rdlc";
                                }
                                catch (Exception)
                                {
                                }

                                break;
                            case "ReportVentaTransporte":

                                try
                                {
                                    string strTransporteId = Request.QueryString["TransporteId"].ToString();
                                    string strFechaInicial = Request.QueryString["FechaInicial"].ToString();
                                    string strFechaFinal = Request.QueryString["FechaFinal"].ToString();

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                    long TransporteId = 0;
                                    if (!string.IsNullOrWhiteSpace(strTransporteId))
                                    {
                                        TransporteId = Convert.ToInt64(strTransporteId);
                                    }

                                    List<ReporteVentaTransporteModel> Ventas = new FacturaBL().ReporteVentaTransporte(TransporteId, FechaInicial, FechaFinal);
                                    dt = GenerarVentaTransporte(Ventas);

                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                    Fecha = string.Format("DEL {0} AL {1}", FechaInicial.ToString("dd/MM/yyyy"), FechaFinal.ToString("dd/MM/yyyy"));

                                    Reports = "~/Reports/ReportVentaTransporte.rdlc";
                                }
                                catch (Exception)
                                {
                                }

                                break;
                            case "ReportInventarioIDsxTiendaYProducto":
                                try
                                {
                                    strCentroId = Request.QueryString["CentroId"].ToString();
                                    string strProductoId = Request.QueryString["ProductoId"].ToString();

                                    if (!string.IsNullOrWhiteSpace(strCentroId))
                                    {
                                        CentroId = Convert.ToInt64(strCentroId);
                                    }

                                    long ProductoIds = 0;

                                    if (!string.IsNullOrWhiteSpace(strProductoId))
                                    {
                                        ProductoIds = Convert.ToInt64(strProductoId);
                                    }

                                    List<ReporteInventarioIDsModel> Existencias = new ProductoBL().ReporteProductoIDs(CentroId, ProductoIds);
                                    dt = GenerarProductosIDs(Existencias);

                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                    Reports = "~/Reports/ReportInventarioIDsxTiendaYProducto.rdlc";
                                }
                                catch (Exception)
                                {
                                }

                                break;
                            case "ReportCierreTransporte":

                                try
                                {
                                    string strTransporteId = Request.QueryString["TransporteId"].ToString();
                                    string strFechaInicial = Request.QueryString["FechaInicial"].ToString();
                                    string strFechaFinal = Request.QueryString["FechaFinal"].ToString();

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                    long TransporteId = 0;
                                    if (!string.IsNullOrWhiteSpace(strTransporteId))
                                    {
                                        TransporteId = Convert.ToInt64(strTransporteId);
                                    }

                                    List<ReporteCierreTransporteModel> Ventas = new FacturaBL().ReporteCierreTransporte(TransporteId, FechaInicial, FechaFinal);
                                    dt = GenerarCierreTransporte(Ventas);

                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                    Fecha = string.Format("DEL {0} AL {1}", FechaInicial.ToString("dd/MM/yyyy"), FechaFinal.ToString("dd/MM/yyyy"));

                                    Reports = "~/Reports/ReportCierreTransporte.rdlc";
                                }
                                catch (Exception)
                                {
                                }

                                break;
                            case "ReportProductoReserva":

                                try
                                {
                                    strCentroId = Request.QueryString["CentroId"].ToString();
                                    string strCategoriaId = Request.QueryString["CategoriaId"].ToString();
                                
                                    if (!string.IsNullOrWhiteSpace(strCentroId))
                                    {
                                        CentroId = Convert.ToInt64(strCentroId);
                                    }

                                    long CategoriaId = 0;
                                    if (!string.IsNullOrWhiteSpace(strCategoriaId))
                                    {
                                        CategoriaId = Convert.ToInt64(strCategoriaId);
                                    }

                                    List<ReporteProductoReservaModel> Ventas = new ProductoBL().ReporteProductoReserva(CentroId, CategoriaId);
                                    dt = GenerarProductoReserva(Ventas);

                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                 
                                    Reports = "~/Reports/ReportProductoReserva.rdlc";
                                }
                                catch (Exception)
                                {
                                }

                                break;
                            case "ReportVentaxTipoCliente":

                                try
                                {
                                    string strAgenciaId = Request.QueryString["AgenciaId"].ToString();
                                    string strTipoId = Request.QueryString["TipoId"].ToString();
                                    string strFechaInicial = Request.QueryString["FechaInicial"].ToString();
                                    string strFechaFinal = Request.QueryString["FechaFinal"].ToString();

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                    long AgenciaId = 0;
                                    if (!string.IsNullOrWhiteSpace(strAgenciaId))
                                    {
                                        AgenciaId = Convert.ToInt64(strAgenciaId);
                                    }

                                    long TipoId = 0;
                                    if (!string.IsNullOrWhiteSpace(strTipoId))
                                    {
                                        TipoId = Convert.ToInt64(strTipoId);
                                    }

                                    List<ReporteVentaxTipoCliente> Ventas = new FacturaBL().ReporteVentaxTipoCliente(AgenciaId, TipoId, FechaInicial, FechaFinal);
                                    dt = GenerarVentaxTipoCliente(Ventas);

                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                    Fecha = string.Format("DEL {0} AL {1}", FechaInicial.ToString("dd/MM/yyyy"), FechaFinal.ToString("dd/MM/yyyy"));

                                    Reports = "~/Reports/ReportVentaxTipoCliente.rdlc";
                                }
                                catch (Exception)
                                {
                                }

                                break;
                            case "ReportGraficaVentaxTipoCliente":

                                try
                                {
                                    string strAgenciaId = Request.QueryString["AgenciaId"].ToString();                                 
                                    string strFechaInicial = Request.QueryString["FechaInicial"].ToString();
                                    string strFechaFinal = Request.QueryString["FechaFinal"].ToString();

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                    long AgenciaId = 0;
                                    if (!string.IsNullOrWhiteSpace(strAgenciaId))
                                    {
                                        AgenciaId = Convert.ToInt64(strAgenciaId);
                                    }
                                   
                                    List<ReporteGraficoVentaxTipoCliente> Ventas = new FacturaBL().ReporteGraficaVentaxTipoCliente(AgenciaId, FechaInicial, FechaFinal);
                                    dt = GenerarGraficaVentaxTipoCliente(Ventas);

                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                    Fecha = string.Format("DEL {0} AL {1}", FechaInicial.ToString("dd/MM/yyyy"), FechaFinal.ToString("dd/MM/yyyy"));

                                    Reports = "~/Reports/ReportGraficaVentaxTipoCliente.rdlc";
                                }
                                catch (Exception)
                                {
                                }

                                break;
                            case "ReportVentaComisionxVendedorConfigurable":

                                try
                                {
                                    string strVendedorId = Request.QueryString["VendedorId"].ToString();
                                    string strFechaInicial = Request.QueryString["FechaInicial"].ToString();
                                    string strFechaFinal = Request.QueryString["FechaFinal"].ToString();

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                    long VendedorId = 0;
                                    if (!string.IsNullOrWhiteSpace(strVendedorId))
                                    {
                                        VendedorId = Convert.ToInt64(strVendedorId);
                                    }

                                    List<ReporteVentaComisionxVendedorConfigurable> Ventas = new FacturaBL().ReporteVentaComisionxVendedorConfigurable(VendedorId, FechaInicial, FechaFinal);
                                    dt = GenerarVentaxVendedorConfigurable(Ventas);

                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                    Fecha = string.Format("DEL {0} AL {1}", FechaInicial.ToString("dd/MM/yyyy"), FechaFinal.ToString("dd/MM/yyyy"));

                                    Reports = "~/Reports/ReportVentaComisionxVendedorConfigurable.rdlc";
                                }
                                catch (Exception)
                                {
                                }

                                break;
                            case "ReportReparacionPagosTecnicos":

                                try
                                {
                                    string strTecnicoId = Request.QueryString["TecnicoId"].ToString();
                                    string strFechaInicial = Request.QueryString["FechaInicial"].ToString();
                                    string strFechaFinal = Request.QueryString["FechaFinal"].ToString();

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                    long TecnicoId = 0;
                                    if (!string.IsNullOrWhiteSpace(strTecnicoId))
                                    {
                                        TecnicoId = Convert.ToInt64(strTecnicoId);
                                    }

                                    List<HistorialReparacion> Ventas = new ReparacionBL().ObtenerHistorialReparacionxTecnicoFecha(TecnicoId, FechaInicial, FechaFinal);
                                    dt = GenerarReparacionPagosTecnicos(Ventas);

                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                    Fecha = string.Format("DEL {0} AL {1}", FechaInicial.ToString("dd/MM/yyyy"), FechaFinal.ToString("dd/MM/yyyy"));

                                    Reports = "~/Reports/ReportReparacionPagosTecnicos.rdlc";
                                }
                                catch (Exception)
                                {
                                }

                                break;
                            case "ReportVentaxFormaPago":

                                try
                                {
                                    string strFormaId = Request.QueryString["FormaId"].ToString();
                                    string strFechaInicial = Request.QueryString["FechaInicial"].ToString();
                                    string strFechaFinal = Request.QueryString["FechaFinal"].ToString();

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                    long FormaId = 0;
                                    if (!string.IsNullOrWhiteSpace(strFormaId))
                                    {
                                        FormaId = Convert.ToInt64(strFormaId);
                                    }

                                    List<ReporteVentaxFormaPago> Ventas = new FacturaBL().ReporteVentaxFormaPago(CustomHelper.getAgenciaId(), FormaId, FechaInicial, FechaFinal);
                                    dt = GenerarVentaxFormaPago(Ventas);

                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                    Fecha = string.Format("DEL {0} AL {1}", FechaInicial.ToString("dd/MM/yyyy"), FechaFinal.ToString("dd/MM/yyyy"));

                                    Reports = "~/Reports/ReportVentaxFormaPago.rdlc";
                                }
                                catch (Exception)
                                {
                                }

                                break;
                            case "ReportProductoReservado":

                                try
                                {
                                    strCentroId = Request.QueryString["CentroId"].ToString();
                                    string strCategoriaId = Request.QueryString["CategoriaId"].ToString();
                                    string strProductoId = Request.QueryString["ProductoId"].ToString();
                                    string strEstadoId = Request.QueryString["EstadoId"].ToString();
                                    string strFechaInicial = Request.QueryString["FechaInicial"].ToString();
                                    string strFechaFinal = Request.QueryString["FechaFinal"].ToString();

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                    if (!string.IsNullOrWhiteSpace(strCentroId))
                                    {
                                        CentroId = Convert.ToInt64(strCentroId);
                                    }

                                    long CategoriaId = 0;
                                    if (!string.IsNullOrWhiteSpace(strCategoriaId))
                                    {
                                        CategoriaId = Convert.ToInt64(strCategoriaId);
                                    }

                                    string sProductoId = "0";
                                    if (!string.IsNullOrWhiteSpace(strProductoId))
                                    {
                                        sProductoId = strProductoId;
                                    }

                                    int EstadoId = 0;
                                    if (!string.IsNullOrWhiteSpace(strEstadoId))
                                    {
                                        EstadoId = Convert.ToInt32(strEstadoId);
                                    }

                                    List<ReporteProductoReservado> Ventas = new ProductoBL().ReporteProductoReservado(CentroId, CategoriaId, sProductoId, EstadoId == 1 ? true : false, FechaInicial, FechaFinal);
                                    dt = GenerarProductoReservado(Ventas);

                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                    Fecha = string.Format("DEL {0} AL {1}", FechaInicial.ToString("dd/MM/yyyy"), FechaFinal.ToString("dd/MM/yyyy"));

                                    Reports = "~/Reports/ReportProductoReservado.rdlc";
                                }
                                catch (Exception)
                                {
                                }

                                break;
                            case "ReportProductoReservadoActual":

                                try
                                {
                                    strCentroId = Request.QueryString["CentroId"].ToString();                                   
                                    string strFechaInicial = Request.QueryString["FechaInicial"].ToString();
                                    string strFechaFinal = Request.QueryString["FechaFinal"].ToString();

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                    if (!string.IsNullOrWhiteSpace(strCentroId))
                                    {
                                        CentroId = Convert.ToInt64(strCentroId);
                                    }
                                  
                                    List<ReporteProductoReservado> Ventas = new ProductoBL().ReporteProductoReservado(CentroId, 0, "0", false, FechaInicial, FechaFinal);
                                    dt = GenerarProductoReservado(Ventas);

                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                    Fecha = string.Format("DEL {0} AL {1}", FechaInicial.ToString("dd/MM/yyyy"), FechaFinal.ToString("dd/MM/yyyy"));

                                    Reports = "~/Reports/ReportProductoReservadoActual.rdlc";
                                }
                                catch (Exception)
                                {
                                }

                                break;
                            case "ReportEgresoEfectivo":

                                try
                                {
                                    strCentroId = Request.QueryString["CentroId"].ToString();
                                    string strCategoriaId = Request.QueryString["CategoriaId"].ToString();
                                    string strFechaInicial = Request.QueryString["FechaInicial"].ToString();
                                    string strFechaFinal = Request.QueryString["FechaFinal"].ToString();

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                    if (!string.IsNullOrWhiteSpace(strCentroId))
                                    {
                                        CentroId = Convert.ToInt64(strCentroId);
                                    }

                                    long CategoriaId = 0;
                                    if (!string.IsNullOrWhiteSpace(strCategoriaId))
                                    {
                                        CategoriaId = Convert.ToInt64(strCategoriaId);
                                    }

                                    List<ReporteEgresosEfectivo> Egresos = new GastoBL().ReporteEgresosEfectivo(CentroId, CategoriaId, FechaInicial, FechaFinal);
                                    dt = GenerarEgresosEfectivo(Egresos);

                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                    Fecha = string.Format("DEL {0} AL {1}", FechaInicial.ToString("dd/MM/yyyy"), FechaFinal.ToString("dd/MM/yyyy"));

                                    Reports = "~/Reports/ReportEgresoEfectivo.rdlc";
                                }
                                catch (Exception)
                                {
                                }

                                break;
                            case "ReportAbonoxCliente":

                                try
                                {                                    
                                    string strClienteId = Request.QueryString["ClienteId"].ToString();
                                    string strFechaInicial = Request.QueryString["FechaInicial"].ToString();
                                    string strFechaFinal = Request.QueryString["FechaFinal"].ToString();

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }
                                    
                                    long ClienteId = 0;
                                    if (!string.IsNullOrWhiteSpace(strClienteId))
                                    {
                                        ClienteId = Convert.ToInt64(strClienteId);
                                    }

                                    List<ReporteAbonoxCliente> Abonos = new ReciboBL().ReporteAbonoxCliente(ClienteId, FechaInicial, FechaFinal);
                                    dt = GenerarAbonoxCliente(Abonos);

                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                    Fecha = string.Format("DEL {0} AL {1}", FechaInicial.ToString("dd/MM/yyyy"), FechaFinal.ToString("dd/MM/yyyy"));

                                    Reports = "~/Reports/ReportAbonoxCliente.rdlc";
                                }
                                catch (Exception)
                                {
                                }

                                break;
                            case "ReportVentaxProductoDiaVendedor":

                                try
                                {
                                    strCentroId = Request.QueryString["CentroId"].ToString();
                                    string strVendedorId = Request.QueryString["VendedorId"].ToString();
                                    string strFechaInicial = Request.QueryString["FechaInicial"].ToString();
                                    string strFechaFinal = Request.QueryString["FechaFinal"].ToString();

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                    CentroId = 0;
                                    if (!string.IsNullOrWhiteSpace(strCentroId))
                                    {
                                        CentroId = Convert.ToInt64(strCentroId);
                                    }

                                    long VendedorId = 0;
                                    if (!string.IsNullOrWhiteSpace(strVendedorId))
                                    {
                                        VendedorId = Convert.ToInt64(strVendedorId);
                                    }

                                    List<ReporteVentaxProductoDiaVendedor> Ventas = new ReciboBL().ReporteVentaxProductoDiaVendedor(CentroId, VendedorId, FechaInicial, FechaFinal);
                                    dt = GenerarVentaxProductoDiaVendedor(Ventas);

                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                    Fecha = string.Format("DEL {0} AL {1}", FechaInicial.ToString("dd/MM/yyyy"), FechaFinal.ToString("dd/MM/yyyy"));

                                    Reports = "~/Reports/ReportVentaxProductoDiaVendedor.rdlc";
                                }
                                catch (Exception)
                                {
                                }

                                break;
                            case "ReportProductoLote":
                                try
                                {
                                    strCentroId = Request.QueryString["CentroId"].ToString();
                                    string strProductoId = Request.QueryString["ProductoId"].ToString();

                                    if (!string.IsNullOrWhiteSpace(strCentroId))
                                    {
                                        CentroId = Convert.ToInt64(strCentroId);
                                    }

                                    long ProductoIds = 0;

                                    if (!string.IsNullOrWhiteSpace(strProductoId))
                                    {
                                        ProductoIds = Convert.ToInt64(strProductoId);
                                    }

                                    List<ReporteProductoLote> Productos = new ProductoBL().ReporteProductoLote(CentroId, ProductoIds);
                                    dt = GenerarProductoLote(Productos);

                                    Usuario = CustomHelper.getUsuarioNombre();
                                    Rol = new RolBL().ObtenerPermisoPorUsuario(CustomHelper.getUserId());
                                    Reports = "~/Reports/ReportProductoLote.rdlc";
                                }
                                catch (Exception)
                                {
                                }

                                break;
                        }
                       
                        this.rptReport.AsyncRendering = true;
                        this.rptReport.SizeToReportContent = true;
                        this.rptReport.ZoomMode = ZoomMode.FullPage;
                        this.rptReport.LocalReport.ReportPath = Server.MapPath(Reports);
                        this.rptReport.LocalReport.DataSources.Clear();

                        if (strReportID.Equals("ReportKpiDelivery"))
                        {
                            ReportParameter[] Parametros = new ReportParameter[4];
                            Parametros[0] = new ReportParameter("Usuario", Usuario.ToUpper());
                            Parametros[1] = new ReportParameter("FechaInicial", FechaInicialG.ToString());
                            Parametros[2] = new ReportParameter("FechaFinal", FechaFinalG.ToString());
                            Parametros[3] = new ReportParameter("Agencia", Agencia);
                            this.rptReport.LocalReport.SetParameters(Parametros);
                        }
                        else if (strReportID.Equals("ReportTopClientes"))
                        {
                            ReportParameter[] Parametros = new ReportParameter[5];
                            Parametros[0] = new ReportParameter("Usuario", Usuario.ToUpper());
                            Parametros[1] = new ReportParameter("FechaInicial", FechaInicialG.ToString());
                            Parametros[2] = new ReportParameter("FechaFinal", FechaFinalG.ToString());
                            Parametros[3] = new ReportParameter("ClienteMasPedidos", Param1);
                            Parametros[4] = new ReportParameter("ClienteMasMonto", Param2);
                            this.rptReport.LocalReport.SetParameters(Parametros);
                        }
                        else { 

                        if (!string.IsNullOrWhiteSpace(Usuario) && string.IsNullOrWhiteSpace(Fecha))
                        {                       
                            ReportParameter[] Parametros = new ReportParameter[2];
                            Parametros[0] = new ReportParameter("Usuario", Usuario.ToUpper());
                            Parametros[1] = new ReportParameter("Rol", Rol.ToUpper());   
                            this.rptReport.LocalReport.SetParameters(Parametros);
                        }

                        if (!string.IsNullOrWhiteSpace(Usuario) && !string.IsNullOrWhiteSpace(Fecha))
                        {
                            ReportParameter[] Parametros = new ReportParameter[3];
                            Parametros[0] = new ReportParameter("Usuario", Usuario.ToUpper());
                            Parametros[1] = new ReportParameter("Rol", Rol.ToUpper());
                            Parametros[2] = new ReportParameter("Fecha", Fecha);
                            this.rptReport.LocalReport.SetParameters(Parametros);
                        }
                        }

                        if (strReportID.Equals("ReportCierre"))
                        {
                            this.rptReport.LocalReport.DataSources.Add(new ReportDataSource("Cierre", Cierre.Tables[0]));
                            this.rptReport.LocalReport.DataSources.Add(new ReportDataSource("CierreForma", Cierre.Tables[1]));
                        }
                        else if (strReportID.Equals("ReportCierrexUsuario"))
                        {
                            this.rptReport.LocalReport.DataSources.Add(new ReportDataSource("Cierre", Cierre.Tables[0]));
                            this.rptReport.LocalReport.DataSources.Add(new ReportDataSource("CierreForma", Cierre.Tables[1]));
                        }
                        else if (strReportID.Equals("ReportCierrexUsuarioHora"))
                        {
                            this.rptReport.LocalReport.DataSources.Add(new ReportDataSource("Cierre", Cierre.Tables[0]));
                            this.rptReport.LocalReport.DataSources.Add(new ReportDataSource("CierreForma", Cierre.Tables[1]));
                        }
                        else if (strReportID.Equals("ReportEgreso"))
                        {
                            this.rptReport.LocalReport.DataSources.Add(new ReportDataSource("Movimiento", Cierre.Tables[0]));
                            this.rptReport.LocalReport.DataSources.Add(new ReportDataSource("MovimientoForma", Cierre.Tables[1]));
                        }
                        else if (strReportID.Equals("ReportKpiDelivery")|| strReportID.Equals("ReportTopClientes"))
                        {
                            this.rptReport.LocalReport.DataSources.Add(rd);

                        }
                        
      
                        else
                        {
                            ReportDataSource _rsource = new ReportDataSource("Inventario", dt);
                            this.rptReport.LocalReport.DataSources.Add(_rsource);        
                        }                        
                                        
                        this.rptReport.LocalReport.Refresh();

                    }

                }
                catch (Exception)
                {
                }
            }
        }
    }
}