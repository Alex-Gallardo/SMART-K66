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
    public class CierreController : Controller
    {
        #region Metodos Privados 

            private void CargaControles(bool centroGeneral = false)
            {
                var Agencias = new AgenciaBL().ObtenerListado(false);

                if (centroGeneral)
                {
                    if (Agencias != null && Agencias.Count() > 0)
                    {
                        Agencias.Insert(0, new Agencia() { AgenciaId = 0, Nombre = "GENERAL" });
                    }
                }   

                ViewBag.Agencias = new SelectList(Agencias, "AgenciaId", "Nombre");
            }

            private byte[] GetReportBytes(string reportPath, DataSet reportDataSource, decimal pageWidth = 13.38m, decimal pageHeight = 8.5m, decimal MarginLeft = 1m, decimal MarginRight = 1m)
            {

                byte[] reportBytes = null;

                // Se crea la instancia del reporte y se cargan sus datos.
                LocalReport reporte = new LocalReport() { ReportPath = reportPath };
                reporte.DataSources.Add(new ReportDataSource("CierreEncabezado", reportDataSource.Tables[0]));
                reporte.DataSources.Add(new ReportDataSource("CierreDetalle", reportDataSource.Tables[1]));
           
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

        // GET: Cierre
        [Permiso("Control.Cierre_Caja.Crear")]
        public ActionResult Caja()
        {
            CustomHelper.setTitle("Cierre de Caja", "Nuevo");

            return View(new CierreBL().ObtenerDisponibilidadCierre(CustomHelper.getAgenciaId(), CustomHelper.getUserId(), DateTime.Today));
        }

        [Permiso("Control.Cierre_Caja.Crear")]
        [HttpPost]
        public ActionResult Caja(long[] formaPagoIDs, decimal[] cantidadCajeroIDs, decimal[] cantidadSistemaIDs, decimal gastoIDs, decimal retiroIDs)
        {
            string strMensaje = new CierreBL().Guardar(formaPagoIDs, cantidadCajeroIDs, cantidadSistemaIDs, gastoIDs, retiroIDs, CustomHelper.getAgenciaId(), CustomHelper.getUserId());

            if (strMensaje.Equals("OK"))
            {
                TempData["Cierre-Success"] = strMensaje;
                return RedirectToAction("Caja");
            }
            else
            {
                ModelState.AddModelError("", strMensaje);
            }

            return View(new CierreBL().ObtenerDisponibilidadCierre(CustomHelper.getAgenciaId(), CustomHelper.getUserId(), DateTime.Today));
        }

        [Permiso("Control.Cierre_Caja.Crear")]
        public ActionResult Administracion(long? AgenciaId, DateTime? FechaInicial, DateTime? FechaFinal)
        {
            CustomHelper.setTitle("Cierre de Caja", "Administración");
            CierreCajaModelxCajero CierresActuales = new CierreCajaModelxCajero();

            if (AgenciaId != null && FechaInicial != null && FechaFinal != null)
            {
                CierresActuales = new CierreBL().ObtenerCierres(AgenciaId.Value, FechaInicial.Value, FechaFinal.Value);               
            }

            this.CargaControles(true);
            return View(CierresActuales);
        }

        [Permiso("Control.Cierre_Caja.Seguimiento")]
        public ActionResult Seguimiento()
        {
            CustomHelper.setTitle("Cierre de Caja", "Seguimiento");

            return View(new CierreBL().ObtenerCierresPendientes(CustomHelper.getUserId()));
        }

        [Permiso("Control.Cierre_Caja.Crear")]
        public ActionResult Historial(long AgenciaId, long CajeroId, DateTime Fecha)
        {
            CustomHelper.setTitle("Corte de Caja", "Historial");

            return View(new CorteCajaBL().ObtenerHistorialxAgenciaCajero(AgenciaId, CajeroId, Fecha));
        }

        [Permiso("Control.Reporte.Boleta_Cierre")]
        public ActionResult Boleta(long id)
        {
            Cierre CierreActual = new CierreBL().ObtenerPorId(id);

            if (CierreActual != null)
            {
                DataSet Movimiento = new DataSet("Inventario");

                DataTable Encabezado = new DataTable("CierreEncabezado");
                DataTable Detalle = new DataTable("CierreDetalle");
               
                Encabezado.Columns.Add(new DataColumn("CierreId", typeof(long)));
                Encabezado.Columns.Add(new DataColumn("Agencia", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Cajero", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Fecha", typeof(string)));           
             
                Encabezado.Rows.Add(CierreActual.CierreId, CierreActual.Agencia.Nombre, CierreActual.Cajero.Nombre, CierreActual.Fecha.ToString("dd/MM/yyyy"));

                Detalle.Columns.Add(new DataColumn("CierreId", typeof(long)));
                Detalle.Columns.Add(new DataColumn("Forma", typeof(string)));
                Detalle.Columns.Add(new DataColumn("MontoSistema", typeof(decimal)));
                Detalle.Columns.Add(new DataColumn("MontoCajero", typeof(decimal)));
                Detalle.Columns.Add(new DataColumn("Faltante", typeof(decimal)));
                Detalle.Columns.Add(new DataColumn("Sobrante", typeof(decimal)));

                if (CierreActual.Detalles != null && CierreActual.Detalles.Count() > 0)
                {
                    foreach (var DetalleActual in CierreActual.Detalles)
                    {
                        Detalle.Rows.Add(CierreActual.CierreId, DetalleActual.FormaPago.Nombre, DetalleActual.MontoSistema, DetalleActual.MontoCajero, DetalleActual.Faltante, DetalleActual.Sobrante);
                    }
                }
              
                Movimiento.Tables.Add(Encabezado);
                Movimiento.Tables.Add(Detalle);
              
                // Se define la ruta del reporte
                var reportPath = Server.MapPath("~/Reports/ReportMovCierre.rdlc");

                // se obtienen los bytes del reporte en pdf
                var bytes = GetReportBytes(reportPath, Movimiento, 8.5m, 11.0m, 0m, 0m);

                return File(bytes, "application/pdf");
            }

            return View();
        }

        [ActionName("ActualizarCierreRecibir")]
        public JsonResult ActualizarCierreRecibir(long cierreId)
        {
            if (cierreId > 0)
            {
                string Mensaje = new CierreBL().Recibir(cierreId);
                if (Mensaje.Equals("OK"))
                {
                    return Json(new { Operacion = true }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ActionName("Eliminar")]
        public JsonResult Eliminar(long id)
        {
            if (id > 0)
            {
                string Mensaje = new CierreBL().Eliminar(id);
                if (Mensaje.Equals("OK"))
                {
                    return Json(new { Operacion = true }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }
    }
}