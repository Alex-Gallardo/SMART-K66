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
        public JsonResult GetData(string from, string to)
        {
            var rows = new List<Dictionary<string, object>>();

            // Leemos la conexión del Web.config
            var connString = ConfigurationManager.ConnectionStrings["HanaOdbc"].ConnectionString;

            // Buscamos el archivo SQL en la raíz del proyecto
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
                                var value = reader.IsDBNull(i) ? null : reader.GetValue(i);

                                row[name] = value is DateTime dt ? dt.ToString("yyyy-MM-dd") : value;
                            }
                            rows.Add(row);
                        }
                    }
                }
            }

            // En MVC clásico, es obligatorio permitir peticiones GET para JSON
            return Json(new { rows = rows }, JsonRequestBehavior.AllowGet);
        }
    }
}