using DiamDev.Give.BLL;
using DiamDev.Give.Entities;
using DiamDev.Give.UI.App_Start;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PagedList;
using System.Data;
using Microsoft.Reporting.WebForms;
using System.Collections;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;

namespace DiamDev.Give.UI.Controllers
{    
    [HandleError]
    public class InicioController : Controller
    {
        #region Metodos Privados

            private void CargaControles()
            {                
                var Agencias = new AgenciaBL().ObtenerListadoPorUsuario(null);
                
                ViewBag.Agencias = new SelectList(Agencias, "AgenciaId", "Nombre");                
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

        // GET: Inicio
        [Authorize]
        public ActionResult Dashboard()
        {
            CustomHelper.setTitle("Dashboard", "Inicio");
            return View();
        }

        [Authorize]
        public ActionResult Agencias(int? page, string search)
        {
            CustomHelper.setTitle("Agencias", "Listado");

            List<Agencia> Agencias = new List<Agencia>();

            try
            {
                if (!string.IsNullOrWhiteSpace(search) && search != null)
                {
                    Agencias = new AgenciaBL().Buscar(search, CustomHelper.getUserId()).ToList();
                }
                else
                {
                    Agencias = new AgenciaBL().ObtenerListado(true, CustomHelper.getUserId()).ToList();
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = string.Format("Message: {0} StackTrace: {1}", ex.Message, ex.StackTrace);
                return View("~/Views/Shared/Error.cshtml");
            }

            ViewBag.Search = search;

            int pageSize = 5;
            int pageNumber = (page ?? 1);
            return View(Agencias.ToPagedList(pageNumber, pageSize));
        }

        [Authorize]
        [HttpPost]
        public ActionResult Agencias(long? agenciaId)
        {
            if (agenciaId.HasValue)
            {
                Agencia AgenciaActual = new AgenciaBL().ObtenerPorId(agenciaId.Value);

                if (AgenciaActual != null)
                {
                    CustomHelper.setAgencia(AgenciaActual);

                    return RedirectToAction("Dashboard", "Inicio");
                }
            }
            else
            {
                ModelState.AddModelError("", "Debe seleccionar una agencia");
                return RedirectToAction("Agencias", "Inicio");
            }

            return View();
        }

        public ActionResult Producto(string search)
        {
            CustomHelper.setTitle("Producto x Codigo", "Consulta");
            Producto ProductoActual = new Producto();

            if (!string.IsNullOrWhiteSpace(search) && search != null)
            {
                ProductoActual = new ProductoBL().ObtenerProductoxCodigo(search);
            }

            return View(ProductoActual);
        }

        public ActionResult Pedido()
        {
            CustomHelper.setTitle("Pedido", "Nuevo");

            this.CargaControles();
            return View();
        }
       
        [HttpPost]
        public ActionResult Pedido(Pedido modelo, string[] productoIds, string[] nombreProductoIds, long[] presentacionIds, string[] nombrePresentacionIds, decimal[] existenciaIds, decimal[] cantidadIds, decimal[] precioIds)
        {
            if (productoIds == null || productoIds.Length == 0)
            {
                ModelState.AddModelError("", "Para realizar una venta debe de asignar productos");
            }
                        
            modelo.UsrCreo = 20161012002;
            modelo.Detalles = new List<PedidoDetalle>();

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
                    PedidoDetalle Detalle = new PedidoDetalle();
                    Detalle.ProductoId = productoIds[i];
                    Detalle.UnidadId = presentacionIds[i];
                    Detalle.Existencia = existenciaIds[i];
                    Detalle.Cantidad = cantidadIds[i];

                    Detalle.Descuento = 0;
                    Detalle.Precio = precioIds[i];

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
                long PedidoId = new PedidoBL().AgregarAnonimo(modelo);
                if (PedidoId > 0)
                {
                    TempData["Pedido-Success"] = "OK";                    
                    return RedirectToAction("Boleta", "Inicio", new { Id = PedidoId });
                }
                else
                {
                    ModelState.AddModelError("", "El pedido no se pudo generar por favor intente de nuevo");
                }
            }

            ViewBag.productoIds = productoIds;
            ViewBag.nombreProductoIds = nombreProductoIds;
            ViewBag.presentacionIds = presentacionIds;
            ViewBag.nombrePresentacionIds = nombrePresentacionIds;
            ViewBag.existenciaIds = existenciaIds;
            ViewBag.cantidadIds = cantidadIds;
            ViewBag.precioIds = precioIds;

            this.CargaControles();
            return View(modelo);
        }

        public ActionResult Boleta(long Id)
        {
            Pedido PedidoActual = new PedidoBL().ObtenerPorId(Id, true);

            if (PedidoActual != null)
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

                Encabezado.Rows.Add(PedidoActual.PedidoId, PedidoActual.Agencia.Nombre, PedidoActual.Cliente.Nombre, PedidoActual.Cliente.Direccion, PedidoActual.Descripcion, PedidoActual.Fecha.ToString("dd/MM/yyyy"), 0, 0);

                Detalle.Columns.Add(new DataColumn("MovimientoId", typeof(long)));
                Detalle.Columns.Add(new DataColumn("ProductoId", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Nombre", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Presentacion", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Cantidad", typeof(int)));
                Detalle.Columns.Add(new DataColumn("Precio", typeof(decimal)));

                if (PedidoActual.Detalles != null && PedidoActual.Detalles.Count() > 0)
                {
                    foreach (var DetalleActual in PedidoActual.Detalles)
                    {
                        Detalle.Rows.Add(PedidoActual.PedidoId, DetalleActual.ProductoId, string.Format("{0} - {1}", DetalleActual.Producto.Codigo, DetalleActual.Producto.Nombre), DetalleActual.Unidad.Nombre, DetalleActual.Cantidad, DetalleActual.Precio);
                    }
                }

                Movimiento.Tables.Add(Encabezado);
                Movimiento.Tables.Add(Detalle);

                // Se define la ruta del reporte
                var reportPath = Server.MapPath("~/Reports/ReportMovPedidoAnonimo.rdlc");

                // se obtienen los bytes del reporte en pdf
                var bytes = GetReportBytes(reportPath, Movimiento, 8.5m, 11.0m, 0.2m, 0m);

                return File(bytes, "application/pdf");
            }

            return View();
        }

        [ActionName("ObtenerPorNit")]
        public JsonResult ObtenerPorNit(string nit)
        {
            if (string.IsNullOrWhiteSpace(nit))
            {
                return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
            }

            var cliente = new ClienteBL().ObtenerPorNit(nit, 0);

            if (cliente == null)
            {
                return Json(new { Operacion = true, Data = (object)null }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { Operacion = true, Data = new { cliente.ClienteId, cliente.Nit, cliente.Nombre, cliente.Direccion, cliente.DPI, cliente.NoTelefono, cliente.EmailCliente, cliente.Vip, cliente.Activo } }, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerProducto")]
        public JsonResult ObtenerProducto(long agenciaId, string productoId, long presentacionId, bool empleado)
        {
            if (!string.IsNullOrWhiteSpace(productoId))
            {
                Producto ProductoActual = new ProductoBL().ObtenerExistenciaPorAgenciaYProducto(agenciaId, productoId, presentacionId, true, empleado);
                if (ProductoActual != null)
                {
                    return Json(new { Operacion = true, Data = ProductoActual }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult ConsultaProductoAutocomplementar(string search)
        {
            List<Producto> Productos = new ProductoBL().BuscarProductoxAutocompletar(search);
            return Json(Productos, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerFotografiaProducto")]
        public JsonResult ObtenerFotografiaProducto(string productoId)
        {
            if (!string.IsNullOrWhiteSpace(productoId))
            {
                ProductoFotografia FotografiaActual = new ProductoBL().Fotografia(1, productoId);
                if (FotografiaActual != null)
                {
                    return Json(new { Operacion = true, Data = string.Format("data:{0};base64,{1}", FotografiaActual.ContentType, Convert.ToBase64String(FotografiaActual.Content)) }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerProductoxTextoLibre")]
        public JsonResult ObtenerProductoxTextoLibre(string search)
        {
            if (!string.IsNullOrWhiteSpace(search))
            {
                List<Producto> Productos = new ProductoBL().BuscarProductoxTextoLibre(search);
                if (Productos != null && Productos.Count() > 0)
                {
                    return Json(new { Operacion = true, Data = Productos }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerPresentacionPorProducto")]
        public JsonResult PresentacionListado(string id)
        {
            IList _result = new List<SelectListItem>();
            _result = new ProductoBL().ObtenerPresentacionPorProductoId(id).Select(m => new SelectListItem() { Text = m.Nombre, Value = m.UnidadId.ToString() }).ToList();
            return Json(_result, JsonRequestBehavior.AllowGet);
        }
    }
}