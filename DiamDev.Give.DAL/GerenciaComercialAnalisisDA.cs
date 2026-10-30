using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Data.SqlClient;
using System.Linq;

namespace DiamDev.Give.DAL
{
    public sealed partial class GerenciaComercialDA
    {
        private const string ComparativoP = "WITH P AS (SELECT ? AS \"Desde\", ? AS \"Hasta\", ? AS \"AnteriorDesde\", ? AS \"AnteriorHasta\", ? AS \"Moneda\", ? AS \"Buscar\" FROM DUMMY), ";

        private static OdbcParameter[] ParametrosComparativos(Filtro f, int? primero = null, int? ultimo = null)
        {
            var p = new List<OdbcParameter>
            {
                new OdbcParameter("desde", OdbcType.Date) { Value = f.Desde.Date },
                new OdbcParameter("hasta", OdbcType.Date) { Value = f.Hasta.Date.AddDays(1) },
                new OdbcParameter("anteriorDesde", OdbcType.Date) { Value = f.Desde.Date.AddYears(-1) },
                new OdbcParameter("anteriorHasta", OdbcType.Date) { Value = f.Hasta.Date.AddYears(-1).AddDays(1) },
                new OdbcParameter("moneda", OdbcType.NVarChar) { Value = f.Moneda ?? "" },
                new OdbcParameter("buscar", OdbcType.NVarChar) { Value = "%" + (f.Buscar ?? "").ToUpperInvariant().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%" }
            };
            if (primero.HasValue) p.Add(new OdbcParameter("primero", OdbcType.Int) { Value = primero.Value });
            if (ultimo.HasValue) p.Add(new OdbcParameter("ultimo", OdbcType.Int) { Value = ultimo.Value });
            return p.ToArray();
        }

        private static string ParteComparativo(string dimension, string empresa)
        {
            if (dimension != "agentes" && dimension != "clientes" && dimension != "productos") throw new ArgumentException("Dimensión inválida.");
            string s = Schema(empresa), e = "'" + empresa + "'";
            return string.Join(" UNION ALL ", new[] { "OINV|INV1|1", "ORIN|RIN1|-1" }.Select(item =>
            {
                var x = item.Split('|');
                string tabla = x[0], linea = x[1], signo = x[2];
                string codigo = dimension == "agentes" ? "TO_NVARCHAR(H.\"SlpCode\")" : dimension == "clientes" ? "H.\"CardCode\"" : "L.\"ItemCode\"";
                string nombre = dimension == "agentes" ? "COALESCE(S.\"SlpName\", 'Sin agente')" : dimension == "clientes" ? "H.\"CardName\"" : "COALESCE(I.\"ItemName\", L.\"Dscription\")";
                string familia = dimension == "productos" ? "COALESCE(G.\"ItmsGrpNam\", 'Sin familia')" : "CAST(NULL AS NVARCHAR(100))";
                string joins = dimension == "agentes" ? " LEFT JOIN " + s + ".\"OSLP\" S ON S.\"SlpCode\"=H.\"SlpCode\"" : dimension == "productos" ? " JOIN " + s + ".\"" + linea + "\" L ON L.\"DocEntry\"=H.\"DocEntry\" LEFT JOIN " + s + ".\"OITM\" I ON I.\"ItemCode\"=L.\"ItemCode\" LEFT JOIN " + s + ".\"OITB\" G ON G.\"ItmsGrpCod\"=I.\"ItmsGrpCod\"" : "";
                string monto = dimension == "productos" ? "CASE WHEN H.\"DocCur\" IN ('QTZ','Q','GTQ') THEN L.\"LineTotal\" ELSE L.\"TotalFrgn\" END" : "CASE WHEN H.\"DocCur\" IN ('QTZ','Q','GTQ') THEN H.\"DocTotal\"-H.\"VatSum\" ELSE H.\"DocTotalFC\"-H.\"VatSumFC\" END";
                string cantidad = dimension == "productos" ? "L.\"Quantity\"" : "0";
                string filtroLinea = dimension == "productos" ? " AND L.\"ItemCode\" IS NOT NULL AND L.\"ItemCode\"<>''" : "";
                return "SELECT " + e + " AS \"Empresa\", " + codigo + " AS \"Codigo\", " + nombre + " AS \"Nombre\", " + familia + " AS \"Familia\", CASE WHEN H.\"DocCur\" IN ('QTZ','Q','GTQ') THEN 'GTQ' ELSE H.\"DocCur\" END AS \"Moneda\", CASE WHEN H.\"DocDate\">=P.\"Desde\" AND H.\"DocDate\"<P.\"Hasta\" THEN 1 ELSE 0 END AS \"Periodo\", " + signo + " * " + monto + " AS \"Neto\", " + signo + " * " + cantidad + " AS \"Cantidad\" FROM " + s + ".\"" + tabla + "\" H" + joins + " CROSS JOIN P WHERE H.\"CANCELED\"='N' AND ((H.\"DocDate\">=P.\"Desde\" AND H.\"DocDate\"<P.\"Hasta\") OR (H.\"DocDate\">=P.\"AnteriorDesde\" AND H.\"DocDate\"<P.\"AnteriorHasta\")) AND (P.\"Moneda\"='' OR CASE WHEN H.\"DocCur\" IN ('QTZ','Q','GTQ') THEN 'GTQ' ELSE H.\"DocCur\" END=P.\"Moneda\")" + filtroLinea;
            }));
        }

        private static string ConsultaComparativa(string dimension, Filtro f)
        {
            string union = string.Join(" UNION ALL ", SeleccionarEmpresas(f.Empresa).Select(e => ParteComparativo(dimension, e)));
            return ComparativoP + "X AS (" + union + "), G AS (SELECT \"Empresa\", \"Codigo\", MAX(\"Nombre\") AS \"Nombre\", MAX(\"Familia\") AS \"Familia\", \"Moneda\", SUM(CASE WHEN \"Periodo\"=1 THEN \"Neto\" ELSE 0 END) AS \"Actual\", SUM(CASE WHEN \"Periodo\"=0 THEN \"Neto\" ELSE 0 END) AS \"Anterior\", SUM(CASE WHEN \"Periodo\"=1 THEN \"Cantidad\" ELSE 0 END) AS \"CantidadActual\", SUM(CASE WHEN \"Periodo\"=0 THEN \"Cantidad\" ELSE 0 END) AS \"CantidadAnterior\" FROM X GROUP BY \"Empresa\", \"Codigo\", \"Moneda\"), T AS (SELECT G.*, SUM(G.\"Actual\") OVER (PARTITION BY G.\"Empresa\", G.\"Moneda\") AS \"TotalEmpresa\" FROM G), R AS (SELECT T.*, T.\"Actual\"-T.\"Anterior\" AS \"Delta\", CASE WHEN T.\"Anterior\"=0 THEN NULL ELSE 100*(T.\"Actual\"-T.\"Anterior\")/ABS(T.\"Anterior\") END AS \"Variacion\", CASE WHEN T.\"TotalEmpresa\"=0 THEN NULL ELSE 100*T.\"Actual\"/T.\"TotalEmpresa\" END AS \"Participacion\" FROM T) ";
        }

        public PaginaDatos Comparar(string dimension, Filtro f, string orden)
        {
            string baseSql = ConsultaComparativa(dimension, f);
            string filtrado = " FROM R CROSS JOIN P WHERE UPPER(COALESCE(R.\"Codigo\",'') || ' ' || COALESCE(R.\"Nombre\",'') || ' ' || COALESCE(R.\"Familia\",'')) LIKE P.\"Buscar\" ESCAPE '\\'";
            var ordenes = new Dictionary<string, string> { { "actual", "R.\"Actual\" DESC" }, { "anterior", "R.\"Anterior\" DESC" }, { "delta", "R.\"Delta\" DESC" }, { "caida", "R.\"Delta\" ASC" }, { "variacion", "R.\"Variacion\" DESC" }, { "cantidad", "R.\"CantidadActual\" DESC" }, { "nombre", "R.\"Nombre\" ASC" } };
            string seguro = ordenes.ContainsKey(orden ?? "") ? ordenes[orden] : ordenes["actual"];
            int primero = (f.Pagina - 1) * f.Tamano + 1;
            string sql = baseSql + "SELECT * FROM (SELECT R.*, COUNT(*) OVER () AS \"TotalFilas\", ROW_NUMBER() OVER (ORDER BY " + seguro + ", R.\"Empresa\", R.\"Moneda\", R.\"Codigo\") AS \"Fila\"" + filtrado + ") Z WHERE Z.\"Fila\">=? AND Z.\"Fila\"<=? ORDER BY Z.\"Fila\"";
            var filas = HanaHelper.EjecutarConsulta(sql, ParametrosComparativos(f, primero, primero + f.Tamano - 1));
            int total = filas.Rows.Count > 0 ? Convert.ToInt32(filas.Rows[0]["TotalFilas"]) : Convert.ToInt32(HanaHelper.EjecutarConsulta(baseSql + "SELECT COUNT(*) AS \"Total\"" + filtrado, ParametrosComparativos(f)).Rows[0][0]);
            if (filas.Columns.Contains("TotalFilas")) filas.Columns.Remove("TotalFilas");
            return new PaginaDatos { Total = total, Filas = filas };
        }

        public DataTable OperadoresComerciales()
        {
            using (var cn = AbrirSql())
            using (var cmd = new SqlCommand("SELECT DISTINCT Empresa_Id AS EmpresaId, Codigo FROM dbo.Usuario_Empresa WHERE Empresa_Id IN (20210705001,20210705003,20210705004) AND Codigo IS NOT NULL", cn))
            using (var adapter = new SqlDataAdapter(cmd))
            {
                var tabla = new DataTable();
                adapter.Fill(tabla);
                return tabla;
            }
        }

        public DataTable Historico(string dimension, Filtro f, string codigo)
        {
            if (dimension != "agentes" && dimension != "clientes" && dimension != "productos") throw new ArgumentException("Dimensión inválida.");
            if (string.IsNullOrWhiteSpace(f.Empresa) || string.IsNullOrWhiteSpace(codigo) || codigo.Length > 60) throw new ArgumentException("Seleccione empresa y código.");
            string s = Schema(f.Empresa);
            string codigoCol = dimension == "agentes" ? "H.\"SlpCode\"" : dimension == "clientes" ? "H.\"CardCode\"" : "L.\"ItemCode\"";
            var partes = new[] { "OINV|INV1|1", "ORIN|RIN1|-1" }.Select(item =>
            {
                var x = item.Split('|');
                string joins = dimension == "productos" ? " JOIN " + s + ".\"" + x[1] + "\" L ON L.\"DocEntry\"=H.\"DocEntry\"" : "";
                string monto = dimension == "productos" ? "CASE WHEN H.\"DocCur\" IN ('QTZ','Q','GTQ') THEN L.\"LineTotal\" ELSE L.\"TotalFrgn\" END" : "CASE WHEN H.\"DocCur\" IN ('QTZ','Q','GTQ') THEN H.\"DocTotal\"-H.\"VatSum\" ELSE H.\"DocTotalFC\"-H.\"VatSumFC\" END";
                return "SELECT TO_NVARCHAR(H.\"DocDate\", 'YYYY-MM') AS \"Mes\", CASE WHEN H.\"DocCur\" IN ('QTZ','Q','GTQ') THEN 'GTQ' ELSE H.\"DocCur\" END AS \"Moneda\", " + x[2] + " * " + monto + " AS \"Neto\", " + x[2] + " * " + (dimension == "productos" ? "L.\"Quantity\"" : "0") + " AS \"Cantidad\" FROM " + s + ".\"" + x[0] + "\" H" + joins + " CROSS JOIN P WHERE H.\"CANCELED\"='N' AND H.\"DocDate\">=P.\"Desde\" AND H.\"DocDate\"<P.\"Hasta\" AND TO_NVARCHAR(" + codigoCol + ")=P.\"Codigo\" AND (P.\"Moneda\"='' OR CASE WHEN H.\"DocCur\" IN ('QTZ','Q','GTQ') THEN 'GTQ' ELSE H.\"DocCur\" END=P.\"Moneda\")";
            });
            var inicio = new DateTime(f.Hasta.Year, f.Hasta.Month, 1).AddYears(-5);
            string sql = "WITH P AS (SELECT ? AS \"Desde\", ? AS \"Hasta\", ? AS \"Codigo\", ? AS \"Moneda\" FROM DUMMY) SELECT X.\"Mes\", X.\"Moneda\", SUM(X.\"Neto\") AS \"Neto\", SUM(X.\"Cantidad\") AS \"Cantidad\" FROM (" + string.Join(" UNION ALL ", partes) + ") X GROUP BY X.\"Mes\", X.\"Moneda\" ORDER BY X.\"Mes\"";
            return HanaHelper.EjecutarConsulta(sql, new[] { new OdbcParameter("desde", OdbcType.Date) { Value = inicio }, new OdbcParameter("hasta", OdbcType.Date) { Value = f.Hasta.Date.AddDays(1) }, new OdbcParameter("codigo", OdbcType.NVarChar) { Value = codigo }, new OdbcParameter("moneda", OdbcType.NVarChar) { Value = f.Moneda ?? "" } });
        }
    }
}
