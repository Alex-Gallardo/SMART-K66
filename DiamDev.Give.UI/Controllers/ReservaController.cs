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
using OfficeOpenXml;

namespace DiamDev.Give.UI.Controllers
{
    [Authorize]
    [Seguridad]
    [HandleError]
    public class ReservaController : Controller
    {
        #region Metodos Privados

            private void CargaFormas()
            {
                var Formas = new FormaPagoBL().ObtenerListado(false);

                ViewBag.Formas = new SelectList(Formas, "FormaPagoId", "Nombre");
            }

            private byte[] GetReportBytes(string reportPath, DataSet reportDataSource, decimal pageWidth = 13.38m, decimal pageHeight = 8.5m, decimal MarginLeft = 1m, decimal MarginRight = 1m, bool Historial = false)
            {

                byte[] reportBytes = null;

                // Se crea la instancia del reporte y se cargan sus datos.
                LocalReport reporte = new LocalReport() { ReportPath = reportPath };
                if (Historial)
                {
                    reporte.DataSources.Add(new ReportDataSource("MovimientoEncabezado", reportDataSource.Tables[0]));
                    reporte.DataSources.Add(new ReportDataSource("MovimientoDetalle", reportDataSource.Tables[1]));
                    reporte.DataSources.Add(new ReportDataSource("MovimientoPago", reportDataSource.Tables[2]));
                }
                else
                {
                    reporte.DataSources.Add(new ReportDataSource("MovimientoEncabezado", reportDataSource.Tables[0]));
                    reporte.DataSources.Add(new ReportDataSource("MovimientoDetalle", reportDataSource.Tables[1]));
                }

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

        // GET: Reserva
        [Permiso("Control.Reserva.Ver_Listado")]
        public ActionResult Index(int? page, string search)
        {
            CustomHelper.setTitle("Reserva", "Listado");

            List<Reserva> Reservas = new List<Reserva>();

            try
            {
                if (!string.IsNullOrWhiteSpace(search) && search != null)
                {
                    Reservas = new ReservaBL().Buscar(search, CustomHelper.getUserId()).ToList();
                }
                else
                {
                    Reservas = new ReservaBL().ObtenerListado(CustomHelper.getUserId()).ToList();
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
            return View(Reservas.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Reserva.Ver_Listado")]
        public ActionResult Reserva_x_Cliente(long? cliente)
        {
            CustomHelper.setTitle("Reserva x Cliente", "Listado");

            List<Reserva> Reservas = new List<Reserva>();

            try
            {
                if (cliente != null)
                {
                    Reservas = new ReservaBL().ObtenerListadoxCliente(cliente.Value).ToList();
                }             
            }
            catch (Exception ex)
            {
                ViewBag.Error = string.Format("Message: {0} StackTrace: {1}", ex.Message, ex.StackTrace);
                return View("~/Views/Shared/Error.cshtml");
            }

            if (cliente != null)
            {
                ViewBag.ClienteIds = cliente.Value;
            }
            else
            {
                ViewBag.ClienteIds = 0;
            }

            return View(Reservas);
        }

        [Permiso("Control.Reserva.Ver_Listado")]
        public ActionResult Excel(long ClienteId)
        {
            List<Reserva> Reservas = new List<Reserva>();
            Reservas = new ReservaBL().ObtenerListadoxCliente(ClienteId).ToList();

            if (Reservas == null)
            {
                return HttpNotFound();
            }

            if (Reservas.Count() == 0)
            {
                return HttpNotFound();
            }

            using (var pck = new ExcelPackage())
            {
                var ws = pck.Workbook.Worksheets.Add("Reservas");
                ws.Cells["A1"].Value = "#Reserva";
                ws.Cells["B1"].Value = "Fecha";
                ws.Cells["C1"].Value = "Cliente";
                ws.Cells["D1"].Value = "#Telefono";
                ws.Cells["E1"].Value = "Operado";
                ws.Cells["F1"].Value = "Anulado";
                ws.Cells["G1"].Value = "Producto(s)";
                ws.Cells["H1"].Value = "Total";
                ws.Cells["I1"].Value = "Saldo";

                var fila = 1;
                foreach (var Reserva in Reservas)
                {
                    fila++;
                    ws.Cells[fila, 1].Value = Reserva.ReservaId;
                    ws.Cells[fila, 2].Value = Reserva.Fecha.ToString("dd/MM/yyyy");
                    ws.Cells[fila, 3].Value = Reserva.Cliente == null ? "No Disponible" : Reserva.Cliente.Nombre;
                    ws.Cells[fila, 4].Value = string.IsNullOrWhiteSpace(Reserva.Telefono) ? "No Disponible" : Reserva.Telefono;
                    ws.Cells[fila, 5].Value = Reserva.Operado == true ? "Sí" : "No";
                    ws.Cells[fila, 6].Value = Reserva.Anulada == true ? "Sí" : "No";
                    ws.Cells[fila, 7].Value = string.IsNullOrWhiteSpace(Reserva.Productos) ? "No Disponible" : Reserva.Productos;
                    ws.Cells[fila, 8].Value = Reserva.Detalles.Count == 0 ? "Q0.00" : Reserva.Detalles.Sum(x => x.Cantidad * x.Precio).ToString("C");
                    ws.Cells[fila, 9].Value = Reserva.Pagos.Count == 0 ? Reserva.Detalles.Sum(x => x.Cantidad * x.Precio).ToString("C") : (Reserva.Detalles.Sum(x => x.Cantidad * x.Precio) - Reserva.Pagos.Sum(x => x.Valor)).ToString("C");                 
                }

                using (var range = ws.Cells[1, 1, fila, 9])
                {
                    range.AutoFitColumns();
                }

                return File(pck.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", string.Format("reservas_cliente_{0}.xlsx", ClienteId));
            }
        }

        [Permiso("Control.Reserva.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Reserva", "Nueva");

            ViewBag.ClienteIds = 0;

            this.CargaFormas();
            return View();
        }

        [Permiso("Control.Reserva.Crear")]
        [HttpPost]
        public ActionResult Crear(Reserva modelo, string[] productoIds, string[] nombreProductoIds, long[] presentacionIds, string[] nombrePresentacionIds, decimal[] cantidadIds, decimal[] precioIds, long[] formaIds, decimal[] pagarIds, string[] notaIds, decimal[] descuentoIds)
        {
            if (productoIds == null || productoIds.Length == 0)
            {
                ModelState.AddModelError("", "Para realizar una reserva debe de asignar productos");
            }

            if (formaIds == null || formaIds.Length == 0)
            {
                ModelState.AddModelError("", "Para realizar una reserva debe de ingresar la forma de pago");
            }
            else
            {
                modelo.Pagos = new List<ReservaPago>();
                for (int i = 0; i < formaIds.Length; i++)
                {
                    ReservaPago Forma = new ReservaPago();
                    Forma.FormaPagoId = formaIds[i];
                    Forma.Valor = pagarIds[i];
                    Forma.Nota = notaIds[i];

                    modelo.Pagos.Add(Forma);
                }
            }

            modelo.AgenciaId = CustomHelper.getAgenciaId();
            modelo.UsrCreo = CustomHelper.getUserId();            
          
            if (ModelState.IsValid)
            {
                modelo.Detalles = new List<ReservaDetalle>();
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
                        ReservaDetalle Detalle = new ReservaDetalle();
                        Detalle.ProductoId = productoIds[i];
                        Detalle.UnidadId = presentacionIds[i];
                        Detalle.Cantidad = cantidadIds[i];

                        Detalle.Precio = precioIds[i] - descuentoIds[i];

                        modelo.Detalles.Add(Detalle);
                    }
                }

                string strMensaje = new ReservaBL().Guardar(modelo);
                if (strMensaje.Equals("OK"))
                {
                    TempData["Reserva-Success"] = strMensaje;
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
            ViewBag.descuentoIds = descuentoIds;
            ViewBag.precioIds = precioIds;

            ViewBag.formaIds = formaIds;
            ViewBag.pagarIds = pagarIds;
            ViewBag.notaIds = notaIds;

            this.CargaFormas();
            return View(modelo);
        }

        [Permiso("Control.Reserva.Detalle")]
        public ActionResult Detalle(long id)
        {
            Reserva ReservaActual = new ReservaBL().ObtenerPorId(id, true);

            if (ReservaActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Reserva", "Detalle");

            this.CargaFormas();
            return View(ReservaActual);
        }

        [Permiso("Control.Reserva.Anular")]
        public ActionResult Anular(long id)
        {
            Reserva ReservaActual = new ReservaBL().ObtenerPorId(id, true);

            if (ReservaActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Reserva", "Anular");

            return View(ReservaActual);
        }

        [Permiso("Control.Reserva.Anular")]
        [HttpPost]
        public ActionResult Anular(long reservaId, string comentario)
        {
            string strMensaje = new ReservaBL().Anular(reservaId, comentario, CustomHelper.getUserId());
            if (strMensaje.Equals("OK"))
            {
                TempData["Reserva_Anular-Success"] = strMensaje;
                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("", strMensaje);
            }

            Reserva ReservaActual = new ReservaBL().ObtenerPorId(reservaId, true);

            if (ReservaActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Reserva", "Anular");

            return View(ReservaActual);
        }

        [Permiso("Control.Reporte.Boleta_Reserva")]
        public ActionResult Boleta(long Id)
        {
            Reserva ReservaActual = new ReservaBL().ObtenerPorId(Id, true);

            if (ReservaActual != null)
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
                Encabezado.Columns.Add(new DataColumn("Vendedor", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Comentario", typeof(string)));

                string DescripcionPago = string.Empty;
                if (ReservaActual.Pagos != null && ReservaActual.Pagos.Count() > 0)
                {
                    DescripcionPago = string.Format("EL MONTO TOTAL A CANCELAR ES DE: {0:C}, DEJANDO UN ADELANTO DE: {1:C}, MONTO PENDIENTE A CANCELAR ES DE: {2:C}", ReservaActual.Detalles.Sum(x => x.Cantidad * x.Precio), ReservaActual.Pagos.Sum(x => x.Valor), ReservaActual.Detalles.Sum(x => x.Cantidad * x.Precio) - ReservaActual.Pagos.Sum(x => x.Valor));  
                }

                Encabezado.Rows.Add(ReservaActual.ReservaId, ReservaActual.Agencia.Nombre, ReservaActual.Cliente.Nombre, ReservaActual.Cliente.Direccion, DescripcionPago, ReservaActual.Fecha.ToString("dd/MM/yyyy"), ReservaActual.Telefono, "", ReservaActual.Observaciones);

                Detalle.Columns.Add(new DataColumn("MovimientoId", typeof(long)));
                Detalle.Columns.Add(new DataColumn("ProductoId", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Nombre", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Presentacion", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Cantidad", typeof(decimal)));
                Detalle.Columns.Add(new DataColumn("Precio", typeof(decimal)));
                Detalle.Columns.Add(new DataColumn("Minimo", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Maximo", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Marca", typeof(string)));

                if (ReservaActual.Detalles != null && ReservaActual.Detalles.Count() > 0)
                {
                    foreach (var DetalleActual in ReservaActual.Detalles)
                    {
                        Detalle.Rows.Add(ReservaActual.ReservaId, DetalleActual.ProductoId, string.Format("{0} - {1}", DetalleActual.Producto.Codigo, DetalleActual.Producto.Nombre), DetalleActual.Unidad.Nombre, DetalleActual.Cantidad, DetalleActual.Precio, 0, 0, "");
                    }
                }

                Movimiento.Tables.Add(Encabezado);
                Movimiento.Tables.Add(Detalle);

                // Se define la ruta del reporte
                var reportPath = Server.MapPath("~/Reports/ReportMovReserva.rdlc");

                // se obtienen los bytes del reporte en pdf
                var bytes = GetReportBytes(reportPath, Movimiento, 8.5m, 11.0m, 0.2m, 0m);

                return File(bytes, "application/pdf");

            }

            return View();
        }

        [Permiso("Control.Reporte.Boleta_Reserva")]
        public ActionResult Boleta_Historial(long Id)
        {
            Reserva ReservaActual = new ReservaBL().ObtenerPorId(Id, true);

            if (ReservaActual != null)
            {
                DataSet Movimiento = new DataSet("Inventario");

                DataTable Encabezado = new DataTable("MovimientoEncabezado");
                DataTable Detalle = new DataTable("MovimientoDetalle");
                DataTable Pago = new DataTable("MovimientoPago");

                Encabezado.Columns.Add(new DataColumn("MovimientoId", typeof(long)));
                Encabezado.Columns.Add(new DataColumn("Agencia", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Nombre", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Direccion", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Descripcion", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Fecha", typeof(DateTime)));
                Encabezado.Columns.Add(new DataColumn("Categoria", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Vendedor", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Comentario", typeof(string)));

                string DescripcionPago = string.Empty;
                if (ReservaActual.Pagos != null && ReservaActual.Pagos.Count() > 0)
                {
                    DescripcionPago = string.Format("EL MONTO TOTAL A CANCELAR ES DE: {0:C}, DEJANDO UN ADELANTO DE: {1:C}, MONTO PENDIENTE A CANCELAR ES DE: {2:C}", ReservaActual.Detalles.Sum(x => x.Cantidad * x.Precio), ReservaActual.Pagos.Sum(x => x.Valor), ReservaActual.Detalles.Sum(x => x.Cantidad * x.Precio) - ReservaActual.Pagos.Sum(x => x.Valor));
                }

                Encabezado.Rows.Add(ReservaActual.ReservaId, ReservaActual.Agencia.Nombre, ReservaActual.Cliente.Nombre, ReservaActual.Cliente.Direccion, DescripcionPago, ReservaActual.Fecha.ToString("dd/MM/yyyy"), ReservaActual.Telefono, "", ReservaActual.Observaciones);

                Detalle.Columns.Add(new DataColumn("MovimientoId", typeof(long)));
                Detalle.Columns.Add(new DataColumn("ProductoId", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Nombre", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Presentacion", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Cantidad", typeof(decimal)));
                Detalle.Columns.Add(new DataColumn("Precio", typeof(decimal)));
                Detalle.Columns.Add(new DataColumn("Minimo", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Maximo", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Marca", typeof(string)));

                if (ReservaActual.Detalles != null && ReservaActual.Detalles.Count() > 0)
                {
                    foreach (var DetalleActual in ReservaActual.Detalles)
                    {
                        Detalle.Rows.Add(ReservaActual.ReservaId, DetalleActual.ProductoId, string.Format("{0} - {1}", DetalleActual.Producto.Codigo, DetalleActual.Producto.Nombre), DetalleActual.Unidad.Nombre, DetalleActual.Cantidad, DetalleActual.Precio, 0, 0, "");
                    }
                }

                Pago.Columns.Add(new DataColumn("MovimientoId", typeof(long)));
                Pago.Columns.Add(new DataColumn("FormaPago", typeof(string)));
                Pago.Columns.Add(new DataColumn("Responsable", typeof(string)));
                Pago.Columns.Add(new DataColumn("Fecha", typeof(string)));
                Pago.Columns.Add(new DataColumn("Monto", typeof(decimal)));

                if (ReservaActual.Pagos != null && ReservaActual.Pagos.Count() > 0)
                {
                    foreach (var DetalleActual in ReservaActual.Pagos)
                    {
                        Pago.Rows.Add(ReservaActual.ReservaId, DetalleActual.FormaPago == null ? "No Disponible" : DetalleActual.FormaPago.Nombre, DetalleActual.UsuarioOperacion == null ? "No Disponible" : DetalleActual.UsuarioOperacion.Nombre, DetalleActual.Fecha.ToString("dd/MM/yyyy"), DetalleActual.Valor);
                    }
                }

                Movimiento.Tables.Add(Encabezado);
                Movimiento.Tables.Add(Detalle);
                Movimiento.Tables.Add(Pago);

                // Se define la ruta del reporte
                var reportPath = Server.MapPath("~/Reports/ReportMovReservaHistorial.rdlc");

                // se obtienen los bytes del reporte en pdf
                var bytes = GetReportBytes(reportPath, Movimiento, 8.5m, 11.0m, 0.2m, 0m, true);

                return File(bytes, "application/pdf");

            }

            return View();
        }

        [HttpPost]
        [ActionName("Pago")]
        public ActionResult Pago(ReservaPagoModel modelo)
        {
            if (modelo != null)
            {
                string Mensaje = new ReservaBL().Pago(modelo, CustomHelper.getUserId());

                if (Mensaje.Equals("OK"))
                {
                    return Json(new { Operacion = true }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerReservaActual")]
        public JsonResult ObtenerReservaActual(long reservaId)
        {
            if (reservaId > 0)
            {
                MensajePedido ReservaActual = new ReservaBL().ObtenerReserva(reservaId);
                if (ReservaActual != null)
                {
                    return Json(new { Operacion = true, Data = ReservaActual }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }
    }
}