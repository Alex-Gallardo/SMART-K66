using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Odbc;
using System.IO;
using System.Web.Mvc;

namespace DiamDev.Give.UI.Controllers
{
    public class DashboardController : Controller
    {
        // 1. Esto devuelve la vista visual (HTML)
        public ActionResult Index()
        {
            return View();
        }

        // 2. Esta es tu nueva "API" que devuelve el JSON
        [HttpGet]
        public JsonResult GetData(string from, string to, string empresa)
        {
            var rows = new List<Dictionary<string, object>>();

            // Connection String base
            var baseConnString = ConfigurationManager
                .ConnectionStrings["HanaOdbc"]
                .ConnectionString;

            // Obtener schema según empresa
            var companySchema = ObtenerCompanySchema(empresa);

            // Reemplazar CS dinámicamente
            var connString = baseConnString.Replace(
                "CS=SBO_GRACO",
                $"CS={companySchema}"
            );

            // SQL
            var sqlPath = Server.MapPath("~/dashboard.sql");
            var sql = System.IO.File.ReadAllText(sqlPath);

            using (var conn = new OdbcConnection(connString))
            {
                conn.Open();

                using (var cmd = new OdbcCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@p1", from);
                    cmd.Parameters.AddWithValue("@p2", to);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var row = new Dictionary<string, object>();

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                var name = reader.GetName(i);
                                var value = reader.IsDBNull(i)
                                    ? null
                                    : reader.GetValue(i);

                                row[name] = value is DateTime dt
                                    ? dt.ToString("yyyy-MM-dd")
                                    : value;
                            }

                            rows.Add(row);
                        }
                    }
                }
            }

            return Json(new { rows = rows }, JsonRequestBehavior.AllowGet);
        }



        private string ObtenerCompanySchema(string empresa)
        {
            switch ((empresa ?? "").ToUpper())
            {
                case "BOLIK":
                    return "SBOBOLIK";

                case "GRACO":
                    return "SBO_GRACO";

                case "ESCOCESA":
                    return "SBOESCOCESA";

                default:
                    return "SBO_GRACO";
            }
        }
    }
}