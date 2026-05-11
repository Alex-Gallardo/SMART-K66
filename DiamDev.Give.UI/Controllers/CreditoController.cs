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
    public class CreditoController : Controller
    {
        #region Metodos Privados

            private void CargaControles()
            {
                var Tipos = new CreditoTipoBL().ObtenerListado();
                var Agencias = new AgenciaBL().ObtenerListadoPorUsuario(CustomHelper.getUserId());
               
                ViewBag.Tipos = new SelectList(Tipos, "CreditoTipoId", "Nombre");
                ViewBag.Agencias = new SelectList(Agencias, "AgenciaId", "Nombre");

                this.CargaDescuentos();
            }

            private void CargaDescuentos()
            {
                var Descuentos = new FacturaBL().ObtenerPorcentajeDescuento();

                ViewBag.Descuentos = new SelectList(Descuentos, "DescuentoId", "Valor");
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
                reporte.DataSources.Add(new ReportDataSource("CreditoEncabezado", reportDataSource.Tables[0]));
                reporte.DataSources.Add(new ReportDataSource("CreditoDetalle", reportDataSource.Tables[1]));

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

        // GET: Credito
        [Permiso("Control.Credito.Ver_Listado")]
        public ActionResult Index(int? page, string search)
        {
            CustomHelper.setTitle("Credito", "Listado");

            List<Credito> Creditos = new List<Credito>();

            try
            {
                if (!string.IsNullOrWhiteSpace(search) && search != null)
                {
                    Creditos = new CreditoBL().Buscar(search).ToList();
                }
                else
                {
                    Creditos = new CreditoBL().ObtenerListado().ToList();
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
            return View(Creditos.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Credito.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Credito", "Nuevo");

            this.CargaControles();
            return View();
        }

        [Permiso("Control.Credito.Crear")]
        [HttpPost]
        public ActionResult Crear(Credito modelo, string[] productoIds, string[] nombreProductoIds, long[] presentacionIds, string[] nombrePresentacionIds, decimal[] existenciaIds, decimal[] cantidadIds, decimal[] precioIds, decimal[] descuentoIds)
        {
            if (productoIds == null || productoIds.Length == 0)
            {
                ModelState.AddModelError("", "Para realizar un credito debe de asignar productos");
            }

            modelo.Detalles = new List<CreditoDetalle>();
            for (int i = 0; i < productoIds.Length; i++)
            {
                if (modelo.Detalles.Where(x => x.ProductoId == productoIds[i]).Count() > 0)
                {
                    foreach (var item in modelo.Detalles)
                    {
                        if (item.ProductoId == productoIds[i])
                        {
                            item.Cantidad += cantidadIds[i];
                            break;
                        }
                    }
                }
                else
                {
                    CreditoDetalle Detalle = new CreditoDetalle();
                    Detalle.ProductoId = productoIds[i];
                    Detalle.UnidadId = presentacionIds[i];
                    Detalle.Existencia = existenciaIds[i];
                    Detalle.Cantidad = cantidadIds[i];

                    Detalle.Descuento = descuentoIds[i];
                    Detalle.Precio = precioIds[i] - ((precioIds[i] * descuentoIds[i]) / 100);

                    modelo.Detalles.Add(Detalle);
                }
            }

            if (modelo.Detalles != null && modelo.Detalles.Count() > 0)
            {
                bool ExistenciaNoValida = modelo.Detalles.Where(x => x.Cantidad > x.Existencia).Count() > 0;
                if (ExistenciaNoValida)
                {
                    ModelState.AddModelError("", "Hay producto(s) que sobre pasan las existencias");
                }
            }

            if (ModelState.IsValid)
            {
                modelo.UsrInicial = CustomHelper.getUserId();
                string strMensaje = new CreditoBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Credito-Success"] = strMensaje;
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
            ViewBag.existenciaIds = existenciaIds;
            ViewBag.cantidadIds = cantidadIds;
            ViewBag.descuentoIds = descuentoIds;
            ViewBag.precioIds = precioIds;

            this.CargaControles();
            return View(modelo);
        }

        [Permiso("Control.Credito.Editar")]
        public ActionResult Editar(long id)
        {
            Credito CreditoActual = new CreditoBL().ObtenerPorId(id, true);

            if (CreditoActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Credito", "Editar");

            this.CargaDescuentos();
            return View(CreditoActual);
        }

        [Permiso("Control.Credito.Detalle")]
        public ActionResult Detalle(long id)
        {
            Credito CreditoActual = new CreditoBL().ObtenerPorId(id, true);

            if (CreditoActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Credito", "Detalle");

            return View(CreditoActual);
        }

        [Permiso("Control.Credito.Pago")]
        public ActionResult Pago(long id)
        {
            Credito CreditoActual = new CreditoBL().ObtenerPorId(id, true);

            if (CreditoActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Credito", "Pago");
                      
            this.CargaFormas();
            return View(CreditoActual);
        }

        [Permiso("Control.Credito.Pago")]
        [HttpPost]
        public ActionResult Pago(Credito modelo, long[] formaIds, decimal[] pagarIds, string[] notaIds)
        {
            Credito CreditoActual = new CreditoBL().ObtenerPorId(modelo.CreditoId, true);

            if (formaIds == null || formaIds.Length == 0)
            {
                ModelState.AddModelError("", "Para realizar un pago, debera seleccionar la forma de pago");
            }
            else
            {
                modelo.Pagos = new List<CreditoPago>();
                for (int i = 0; i < formaIds.Length; i++)
                {
                    CreditoPago Forma = new CreditoPago();
                    Forma.FormaPagoId = formaIds[i];
                    Forma.Valor = pagarIds[i];
                    Forma.Nota = notaIds[i];
                                        
                    Forma.CreditoId = modelo.CreditoId;
                    Forma.UsrOperacionId = CustomHelper.getUserId();

                    modelo.Pagos.Add(Forma);
                }
            }

            string strMensaje = new CreditoBL().GenerarPago(modelo, CustomHelper.getUserId(), modelo.Pagos);
            if (strMensaje.Equals("OK"))
            {
                TempData["Pago-Success"] = strMensaje;
                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("", strMensaje);
            }

            ViewBag.formaIds = formaIds;
            ViewBag.pagarIds = pagarIds;         
            ViewBag.notaIds = notaIds;

            this.CargaFormas();
            return View(CreditoActual);
        }

        [Permiso("Control.Credito.Anular")]
        public ActionResult Anular(long id)
        {
            Credito CreditoActual = new CreditoBL().ObtenerPorId(id, true);

            if (CreditoActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Credito", "Anular");

            return View(CreditoActual);
        }

        [Permiso("Control.Credito.Anular")]
        [HttpPost]
        public ActionResult Anular(long creditoId, string comentario)
        {
            string strMensaje = new CreditoBL().Anular(creditoId, comentario, CustomHelper.getUserId());
            if (strMensaje.Equals("OK"))
            {
                TempData["Credito_Anular-Success"] = strMensaje;
                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("", strMensaje);
            }

            Credito CreditoActual = new CreditoBL().ObtenerPorId(creditoId, true);

            if (CreditoActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Credito", "Anular");

            return View(CreditoActual);
        }

        [Permiso("Control.Credito.Consulta")]
        public ActionResult Consulta(long? ClienteId, string NombreCliente)
        {
            CustomHelper.setTitle("Credito", "Consulta");

            List<Credito> Creditos = new List<Credito>();

            try
            {

                if (!ClienteId.HasValue)
                {
                    ClienteId = 0;
                }

                Creditos = new CreditoBL().ObtenerListado(ClienteId).ToList();
            }
            catch (Exception ex)
            {
                ViewBag.Error = string.Format("Message: {0} StackTrace: {1}", ex.Message, ex.StackTrace);
                return View("~/Views/Shared/Error.cshtml");
            }

            ViewBag.NombreCliente = NombreCliente;

            return View(Creditos);
        }

        [Permiso("Control.Reporte.Boleta_Credito")]
        public ActionResult Boleta(long id)
        {
            Credito CreditoActual = new CreditoBL().ObtenerPorId(id, true);

            if (CreditoActual != null)
            {
                DataSet Credito = new DataSet("Inventario");

                DataTable Encabezado = new DataTable("CreditoEncabezado");
                DataTable Detalle = new DataTable("CreditoDetalle");

                Encabezado.Columns.Add(new DataColumn("CreditoId", typeof(long)));
                Encabezado.Columns.Add(new DataColumn("Tipo", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Agencia", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Nombre", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Direccion", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Descripcion", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Fecha", typeof(DateTime)));
              
                string Nombre = string.Empty;
                string Direccion = string.Empty;

                if (CreditoActual.TipoId >= 1 && CreditoActual.TipoId <= 4)
                {
                    Nombre = CreditoActual.Cliente.Nombre;
                    Direccion = CreditoActual.Cliente.Direccion;
                }
                              
                Encabezado.Rows.Add(CreditoActual.CreditoId, CreditoActual.Tipo.Nombre, CreditoActual.Agencia.Nombre, Nombre, Direccion, CreditoActual.Descripcion, CreditoActual.Fecha);

                Detalle.Columns.Add(new DataColumn("CreditoId", typeof(long)));
                Detalle.Columns.Add(new DataColumn("ProductoId", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Nombre", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Cantidad", typeof(int)));
                Detalle.Columns.Add(new DataColumn("Precio", typeof(decimal)));

                if (CreditoActual.Detalles != null && CreditoActual.Detalles.Count() > 0)
                {
                    foreach (var DetalleActual in CreditoActual.Detalles)
                    {
                        Detalle.Rows.Add(CreditoActual.CreditoId, DetalleActual.ProductoId, DetalleActual.Producto.Nombre, DetalleActual.Cantidad, DetalleActual.Precio);
                    }
                }

                Credito.Tables.Add(Encabezado);
                Credito.Tables.Add(Detalle);

                // Se define la ruta del reporte
                var reportPath = Server.MapPath("~/Reports/ReportMovCredito.rdlc");

                // se obtienen los bytes del reporte en pdf
                var bytes = GetReportBytes(reportPath, Credito, 8.5m, 11.0m, 0.2m, 0m);

                return File(bytes, "application/pdf");

            }

            return View();
        }

        [HttpPost]
        [ActionName("EliminarPieza")]
        public JsonResult EliminarPieza(long creditoId, long agenciaId, string productoId)
        {
            return Json(new { Operacion = new CreditoBL().EliminarPieza(creditoId, agenciaId, productoId) }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ActionName("NuevoProducto")]
        public JsonResult NuevoProducto(CreditoDetalle modelo)
        {
            return Json(new { Operacion = new CreditoBL().NuevoProductoCredito(modelo) }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ActionName("NuevoComentario")]
        public JsonResult NuevoComentario(long CreditoId, string Comentario)
        {
            return Json(new { Operacion = new CreditoAnotacionBL().Guardar(new CreditoAnotacion() { CreditoId = CreditoId, Comentario = Comentario, UsrAnotacion = CustomHelper.getUserId() }) }, JsonRequestBehavior.AllowGet);
        }
    }
}