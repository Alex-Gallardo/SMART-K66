using DiamDev.Give.BLL.Excel;
using Newtonsoft.Json;
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

            var nombresColumnas = new[] {"ID", "CODIGO", "BODEGA", "NOMBRE", "MARCA", "CANTIDAD", "COSTO", "P VENTA", "MIN", "MAX", "RENT Q.", "RENT %", "MODIFICACION"};
            var rowsCount = 0;

            var filas = new List<ProductoCargaMasivaDetalle>();

            foreach (var worksheet in Workbook.Worksheets(fileName))
            {
                foreach (var row in worksheet.Rows)
                {
                    rowsCount++;

                    if (rowsCount == 1)
                    {
                        var nombresEnArchivo = row.Cells.Select(x => x.Text.Trim()).ToArray();
                        for (int i = 0; i < nombresColumnas.Length; i++)
                        {
                            if (nombresColumnas[i]!= nombresEnArchivo[i])
                            {
                                error = "la primer linea del archivo debe contener el nombre de las columnas: " +
                                    "[" + string.Join(", ", nombresColumnas) + "]";
                                return Resultado(success, error);
                            }
                        }
                        continue;
                    }
                    string rowId;
                    string codigo;
                    string bodega;
                    string marca;
                    double cantidad;
                    double costo;
                    double precioVenta;
                    double min;
                    double max;
                    double rentQ;
                    double rentP;
                    string modificacion;

                    var cells = row.Cells;
                    rowId = cells[0].Text.Trim();
                    codigo = cells[1].Text.Trim();
                    bodega = cells[2].Text.Trim();
                    marca = cells[3].Text.Trim();
                    cantidad = cells[4].Amount;
                    costo = cells[5].Amount;
                    precioVenta = cells[6].Amount;
                    min = cells[7].Amount;
                    max = cells[8].Amount;
                    rentQ = cells[9].Amount;
                    rentP = cells[10].Amount;
                    modificacion = cells.Length > 11 ? cells[11].Text.Trim() : "";

                    filas.Add(new ProductoCargaMasivaDetalle {
                        Id = rowId,
                        Codigo = codigo,
                        Bodega = bodega,
                        Marca = marca,
                        Cantidad = cantidad,
                        Costo = costo,
                        PrecioVenta = precioVenta,
                        Min = min,
                        Max = max,
                        RentQ = rentQ,
                        RentP = rentP,
                        Modificacion = modificacion
                    });
                }
            }

            if (filas.Count == 0)
            {
                error = "El archivo está vacío.";
                return Resultado(success, error);
            }

            var jsonName = Path.Combine(fileDir, Path.GetFileNameWithoutExtension(fileName) + ".json");
            var json = JsonConvert.SerializeObject(filas);

            System.IO.File.WriteAllText(jsonName, json);

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

        private class ProductoCargaMasivaDetalle
        {
            public string Bodega { get; set; }
            public double Cantidad { get; set; }
            public string Codigo { get; set; }
            public double Costo { get; set; }
            public string Id { get; set; }
            public string Marca { get; set; }
            public double Max { get; set; }
            public double Min { get; set; }
            public string Modificacion { get; set; }
            public double PrecioVenta { get; set; }
            public double RentP { get; set; }
            public double RentQ { get; set; }
        }
    }
}