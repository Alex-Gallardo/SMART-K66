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
    public class LaboratorioController : Controller
    {
        #region Metodos Privados

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

        // GET: Laboratorio
        [Permiso("Control.Laboratorio.Ver_Listado")]
        public ActionResult Index(int? page, DateTime? FechaInicial, DateTime? FechaFinal)
        {
            CustomHelper.setTitle("Laboratorio", "Listado");
            List<Laboratorio> Laboratorios = new List<Laboratorio>();
         
            try
            {
                if (!FechaInicial.HasValue && !FechaFinal.HasValue)
                {
                    FechaInicial = DateTime.Today;
                    FechaFinal = DateTime.Today;
                }

                Laboratorios = new LaboratorioBL().ObtenerListadoPorFecha(FechaInicial.Value, FechaFinal.Value, CustomHelper.getUserId()).ToList();          
            }
            catch (Exception)
            {
            }

            int pageSize = 15;
            int pageNumber = (page ?? 1);
            return View(Laboratorios.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Laboratorio.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Laboratorio", "Nuevo");

            return View();
        }

        [Permiso("Control.Laboratorio.Crear")]
        [HttpPost]
        public ActionResult Crear(Laboratorio modelo)
        {  
            modelo.AgenciaId = CustomHelper.getAgenciaId();
            modelo.UsrCreo = CustomHelper.getUserId();

            if (ModelState.IsValid)
            {
                string strMensaje = new LaboratorioBL().Guardar(modelo);
                if (strMensaje.Equals("OK"))
                {
                    TempData["Laboratorio-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }                     
           
            return View(modelo);
        }

        [Permiso("Control.Laboratorio.Detalle")]
        public ActionResult Detalle(long id)
        {
            Laboratorio LaboratorioActual = new LaboratorioBL().ObtenerPorId(id, true);

            if (LaboratorioActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Laboratorio", "Detalle");

            return View(LaboratorioActual);
        }

        [Permiso("Control.Reporte.Laboratorio")]
        public ActionResult Boleta(long Id)
        {
            Laboratorio LaboratorioActual = new LaboratorioBL().ObtenerPorId(Id, true);

            if (LaboratorioActual != null)
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
   
                Encabezado.Rows.Add(LaboratorioActual.LaboratorioId, LaboratorioActual.Agencia.Nombre, string.Format("{0} - {1}", LaboratorioActual.ProductoBase.Nombre, LaboratorioActual.ProductoBase.Unidad.Nombre), string.Format("{0} - {1}", LaboratorioActual.ProductoDestino.Nombre, LaboratorioActual.ProductoDestino.Unidad.Nombre), LaboratorioActual.CantidadBase, LaboratorioActual.CantidadDestino, LaboratorioActual.UsuarioCreo.Nombre, LaboratorioActual.Fecha.ToString("dd/MM/yyyy"));
                             
                Laboratorio.Tables.Add(Encabezado);
           
                // Se define la ruta del reporte
                var reportPath = Server.MapPath("~/Reports/ReportMovLaboratorio.rdlc");

                // se obtienen los bytes del reporte en pdf
                var bytes = GetReportBytes(reportPath, Laboratorio, 8.5m, 11.0m, 0.2m, 0m);

                return File(bytes, "application/pdf");
            }

            return View();
        }
    }
}