using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DiamDev.Give.BLL;
using DiamDev.Give.Entities;
using DiamDev.Give.UI.App_Start;
using PagedList;
using System.Data;
using Microsoft.Reporting.WebForms;
using System.Collections;

namespace DiamDev.Give.UI.Controllers
{
    [Authorize]
    [Seguridad]
    [HandleError]
    public class Pedido_K66Controller : Controller
    {
        #region Metodos Privados

        private void CargaEmpresas()
        {
            var Empresas = new EmpresaBL().ObtenerListadoxUsuario(CustomHelper.getUserId());

            ViewBag.Empresas = new SelectList(Empresas, "EmpresaId", "Nombre");
        }

        private byte[] GetReportBytes(string reportPath, DataSet reportDataSource, decimal pageWidth = 13.38m, decimal pageHeight = 8.5m, decimal MarginLeft = 1m, decimal MarginRight = 1m)
        {
            byte[] reportBytes = null;

            // Se crea la instancia del reporte y se cargan sus datos.
            LocalReport reporte = new LocalReport() { ReportPath = reportPath };
            reporte.DataSources.Add(new ReportDataSource("PedidoEncabezadoK66", reportDataSource.Tables[0]));
            reporte.DataSources.Add(new ReportDataSource("PedidoDetalleK66", reportDataSource.Tables[1]));

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

        // GET: Pedido_K66
        [Permiso("Control.Pedido_K66.Ver_Listado")]
        public ActionResult Index(int? page, string search, DateTime? FechaInicial, DateTime? FechaFinal)
        {
            CustomHelper.setTitle("Pedido K66", "Listado");
            List<PedidoK66> Pedidos = new List<PedidoK66>();

            try
            {
                if (!FechaInicial.HasValue && !FechaFinal.HasValue)
                {
                    FechaInicial = DateTime.Today;
                    FechaFinal = DateTime.Today;
                }

                if (!string.IsNullOrWhiteSpace(search) && search != null)
                {
                    Pedidos = new Pedidok66BL().Buscar(search, CustomHelper.getUserId()).ToList();
                }
                else
                {
                    Pedidos = new Pedidok66BL().ObtenerListadoxFecha(FechaInicial.Value, FechaFinal.Value, CustomHelper.getUserId()).ToList();
                }
            }
            catch (Exception)
            { }

            ViewBag.Search = search;
            ViewBag.fechaInicial = FechaInicial.Value.ToString("yyyy-MM-dd");
            ViewBag.fechaFinal = FechaFinal.Value.ToString("yyyy-MM-dd");

            int pageSize = 15;
            int pageNumber = (page ?? 1);
            return View(Pedidos.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Pedido_K66_Pendiente_Aprobacion.Ver_Listado")]
        public ActionResult Pendiente_Aprobacion(int? page, string search, DateTime? FechaInicial, DateTime? FechaFinal)
        {
            CustomHelper.setTitle("Pedido K66 - Pendiente de Aprobacion", "Listado");
            List<PedidoK66> Pedidos = new List<PedidoK66>();

            try
            {
                if (!FechaInicial.HasValue && !FechaFinal.HasValue)
                {
                    FechaInicial = DateTime.Today;
                    FechaFinal = DateTime.Today;
                }

                if (!string.IsNullOrWhiteSpace(search) && search != null)
                {
                    Pedidos = new Pedidok66BL().BuscarxEstado(search, 3, 0).ToList();
                }
                else
                {
                    Pedidos = new Pedidok66BL().ObtenerListadoxFechaxEstado(FechaInicial.Value, FechaFinal.Value, 3, 0).ToList();
                }
            }
            catch (Exception)
            { }

            ViewBag.Search = search;

            int pageSize = 15;
            int pageNumber = (page ?? 1);
            return View(Pedidos.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Pedido_K66_Rechazado.Ver_Listado")]
        public ActionResult Rechazado(int? page, string search)
        {
            CustomHelper.setTitle("Pedido K66 - Rechazadas", "Listado");
            List<PedidoK66> Pedidos = new List<PedidoK66>();

            try
            {
                if (!string.IsNullOrWhiteSpace(search) && search != null)
                {
                    Pedidos = new Pedidok66BL().BuscarxEstado(search, 1, CustomHelper.getUserId()).ToList();
                }
                else
                {
                    Pedidos = new Pedidok66BL().ObtenerListadoxFechaxEstado(DateTime.Today, DateTime.Today, 1, CustomHelper.getUserId()).ToList();
                }
            }
            catch (Exception)
            { }

            ViewBag.Search = search;

            int pageSize = 15;
            int pageNumber = (page ?? 1);
            return View(Pedidos.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Pedido_K66.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Pedido K66", "Nuevo");

            ViewBag.ClienteIds = 0;
            ViewBag.DireccionIds = 0;
            ViewBag.TipoPedidoIds = 0;

            ViewBag.Unidad = CustomHelper.Permiso("Control.Pedido_K66.Unidad") ? 1 : 0;
            ViewBag.Inventario = CustomHelper.Permiso("Control.Pedido_K66.Inventario");

            this.CargaEmpresas();
            return View();
        }

        [Permiso("Control.Pedido_K66.Crear")]
        [HttpPost]
        public ActionResult Crear(PedidoK66 modelo, string[] productoIds, string[] nombreProductoIds, string[] unidadIds, decimal[] existenciaIds, decimal[] cantidadIds, decimal[] precioIds, decimal[] precioOriginalIds, bool[] precioCambiadoIds, decimal[] descuentoIds, string [] bodegaIds, HttpPostedFileBase documentoApp)
        {
            modelo.Detalles = new List<PedidoDetalleK66>();

            if (productoIds == null || productoIds.Length == 0)
            {
                ModelState.AddModelError("", "Para realizar un pedido debe de asignar productos");
            }
            else
            {
                for (int i = 0; i < productoIds.Length; i++)
                {
                    PedidoDetalleK66 Detalle = new PedidoDetalleK66
                    {
                        ProductoId = productoIds[i],
                        Nombre = nombreProductoIds[i],
                        Unidad = unidadIds[i],
                        Existencia = existenciaIds[i],
                        Cantidad = cantidadIds[i],
                        Precio = precioIds[i],
                        PrecioOriginal = precioOriginalIds[i],
                        PrecioCambiado = precioCambiadoIds[i],
                        Descuento = descuentoIds[i],
                        WarehouseId = bodegaIds[i]
                    };

                    modelo.Detalles.Add(Detalle);
                }
            }

            if (!string.IsNullOrWhiteSpace(modelo.OrdenCompraCliente))
            {
                if (documentoApp != null)
                {
                    modelo.Documento = new ProductoFotografia();
                    if (documentoApp != null)
                    {
                        byte[] FileData = new byte[documentoApp.ContentLength + 1];
                        documentoApp.InputStream.Read(FileData, 0, documentoApp.ContentLength);

                        modelo.DocumentoOrdenCompraRespaldo = documentoApp.FileName.Replace(" ", "_");
                        modelo.Documento = new ProductoFotografia() { Nombre = documentoApp.FileName, Content = FileData, ContentType = documentoApp.ContentType, Length = documentoApp.ContentLength };
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Se le informa que debe de ingresar el documento de respaldo de la orden de compra");
                }
            }

            modelo.Documentos = new List<ProductoFotografia>();

            for (int i = 0; i < Request.Files.Count; i++)
            {
                var Archivo = Request.Files[i];
                if (Archivo != null)
                {
                    if (!string.IsNullOrWhiteSpace(Archivo.FileName))
                    {
                        //Se agregan las fotografias
                        byte[] FileData = new byte[Archivo.ContentLength + 1];
                        Archivo.InputStream.Read(FileData, 0, Archivo.ContentLength);

                        modelo.Documentos.Add(new ProductoFotografia() { Nombre = Archivo.FileName, Content = FileData, ContentType = Archivo.ContentType, Length = Archivo.ContentLength });
                    }
                }
            }

            modelo.ResponsableId = CustomHelper.getUserId();

            if (modelo.DireccionEntrega == null)
            {
                ModelState.AddModelError("", "Para realizar un pedido debe de asignar direccion del cliente.");
            }
            if (ModelState.IsValid)
            {
                string strMensaje = new Pedidok66BL().Guardar(modelo);
                if (strMensaje.Equals("OK"))
                {
                    TempData["Pedido-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }

            ViewBag.ClienteIds = modelo.IDK66;
            ViewBag.DireccionIds = modelo.DireccionId;
            ViewBag.TipoPedidoIds = modelo.TipoPedidoId;

            ViewBag.productoIds = productoIds;
            ViewBag.nombreProductoIds = nombreProductoIds;
            ViewBag.unidadIds = unidadIds;
            ViewBag.existenciaIds = existenciaIds;
            ViewBag.cantidadIds = cantidadIds;
            ViewBag.precioIds = precioIds;
            ViewBag.precioOriginalIds = precioOriginalIds;
            ViewBag.precioCambiadoIds = precioCambiadoIds;
            ViewBag.descuentoIds = descuentoIds;

            ViewBag.Unidad = CustomHelper.Permiso("Control.Pedido_K66.Unidad") ? 1 : 0;
            ViewBag.Inventario = CustomHelper.Permiso("Control.Pedido_K66.Inventario");

            this.CargaEmpresas();
            return View(modelo);
        }

        [Permiso("Control.Pedido_K66.Crear")]
        public ActionResult Detalle(long id)
        {
            PedidoK66 PedidoK66Actual = new Pedidok66BL().ObtenerxId(id);

            if (PedidoK66Actual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Pedido K66", "Detalle");

            return View(PedidoK66Actual);
        }

        [Permiso("Control.Pedido_K66_Pendiente_Aprobacion.Ver_Listado")]
        public ActionResult Aprobar(long id)
        {
            PedidoK66 PedidoK66Actual = new Pedidok66BL().ObtenerxId(id);

            if (PedidoK66Actual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Pedido K66", "Aprobar");

            return View(PedidoK66Actual);
        }

        [Permiso("Control.Pedido_K66_Pendiente_Aprobacion.Ver_Listado")]
        [HttpPost]
        public ActionResult Aprobar(PedidoK66 modelo)
        {
            string strMensaje = new Pedidok66BL().Aprobar(modelo, CustomHelper.getUserId());
            if (strMensaje.Equals("OK"))
            {
                TempData["Pedido-Success"] = strMensaje;
                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("", strMensaje);
            }

            return View(new Pedidok66BL().ObtenerxId(modelo.PedidoId));
        }

        [Permiso("Control.Pedido_K66_Pendiente_Aprobacion.Ver_Listado")]
        public ActionResult Rechazar(long id)
        {
            PedidoK66 PedidoK66Actual = new Pedidok66BL().ObtenerxId(id);

            if (PedidoK66Actual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Pedido K66", "Rechazar");

            return View(PedidoK66Actual);
        }

        [Permiso("Control.Pedido_K66_Pendiente_Aprobacion.Ver_Listado")]
        [HttpPost]
        public ActionResult Rechazar(PedidoK66 modelo)
        {
            string strMensaje = new Pedidok66BL().Rechazar(modelo, CustomHelper.getUserId());
            if (strMensaje.Equals("OK"))
            {
                TempData["Pedido-Success"] = strMensaje;
                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("", strMensaje);
            }

            return View(new Pedidok66BL().ObtenerxId(modelo.PedidoId));
        }

        [Permiso("Control.Pedido_K66.Crear")]
        public ActionResult Editar(long id)
        {
            PedidoK66 PedidoK66Actual = new Pedidok66BL().ObtenerxId(id);

            if (PedidoK66Actual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Pedido K66", "Editar");

            if (PedidoK66Actual.Detalles != null && PedidoK66Actual.Detalles.Count() > 0)
            {
                ViewBag.productoIds = PedidoK66Actual.Detalles.OrderBy(x => x.DetalleId).Select(x => x.ProductoId).ToList();
                ViewBag.nombreProductoIds = PedidoK66Actual.Detalles.OrderBy(x => x.DetalleId).Select(x => x.Nombre).ToList();
                ViewBag.unidadIds = PedidoK66Actual.Detalles.OrderBy(x => x.DetalleId).Select(x => x.Unidad).ToList();
                ViewBag.existenciaIds = PedidoK66Actual.Detalles.OrderBy(x => x.DetalleId).Select(x => x.Cantidad).ToList();
                ViewBag.cantidadIds = PedidoK66Actual.Detalles.OrderBy(x => x.DetalleId).Select(x => x.Cantidad).ToList();
                ViewBag.precioIds = PedidoK66Actual.Detalles.OrderBy(x => x.DetalleId).Select(x => x.Precio).ToList();
                ViewBag.precioOriginalIds = PedidoK66Actual.Detalles.OrderBy(x => x.DetalleId).Select(x => x.PrecioOriginal).ToList();
                ViewBag.precioCambiadoIds = PedidoK66Actual.Detalles.OrderBy(x => x.DetalleId).Select(x => x.PrecioCambiado).ToList();
                ViewBag.descuentoIds = PedidoK66Actual.Detalles.OrderBy(x => x.DetalleId).Select(x => x.Descuento).ToList();
            }
            else
            {
                ViewBag.productoIds = "";
                ViewBag.nombreProductoIds = "";
                ViewBag.unidadIds = "";
                ViewBag.existenciaIds = 0;
                ViewBag.cantidadIds = 0;
                ViewBag.precioIds = 0;
                ViewBag.precioOriginalIds = 0;
                ViewBag.precioCambiadoIds = false;
                ViewBag.descuentoIds = 0;
            }

            ViewBag.ClienteIds = PedidoK66Actual.IDK66;

            this.CargaEmpresas();
            return View(PedidoK66Actual);
        }

        [Permiso("Control.Pedido_K66.Crear")]
        [HttpPost]
        public ActionResult Editar(PedidoK66 modelo, string[] productoIds, string[] nombreProductoIds, string[] unidadIds, decimal[] existenciaIds, decimal[] cantidadIds, decimal[] precioIds, decimal[] precioOriginalIds, bool[] precioCambiadoIds, decimal[] descuentoIds, HttpPostedFileBase documentoApp)
        {
            modelo.Detalles = new List<PedidoDetalleK66>();

            if (productoIds == null || productoIds.Length == 0)
            {
                ModelState.AddModelError("", "Para realizar un pedido debe de asignar productos");
            }
            else
            {
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
                        PedidoDetalleK66 Detalle = new PedidoDetalleK66();
                        Detalle.ProductoId = productoIds[i];
                        Detalle.Nombre = nombreProductoIds[i];
                        Detalle.Unidad = unidadIds[i];
                        Detalle.Existencia = existenciaIds[i];
                        Detalle.Cantidad = cantidadIds[i];
                        Detalle.Precio = precioIds[i];
                        Detalle.PrecioOriginal = precioOriginalIds[i];
                        Detalle.PrecioCambiado = precioCambiadoIds[i];
                        Detalle.Descuento = descuentoIds[i];

                        modelo.Detalles.Add(Detalle);
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(modelo.OrdenCompraCliente))
            {
                if (documentoApp != null)
                {
                    modelo.Documento = new ProductoFotografia();
                    if (documentoApp != null)
                    {
                        byte[] FileData = new byte[documentoApp.ContentLength + 1];
                        documentoApp.InputStream.Read(FileData, 0, documentoApp.ContentLength);

                        modelo.DocumentoOrdenCompraRespaldo = documentoApp.FileName.Replace(" ", "_");
                        modelo.Documento = new ProductoFotografia() { Nombre = documentoApp.FileName, Content = FileData, ContentType = documentoApp.ContentType, Length = documentoApp.ContentLength };
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Se le informa que debe de ingresar el documento de respaldo de la orden de compra");
                }
            }

            modelo.Documentos = new List<ProductoFotografia>();

            for (int i = 0; i < Request.Files.Count; i++)
            {
                var Archivo = Request.Files[i];
                if (Archivo != null)
                {
                    if (!string.IsNullOrWhiteSpace(Archivo.FileName))
                    {
                        //Se agregan las fotografias
                        byte[] FileData = new byte[Archivo.ContentLength + 1];
                        Archivo.InputStream.Read(FileData, 0, Archivo.ContentLength);

                        modelo.Documentos.Add(new ProductoFotografia() { Nombre = Archivo.FileName, Content = FileData, ContentType = Archivo.ContentType, Length = Archivo.ContentLength });
                    }
                }
            }

            modelo.ResponsableId = CustomHelper.getUserId();

            if (ModelState.IsValid)
            {
                string strMensaje = new Pedidok66BL().Guardar(modelo);
                if (strMensaje.Equals("OK"))
                {
                    TempData["Pedido-Success"] = strMensaje;
                    return RedirectToAction("Pendiente_Aprobacion");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }

            ViewBag.productoIds = productoIds;
            ViewBag.nombreProductoIds = nombreProductoIds;
            ViewBag.unidadIds = unidadIds;
            ViewBag.existenciaIds = existenciaIds;
            ViewBag.cantidadIds = cantidadIds;
            ViewBag.precioIds = precioIds;
            ViewBag.precioOriginalIds = precioOriginalIds;
            ViewBag.precioCambiadoIds = precioCambiadoIds;
            ViewBag.descuentoIds = descuentoIds;

            this.CargaEmpresas();
            return View(modelo);
        }

        [Permiso("Control.Pedido_K66.Crear")]
        public ActionResult Boleta(long id)
        {
            PedidoK66 PedidoActual = new Pedidok66BL().ObtenerxId(id);          

            if (PedidoActual != null)
            {
                DataSet Movimiento = new DataSet("Inventario");

                DataTable Encabezado = new DataTable("PedidoEncabezadoK66");
                DataTable Detalle = new DataTable("PedidoDetalleK66");

                Encabezado.Columns.Add(new DataColumn("PedidoId", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Empresa", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Tipo", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Estado", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("CUSTOMERORDERROWID", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("CUSTOMERORDERID", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("IDK66", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Nit", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Nombre", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Direccion", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("OrdenCompraCliente", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("ObservacionesGenerales", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("ComentarioAprobacion", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("FechaHoraPedido", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("FechaHoraUltimoIntento", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("FechaPrometida", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Fecha", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Hora", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Responsable", typeof(string)));

                Encabezado.Columns.Add(new DataColumn("TerminoEntrega", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Vendedor", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("ImpuestoTAX", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Moneda", typeof(string)));

                Encabezado.Columns.Add(new DataColumn("DireccionEntrega", typeof(string)));

                Encabezado.Rows.Add(PedidoActual.PedidoId, PedidoActual.Empresa.Nombre, "Generico", PedidoActual.Estado.Nombre, PedidoActual.CUSTOMERORDERROWID, PedidoActual.CUSTOMERORDERID, PedidoActual.IDK66, PedidoActual.Nit, PedidoActual.Nombre, PedidoActual.Direccion, PedidoActual.OrdenCompraCliente, PedidoActual.ObservacionesGenerales, PedidoActual.ComentarioAprobacion, PedidoActual.FechaHoraPedido == null ? "" : PedidoActual.FechaHoraPedido.Value.ToString("dd/MM/yyyy"), PedidoActual.FechaHoraUltimoIntento == null ? "" : PedidoActual.FechaHoraUltimoIntento.Value.ToString("dd/MM/yyyy"), PedidoActual.FechaPrometida == null ? "" : PedidoActual.FechaPrometida.Value.ToString("dd/MM/yyyy"), PedidoActual.Fecha == null ? "" : PedidoActual.Fecha.ToString("dd/MM/yyyy"), PedidoActual.FechaHoraPedido == null ? "" : PedidoActual.FechaHoraPedido.Value.ToString("hh:mm:ss tt").ToUpper(), PedidoActual.Responsable.Nombre.ToUpper(), PedidoActual.TerminoEntrega, PedidoActual.Vendedor, PedidoActual.ImpuestoTAX, PedidoActual.Moneda, PedidoActual.DireccionEntrega);

                Detalle.Columns.Add(new DataColumn("PedidoId", typeof(string)));
                Detalle.Columns.Add(new DataColumn("DetalleId", typeof(string)));
                Detalle.Columns.Add(new DataColumn("ProductoId", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Nombre", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Unidad", typeof(string)));               
                Detalle.Columns.Add(new DataColumn("Cantidad", typeof(decimal)));
                Detalle.Columns.Add(new DataColumn("PrecioOriginal", typeof(decimal)));
                Detalle.Columns.Add(new DataColumn("Precio", typeof(decimal)));

                if (PedidoActual.Detalles != null && PedidoActual.Detalles.Count() > 0)
                {
                    foreach (var DetalleActual in PedidoActual.Detalles)
                    {
                        Detalle.Rows.Add(PedidoActual.PedidoId, DetalleActual.DetalleId, DetalleActual.ProductoId, DetalleActual.Nombre, DetalleActual.Unidad, DetalleActual.Cantidad, DetalleActual.PrecioOriginal, DetalleActual.Precio);
                    }
                }

                Movimiento.Tables.Add(Encabezado);
                Movimiento.Tables.Add(Detalle);

                // Se define la ruta del reporte
                var reportPath = Server.MapPath(string.Format("~/Reports/ReportMovPedidoK66_{0}.rdlc", PedidoActual.EmpresaId));

                // se obtienen los bytes del reporte en pdf
                var bytes = GetReportBytes(reportPath, Movimiento, 8.5m, 11.0m, 0.2m, 0m);

                return File(bytes, "application/pdf");
            }

            return View();
        }

        [ActionName("ObtenerTipoPedidoxEmpresa")]
        public JsonResult ObtenerTipoPedidoxEmpresa(long id)
        {
            IList _result = new List<SelectListItem>();
            _result = new PedidoTipok66BL().ObtenerListadoxEmpresa(id).Select(m => new SelectListItem() { Text = m.Nombre, Value = m.TipoId.ToString() }).ToList();
            return Json(_result, JsonRequestBehavior.AllowGet);
        }
    }
}