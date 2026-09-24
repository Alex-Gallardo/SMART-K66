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
    public static string Render(Type type, object model, string body = null, string vista = "activas", bool editable = false)
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
            viewData["PruebasSoloLectura"]=!editable; viewData["PreviewBody"]=body; viewData["PaginaRutas"]=1; viewData["Vista"]=vista;
            viewData["EsAdministracion"]=model is PilotoAdminLista || model is PilotoAdminEdicion;
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
    public static PilotoRuta RutaEditable()
    {
        var ruta=Ruta(); ruta.Documentos=ruta.Documentos.Take(3).ToList(); ruta.TotalDocumentos=3; ruta.TamanoPaginaDocumentos=200; ruta.PuedeCerrar=true;
        ruta.ImagenesDisponibles=true; ruta.PuedeAdjuntarImagen=true;
        ruta.Documentos[0].TieneImagen=true; ruta.Documentos[0].ImagenNombre="entrega-demo.jpg";
        ruta.Documentos[1].Cliente=ruta.Documentos[0].Cliente;
        ruta.Documentos[1].Direccion=ruta.Documentos[0].Direccion;
        ruta.Solicitud=Guid.NewGuid();
        ruta.Documentos[1].Visito=false; ruta.Documentos[1].Entrega="NO ENTREGADO"; ruta.Documentos[1].Motivo="CLIENTE CERRADO";
        ruta.Documentos[1].ObservacionPiloto="Se encontró el local cerrado al llegar.";
        ruta.Version=PilotoReglas.Version(ruta);
        return ruta;
    }
    public static PilotoRuta RutaConBorrador()
    {
        var ruta=RutaEditable();
        var segundo=ruta.Documentos[1];
        segundo.AnteriorMasivo=new PilotoResultado { RowId=segundo.RowId, Visito=false,
            Entrega="NO ENTREGADO", Motivo="CLIENTE CERRADO", Observaciones="Se encontró el local cerrado al llegar." };
        ruta.Documentos[0].AnteriorMasivo=new PilotoResultado { RowId=ruta.Documentos[0].RowId,
            Visito=true, Entrega="ENTREGADO" };
        segundo.Visito=true; segundo.Entrega="ENTREGADO"; segundo.Motivo=null; segundo.ObservacionPiloto=null;
        ruta.Borradores.Add(new PilotoBorradorCliente { RutaId=ruta.Id, Version=ruta.Version,
            PrimerRowId=ruta.Documentos[0].RowId, MasivoActivo=true });
        return ruta;
    }
    public static PilotoAdminLista AdminLista()
    {
        var lista=new PilotoAdminLista();
        lista.Usuarios.Add(new PilotoAdminUsuario {UsuarioId=1,Login="piloto.uno",Nombre="Piloto de demostración",UsuarioActivo=true,AutenticarSite=true,TieneRolPiloto=true,VinculoActivo=true,Piloto="EMPLEADO DEMO",Centros=2,Vehiculos=1});
        lista.Usuarios.Add(new PilotoAdminUsuario {UsuarioId=2,Login="piloto.pendiente",Nombre="Usuario pendiente",UsuarioActivo=true,AutenticarSite=true,TieneRolPiloto=true,Centros=0,Vehiculos=0});
        lista.Usuarios.Add(new PilotoAdminUsuario {UsuarioId=3,Login="piloto.inactivo",Nombre="Usuario inactivo",UsuarioActivo=false,AutenticarSite=true,TieneRolPiloto=true,VinculoActivo=false,Piloto="OTRO PILOTO",Centros=1,Vehiculos=0});
        return lista;
    }
    public static PilotoAdminEdicion AdminEdicion()
    {
        var modelo=new PilotoAdminEdicion {Usuario=AdminLista().Usuarios[0],Formulario=new PilotoAdminGuardar {UsuarioId=1,EmpleadoRowId=101,CodigoOperador="PILOTO01",Activo=true,Centros=new[]{"CENTRO-1"},Placas=new[]{"C-001"}}};
        modelo.Empleados.Add(new PilotoAdminEmpleado {RowId=101,Nombre="EMPLEADO DEMO",Activo=true,Seleccionado=true});
        modelo.Empleados.Add(new PilotoAdminEmpleado {RowId=102,Nombre="EMPLEADO DISPONIBLE",Activo=true});
        modelo.Centros.Add(new PilotoAdminOpcion {Codigo="CENTRO-1",Nombre="CENTRO-1",Activo=true,Seleccionado=true});
        modelo.Centros.Add(new PilotoAdminOpcion {Codigo="CENTRO-2",Nombre="CENTRO-2",Activo=true});
        modelo.Vehiculos.Add(new PilotoAdminOpcion {Codigo="C-001",Nombre="Marca · Línea · Camión · K66",Activo=true,Seleccionado=true});
        modelo.Vehiculos.Add(new PilotoAdminOpcion {Codigo="C-002",Nombre="Marca · Panel · K66",Activo=true});
        modelo.Rutas.Add(new PilotoRuta {Id="DEMO-2026-001",Fecha=new DateTime(2026,9,22),Estado="E",Centro="CENTRO-1",Placa="C-001"});
        return modelo;
    }
}
