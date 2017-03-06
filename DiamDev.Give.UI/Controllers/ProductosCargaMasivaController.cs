using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DiamDev.Give.UI.Controllers
{
    public class ProductosCargaMasivaController : Controller
    {
        // GET: ProductoImportacion
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Index(List<HttpPostedFileBase> archivos)
        {

            bool success = false;
            string error = null;
            

            if (archivos == null || archivos.Count < 1)
            {
                error = "El archivo está vacío";
                return Resultado(success, error);
            }

            var archivo = archivos.FirstOrDefault(x => x != null && x.ContentLength > 0 && !string.IsNullOrWhiteSpace(x.FileName));

            if (!archivo.FileName.EndsWith("xlsx"))
            {
                error = "El archivo debe ser un excel (.xlsx) valido.";
                return Resultado(success, error);
            }

            var id = Guid.NewGuid();
            var hoy = DateTime.Today;
            var anio = hoy.Year;
            var mes = hoy.Month;
            var dia = hoy.Day;
            var fileDir = Server.MapPath("~/App_Data/Productos/");
            var fileName = Path.Combine(fileDir, $"{anio}-{mes:00}-{dia:00}-{id}.xlsx");
            if (!Directory.Exists(fileDir))
            {
                Directory.CreateDirectory(fileDir);
            }
            
            archivo.SaveAs(fileName);

            // TODO: validar Formato
            success = true;
            return Resultado(success, error);
        }

        private ActionResult Resultado(bool success, string error)
        {
            bool isAjaxRequest = Request["ajax"] == "1";
            if (isAjaxRequest)
            {
                return Json(new { success, error });
            }
            else
            {
                ViewBag.UploadSuccess = success;
                ViewBag.UploadError = error;
                return View();
            }
        }
    }
}