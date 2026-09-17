using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Web.Mvc;
using DiamDev.Give.BLL;
using DiamDev.Give.Entities;

namespace DiamDev.Give.UI.Controllers
{
    [Authorize]
    public sealed class PilotoController : Controller
    {
        protected override void OnActionExecuting(ActionExecutingContext context)
        {
            Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            if (PilotoRutaBL.PruebasSoloLectura)
            {
                ViewBag.PruebasSoloLectura=true;
                if(!PilotoRutaBL.PermiteUsuarioPrueba(User.Identity.Name))
                {
                    context.Result=ErrorPiloto(new PilotoException(403,"La consulta temporal no esta habilitada para tu usuario."));
                    return;
                }
            }
            else if (!string.Equals(Convert.ToString(Session["Pilotos.Autenticado"]), User.Identity.Name, StringComparison.Ordinal))
            {
                context.Result = RedirectToAction("Login", "Seguridad", new { returnUrl = Url.Action("Index", "Piloto") });
                return;
            }
            base.OnActionExecuting(context);
        }

        public ActionResult Index(string desde, string hasta, int pagina = 1)
        {
            try
            {
                var hoy = DateTime.Today;
                var inicio = Fecha(desde, hoy.AddDays(-7)); var fin = Fecha(hasta, hoy);
                return View(new PilotoRutaBL().Listar(User.Identity.Name, inicio, fin, pagina));
            }
            catch (Exception e) { return ErrorPiloto(e); }
        }

        public ActionResult Detalle(string id)
        {
            try { return View(new PilotoRutaBL().Detalle(User.Identity.Name, id)); }
            catch (Exception e) { return ErrorPiloto(e); }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Completar(PilotoCierre cierre)
        {
            try
            {
                if (!ModelState.IsValid) throw new PilotoException(400, "Revisa los resultados de los documentos.");
                new PilotoRutaBL().Cerrar(User.Identity.Name, cierre);
                TempData["Piloto.Mensaje"] = "Ruta completada. Se guardaron los resultados de sus documentos.";
                return RedirectToAction("Detalle", new { id = cierre.RutaId });
            }
            catch (Exception e) { return ErrorPiloto(e); }
        }

        private static DateTime Fecha(string value, DateTime defecto)
        {
            if (string.IsNullOrWhiteSpace(value)) return defecto;
            DateTime result;
            if (!DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                throw new PilotoException(400, "La fecha debe tener formato yyyy-MM-dd.");
            return result;
        }

        private ActionResult ErrorPiloto(Exception e)
        {
            var esperado = e as PilotoException;
            Response.StatusCode = esperado == null ? 503 : esperado.StatusCode;
            Response.TrySkipIisCustomErrors = true;
            ViewBag.Mensaje = esperado == null ? "No se pudo completar la operacion. Recarga la ruta para verificar su estado antes de reintentar." : esperado.Message;
            if (esperado == null)
            {
                // No registrar parametros, SQL, credenciales ni resultados comerciales.
                var sql = e as SqlException;
                Trace.TraceError("Pilotos: tipo={0}, numeroSQL={1}", e.GetType().Name, sql == null ? 0 : sql.Number);
            }
            return View("Error");
        }
    }
}
