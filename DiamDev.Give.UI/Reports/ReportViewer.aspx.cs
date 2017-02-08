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

            if (Movimientos != null && Movimientos.Count() > 0)
            {
                foreach (var Movimiento in Movimientos)
                {
                    DTMovimientos.Rows.Add(Movimiento.MovimientoId, Movimiento.Agencia, Movimiento.Nombre, Movimiento.Descripcion, Movimiento.Total, Movimiento.Usuario, Movimiento.Forma);
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

        private DataTable GenerarHorario(List<HorarioModel> Horarios)
        {
            DataTable DTHorarios = new DataTable("Inventario");
            DTHorarios.Columns.Add(new DataColumn("PersonalId", typeof(long)));
            DTHorarios.Columns.Add(new DataColumn("Nombre", typeof(string)));           
            DTHorarios.Columns.Add(new DataColumn("Fecha", typeof(string)));
            DTHorarios.Columns.Add(new DataColumn("Entrada", typeof(string)));
            DTHorarios.Columns.Add(new DataColumn("Salida", typeof(string)));

            if (Horarios != null && Horarios.Count() > 0)
            {
                foreach (var Horario in Horarios)
                {
                    DTHorarios.Rows.Add(Horario.PersonaId, Horario.Nombre, Horario.Fecha.ToString("dd/MM/yyyy"), Horario.Entrada.ToShortTimeString(), Horario.Salida == null ? "0:00" : Horario.Salida.Value.ToShortTimeString());
                }
            }

            return DTHorarios;
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

                        string strAgenciaId = string.Empty;
                        string strPersonalId = string.Empty;
                        long AgenciaId = 0;
                        long PersonalId = 0;
                        long PrecioId = 0;
                        long ProveedorId = 0;
                        string ProductoId = string.Empty;

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

                                    strAgenciaId = Request.QueryString["CentroId"].ToString();

                                    if (!string.IsNullOrWhiteSpace(strAgenciaId))
                                    {
                                        AgenciaId = Convert.ToInt64(strAgenciaId);
                                    }

                                    Inventarios = new ProductoBL().ObtenerExistenciaPorPresentacion(AgenciaId, CustomHelper.getUserId(), PrecioId, false, false, false).Select(x =>
                                                         new ProductoInventarioModel
                                                         {
                                                             ProductoId = x.ProductoId, 
                                                             Codigo = x.Codigo,
                                                             Nombre = x.Nombre,
                                                             Agencia = x.Agencia,
                                                             Unidad = x.Unidad,
                                                             Existencia = x.Existencia,
                                                             Precio = x.Precio
                                                         }
                                                     ).ToList();

                                    dt = GenerarInventario(Inventarios);
                                    Reports = "~/Reports/ReportInventario.rdlc";

                                }
                                catch (Exception)
                                {
                                }


                                break;
                            case "ReportInventarioxPresentacion":

                                try
                                {

                                    strAgenciaId = Request.QueryString["CentroId"].ToString();

                                    if (!string.IsNullOrWhiteSpace(strAgenciaId))
                                    {
                                        AgenciaId = Convert.ToInt64(strAgenciaId);
                                    }

                                    Inventarios = new ProductoBL().ObtenerExistenciaPorPresentacion(AgenciaId, CustomHelper.getUserId(), PrecioId, true, false, false).Select(x =>
                                                         new ProductoInventarioModel
                                                         {
                                                             ProductoId = x.ProductoId,
                                                             Codigo = x.Codigo,
                                                             Nombre = x.Nombre,
                                                             Agencia = x.Agencia,
                                                             Unidad = x.Unidad,
                                                             Existencia = x.Existencia,
                                                             Precio = x.Precio
                                                         }
                                                     ).ToList();

                                    dt = GenerarInventario(Inventarios);
                                    Reports = "~/Reports/ReportInventarioxPresentacion.rdlc";

                                }
                                catch (Exception)
                                {
                                }


                                break;
                            case "ReportInventarioTransito":

                                try
                                {

                                    strAgenciaId = Request.QueryString["CentroId"].ToString();

                                    if (!string.IsNullOrWhiteSpace(strAgenciaId))
                                    {
                                        AgenciaId = Convert.ToInt64(strAgenciaId);
                                    }

                                    Inventarios = new ProductoBL().ObtenerExistenciaPorPresentacion(AgenciaId, CustomHelper.getUserId(), PrecioId, true, true, false).Select(x =>
                                                         new ProductoInventarioModel
                                                         {
                                                             ProductoId = x.ProductoId,
                                                             Codigo = x.Codigo,
                                                             Nombre = x.Nombre,
                                                             Agencia = x.Agencia,
                                                             Unidad = x.Unidad,
                                                             Existencia = x.Existencia,
                                                             Precio = x.Precio
                                                         }
                                                     ).ToList();

                                    dt = GenerarInventario(Inventarios);
                                    Reports = "~/Reports/ReportInventarioTransito.rdlc";

                                }
                                catch (Exception)
                                {
                                }


                                break;
                            case "ReportInventarioxPrecioVenta":

                                try
                                {

                                    strAgenciaId = Request.QueryString["CentroId"].ToString();
                                    string strPrecioId = Request.QueryString["PrecioId"].ToString();

                                    if (!string.IsNullOrWhiteSpace(strAgenciaId))
                                    {
                                        AgenciaId = Convert.ToInt64(strAgenciaId);
                                    }

                                    if (!string.IsNullOrWhiteSpace(strPrecioId))
                                    {
                                        PrecioId = Convert.ToInt64(strPrecioId);
                                    }

                                    Inventarios = new ProductoBL().ObtenerExistenciaPorPresentacion(AgenciaId, CustomHelper.getUserId(), PrecioId, true, false, true).Select(x =>
                                                         new ProductoInventarioModel
                                                         {
                                                             ProductoId = x.ProductoId,
                                                             Codigo = x.Codigo,
                                                             Nombre = x.Nombre,
                                                             Agencia = x.Agencia,
                                                             Unidad = x.Unidad,
                                                             Existencia = x.Existencia,
                                                             Precio = x.Precio
                                                         }
                                                     ).ToList();

                                    dt = GenerarInventario(Inventarios);
                                    Reports = "~/Reports/ReportInventarioxPrecioVenta.rdlc";

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
                                    strAgenciaId = Request.QueryString["CentroId"].ToString(); 

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                    if (!string.IsNullOrWhiteSpace(strAgenciaId))
                                    {
                                        AgenciaId = Convert.ToInt64(strAgenciaId);
                                    }

                                    //Detalle del Cierre
                                    Facturas = new FacturaBL().ObtenerFactura(FechaInicial, FechaFinal, AgenciaId, CustomHelper.getUserId()).Select(x =>
                                                         new FacturaModel
                                                         {
                                                            FacturaId = x.FacturaId,
                                                            Documento = x.Documento,
                                                            Fecha = x.Fecha,
                                                            Agencia = x.Agencia, 
                                                            Nombre = x.Nombre,
                                                            Descuento = x.Descuento,
                                                            Total = x.Total,
                                                            TotalLiquido = x.TotalLiquido,
                                                            Forma = x.Forma
                                                         }
                                                     ).ToList();

                                    //Resumen de Forma de Pago
                                    List<FormaPago> Formas = new FacturaBL().ObtenerFacturaPorFormaPago(FechaInicial, FechaFinal, AgenciaId, CustomHelper.getUserId()).ToList();
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

                                    if (Facturas != null && Facturas.Count() > 0)
                                    {
                                        foreach (var Factura in Facturas)
                                        {

                                            if (FacturaId ==  0)
                                            {
                                                FacturaId = Factura.FacturaId;
                                            }

                                            DTCierre.Rows.Add(Factura.FacturaId, Factura.Documento, Factura.Fecha, Factura.Agencia, Factura.Nombre, Factura.Descuento, Factura.Total, Factura.TotalLiquido, Factura.Forma);
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

                                    Reports = "~/Reports/ReportCierre.rdlc";
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
                                    strAgenciaId = Request.QueryString["CentroId"].ToString();

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;
                                 
                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                    if (!string.IsNullOrWhiteSpace(strAgenciaId))
                                    {
                                        AgenciaId = Convert.ToInt64(strAgenciaId);
                                    }

                                    Movimientos = new MovimientoBL().ObtenerMovimientoPorTipo(FechaInicial, FechaFinal, 1, AgenciaId, CustomHelper.getUserId(), ProveedorId, ProductoId).Select(x =>
                                                        new MovimientoModel
                                                        {
                                                            MovimientoId = x.MovimientoId,
                                                            Agencia = x.Agencia,                                                          
                                                            Nombre = x.Nombre,
                                                            Descripcion = x.Descripcion,
                                                            Total = x.Total,
                                                            Usuario = x.Usuario,
                                                            Forma = x.Forma
                                                        }
                                                    ).ToList();

                                    dt = GenerarMovimiento(Movimientos);
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
                                    strAgenciaId = Request.QueryString["CentroId"].ToString();
                                    string strProveedorId = Request.QueryString["ProveedorId"].ToString();

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                    if (!string.IsNullOrWhiteSpace(strAgenciaId))
                                    {
                                        AgenciaId = Convert.ToInt64(strAgenciaId);
                                    }

                                    if (!string.IsNullOrWhiteSpace(strProveedorId))
                                    {
                                        ProveedorId = Convert.ToInt64(strProveedorId);
                                    }

                                    Movimientos = new MovimientoBL().ObtenerMovimientoPorTipo(FechaInicial, FechaFinal, 1, AgenciaId, CustomHelper.getUserId(), ProveedorId, ProductoId).Select(x =>
                                                        new MovimientoModel
                                                        {
                                                            MovimientoId = x.MovimientoId,
                                                            Agencia = x.Agencia,
                                                            Nombre = x.Nombre,
                                                            Descripcion = x.Descripcion,
                                                            Total = x.Total,
                                                            Usuario = x.Usuario,
                                                            Forma = x.Forma
                                                        }
                                                    ).ToList();

                                    dt = GenerarMovimiento(Movimientos);
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
                                    strAgenciaId = Request.QueryString["CentroId"].ToString();
                                    string strProductoId = Request.QueryString["ProductoId"].ToString();

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                    if (!string.IsNullOrWhiteSpace(strAgenciaId))
                                    {
                                        AgenciaId = Convert.ToInt64(strAgenciaId);
                                    }

                                    if (!string.IsNullOrWhiteSpace(strProductoId))
                                    {
                                        ProductoId = strProductoId;
                                    }

                                    Movimientos = new MovimientoBL().ObtenerMovimientoPorTipo(FechaInicial, FechaFinal, 1, AgenciaId, CustomHelper.getUserId(), ProveedorId, ProductoId).Select(x =>
                                                        new MovimientoModel
                                                        {
                                                            MovimientoId = x.MovimientoId,
                                                            Agencia = x.Agencia,
                                                            Nombre = x.Nombre,
                                                            Descripcion = x.Descripcion,
                                                            Total = x.Total,
                                                            Usuario = x.Usuario,
                                                            Forma = x.Forma
                                                        }
                                                    ).ToList();

                                    dt = GenerarMovimiento(Movimientos);
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
                                    strAgenciaId = Request.QueryString["CentroId"].ToString();

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                    if (!string.IsNullOrWhiteSpace(strAgenciaId))
                                    {
                                        AgenciaId = Convert.ToInt64(strAgenciaId);
                                    }

                                    Movimientos = new MovimientoBL().ObtenerMovimientoPorTipo(FechaInicial, FechaFinal, 2, AgenciaId, CustomHelper.getUserId(), ProveedorId, ProductoId).Select(x =>
                                                        new MovimientoModel
                                                        {
                                                            MovimientoId = x.MovimientoId,
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

                                    if (Movimientos != null && Movimientos.Count() > 0)
                                    {
                                        foreach (var Movimiento in Movimientos)
                                        {
                                            if (MovimientoId == 0)
                                            {
                                                MovimientoId = Movimiento.MovimientoId;
                                            }

                                            DTMovimiento.Rows.Add(Movimiento.MovimientoId, Movimiento.Agencia, Movimiento.Nombre, Movimiento.Descripcion, Movimiento.Total, Movimiento.Usuario, Movimiento.Forma);
                                        }
                                    }

                                    //Resumen de Forma de Pago
                                    List<FormaPago> Formas = new MovimientoBL().ObtenerMovimientoPorFormaPago(FechaInicial, FechaFinal, AgenciaId, CustomHelper.getUserId()).ToList();

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
                                    strAgenciaId = Request.QueryString["CentroId"].ToString();

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                    if (!string.IsNullOrWhiteSpace(strAgenciaId))
                                    {
                                        AgenciaId = Convert.ToInt64(strAgenciaId);
                                    }

                                    Ganancias = new ProductoBL().ObtenerGananciaPorProductoVenta(FechaInicial, FechaFinal, AgenciaId, CustomHelper.getUserId()).Select(x =>
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
                                    Reports = "~/Reports/ReportGanancia.rdlc";
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
                                    strAgenciaId = Request.QueryString["CentroId"].ToString();

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                    if (!string.IsNullOrWhiteSpace(strAgenciaId))
                                    {
                                        AgenciaId = Convert.ToInt64(strAgenciaId);
                                    }

                                    Diarios = new DiarioBL().ObtenerDiarioPorFecha(FechaInicial, FechaFinal, AgenciaId, CustomHelper.getUserId(), false, false).Select(x =>
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
                                    strAgenciaId = Request.QueryString["CentroId"].ToString();

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                    if (!string.IsNullOrWhiteSpace(strAgenciaId))
                                    {
                                        AgenciaId = Convert.ToInt64(strAgenciaId);
                                    }

                                    Diarios = new DiarioBL().ObtenerDiarioPorFecha(FechaInicial, FechaFinal, AgenciaId, CustomHelper.getUserId(), true, false).Select(x =>
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
                                    strAgenciaId = Request.QueryString["CentroId"].ToString();

                                    DateTime FechaInicial = DateTime.Today;
                                    DateTime FechaFinal = DateTime.Today;

                                    if (!string.IsNullOrWhiteSpace(strFechaInicial) && !string.IsNullOrWhiteSpace(strFechaFinal))
                                    {
                                        FechaInicial = Convert.ToDateTime(strFechaInicial);
                                        FechaFinal = Convert.ToDateTime(strFechaFinal);
                                    }

                                    if (!string.IsNullOrWhiteSpace(strAgenciaId))
                                    {
                                        AgenciaId = Convert.ToInt64(strAgenciaId);
                                    }

                                    Diarios = new DiarioBL().ObtenerDiarioPorFecha(FechaInicial, FechaFinal, AgenciaId, CustomHelper.getUserId(), false, true).Select(x =>
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
                            case "ReportHorarios":

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

                                    List<HorarioModel> Horarios = new PersonalBL().ObtenerHorarioPersonalPorFecha(FechaInicial, FechaFinal);

                                    dt = GenerarHorario(Horarios);
                                    Reports = "~/Reports/ReportHorarios.rdlc";

                                }
                                catch (Exception)
                                {
                                }

                                break;
                            case "ReportHorario":

                                try
                                {
                                    string strFechaInicial = Request.QueryString["FechaInicial"].ToString();
                                    string strFechaFinal = Request.QueryString["FechaFinal"].ToString();
                                    strPersonalId = Request.QueryString["PersonalId"].ToString();

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

                                    List<HorarioModel> Horarios = new PersonalBL().ObtenerHorarioPersonalPorFecha(FechaInicial, FechaFinal, PersonalId);

                                    dt = GenerarHorario(Horarios);
                                    Reports = "~/Reports/ReportHorario.rdlc";
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

                        if (strReportID.Equals("ReportCierre"))
                        {
                            this.rptReport.LocalReport.DataSources.Add(new ReportDataSource("Cierre", Cierre.Tables[0]));
                            this.rptReport.LocalReport.DataSources.Add(new ReportDataSource("CierreForma", Cierre.Tables[1]));
                        }
                        else if (strReportID.Equals("ReportEgreso"))
                        {
                            this.rptReport.LocalReport.DataSources.Add(new ReportDataSource("Movimiento", Cierre.Tables[0]));
                            this.rptReport.LocalReport.DataSources.Add(new ReportDataSource("MovimientoForma", Cierre.Tables[1]));
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