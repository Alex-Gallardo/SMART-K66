using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Linq;

namespace DiamDev.Give.DAL
{
    public sealed partial class GerenciaComercialDA
    {
        public sealed class PeriodoAvance
        {
            public int Indice { get; set; }
            public DateTime Desde { get; set; }
            public DateTime Hasta { get; set; }
        }

        public sealed class DatosAvance
        {
            public DataTable Ventas { get; set; }
            public DataTable Unidades { get; set; }
        }

        private static string CtePeriodos(IList<PeriodoAvance> periodos)
        {
            return "WITH F AS (SELECT ? AS \"Moneda\" FROM DUMMY), P AS (" +
                string.Join(" UNION ALL ", periodos.Select((p, i) => "SELECT ? AS \"Desde\", ? AS \"Hasta\", " + i + " AS \"Periodo\" FROM DUMMY")) + ") ";
        }

        private static OdbcParameter[] ParametrosAvance(string moneda, IList<PeriodoAvance> periodos)
        {
            var parametros = new List<OdbcParameter> { new OdbcParameter("moneda", OdbcType.NVarChar) { Value = moneda } };
            foreach (var p in periodos)
            {
                parametros.Add(new OdbcParameter("desde", OdbcType.Date) { Value = p.Desde.Date });
                parametros.Add(new OdbcParameter("hasta", OdbcType.Date) { Value = p.Hasta.Date.AddDays(1) });
            }
            return parametros.ToArray();
        }

        private static string DocumentosAvance(string schema)
        {
            return string.Join(" UNION ALL ", new[] { "OINV|1", "ORIN|-1" }.Select(item =>
            {
                var x = item.Split('|');
                return "SELECT P.\"Periodo\", COALESCE(H.\"SlpCode\",-1) AS \"SlpCode\", " + x[1] + " * CASE WHEN H.\"DocCur\" IN ('QTZ','Q','GTQ') THEN H.\"DocTotal\"-H.\"VatSum\" ELSE H.\"DocTotalFC\"-H.\"VatSumFC\" END AS \"Neto\", " + (x[1] == "1" ? "1" : "0") + " AS \"Facturas\", " + (x[1] == "-1" ? "1" : "0") + " AS \"NotasCredito\" FROM " + schema + ".\"" + x[0] + "\" H JOIN P ON H.\"DocDate\">=P.\"Desde\" AND H.\"DocDate\"<P.\"Hasta\" CROSS JOIN F WHERE H.\"CANCELED\"='N' AND CASE WHEN H.\"DocCur\" IN ('QTZ','Q','GTQ') THEN 'GTQ' ELSE H.\"DocCur\" END=F.\"Moneda\"";
            }));
        }

        private static string LineasAvance(string schema)
        {
            return string.Join(" UNION ALL ", new[] { "OINV|INV1|1", "ORIN|RIN1|-1" }.Select(item =>
            {
                var x = item.Split('|');
                return "SELECT P.\"Periodo\", TO_NVARCHAR(COALESCE(H.\"SlpCode\",-1)) AS \"Codigo\", COALESCE(NULLIF(TRIM(L.\"unitMsr\"),''),'Sin unidad') AS \"Unidad\", " + x[2] + " * L.\"Quantity\" AS \"Cantidad\" FROM " + schema + ".\"" + x[0] + "\" H JOIN " + schema + ".\"" + x[1] + "\" L ON L.\"DocEntry\"=H.\"DocEntry\" JOIN P ON H.\"DocDate\">=P.\"Desde\" AND H.\"DocDate\"<P.\"Hasta\" CROSS JOIN F WHERE H.\"CANCELED\"='N' AND L.\"ItemCode\" IS NOT NULL AND L.\"ItemCode\"<>'' AND CASE WHEN H.\"DocCur\" IN ('QTZ','Q','GTQ') THEN 'GTQ' ELSE H.\"DocCur\" END=F.\"Moneda\"";
            }));
        }

        public DatosAvance AvancesAgentes(string empresa, string moneda, IList<PeriodoAvance> periodos)
        {
            if (!EmpresaIds.ContainsKey(empresa)) throw new ArgumentException("Empresa inválida.");
            if (moneda != "GTQ" && moneda != "USD") throw new ArgumentException("Moneda inválida.");
            if (periodos == null || periodos.Count < 2 || periodos.Count > 12 || periodos.Any(p => p.Desde > p.Hasta)) throw new ArgumentException("Seleccione entre 2 y 12 períodos válidos.");
            string schema = Schema(empresa);
            string cte = CtePeriodos(periodos);
            string ventas = cte + ", X AS (" + DocumentosAvance(schema) + "), G AS (SELECT \"Periodo\", \"SlpCode\", SUM(\"Neto\") AS \"Neto\", SUM(\"Facturas\") AS \"Facturas\", SUM(\"NotasCredito\") AS \"NotasCredito\" FROM X GROUP BY \"Periodo\", \"SlpCode\"), A AS (SELECT S.\"SlpCode\", S.\"SlpName\", S.\"Active\" FROM " + schema + ".\"OSLP\" S WHERE S.\"Active\"='Y' OR EXISTS (SELECT 1 FROM G WHERE G.\"SlpCode\"=S.\"SlpCode\") UNION ALL SELECT DISTINCT G.\"SlpCode\", 'Sin agente SAP' AS \"SlpName\", 'N' AS \"Active\" FROM G WHERE NOT EXISTS (SELECT 1 FROM " + schema + ".\"OSLP\" S WHERE S.\"SlpCode\"=G.\"SlpCode\")) SELECT '" + empresa + "' AS \"Empresa\", P.\"Periodo\", TO_NVARCHAR(A.\"SlpCode\") AS \"Codigo\", A.\"SlpName\" AS \"Nombre\", A.\"Active\" AS \"Activo\", COALESCE(G.\"Neto\",0) AS \"Neto\", COALESCE(G.\"Facturas\",0) AS \"Facturas\", COALESCE(G.\"NotasCredito\",0) AS \"NotasCredito\" FROM A CROSS JOIN P LEFT JOIN G ON G.\"SlpCode\"=A.\"SlpCode\" AND G.\"Periodo\"=P.\"Periodo\" ORDER BY A.\"SlpName\", P.\"Periodo\"";
            string unidades = cte + ", X AS (" + LineasAvance(schema) + ") SELECT X.\"Periodo\", X.\"Codigo\", X.\"Unidad\", SUM(X.\"Cantidad\") AS \"Cantidad\" FROM X GROUP BY X.\"Periodo\", X.\"Codigo\", X.\"Unidad\" ORDER BY X.\"Periodo\", X.\"Codigo\", X.\"Unidad\"";
            var parametros = ParametrosAvance(moneda, periodos);
            return new DatosAvance { Ventas = HanaHelper.EjecutarConsulta(ventas, parametros), Unidades = HanaHelper.EjecutarConsulta(unidades, ParametrosAvance(moneda, periodos)) };
        }
    }
}
