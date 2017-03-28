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
using System.Collections;
using System.Data;
using Microsoft.Reporting.WebForms;
using DiamDev.Give.DAL;
using System.Data.Entity;

namespace DiamDev.Give.UI.Controllers
{
    [Authorize]
    [Seguridad]
    [HandleError]
    public class TrasladoController : Controller
    {
        #region Metodos Privados

        private void CargaControles()
        {
            var Agencias = new AgenciaBL().ObtenerListado(false, 0);
            var Productos = new ProductoBL().ObtenerListado(true, false, true);

            ViewBag.Agencias = new SelectList(Agencias, "AgenciaId", "Nombre");
            ViewBag.Productos = new SelectList(Productos, "ProductoId", "Nombre");
        }

        private byte[] GetReportBytes(string reportPath, DataSet reportDataSource, decimal pageWidth = 13.38m, decimal pageHeight = 8.5m, decimal MarginLeft = 1m, decimal MarginRight = 1m)
        {

            byte[] reportBytes = null;

            // Se crea la instancia del reporte y se cargan sus datos.
            LocalReport reporte = new LocalReport() { ReportPath = reportPath };
            reporte.DataSources.Add(new ReportDataSource("TrasladoEncabezado", reportDataSource.Tables[0]));
            reporte.DataSources.Add(new ReportDataSource("TrasladoDetalle", reportDataSource.Tables[1]));

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

        // GET: Traslado
        [Permiso("Control.Traslado.Ver_Listado")]
        public ActionResult Index(DateTime? FechaInicial, DateTime? FechaFinal)
        {
            CustomHelper.setTitle("Traslado", "Listado");

            List<Traslado> Traslados = new List<Traslado>();

            if (!FechaInicial.HasValue && !FechaFinal.HasValue)
            {
                FechaInicial = DateTime.Today;
                FechaFinal = DateTime.Today;
            }

            try
            {
                Traslados = new TrasladoBL().ObtenerListado(FechaInicial.Value, FechaFinal.Value).ToList();
            }
            catch (Exception ex)
            {
                ViewBag.Error = string.Format("Message: {0} StackTrace: {1}", ex.Message, ex.StackTrace);
                return View("~/Views/Shared/Error.cshtml");
            }

            return View(Traslados);
        }

        [Permiso("Control.Traslado.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Traslado", "Nuevo");

            this.CargaControles();
            return View();
        }

        [Permiso("Control.Traslado.Crear")]
        [HttpPost]
        public ActionResult Crear(Traslado modelo, string[] productoIds, long[] presentacionIds, decimal[] cantidadIds)
        {
            if (productoIds == null || productoIds.Length == 0)
            {
                ModelState.AddModelError("", "Para realizar un traslado debe de asignar productos");
            }
            else
            {
                modelo.Detalles = new List<TrasladoDetalle>();
                for (int i = 0; i < productoIds.Length; i++)
                {
                    TrasladoDetalle Detalle = new TrasladoDetalle();
                    Detalle.ProductoId = productoIds[i];
                    Detalle.UnidadId = presentacionIds[i];
                    Detalle.Cantidad = cantidadIds[i];

                    modelo.Detalles.Add(Detalle);
                }
            }

            if (ModelState.IsValid)
            {
                modelo.UsrInicial = CustomHelper.getUserId();
                string strMensaje = new TrasladoBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    using (var db = new GiveContext())
                    {
                        var agenciaOrigen = db.Agencias.FirstOrDefault(a => a.AgenciaId == modelo.AgenciaOrigenId);
                        var agenciaDestino = db.Agencias.FirstOrDefault(a => a.AgenciaId == modelo.AgenciaDestinoId);

                        if (agenciaOrigen != null && agenciaDestino != null)
                        {
                            foreach (var p in modelo.Detalles)
                            {
                                var productoId = p.ProductoId;
                                var producto = db.Productos.Include(pr => pr.Marca).FirstOrDefault(pr => pr.ProductoId == productoId);

                                if (producto == null) continue;

                                var fechaHora = DateTime.Now;
                                db.RegistrosKardex.Add(new RegistroKardex
                                {
                                    FechaHora = fechaHora,
                                    Fecha = DateTime.Today,
                                    ProductoId = p.ProductoId,
                                    ProductoCodigo = producto.Codigo,
                                    ProductoNombre = producto.Nombre,
                                    ProductoDescripcion = producto.Descripcion,
                                    MarcaId = producto.MarcaId,
                                    MarcaNombre = producto.Marca.Nombre,
                                    DocumentoNumero = modelo.TrasladoId.ToString(),
                                    AgenciaId = agenciaOrigen.AgenciaId,
                                    AgenciaNombre = agenciaOrigen.Nombre,
                                    TipoRegistro = "Taslado",
                                    SalidaCantidadTienda = p.Cantidad,
                                    SalidaCostoTienda = producto.PrecioActual
                                });

                                db.RegistrosKardex.Add(new RegistroKardex
                                {
                                    FechaHora = fechaHora,
                                    Fecha = DateTime.Today,
                                    ProductoId = p.ProductoId,
                                    ProductoCodigo = producto.Codigo,
                                    ProductoNombre = producto.Nombre,
                                    ProductoDescripcion = producto.Descripcion,
                                    MarcaId = producto.MarcaId,
                                    MarcaNombre = producto.Marca.Nombre,
                                    DocumentoNumero = modelo.TrasladoId.ToString(),
                                    AgenciaId = agenciaDestino.AgenciaId,
                                    AgenciaNombre = agenciaDestino.Nombre,
                                    TipoRegistro = "Taslado",
                                    IngresoCantidadTienda = p.Cantidad,
                                    IngresoCostoTienda = producto.PrecioActual
                                });
                            }

                            db.SaveChanges();
                        }
                    }

                    TempData["Traslado-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }

            }

            ViewBag.productoIds = productoIds;
            ViewBag.presentacionIds = presentacionIds;
            ViewBag.cantidadIds = cantidadIds;

            this.CargaControles();
            return View(modelo);
        }

        [Permiso("Control.Traslado.Detalle")]
        public ActionResult Detalle(long id)
        {
            Traslado TrasladoActual = new TrasladoBL().ObtenerPorId(id, true);

            if (TrasladoActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Traslado", "Detalle");

            return View(TrasladoActual);
        }

        [Permiso("Control.Reporte.Boleta_Traslado")]
        public ActionResult Boleta(long id)
        {
            Traslado TrasladoActual = new TrasladoBL().ObtenerPorId(id, true);

            if (TrasladoActual != null)
            {
                DataSet Traslado = new DataSet("Inventario");

                DataTable Encabezado = new DataTable("TrasladoEncabezado");
                DataTable Detalle = new DataTable("TrasladoDetalle");

                Encabezado.Columns.Add(new DataColumn("TrasladoId", typeof(long)));
                Encabezado.Columns.Add(new DataColumn("AgenciaOrigen", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("AgenciaDestino", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Descripcion", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Fecha", typeof(DateTime)));

                Encabezado.Rows.Add(TrasladoActual.TrasladoId, TrasladoActual.AgenciaOrigen.Nombre, TrasladoActual.AgenciaDestino.Nombre, TrasladoActual.Descripcion, TrasladoActual.Fecha);

                Detalle.Columns.Add(new DataColumn("TrasladoId", typeof(long)));
                Detalle.Columns.Add(new DataColumn("ProductoId", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Nombre", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Unidad", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Cantidad", typeof(decimal)));

                if (TrasladoActual.Detalles != null && TrasladoActual.Detalles.Count() > 0)
                {
                    foreach (var DetalleActual in TrasladoActual.Detalles)
                    {
                        Detalle.Rows.Add(TrasladoActual.TrasladoId, DetalleActual.ProductoId, DetalleActual.Producto.Nombre, DetalleActual.Unidad.Nombre, DetalleActual.Cantidad);
                    }
                }

                Traslado.Tables.Add(Encabezado);
                Traslado.Tables.Add(Detalle);

                // Se define la ruta del reporte
                var reportPath = Server.MapPath("~/Reports/ReportMovTraslado.rdlc");

                // se obtienen los bytes del reporte en pdf
                var bytes = GetReportBytes(reportPath, Traslado, 8.5m, 11.0m, 0.2m, 0m);

                return File(bytes, "application/pdf");

            }

            return View();
        }

        [ActionName("ObtenerAgenciaPorId")]
        public JsonResult AgenciaListado(long id)
        {
            IList _result = new List<SelectListItem>();
            _result = new AgenciaBL().ObtenerListadoPorAgencia(id).Select(m => new SelectListItem() { Text = m.Nombre, Value = m.AgenciaId.ToString() }).ToList();
            return Json(_result, JsonRequestBehavior.AllowGet);
        }
    }
}