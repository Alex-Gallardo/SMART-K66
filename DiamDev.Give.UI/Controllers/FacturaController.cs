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
using System.Data;
using Microsoft.Reporting.WebForms;

namespace DiamDev.Give.UI.Controllers
{
    [Authorize]
    [Seguridad]
    [HandleError]
    public class FacturaController : Controller
    {       
        #region Metodos Privados

            private void CargaControles()
            {              
                var Clientes = new ClienteBL().ObtenerListado(false, true);
                var Vendedores = new VendedorBL().ObtenerVendedoresPorAgencia(CustomHelper.getAgenciaId());
                var Series = new SerieBL().ObtenerSeriesPorAgencia(CustomHelper.getAgenciaId());

                ViewBag.Clientes = new SelectList(Clientes, "ClienteId", "Nombre");
                ViewBag.Vendedores = new SelectList(Vendedores, "VendedorId", "Nombre");
                ViewBag.Series = new SelectList(Series, "SerieId", "Nombre");
                              
                this.CargaFormas();
            }          

            private void CargaFormas()
            {
                var Formas = new FormaPagoBL().ObtenerListado(false);

                ViewBag.Formas = new SelectList(Formas, "FormaPagoId", "Nombre");
            }

            private byte[] GetReportBytes(string reportPath, DataSet reportDataSource, decimal pageWidth = 13.38m, decimal pageHeight = 8.5m, decimal MarginLeft = 1m, decimal MarginRight = 1m)
            {

                byte[] reportBytes = null;

                // Se crea la instancia del reporte y se cargan sus datos.
                LocalReport reporte = new LocalReport() { ReportPath = reportPath };
                reporte.DataSources.Add(new ReportDataSource("MovimientoEncabezado", reportDataSource.Tables[0]));
                reporte.DataSources.Add(new ReportDataSource("MovimientoDetalle", reportDataSource.Tables[1]));

                string deviceInfo =
                    "<DeviceInfo>" +
                    "  <OutputFormat>PDF</OutputFormat>" + // Formato del documento PDF
                    "  <PageWidth>" + pageWidth + "in</PageWidth>" + // Ancho de 8.5 pulgadas para paginas oficio
                    "  <PageHeight>" + pageHeight + "in</PageHeight>" + // Alto de 13.38 pulgadas para paginas oficio
                    "  <MarginTop>0.5in</MarginTop>" + // margen superior de 0.5 pulgadas
                    "  <MarginLeft>" + MarginLeft + "</MarginLeft>" + // margen izquierdo de 1 pulgada
                    "  <MarginRight>" + MarginRight + "</MarginRight>" + // margen derecho de 1 pulgada.
                    "  <MarginBottom>0.5in</MarginBottom>" + // margen inferior de 0.5 pulgadas.
                    "</DeviceInfo>";

                string mimeType;
                string encoding;
                string fileNameExtension;
                Warning[] warnings;
                string[] streams;

                // Se renderiza el reporte.
                reportBytes = reporte.Render("PDF",
                    deviceInfo,
                    out mimeType,
                    out encoding,
                    out fileNameExtension,
                    out streams,
                    out warnings);

                return reportBytes;
            }

        #endregion

        // GET: Factura
        [Permiso("Control.Factura.Ver_Listado")]
        public ActionResult Index(DateTime? FechaInicial, DateTime? FechaFinal)
        {
            CustomHelper.setTitle("Factura", "Listado");

            List<Factura> Facturas = new List<Factura>();

            if (!FechaInicial.HasValue && !FechaFinal.HasValue)
            {
                FechaInicial = DateTime.Today;
                FechaFinal = DateTime.Today;
            }

            try
            {
                Facturas = new FacturaBL().ObtenerListadoPorFecha(FechaInicial.Value, FechaFinal.Value, CustomHelper.getUserId()).ToList();
            }
            catch (Exception)
            {
            }

            return View(Facturas);
        }

        [Permiso("Control.Factura.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Factura", "Nueva");         

            this.CargaControles();
            return View();
        }

        [Permiso("Control.Factura.Crear")]
        [HttpPost]
        public ActionResult Crear(Factura modelo, string[] productoIds, string[] nombreProductoIds, long[] presentacionIds, string[] nombrePresentacionIds, decimal[] cantidadIds, decimal[] precioIds, long[] formaIds, decimal[] pagarIds, string[] notaIds)
        {
            if (productoIds == null || productoIds.Length == 0)
            {
                ModelState.AddModelError("", "Para realizar una venta debe de asignar productos");
            }

            if (formaIds == null || formaIds.Length == 0)
            {
                ModelState.AddModelError("", "Para realizar una factura debe de ingresar la forma de pago");
            }
            else
            {
                modelo.Pagos = new List<FacturaFormaPago>();
                for (int i = 0; i < formaIds.Length; i++)
                {
                    FacturaFormaPago Forma = new FacturaFormaPago();
                    Forma.FormaPagoId = formaIds[i];
                    Forma.Valor = pagarIds[i];
                    Forma.Nota = notaIds[i];

                    modelo.Pagos.Add(Forma);
                }
            }

            if (modelo.NoFactura == 0)
            {
                ModelState.AddModelError("", "Para realizar una venta debe de asignar un no. de factura");
            }

            modelo.AgenciaId = CustomHelper.getAgenciaId();
            modelo.UsrCreo = CustomHelper.getUserId();
            modelo.FacturaElectronica = false;

            if (ModelState.IsValid)
            {
                modelo.Detalles = new List<FacturaDetalle>();
                for (int i = 0; i < productoIds.Length; i++)
                {
                    FacturaDetalle Detalle = new FacturaDetalle();
                    Detalle.ProductoId = productoIds[i];
                    Detalle.UnidadId = presentacionIds[i];
                    Detalle.Cantidad = cantidadIds[i];
                    Detalle.Precio = precioIds[i];

                    modelo.Detalles.Add(Detalle);
                }

                string strMensaje = new FacturaBL().Guardar(modelo);
                if (strMensaje.Equals("OK"))
                {
                    TempData["Factura-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }
                      
            ViewBag.productoIds = productoIds;
            ViewBag.nombreProductoIds = nombreProductoIds;
            ViewBag.presentacionIds = presentacionIds;
            ViewBag.nombrePresentacionIds = nombrePresentacionIds;
            ViewBag.cantidadIds = cantidadIds;
            ViewBag.precioIds = precioIds;

            ViewBag.formaIds = formaIds;
            ViewBag.pagarIds = pagarIds;
            ViewBag.notaIds = notaIds;
            
            this.CargaControles();
            return View(modelo);
        }


        [Permiso("Control.Factura.Anular")]
        public ActionResult Anular(long id)
        {
            Factura FacturaActual = new FacturaBL().ObtenerPorId(id, true, true, false);

            if (FacturaActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Factura", "Anular");

            return View(FacturaActual);
        }

        [Permiso("Control.Factura.Anular")]
        [HttpPost]
        public ActionResult Anular(long facturaId, string comentario)
        {
            string strMensaje = new FacturaBL().Anular(facturaId, comentario, CustomHelper.getUserId());
            if (strMensaje.Equals("OK"))
            {
                TempData["Factura_Anular-Success"] = strMensaje;
                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("", strMensaje);
            }

            Factura FacturaActual = new FacturaBL().ObtenerPorId(facturaId, true, true, false);

            if (FacturaActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Factura", "Anular");

            return View(FacturaActual);
        }

        [Permiso("Control.Factura.Detalle")]
        public ActionResult Detalle(long id)
        {
            Factura FacturaActual = new FacturaBL().ObtenerPorId(id, true, true, false);

            if (FacturaActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Factura", "Detalle");

            return View(FacturaActual);
        }        

        [Permiso("Control.Reporte.Boleta_Factura")]
        public ActionResult Boleta(long Id)
        {
            Factura FacturaActual = new FacturaBL().ObtenerPorId(Id, true, true, false, true);

            if (FacturaActual != null)
            {
                DataSet Movimiento = new DataSet("Inventario");

                DataTable Encabezado = new DataTable("MovimientoEncabezado");
                DataTable Detalle = new DataTable("MovimientoDetalle");

                Encabezado.Columns.Add(new DataColumn("MovimientoId", typeof(long)));
                Encabezado.Columns.Add(new DataColumn("Agencia", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Nombre", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Direccion", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Descripcion", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Fecha", typeof(DateTime)));
                Encabezado.Columns.Add(new DataColumn("Descuento", typeof(decimal)));
                Encabezado.Columns.Add(new DataColumn("Total", typeof(decimal)));

                Encabezado.Rows.Add(FacturaActual.FacturaId, FacturaActual.Agencia.Nombre, FacturaActual.Cliente.Nombre, FacturaActual.Cliente.Direccion, FacturaActual.Cliente.Nit, FacturaActual.Fecha.ToString("dd/MM/yyyy"), FacturaActual.DescuentoTotal, FacturaActual.Total);

                Detalle.Columns.Add(new DataColumn("MovimientoId", typeof(long)));
                Detalle.Columns.Add(new DataColumn("ProductoId", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Nombre", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Presentacion", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Cantidad", typeof(int)));
                Detalle.Columns.Add(new DataColumn("Precio", typeof(decimal)));

                if (FacturaActual.Detalles != null && FacturaActual.Detalles.Count() > 0)
                {
                    foreach (var DetalleActual in FacturaActual.Detalles)
                    {
                        Detalle.Rows.Add(FacturaActual.FacturaId, DetalleActual.ProductoId, string.Format("{0} - {1}", DetalleActual.Producto.Codigo, DetalleActual.Producto.Nombre), DetalleActual.Unidad.Nombre, DetalleActual.Cantidad, DetalleActual.Precio);
                    }
                }

                Movimiento.Tables.Add(Encabezado);
                Movimiento.Tables.Add(Detalle);

                // Se define la ruta del reporte
                var reportPath = Server.MapPath("~/Reports/ReportMovFactura.rdlc");

                // se obtienen los bytes del reporte en pdf
                var bytes = GetReportBytes(reportPath, Movimiento, 8.5m, 11.0m, 0.2m, 0m);
           
                return File(bytes, "application/pdf");
            }

            return View();
        }

        [ActionName("ObtenerFacturaActual")]
        public JsonResult ObtenerFactura(long serieId)
        {
            if (serieId > 0)
            {
                SerieAgenciaFactura FacturaActual = new SerieBL().ObtenerFacturaActual(CustomHelper.getAgenciaId(), serieId);
                if (FacturaActual != null && FacturaActual.Factura > 0)
                {
                    return Json(new { Operacion = true, Data = FacturaActual }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }
    }
}