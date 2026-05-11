using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using DiamDev.Give.BLL;
using DiamDev.Give.Entities;
using DiamDev.Give.UI.App_Start;
using DiamDev.Give.UI.Models;
using Microsoft.Reporting.WebForms;

namespace DiamDev.Give.UI.Controllers
{
    [Authorize]
    [Seguridad]
    [HandleError]
    public class TokenController : Controller
    {
        #region Metodos Privados

            private byte[] GetReportBytes(string reportPath, DataSet reportDataSource, decimal pageWidth = 13.38m, decimal pageHeight = 8.5m, decimal MarginLeft = 1m, decimal MarginRight = 1m)
            {

                byte[] reportBytes = null;

                // Se crea la instancia del reporte y se cargan sus datos.
                LocalReport reporte = new LocalReport() { ReportPath = reportPath };
                reporte.DataSources.Add(new ReportDataSource("Token", reportDataSource.Tables[0]));               

                string deviceInfo =
                    "<DeviceInfo>" +
                    "  <OutputFormat>PDF</OutputFormat>" + // Formato del documento PDF
                    "  <PageWidth>" + pageWidth + "in</PageWidth>" + // Ancho de 8.5 pulgadas para paginas oficio
                    "  <PageHeight>" + pageHeight + "in</PageHeight>" + // Alto de 13.38 pulgadas para paginas oficio
                    "  <MarginTop>0.0in</MarginTop>" + // margen superior de 0.5 pulgadas
                    "  <MarginLeft>" + MarginLeft + "</MarginLeft>" + // margen izquierdo de 1 pulgada
                    "  <MarginRight>" + MarginRight + "</MarginRight>" + // margen derecho de 1 pulgada.
                    "  <MarginBottom>0.0in</MarginBottom>" + // margen inferior de 0.5 pulgadas.
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

        // GET: Token
        [Permiso("Control.Token.Ver_Listado")]
        public ActionResult Index(DateTime? FechaInicial, DateTime? FechaFinal)
        {
            CustomHelper.setTitle("Token", "Listado");

            List<Token> Tokens = new List<Token>();

            if (!FechaInicial.HasValue && !FechaFinal.HasValue)
            {
                FechaInicial = DateTime.Today;
                FechaFinal = DateTime.Today;
            }

            try
            {
                Tokens = new TokenBL().ObtenerListado(FechaInicial.Value, FechaFinal.Value).ToList();
            }
            catch (Exception ex)
            {
                ViewBag.Error = string.Format("Message: {0} StackTrace: {1}", ex.Message, ex.StackTrace);
                return View("~/Views/Shared/Error.cshtml");
            }

            ViewBag.fechaInicial = FechaInicial.Value.ToString("yyyy-MM-dd");
            ViewBag.fechaFinal = FechaFinal.Value.ToString("yyyy-MM-dd");

            return View(Tokens);
        }

        [Permiso("Control.Token.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Token", "Nuevo");

            string strAtributo = "checked='checked'";

            ViewBag.administrativoSi = "";
            ViewBag.administrativoNo = strAtributo;

            return View();
        }

        [HttpPost]
        [Permiso("Control.Token.Crear")]
        public ActionResult Crear(Token modelo, bool administrativo)
        {
            if (ModelState.IsValid)
            {
                modelo.Administrativo = administrativo;
                string strMensaje = new TokenBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Token-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }

            string strAtributo = "checked='checked'";

            ViewBag.administrativoSi = administrativo == true ? strAtributo : "";
            ViewBag.administrativoNo = administrativo == false ? strAtributo : "";

            return View(modelo);
        }

        [Permiso("Control.Token.Boleta")]
        public ActionResult Boleta(long id)
        {
            Token TokenActual = new TokenBL().ObtenerPorId(id);

            if (TokenActual != null)
            {
                DataSet Movimiento = new DataSet("Token");

                DataTable Token = new DataTable("Token");

                Token.Columns.Add(new DataColumn("TokenId", typeof(long)));
                Token.Columns.Add(new DataColumn("Token", typeof(string)));

                Token.Rows.Add(TokenActual.TokenId, TokenActual.TokenValido);
                
                Movimiento.Tables.Add(Token);
                
                // Se define la ruta del reporte
                var reportPath = Server.MapPath("~/Reports/ReportMovToken.rdlc");

                // se obtienen los bytes del reporte en pdf
                var bytes = GetReportBytes(reportPath, Movimiento, 2.0m, 2.0m, 0m, 0m);

                return File(bytes, "application/pdf");
            }

            return View();
        }
    }
}