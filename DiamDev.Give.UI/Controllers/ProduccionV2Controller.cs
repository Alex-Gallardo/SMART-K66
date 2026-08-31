using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Odbc;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Web;
using System.Web.Mvc;

namespace DiamDev.Give.UI.Controllers
{
    [Authorize]
    public class ProduccionV2Controller : Controller
    {
        private const int MaxRangeDays = 366;
        private const int CommandTimeoutSeconds = 120;
        private const int MaxJsonLengthBytes = 50 * 1024 * 1024;

        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public JsonResult GetData(string from, string to, string empresa)
        {
            DateTime fromDate;
            DateTime toDate;
            string companySchema;

            if (!TryParseDateRange(from, to, out fromDate, out toDate))
            {
                return JsonError(
                    400,
                    "Las fechas deben usar el formato yyyy-MM-dd, el inicio no puede ser posterior al fin y el rango máximo es de 366 días.");
            }

            if (!TryGetCompanySchema(empresa, out companySchema))
            {
                return JsonError(400, "La empresa indicada no es válida.");
            }

            try
            {
                var connectionSetting = ConfigurationManager.ConnectionStrings["HanaOdbc"];
                if (connectionSetting == null || string.IsNullOrWhiteSpace(connectionSetting.ConnectionString))
                {
                    throw new ConfigurationErrorsException(
                        "No se encontró la cadena de conexión HanaOdbc.");
                }

                var connectionBuilder = new OdbcConnectionStringBuilder(
                    connectionSetting.ConnectionString);
                connectionBuilder["CS"] = companySchema;

                var sqlPath = Server.MapPath("~/App_Data/dashboard_v2.sql");
                if (string.IsNullOrWhiteSpace(sqlPath) || !System.IO.File.Exists(sqlPath))
                {
                    throw new FileNotFoundException(
                        "No se encontró la consulta del Dashboard de Producción v2.",
                        sqlPath);
                }

                var rows = ExecuteQuery(
                    connectionBuilder.ConnectionString,
                    System.IO.File.ReadAllText(sqlPath),
                    fromDate,
                    toDate);

                Response.Cache.SetCacheability(HttpCacheability.NoCache);
                Response.Cache.SetNoStore();

                var result = Json(
                    new { rows = rows },
                    JsonRequestBehavior.AllowGet);
                result.MaxJsonLength = MaxJsonLengthBytes;
                return result;
            }
            catch (Exception ex)
            {
                Trace.TraceError(
                    "ProduccionV2.GetData falló para empresa {0}, rango {1:yyyy-MM-dd}..{2:yyyy-MM-dd}: {3}",
                    empresa,
                    fromDate,
                    toDate,
                    ex);

                return JsonError(
                    500,
                    "No fue posible consultar la información de producción. Intenta nuevamente o contacta a IT.");
            }
        }

        private static List<Dictionary<string, object>> ExecuteQuery(
            string connectionString,
            string sql,
            DateTime fromDate,
            DateTime toDate)
        {
            var rows = new List<Dictionary<string, object>>();

            using (var connection = new OdbcConnection(connectionString))
            {
                connection.Open();

                using (var command = new OdbcCommand(sql, connection))
                {
                    command.CommandTimeout = CommandTimeoutSeconds;

                    // ODBC usa parámetros posicionales; el orden debe coincidir
                    // con los dos signos ? de dashboard_v2.sql.
                    command.Parameters.Add("@p1", OdbcType.VarChar, 10).Value =
                        fromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    command.Parameters.Add("@p2", OdbcType.VarChar, 10).Value =
                        toDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var row = new Dictionary<string, object>(
                                StringComparer.OrdinalIgnoreCase);

                            for (var i = 0; i < reader.FieldCount; i++)
                            {
                                var value = reader.IsDBNull(i)
                                    ? null
                                    : reader.GetValue(i);

                                row[reader.GetName(i)] = value is DateTime
                                    ? ((DateTime)value).ToString(
                                        "yyyy-MM-dd",
                                        CultureInfo.InvariantCulture)
                                    : value;
                            }

                            rows.Add(row);
                        }
                    }
                }
            }

            return rows;
        }

        private static bool TryParseDateRange(
            string from,
            string to,
            out DateTime fromDate,
            out DateTime toDate)
        {
            var validFrom = DateTime.TryParseExact(
                from,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out fromDate);
            var validTo = DateTime.TryParseExact(
                to,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out toDate);

            if (!validFrom || !validTo || fromDate > toDate)
            {
                return false;
            }

            return (toDate - fromDate).TotalDays < MaxRangeDays;
        }

        private static bool TryGetCompanySchema(
            string empresa,
            out string companySchema)
        {
            switch ((empresa ?? string.Empty).Trim().ToUpperInvariant())
            {
                case "GRACO":
                    companySchema = "SBO_GRACO";
                    return true;

                case "BOLIK":
                    companySchema = "SBOBOLIK";
                    return true;

                case "ESCOCESA":
                    companySchema = "SBOESCOCESA";
                    return true;

                default:
                    companySchema = null;
                    return false;
            }
        }

        private JsonResult JsonError(int statusCode, string message)
        {
            Response.StatusCode = statusCode;
            Response.TrySkipIisCustomErrors = true;
            return Json(
                new { error = message },
                JsonRequestBehavior.AllowGet);
        }
    }
}
