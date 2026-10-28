using System;
using System.Diagnostics;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using DiamDev.Give.BLL;
using DiamDev.Give.Entities;

namespace DiamDev.Give.UI.Controllers
{
    [Authorize]
    public sealed class LiquidacionController : Controller
    {
        protected override void OnActionExecuting(ActionExecutingContext context)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            ViewBag.PilotoBackoffice=true;
            base.OnActionExecuting(context);
        }
        private ActionResult Error(Exception e)
        {
            var conocido=e as PilotoException;
            if(conocido==null) Trace.TraceError("Liquidación: {0}",e);
            Response.StatusCode=conocido==null?503:conocido.StatusCode;
            Response.TrySkipIisCustomErrors=true;
            ViewBag.Mensaje=conocido==null?"No se pudo completar la operación. Intenta de nuevo o informa a Distribución.":conocido.Message;
            return View("Error");
        }
        private static RouteValueDictionary Retorno(PilotoLiquidacionFiltro filtro,string id,bool verLiquidadas=false)
        {
            filtro=filtro??new PilotoLiquidacionFiltro();filtro.Validar(DateTime.UtcNow.AddHours(-6).Date);
            return new RouteValueDictionary(new{id, filtro.Desde,filtro.Hasta,filtro.Buscar,VerLiquidadas=verLiquidadas||filtro.VerLiquidadas,filtro.Pagina});
        }
        [HttpGet]
        public ActionResult Index(PilotoLiquidacionFiltro filtro)
        {
            try {if(!ModelState.IsValid) throw new PilotoException(400,"Revisa los filtros.");return View(new PilotoLiquidacionBL().Listar(User.Identity.Name,filtro));}
            catch(Exception e) {return Error(e);}
        }
        [HttpGet]
        public ActionResult Detalle(string id,PilotoLiquidacionFiltro filtro)
        {
            try {if(!ModelState.IsValid) throw new PilotoException(400,"Revisa los filtros.");filtro=filtro??new PilotoLiquidacionFiltro();filtro.Validar(DateTime.UtcNow.AddHours(-6).Date);ViewBag.Filtro=filtro;return View(new PilotoLiquidacionBL().Detalle(User.Identity.Name,id));}
            catch(Exception e) {return Error(e);}
        }
        [HttpPost,ValidateAntiForgeryToken]
        public ActionResult LiquidarDocumento(string id,int rowId,PilotoLiquidacionFiltro filtro)
        {
            try {var retorno=Retorno(filtro,id);new PilotoLiquidacionBL().LiquidarDocumentos(User.Identity.Name,id,rowId);TempData["Liquidacion.Mensaje"]="Documento liquidado correctamente.";return RedirectToAction("Detalle",retorno);}
            catch(Exception e) {return Error(e);}
        }
        [HttpPost,ValidateAntiForgeryToken]
        public ActionResult LiquidarTodos(string id,PilotoLiquidacionFiltro filtro)
        {
            try {var retorno=Retorno(filtro,id);var n=new PilotoLiquidacionBL().LiquidarDocumentos(User.Identity.Name,id,null);TempData["Liquidacion.Mensaje"]=n+" documentos liquidados. Revisa el resumen y liquida la ruta.";return RedirectToAction("Detalle",retorno);}
            catch(Exception e) {return Error(e);}
        }
        [HttpPost,ValidateAntiForgeryToken]
        public ActionResult LiquidarRuta(string id,PilotoLiquidacionFiltro filtro)
        {
            try {var retorno=Retorno(filtro,id,true);new PilotoLiquidacionBL().LiquidarRuta(User.Identity.Name,id);TempData["Liquidacion.Mensaje"]="Ruta liquidada correctamente. Ya puedes incluirla en un reporte Excel.";return RedirectToAction("Detalle",retorno);}
            catch(Exception e) {return Error(e);}
        }
        [HttpPost,ValidateAntiForgeryToken]
        public ActionResult Exportar(string[] ids)
        {
            try {
                var datos=new PilotoLiquidacionBL().Reporte(User.Identity.Name,ids);
                var archivo=PilotoLiquidacionExcel.Crear(datos);
                return File(archivo,"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet","Liquidacion-Rutas-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".xlsx");
            } catch(Exception e) {return Error(e);}
        }
    }
}
