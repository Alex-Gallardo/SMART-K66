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
using DiamDev.Give.DAL;
using System.Data.Entity;

namespace DiamDev.Give.UI.Controllers
{
    [Authorize]
    [Seguridad]
    [HandleError]
    public class Cuenta_x_CobrarController : Controller
    {
        #region Metodos Privados

            private void CargaFormas()
            {
                var Formas = new FormaPagoBL().ObtenerListado(false, CustomHelper.getEmpresaId());

                ViewBag.Formas = new SelectList(Formas, "FormaPagoId", "Nombre");
            }

            private void CargaClientesNoPagados()
            {
                var Clientes = new CuentaxCobrarBL().ObtenerClienteNoPagadas(CustomHelper.getAgenciaId()).ToList();

                ViewBag.Clientes = new SelectList(Clientes, "ClienteId", "Nombre");
            }

            private byte[] GetReportBytes(string reportPath, DataSet reportDataSource, decimal pageWidth = 13.38m, decimal pageHeight = 8.5m, decimal MarginLeft = 1m, decimal MarginRight = 1m)
            {
                byte[] reportBytes = null;

                // Se crea la instancia del reporte y se cargan sus datos.
                LocalReport reporte = new LocalReport() { ReportPath = reportPath };
                reporte.DataSources.Add(new ReportDataSource("Abono", reportDataSource.Tables[0]));

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

        // GET: Cuenta_x_Cobrar
        [Permiso("Control.Cuenta_x_Cobrar.Ver_Listado")]
        public ActionResult Index(int? page, long? cliente, string search)
        {
            CustomHelper.setTitle("Cuenta x Cobrar", "Listado");
            CuentaxCobrarModel Cuentas = new CuentaxCobrarModel();

            try
            {
                if (!string.IsNullOrWhiteSpace(search) && search != null)
                {
                    Cuentas = new CuentaxCobrarBL().BuscarNoPagadas(search, CustomHelper.getAgenciaId());
                }
                else if (cliente != null)
                {
                    Cuentas = new CuentaxCobrarBL().BuscarNoPagadasxCliente(cliente.Value, CustomHelper.getAgenciaId());
                }
                else
                {
                    Cuentas = new CuentaxCobrarBL().ObtenerListadoNoPagadas(CustomHelper.getAgenciaId());
                }
            }
            catch (Exception)
            {}

            if (Cuentas != null)
            {
                decimal Total = 0;

                if (Cuentas.Recibos != null && Cuentas.Recibos.Count() > 0)
                {
                    Total += Cuentas.Recibos.Sum(y => y.Detalles.Sum(z => z.Cantidad * z.Precio)) - Cuentas.Recibos.Sum(y => y.Abono);           
                }              

                ViewBag.Total = Total.ToString("C4");
            }
            else
            {
                ViewBag.Total = "Q0.0000";
            }

            ViewBag.Search = search;

            this.CargaClientesNoPagados();
            return View(Cuentas);
        }

        [Permiso("Control.Cuenta_x_Cobrar.Pago")]
        public ActionResult Pago_Recibo(long id)
        {
            Recibo ReciboActual = new ReciboBL().ObtenerPorId(id, true, true);

            if (ReciboActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Recibo", "Pago");

            this.CargaFormas();
            return View(ReciboActual);
        }

        [Permiso("Control.Cuenta_x_Cobrar.Pago")]
        public ActionResult Pago_Factura(long id)
        {
            Factura FacturaActual = new FacturaBL().ObtenerPorId(id, true, true, false);

            if (FacturaActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Factura", "Pago");

            this.CargaFormas();
            return View(FacturaActual);
        }

        [Permiso("Control.Cuenta_x_Cobrar.Pago")]
        [HttpPost]
        public ActionResult Pago_Recibo(Recibo modelo, long[] formaIds, decimal[] pagarIds, string[] notaIds)
        {
            Recibo ReciboActual = new ReciboBL().ObtenerPorId(modelo.ReciboId, true, true);

            if (formaIds == null || formaIds.Length == 0)
            {
                ModelState.AddModelError("", "Se le informa que el recibo debe contener un abono");
            }
            else
            {
                modelo.Pagos = new List<ReciboFormaPago>();
                for (int i = 0; i < formaIds.Length; i++)
                {
                    ReciboFormaPago Forma = new ReciboFormaPago();
                    Forma.FormaPagoId = formaIds[i];
                    Forma.Valor = pagarIds[i];
                    Forma.Nota = notaIds[i];

                    Forma.ReciboId = modelo.ReciboId;
                    Forma.UsrOperacionId = CustomHelper.getUserId();

                    modelo.Pagos.Add(Forma);
                }
            }

            if (ModelState.IsValid)
            {
                string strMensaje = new CuentaxCobrarBL().GenerarPagoRecibo(modelo, CustomHelper.getUserId(), modelo.Pagos);
                if (strMensaje.Equals("OK"))
                {
                    TempData["Pago-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }

            ViewBag.formaIds = formaIds;
            ViewBag.pagarIds = pagarIds;
            ViewBag.notaIds = notaIds;

            this.CargaFormas();
            return View(ReciboActual);
        }

        [Permiso("Control.Cuenta_x_Cobrar.Pago")]
        [HttpPost]
        public ActionResult Pago_Factura(Factura modelo, long[] formaIds, decimal[] pagarIds, string[] notaIds)
        {
            Factura FacturaActual = new FacturaBL().ObtenerPorId(modelo.FacturaId, true, true, false);

            if (formaIds == null || formaIds.Length == 0)
            {
                ModelState.AddModelError("", "Se le informa que la factura debe contener un abono");
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

                    Forma.FacturaId = modelo.FacturaId;
                    Forma.UsrOperacionId = CustomHelper.getUserId();

                    modelo.Pagos.Add(Forma);
                }
            }

            if (ModelState.IsValid)
            {
                string strMensaje = new CuentaxCobrarBL().GenerarPagoFactura(modelo, CustomHelper.getUserId(), modelo.Pagos);
                if (strMensaje.Equals("OK"))
                {
                    TempData["Pago-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }

            ViewBag.formaIds = formaIds;
            ViewBag.pagarIds = pagarIds;
            ViewBag.notaIds = notaIds;

            this.CargaFormas();
            return View(FacturaActual);
        }

        [HttpPost]
        [ActionName("GenerarPagoMaximoxRecibo")]
        public JsonResult GenerarPagoMaximoxRecibo(long[] reciboIDs, decimal[] saldoIDs)
        {
            if (reciboIDs != null && saldoIDs != null)
            {
                string Mensaje = new CuentaxCobrarBL().GenerarPagoRecibo(reciboIDs, saldoIDs, CustomHelper.getUserId());
                if (Mensaje.Equals("OK"))
                {
                    return Json(new { Operacion = true }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ActionName("GenerarPagoMaximoxFactura")]
        public JsonResult GenerarPagoMaximoxFactura(long[] facturaIDs, decimal[] saldoIDs)
        {
            if (facturaIDs != null && saldoIDs != null)
            {
                string Mensaje = new CuentaxCobrarBL().GenerarPagoFactura(facturaIDs, saldoIDs, CustomHelper.getUserId());
                if (Mensaje.Equals("OK"))
                {
                    return Json(new { Operacion = true }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [Permiso("Control.Cuenta_x_Cobrar.Pago")]
        public ActionResult Boleta_Recibo(long id, int detalleId)
        {
            ReciboFormaPago AbonoActual = new CuentaxCobrarBL().ObtenerAbonoxRecibo(id, detalleId);

            if (AbonoActual != null)
            {
                DataSet Abono = new DataSet("Abono");

                DataTable Encabezado = new DataTable("Abono");

                Encabezado.Columns.Add(new DataColumn("Documento", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Cliente", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Fecha", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Responsable", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Pago", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Monto", typeof(string)));

                Encabezado.Rows.Add(AbonoActual.ReciboId, AbonoActual.Recibo.Cliente.Nombre, AbonoActual.Fecha.ToString("dd/MM/yyyy"), AbonoActual.UsuarioOperacion.Nombre, AbonoActual.FormaPago.Nombre, AbonoActual.Valor.ToString("C"));

                Abono.Tables.Add(Encabezado);

                // Se define la ruta del reporte
                var reportPath = Server.MapPath("~/Reports/ReportMovAbonoRecibo.rdlc");

                // se obtienen los bytes del reporte en pdf
                var bytes = GetReportBytes(reportPath, Abono, 8.5m, 11.0m, 0.2m, 0m);

                return File(bytes, "application/pdf");
            }

            return View();
        }

        [Permiso("Control.Cuenta_x_Cobrar.Pago")]
        public ActionResult Boleta_Factura(long id, int detalleId)
        {
            FacturaFormaPago AbonoActual = new CuentaxCobrarBL().ObtenerAbonoxFactura(id, detalleId);

            if (AbonoActual != null)
            {
                DataSet Abono = new DataSet("Abono");

                DataTable Encabezado = new DataTable("Abono");

                Encabezado.Columns.Add(new DataColumn("Documento", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Cliente", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Fecha", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Responsable", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Pago", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Monto", typeof(string)));

                Encabezado.Rows.Add(string.Format("{0} - {1}", AbonoActual.Factura.Serie.Nombre, AbonoActual.Factura.NoFactura), AbonoActual.Factura.Cliente.Nombre, AbonoActual.Fecha.ToString("dd/MM/yyyy"), AbonoActual.UsuarioOperacion.Nombre, AbonoActual.FormaPago.Nombre, AbonoActual.Valor.ToString("C"));

                Abono.Tables.Add(Encabezado);

                // Se define la ruta del reporte
                var reportPath = Server.MapPath("~/Reports/ReportMovAbonoFactura.rdlc");

                // se obtienen los bytes del reporte en pdf
                var bytes = GetReportBytes(reportPath, Abono, 8.5m, 11.0m, 0.2m, 0m);

                return File(bytes, "application/pdf");
            }

            return View();
        }
    }
}