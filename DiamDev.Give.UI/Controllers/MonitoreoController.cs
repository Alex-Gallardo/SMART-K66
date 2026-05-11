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

namespace DiamDev.Give.UI.Controllers
{
    [Authorize]
    [Seguridad]
    [HandleError]
    public class MonitoreoController : Controller
    {
        #region Metodos Privados

            private void CargaControles()
            {
                var Agencias = new AgenciaBL().ObtenerListado(false);

                ViewBag.Agencias = new SelectList(Agencias, "AgenciaId", "Nombre");
            }

            private byte[] GetReportBytes(string reportPath, DataSet reportDataSource, decimal pageWidth = 13.38m, decimal pageHeight = 8.5m, decimal MarginLeft = 1m, decimal MarginRight = 1m)
            {

                byte[] reportBytes = null;

                // Se crea la instancia del reporte y se cargan sus datos.
                LocalReport reporte = new LocalReport() { ReportPath = reportPath };
                reporte.DataSources.Add(new ReportDataSource("Laboratorio", reportDataSource.Tables[0]));

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

        // GET: Monitoreo
        [Permiso("Control.Corte_Caja.Crear")]
        public ActionResult Corte_Caja()
        {
            CustomHelper.setTitle("Corte de Caja", "Nuevo");

            this.CargaControles();
            return View();
        }

        [Permiso("Control.Corte_Caja.Crear")]
        [HttpPost]
        [ActionName("NuevoCorteCaja")]
        public ActionResult NuevoCorteCaja(CorteCaja modelo)
        {
            modelo.OperoId = CustomHelper.getUserId();

            if (ModelState.IsValid)
            {
                string Mensaje = new CorteCajaBL().Guardar(modelo);

                if (Mensaje.Equals("OK"))
                {
                    return Json(new { Operacion = true }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }
               

        [Permiso("Control.Corte_Caja.Historial")]
        public ActionResult Historial(long agenciaId, long cajeroId)
        {
            CustomHelper.setTitle("Corte de Caja", "Historial");

            return View(new CorteCajaBL().ObtenerHistorialxAgenciaCajero(agenciaId, cajeroId, DateTime.Today));
        }

        [Permiso("Control.Reporte.Corte_Caja")]
        public ActionResult Corte(long id)
        {
            CorteCaja CorteCajaActual = new CorteCajaBL().ObtenerPorId(id);

            if (CorteCajaActual != null)
            {
                DataSet Laboratorio = new DataSet("Laboratorio");

                DataTable Encabezado = new DataTable("Laboratorio");

                Encabezado.Columns.Add(new DataColumn("LaboratorioId", typeof(long)));
                Encabezado.Columns.Add(new DataColumn("Agencia", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("ProductoBase", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("ProductoDestino", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("CantidadBase", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("CantidadDestino", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Responsable", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Fecha", typeof(string)));

                Encabezado.Rows.Add(CorteCajaActual.CorteId, CorteCajaActual.Agencia.Nombre, CorteCajaActual.Cajero.Nombre, "", CorteCajaActual.Gasto.ToString("C4"), CorteCajaActual.Monto.ToString("C4"), CorteCajaActual.Responsable.Nombre, CorteCajaActual.Fecha.ToString("dd/MM/yyyy"));

                Laboratorio.Tables.Add(Encabezado);

                // Se define la ruta del reporte
                var reportPath = Server.MapPath("~/Reports/ReportMovCorteCaja.rdlc");

                // se obtienen los bytes del reporte en pdf
                var bytes = GetReportBytes(reportPath, Laboratorio, 8.5m, 11.0m, 0.2m, 0m);

                return File(bytes, "application/pdf");
            }

            return View();
        }

        [Permiso("Control.Corte_Caja.Crear")]
        public ActionResult GetCortes(long agenciaId, long cajeroId)
        {            
            return PartialView("_Cortes", new CorteCajaBL().ObtenerListadoPorFecha(agenciaId, cajeroId, DateTime.Today));
        }

        [ActionName("ObtenerCajerosxAgenciaId")]
        public JsonResult ObtenerCajerosxAgenciaId(long id)
        {
            IList _result = new List<SelectListItem>();
            _result = new UsuarioBL().ObtenerUsuarioxAgenciaId(id).Select(m => new SelectListItem() { Text = m.Nombre, Value = m.UsuarioId.ToString() }).ToList();
            return Json(_result, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerCorteActual")]
        public JsonResult ObtenerCorteActual(long agenciaId, long cajeroId)
        {
            if (agenciaId > 0 && cajeroId > 0)
            {
                return Json(new { Operacion = true, Data = new CorteCajaBL().ObtenerDisponibilidadCorteCaja(agenciaId, cajeroId, DateTime.Today) }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ActualizarCorteRecibir")]
        public JsonResult ActualizarCorteRecibir(long corteId)
        {
            if (corteId > 0)
            {
                string Mensaje = new CorteCajaBL().Recibir(corteId);
                if (Mensaje.Equals("OK"))
                {
                    return Json(new { Operacion = true }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }
    }
}