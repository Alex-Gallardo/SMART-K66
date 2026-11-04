using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using DiamDev.Give.DAL;
using DiamDev.Give.UI.App_Start;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace DiamDev.Give.UI.Controllers
{
    /// <summary>Panel de consulta protegido por el permiso de Gerencia Comercial.</summary>
    [Authorize]
    [Permiso("Control.GerenciaComercial.Ver")]
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

        private static string DimensionValida(string dimension)
        {
            if (dimension != "agentes" && dimension != "clientes" && dimension != "productos") throw new ArgumentException("Seleccione agentes, clientes o productos.");
            return dimension;
        }

        private static void ValidarComparacion(GerenciaComercialDA.Filtro f)
        {
            if ((f.Hasta - f.Desde).TotalDays > 366) throw new ArgumentException("El comparativo admite períodos de hasta un año. Para más historia, abra la evolución individual.");
        }

        private static string SinAcentos(string valor)
        {
            var normal = (valor ?? "").Trim().ToUpperInvariant().Normalize(NormalizationForm.FormD);
            var b = new StringBuilder();
            foreach (char c in normal) if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) b.Append(c);
            return b.ToString().Normalize(NormalizationForm.FormC);
        }

        private static string Frente(string nombre)
        {
            string n = SinAcentos(nombre);
            if (n == "SALA DE VENTAS" || n == "SALA VENTAS") return "Sala de Ventas";
            if (n == "GERENCIA") return "Gerencia";
            if (n == "EXPORTACIONES" || n == "EXPORTACION" || n == "DEPTO EXPORTACIONES" || n == "DEPARTAMENTO EXPORTACIONES") return "Exportaciones";
            return "Agente comercial";
        }

        private static void Clasificar(DataTable filas, DataTable operadores)
        {
            if (!filas.Columns.Contains("Frente")) filas.Columns.Add("Frente", typeof(string));
            foreach (DataRow fila in filas.Rows)
            {
                string empresa = Convert.ToString(fila["Empresa"]), codigo = Convert.ToString(fila["Codigo"]);
                string frente = Frente(Convert.ToString(fila["Nombre"]));
                if (frente == "Agente comercial" && operadores != null)
                {
                    long id = empresa == "BOLIK" ? 20210705001L : empresa == "FAES" ? 20210705003L : empresa == "GRACO" ? 20210705004L : 0;
                    foreach (DataRow operador in operadores.Rows)
                    {
                        if (Convert.ToInt64(operador["EmpresaId"]) != id) continue;
                        string valor = Convert.ToString(operador["Codigo"]);
                        int guion = valor.IndexOf('-');
                        if (guion <= 0 || valor.Substring(0, guion).Trim() != codigo) continue;
                        frente = Frente(valor.Substring(guion + 1));
                        if (frente != "Agente comercial") break;
                    }
                }
                fila["Frente"] = frente;
            }
        }

        [HttpGet]
        public ActionResult Comparativo(string dimension, string empresa, string desde, string hasta, string moneda, string buscar, string orden = "actual", int pagina = 1, int tamano = 25)
        {
            Response.Cache.SetNoStore();
            try
            {
                DimensionValida(dimension);
                var f = FiltroValido(empresa, desde, hasta, buscar, moneda, pagina, Math.Min(tamano, 1000));
                ValidarComparacion(f);
                var datos = _datos.Comparar(dimension, f, orden);
                string advertencia = null;
                if (dimension == "agentes")
                {
                    DataTable operadores = null;
                    try { operadores = _datos.OperadoresComerciales(); }
                    catch (Exception ex) { Trace.TraceError("[GerenciaComercial] Usuario_Empresa: {0}", ex); advertencia = "No se pudo leer Usuario_Empresa; los frentes se identifican por el nombre SAP."; }
                    Clasificar(datos.Filas, operadores);
                }
                return JsonGrande(new { filas = Filas(datos.Filas), total = datos.Total, pagina = f.Pagina, tamano = f.Tamano, anteriorDesde = f.Desde.AddYears(-1).ToString("yyyy-MM-dd"), anteriorHasta = f.Hasta.AddYears(-1).ToString("yyyy-MM-dd"), advertencia });
            }
            catch (Exception ex) { return Error(ex); }
        }

        private static List<GerenciaComercialDA.PeriodoAvance> PeriodosAvance(string rangos)
        {
            if (string.IsNullOrWhiteSpace(rangos) || rangos.Length > 300) throw new ArgumentException("Seleccione entre 2 y 12 períodos.");
            string[] partes = rangos.Split('|');
            if (partes.Length < 2 || partes.Length > 12) throw new ArgumentException("Seleccione entre 2 y 12 períodos.");
            var resultado = new List<GerenciaComercialDA.PeriodoAvance>();
            var vistos = new HashSet<string>();
            for (int i = 0; i < partes.Length; i++)
            {
                string[] fechas = partes[i].Split(',');
                DateTime desde, hasta;
                if (fechas.Length != 2 ||
                    !DateTime.TryParseExact(fechas[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out desde) ||
                    !DateTime.TryParseExact(fechas[1], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out hasta) ||
                    desde > hasta || hasta > DateTime.Today || desde < new DateTime(DateTime.Today.Year - 10, 1, 1) || (hasta - desde).TotalDays > 365)
                    throw new ArgumentException("Cada período debe tener fechas válidas, sin futuro, y durar como máximo un año.");
                if (!vistos.Add(partes[i])) throw new ArgumentException("Hay períodos duplicados.");
                resultado.Add(new GerenciaComercialDA.PeriodoAvance { Indice = i, Desde = desde, Hasta = hasta });
            }
            return resultado;
        }

        [HttpGet]
        public ActionResult Avances(string empresa, string moneda, string rangos)
        {
            Response.Cache.SetNoStore();
            try
            {
                empresa = (empresa ?? "").Trim().ToUpperInvariant();
                moneda = (moneda ?? "").Trim().ToUpperInvariant();
                if (empresa != "BOLIK" && empresa != "FAES" && empresa != "GRACO") throw new ArgumentException("Seleccione una empresa.");
                if (moneda != "GTQ" && moneda != "USD") throw new ArgumentException("Seleccione GTQ o USD.");
                var periodos = PeriodosAvance(rangos);
                var datos = _datos.AvancesAgentes(empresa, moneda, periodos);
                string advertencia = null;
                try { Clasificar(datos.Ventas, _datos.OperadoresComerciales()); }
                catch (Exception ex)
                {
                    Trace.TraceError("[GerenciaComercial] Usuario_Empresa en avances: {0}", ex);
                    Clasificar(datos.Ventas, null);
                    advertencia = "Usuario_Empresa no está disponible; los frentes se identifican por nombre SAP.";
                }
                return JsonGrande(new
                {
                    empresa,
                    moneda,
                    periodos = periodos.Select(p => new { p.Indice, desde = p.Desde.ToString("yyyy-MM-dd"), hasta = p.Hasta.ToString("yyyy-MM-dd") }),
                    ventas = Filas(datos.Ventas),
                    unidades = Filas(datos.Unidades),
                    advertencia,
                    actualizado = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }
            catch (Exception ex) { return Error(ex); }
        }

        [HttpGet]
        public ActionResult ExportarAvances(string empresa, string moneda, string rangos)
        {
            Response.Cache.SetNoStore();
            try
            {
                empresa = (empresa ?? "").Trim().ToUpperInvariant();
                moneda = (moneda ?? "").Trim().ToUpperInvariant();
                if (empresa != "BOLIK" && empresa != "FAES" && empresa != "GRACO") throw new ArgumentException("Seleccione una empresa.");
                if (moneda != "GTQ" && moneda != "USD") throw new ArgumentException("Seleccione GTQ o USD.");
                var periodos = PeriodosAvance(rangos);
                var datos = _datos.AvancesAgentes(empresa, moneda, periodos);
                if (datos.Ventas.Rows.Count > MaximoExportacion) return new HttpStatusCodeResult(400, "La exportación supera 10,000 filas de origen.");
                try { Clasificar(datos.Ventas, _datos.OperadoresComerciales()); }
                catch (Exception ex) { Trace.TraceError("[GerenciaComercial] Usuario_Empresa en Excel: {0}", ex); Clasificar(datos.Ventas, null); }
                using (var paquete = new ExcelPackage())
                {
                    var hoja = paquete.Workbook.Worksheets.Add("Avances por agente");
                    var titulos = new List<string> { "Código SAP", "Agente", "Frente" };
                    titulos.AddRange(periodos.Select(p => p.Desde.ToString("yyyy-MM-dd") + " al " + p.Hasta.ToString("yyyy-MM-dd") + " (" + moneda + ")"));
                    titulos.AddRange(new[] { "Cambio absoluto", "Variación %", "Participación último %" });
                    for (int c = 0; c < titulos.Count; c++) hoja.Cells[1, c + 1].Value = titulos[c];
                    var totales = new decimal[periodos.Count];
                    var grupos = datos.Ventas.AsEnumerable().GroupBy(r => Convert.ToString(r["Codigo"])).OrderBy(g => Convert.ToString(g.First()["Nombre"]));
                    int fila = 2;
                    foreach (var grupo in grupos)
                    {
                        var primero = grupo.First();
                        hoja.Cells[fila, 1].Value = CeldaSegura(grupo.Key);
                        hoja.Cells[fila, 2].Value = CeldaSegura(primero["Nombre"]);
                        hoja.Cells[fila, 3].Value = CeldaSegura(primero["Frente"]);
                        var valores = new decimal[periodos.Count];
                        foreach (var registro in grupo)
                        {
                            int indice = Convert.ToInt32(registro["Periodo"]);
                            valores[indice] = registro["Neto"] == DBNull.Value ? 0 : Convert.ToDecimal(registro["Neto"]);
                            totales[indice] += valores[indice];
                        }
                        for (int i = 0; i < valores.Length; i++) { hoja.Cells[fila, i + 4].Value = valores[i]; hoja.Cells[fila, i + 4].Style.Numberformat.Format = "#,##0.00;[Red](#,##0.00)"; }
                        int ultimo = valores.Length - 1;
                        hoja.Cells[fila, ultimo + 5].Value = valores[ultimo] - valores[ultimo - 1];
                        if (valores[ultimo - 1] != 0) { hoja.Cells[fila, ultimo + 6].Value = (double)((valores[ultimo] - valores[ultimo - 1]) / Math.Abs(valores[ultimo - 1])); hoja.Cells[fila, ultimo + 6].Style.Numberformat.Format = "0.0%"; }
                        else hoja.Cells[fila, ultimo + 6].Value = "Sin base";
                        fila++;
                    }
                    hoja.Cells[fila, 2].Value = "TOTAL " + empresa;
                    for (int i = 0; i < totales.Length; i++) { hoja.Cells[fila, i + 4].Value = totales[i]; hoja.Cells[fila, i + 4].Style.Numberformat.Format = "#,##0.00;[Red](#,##0.00)"; }
                    hoja.Cells[fila, totales.Length + 4].Value = totales[totales.Length - 1] - totales[totales.Length - 2];
                    if (totales[totales.Length - 2] != 0)
                    {
                        hoja.Cells[fila, totales.Length + 5].Value = (double)((totales[totales.Length - 1] - totales[totales.Length - 2]) / Math.Abs(totales[totales.Length - 2]));
                        hoja.Cells[fila, totales.Length + 5].Style.Numberformat.Format = "0.0%";
                    }
                    else hoja.Cells[fila, totales.Length + 5].Value = "Sin base";
                    if (totales[totales.Length - 1] != 0) { hoja.Cells[fila, totales.Length + 6].Value = 1; hoja.Cells[fila, totales.Length + 6].Style.Numberformat.Format = "0.0%"; }
                    for (int r = 2; r < fila; r++) if (totales[totales.Length - 1] != 0) { hoja.Cells[r, totales.Length + 6].Value = (double)(Convert.ToDecimal(hoja.Cells[r, totales.Length + 3].Value) / totales[totales.Length - 1]); hoja.Cells[r, totales.Length + 6].Style.Numberformat.Format = "0.0%"; }
                    using (var encabezado = hoja.Cells[1, 1, 1, titulos.Count])
                    {
                        encabezado.Style.Font.Bold = true;
                        encabezado.Style.Font.Color.SetColor(System.Drawing.Color.White);
                        encabezado.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        encabezado.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(20, 46, 76));
                    }
                    hoja.Cells[fila, 1, fila, titulos.Count].Style.Font.Bold = true;
                    hoja.View.FreezePanes(2, 4);
                    hoja.Cells[1, 1, Math.Max(2, fila - 1), titulos.Count].AutoFilter = true;
                    hoja.Cells[hoja.Dimension.Address].AutoFitColumns();

                    var unidad = paquete.Workbook.Worksheets.Add("Unidades por medida");
                    unidad.Cells[1, 1].Value = "Unidad";
                    for (int i = 0; i < periodos.Count; i++) unidad.Cells[1, i + 2].Value = periodos[i].Desde.ToString("yyyy-MM-dd") + " al " + periodos[i].Hasta.ToString("yyyy-MM-dd");
                    int uFila = 2;
                    foreach (var grupo in datos.Unidades.AsEnumerable().GroupBy(r => Convert.ToString(r["Unidad"])).OrderBy(g => g.Key))
                    {
                        unidad.Cells[uFila, 1].Value = CeldaSegura(grupo.Key);
                        foreach (var registro in grupo)
                        {
                            int indice = Convert.ToInt32(registro["Periodo"]);
                            decimal cantidad = registro["Cantidad"] == DBNull.Value ? 0 : Convert.ToDecimal(registro["Cantidad"]);
                            unidad.Cells[uFila, indice + 2].Value = Convert.ToDecimal(unidad.Cells[uFila, indice + 2].Value ?? 0) + cantidad;
                        }
                        uFila++;
                    }
                    unidad.Cells[1, 1, 1, periodos.Count + 1].Style.Font.Bold = true;
                    unidad.View.FreezePanes(2, 2);
                    unidad.Cells[unidad.Dimension.Address].AutoFitColumns();
                    return File(paquete.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "avances-" + empresa.ToLowerInvariant() + "-" + DateTime.Today.ToString("yyyyMMdd") + ".xlsx");
                }
            }
            catch (Exception ex) { return Error(ex); }
        }

        [HttpGet]
        public ActionResult Historico(string dimension, string empresa, string codigo, string hasta, string moneda)
        {
            Response.Cache.SetNoStore();
            try
            {
                DimensionValida(dimension);
                var f = FiltroValido(empresa, null, hasta, "", moneda, 1, 1);
                return JsonGrande(new { filas = Filas(_datos.Historico(dimension, f, codigo)), empresa = f.Empresa, codigo, dimension });
            }
            catch (Exception ex) { return Error(ex); }
        }

        [HttpGet]
        public ActionResult ExportarComparativo(string dimension, string empresa, string desde, string hasta, string moneda, string buscar, string orden = "actual")
        {
            Response.Cache.SetNoStore();
            try
            {
                DimensionValida(dimension);
                var f = FiltroValido(empresa, desde, hasta, buscar, moneda, 1, MaximoExportacion);
                ValidarComparacion(f);
                var datos = _datos.Comparar(dimension, f, orden);
                if (datos.Total > MaximoExportacion) return new HttpStatusCodeResult(400, "La exportación supera 10,000 filas. Aplique filtros.");
                if (dimension == "agentes")
                {
                    try { Clasificar(datos.Filas, _datos.OperadoresComerciales()); }
                    catch (Exception ex) { Trace.TraceError("[GerenciaComercial] Usuario_Empresa: {0}", ex); Clasificar(datos.Filas, null); }
                }
                using (var paquete = new ExcelPackage())
                {
                    var hoja = paquete.Workbook.Worksheets.Add("Comparativo " + dimension);
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
                    return File(paquete.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "comparativo-" + dimension + "-" + DateTime.Today.ToString("yyyyMMdd") + ".xlsx");
                }
            }
            catch (Exception ex) { return Error(ex); }
        }
    }
}
