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
    public class Contrasena_PagoController : Controller
    {    
        #region Metodos Privados

            private void CargaControles()
            {               
                var Proveedores = new ProveedorBL().ObtenerListado(false);
                var Formas = new FormaPagoBL().ObtenerListado(false);
             
                ViewBag.Proveedores = new SelectList(Proveedores, "ProveedorId", "Nombre");
                ViewBag.Formas = new SelectList(Formas, "FormaPagoId", "Nombre");
            }

            private byte[] GetReportBytes(string reportPath, DataSet reportDataSource, decimal pageWidth = 13.38m, decimal pageHeight = 8.5m, decimal MarginLeft = 1m, decimal MarginRight = 1m)
            {
                byte[] reportBytes = null;

                // Se crea la instancia del reporte y se cargan sus datos.
                LocalReport reporte = new LocalReport() { ReportPath = reportPath };
                reporte.DataSources.Add(new ReportDataSource("Contrasena", reportDataSource.Tables[0]));

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

        // GET: Contrasena_Pago
        [Permiso("Control.Contrasena_Pago.Ver_Listado")]
        public ActionResult Index(int? page, string search)
        {
            CustomHelper.setTitle("Contraseña de Pago", "Listado");
            List<ContrasenaPago> Contrasenas = new List<ContrasenaPago>();
         
            try
            {
                if (!string.IsNullOrWhiteSpace(search) && search != null)
                {
                    Contrasenas = new ContrasenaPagoBL().Buscar(search).ToList();
                }
                else
                {
                    Contrasenas = new ContrasenaPagoBL().ObtenerListado().ToList();
                }
            }
            catch (Exception)
            {
            }

            ViewBag.Search = search;

            int pageSize = 15;
            int pageNumber = (page ?? 1);
            return View(Contrasenas.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Contrasena_Pago.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Contraseña de Pago", "Nueva");
          
            this.CargaControles();
            return View();
        }

        [Permiso("Control.Contrasena_Pago.Crear")]
        [HttpPost]
        public ActionResult Crear(ContrasenaPago modelo)
        {           
            modelo.UsrCreo = CustomHelper.getUserId();
        
            if (ModelState.IsValid)
            {
                string strMensaje = new ContrasenaPagoBL().Guardar(modelo);
                if (strMensaje.Equals("OK"))
                {
                    TempData["Contrasena_Pago-Success"] = strMensaje;
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

        [ActionName("ActualizarEstadoContrasenaPago")]
        public JsonResult ActualizarEstadoContrasenaPago(long id)
        {
            if (id > 0)
            {
                string Mensaje = new ContrasenaPagoBL().Operar(id);
                if (Mensaje.Equals("OK"))
                {
                    return Json(new { Operacion = true }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [Permiso("Control.Contrasena_Pago.Boleta")]
        public ActionResult Boleta(long id)
        {
            ContrasenaPago ContrasenaActual = new ContrasenaPagoBL().ObtenerPorId(id);

            if (ContrasenaActual != null)
            {
                DataSet Contrasena = new DataSet("Contrasena");

                DataTable Encabezado = new DataTable("Contrasena");

                Encabezado.Columns.Add(new DataColumn("ContrasenaId", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Documento", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Proveedor", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Fecha", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Responsable", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Forma", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Monto", typeof(string)));

                Encabezado.Rows.Add(ContrasenaActual.ContrasenaId, ContrasenaActual.Documento, ContrasenaActual.Proveedor.Nombre, ContrasenaActual.FechaPago.ToString("dd/MM/yyyy"), ContrasenaActual.UsuarioCreo.Nombre, ContrasenaActual.Pago.Nombre, ContrasenaActual.Monto.ToString("C"));

                Contrasena.Tables.Add(Encabezado);

                // Se define la ruta del reporte
                var reportPath = Server.MapPath("~/Reports/ReportMovContrasena.rdlc");

                // se obtienen los bytes del reporte en pdf
                var bytes = GetReportBytes(reportPath, Contrasena, 8.5m, 11.0m, 0.2m, 0m);

                return File(bytes, "application/pdf");
            }

            return View();
        }
    }
}