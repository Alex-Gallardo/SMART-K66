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
    public class GarantiaController : Controller
    {
        #region Metodos Privados

            private void CargaControles()
            {
                var Documentos = new GarantiaDocumentoBL().ObtenerListado();
                var Series = new SerieBL().ObtenerSeriesPorAgencia(CustomHelper.getAgenciaId());

                ViewBag.Series = new SelectList(Series, "SerieId", "Nombre");
                ViewBag.Documentos = new SelectList(Documentos, "DocumentoId", "Nombre");
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

        // GET: Garantia
        [Permiso("Control.Garantia.Ver_Listado")]
        public ActionResult Index(int? page, string search)
        {
            CustomHelper.setTitle("Garantia", "Listado");

            List<Garantia> Garantias = new List<Garantia>();

            try
            {
                if (!string.IsNullOrWhiteSpace(search) && search != null)
                {
                    Garantias = new GarantiaBL().Buscar(search).ToList();
                }
                else
                {
                    Garantias = new GarantiaBL().ObtenerListado().ToList();
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = string.Format("Message: {0} StackTrace: {1}", ex.Message, ex.StackTrace);
                return View("~/Views/Shared/Error.cshtml");
            }

            ViewBag.Search = search;

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View(Garantias.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Garantia.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Garantia", "Nueva");

            this.CargaControles();
            return View();
        }

        [Permiso("Control.Garantia.Crear")]
        [HttpPost]
        public ActionResult Crear(Garantia modelo, string[] productoIds, string[] nombreProductoIds, string[] idIds)
        {
            if (productoIds == null || productoIds.Length == 0)
            {
                ModelState.AddModelError("", "Para realizar una garantia debe de asignar productos");
            }
            else
            {
                modelo.Detalles = new List<GarantiaDetalle>();
                for (int i = 0; i < productoIds.Length; i++)
                {
                    GarantiaDetalle Detalle = new GarantiaDetalle();
                    Detalle.ProductoId = productoIds[i];                
                    Detalle.ID = idIds[i];

                    modelo.Detalles.Add(Detalle);
                }
            }

            modelo.UsrCreo = CustomHelper.getUserId();

            string strMensaje = new GarantiaBL().Guardar(modelo);

            if (strMensaje.Equals("OK"))
            {
                TempData["Garantia-Success"] = strMensaje;
                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("", strMensaje);
            }

            ViewBag.productoIds = productoIds;
            ViewBag.nombreProductoIds = nombreProductoIds;        
            ViewBag.idIds = idIds;

            this.CargaControles();
            return View(modelo);
        }

        [Permiso("Control.Garantia.Detalle")]
        public ActionResult Detalle(long id)
        {
            Garantia GarantiaActual = new GarantiaBL().ObtenerPorId(id, true);

            if (GarantiaActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Garantia", "Detalle");

            return View(GarantiaActual);
        }

        [Permiso("Control.Garantia.Entrega")]
        public ActionResult Entrega(long id)
        {
            Garantia GarantiaActual = new GarantiaBL().ObtenerPorId(id, true);

            if (GarantiaActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Garantia", "Entrega");

            return View(GarantiaActual);
        }

        [Permiso("Control.Garantia.Entrega")]
        [HttpPost]
        public ActionResult Entrega(Garantia modelo)
        {
            string strMensaje = new GarantiaBL().Entrega(modelo.GarantiaId, CustomHelper.getUserId());

            if (strMensaje.Equals("OK"))
            {
                TempData["Garantia_Entrega-Success"] = strMensaje;
                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("", strMensaje);
            }

            Garantia GarantiaActual = new GarantiaBL().ObtenerPorId(modelo.GarantiaId, true);

            if (GarantiaActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Garantia", "Entrega");

            return View(GarantiaActual);
        }

        [Permiso("Control.Reporte.Boleta_Garantia")]
        public ActionResult Boleta(long id)
        {
            Garantia GarantiaActual = new GarantiaBL().ObtenerPorId(id, true);

            if (GarantiaActual != null)
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
                Encabezado.Columns.Add(new DataColumn("Categoria", typeof(string)));

                string Documento = string.Empty;
                string Cliente = string.Empty;
                string Direccion = string.Empty;

                if (GarantiaActual.DocumentoId == 1)
                {
                    Documento = string.Format("{0} - {1}", GarantiaActual.Factura.Serie.Nombre, GarantiaActual.Factura.NoFactura);
                    Cliente = GarantiaActual.Factura.Cliente.Nombre;
                    Direccion = GarantiaActual.Factura.Cliente.Direccion;
                }
                else if (GarantiaActual.DocumentoId == 2)
                {
                    Documento = string.Format("REC - {0}", GarantiaActual.Recibo.ReciboId);
                    Cliente = GarantiaActual.Recibo.Cliente.Nombre;
                    Direccion = GarantiaActual.Recibo.Cliente.Direccion;
                }

                Encabezado.Rows.Add(GarantiaActual.GarantiaId, Documento, Cliente, Direccion, GarantiaActual.Observaciones, GarantiaActual.Fecha.ToString("dd/MM/yyyy"), "");

                Detalle.Columns.Add(new DataColumn("MovimientoId", typeof(long)));
                Detalle.Columns.Add(new DataColumn("ProductoId", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Nombre", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Presentacion", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Cantidad", typeof(decimal)));
                Detalle.Columns.Add(new DataColumn("Precio", typeof(decimal)));
                Detalle.Columns.Add(new DataColumn("Minimo", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Maximo", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Marca", typeof(string)));

                if (GarantiaActual.Detalles != null && GarantiaActual.Detalles.Count() > 0)
                {
                    foreach (var DetalleActual in GarantiaActual.Detalles)
                    {
                        Detalle.Rows.Add(GarantiaActual.GarantiaId, DetalleActual.ProductoId, string.Format("{0} - {1}", DetalleActual.Producto.Codigo, DetalleActual.Producto.Nombre), DetalleActual.Unidad.Nombre, 0, 0, 0, 0, DetalleActual.ID);
                    }
                }

                Movimiento.Tables.Add(Encabezado);
                Movimiento.Tables.Add(Detalle);

                // Se define la ruta del reporte
                var reportPath = Server.MapPath("~/Reports/ReportMovGarantia.rdlc");

                // se obtienen los bytes del reporte en pdf
                var bytes = GetReportBytes(reportPath, Movimiento, 8.5m, 11.0m, 0.2m, 0m);

                return File(bytes, "application/pdf");
            }

            return View();
        }
    }
}