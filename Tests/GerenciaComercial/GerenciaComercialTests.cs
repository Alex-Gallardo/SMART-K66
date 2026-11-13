using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Web;
using DiamDev.Give.DAL;
using DiamDev.Give.BLL;
using DiamDev.Give.UI.App_Start;
using DiamDev.Give.UI.Controllers;

public static class GerenciaComercialTests
{
    private sealed class ContextoPrueba : HttpContextBase
    {
        private readonly IPrincipal _usuario;
        public ContextoPrueba(string login) { _usuario = new GenericPrincipal(new GenericIdentity(login), new string[0]); }
        public override IPrincipal User { get { return _usuario; } set { } }
    }
    private static int _passed;
    private static object Invoke(string name, params object[] args)
    {
        var method = typeof(GerenciaComercialController).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static);
        return method.Invoke(null, args);
    }
    private static void Check(bool valid, string message)
    {
        if (!valid) throw new Exception(message);
        _passed++;
    }
    private static void Reject(params object[] args)
    {
        try { Invoke("FiltroValido", args); }
        catch (TargetInvocationException ex) { Check(ex.InnerException is ArgumentException, "El filtro debe rechazarse como entrada inválida."); return; }
        throw new Exception("El filtro inválido fue aceptado.");
    }
    private static object[] Filter(string empresa, string desde, string hasta, string buscar, string moneda, string estado)
    {
        return new object[] { empresa, desde, hasta, buscar, moneda, 1, 25, estado, "", "" };
    }
    public static int Main()
    {
        var today = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var yesterday = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var valid = (GerenciaComercialDA.Filtro)Invoke("FiltroValido", Filter(" faes ", yesterday, today, " prueba ", "gtq", "Y"));
        Check(valid.Empresa == "FAES" && valid.Moneda == "GTQ" && valid.Buscar == "prueba" && valid.Estado == "Y", "Normalización de filtros.");
        Reject(Filter("OTRA", yesterday, today, "", "", ""));
        Reject(Filter("FAES", today, yesterday, "", "", ""));
        Reject(Filter("FAES", yesterday, DateTime.Today.AddDays(1).ToString("yyyy-MM-dd"), "", "", ""));
        Reject(Filter("FAES", yesterday, today, "", "EUR", ""));
        Reject(Filter("FAES", yesterday, today, "", "", "INVALIDO"));
        Reject(Filter("FAES", yesterday, today, new string('x', 101), "", ""));

        var table = new DataTable();
        table.Columns.Add("Nombre", typeof(string)); table.Columns.Add("Pass", typeof(string));
        table.Columns.Add("Fecha", typeof(DateTime)); table.Columns.Add("Adjunto", typeof(byte[]));
        table.Rows.Add("Cliente", "secreto", new DateTime(2026, 10, 28), new byte[] { 1, 2 });
        var rows = (List<Dictionary<string, object>>)Invoke("Filas", table);
        Check(rows.Count == 1 && !rows[0].ContainsKey("Pass"), "El panel no debe enviar contraseñas.");
        Check((string)rows[0]["Fecha"] == "2026-10-28 00:00:00", "Formato de fechas estable.");
        Check((string)rows[0]["Adjunto"] == "Archivo binario", "No enviar binarios al navegador.");
        Check((string)Invoke("CeldaSegura", "  =2+2") == "'  =2+2", "Neutralizar fórmulas en Excel.");
        Check((string)Invoke("CeldaSegura", "Cliente") == "Cliente", "Conservar texto normal en Excel.");
        var permiso = (PermisoAttribute)Attribute.GetCustomAttribute(typeof(GerenciaComercialController), typeof(PermisoAttribute));
        Check(permiso != null && permiso.Permiso == "Control.GerenciaComercial.Ver", "Vista y endpoints deben exigir el mismo permiso comercial.");
        var autorizar = typeof(PermisoAttribute).GetMethod("AuthorizeCore", BindingFlags.NonPublic | BindingFlags.Instance);
        RolBL.Permitido = false;
        Check(!(bool)autorizar.Invoke(permiso, new object[] { new ContextoPrueba("vendedor") }), "Un usuario sin permiso no debe acceder al panel.");
        RolBL.Permitido = true;
        Check((bool)autorizar.Invoke(permiso, new object[] { new ContextoPrueba("gerencia") }) && RolBL.UltimoPermiso == "Control.GerenciaComercial.Ver", "El acceso debe consultar el permiso comercial exacto.");

        var sqlVentas = (string)typeof(GerenciaComercialDA).GetMethod("ParteSap", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { "ventas", "FAES" });
        Check(sqlVentas.Contains("\"SBOESCOCESA\".\"OINV\"") && sqlVentas.Contains("\"SBOESCOCESA\".\"ORIN\""), "FAES debe consultar facturas y notas de crédito en su schema configurado.");
        Check(sqlVentas.Contains("'QTZ','Q','GTQ'") && sqlVentas.Contains("'GTQ' ELSE"), "La moneda local SAP debe normalizarse a GTQ.");
        Check(sqlVentas.Contains("-1 * CASE"), "Las notas de crédito deben descontarse de las ventas.");
        Check(sqlVentas.Contains("P.\"Agente\"") && sqlVentas.Contains("P.\"Cliente\"") && sqlVentas.Contains("P.\"Estado\""), "Los filtros avanzados deben aplicarse en SAP.");
        var types = (string[])typeof(GerenciaComercialController).GetField("Tipos", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        Check(Array.IndexOf(types, "productos") < 0 && Array.IndexOf(types, "clientesSql") < 0 && Array.IndexOf(types, "productosSql") < 0, "Las tres secciones en desarrollo no deben admitir listados, detalles ni exportaciones.");
        Check(Array.IndexOf(types, "ventas") >= 0 && Array.IndexOf(types, "clientes") >= 0, "Las secciones activas deben permanecer disponibles.");

        HanaHelper.Consultas.Clear();
        new GerenciaComercialDA().DetalleSap("ventas", "FAES", "123");
        Check(HanaHelper.Consultas.Count == 2, "El detalle de ventas debe consultar encabezado y productos.");
        Check(HanaHelper.Consultas[0].Contains("\"SBOESCOCESA\".\"OINV\"") && HanaHelper.Consultas[1].Contains("\"SBOESCOCESA\".\"INV1\""), "La factura debe usar el esquema y tablas SAP correctos.");
        Check(!HanaHelper.Consultas[0].Contains("SELECT *") && !HanaHelper.Consultas[1].Contains("SELECT *"), "El detalle de ventas no debe devolver todas las columnas técnicas SAP.");
        Check(HanaHelper.Consultas[0].Contains("\"Subtotal\"") && HanaHelper.Consultas[0].Contains("\"Cliente\"") && HanaHelper.Consultas[1].Contains("\"PrecioUnitario\""), "El detalle debe conservar importes y datos comerciales útiles.");
        HanaHelper.Consultas.Clear();
        new GerenciaComercialDA().DetalleSap("notas", "BOLIK", "456");
        Check(HanaHelper.Consultas[0].Contains("\"ORIN\"") && HanaHelper.Consultas[1].Contains("\"RIN1\""), "Las notas de crédito deben consultar sus propias tablas.");
        HanaHelper.Consultas.Clear();
        new GerenciaComercialDA().Comparar("agentes", valid, "caida");
        Check(HanaHelper.Consultas.Count == 1 && HanaHelper.Consultas[0].Contains("\"OINV\"") && HanaHelper.Consultas[0].Contains("\"ORIN\""), "El comparativo debe contar y paginar facturas menos notas de crédito en una consulta.");
        Check(HanaHelper.Consultas[0].Contains("\"AnteriorDesde\"") && HanaHelper.Consultas[0].Contains("\"AnteriorHasta\""), "Debe comparar contra las mismas fechas del año anterior.");
        Check(HanaHelper.Consultas[0].Contains("PARTITION BY G.\"Empresa\", G.\"Moneda\"") && HanaHelper.Consultas[0].Contains("\"Participacion\""), "La participación debe usar el total de cada empresa y moneda antes de buscar.");
        Check(HanaHelper.Consultas[0].Contains("R.\"Delta\" ASC") && HanaHelper.Consultas[0].Contains("ROW_NUMBER()") && HanaHelper.Consultas[0].Contains("COUNT(*) OVER"), "La mayor caída debe ordenarse y paginarse en SAP.");
        HanaHelper.Consultas.Clear();
        new GerenciaComercialDA().Comparar("productos", valid, "cantidad");
        Check(HanaHelper.Consultas[0].Contains("\"INV1\"") && HanaHelper.Consultas[0].Contains("\"RIN1\"") && HanaHelper.Consultas[0].Contains("\"TotalFrgn\""), "Productos deben usar líneas SAP en moneda de documento.");
        Check(HanaHelper.Consultas[0].Contains("D.\"TotalEmpresa\" FROM G LEFT JOIN D") && HanaHelper.Consultas[0].Contains("SUM(\"Monto\"*\"Signo\")"), "La participación de productos debe dividirse entre la venta neta completa de su empresa, no entre líneas.");
        HanaHelper.Consultas.Clear();
        new GerenciaComercialDA().Historico("clientes", valid, "C001");
        Check(HanaHelper.Consultas[0].Contains("'YYYY-MM'") && HanaHelper.Consultas[0].Contains("P.\"Codigo\""), "El histórico individual debe agruparse por mes y filtrar el código.");
        Check((string)Invoke("Frente", "SALA DE VENTAS") == "Sala de Ventas" && (string)Invoke("Frente", "EXPORTACIÓN") == "Exportaciones", "Clasificación exacta de frentes comerciales.");
        var agentes = new DataTable(); agentes.Columns.Add("Empresa", typeof(string)); agentes.Columns.Add("Codigo", typeof(string)); agentes.Columns.Add("Nombre", typeof(string)); agentes.Rows.Add("FAES", "2", "Otro nombre SAP");
        var operadores = new DataTable(); operadores.Columns.Add("EmpresaId", typeof(long)); operadores.Columns.Add("Codigo", typeof(string)); operadores.Columns.Add("Depto", typeof(string)); operadores.Rows.Add(20210705003L, "2-GERENCIA", "X");
        Invoke("Clasificar", agentes, operadores);
        Check((string)agentes.Rows[0]["Frente"] == "Gerencia", "Usuario_Empresa debe relacionarse por empresa y código SAP.");
        var comparison = typeof(GerenciaComercialController).GetMethod("Comparativo").GetCustomAttributes(typeof(System.Web.Mvc.HttpGetAttribute), true);
        Check(comparison.Length == 1 && typeof(GerenciaComercialController).GetMethod("Historico").GetCustomAttributes(typeof(System.Web.Mvc.HttpGetAttribute), true).Length == 1, "Los nuevos endpoints deben estar bajo el permiso del controlador.");
        string desdeAvance = DateTime.Today.AddYears(-2).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string hastaAvance = DateTime.Today.AddYears(-2).AddDays(10).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string desdeSegundo = DateTime.Today.AddYears(-1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string hastaSegundo = DateTime.Today.AddYears(-1).AddDays(10).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string rangos = desdeAvance + "," + hastaAvance + "|" + desdeSegundo + "," + hastaSegundo;
        var periodos = (List<GerenciaComercialDA.PeriodoAvance>)Invoke("PeriodosAvance", rangos);
        Check(periodos.Count == 2 && periodos[0].Indice == 0 && periodos[1].Indice == 1, "Los períodos deben conservar el orden elegido.");
        int leap = DateTime.Today.Year - 1; while (!DateTime.IsLeapYear(leap)) leap--;
        var anioBisiesto = (List<GerenciaComercialDA.PeriodoAvance>)Invoke("PeriodosAvance", leap + "-01-01," + leap + "-12-31|" + rangos.Split('|')[1]);
        Check(anioBisiesto[0].Hasta.Month == 12 && anioBisiesto[0].Hasta.Day == 31, "Aceptar años bisiestos completos.");
        try { Invoke("PeriodosAvance", rangos + "|" + desdeAvance + "," + hastaAvance); throw new Exception("Se aceptó un período duplicado."); }
        catch (TargetInvocationException ex) { Check(ex.InnerException is ArgumentException, "Rechazar períodos duplicados."); }
        try { Invoke("PeriodosAvance", desdeAvance + "," + DateTime.Today.AddDays(1).ToString("yyyy-MM-dd") + "|" + desdeSegundo + "," + hastaSegundo); throw new Exception("Se aceptó una fecha futura."); }
        catch (TargetInvocationException ex) { Check(ex.InnerException is ArgumentException, "Rechazar fechas futuras."); }
        HanaHelper.Consultas.Clear(); HanaHelper.Parametros.Clear();
        new GerenciaComercialDA().AvancesAgentes("FAES", "GTQ", periodos);
        Check(HanaHelper.Consultas.Count == 2 && HanaHelper.Consultas[0].Contains("\"OINV\"") && HanaHelper.Consultas[0].Contains("\"ORIN\"") && HanaHelper.Consultas[0].Contains("\"OSLP\""), "Ventas de avance: facturas, notas y catálogo de agentes de FAES.");
        Check(HanaHelper.Consultas[0].Contains("\"SBOESCOCESA\".\"OINV\"") && HanaHelper.Consultas[0].Contains("COALESCE(G.\"Neto\",0)"), "Los agentes sin venta deben aparecer con valor cero.");
        Check(HanaHelper.Consultas[1].Contains("\"INV1\"") && HanaHelper.Consultas[1].Contains("\"RIN1\"") && HanaHelper.Consultas[1].Contains("\"Unidad\""), "Cantidades netas agrupadas por unidad de medida.");
        Check(HanaHelper.Parametros[0].Length == 5 && Convert.ToString(HanaHelper.Parametros[0][0].Value) == "GTQ" && HanaHelper.Consultas[0].Contains("? AS \"Desde\""), "Moneda y fechas deben enviarse como parámetros ODBC.");
        Check(typeof(GerenciaComercialController).GetMethod("Avances").GetCustomAttributes(typeof(System.Web.Mvc.HttpGetAttribute), true).Length == 1, "Avances debe conservar el permiso del controlador.");
        Check(typeof(GerenciaComercialController).GetMethod("ExportarAvances").GetCustomAttributes(typeof(System.Web.Mvc.HttpGetAttribute), true).Length == 1, "El Excel de avances debe conservar el permiso del controlador.");
        Check(typeof(GerenciaComercialController).GetMethod("PdfAvances").GetCustomAttributes(typeof(System.Web.Mvc.HttpGetAttribute), true).Length == 1, "El PDF debe conservar el permiso del controlador.");
        var ventasPdf = new DataTable();
        ventasPdf.Columns.Add("Codigo", typeof(string)); ventasPdf.Columns.Add("Nombre", typeof(string)); ventasPdf.Columns.Add("Frente", typeof(string)); ventasPdf.Columns.Add("Periodo", typeof(int)); ventasPdf.Columns.Add("Neto", typeof(decimal));
        var unidadesPdf = new DataTable();
        unidadesPdf.Columns.Add("Periodo", typeof(int)); unidadesPdf.Columns.Add("Unidad", typeof(string)); unidadesPdf.Columns.Add("Cantidad", typeof(decimal));
        var periodosPdf = new List<GerenciaComercialDA.PeriodoAvance>();
        for (int p = 0; p < 5; p++)
        {
            var fecha = DateTime.Today.AddYears(-5 + p);
            periodosPdf.Add(new GerenciaComercialDA.PeriodoAvance { Indice = p, Desde = fecha, Hasta = fecha.AddDays(25) });
            unidadesPdf.Rows.Add(p, "UN", 25m + p);
            for (int a = 1; a <= 8; a++) ventasPdf.Rows.Add(a.ToString(), "Agente " + a, "Agente comercial", p, 100m + p * 15m - a * 5m);
        }
        var pdfType = typeof(GerenciaComercialController).Assembly.GetType("DiamDev.Give.UI.Controllers.GerenciaComercialAvancesPdf");
        var pdfMethod = pdfType.GetMethod("Crear", BindingFlags.NonPublic | BindingFlags.Static);
        var pdf = (byte[])pdfMethod.Invoke(null, new object[] { "FAES", "GTQ", periodosPdf, ventasPdf, unidadesPdf });
        var pdfText = System.Text.Encoding.GetEncoding("ISO-8859-1").GetString(pdf);
        Check(pdfText.StartsWith("%PDF-1.4") && pdfText.Contains("/Count 5") && pdfText.Contains("%%EOF") && pdf.Length > 10000, "PDF multipágina con gráfica, lectura y panorama completo.");
        string sample = Environment.GetEnvironmentVariable("GC_PDF_SAMPLE");
        if (!string.IsNullOrEmpty(sample)) System.IO.File.WriteAllBytes(sample, pdf);
        var periodosDoce = new List<GerenciaComercialDA.PeriodoAvance>();
        var ventasDoce = ventasPdf.Clone();
        for (int p = 0; p < 12; p++)
        {
            var fecha = DateTime.Today.AddMonths(-12 + p);
            periodosDoce.Add(new GerenciaComercialDA.PeriodoAvance { Indice = p, Desde = fecha, Hasta = fecha.AddDays(10) });
            for (int a = 1; a <= 13; a++) ventasDoce.Rows.Add(a.ToString(), "Agente " + a, "Agente comercial", p, 100m + p * 15m - a * 5m);
        }
        var pdfDoce = (byte[])pdfMethod.Invoke(null, new object[] { "BOLIK", "USD", periodosDoce, ventasDoce, unidadesPdf });
        Check(System.Text.Encoding.GetEncoding("ISO-8859-1").GetString(pdfDoce).Contains("/Count 7"), "El PDF de 12 períodos y 13 agentes reparte gráficas y panorama en siete páginas.");
        string sampleDoce = Environment.GetEnvironmentVariable("GC_PDF_SAMPLE_12");
        if (!string.IsNullOrEmpty(sampleDoce)) System.IO.File.WriteAllBytes(sampleDoce, pdfDoce);
        var pdfVacio = (byte[])pdfMethod.Invoke(null, new object[] { "GRACO", "GTQ", periodosPdf.Take(2).ToList(), ventasPdf.Clone(), unidadesPdf.Clone() });
        Check(System.Text.Encoding.GetEncoding("ISO-8859-1").GetString(pdfVacio).Contains("/Count 3"), "El PDF vacío conserva resumen, lectura y panorama sin fallar.");
        var analyticsFiltro = (GerenciaComercialDA.Filtro)Invoke("AnalyticsFiltro", "all", desdeAvance, hastaAvance, "USD", 2, 25, "CLIENTE");
        Check(analyticsFiltro.Empresa == "" && analyticsFiltro.Moneda == "USD", "Analytics valida empresa y moneda antes de consultar.");
        HanaHelper.Consultas.Clear(); HanaHelper.Parametros.Clear();
        new GerenciaComercialDA().Analytics(analyticsFiltro, "producto-agente", "FAES|2", "", "P1", "", "current", true, false);
        var analyticsSql = HanaHelper.Consultas[0];
        Check(analyticsSql.Contains("\"OINV\"") && analyticsSql.Contains("\"ORIN\"") && analyticsSql.Contains("\"TotalFrgn\""), "Analytics resta créditos y distingue moneda documental.");
        Check(analyticsSql.Contains("\"agentKey\"") && analyticsSql.Contains("\"SlpCode\"") && analyticsSql.Contains("\"unitMsr\""), "Agentes por empresa y código; cantidades por unidad.");
        Check(analyticsSql.Contains("ROW_NUMBER()") && analyticsSql.Contains("COUNT(*) OVER()") && analyticsSql.Contains("\"rowNumber\">?"), "Paginación y conteo en SAP.");
        Check(analyticsSql.Contains("PARTITION BY \"company\"") && !analyticsSql.Contains("CL0149"), "Participación por empresa y cobertura completa predeterminada.");
        Check(HanaHelper.Parametros[0].Length == 13 && Convert.ToString(HanaHelper.Parametros[0][5].Value) == "FAES|2" && Convert.ToInt32(HanaHelper.Parametros[0][11].Value) == 25, "Filtros e índices de página parametrizados.");
        HanaHelper.Consultas.Clear();
        new GerenciaComercialDA().Analytics(analyticsFiltro, "general", orden:"current;DROP TABLE X", origen:true);
        Check(HanaHelper.Consultas[0].Contains("CL0149") && !HanaHelper.Consultas[0].Contains("DROP TABLE"), "Exclusiones explícitas y orden restringido.");
        foreach (string endpoint in new[] { "AnalyticsPanel", "AnalyticsDetalle", "AnalyticsPerfil", "AnalyticsExcel", "Operaciones" })
            Check(typeof(GerenciaComercialController).GetMethod(endpoint).GetCustomAttributes(typeof(System.Web.Mvc.HttpGetAttribute), true).Length == 1, "Endpoint Analytics bajo el permiso comercial: " + endpoint);
        Console.WriteLine("OK: " + _passed + " verificaciones de filtros, privacidad, detalle, avances y Analytics.");
        return 0;
    }
}
