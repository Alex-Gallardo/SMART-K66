using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
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
        public ActionResult Index(string placa,string id,int pagina=1,int paginaCerradas=1,int paginaAbiertas=1,string seccion=null)
        {
            try
            {
                var da=new GaritaRutaDA();
                if(string.IsNullOrWhiteSpace(placa) && string.IsNullOrWhiteSpace(id)) {da.Preparar(User.Identity.Name);return View(new GaritaRuta());}
                if(string.IsNullOrWhiteSpace(id)) {var lista=da.BuscarPorPlaca(User.Identity.Name,placa,pagina,paginaCerradas,paginaAbiertas);lista.Seccion=GaritaReglas.Seccion(seccion);if(lista.Seccion=="") lista.Seccion=lista.TotalAbiertas>0?"salidas":lista.TotalRutas>0?"en-ruta":"regresos";return View(lista);}
                var detalle=da.Detalle(User.Identity.Name,placa,id);detalle.Seccion=GaritaReglas.Seccion(seccion);detalle.Pagina=Math.Max(1,pagina);detalle.PaginaAbiertas=Math.Max(1,paginaAbiertas);detalle.PaginaCerradas=Math.Max(1,paginaCerradas);return View(detalle);
            }
            catch(Exception e) { ViewBag.Placa=placa;return Error(e); }
        }
        [HttpPost,ValidateAntiForgeryToken]
        public ActionResult RegistrarEntrada(GaritaRegistroEntrada modelo,HttpPostedFileBase foto)
        {
            try
            {
                if(modelo==null || !ModelState.IsValid) throw new PilotoException(400,"Revisa la ruta antes de registrar la entrada.");
                string nombreEntrada;
                modelo.FotoContenido=LeerFoto(foto,out nombreEntrada);
                modelo.FotoNombre=nombreEntrada;
                var fechaUtc=new GaritaRutaDA().RegistrarEntrada(User.Identity.Name,modelo);
                var destino=Url.Action("Index",new{placa=modelo.PlacaBusqueda,id=modelo.RutaId,seccion=GaritaReglas.Seccion(modelo.Seccion),pagina=modelo.Pagina,paginaAbiertas=modelo.PaginaAbiertas,paginaCerradas=modelo.PaginaCerradas});
                if(Request.IsAjaxRequest()) return Json(new{ok=true,tipo="entrada",hora=fechaUtc.AddHours(-6).ToString("hh:mm tt",CultureInfo.InvariantCulture),url=destino});
                TempData["Garita.Mensaje"]="Hora de entrada registrada. La ruta continúa Cerrada.";
                return Redirect(destino);
            }
            catch(Exception e) {ViewBag.Placa=modelo==null?null:modelo.PlacaBusqueda;return Error(e);}
        }
        [HttpPost,ValidateAntiForgeryToken]
        public ActionResult RegistrarSalida(GaritaRegistroSalida modelo,HttpPostedFileBase foto)
        {
            try
            {
                if(modelo==null || !ModelState.IsValid) throw new PilotoException(400,"Revisa los datos antes de registrar la salida.");
                string nombreSalida;
                modelo.FotoContenido=LeerFoto(foto,out nombreSalida);
                modelo.FotoNombre=nombreSalida;
                var fechaUtc=new GaritaRutaDA().RegistrarSalida(User.Identity.Name,modelo);
                var destino=Url.Action("Index",new{placa=modelo.PlacaBusqueda,id=modelo.RutaId,seccion=GaritaReglas.Seccion(modelo.Seccion),pagina=modelo.Pagina,paginaAbiertas=modelo.PaginaAbiertas,paginaCerradas=modelo.PaginaCerradas});
                if(Request.IsAjaxRequest()) return Json(new{ok=true,tipo="salida",hora=fechaUtc.AddHours(-6).ToString("hh:mm tt",CultureInfo.InvariantCulture),url=destino});
                TempData["Garita.Mensaje"]="Hora de salida registrada. La ruta está En ruta.";
                return Redirect(destino);
            }
            catch(Exception e) { ViewBag.Placa=modelo==null?null:modelo.PlacaBusqueda;return Error(e); }
        }
        [HttpGet]
        public ActionResult FotoSalida(string placa,string id)
        {
            try {var foto=new GaritaRutaDA().ObtenerFoto(User.Identity.Name,placa,id);Response.AddHeader("X-Content-Type-Options","nosniff");return File(foto.Contenido,foto.ContentType);}
            catch(Exception e) {ViewBag.Placa=placa;return Error(e);}
        }
        [HttpGet]
        public ActionResult FotoEntrada(string placa,string id)
        {
            try {var foto=new GaritaRutaDA().ObtenerFotoEntrada(User.Identity.Name,placa,id);Response.AddHeader("X-Content-Type-Options","nosniff");return File(foto.Contenido,foto.ContentType);}
            catch(Exception e) {ViewBag.Placa=placa;return Error(e);}
        }
        [HttpGet]
        public ActionResult FotoPiloto(string placa,string id,int rowId,bool cliente=false)
        {
            try {var foto=new GaritaRutaDA().ObtenerFotoPiloto(User.Identity.Name,placa,id,rowId,cliente);Response.AddHeader("X-Content-Type-Options","nosniff");return File(foto.Contenido,foto.ContentType);}
            catch(Exception e) {ViewBag.Placa=placa;return Error(e);}
        }
        private static byte[] LeerFoto(HttpPostedFileBase foto,out string nombre)
        {
            nombre=null;
            if(foto==null || (foto.ContentLength==0 && string.IsNullOrWhiteSpace(foto.FileName))) return null;
            if(foto.ContentLength<=0 || foto.ContentLength>PilotoReglas.MaxImagenBytes) throw new PilotoException(400,"La fotografía debe pesar hasta 10 MB.");
            nombre=Path.GetFileName(foto.FileName??"");
            using(var memoria=new MemoryStream())
            {
                var bloque=new byte[81920];int leidos;
                while((leidos=foto.InputStream.Read(bloque,0,bloque.Length))>0)
                {if(memoria.Length+leidos>PilotoReglas.MaxImagenBytes) throw new PilotoException(400,"La fotografía supera 10 MB.");memoria.Write(bloque,0,leidos);}
                var contenido=memoria.ToArray();
                PilotoReglas.ValidarImagen(nombre,contenido);
                return contenido;
            }
        }
        [HttpPost,ValidateAntiForgeryToken]
        public ActionResult Diferencia(GaritaDiferencia modelo)
        {
            try
            {
                if(!ModelState.IsValid) throw new PilotoException(400,"Revisa la descripción de la diferencia.");
                new GaritaRutaDA().Reportar(User.Identity.Name,modelo);
                var destino=Url.Action("Index",new{placa=modelo.PlacaBusqueda,id=modelo.RutaId,seccion=GaritaReglas.Seccion(modelo.Seccion),pagina=modelo.Pagina,paginaAbiertas=modelo.PaginaAbiertas,paginaCerradas=modelo.PaginaCerradas});
                if(Request.IsAjaxRequest()) return Json(new{ok=true,tipo="diferencia",url=destino});
                TempData["Garita.Mensaje"]="Diferencia registrada. No permitas la salida; contacta a Distribución.";
                return Redirect(destino);
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
            if(Request.IsAjaxRequest()) return Json(new{ok=false,mensaje=ViewBag.Mensaje});
            return View("Error");
        }
    }
}
