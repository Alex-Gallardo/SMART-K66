using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DiamDev.Give.UI.Controllers
{
    public class KardexController: Controller
    {
        
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Generar(DateTime desde, DateTime hasta)
        {

            RegistroKardex[] kardex;
            using (var db = new GiveContext())
            {
                kardex = db.RegistrosKardex.Where(x => x.Fecha >= desde && x.Fecha <= hasta).OrderBy(x => x.FechaHora).ToArray();
            }

            var agencias = kardex.Select(x => new { x.AgenciaId, x.AgenciaNombre }).Distinct().ToArray();
            var agenciasIndex = new Dictionary<long, int>();

            using (var pck = new ExcelPackage())
            {
                var ws = pck.Workbook.Worksheets.Add("Kardex");

                ws.Cells["A3"].Value = "Proyecto Give Guatemala, S.A.";
                ws.Cells["A3:E3"].Merge = true;

                ws.Cells["A4"].Value = $"Kardex de inventario al {hasta:dd/MM/yyyy}";
                ws.Cells["A4:E4"].Merge = true;

                ws.Cells["A7"].Value = "Id";
                ws.Cells["B7"].Value = "Código";
                ws.Cells["C7"].Value = "Marca";
                ws.Cells["D7"].Value = "Descripción";
                ws.Cells["E7"].Value = "Fecha";
                ws.Cells["F7"].Value = "Documento";
                ws.Cells["G7"].Value = "Concepto";

                ws.Cells["A7:A9"].Merge = true;
                ws.Cells["B7:B9"].Merge = true;
                ws.Cells["C7:C9"].Merge = true;
                ws.Cells["D7:D9"].Merge = true;
                ws.Cells["E7:E9"].Merge = true;
                ws.Cells["F7:F9"].Merge = true;
                ws.Cells["G7:G9"].Merge = true;

                var columna = 8;
                var index = 0;
                foreach (var agencia in agencias)
                {
                    agenciasIndex[agencia.AgenciaId] = index++;

                    ws.Cells[7, columna].Value = agencia.AgenciaNombre;
                    ws.Cells[8, columna].Value = "Ingresos";
                    ws.Cells[9, columna].Value = "Cantidad";
                    ws.Cells[9, columna + 1].Value = "Costo";
                    ws.Cells[9, columna + 2].Value = "Total";

                    ws.Cells[8, columna + 3].Value = "Egresos";
                    ws.Cells[9, columna + 3].Value = "Cantidad";
                    ws.Cells[9, columna + 4].Value = "Costo";
                    ws.Cells[9, columna + 5].Value = "Total";

                    ws.Cells[8, columna + 6].Value = "Existencias";
                    ws.Cells[9, columna + 6].Value = "Cantidad";
                    ws.Cells[9, columna + 7].Value = "Costo Promedio";
                    ws.Cells[9, columna + 8].Value = "Total";

                    ws.Cells[7, columna, 7, columna + 8].Merge = true;
                    ws.Cells[8, columna, 8, columna + 2].Merge = true;
                    ws.Cells[8, columna + 3, 8, columna + 5].Merge = true;
                    ws.Cells[8, columna + 6, 8, columna + 8].Merge = true;
                    
                    columna += 9;

                }

                var fila = 10;

                foreach (var item in kardex)
                {
                    columna = (agenciasIndex[item.AgenciaId] * 9) + 8;

                    ws.Cells[fila, 2].Value = item.ProductoCodigo?.Trim();
                    ws.Cells[fila, 3].Value = item.MarcaNombre?.Trim();
                    ws.Cells[fila, 4].Value = item.ProductoDescripcion?.Trim();
                    ws.Cells[fila, 5].Value = Fecha(item.FechaHora);
                    ws.Cells[fila, 5].Style.Numberformat.Format = "dd/mm/yyyy";
                    
                    // ingresos
                    var cantidadI = ExcelCellBase.GetAddress(fila, columna);
                    var costoI = ExcelCellBase.GetAddress(fila, columna + 1);
                    var totalI = ExcelCellBase.GetAddress(fila, columna + 2);

                    if (item.IngresoCantidadTienda > 0)
                        ws.Cells[cantidadI].Value = item.IngresoCantidadTienda;
                    else
                        ws.Cells[cantidadI].Value = 0;

                    if (item.IngresoCostoTienda > 0)
                        ws.Cells[costoI].Value = item.IngresoCostoTienda;
                    else
                        ws.Cells[costoI].Value = 0;

                    ws.Cells[totalI].Formula = $"{cantidadI}*{costoI}";

                    // egreso
                    var cantidadE = ExcelCellBase.GetAddress(fila, columna + 3);
                    var costoE = ExcelCellBase.GetAddress(fila, columna + 4);
                    var totalE = ExcelCellBase.GetAddress(fila, columna + 5);

                    if (item.SalidaCantidadTienda > 0)
                        ws.Cells[cantidadE].Value = item.SalidaCantidadTienda;
                    else
                        ws.Cells[cantidadE].Value = 0;

                    if (item.SalidaCostoTienda > 0)
                        ws.Cells[costoE].Value = item.SalidaCostoTienda;
                    else
                        ws.Cells[costoE].Value = 0;

                    ws.Cells[totalE].Formula = $"{cantidadE}*{costoE}";

                    // existencias
                    var cantidadX = ExcelCellBase.GetAddress(fila, columna + 6);
                    var costoX = ExcelCellBase.GetAddress(fila, columna + 7);
                    var totalX = ExcelCellBase.GetAddress(fila, columna + 8);

                    ws.Cells[cantidadX].Formula = $"{cantidadI}-{cantidadE}";
                    ws.Cells[costoX].Formula = $"({costoI}+{costoE})/2";
                    ws.Cells[totalX].Formula = $"({cantidadI}*{costoI})+({cantidadE}*{costoE})";

                    fila++;
                }

                ws.Cells[fila, 1].Value = "Totales:";

                columna = 8;
                var primerFila = 10;
                var ultimaFila = fila - 1;

                foreach (var agencia in agencias)
                {
                    // Ingresos

                    // Cantidad
                    var inicio = ExcelCellBase.GetAddress(primerFila, columna);
                    var finale = ExcelCellBase.GetAddress(ultimaFila, columna);
                    ws.Cells[fila, columna].Formula = $"SUM({inicio}:{finale})";

                    // Total
                    inicio = ExcelCellBase.GetAddress(primerFila, columna + 2);
                    finale = ExcelCellBase.GetAddress(ultimaFila, columna + 2);
                    ws.Cells[fila, columna + 2].Formula = $"SUM({inicio}:{finale})";

                    ColocarFormatoNumero(ws, primerFila, columna + 0, fila, columna + 0);
                    ColocarFormatoMoneda(ws, primerFila, columna + 1, fila, columna + 1);
                    ColocarFormatoMoneda(ws, primerFila, columna + 2, fila, columna + 2);

                    // Egresos

                    // Cantidad
                    inicio = ExcelCellBase.GetAddress(primerFila, columna + 3);
                    finale = ExcelCellBase.GetAddress(ultimaFila, columna + 3);
                    ws.Cells[fila, columna + 3].Formula = $"SUM({inicio}:{finale})";

                    // Total
                    inicio = ExcelCellBase.GetAddress(primerFila, columna + 5);
                    finale = ExcelCellBase.GetAddress(ultimaFila, columna + 5);
                    ws.Cells[fila, columna + 5].Formula = $"SUM({inicio}:{finale})";

                    ColocarFormatoNumero(ws, primerFila, columna + 3, fila, columna + 3);
                    ColocarFormatoMoneda(ws, primerFila, columna + 4, fila, columna + 4);
                    ColocarFormatoMoneda(ws, primerFila, columna + 5, fila, columna + 5);

                    // Existencias

                    // Cantidad
                    inicio = ExcelCellBase.GetAddress(primerFila, columna + 6);
                    finale = ExcelCellBase.GetAddress(ultimaFila, columna + 6);
                    ws.Cells[fila, columna + 6].Formula = $"SUM({inicio}:{finale})";

                    // Total
                    inicio = ExcelCellBase.GetAddress(primerFila, columna + 8);
                    finale = ExcelCellBase.GetAddress(ultimaFila, columna + 8);
                    ws.Cells[fila, columna + 8].Formula = $"SUM({inicio}:{finale})";

                    ColocarFormatoNumero(ws, primerFila, columna + 6, fila, columna + 6);
                    ColocarFormatoMoneda(ws, primerFila, columna + 7, fila, columna + 7);
                    ColocarFormatoMoneda(ws, primerFila, columna + 8, fila, columna + 8);


                    columna += 9;
                   
                }

                columna -= 9;
                ColocarFondoAzul(ws, 7, 1, 9, columna + 8);
                ColocarFondoAzul(ws, fila, 1, fila, columna + 8);
                ColocarBordes(ws, 7, 1, fila, columna + 8);

                ws.Calculate();

                AjustarAncho(ws, 7, 1, fila, columna + 8);

                return File(pck.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"kardex_{desde:yyMMdd}_{hasta:yyMMdd}.xlsx");
            }
        }

        private static void ColocarFondoAzul(ExcelWorksheet ws, int filaInicio, int columnaInicio, int filaFin, int columnaFin)
        {
            using (var range = ws.Cells[filaInicio, columnaInicio, filaFin, columnaFin])
            {
                range.Style.Font.Bold = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(142, 180, 227));
            }
        }

        private static void ColocarBordes(ExcelWorksheet ws, int filaInicio, int columnaInicio, int filaFin, int columnaFin)
        {
            using (var range = ws.Cells[filaInicio, columnaInicio, filaFin, columnaFin])
            {
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }
        }

        private static void ColocarFormatoMoneda(ExcelWorksheet ws, int filaInicio, int columnaInicio, int filaFin, int columnaFin)
        {
            using (var range = ws.Cells[filaInicio, columnaInicio, filaFin, columnaFin])
            {
                range.Style.Numberformat.Format = "[$Q-100A]#,##0.00;[RED]([$Q-100A]#,##0.00)";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            }
        }
        private static void ColocarFormatoNumero(ExcelWorksheet ws, int filaInicio, int columnaInicio, int filaFin, int columnaFin)
        {
            using (var range = ws.Cells[filaInicio, columnaInicio, filaFin, columnaFin])
            {
                range.Style.Numberformat.Format = "#,##0.00";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            }
        }

        private static int Fecha(DateTime fecha)
        {
            DateTime start = new DateTime(1900, 1, 1);
            TimeSpan diff = fecha - start;
            return diff.Days + 2;
        }

        private static void AjustarAncho(ExcelWorksheet ws, int filaInicio, int columnaInicio, int filaFin, int columnaFin)
        {
            using (var range = ws.Cells[filaInicio, columnaInicio, filaFin, columnaFin])
            {
                range.AutoFitColumns();
            }
        }
    }
}