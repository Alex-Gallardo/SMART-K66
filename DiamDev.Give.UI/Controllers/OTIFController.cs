using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Odbc;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DiamDev.Give.BLL;
using DiamDev.Give.Entities;
using DiamDev.Give.UI.App_Start;

namespace DiamDev.Give.UI.Controllers
{
    [Authorize]
    public class OTIFController : Controller
    {
        private const int MaxRangeDays = 1827;
        private const int CommandTimeoutSeconds = 120;
        private const int MaxJsonLengthBytes = 100 * 1024 * 1024;
        private const string GlobalPermission = "Control.OTIF.VerTodos";
        private const string ScopeMarker = "/* OTIF_SCOPE_FILTER */";
        private readonly UsuarioEmpresaBL _usuarioEmpresa = new UsuarioEmpresaBL();

        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public JsonResult GetScope()
        {
            try
            {
                var global = CustomHelper.Permiso(GlobalPermission);
                var assignedCompanies = global
                    ? new HashSet<long>()
                    : new HashSet<long>(_usuarioEmpresa.ObtenerPorUsuarioId(CustomHelper.getUserId())
                        .Where(r => !string.IsNullOrWhiteSpace(r.Codigo))
                        .Select(r => r.EmpresaId));

                var companies = new List<object>();
                if (global || assignedCompanies.Contains(UsuarioEmpresaBL.ID_GRACO))
                    companies.Add(new { code = "GRACO", name = "Graco" });
                if (global || assignedCompanies.Contains(UsuarioEmpresaBL.ID_BOLIK))
                    companies.Add(new { code = "BOLIK", name = "Bolik" });
                if (global || assignedCompanies.Contains(UsuarioEmpresaBL.ID_FAES))
                    companies.Add(new { code = "ESCOCESA", name = "Escocesa" });

                NoCache();
                return Json(new { global, empresas = companies }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Trace.TraceError("OTIF.GetScope falló: {0}", ex);
                return JsonError(500, "No fue posible determinar el acceso a OTIF. Contacta a IT.");
            }
        }

        [HttpGet]
        public JsonResult GetData(string from, string to, string empresa)
        {
            DateTime fromDate;
            DateTime toDate;
            string companySchema;
            long companyId;

            if (!TryParseDateRange(from, to, out fromDate, out toDate))
            {
                return JsonError(400,
                    "Las fechas deben usar yyyy-MM-dd, el inicio no puede ser posterior al fin y el rango máximo es de cinco años.");
            }

            if (!TryGetCompany(empresa, out companyId, out companySchema))
            {
                return JsonError(400, "La empresa indicada no es válida.");
            }

            try
            {
                var global = CustomHelper.Permiso(GlobalPermission);
                var assignments = global
                    ? new List<UsuarioEmpresa>()
                    : _usuarioEmpresa.ObtenerPorUsuarioId(CustomHelper.getUserId())
                        .Where(r => r.EmpresaId == companyId && !string.IsNullOrWhiteSpace(r.Codigo))
                        .ToList();
                if (!global && assignments.Count == 0)
                    return JsonError(403, "No tienes acceso a los datos de esta empresa en OTIF.");

                var setting = ConfigurationManager.ConnectionStrings["HanaOdbc"];
                if (setting == null || string.IsNullOrWhiteSpace(setting.ConnectionString))
                {
                    throw new ConfigurationErrorsException(
                        "No se encontró la cadena de conexión HanaOdbc.");
                }

                var connectionBuilder = new OdbcConnectionStringBuilder(setting.ConnectionString);
                connectionBuilder["CS"] = companySchema;

                var sqlPath = Server.MapPath("~/App_Data/otif.sql");
                if (string.IsNullOrWhiteSpace(sqlPath) || !System.IO.File.Exists(sqlPath))
                {
                    throw new FileNotFoundException(
                        "No se encontró la consulta del dashboard OTIF.", sqlPath);
                }

                List<Dictionary<string, object>> rows;
                using (var connection = new OdbcConnection(connectionBuilder.ConnectionString))
                {
                    connection.Open();
                    var agentCodes = global
                        ? new List<int>()
                        : ResolveAgentCodes(connection, assignments);
                    if (!global && agentCodes.Count == 0)
                        return JsonError(403, "Tus asignaciones no corresponden a vendedores SAP de esta empresa.");

                    var sql = ApplyScope(System.IO.File.ReadAllText(sqlPath), global, agentCodes.Count);
                    rows = ExecuteQuery(connection, sql, fromDate, toDate, agentCodes);
                }

                NoCache();

                var result = Json(new { rows = rows }, JsonRequestBehavior.AllowGet);
                result.MaxJsonLength = MaxJsonLengthBytes;
                return result;
            }
            catch (Exception ex)
            {
                Trace.TraceError(
                    "OTIF.GetData falló para empresa {0}, rango {1:yyyy-MM-dd}..{2:yyyy-MM-dd}: {3}",
                    empresa, fromDate, toDate, ex);
                return JsonError(500,
                    "No fue posible consultar la información OTIF. Intenta nuevamente o contacta a IT.");
            }
        }

        private static List<int> ResolveAgentCodes(OdbcConnection connection,
            IEnumerable<UsuarioEmpresa> assignments)
        {
            var availableCodes = new HashSet<int>();
            var codesByName = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
            using (var command = new OdbcCommand("SELECT \"SlpCode\", \"SlpName\" FROM OSLP", connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var code = Convert.ToInt32(reader.GetValue(0), CultureInfo.InvariantCulture);
                    availableCodes.Add(code);
                    var name = reader.IsDBNull(1) ? "" : reader.GetString(1).Trim();
                    if (name.Length == 0) continue;
                    List<int> matches;
                    if (!codesByName.TryGetValue(name, out matches))
                    {
                        matches = new List<int>();
                        codesByName.Add(name, matches);
                    }
                    matches.Add(code);
                }
            }

            var parser = new UsuarioEmpresaBL();
            var authorized = new HashSet<int>();
            foreach (var assignment in assignments)
            {
                var rawCode = (assignment.Codigo ?? "").Trim();
                var parsed = parser.ParseCodigo(assignment.Codigo);
                int code;
                if (!string.IsNullOrWhiteSpace(parsed.SapId))
                {
                    var numeric = int.TryParse(parsed.SapId, NumberStyles.None,
                        CultureInfo.InvariantCulture, out code);
                    if (numeric && availableCodes.Contains(code))
                        authorized.Add(code);
                    if (numeric) continue;
                }
                else if (int.TryParse(rawCode, NumberStyles.None,
                        CultureInfo.InvariantCulture, out code))
                {
                    if (availableCodes.Contains(code)) authorized.Add(code);
                    continue;
                }

                List<int> matchesByName;
                var name = (parsed.SapId.Length > 0 ? rawCode : parsed.AgenteNombre ?? "").Trim();
                if (name.Length > 0 && codesByName.TryGetValue(name, out matchesByName) &&
                    matchesByName.Count == 1)
                    authorized.Add(matchesByName[0]);
            }
            return authorized.OrderBy(x => x).ToList();
        }

        private static string ApplyScope(string sql, bool global, int agentCount)
        {
            var markerAt = sql.IndexOf(ScopeMarker, StringComparison.Ordinal);
            if (markerAt < 0 || sql.IndexOf(ScopeMarker, markerAt + ScopeMarker.Length,
                    StringComparison.Ordinal) >= 0)
                throw new InvalidOperationException("La consulta OTIF no contiene un único filtro de alcance.");
            if (!global && agentCount <= 0)
                throw new InvalidOperationException("No hay vendedores autorizados para la consulta.");

            var clause = global ? "AND 1 = 1" :
                "AND o.\"SlpCode\" IN (" + string.Join(", ", Enumerable.Repeat("?", agentCount)) + ")";
            return sql.Replace(ScopeMarker, clause);
        }

        private static List<Dictionary<string, object>> ExecuteQuery(
            OdbcConnection connection, string sql, DateTime fromDate, DateTime toDate,
            IEnumerable<int> agentCodes)
        {
            var rows = new List<Dictionary<string, object>>();
            using (var command = new OdbcCommand(sql, connection))
            {
                command.CommandTimeout = CommandTimeoutSeconds;
                // ODBC vincula los parámetros por posición, en el orden del SQL.
                command.Parameters.Add("@p1", OdbcType.VarChar, 10).Value =
                    fromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                command.Parameters.Add("@p2", OdbcType.VarChar, 10).Value =
                    toDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                var agentIndex = 0;
                foreach (var code in agentCodes)
                    command.Parameters.Add("@agent" + agentIndex++, OdbcType.Int).Value = code;

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var row = new Dictionary<string, object>(
                            StringComparer.OrdinalIgnoreCase);
                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            row[reader.GetName(i)] = value is DateTime
                                ? ((DateTime)value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                                : value;
                        }
                        rows.Add(row);
                    }
                }
            }

            return rows;
        }

        private static bool TryParseDateRange(string from, string to,
            out DateTime fromDate, out DateTime toDate)
        {
            var validFrom = DateTime.TryParseExact(from, "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out fromDate);
            var validTo = DateTime.TryParseExact(to, "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out toDate);
            return validFrom && validTo && fromDate <= toDate &&
                   (toDate - fromDate).TotalDays < MaxRangeDays;
        }

        private static bool TryGetCompany(string empresa, out long companyId,
            out string companySchema)
        {
            switch ((empresa ?? string.Empty).Trim().ToUpperInvariant())
            {
                case "GRACO":
                    companyId = UsuarioEmpresaBL.ID_GRACO;
                    companySchema = "SBO_GRACO";
                    return true;
                case "BOLIK":
                    companyId = UsuarioEmpresaBL.ID_BOLIK;
                    companySchema = "SBOBOLIK";
                    return true;
                case "ESCOCESA":
                    companyId = UsuarioEmpresaBL.ID_FAES;
                    companySchema = "SBOESCOCESA";
                    return true;
                default:
                    companyId = 0;
                    companySchema = null;
                    return false;
            }
        }

        private void NoCache()
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
        }

        private JsonResult JsonError(int statusCode, string message)
        {
            Response.StatusCode = statusCode;
            Response.TrySkipIisCustomErrors = true;
            NoCache();
            return Json(new { error = message }, JsonRequestBehavior.AllowGet);
        }
    }
}
