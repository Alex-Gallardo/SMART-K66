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
    public class Nota_CreditoController : Controller
    {
        #region Metodos Privados

            private void CargaControles()
            {             
                var Formas = new FormaPagoBL().ObtenerListado(false);

                ViewBag.Formas = new SelectList(Formas, "FormaPagoId", "Nombre");
            }

            private void CargaSeries()
            {
                var Series = new SerieBL().ObtenerSeriesPorAgencia(CustomHelper.getAgenciaId(), true);

                ViewBag.Series = new SelectList(Series, "SerieId", "Nombre");
            }

            private void CargaTipos() 
            {
                var Tipos = new List<NotaTipo>() { new NotaTipo() { TipoId = 1, Nombre = "Devolución" }, new NotaTipo() { TipoId = 2, Nombre = "Vale de Regalo" } };

                ViewBag.Tipos = new SelectList(Tipos, "TipoId", "Nombre");
            }

            private byte[] GetReportBytes(string reportPath, DataSet reportDataSource, decimal pageWidth = 13.38m, decimal pageHeight = 8.5m, decimal MarginLeft = 1m, decimal MarginRight = 1m)
            {

                byte[] reportBytes = null;

                // Se crea la instancia del reporte y se cargan sus datos.
                LocalReport reporte = new LocalReport() { ReportPath = reportPath };
                reporte.DataSources.Add(new ReportDataSource("MovimientoEncabezado", reportDataSource.Tables[0]));
                reporte.DataSources.Add(new ReportDataSource("MovimientoDetalle", reportDataSource.Tables[1]));
                reporte.DataSources.Add(new ReportDataSource("MovimientoControl", reportDataSource.Tables[2]));

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

        // GET: Nota_Credito
        [Permiso("Control.Nota_Credito.Ver_Listado")]
        public ActionResult Index(DateTime? FechaInicial, DateTime? FechaFinal)
        {
            CustomHelper.setTitle("Nota Credito", "Listado");

            List<NotaCredito> Creditos = new List<NotaCredito>();

            if (!FechaInicial.HasValue && !FechaFinal.HasValue)
            {
                FechaInicial = DateTime.Today;
                FechaFinal = DateTime.Today;
            }

            try
            {
                Creditos = new NotaCreditoBL().ObtenerListadoPorFecha(FechaInicial.Value, FechaFinal.Value).ToList();
            }
            catch (Exception)
            {
            }

            return View(Creditos);
        }

        [Permiso("Control.Nota_Credito.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Nota Credito", "Nueva");

            ViewBag.ClienteIds = 0;

            this.CargaTipos();
            this.CargaSeries();
            this.CargaControles();
            return View(new NotaCredito() { ClienteId = 0 });
        }

        [Permiso("Control.Nota_Credito.Crear")]
        [HttpPost]
        public ActionResult Crear(NotaCredito modelo, long[] formaIds, decimal[] pagarIds, string[] notaIds)
        {
            if (modelo.TipoId != 1)
            {
                if (formaIds == null || formaIds.Length == 0)
                {
                    ModelState.AddModelError("", "Para realizar una nota de credito debe cancelarla");
                }
                else
                {
                    modelo.Pagos = new List<NotaCreditoFormaPago>();
                    for (int i = 0; i < formaIds.Length; i++)
                    {
                        NotaCreditoFormaPago Forma = new NotaCreditoFormaPago();
                        Forma.FormaPagoId = formaIds[i];
                        Forma.Valor = pagarIds[i];
                        Forma.Nota = notaIds[i];

                        modelo.Pagos.Add(Forma);
                    }
                }
            }
            else
            {
                modelo.Devolucion = true;
            }

            modelo.AgenciaId = CustomHelper.getAgenciaId();
            modelo.UsrCreo = CustomHelper.getUserId();

            if (ModelState.IsValid)
            {                
                modelo.Operado = false;

                string strMensaje = new NotaCreditoBL().Guardar(modelo);
                if (strMensaje.Equals("OK"))
                {                  
                    TempData["Nota-Credito-Success"] = strMensaje;
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

            ViewBag.ClienteIds = modelo.ClienteId;

            this.CargaTipos();
            this.CargaSeries();
            this.CargaControles();
            return View(modelo);
        }

        [Permiso("Control.Nota_Credito.Anular")]
        public ActionResult Anular(long id)
        {
            NotaCredito NotaCreditoActual = new NotaCreditoBL().ObtenerPorId(id, true);

            if (NotaCreditoActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Nota Credito", "Anular");

            return View(NotaCreditoActual);
        }

        [Permiso("Control.Nota_Credito.Anular")]
        [HttpPost]
        public ActionResult Anular(long CreditoId, string Comentario)
        {
            string strMensaje = new NotaCreditoBL().Anular(CreditoId, Comentario, CustomHelper.getUserId());
            if (strMensaje.Equals("OK"))
            {
                TempData["Nota-Credito_Anular-Success"] = strMensaje;
                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("", strMensaje);
            }

            NotaCredito NotaCreditoActual = new NotaCreditoBL().ObtenerPorId(CreditoId, true);

            if (NotaCreditoActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Nota Credito", "Anular");

            return View(NotaCreditoActual);
        }

        [Permiso("Control.Nota_Credito.Detalle")]
        public ActionResult Detalle(long id)
        {
            NotaCredito NotaCreditoActual = new NotaCreditoBL().ObtenerPorId(id, true);

            if (NotaCreditoActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Nota Credito", "Detalle");

            return View(NotaCreditoActual);
        }

        [Permiso("Control.Reporte.Nota_Credito")]
        public ActionResult Boleta(long Id)
        {
            NotaCredito NotaActual = new NotaCreditoBL().ObtenerPorId(Id, true);

            if (NotaActual != null)
            {
                DataSet Movimiento = new DataSet("Inventario");

                DataTable Encabezado = new DataTable("MovimientoEncabezado");
                DataTable Detalle = new DataTable("MovimientoDetalle");
                DataTable Control = new DataTable("MovimientoControl");

                Encabezado.Columns.Add(new DataColumn("MovimientoId", typeof(long)));
                Encabezado.Columns.Add(new DataColumn("Agencia", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Nombre", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Direccion", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Descripcion", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Fecha", typeof(DateTime)));
                Encabezado.Columns.Add(new DataColumn("Descuento", typeof(decimal)));
                Encabezado.Columns.Add(new DataColumn("Total", typeof(decimal)));

                Encabezado.Rows.Add(NotaActual.CreditoId, NotaActual.Agencia.Nombre, NotaActual.Cliente.Nombre, NotaActual.Cliente.Direccion, NotaActual.Cliente.Nit, NotaActual.Fecha.ToString("dd/MM/yyyy"), 0, NotaActual.Monto);

                Detalle.Columns.Add(new DataColumn("MovimientoId", typeof(long)));
                Detalle.Columns.Add(new DataColumn("ProductoId", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Nombre", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Presentacion", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Cantidad", typeof(int)));
                Detalle.Columns.Add(new DataColumn("Precio", typeof(decimal)));

                Detalle.Rows.Add(NotaActual.CreditoId, 0, NotaActual.Nota, "", 1, NotaActual.Monto);

                Control.Columns.Add(new DataColumn("MovimientoId", typeof(long)));
                Control.Columns.Add(new DataColumn("Factura", typeof(string)));
                Control.Columns.Add(new DataColumn("FormaPago", typeof(string)));

                Control.Rows.Add(NotaActual.FacturaId, string.Format("{0} - {1}", NotaActual.Serie, NotaActual.NoNotaCredito), "");

                Movimiento.Tables.Add(Encabezado);
                Movimiento.Tables.Add(Detalle);
                Movimiento.Tables.Add(Control);

                // Se define la ruta del reporte
                var reportPath = Server.MapPath("~/Reports/ReportMovNotaCredito.rdlc");

                // se obtienen los bytes del reporte en pdf
                var bytes = GetReportBytes(reportPath, Movimiento, 8.5m, 11.0m, 0.2m, 0m);

                return File(bytes, "application/pdf");
            }

            return View();
        }
    }
}