using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Web;
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
        public ActionResult Index(string placa,string id,int pagina=1,int paginaCerradas=1,int paginaAbiertas=1)
        {
            try
            {
                var da=new GaritaRutaDA();
                if(string.IsNullOrWhiteSpace(placa) && string.IsNullOrWhiteSpace(id)) {da.Preparar(User.Identity.Name);return View(new GaritaRuta());}
                if(string.IsNullOrWhiteSpace(id)) return View(da.BuscarPorPlaca(User.Identity.Name,placa,pagina,paginaCerradas,paginaAbiertas));
                return View(da.Detalle(User.Identity.Name,placa,id));
            }
            catch(Exception e) { ViewBag.Placa=placa;return Error(e); }
        }
        [HttpPost,ValidateAntiForgeryToken]
        public ActionResult RegistrarEntrada(GaritaRegistroEntrada modelo)
        {
            try
            {
                if(modelo==null || !ModelState.IsValid) throw new PilotoException(400,"Revisa la ruta antes de registrar la entrada.");
                new GaritaRutaDA().RegistrarEntrada(User.Identity.Name,modelo);
                TempData["Garita.Mensaje"]="Hora de entrada registrada. La ruta continúa Cerrada.";
                return RedirectToAction("Index",new{placa=modelo.PlacaBusqueda,id=modelo.RutaId});
            }
            catch(Exception e) {ViewBag.Placa=modelo==null?null:modelo.PlacaBusqueda;return Error(e);}
        }
        [HttpPost,ValidateAntiForgeryToken]
        public ActionResult RegistrarSalida(GaritaRegistroSalida modelo,HttpPostedFileBase foto)
        {
            try
            {
                if(modelo==null || !ModelState.IsValid) throw new PilotoException(400,"Revisa los datos antes de registrar la salida.");
                if(foto!=null && (foto.ContentLength>0 || !string.IsNullOrWhiteSpace(foto.FileName)))
                {
                    if(foto.ContentLength<=0 || foto.ContentLength>PilotoReglas.MaxImagenBytes) throw new PilotoException(400,"La fotografía debe pesar hasta 10 MB.");
                    modelo.FotoNombre=Path.GetFileName(foto.FileName??"");
                    using(var memoria=new MemoryStream())
                    {
                        var bloque=new byte[81920];int leidos;
                        while((leidos=foto.InputStream.Read(bloque,0,bloque.Length))>0)
                        {if(memoria.Length+leidos>PilotoReglas.MaxImagenBytes) throw new PilotoException(400,"La fotografía supera 10 MB.");memoria.Write(bloque,0,leidos);}
                        modelo.FotoContenido=memoria.ToArray();
                    }
                    PilotoReglas.ValidarImagen(modelo.FotoNombre,modelo.FotoContenido);
                }
                new GaritaRutaDA().RegistrarSalida(User.Identity.Name,modelo);
                TempData["Garita.Mensaje"]="Hora de salida registrada. La ruta está En ruta.";
                return RedirectToAction("Index",new{placa=modelo.PlacaBusqueda,id=modelo.RutaId});
            }
            catch(Exception e) { ViewBag.Placa=modelo==null?null:modelo.PlacaBusqueda;return Error(e); }
        }
        [HttpGet]
        public ActionResult FotoSalida(string placa,string id)
        {
            try {var foto=new GaritaRutaDA().ObtenerFoto(User.Identity.Name,placa,id);Response.AddHeader("X-Content-Type-Options","nosniff");return File(foto.Contenido,foto.ContentType);}
            catch(Exception e) {ViewBag.Placa=placa;return Error(e);}
        }
        [HttpPost,ValidateAntiForgeryToken]
        public ActionResult Diferencia(GaritaDiferencia modelo)
        {
            try
            {
                if(!ModelState.IsValid) throw new PilotoException(400,"Revisa la descripción de la diferencia.");
                new GaritaRutaDA().Reportar(User.Identity.Name,modelo);
                TempData["Garita.Mensaje"]="Diferencia registrada. No permitas la salida; contacta a Distribución.";
                return RedirectToAction("Index",new{placa=modelo.PlacaBusqueda,id=modelo.RutaId});
            }
            catch(Exception e) { ViewBag.Placa=modelo==null?null:modelo.PlacaBusqueda;return Error(e); }
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
                sql!=null && (sql.Number==-2 || sql.Number==1222 || sql.Number==1205)?"La ruta está ocupada. Espera unos segundos y busca la placa otra vez.":
                "No se pudo completar la operación. Busca de nuevo la placa o contacta a Distribución.";
            if(esperado==null) {var referencia=Guid.NewGuid().ToString("N");ViewBag.Referencia=referencia;Trace.TraceError("Garita referencia={0}, tipo={1}, numeroSQL={2}",referencia,e.GetType().Name,sql==null?0:sql.Number);}
            return View("Error");
        }
    }
}
