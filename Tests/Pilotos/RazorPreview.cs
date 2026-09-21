using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.WebPages;
using DiamDev.Give.Entities;

// Renders compiled Razor with fictitious models. No controller, SQL, login or HTTP server.
internal static class RazorPreview
{
    private sealed class PreviewController : Controller {}
    private sealed class PreviewView : IView { public void Render(ViewContext context, TextWriter writer) {} }
    private sealed class PreviewRequest : HttpRequestWrapper {
        public PreviewRequest(HttpRequest r):base(r) {}
        public override string ApplicationPath { get { return "/"; } }
        public override string AppRelativeCurrentExecutionFilePath {get{return "~/Piloto/Index";}}
    }
    private sealed class PreviewContext : HttpContextWrapper {
        readonly HttpRequestBase request;
        public PreviewContext(HttpContext c):base(c){request=new PreviewRequest(c.Request);}
        public override HttpRequestBase Request {get{return request;}}
    }
    public static string Render(Type type, object model, string body = null, string vista = "activas")
    {
        // Standalone renderer has no IIS header pipeline; production uses its normal antiforgery setup.
        System.Web.Helpers.AntiForgeryConfig.SuppressXFrameOptionsHeader=true;
        using(var writer=new StringWriter()) {
            var raw=new HttpContext(new HttpRequest("","http://localhost/Piloto/Index",""),new HttpResponse(writer));
            raw.User=new GenericPrincipal(new GenericIdentity("piloto_demo","Forms"),new string[0]);
            HttpContext.Current=raw;
            var http=new PreviewContext(raw);
            var route=new RouteData(); route.Values["controller"]="Piloto"; route.Values["action"]="Index";
            var controller=new PreviewController();
            controller.ControllerContext=new ControllerContext(http,route,controller);
            var ruta=model as PilotoRuta; if(ruta!=null && vista=="historial") ruta.Estado="C";
            var viewData=new ViewDataDictionary(model);
            viewData["PruebasSoloLectura"]=true; viewData["PreviewBody"]=body; viewData["PaginaRutas"]=1; viewData["Vista"]=vista;
            viewData["Title"]="Pilotos · Demo"; viewData["Desde"]="2026-09-08"; viewData["Hasta"]="2026-09-21";
            viewData["Mensaje"]="La ruta no está disponible. Consulta a Distribución.";
            var page=(WebViewPage)Activator.CreateInstance(type);
            page.ViewContext=new ViewContext(controller.ControllerContext,new PreviewView(),viewData,new TempDataDictionary(),writer);
            page.ViewData=viewData; page.InitHelpers();
            page.PushContext(new WebPageContext(http,page,null),writer);
            page.ViewContext.Writer=page.Output;
            page.Execute(); page.Layout=null; page.PopContext();
            return writer.ToString();
        }
    }
    public static PilotoLista Lista(bool fija=false,bool vacia=false,string vista="activas")
    {
        var lista=new PilotoLista {Desde=new DateTime(2026,9,8),Hasta=new DateTime(2026,9,21),Pagina=1,Vista=vista,RutaFija=fija,Rutas=new List<PilotoRuta>()};
        if(!vacia) for(int i=1;i<=(fija?1:4);i++) lista.Rutas.Add(new PilotoRuta {Id="DEMO-2026-"+i.ToString("000"),Placa="DEMO-001",Fecha=new DateTime(2026,9,21),Estado=fija?"C":vista=="historial"?(i%2==0?"X":"C"):(i%2==0?"A":"E"),Centro="CENTRO DE DISTRIBUCIÓN",Piloto="Piloto de demostración"});
        return lista;
    }
    public static PilotoRuta Ruta()
    {
        var ruta=Lista().Rutas[0]; ruta.Transporte="Transporte de demostración"; ruta.Vehiculo="Camión · Línea de prueba";
        ruta.TotalDocumentos=26; ruta.TamanoPaginaDocumentos=25; ruta.PaginaDocumentos=1;
        for(int i=1;i<=25;i++) ruta.Documentos.Add(new PilotoDocumento {RowId=i,Documento="DOC-DEMO-00"+i,Tipo="FACTURA",Empresa="DEMO",Cliente="Cliente de demostración "+i,Direccion="Dirección ficticia, zona de distribución. Referencia de acceso para el piloto.",Bultos=12,Visito=i==1?(bool?)true:null,Entrega=i==1?"ENTREGADO":null,Observaciones=i==2?"Ejemplo: verificar acceso antes de descargar.":null});
        return ruta;
    }
}
