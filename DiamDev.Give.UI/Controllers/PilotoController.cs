using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
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
                try { PilotoRutaBL.ValidarConfiguracionPrueba(); }
                catch (PilotoException e) { context.Result=ErrorPiloto(e); return; }
                ViewBag.PruebasSoloLectura=true;
                if(!PilotoRutaBL.PermiteUsuarioPrueba(User.Identity.Name))
                {
                    context.Result=ErrorPiloto(new PilotoException(403,"La consulta temporal no esta habilitada para tu usuario."));
                    return;
                }
            }
            else if (!string.Equals(Convert.ToString(Session["Pilotos.Autenticado"]), User.Identity.Name, StringComparison.Ordinal))
            {
                ViewBag.ConfigurarPrueba=!PilotoRutaBL.Habilitado;
                ViewBag.SesionVencida=PilotoRutaBL.Habilitado;
                context.Result = ErrorPiloto(new PilotoException(403,
                    PilotoRutaBL.Habilitado
                    ? "La sesión del portal de pilotos no está disponible. Inicia sesión nuevamente; si el problema continúa, solicita la revisión de tu acceso a Distribución."
                    : "Tu sesión POS está iniciada. El acceso al portal de pilotos aún no está habilitado; solicita al administrador que revise la configuración del sitio."));
                return;
            }
            base.OnActionExecuting(context);
        }

        public ActionResult Index(string desde, string hasta, int pagina = 1, string vista = null)
        {
            try
            {
                var hoy = DateTime.Today;
                var inicio = PilotoRutaBL.ConsultaRutaFija ? hoy : Fecha(desde, hoy.AddDays(-13));
                var fin = PilotoRutaBL.ConsultaRutaFija ? hoy : Fecha(hasta, hoy);
                if(PilotoRutaBL.ConsultaRutaFija) pagina=1;
                return View(new PilotoRutaBL().Listar(User.Identity.Name, inicio, fin, pagina,vista));
            }
            catch (Exception e) { return ErrorPiloto(e); }
        }

        public ActionResult Detalle(string id, int pagina = 1, string desde = null, string hasta = null, int paginaRutas = 1, string vista = null)
        {
            try
            {
                if(paginaRutas<1 || paginaRutas>1000) throw new PilotoException(400,"La página de rutas no es válida.");
                ViewBag.Desde=string.IsNullOrEmpty(desde) ? null : Fecha(desde,DateTime.Today).ToString("yyyy-MM-dd");
                ViewBag.Hasta=string.IsNullOrEmpty(hasta) ? null : Fecha(hasta,DateTime.Today).ToString("yyyy-MM-dd");
                ViewBag.PaginaRutas=paginaRutas;
                ViewBag.Vista=PilotoReglas.VistaRutas(vista);
                return View(new PilotoRutaBL().Detalle(User.Identity.Name, id,pagina));
            }
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
            catch (Exception e)
            {
                var validacion=e as PilotoException;
                if(validacion!=null && validacion.StatusCode==400 && cierre!=null && cierre.Documentos!=null && cierre.Documentos.Count<=PilotoReglas.MaxDocumentos)
                {
                    try
                    {
                        var ruta=new PilotoRutaBL().Detalle(User.Identity.Name,cierre.RutaId);
                        if(ruta.PuedeCerrar && ruta.Version==cierre.Version && cierre.Documentos.All(d=>d!=null) &&
                            cierre.Documentos.Select(d=>d.RowId).Distinct().Count()==ruta.Documentos.Count &&
                            cierre.Documentos.Count==ruta.Documentos.Count && cierre.Documentos.All(d=>ruta.Documentos.Any(r=>r.RowId==d.RowId)))
                        {
                            foreach(var d in ruta.Documentos)
                            {
                                var recibido=cierre.Documentos.Single(r=>r.RowId==d.RowId);
                                d.Visito=recibido.Visito; d.Entrega=recibido.Entrega; d.Motivo=recibido.Motivo;
                            }
                            ruta.Solicitud=cierre.Solicitud;
                            ModelState.Clear(); ModelState.AddModelError("",validacion.Message);
                            Response.StatusCode=400; Response.TrySkipIisCustomErrors=true;
                            return View("Detalle",ruta);
                        }
                    }
                    catch { /* El formulario no se reconstruye si ya no se puede autorizar su lectura. */ }
                }
                return ErrorPiloto(e);
            }
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
            var sql = e as SqlException;
            Response.StatusCode = esperado == null ? 503 : esperado.StatusCode;
            Response.TrySkipIisCustomErrors = true;
            ViewBag.Mensaje = esperado != null ? esperado.Message
                : e is ConfigurationErrorsException ? "La configuración de conexión del portal está incompleta. Solicita su revisión al administrador."
                : sql != null && (sql.Number==-2 || sql.Number==1222 || sql.Number==1205) ? "La consulta encontró una espera o un bloqueo. Espera unos segundos y vuelve a consultar la ruta."
                : sql != null && (sql.Number==229 || sql.Number==916 || sql.Number==4060 || sql.Number==18456) ? "El portal no pudo acceder a las bases necesarias. Solicita al administrador que revise la conexión y sus permisos."
                : sql != null && (sql.Number==208 || sql.Number==207) ? "La estructura de datos requerida no está disponible. Solicita al administrador que revise la instalación del portal."
                : "No se pudo completar la operación. Vuelve a consultar la ruta para comprobar su estado.";
            if (esperado == null)
            {
                // No registrar parametros, SQL, credenciales ni resultados comerciales.
                var referencia=Guid.NewGuid().ToString("N");
                ViewBag.Referencia=referencia;
                Trace.TraceError("Pilotos: referencia={0}, tipo={1}, numeroSQL={2}", referencia, e.GetType().Name, sql == null ? 0 : sql.Number);
            }
            return View("Error");
        }
    }
}
