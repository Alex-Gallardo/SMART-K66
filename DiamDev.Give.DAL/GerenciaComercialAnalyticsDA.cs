using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Linq;

namespace DiamDev.Give.DAL
{
    public sealed partial class GerenciaComercialDA
    {
        private static readonly string[] AnalyticsVistas = { "general", "agentes", "clientes", "productos", "producto-agente", "tendencia", "conteos", "clientes-ranking" };

        public DataTable Analytics(Filtro f, string vista, string agente = "", string cliente = "", string codigo = "", string producto = "", string orden = "current", bool descendente = true, bool origen = false)
        {
            if (!AnalyticsVistas.Contains(vista)) throw new ArgumentException("Vista inválida.");
            var movimientos = new List<string>();
            foreach (string empresa in SeleccionarEmpresas(f.Empresa))
            foreach (bool credito in new[] { false, true })
            {
                string s = Schema(empresa), h = credito ? "ORIN" : "OINV", l = credito ? "RIN1" : "INV1", signo = credito ? "-1" : "1";
                movimientos.Add("SELECT '" + empresa + "' AS \"company\", TO_NVARCHAR(COALESCE(H.\"SlpCode\",-1)) AS \"agentCode\", '" + empresa + "|' || TO_NVARCHAR(COALESCE(H.\"SlpCode\",-1)) AS \"agentKey\", COALESCE(S.\"SlpName\",'Sin agente') AS \"agent\", H.\"CardCode\" AS \"clientCode\", H.\"CardName\" AS \"clientName\", L.\"ItemCode\" AS \"code\", COALESCE(I.\"ItemName\",L.\"Dscription\") AS \"name\", COALESCE(NULLIF(TRIM(L.\"unitMsr\"),''),'Sin unidad') AS \"unit\", CASE WHEN H.\"CardCode\" LIKE 'CL%' THEN 'LOCAL' ELSE 'EXPORTACION' END AS \"type\", H.\"DocDate\" AS \"date\", " + signo + "*CASE WHEN P.\"Currency\"='USD' THEN L.\"TotalFrgn\" ELSE L.\"LineTotal\" END AS \"amount\", " + signo + "*L.\"Quantity\" AS \"qty\" FROM " + s + ".\"" + h + "\" H JOIN " + s + ".\"" + l + "\" L ON H.\"DocEntry\"=L.\"DocEntry\" LEFT JOIN " + s + ".\"OSLP\" S ON H.\"SlpCode\"=S.\"SlpCode\" LEFT JOIN " + s + ".\"OITM\" I ON L.\"ItemCode\"=I.\"ItemCode\" CROSS JOIN P WHERE H.\"CANCELED\"='N' AND H.\"DocType\"='I' AND COALESCE(L.\"ItemCode\",'')<>'' AND (H.\"DocDate\" BETWEEN P.\"From\" AND P.\"To\" OR H.\"DocDate\" BETWEEN P.\"BeforeFrom\" AND P.\"BeforeTo\") AND CASE WHEN H.\"DocCur\" IN ('Q','QTZ','GTQ') THEN 'GTQ' ELSE H.\"DocCur\" END=P.\"Currency\"" + (origen ? " AND H.\"CardCode\"<>'CL0149' AND COALESCE(S.\"SlpName\",'') NOT IN ('OTROS','EMPLEADOS')" : ""));
            }
            string cte = "WITH P AS (SELECT CAST(? AS DATE) AS \"From\", CAST(? AS DATE) AS \"To\", CAST(? AS DATE) AS \"BeforeFrom\", CAST(? AS DATE) AS \"BeforeTo\", CAST(? AS NVARCHAR(3)) AS \"Currency\" FROM DUMMY), M AS (" + string.Join(" UNION ALL ", movimientos) + "), V AS (SELECT M.*, CASE WHEN M.\"date\" BETWEEN P.\"From\" AND P.\"To\" THEN M.\"amount\" ELSE 0 END AS \"current\", CASE WHEN M.\"date\" BETWEEN P.\"BeforeFrom\" AND P.\"BeforeTo\" THEN M.\"amount\" ELSE 0 END AS \"previous\", CASE WHEN M.\"date\" BETWEEN P.\"From\" AND P.\"To\" THEN M.\"qty\" ELSE 0 END AS \"qtyCurrent\", CASE WHEN M.\"date\" BETWEEN P.\"BeforeFrom\" AND P.\"BeforeTo\" THEN M.\"qty\" ELSE 0 END AS \"qtyPrevious\", MONTH(CASE WHEN M.\"date\" BETWEEN P.\"From\" AND P.\"To\" THEN M.\"date\" ELSE ADD_YEARS(M.\"date\",1) END) AS \"month\", TO_VARCHAR(CASE WHEN M.\"date\" BETWEEN P.\"From\" AND P.\"To\" THEN M.\"date\" ELSE ADD_YEARS(M.\"date\",1) END,'YYYY-MM') AS \"monthKey\" FROM M CROSS JOIN P WHERE (?='' OR M.\"agentKey\"=?))";
            string dims;
            switch (vista) {
                case "general": dims = "\"company\",\"type\""; break;
                case "agentes": dims = "\"company\",\"agentKey\",\"agentCode\",\"agent\""; break;
                case "clientes-ranking": dims = "\"company\",\"clientCode\",\"agentKey\",\"agent\""; break;
                case "clientes": dims = "\"company\",\"clientCode\",\"agentKey\",\"agent\",\"unit\""; break;
                case "productos": dims = "\"company\",\"code\",\"name\",\"unit\""; break;
                case "producto-agente": dims = "\"company\",\"code\",\"name\",\"agentKey\",\"agent\",\"unit\""; break;
                case "tendencia": dims = "\"company\",\"monthKey\",\"month\""; break;
                default: dims = "\"company\""; break;
            }
            string select = vista == "clientes-ranking" ? "\"company\",\"clientCode\" AS \"code\",MAX(\"clientName\") AS \"name\",\"agentKey\",\"agent\"" : vista == "clientes" ? "\"company\",\"clientCode\" AS \"code\",MAX(\"clientName\") AS \"name\",\"agentKey\",\"agent\",\"unit\"" : dims;
            string agregado = vista == "conteos" ? "SELECT \"company\",COUNT(DISTINCT CASE WHEN \"current\"<>0 THEN \"clientCode\" END) AS \"clients\",COUNT(DISTINCT CASE WHEN \"current\"<>0 THEN \"code\" END) AS \"products\" FROM V GROUP BY \"company\"" :
                "SELECT " + select + ", SUM(\"current\") AS \"current\", SUM(\"previous\") AS \"previous\", SUM(\"qtyCurrent\") AS \"qtyCurrent\", SUM(\"qtyPrevious\") AS \"qtyPrevious\" FROM V GROUP BY " + dims;
            var ps = new List<OdbcParameter> {
                new OdbcParameter("from",OdbcType.Date){Value=f.Desde.Date}, new OdbcParameter("to",OdbcType.Date){Value=f.Hasta.Date},
                new OdbcParameter("beforeFrom",OdbcType.Date){Value=f.Desde.AddYears(-1).Date}, new OdbcParameter("beforeTo",OdbcType.Date){Value=f.Hasta.AddYears(-1).Date},
                new OdbcParameter("currency",OdbcType.NVarChar){Value=f.Moneda}, new OdbcParameter("agent",OdbcType.NVarChar){Value=agente ?? ""}, new OdbcParameter("agent2",OdbcType.NVarChar){Value=agente ?? ""}
            };
            if (vista == "conteos") return HanaHelper.EjecutarConsulta(cte + " " + agregado, ps.ToArray());
            string campoNombre = new[] { "clientes","clientes-ranking","productos","producto-agente" }.Contains(vista) ? "G.\"name\"" : vista == "agentes" ? "G.\"agent\"" : "G.\"company\"";
            string campoCodigo = new[] { "clientes","clientes-ranking","productos","producto-agente" }.Contains(vista) ? "G.\"code\"" : "G.\"company\"";
            string buscar = (f.Buscar ?? "").ToUpperInvariant();
            ps.Add(new OdbcParameter("search",OdbcType.NVarChar){Value="%" + buscar.Replace("%","\\%").Replace("_","\\_") + "%"});
            ps.Add(new OdbcParameter("client",OdbcType.NVarChar){Value="%" + (cliente ?? "").ToUpperInvariant().Replace("%","\\%").Replace("_","\\_") + "%"});
            ps.Add(new OdbcParameter("code",OdbcType.NVarChar){Value="%" + (codigo ?? "").ToUpperInvariant().Replace("%","\\%").Replace("_","\\_") + "%"});
            ps.Add(new OdbcParameter("product",OdbcType.NVarChar){Value="%" + (producto ?? "").ToUpperInvariant().Replace("%","\\%").Replace("_","\\_") + "%"});
            string orderColumn = new[] { "current","previous","qtyCurrent","qtyPrevious","company","diff","growth","participation","name","code","agent","unit","type","month","rank","indicator" }.Contains(orden) ? orden : "current";
            // Los identificadores de orden se limitan también a columnas presentes en cada dimensión.
            if (!new[] {"current","previous","qtyCurrent","qtyPrevious","diff","growth","participation","rank","indicator","company"}.Contains(orderColumn) && !select.Contains("\"" + orderColumn + "\"")) orderColumn = "current";
            string estable = vista=="general" ? "\"company\",\"type\"" : vista=="agentes" ? "\"company\",\"agentKey\"" : vista=="clientes-ranking" ? "\"company\",\"code\",\"agentKey\"" : vista=="clientes" ? "\"company\",\"code\",\"agentKey\",\"unit\"" : vista=="productos" ? "\"company\",\"code\",\"name\",\"unit\"" : vista=="producto-agente" ? "\"company\",\"code\",\"name\",\"agentKey\",\"unit\"" : "\"company\",\"monthKey\",\"month\"";
            string sql = cte + ", G0 AS (" + agregado + "), G AS (SELECT G0.*, \"current\"-\"previous\" AS \"diff\", CASE WHEN \"previous\"=0 THEN NULL ELSE 100*(\"current\"-\"previous\")/ABS(\"previous\") END AS \"growth\", CASE WHEN SUM(\"current\") OVER(PARTITION BY \"company\")=0 THEN NULL ELSE 100*\"current\"/SUM(\"current\") OVER(PARTITION BY \"company\") END AS \"participation\", CASE WHEN \"current\">\"previous\" THEN 'SUBE' WHEN \"current\"<\"previous\" THEN 'BAJA' ELSE 'IGUAL' END AS \"indicator\", ROW_NUMBER() OVER(ORDER BY \"current\" DESC," + estable + ") AS \"rank\" FROM G0), R AS (SELECT G.*,COUNT(*) OVER() AS \"total\",ROW_NUMBER() OVER(ORDER BY G.\"" + orderColumn + "\" " + (descendente ? "DESC" : "ASC") + ",G.\"rank\") AS \"rowNumber\" FROM G WHERE UPPER(COALESCE(" + campoNombre + ",'' ) || ' ' || COALESCE(" + campoCodigo + ",'')) LIKE ? ESCAPE '\\' AND UPPER(COALESCE(" + campoNombre + ",'') || ' ' || COALESCE(" + campoCodigo + ",'')) LIKE ? ESCAPE '\\' AND UPPER(COALESCE(" + campoCodigo + ",'')) LIKE ? ESCAPE '\\' AND UPPER(COALESCE(" + campoNombre + ",'')) LIKE ? ESCAPE '\\') SELECT * FROM R WHERE \"rowNumber\">? AND \"rowNumber\"<=? ORDER BY \"rowNumber\"";
            ps.Add(new OdbcParameter("offset",OdbcType.Int){Value=(f.Pagina-1)*f.Tamano});
            ps.Add(new OdbcParameter("end",OdbcType.Int){Value=f.Pagina*f.Tamano});
            return HanaHelper.EjecutarConsulta(sql, ps.ToArray());
        }
    }
}
