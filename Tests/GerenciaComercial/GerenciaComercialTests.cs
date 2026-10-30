using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
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
        Console.WriteLine("OK: " + _passed + " verificaciones de filtros, privacidad, detalle y análisis histórico.");
        return 0;
    }
}
