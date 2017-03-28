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
    public class Producto_EgresoController : Controller
    {

        #region Metodos Privados

        private void CargaControles()
        {            
            var Clientes = new ClienteBL().ObtenerListado(false, true);
            var Productos = new ProductoBL().ObtenerListado(true, false, true);
            var Formas = new FormaPagoBL().ObtenerListado(false);
           
            ViewBag.Clientes = new SelectList(Clientes, "ClienteId", "Nombre");
            ViewBag.Productos = new SelectList(Productos, "ProductoId", "Nombre");
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

        // GET: Producto_Egreso
        [Permiso("Control.Producto_Egreso.Ver_Listado")]
        public ActionResult Index(DateTime? FechaInicial, DateTime? FechaFinal)
        {
            CustomHelper.setTitle("Producto Egreso", "Listado");

            List<Movimiento> Movimientos = new List<Movimiento>();

            if (!FechaInicial.HasValue && !FechaFinal.HasValue)
            {
                FechaInicial = DateTime.Today;
                FechaFinal = DateTime.Today;
            }

            try
            {
                Movimientos = new MovimientoBL().ObtenerListadoPorFecha(FechaInicial.Value, FechaFinal.Value, 2, CustomHelper.getUserId(), false).ToList();
            }
            catch (Exception)
            {
            }

            return View(Movimientos);
        }

        [Permiso("Control.Producto_Egreso.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Producto Egreso", "Nuevo");

            this.CargaControles();
            return View();
        }

        [Permiso("Control.Producto_Egreso.Crear")]
        [HttpPost]
        public ActionResult Crear(Movimiento modelo, string[] productoIds, long[] presentacionIds, decimal[] cantidadIds, decimal[] precioIds, long[] formaIds, decimal[] pagarIds, string[] notaIds, int descuento)
        {
            if (productoIds == null || productoIds.Length == 0)
            {
                ModelState.AddModelError("", "Para realizar un ingreso debe de asignar productos");
            }

            if (formaIds == null || formaIds.Length == 0)
            {
                ModelState.AddModelError("", "Para realizar un egreso debe de cancelar los productos");
            }

            modelo.AgenciaId = CustomHelper.getAgenciaId();
            modelo.UsrCreo = CustomHelper.getUserId();

            if (ModelState.IsValid)
            {
                modelo.Detalles = new List<MovimientoDetalle>();
                for (int i = 0; i < productoIds.Length; i++)
                {
                    MovimientoDetalle Detalle = new MovimientoDetalle();
                    Detalle.ProductoId = productoIds[i];
                    Detalle.UnidadId = presentacionIds[i];
                    Detalle.Cantidad = cantidadIds[i];
                    Detalle.Precio = precioIds[i];

                    modelo.Detalles.Add(Detalle);
                }

                modelo.Pagos = new List<MovimientoFormaPago>();
                for (int i = 0; i < formaIds.Length; i++)
                {
                    MovimientoFormaPago Forma = new MovimientoFormaPago();
                    Forma.FormaPagoId = formaIds[i];
                    Forma.Valor = pagarIds[i];
                    Forma.Nota = notaIds[i];

                    modelo.Pagos.Add(Forma);
                }

                modelo.Descuento = descuento;
                modelo.MovimientoTipoId = 2;
                modelo.Operado = true;

                string strMensaje = new MovimientoBL().Guardar(modelo);
                if (strMensaje.Equals("OK"))
                {
                    using (var db = new GiveContext())
                    {
                        var agencia = db.Agencias.FirstOrDefault(a => a.AgenciaId == modelo.AgenciaId);

                        if (agencia != null)
                        {
                            foreach (var p in modelo.Detalles)
                            {
                                var productoId = p.ProductoId;
                                var producto = db.Productos.Include(pr => pr.Marca).FirstOrDefault(pr => pr.ProductoId == productoId);


                                if (producto == null) continue;


                                db.RegistrosKardex.Add(new RegistroKardex
                                {
                                    FechaHora = DateTime.Now,
                                    Fecha = DateTime.Today,
                                    ProductoId = p.ProductoId,
                                    ProductoCodigo = producto.Codigo,
                                    ProductoNombre = producto.Nombre,
                                    ProductoDescripcion = producto.Descripcion,
                                    MarcaId = producto.MarcaId,
                                    MarcaNombre = producto.Marca.Nombre,
                                    DocumentoNumero = modelo.MovimientoId.ToString(),
                                    AgenciaId = modelo.AgenciaId,
                                    AgenciaNombre = agencia.Nombre,
                                    TipoRegistro = "Egreso",
                                    SalidaCantidadTienda = p.Cantidad,
                                    SalidaCostoTienda = p.Precio
                                });
                            }

                            db.SaveChanges();
                        }
                    }
                    TempData["Producto-Egreso-Success"] = strMensaje;
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
            ViewBag.precioIds = precioIds;

            ViewBag.formaIds = formaIds;
            ViewBag.pagarIds = pagarIds;
            ViewBag.notaIds = notaIds;

            this.CargaControles();
            return View(modelo);
        }

        [Permiso("Control.Producto_Egreso.Detalle")]
        public ActionResult Detalle(long id)
        {
            Movimiento MovimientoActual = new MovimientoBL().ObtenerPorId(id, false);

            if (MovimientoActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Producto Egreso", "Detalle");

            return View(MovimientoActual);
        }

        [Permiso("Control.Reporte.Boleta_Egreso")]
        public ActionResult Boleta(long Id)
        {
            Movimiento MovimientoActual = new MovimientoBL().ObtenerPorId(Id, false);

            if (MovimientoActual != null)
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

                Encabezado.Rows.Add(MovimientoActual.MovimientoId, MovimientoActual.Agencia.Nombre, MovimientoActual.Cliente.Nombre, MovimientoActual.Cliente.Direccion, MovimientoActual.Descripcion, MovimientoActual.Fecha.ToString("dd/MM/yyyy"), MovimientoActual.DescuentoTotal, MovimientoActual.Total);

                Detalle.Columns.Add(new DataColumn("MovimientoId", typeof(long)));
                Detalle.Columns.Add(new DataColumn("ProductoId", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Nombre", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Presentacion", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Cantidad", typeof(int)));
                Detalle.Columns.Add(new DataColumn("Precio", typeof(decimal)));

                if (MovimientoActual.Detalles != null && MovimientoActual.Detalles.Count() > 0)
                {
                    foreach (var DetalleActual in MovimientoActual.Detalles)
                    {
                        Detalle.Rows.Add(MovimientoActual.MovimientoId, DetalleActual.ProductoId, DetalleActual.Producto.Nombre, DetalleActual.Unidad.Nombre, DetalleActual.Cantidad, DetalleActual.Precio);
                    }
                }

                Movimiento.Tables.Add(Encabezado);
                Movimiento.Tables.Add(Detalle);

                // Se define la ruta del reporte
                var reportPath = Server.MapPath("~/Reports/ReportMovEgreso.rdlc");

                // se obtienen los bytes del reporte en pdf
                var bytes = GetReportBytes(reportPath, Movimiento, 8.5m, 11.0m, 0.2m, 0m);

                return File(bytes, "application/pdf");
            }

            return View();
        }

        [ActionName("ObtenerPresentacionPorProducto")]
        public JsonResult PresentacionListado(string id)
        {
            IList _result = new List<SelectListItem>();
            _result = new ProductoBL().ObtenerPresentacionPorProductoId(id).Select(m => new SelectListItem() { Text = m.Nombre, Value = m.UnidadId.ToString() }).ToList();
            return Json(_result, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerDescuento")]
        public JsonResult ObtenerDescuento(long clienteId)
        {
            if (clienteId > 0)
            {
                return Json(new { Operacion = true, Data = new ClienteBL().ObtenerDescuentoPorId(clienteId) }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }
    }
}