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
    public class NominaController : Controller
    {
        #region Metodos Privados

            private void CargaControles()
            {
                var Tipos = new NominaTipoBL().ObtenerListado();

                ViewBag.Tipos = new SelectList(Tipos, "TipoId", "Nombre");
            }

            private byte[] GetReportBytes(string reportPath, DataSet reportDataSource, decimal pageWidth = 13.38m, decimal pageHeight = 8.5m, decimal MarginLeft = 1m, decimal MarginRight = 1m)
            {

                byte[] reportBytes = null;

                // Se crea la instancia del reporte y se cargan sus datos.
                LocalReport reporte = new LocalReport() { ReportPath = reportPath };
                reporte.DataSources.Add(new ReportDataSource("NominaEncabezado", reportDataSource.Tables[0]));
                reporte.DataSources.Add(new ReportDataSource("NominaDetalle", reportDataSource.Tables[1]));

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

        // GET: Nomina
        [Permiso("Control.Nomina.Ver_Listado")]
        public ActionResult Index(DateTime? FechaInicial, DateTime? FechaFinal)
        {
            CustomHelper.setTitle("Nomina", "Listado");

            List<Nomina> Nominas = new List<Nomina>();

            if (!FechaInicial.HasValue && !FechaFinal.HasValue)
            {
                FechaInicial = DateTime.Today;
                FechaFinal = DateTime.Today;
            }

            try
            {
                Nominas = new NominaBL().ObtenerListado(FechaInicial.Value, FechaFinal.Value).ToList();
            }
            catch (Exception ex)
            {
                ViewBag.Error = string.Format("Message: {0} StackTrace: {1}", ex.Message, ex.StackTrace);
                return View("~/Views/Shared/Error.cshtml");
            }

            return View(Nominas);
        }

        [Permiso("Control.Nomina.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Nomina", "Nueva");

            this.CargaControles();
            return View();
        }

        [Permiso("Control.Nomina.Crear")]
        [HttpPost]
        public ActionResult Crear(Nomina modelo, EmpleadoNominaModel[] empleados)
        {
            if (empleados == null || empleados.Length == 0)
            {
                ModelState.AddModelError("", "Para realizar una nomina debe de asignar personal");
            }
            else
            {
                modelo.Detalles = new List<NominaDetalle>();
                for (int i = 0; i < empleados.Length; i++)
                {
                    NominaDetalle Detalle = new NominaDetalle();
                    Detalle.PersonalId = empleados[i].PersonalId;
                    Detalle.Puesto = empleados[i].Puesto;
                    Detalle.Dias = empleados[i].Dias;
                    Detalle.Sueldo = empleados[i].Sueldo;
                    Detalle.Bonificacion = empleados[i].Bonificacion;
                    Detalle.OtrosIngresos = empleados[i].OtrosIngresos;
                    Detalle.IGSS = empleados[i].IGSS;
                    Detalle.OtrosDescuentos = empleados[i].OtrosDescuentos;

                    modelo.Detalles.Add(Detalle);
                }
            }

            if (ModelState.IsValid)
            {
                string strMensaje = new NominaBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Nomina-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }

            this.CargaControles();
            return View(modelo);
        }

        [Permiso("Control.Nomina.Detalle")]
        public ActionResult Detalle(long id)
        {
            Nomina NominaActual = new NominaBL().ObtenerPorId(id, true);

            if (NominaActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Nomina", "Detalle");

            return View(NominaActual);
        }

        [Permiso("Control.Reporte.Boleta_Nomina")]
        public ActionResult Boleta(long id)
        {
            Nomina NominaActual = new NominaBL().ObtenerPorId(id, true);

            if (NominaActual != null)
            {
                DataSet Nomina = new DataSet("Inventario");

                DataTable Encabezado = new DataTable("NominaEncabezado");
                DataTable Detalle = new DataTable("NominaDetalle");

                Encabezado.Columns.Add(new DataColumn("NominaId", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Tipo", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("FechaInicial", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("FechaFinal", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Descripcion", typeof(string)));

                Encabezado.Rows.Add(NominaActual.NominaId, NominaActual.Tipo.Nombre, NominaActual.FechaInicial.ToString("dd/MM/yyyy"), NominaActual.FechaFinal.ToString("dd/MM/yyyy"), NominaActual.Descripcion);

                Detalle.Columns.Add(new DataColumn("NominaId", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Nombre", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Puesto", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Dias", typeof(int)));
                Detalle.Columns.Add(new DataColumn("Sueldo", typeof(decimal)));
                Detalle.Columns.Add(new DataColumn("Bonificacion", typeof(decimal)));
                Detalle.Columns.Add(new DataColumn("OtrosIngresos", typeof(decimal)));
                Detalle.Columns.Add(new DataColumn("IGSS", typeof(decimal)));
                Detalle.Columns.Add(new DataColumn("OtrosDescuentos", typeof(decimal)));

                if (NominaActual.Detalles != null && NominaActual.Detalles.Count() > 0)
                {
                    foreach (var DetalleActual in NominaActual.Detalles)
                    {
                        Detalle.Rows.Add(NominaActual.NominaId, DetalleActual.Personal.Nombre, DetalleActual.Puesto, DetalleActual.Dias, DetalleActual.Sueldo, DetalleActual.Bonificacion, DetalleActual.OtrosIngresos, DetalleActual.IGSS, DetalleActual.OtrosDescuentos);
                    }
                }

                Nomina.Tables.Add(Encabezado);
                Nomina.Tables.Add(Detalle);

                // Se define la ruta del reporte
                var reportPath = Server.MapPath("~/Reports/ReportMovNomina.rdlc");

                // se obtienen los bytes del reporte en pdf
                var bytes = GetReportBytes(reportPath, Nomina, 11.0m, 8.5m, 0.2m, 0m);

                return File(bytes, "application/pdf");
            }

            return View();
        }

        [Permiso("Control.Reporte.Boleta_Nomina")]
        public ActionResult Comprobante(long id)
        {
            Nomina NominaActual = new NominaBL().ObtenerPorId(id, true);

            if (NominaActual != null)
            {
                DataSet Nomina = new DataSet("Inventario");

                DataTable Encabezado = new DataTable("NominaEncabezado");
                DataTable Detalle = new DataTable("NominaDetalle");

                Encabezado.Columns.Add(new DataColumn("NominaId", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Tipo", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("FechaInicial", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("FechaFinal", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Descripcion", typeof(string)));

                Encabezado.Rows.Add(NominaActual.NominaId, NominaActual.Tipo.Nombre, NominaActual.FechaInicial.ToString("dd/MM/yyyy"), NominaActual.FechaFinal.ToString("dd/MM/yyyy"), NominaActual.Descripcion);

                Detalle.Columns.Add(new DataColumn("NominaId", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Nombre", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Puesto", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Dias", typeof(int)));
                Detalle.Columns.Add(new DataColumn("Sueldo", typeof(decimal)));
                Detalle.Columns.Add(new DataColumn("Bonificacion", typeof(decimal)));
                Detalle.Columns.Add(new DataColumn("OtrosIngresos", typeof(decimal)));
                Detalle.Columns.Add(new DataColumn("IGSS", typeof(decimal)));
                Detalle.Columns.Add(new DataColumn("OtrosDescuentos", typeof(decimal)));

                if (NominaActual.Detalles != null && NominaActual.Detalles.Count() > 0)
                {
                    foreach (var DetalleActual in NominaActual.Detalles)
                    {
                        Detalle.Rows.Add(NominaActual.NominaId, DetalleActual.Personal.Nombre, DetalleActual.Puesto, DetalleActual.Dias, DetalleActual.Sueldo, DetalleActual.Bonificacion, DetalleActual.OtrosIngresos, DetalleActual.IGSS, DetalleActual.OtrosDescuentos);
                    }
                }

                Nomina.Tables.Add(Encabezado);
                Nomina.Tables.Add(Detalle);

                // Se define la ruta del reporte
                var reportPath = Server.MapPath("~/Reports/ReportMovComprobante.rdlc");

                // se obtienen los bytes del reporte en pdf
                var bytes = GetReportBytes(reportPath, Nomina, 8.5m, 5.5m, 0.2m, 0m);

                return File(bytes, "application/pdf");
            }

            return View();
        }

        [ActionName("ObtenerPersonalNomina")]
        public JsonResult ObtenerPersonalNomina(DateTime? fechaInicial, DateTime? fechaFinal, int tipoId)
        {
            if (fechaInicial != null && fechaFinal != null)
            {
                List<NominaModel> Empleados = new NominaBL().ObtenerListadoNominaLiquidar(fechaInicial.Value, fechaFinal.Value, tipoId);
                if (Empleados != null && Empleados.Count() > 0)
                {
                    return Json(new { Operacion = true, Data = Empleados }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }
    }
}