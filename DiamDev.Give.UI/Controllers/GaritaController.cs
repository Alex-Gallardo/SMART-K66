using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Web.Mvc;
using DiamDev.Give.DAL;
using DiamDev.Give.Entities;

namespace DiamDev.Give.UI.Controllers
{
    [Authorize]
    public sealed class GaritaController : Controller
    {
        protected override void OnActionExecuting(ActionExecutingContext context)
        {
            Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            base.OnActionExecuting(context);
        }
        [HttpGet]
        public ActionResult Index(string id)
        {
            try
            {
                if(string.IsNullOrWhiteSpace(id)) return View(new GaritaRutaDA().Preparar(User.Identity.Name));
                return View(new GaritaRutaDA().Buscar(User.Identity.Name,id));
            }
            catch(Exception e) { ViewBag.RutaId=id;return Error(e); }
        }
        [HttpPost,ValidateAntiForgeryToken]
        public ActionResult Confirmar(GaritaConfirmacion modelo)
        {
            try
            {
                if(!ModelState.IsValid) throw new PilotoException(400,"Revisa los datos antes de confirmar.");
                new GaritaRutaDA().Confirmar(User.Identity.Name,modelo);
                TempData["Garita.Mensaje"]="Salida registrada. El piloto ya puede continuar con la ruta.";
                return RedirectToAction("Index",new{id=modelo.RutaId});
            }
            catch(Exception e) { ViewBag.RutaId=modelo==null?null:modelo.RutaId;return Error(e); }
        }
        [HttpPost,ValidateAntiForgeryToken]
        public ActionResult Diferencia(GaritaDiferencia modelo)
        {
            try
            {
                if(!ModelState.IsValid) throw new PilotoException(400,"Revisa la descripción de la diferencia.");
                new GaritaRutaDA().Reportar(User.Identity.Name,modelo);
                TempData["Garita.Mensaje"]="Diferencia registrada. No permitas la salida; contacta a Distribución.";
                return RedirectToAction("Index",new{id=modelo.RutaId});
            }
            catch(Exception e) { ViewBag.RutaId=modelo==null?null:modelo.RutaId;return Error(e); }
        }
        [HttpGet]
        public ActionResult Administrar(string login)
        {
            try {return View(new GaritaRutaDA().Administrar(User.Identity.Name,login));}
            catch(Exception e) {ViewBag.EsAdmin=true;return Error(e);}
        }
        [HttpPost,ValidateAntiForgeryToken]
        public ActionResult Centro(string login,string centro,bool asignar)
        {
            try {new GaritaRutaDA().Centro(User.Identity.Name,login,centro,asignar);TempData["Garita.Mensaje"]="Centro actualizado.";return RedirectToAction("Administrar",new{login});}
            catch(Exception e) {ViewBag.EsAdmin=true;return Error(e);}
        }
        [HttpPost,ValidateAntiForgeryToken]
        public ActionResult ResolverDiferencia(string id,string nota)
        {
            try {new GaritaRutaDA().Resolver(User.Identity.Name,id,nota);TempData["Garita.Mensaje"]="Diferencia resuelta. Garita debe volver a revisar la ruta.";return RedirectToAction("Administrar",new{id});}
            catch(Exception e) {ViewBag.EsAdmin=true;return Error(e);}
        }
        private ActionResult Error(Exception e)
        {
            var esperado=e as PilotoException;var sql=e as SqlException;
            Response.StatusCode=esperado==null?503:esperado.StatusCode;
            Response.TrySkipIisCustomErrors=true;
            ViewBag.Mensaje=esperado!=null?esperado.Message:
                sql!=null && (sql.Number==-2 || sql.Number==1222 || sql.Number==1205)?"La ruta está ocupada. Espera unos segundos y búscala otra vez.":
                "No se pudo completar la operación. Busca de nuevo la ruta o contacta a Distribución.";
            if(esperado==null) {var referencia=Guid.NewGuid().ToString("N");ViewBag.Referencia=referencia;Trace.TraceError("Garita referencia={0}, tipo={1}, numeroSQL={2}",referencia,e.GetType().Name,sql==null?0:sql.Number);}
            return View("Error");
        }
    }
}
