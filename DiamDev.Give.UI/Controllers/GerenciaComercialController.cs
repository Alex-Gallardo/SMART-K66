using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DiamDev.Give.DAL;
using DiamDev.Give.UI.App_Start;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace DiamDev.Give.UI.Controllers
{
    /// <summary>Panel de consulta para cualquier usuario que haya iniciado sesión.</summary>
    [Authorize]
    public sealed class GerenciaComercialController : Controller
    {
        private readonly GerenciaComercialDA _datos = new GerenciaComercialDA();
        private const int MaximoExportacion = 10000;
        private static readonly string[] Tipos = { "ventas", "pedidos", "clientes", "agentes", "pedidosSql", "agentesSql" };

        [HttpGet]
        public ActionResult Index()
        {
            CustomHelper.setTitle("Gerencia Comercial", "Panel de consulta");
            ViewBag.Title = "Gerencia Comercial";
            Response.Cache.SetNoStore();
            return View();
        }

        private static GerenciaComercialDA.Filtro FiltroValido(string empresa, string desde, string hasta, string buscar, string moneda, int pagina, int tamano, string estado = null, string agente = null, string cliente = null)
        {
            empresa = (empresa ?? "").Trim().ToUpperInvariant();
            moneda = (moneda ?? "").Trim().ToUpperInvariant();
            if (empresa != "" && empresa != "BOLIK" && empresa != "FAES" && empresa != "GRACO") throw new ArgumentException("Seleccione una empresa válida.");
            if (moneda != "" && moneda != "GTQ" && moneda != "USD") throw new ArgumentException("Seleccione una moneda válida.");
            DateTime inicio, fin;
            if (string.IsNullOrWhiteSpace(desde)) inicio = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            else if (!DateTime.TryParseExact(desde, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out inicio)) throw new ArgumentException("Fecha inicial inválida.");
            if (string.IsNullOrWhiteSpace(hasta)) fin = DateTime.Today;
            else if (!DateTime.TryParseExact(hasta, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out fin)) throw new ArgumentException("Fecha final inválida.");
            if (inicio > fin || fin > DateTime.Today || (fin - inicio).TotalDays > 1826) throw new ArgumentException("Seleccione un rango válido de hasta cinco años, sin fechas futuras.");
            if (pagina < 1 || pagina > 100000) throw new ArgumentException("Página inválida.");
            if (tamano < 1 || tamano > MaximoExportacion) throw new ArgumentException("Tamaño de página inválido.");
            buscar = (buscar ?? "").Trim();
            if (buscar.Length > 100) throw new ArgumentException("La búsqueda admite hasta 100 caracteres.");
            estado = (estado ?? "").Trim(); agente = (agente ?? "").Trim(); cliente = (cliente ?? "").Trim();
            if (estado != "" && estado != "Y" && estado != "N" && estado != "O" && estado != "C" && estado != "Pendiente" && estado != "Sincronizado") throw new ArgumentException("Estado inválido.");
            if (agente.Length > 30 || cliente.Length > 60) throw new ArgumentException("Filtro avanzado demasiado largo.");
            return new GerenciaComercialDA.Filtro { Empresa = empresa, Desde = inicio, Hasta = fin, Buscar = buscar, Moneda = moneda, Estado = estado, Agente = agente, Cliente = cliente, Pagina = pagina, Tamano = tamano };
        }

        private static List<Dictionary<string, object>> Filas(DataTable tabla)
        {
            var resultado = new List<Dictionary<string, object>>();
            if (tabla == null) return resultado;
            foreach (DataRow fila in tabla.Rows)
            {
                var obj = new Dictionary<string, object>();
                foreach (DataColumn columna in tabla.Columns)
                {
                    string nombre = columna.ColumnName;
                    if (nombre.IndexOf("password", StringComparison.OrdinalIgnoreCase) >= 0 || nombre.IndexOf("passwd", StringComparison.OrdinalIgnoreCase) >= 0 || nombre.Equals("Pass", StringComparison.OrdinalIgnoreCase)) continue;
                    object valor = fila[columna];
                    if (valor == DBNull.Value) valor = null;
                    else if (valor is DateTime) valor = ((DateTime)valor).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    else if (valor is byte[]) valor = "Archivo binario";
                    else if (valor is string && ((string)valor).Length > 4000) valor = ((string)valor).Substring(0, 4000) + "…";
                    obj[nombre] = valor;
                }
                resultado.Add(obj);
            }
            return resultado;
        }

        private static JsonResult JsonGrande(object valor)
        {
            return new JsonResult { Data = valor, JsonRequestBehavior = JsonRequestBehavior.AllowGet, MaxJsonLength = 8 * 1024 * 1024 };
        }

        private ActionResult Error(Exception ex)
        {
            if (ex is ArgumentException) { Response.StatusCode = 400; return JsonGrande(new { error = ex.Message }); }
            Trace.TraceError("[GerenciaComercial] {0}", ex);
            Response.StatusCode = 503;
            return JsonGrande(new { error = "No fue posible consultar las fuentes de datos. Inténtelo de nuevo o contacte a soporte." });
        }

        private bool AgregarSap(Func<DataTable> consulta, List<Dictionary<string, object>> destino, List<string> advertencias, string seccion, string empresa)
        {
            try { destino.AddRange(Filas(consulta())); return true; }
            catch (Exception ex)
            {
                Trace.TraceError("[GerenciaComercial] SAP {0} {1}: {2}", empresa, seccion, ex);
                advertencias.Add(seccion + " SAP " + empresa + " no está disponible.");
                return false;
            }
        }

        [HttpGet]
        public ActionResult Resumen(string empresa, string desde, string hasta, string moneda, bool forzar = false)
        {
            Response.Cache.SetNoStore();
            try
            {
                var f = FiltroValido(empresa, desde, hasta, "", moneda, 1, 20);
                string cacheKey = "GerenciaComercial|" + f.Empresa + "|" + f.Desde.ToString("yyyyMMdd") + "|" + f.Hasta.ToString("yyyyMMdd") + "|" + f.Moneda;
                var cached = HttpRuntime.Cache[cacheKey];
                if (!forzar && cached != null) return JsonGrande(cached);
                var ventas = new List<Dictionary<string, object>>();
                var cobros = new List<Dictionary<string, object>>();
                var catalogos = new List<Dictionary<string, object>>();
                var productos = new List<Dictionary<string, object>>();
                var agentes = new List<Dictionary<string, object>>();
                var clientes = new List<Dictionary<string, object>>();
                var advertencias = new List<string>();
                foreach (var e in (f.Empresa == "" ? new[] { "BOLIK", "FAES", "GRACO" } : new[] { f.Empresa }))
                {
                    var fEmpresa = new GerenciaComercialDA.Filtro { Empresa = e, Desde = f.Desde, Hasta = f.Hasta, Moneda = f.Moneda, Buscar = "", Pagina = 1, Tamano = 20 };
                    AgregarSap(() => _datos.VentasDiarias(fEmpresa), ventas, advertencias, "Ventas", e);
                    AgregarSap(() => _datos.Cobros(fEmpresa), cobros, advertencias, "Cobros", e);
                    AgregarSap(() => _datos.Catalogos(fEmpresa), catalogos, advertencias, "Catálogos", e);
                    AgregarSap(() => _datos.TopProductos(fEmpresa), productos, advertencias, "Productos destacados", e);
                    AgregarSap(() => _datos.TopDimension(fEmpresa, "agentes"), agentes, advertencias, "Agentes destacados", e);
                    AgregarSap(() => _datos.TopDimension(fEmpresa, "clientes"), clientes, advertencias, "Clientes destacados", e);
                }
                var sql = new List<Dictionary<string, object>>();
                try { sql = Filas(_datos.ResumenSql(f)); }
                catch (Exception ex) { Trace.TraceError("[GerenciaComercial] SQL: {0}", ex); advertencias.Add("SQL POS-SmartK66_DEV no está disponible; los pedidos K66 no se incluyen."); }
                if (catalogos.Count == 0 && ventas.Count == 0 && sql.Count == 0) throw new InvalidOperationException("Ninguna fuente disponible.");
                var payload = new { ventas, cobros, catalogos, productos, agentes, clientes, pedidosK66 = sql, advertencias, actualizado = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), desde = f.Desde.ToString("yyyy-MM-dd"), hasta = f.Hasta.ToString("yyyy-MM-dd") };
                if (advertencias.Count == 0) HttpRuntime.Cache.Insert(cacheKey, payload, null, DateTime.Now.AddMinutes(2), System.Web.Caching.Cache.NoSlidingExpiration);
                return JsonGrande(payload);
            }
            catch (Exception ex) { return Error(ex); }
        }

        private GerenciaComercialDA.PaginaDatos Consultar(string tipo, GerenciaComercialDA.Filtro f)
        {
            if (!Tipos.Contains(tipo)) throw new ArgumentException("Seleccione una tabla válida.");
            return tipo.EndsWith("Sql", StringComparison.Ordinal) ? _datos.ListarSql(tipo, f) : _datos.ListarSap(tipo, f);
        }

        [HttpGet]
        public ActionResult Listar(string tipo, string empresa, string desde, string hasta, string buscar, string moneda, string estado, string agente, string cliente, int pagina = 1, int tamano = 25)
        {
            Response.Cache.SetNoStore();
            try
            {
                var f = FiltroValido(empresa, desde, hasta, buscar, moneda, pagina, Math.Min(tamano, 100), estado, agente, cliente);
                var datos = Consultar(tipo, f);
                return JsonGrande(new { filas = Filas(datos.Filas), total = datos.Total, pagina = f.Pagina, tamano = f.Tamano, fuente = tipo.EndsWith("Sql", StringComparison.Ordinal) ? "SQL" : "SAP" });
            }
            catch (Exception ex) { return Error(ex); }
        }

        [HttpGet]
        public ActionResult Detalle(string tipo, string empresa, string codigo, string documento)
        {
            Response.Cache.SetNoStore();
            try
            {
                var f = FiltroValido(empresa, null, null, null, null, 1, 1);
                if (f.Empresa == "" || string.IsNullOrWhiteSpace(codigo) || !Tipos.Contains(tipo)) throw new ArgumentException("Detalle inválido.");
                var tablas = tipo.EndsWith("Sql", StringComparison.Ordinal)
                    ? _datos.DetalleSql(tipo, f.Empresa, codigo)
                    : _datos.DetalleSap(tipo == "ventas" && documento == "nota" ? "notas" : tipo, f.Empresa, codigo);
                if (!tablas.ContainsKey("Principal") || tablas["Principal"].Rows.Count == 0) return HttpNotFound();
                return JsonGrande(new { secciones = tablas.ToDictionary(x => x.Key, x => Filas(x.Value)), fuente = tipo.EndsWith("Sql", StringComparison.Ordinal) ? "SQL" : "SAP" });
            }
            catch (Exception ex) { return Error(ex); }
        }

        private static string CeldaSegura(object valor)
        {
            string texto = Convert.ToString(valor, CultureInfo.InvariantCulture) ?? "";
            string inicio = texto.TrimStart();
            if (inicio.Length > 0 && "=+-@\t\r".IndexOf(inicio[0]) >= 0) texto = "'" + texto;
            return texto;
        }

        [HttpGet]
        public ActionResult Exportar(string tipo, string empresa, string desde, string hasta, string buscar, string moneda, string estado, string agente, string cliente)
        {
            Response.Cache.SetNoStore();
            try
            {
                var f = FiltroValido(empresa, desde, hasta, buscar, moneda, 1, MaximoExportacion, estado, agente, cliente);
                var datos = Consultar(tipo, f);
                if (datos.Total > MaximoExportacion) return new HttpStatusCodeResult(400, "La exportación supera 10,000 filas. Reduzca el rango o aplique filtros.");
                using (var paquete = new ExcelPackage())
                {
                    var hoja = paquete.Workbook.Worksheets.Add("Gerencia Comercial");
                    var columnas = datos.Filas.Columns.Cast<DataColumn>().Where(c => c.ColumnName != "Fila").ToList();
                    for (int c = 0; c < columnas.Count; c++) hoja.Cells[1, c + 1].Value = columnas[c].ColumnName;
                    for (int r = 0; r < datos.Filas.Rows.Count; r++)
                        for (int c = 0; c < columnas.Count; c++)
                        {
                            object valor = datos.Filas.Rows[r][columnas[c]];
                            hoja.Cells[r + 2, c + 1].Value = valor == DBNull.Value ? "" : valor is string ? CeldaSegura(valor) : valor;
                        }
                    using (var encabezado = hoja.Cells[1, 1, 1, columnas.Count])
                    {
                        encabezado.Style.Font.Bold = true;
                        encabezado.Style.Font.Color.SetColor(System.Drawing.Color.White);
                        encabezado.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        encabezado.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(20, 46, 76));
                    }
                    hoja.View.FreezePanes(2, 1);
                    hoja.Cells[1, 1, Math.Max(2, datos.Filas.Rows.Count + 1), columnas.Count].AutoFilter = true;
                    hoja.Cells[hoja.Dimension.Address].AutoFitColumns();
                    return File(paquete.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "gerencia-comercial-" + tipo + "-" + DateTime.Today.ToString("yyyyMMdd") + ".xlsx");
                }
            }
            catch (Exception ex) { return Error(ex); }
        }
    }
}
