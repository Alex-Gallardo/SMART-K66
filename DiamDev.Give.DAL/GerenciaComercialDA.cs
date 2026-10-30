using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Odbc;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DiamDev.Give.DAL
{
    /// <summary>Consultas comerciales de solo lectura. Nunca ejecuta SQL recibido del navegador.</summary>
    public sealed partial class GerenciaComercialDA
    {
        private static readonly string[] Empresas = { "BOLIK", "FAES", "GRACO" };
        private static readonly Dictionary<string, long> EmpresaIds = new Dictionary<string, long>
        {
            { "BOLIK", 20210705001L }, { "FAES", 20210705003L }, { "GRACO", 20210705004L }
        };

        public sealed class Filtro
        {
            public string Empresa { get; set; }
            public string Buscar { get; set; }
            public string Moneda { get; set; }
            public string Estado { get; set; }
            public string Agente { get; set; }
            public string Cliente { get; set; }
            public DateTime Desde { get; set; }
            public DateTime Hasta { get; set; }
            public int Pagina { get; set; }
            public int Tamano { get; set; }
        }

        public sealed class PaginaDatos
        {
            public int Total { get; set; }
            public DataTable Filas { get; set; }
        }

        private static IEnumerable<string> SeleccionarEmpresas(string empresa)
        {
            return string.IsNullOrEmpty(empresa) ? Empresas : new[] { empresa };
        }

        private static string Schema(string empresa)
        {
            if (!EmpresaIds.ContainsKey(empresa)) throw new ArgumentException("Empresa inválida.");
            string schema = ConfigurationManager.AppSettings["HanaSchema." + empresa];
            if (string.IsNullOrWhiteSpace(schema) || !Regex.IsMatch(schema, @"^[A-Za-z0-9_]+$"))
                throw new ConfigurationErrorsException("Configure un schema HANA válido para " + empresa + ".");
            return "\"" + schema + "\"";
        }

        private static OdbcParameter[] Parametros(Filtro f, int? primero = null, int? ultimo = null)
        {
            var ps = new List<OdbcParameter>
            {
                new OdbcParameter("desde", OdbcType.Date) { Value = f.Desde.Date },
                new OdbcParameter("hasta", OdbcType.Date) { Value = f.Hasta.Date.AddDays(1) },
                new OdbcParameter("buscar", OdbcType.NVarChar) { Value = "%" + (f.Buscar ?? "").ToUpperInvariant().Replace("%", "\\%").Replace("_", "\\_") + "%" },
                new OdbcParameter("moneda", OdbcType.NVarChar) { Value = f.Moneda ?? "" },
                new OdbcParameter("estado", OdbcType.NVarChar) { Value = f.Estado ?? "" },
                new OdbcParameter("agente", OdbcType.NVarChar) { Value = f.Agente ?? "" },
                new OdbcParameter("cliente", OdbcType.NVarChar) { Value = f.Cliente ?? "" }
            };
            if (primero.HasValue) ps.Add(new OdbcParameter("primero", OdbcType.Int) { Value = primero.Value });
            if (ultimo.HasValue) ps.Add(new OdbcParameter("ultimo", OdbcType.Int) { Value = ultimo.Value });
            return ps.ToArray();
        }

        private const string Cte = "WITH P AS (SELECT ? AS \"Desde\", ? AS \"Hasta\", ? AS \"Buscar\", ? AS \"Moneda\", ? AS \"Estado\", ? AS \"Agente\", ? AS \"Cliente\" FROM DUMMY) ";

        private static string ParteSap(string tipo, string empresa)
        {
            string s = Schema(empresa);
            string e = "'" + empresa + "'";
            switch (tipo)
            {
                case "clientes":
                    return string.Format(@"SELECT {0} AS ""Empresa"", 'SAP' AS ""Fuente"", C.""CardCode"" AS ""Codigo"", C.""CardName"" AS ""Nombre"", 'Cliente' AS ""Tipo"", CAST(NULL AS DATE) AS ""Fecha"", C.""validFor"" AS ""Estado"", CASE WHEN C.""Currency"" IN ('QTZ','Q','GTQ') THEN 'GTQ' ELSE C.""Currency"" END AS ""Moneda"", CAST(NULL AS DECIMAL(19,2)) AS ""Importe"", S.""SlpName"" AS ""Secundario"", CAST(NULL AS DECIMAL(19,2)) AS ""Existencia""
                        FROM {1}.""OCRD"" C LEFT JOIN {1}.""OSLP"" S ON S.""SlpCode""=C.""SlpCode"" CROSS JOIN P
                        WHERE C.""CardType""='C' AND (P.""Estado""='' OR C.""validFor""=P.""Estado"") AND (P.""Agente""='' OR TO_NVARCHAR(C.""SlpCode"")=P.""Agente"") AND (P.""Cliente""='' OR C.""CardCode""=P.""Cliente"") AND UPPER(C.""CardCode"" || ' ' || C.""CardName"" || ' ' || COALESCE(C.""LicTradNum"",'')) LIKE P.""Buscar"" ESCAPE '\'", e, s);
                case "productos":
                    return string.Format(@"SELECT {0} AS ""Empresa"", 'SAP' AS ""Fuente"", I.""ItemCode"" AS ""Codigo"", I.""ItemName"" AS ""Nombre"", G.""ItmsGrpNam"" AS ""Tipo"", CAST(NULL AS DATE) AS ""Fecha"", I.""validFor"" AS ""Estado"", CAST(NULL AS NVARCHAR(10)) AS ""Moneda"", CAST(NULL AS DECIMAL(19,2)) AS ""Importe"", I.""InvntryUom"" AS ""Secundario"", I.""OnHand""-I.""IsCommited"" AS ""Existencia""
                        FROM {1}.""OITM"" I LEFT JOIN {1}.""OITB"" G ON G.""ItmsGrpCod""=I.""ItmsGrpCod"" CROSS JOIN P
                        WHERE (P.""Estado""='' OR I.""validFor""=P.""Estado"") AND UPPER(I.""ItemCode"" || ' ' || I.""ItemName"") LIKE P.""Buscar"" ESCAPE '\'", e, s);
                case "agentes":
                    return string.Format(@"SELECT {0} AS ""Empresa"", 'SAP' AS ""Fuente"", TO_NVARCHAR(S.""SlpCode"") AS ""Codigo"", S.""SlpName"" AS ""Nombre"", 'Agente' AS ""Tipo"", CAST(NULL AS DATE) AS ""Fecha"", S.""Active"" AS ""Estado"", CAST(NULL AS NVARCHAR(10)) AS ""Moneda"", CAST(NULL AS DECIMAL(19,2)) AS ""Importe"", CAST(NULL AS NVARCHAR(100)) AS ""Secundario"", CAST(NULL AS DECIMAL(19,2)) AS ""Existencia""
                        FROM {1}.""OSLP"" S CROSS JOIN P WHERE (P.""Estado""='' OR S.""Active""=P.""Estado"") AND (P.""Agente""='' OR TO_NVARCHAR(S.""SlpCode"")=P.""Agente"") AND UPPER(TO_NVARCHAR(S.""SlpCode"") || ' ' || S.""SlpName"") LIKE P.""Buscar"" ESCAPE '\'", e, s);
                case "pedidos":
                    return string.Format(@"SELECT {0} AS ""Empresa"", 'SAP' AS ""Fuente"", TO_NVARCHAR(O.""DocEntry"") AS ""Codigo"", O.""CardName"" AS ""Nombre"", 'Pedido ' || TO_NVARCHAR(O.""DocNum"") AS ""Tipo"", O.""DocDate"" AS ""Fecha"", O.""DocStatus"" AS ""Estado"", CASE WHEN O.""DocCur"" IN ('QTZ','Q','GTQ') THEN 'GTQ' ELSE O.""DocCur"" END AS ""Moneda"", CASE WHEN O.""DocCur"" IN ('QTZ','Q','GTQ') THEN O.""DocTotal"" ELSE O.""DocTotalFC"" END AS ""Importe"", O.""CardCode"" AS ""Secundario"", CAST(NULL AS DECIMAL(19,2)) AS ""Existencia""
                        FROM {1}.""ORDR"" O CROSS JOIN P WHERE O.""CANCELED""='N' AND O.""DocDate"">=P.""Desde"" AND O.""DocDate""<P.""Hasta"" AND (P.""Moneda""='' OR CASE WHEN O.""DocCur"" IN ('QTZ','Q','GTQ') THEN 'GTQ' ELSE O.""DocCur"" END=P.""Moneda"") AND (P.""Estado""='' OR O.""DocStatus""=P.""Estado"") AND (P.""Agente""='' OR TO_NVARCHAR(O.""SlpCode"")=P.""Agente"") AND (P.""Cliente""='' OR O.""CardCode""=P.""Cliente"") AND UPPER(O.""CardCode"" || ' ' || O.""CardName"" || ' ' || TO_NVARCHAR(O.""DocNum"")) LIKE P.""Buscar"" ESCAPE '\'", e, s);
                case "ventas":
                    return string.Join(" UNION ALL ", new[] { "OINV", "ORIN" }.Select((t, i) => string.Format(@"SELECT {0} AS ""Empresa"", 'SAP' AS ""Fuente"", TO_NVARCHAR(V.""DocEntry"") AS ""Codigo"", V.""CardName"" AS ""Nombre"", '{2} ' || TO_NVARCHAR(V.""DocNum"") AS ""Tipo"", V.""DocDate"" AS ""Fecha"", V.""DocStatus"" AS ""Estado"", CASE WHEN V.""DocCur"" IN ('QTZ','Q','GTQ') THEN 'GTQ' ELSE V.""DocCur"" END AS ""Moneda"", {4} * CASE WHEN V.""DocCur"" IN ('QTZ','Q','GTQ') THEN V.""DocTotal""-V.""VatSum"" ELSE V.""DocTotalFC""-V.""VatSumFC"" END AS ""Importe"", V.""CardCode"" AS ""Secundario"", CAST(NULL AS DECIMAL(19,2)) AS ""Existencia""
                        FROM {1}.""{3}"" V CROSS JOIN P WHERE V.""CANCELED""='N' AND V.""DocDate"">=P.""Desde"" AND V.""DocDate""<P.""Hasta"" AND (P.""Moneda""='' OR CASE WHEN V.""DocCur"" IN ('QTZ','Q','GTQ') THEN 'GTQ' ELSE V.""DocCur"" END=P.""Moneda"") AND (P.""Estado""='' OR V.""DocStatus""=P.""Estado"") AND (P.""Agente""='' OR TO_NVARCHAR(V.""SlpCode"")=P.""Agente"") AND (P.""Cliente""='' OR V.""CardCode""=P.""Cliente"") AND UPPER(V.""CardCode"" || ' ' || V.""CardName"" || ' ' || TO_NVARCHAR(V.""DocNum"")) LIKE P.""Buscar"" ESCAPE '\'", e, s, i == 0 ? "Factura" : "Nota de crédito", t, i == 0 ? "1" : "-1")));
                default: throw new ArgumentException("Consulta inválida.");
            }
        }

        public PaginaDatos ListarSap(string tipo, Filtro f)
        {
            string union = string.Join(" UNION ALL ", SeleccionarEmpresas(f.Empresa).Select(e => ParteSap(tipo, e)));
            string count = Cte + "SELECT COUNT(*) AS \"Total\" FROM (" + union + ") Q";
            int total = Convert.ToInt32(HanaHelper.EjecutarConsulta(count, Parametros(f)).Rows[0][0]);
            int primero = (f.Pagina - 1) * f.Tamano + 1;
            string orden = tipo == "pedidos" || tipo == "ventas" ? "Q.\"Fecha\" DESC, Q.\"Empresa\", Q.\"Tipo\", Q.\"Codigo\" DESC" : "Q.\"Empresa\", Q.\"Nombre\", Q.\"Codigo\"";
            string sql = Cte + "SELECT * FROM (SELECT Q.*, ROW_NUMBER() OVER (ORDER BY " + orden + ") AS \"Fila\" FROM (" + union + ") Q) Z WHERE Z.\"Fila\">=? AND Z.\"Fila\"<=? ORDER BY Z.\"Fila\"";
            return new PaginaDatos { Total = total, Filas = HanaHelper.EjecutarConsulta(sql, Parametros(f, primero, primero + f.Tamano - 1)) };
        }

        private static string ParteResumenVentas(string empresa)
        {
            string s = Schema(empresa), e = "'" + empresa + "'";
            return string.Join(" UNION ALL ", new[] { "OINV", "ORIN" }.Select((t, i) => string.Format(@"SELECT {0} AS ""Empresa"", V.""DocDate"" AS ""Dia"", CASE WHEN V.""DocCur"" IN ('QTZ','Q','GTQ') THEN 'GTQ' ELSE V.""DocCur"" END AS ""Moneda"", {2} AS ""Signo"", CASE WHEN V.""DocCur"" IN ('QTZ','Q','GTQ') THEN V.""DocTotal""-V.""VatSum"" ELSE V.""DocTotalFC""-V.""VatSumFC"" END AS ""Monto"", V.""CardCode"" AS ""Cliente""
                FROM {1}.""{3}"" V CROSS JOIN P WHERE V.""CANCELED""='N' AND V.""DocDate"">=P.""Desde"" AND V.""DocDate""<P.""Hasta"" AND (P.""Moneda""='' OR CASE WHEN V.""DocCur"" IN ('QTZ','Q','GTQ') THEN 'GTQ' ELSE V.""DocCur"" END=P.""Moneda"")", e, s, i == 0 ? "1" : "-1", t)));
        }

        public DataTable VentasDiarias(Filtro f)
        {
            string union = string.Join(" UNION ALL ", SeleccionarEmpresas(f.Empresa).Select(ParteResumenVentas));
            return HanaHelper.EjecutarConsulta(Cte + "SELECT X.\"Empresa\", X.\"Dia\", X.\"Moneda\", SUM(X.\"Monto\"*X.\"Signo\") AS \"Neto\", SUM(CASE WHEN X.\"Signo\"=1 THEN 1 ELSE 0 END) AS \"Facturas\", SUM(CASE WHEN X.\"Signo\"=-1 THEN 1 ELSE 0 END) AS \"NotasCredito\", COUNT(DISTINCT CASE WHEN X.\"Signo\"=1 THEN X.\"Cliente\" END) AS \"Clientes\" FROM (" + union + ") X GROUP BY X.\"Empresa\", X.\"Dia\", X.\"Moneda\" ORDER BY X.\"Dia\"", Parametros(f));
        }

        public DataTable Cobros(Filtro f)
        {
            var partes = SeleccionarEmpresas(f.Empresa).Select(e =>
            {
                string s = Schema(e);
                return string.Format(@"SELECT '{0}' AS ""Empresa"", CASE WHEN R.""DocCurr"" IN ('QTZ','Q','GTQ') THEN 'GTQ' ELSE R.""DocCurr"" END AS ""Moneda"", COUNT(*) AS ""Recibos"", SUM(CASE WHEN R.""DocCurr"" IN ('QTZ','Q','GTQ') THEN R.""DocTotal"" ELSE R.""DocTotalFC"" END) AS ""Monto"" FROM {1}.""ORCT"" R CROSS JOIN P WHERE R.""Canceled""='N' AND R.""DocDate"">=P.""Desde"" AND R.""DocDate""<P.""Hasta"" AND (P.""Moneda""='' OR CASE WHEN R.""DocCurr"" IN ('QTZ','Q','GTQ') THEN 'GTQ' ELSE R.""DocCurr"" END=P.""Moneda"") GROUP BY R.""DocCurr""", e, s);
            });
            return HanaHelper.EjecutarConsulta(Cte + string.Join(" UNION ALL ", partes), Parametros(f));
        }

        public DataTable Catalogos(Filtro f)
        {
            var partes = SeleccionarEmpresas(f.Empresa).Select(e =>
            {
                string s = Schema(e);
                return string.Format(@"SELECT '{0}' AS ""Empresa"", (SELECT COUNT(*) FROM {1}.""OCRD"" WHERE ""CardType""='C') AS ""Clientes"", (SELECT COUNT(*) FROM {1}.""OITM"") AS ""Productos"", (SELECT COUNT(*) FROM {1}.""OSLP"") AS ""Agentes"", (SELECT COUNT(*) FROM {1}.""ORDR"" WHERE ""CANCELED""='N' AND ""DocStatus""='O') AS ""PedidosAbiertos"", (SELECT COUNT(*) FROM {1}.""ORDR"" WHERE ""CANCELED""='N' AND ""DocDate"">=P.""Desde"" AND ""DocDate""<P.""Hasta"" AND (P.""Moneda""='' OR CASE WHEN ""DocCur"" IN ('QTZ','Q','GTQ') THEN 'GTQ' ELSE ""DocCur"" END=P.""Moneda"")) AS ""PedidosPeriodo"" FROM DUMMY CROSS JOIN P", e, s);
            });
            return HanaHelper.EjecutarConsulta(Cte + string.Join(" UNION ALL ", partes), Parametros(f));
        }

        public DataTable TopProductos(Filtro f)
        {
            var partes = SeleccionarEmpresas(f.Empresa).Select(e =>
            {
                string s = Schema(e);
                return string.Join(" UNION ALL ", new[] { "OINV|INV1|1", "ORIN|RIN1|-1" }.Select(v =>
                {
                    var x = v.Split('|');
                    return string.Format(@"SELECT '{0}' AS ""Empresa"", L.""ItemCode"" AS ""Codigo"", L.""Dscription"" AS ""Nombre"", {3} * L.""Quantity"" AS ""Cantidad"" FROM {1}.""{2}"" H JOIN {1}.""{4}"" L ON L.""DocEntry""=H.""DocEntry"" CROSS JOIN P WHERE H.""CANCELED""='N' AND H.""DocDate"">=P.""Desde"" AND H.""DocDate""<P.""Hasta"" AND L.""ItemCode"" IS NOT NULL AND L.""ItemCode""<>'' AND (P.""Moneda""='' OR CASE WHEN H.""DocCur"" IN ('QTZ','Q','GTQ') THEN 'GTQ' ELSE H.""DocCur"" END=P.""Moneda"")", e, s, x[0], x[2], x[1]);
                }));
            });
            string union = string.Join(" UNION ALL ", partes);
            return HanaHelper.EjecutarConsulta(Cte + "SELECT X.\"Empresa\", X.\"Codigo\", MAX(X.\"Nombre\") AS \"Nombre\", SUM(X.\"Cantidad\") AS \"Cantidad\" FROM (" + union + ") X GROUP BY X.\"Empresa\", X.\"Codigo\" ORDER BY \"Cantidad\" DESC LIMIT 10", Parametros(f));
        }

        public DataTable TopDimension(Filtro f, string dimension)
        {
            if (dimension != "agentes" && dimension != "clientes") throw new ArgumentException("Dimensión inválida.");
            string codigo = dimension == "agentes" ? "V.\"SlpCode\"" : "V.\"CardCode\"";
            string nombre = dimension == "agentes" ? "S.\"SlpName\"" : "V.\"CardName\"";
            var partes = SeleccionarEmpresas(f.Empresa).Select(e =>
            {
                string s = Schema(e);
                return string.Join(" UNION ALL ", new[] { "OINV|1", "ORIN|-1" }.Select(v =>
                {
                    var x = v.Split('|');
                    return string.Format(@"SELECT '{0}' AS ""Empresa"", TO_NVARCHAR({2}) AS ""Codigo"", {3} AS ""Nombre"", CASE WHEN V.""DocCur"" IN ('QTZ','Q','GTQ') THEN 'GTQ' ELSE V.""DocCur"" END AS ""Moneda"", {4} * CASE WHEN V.""DocCur"" IN ('QTZ','Q','GTQ') THEN V.""DocTotal""-V.""VatSum"" ELSE V.""DocTotalFC""-V.""VatSumFC"" END AS ""Neto"" FROM {1}.""{5}"" V LEFT JOIN {1}.""OSLP"" S ON S.""SlpCode""=V.""SlpCode"" CROSS JOIN P WHERE V.""CANCELED""='N' AND V.""DocDate"">=P.""Desde"" AND V.""DocDate""<P.""Hasta"" AND (P.""Moneda""='' OR CASE WHEN V.""DocCur"" IN ('QTZ','Q','GTQ') THEN 'GTQ' ELSE V.""DocCur"" END=P.""Moneda"")", e, s, codigo, nombre, x[1], x[0]);
                }));
            });
            string union = string.Join(" UNION ALL ", partes);
            return HanaHelper.EjecutarConsulta(Cte + "SELECT X.\"Empresa\", X.\"Codigo\", MAX(X.\"Nombre\") AS \"Nombre\", X.\"Moneda\", SUM(X.\"Neto\") AS \"Neto\" FROM (" + union + ") X GROUP BY X.\"Empresa\", X.\"Codigo\", X.\"Moneda\" ORDER BY \"Neto\" DESC LIMIT 10", Parametros(f));
        }

        private static SqlConnection AbrirSql()
        {
            var config = ConfigurationManager.ConnectionStrings["GiveContext"];
            if (config == null) throw new ConfigurationErrorsException("Falta GiveContext.");
            var b = new SqlConnectionStringBuilder(config.ConnectionString);
            if (!string.Equals(b.InitialCatalog, "POS-SmartK66_DEV", StringComparison.OrdinalIgnoreCase))
                throw new ConfigurationErrorsException("Gerencia Comercial requiere POS-SmartK66_DEV.");
            b.ApplicationIntent = ApplicationIntent.ReadOnly;
            var cn = new SqlConnection(b.ConnectionString);
            cn.Open();
            return cn;
        }

        private static void ParametrosSql(SqlCommand cmd, Filtro f)
        {
            cmd.Parameters.Add("@desde", SqlDbType.DateTime).Value = f.Desde.Date;
            cmd.Parameters.Add("@hasta", SqlDbType.DateTime).Value = f.Hasta.Date.AddDays(1);
            cmd.Parameters.Add("@buscar", SqlDbType.NVarChar, 220).Value = "%" + (f.Buscar ?? "").Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]") + "%";
            cmd.Parameters.Add("@empresa", SqlDbType.BigInt).Value = string.IsNullOrEmpty(f.Empresa) ? (object)DBNull.Value : EmpresaIds[f.Empresa];
            cmd.Parameters.Add("@estado", SqlDbType.NVarChar, 20).Value = f.Estado ?? "";
            cmd.Parameters.Add("@cliente", SqlDbType.NVarChar, 60).Value = f.Cliente ?? "";
            cmd.Parameters.Add("@moneda", SqlDbType.NVarChar, 10).Value = f.Moneda ?? "";
        }

        private static DataTable LeerSql(SqlConnection cn, string sql, Filtro f, int? primero = null, int? ultimo = null)
        {
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.CommandTimeout = 60;
                ParametrosSql(cmd, f);
                if (primero.HasValue) cmd.Parameters.Add("@primero", SqlDbType.Int).Value = primero.Value;
                if (ultimo.HasValue) cmd.Parameters.Add("@ultimo", SqlDbType.Int).Value = ultimo.Value;
                var result = new DataTable();
                using (var adapter = new SqlDataAdapter(cmd)) adapter.Fill(result);
                return result;
            }
        }

        private static string ParteSql(string tipo)
        {
            const string empresa = "CASE X.Empresa_Id WHEN 20210705001 THEN 'BOLIK' WHEN 20210705003 THEN 'FAES' WHEN 20210705004 THEN 'GRACO' END";
            switch (tipo)
            {
                case "clientesSql":
                    return "SELECT " + empresa + " AS Empresa, 'SQL' AS Fuente, CONVERT(nvarchar(50),X.Cliente_Id) AS Codigo, X.Nombre, 'Cliente POS' AS Tipo, X.Fecha, CASE WHEN X.Activo=1 THEN 'Y' ELSE 'N' END AS Estado, CAST(NULL AS nvarchar(10)) AS Moneda, X.Limite_Credito AS Importe, X.Nit AS Secundario, CAST(NULL AS decimal(19,2)) AS Existencia FROM dbo.Cliente X WHERE X.Empresa_Id IN (20210705001,20210705003,20210705004) AND (@empresa IS NULL OR X.Empresa_Id=@empresa) AND (@estado='' OR (X.Activo=1 AND @estado='Y') OR (X.Activo=0 AND @estado='N')) AND (X.Nombre LIKE @buscar OR X.Nit LIKE @buscar OR CONVERT(nvarchar(50),X.Cliente_Id) LIKE @buscar)";
                case "productosSql":
                    return "SELECT " + empresa + " AS Empresa, 'SQL' AS Fuente, X.Producto_Id AS Codigo, X.Nombre, 'Producto POS' AS Tipo, X.Fecha, CASE WHEN X.Activo=1 THEN 'Y' ELSE 'N' END AS Estado, CAST(NULL AS nvarchar(10)) AS Moneda, CAST(NULL AS decimal(19,2)) AS Importe, X.Codigo AS Secundario, X.Cantidad AS Existencia FROM dbo.Producto X WHERE X.Empresa_Id IN (20210705001,20210705003,20210705004) AND (@empresa IS NULL OR X.Empresa_Id=@empresa) AND (@estado='' OR (X.Activo=1 AND @estado='Y') OR (X.Activo=0 AND @estado='N')) AND (X.Nombre LIKE @buscar OR X.Codigo LIKE @buscar OR X.Producto_Id LIKE @buscar)";
                case "agentesSql":
                    return "SELECT " + empresa + " AS Empresa, 'SQL' AS Fuente, CONVERT(nvarchar(50),X.Vendedor_Id) AS Codigo, X.Nombre, 'Vendedor POS' AS Tipo, X.Fecha, CASE WHEN X.Activo=1 THEN 'Y' ELSE 'N' END AS Estado, CAST(NULL AS nvarchar(10)) AS Moneda, CAST(NULL AS decimal(19,2)) AS Importe, CAST(NULL AS nvarchar(100)) AS Secundario, CAST(NULL AS decimal(19,2)) AS Existencia FROM dbo.Vendedor X WHERE X.Empresa_Id IN (20210705001,20210705003,20210705004) AND (@empresa IS NULL OR X.Empresa_Id=@empresa) AND (@estado='' OR (X.Activo=1 AND @estado='Y') OR (X.Activo=0 AND @estado='N')) AND (X.Nombre LIKE @buscar OR CONVERT(nvarchar(50),X.Vendedor_Id) LIKE @buscar)";
                case "pedidosSql":
                    return "SELECT " + empresa + " AS Empresa, 'SQL' AS Fuente, CONVERT(nvarchar(50),X.Pedido_Id) AS Codigo, X.Nombre, 'Pedido K66' AS Tipo, X.Fecha, CASE WHEN X.Sincronizado=1 THEN 'Sincronizado' ELSE 'Pendiente' END AS Estado, CASE WHEN X.Moneda IN ('QTZ','Q','GTQ') THEN 'GTQ' ELSE X.Moneda END AS Moneda, (SELECT SUM(D.Cantidad*D.Precio) FROM dbo.Pedido_Detalle_K66 D WHERE D.Pedido_Id=X.Pedido_Id) AS Importe, X.ID_K66 AS Secundario, CAST(NULL AS decimal(19,2)) AS Existencia FROM dbo.Pedido_K66 X WHERE X.Empresa_Id IN (20210705001,20210705003,20210705004) AND (@empresa IS NULL OR X.Empresa_Id=@empresa) AND X.Fecha>=@desde AND X.Fecha<@hasta AND (@moneda='' OR CASE WHEN X.Moneda IN ('QTZ','Q','GTQ') THEN 'GTQ' ELSE X.Moneda END=@moneda) AND (@estado='' OR (X.Sincronizado=1 AND @estado='Sincronizado') OR (X.Sincronizado=0 AND @estado='Pendiente')) AND (@cliente='' OR X.ID_K66=@cliente) AND (X.Nombre LIKE @buscar OR X.ID_K66 LIKE @buscar OR CONVERT(nvarchar(50),X.Pedido_Id) LIKE @buscar)";
                default: throw new ArgumentException("Consulta inválida.");
            }
        }

        public PaginaDatos ListarSql(string tipo, Filtro f)
        {
            string parte = ParteSql(tipo);
            using (var cn = AbrirSql())
            {
                int total = Convert.ToInt32(LeerSql(cn, "SELECT COUNT(*) AS Total FROM (" + parte + ") Q", f).Rows[0][0]);
                int primero = (f.Pagina - 1) * f.Tamano + 1;
                string orden = tipo == "pedidosSql" ? "Q.Fecha DESC,Q.Empresa,Q.Codigo DESC" : "Q.Empresa,Q.Nombre,Q.Codigo";
                string sql = "SELECT * FROM (SELECT Q.*,ROW_NUMBER() OVER (ORDER BY " + orden + ") AS Fila FROM (" + parte + ") Q) Z WHERE Z.Fila BETWEEN @primero AND @ultimo ORDER BY Z.Fila";
                return new PaginaDatos { Total = total, Filas = LeerSql(cn, sql, f, primero, primero + f.Tamano - 1) };
            }
        }

        public DataTable ResumenSql(Filtro f)
        {
            const string sql = @"SELECT CASE Empresa_Id WHEN 20210705001 THEN 'BOLIK' WHEN 20210705003 THEN 'FAES' WHEN 20210705004 THEN 'GRACO' END AS Empresa,
                COUNT(*) AS Pedidos, SUM(CASE WHEN Sincronizado=0 THEN 1 ELSE 0 END) AS Pendientes, COUNT(DISTINCT ID_K66) AS Clientes
                FROM dbo.Pedido_K66 WHERE Empresa_Id IN (20210705001,20210705003,20210705004) AND (@empresa IS NULL OR Empresa_Id=@empresa) AND Fecha>=@desde AND Fecha<@hasta
                GROUP BY Empresa_Id";
            using (var cn = AbrirSql()) return LeerSql(cn, sql, f);
        }

        private static OdbcParameter[] ClaveHana(string clave, bool numero)
        {
            if (!numero) return new[] { new OdbcParameter("clave", OdbcType.NVarChar) { Value = clave } };
            int valor;
            if (!int.TryParse(clave, out valor)) throw new ArgumentException("Clave numérica inválida.");
            return new[] { new OdbcParameter("clave", OdbcType.Int) { Value = valor } };
        }

        public Dictionary<string, DataTable> DetalleSap(string tipo, string empresa, string clave)
        {
            string s = Schema(empresa), tabla, llave, secundaria = null, filtroSecundario = null;
            switch (tipo)
            {
                case "clientes": tabla = "OCRD"; llave = "CardCode"; secundaria = "CRD1"; filtroSecundario = "CardCode"; break;
                case "productos": tabla = "OITM"; llave = "ItemCode"; secundaria = "OITW"; filtroSecundario = "ItemCode"; break;
                case "agentes": tabla = "OSLP"; llave = "SlpCode"; break;
                case "pedidos": tabla = "ORDR"; llave = "DocEntry"; secundaria = "RDR1"; filtroSecundario = "DocEntry"; break;
                case "ventas": tabla = "OINV"; llave = "DocEntry"; secundaria = "INV1"; filtroSecundario = "DocEntry"; break;
                case "notas": tabla = "ORIN"; llave = "DocEntry"; secundaria = "RIN1"; filtroSecundario = "DocEntry"; break;
                default: throw new ArgumentException("Detalle inválido.");
            }
            if (clave.Length > 60) throw new ArgumentException("Clave inválida.");
            bool numerica = llave == "DocEntry" || llave == "SlpCode";
            string condicion = "\"" + llave + "\"=?";
            var result = new Dictionary<string, DataTable>();
            if (tipo == "ventas" || tipo == "notas")
            {
                string encabezado = @"SELECT H.""DocNum"" AS ""Documento"", H.""DocDate"" AS ""Fecha"", H.""DocDueDate"" AS ""Vencimiento"", H.""CardCode"" AS ""CodigoCliente"", H.""CardName"" AS ""Cliente"", COALESCE(S.""SlpName"",'—') AS ""Agente"", H.""NumAtCard"" AS ""ReferenciaCliente"", CASE WHEN H.""DocCur"" IN ('QTZ','Q','GTQ') THEN 'GTQ' ELSE H.""DocCur"" END AS ""Moneda"", CASE WHEN H.""DocCur"" IN ('QTZ','Q','GTQ') THEN H.""DocTotal""-H.""VatSum"" ELSE H.""DocTotalFC""-H.""VatSumFC"" END AS ""Subtotal"", CASE WHEN H.""DocCur"" IN ('QTZ','Q','GTQ') THEN H.""VatSum"" ELSE H.""VatSumFC"" END AS ""Impuesto"", CASE WHEN H.""DocCur"" IN ('QTZ','Q','GTQ') THEN H.""DocTotal"" ELSE H.""DocTotalFC"" END AS ""Total"", CASE WHEN H.""CANCELED""='Y' THEN 'Cancelado' WHEN H.""DocStatus""='O' THEN 'Abierto' ELSE 'Cerrado' END AS ""Estado"" FROM " + s + ".\"" + tabla + "\" H LEFT JOIN " + s + ".\"OSLP\" S ON S.\"SlpCode\"=H.\"SlpCode\" WHERE H.\"DocEntry\"=?";
                result["Principal"] = HanaHelper.EjecutarConsulta(encabezado, ClaveHana(clave, true));
                if (result["Principal"].Rows.Count == 0) return result;
                string lineas = @"SELECT L.""ItemCode"" AS ""CodigoProducto"", L.""Dscription"" AS ""Producto"", L.""Quantity"" AS ""Cantidad"", L.""unitMsr"" AS ""Unidad"", L.""Price"" AS ""PrecioUnitario"", CASE WHEN H.""DocCur"" IN ('QTZ','Q','GTQ') THEN L.""LineTotal"" ELSE L.""TotalFrgn"" END AS ""Importe"" FROM " + s + ".\"" + secundaria + "\" L JOIN " + s + ".\"" + tabla + "\" H ON H.\"DocEntry\"=L.\"DocEntry\" WHERE L.\"DocEntry\"=? ORDER BY L.\"LineNum\" LIMIT 100";
                result["Productos"] = HanaHelper.EjecutarConsulta(lineas, ClaveHana(clave, true));
                return result;
            }
            result["Principal"] = HanaHelper.EjecutarConsulta("SELECT * FROM " + s + ".\"" + tabla + "\" WHERE " + condicion, ClaveHana(clave, numerica));
            if (result["Principal"].Rows.Count == 0) return result;
            if (secundaria != null)
            {
                string secCondicion = "\"" + filtroSecundario + "\"=?";
                result["Relacionados"] = HanaHelper.EjecutarConsulta("SELECT * FROM " + s + ".\"" + secundaria + "\" WHERE " + secCondicion + " LIMIT 100", ClaveHana(clave, numerica));
            }
            if (tipo == "clientes") result["Contactos"] = HanaHelper.EjecutarConsulta("SELECT * FROM " + s + ".\"OCPR\" WHERE \"CardCode\"=? LIMIT 100", ClaveHana(clave, false));
            if (tipo == "productos") result["Precios"] = HanaHelper.EjecutarConsulta("SELECT * FROM " + s + ".\"ITM1\" WHERE \"ItemCode\"=? LIMIT 100", ClaveHana(clave, false));
            return result;
        }

        public Dictionary<string, DataTable> DetalleSql(string tipo, string empresa, string clave)
        {
            if (!EmpresaIds.ContainsKey(empresa) || clave.Length > 60) throw new ArgumentException("Clave inválida.");
            string tabla, llave, columnas;
            switch (tipo)
            {
                case "clientesSql": tabla = "Cliente"; llave = "Cliente_Id"; columnas = "Cliente_Id,Empresa_Id,Nit,Nombre,Direccion,No_Telefono,Email_Cliente,Descuento,Vip,Activo,Dias_Credito,Limite_Credito,Nombre_Contacto,Telefono_Contacto,Celular_Contacto,Correo_Contacto,Fecha"; break;
                case "productosSql": tabla = "Producto"; llave = "Producto_Id"; columnas = "Producto_Id,Empresa_Id,Codigo,Nombre,Descripcion,Minimo,Maximo,Cantidad,Tiene_Identificador,Tiene_Envase,Cantidad_Envase,Tiene_Lote,Activo,Fecha"; break;
                case "agentesSql": tabla = "Vendedor"; llave = "Vendedor_Id"; columnas = "Vendedor_Id,Empresa_Id,Nombre,Activo,Fecha"; break;
                case "pedidosSql": tabla = "Pedido_K66"; llave = "Pedido_Id"; columnas = "Pedido_Id,Empresa_Id,Tipo_Pedido_Id,Estado_Id,ID_K66,Nit,Nombre,Direccion,Direccion_Entrega,Orden_Compra_Cliente,Termino_Entrega,Vendedor,Moneda,Sincronizado,Fecha_Hora_Pedido,Fecha_Hora_Sincronizacion,Fecha_Prometida,Fecha"; break;
                default: throw new ArgumentException("Detalle inválido.");
            }
            var tablas = new Dictionary<string, DataTable>();
            using (var cn = AbrirSql())
            {
                using (var cmd = new SqlCommand("SELECT " + columnas + " FROM dbo." + tabla + " WHERE Empresa_Id=@empresa AND " + llave + "=@clave", cn))
                {
                    cmd.Parameters.Add("@empresa", SqlDbType.BigInt).Value = EmpresaIds[empresa];
                    if (tipo == "productosSql") cmd.Parameters.Add("@clave", SqlDbType.NVarChar, 60).Value = clave;
                    else
                    {
                        long id;
                        if (!long.TryParse(clave, out id)) throw new ArgumentException("Clave numérica inválida.");
                        cmd.Parameters.Add("@clave", SqlDbType.BigInt).Value = id;
                    }
                    tablas["Principal"] = new DataTable();
                    using (var adapter = new SqlDataAdapter(cmd)) adapter.Fill(tablas["Principal"]);
                }
                if (tipo == "pedidosSql" && tablas["Principal"].Rows.Count > 0)
                {
                    using (var cmd = new SqlCommand("SELECT TOP (100) Detalle_Id,Pedido_Id,Producto_Id,WarehouseId,Nombre,Unidad,Cantidad,Precio_Original,Precio,Descuento FROM dbo.Pedido_Detalle_K66 WHERE Pedido_Id=@clave ORDER BY Detalle_Id", cn))
                    {
                        cmd.Parameters.Add("@clave", SqlDbType.BigInt).Value = Convert.ToInt64(clave);
                        tablas["Renglones"] = new DataTable();
                        using (var adapter = new SqlDataAdapter(cmd)) adapter.Fill(tablas["Renglones"]);
                    }
                }
            }
            return tablas;
        }
    }
}
