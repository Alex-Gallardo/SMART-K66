using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Mvc;
using DiamDev.Give.DAL;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace DiamDev.Give.UI.Controllers
{
    public sealed partial class GerenciaComercialController
    {
        [HttpGet]
        public ActionResult Operaciones() { Response.Cache.SetNoStore(); return View(); }

        private static GerenciaComercialDA.Filtro AnalyticsFiltro(string empresa, string desde, string hasta, string moneda, int pagina = 1, int tamano = 25, string buscar = "")
        {
            var f = FiltroValido(empresa == "all" ? "" : empresa, desde, hasta, buscar, moneda, pagina, tamano);
            if (f.Moneda != "GTQ" && f.Moneda != "USD") throw new ArgumentException("Seleccione GTQ o USD.");
            ValidarComparacion(f);
            return f;
        }
        private static void AnalyticsTexto(params string[] valores)
        {
            if (valores.Any(v => v != null && v.Length > 100)) throw new ArgumentException("Filtro demasiado largo.");
        }
        [HttpGet]
        public ActionResult AnalyticsPanel(string empresa, string desde, string hasta, string moneda, bool origen = false)
        {
            Response.Cache.SetNoStore();
            try {
                var f = AnalyticsFiltro(empresa, desde, hasta, moneda, 1, MaximoExportacion);
                var agentes = _datos.Analytics(f,"agentes",origen:origen);
                if (agentes.Rows.Count > 0 && Convert.ToInt32(agentes.Rows[0]["total"]) > MaximoExportacion) throw new ArgumentException("Más de 10,000 agentes con movimientos. Seleccione una empresa.");
                return JsonGrande(new { general=Filas(_datos.Analytics(f,"general",origen:origen)), tendencia=Filas(_datos.Analytics(f,"tendencia",origen:origen)), agentes=Filas(agentes), productos=Filas(_datos.Analytics(new GerenciaComercialDA.Filtro { Empresa=f.Empresa,Desde=f.Desde,Hasta=f.Hasta,Moneda=f.Moneda,Pagina=1,Tamano=5 },"productos",origen:origen)), conteos=Filas(_datos.Analytics(f,"conteos",origen:origen)), actualizado=DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") });
            } catch(Exception ex) { return Error(ex); }
        }
        [HttpGet]
        public ActionResult AnalyticsDetalle(string vista, string empresa, string desde, string hasta, string moneda, string agente="", string cliente="", string codigo="", string producto="", string buscar="", string orden="current", bool descendente=true, int pagina=1, int tamano=25, bool origen=false)
        {
            Response.Cache.SetNoStore();
            try {
                AnalyticsTexto(agente,cliente,codigo,producto,orden);
                if (!new[] {"general","agentes","clientes","productos","producto-agente"}.Contains(vista)) throw new ArgumentException("Vista inválida.");
                var f=AnalyticsFiltro(empresa,desde,hasta,moneda,pagina,Math.Min(100,tamano),buscar);
                var tabla=_datos.Analytics(f,vista,agente,cliente,codigo,producto,orden,descendente,origen);
                int total=tabla.Rows.Count==0?0:Convert.ToInt32(tabla.Rows[0]["total"]);
                return JsonGrande(new {rows=Filas(tabla),total,pagina});
            } catch(Exception ex) { return Error(ex); }
        }
        [HttpGet]
        public ActionResult AnalyticsPerfil(string empresa, string desde, string hasta, string moneda, string agente, bool origen=false)
        {
            Response.Cache.SetNoStore();
            try {
                AnalyticsTexto(agente);
                if (string.IsNullOrWhiteSpace(agente)) throw new ArgumentException("Seleccione un agente.");
                var f=AnalyticsFiltro(empresa,desde,hasta,moneda,1,10);
                return JsonGrande(new { ventas=Filas(_datos.Analytics(f,"agentes",agente,origen:origen)), tendencia=Filas(_datos.Analytics(f,"tendencia",agente,origen:origen)), clientes=Filas(_datos.Analytics(f,"clientes-ranking",agente,origen:origen)), productos=Filas(_datos.Analytics(f,"producto-agente",agente,origen:origen)), conteos=Filas(_datos.Analytics(f,"conteos",agente,origen:origen)) });
            } catch(Exception ex) { return Error(ex); }
        }
        [HttpGet]
        public ActionResult AnalyticsExcel(string vista, string empresa, string desde, string hasta, string moneda, string agente="", string cliente="", string codigo="", string producto="", string buscar="", string orden="current", bool descendente=true, bool origen=false)
        {
            Response.Cache.SetNoStore();
            try {
                AnalyticsTexto(agente,cliente,codigo,producto,orden);
                var vistas = vista=="dashboard" ? new[] {"general","agentes","clientes","productos","producto-agente"} : vista=="perfil-agente" ? new[] {"agentes","clientes","producto-agente"} : new[] {vista};
                if (vistas.Any(v=>!new[] {"general","agentes","clientes","productos","producto-agente"}.Contains(v))) throw new ArgumentException("Vista inválida.");
                var f=AnalyticsFiltro(empresa,desde,hasta,moneda,1,MaximoExportacion,buscar);
                using(var paquete=new ExcelPackage()) {
                    foreach(string dimension in vistas) {
                        var tabla=_datos.Analytics(f,dimension,agente,cliente,codigo,producto,orden,descendente,origen);
                        if(tabla.Rows.Count>0 && Convert.ToInt32(tabla.Rows[0]["total"])>MaximoExportacion) throw new ArgumentException("La exportación supera 10,000 filas por vista. Reduzca el rango o filtre.");
                        var hoja=paquete.Workbook.Worksheets.Add(dimension);
                        var columnas=tabla.Columns.Cast<DataColumn>().Where(c=>!new[] {"rowNumber","total"}.Contains(c.ColumnName)).ToList();
                        hoja.Cells[1,1].Value="K66 Analytics · "+dimension+" · "+moneda;
                        hoja.Cells[2,1].Value=(empresa??"Todas")+" · "+f.Desde.ToString("yyyy-MM-dd")+" al "+f.Hasta.ToString("yyyy-MM-dd");
                        hoja.Cells[3,1].Value="Neto de líneas; moneda documental. Cantidades separadas por unidad. Cobertura: "+(origen?"Exclusiones Analytics":"Completa");
                        var etiquetas = new Dictionary<string,string> { {"company","Empresa"},{"agent","Agente"},{"agentCode","Código agente SAP"},{"agentKey","Empresa y agente"},{"code","Código"},{"name","Nombre"},{"unit","Unidad"},{"current","Venta actual"},{"previous","Venta anterior"},{"qtyCurrent","Cantidad actual"},{"qtyPrevious","Cantidad anterior"},{"diff","Diferencia"},{"growth","Variación %"},{"participation","Participación empresa %"},{"rank","Ranking"},{"indicator","Indicador"},{"type","Tipo"} };
                        for(int c=0;c<columnas.Count;c++) { string key=columnas[c].ColumnName; hoja.Cells[5,c+1].Value=etiquetas.ContainsKey(key)?etiquetas[key]:key; }
                        for(int r=0;r<tabla.Rows.Count;r++) for(int c=0;c<columnas.Count;c++) {
                            var valor=tabla.Rows[r][columnas[c]];
                            var celda=hoja.Cells[r+6,c+1];
                            celda.Value=valor==DBNull.Value ? null : valor is string ? CeldaSegura(valor) : valor;
                            if(valor!=DBNull.Value && new[] {"growth","participation"}.Contains(columnas[c].ColumnName)) { celda.Value=Convert.ToDecimal(valor)/100m; celda.Style.Numberformat.Format="0.00%"; }
                            else if(valor is decimal || valor is double) celda.Style.Numberformat.Format="#,##0.00;[Red](#,##0.00)";
                        }
                        hoja.Cells[5,1,5,Math.Max(1,columnas.Count)].Style.Font.Bold=true;
                        hoja.Cells[5,1,5,Math.Max(1,columnas.Count)].Style.Font.Color.SetColor(System.Drawing.Color.White);
                        hoja.Cells[5,1,5,Math.Max(1,columnas.Count)].Style.Fill.PatternType=ExcelFillStyle.Solid;
                        hoja.Cells[5,1,5,Math.Max(1,columnas.Count)].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(21,55,83));
                        hoja.View.FreezePanes(6,1);
                        if(columnas.Count>0) hoja.Cells[5,1,Math.Max(6,tabla.Rows.Count+5),columnas.Count].AutoFilter=true;
                        hoja.Cells[hoja.Dimension.Address].AutoFitColumns(10,45);
                    }
                    return File(paquete.GetAsByteArray(),"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet","K66-Analytics-"+DateTime.Today.ToString("yyyyMMdd")+".xlsx");
                }
            } catch(Exception ex) { return Error(ex); }
        }
    }
}
