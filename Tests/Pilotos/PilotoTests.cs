using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using DiamDev.Give.Entities;
using DiamDev.Give.DAL;
using DiamDev.Give.BLL;
using DiamDev.Give.UI.Controllers;

namespace DiamDev.Give.UI.App_Start
{
    // Sustituye únicamente al atributo de infraestructura durante la compilación aislada.
    public sealed class PermisoAttribute : AuthorizeAttribute
    {
        public string Permiso { get; private set; }
        public PermisoAttribute(string permiso) { Permiso = permiso; }
    }
}

internal static class PilotoTests
{
    static int comprobaciones;
    static void Check(bool ok, string name) { comprobaciones++; if(!ok) throw new Exception(name); }
    static void Rechaza(Action test, int status, string name) {
        try { test(); } catch(PilotoException e) { Check(e.StatusCode==status,name); return; }
        throw new Exception("No se rechazo: "+name);
    }
    static void ProbarAccesoSesion(bool modoPrueba) {
        var http=new ContextoPrueba("consulta_demo");
        var controller=new PilotoController();
        controller.ControllerContext=new ControllerContext(http,new RouteData(),controller);
        var routes=new RouteCollection(); routes.MapRoute("Default","{controller}/{action}/{id}",new {action="Index",id=UrlParameter.Optional});
        controller.Url=new UrlHelper(controller.ControllerContext.RequestContext,routes);
        var indexAction=new ReflectedActionDescriptor(typeof(PilotoController).GetMethod("Index"),"Index",new ReflectedControllerDescriptor(typeof(PilotoController)));
        var filter=new ActionExecutingContext(controller.ControllerContext,indexAction,new Dictionary<string,object>());
        typeof(PilotoController).GetMethod("OnActionExecuting",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(controller,new object[]{filter});
        Check(!(filter.Result is RedirectToRouteResult),"sesion iniciada no regresa al login");
        if(modoPrueba) Check(filter.Result==null,"sesion normal entra a consulta temporal sin marcador PILOTO");
        else {
            Check(filter.Result is ViewResult && http.Response.StatusCode==403,"sin configuracion informa acceso pendiente");
            Check(object.Equals(controller.ViewData["ConfigurarPrueba"],true),"muestra instrucciones de configuracion");
        }
    }
    sealed class ContextoPrueba : HttpContextBase {
        readonly IPrincipal user;
        readonly HttpResponseBase response=new RespuestaPrueba();
        readonly HttpSessionStateBase session=new SesionPrueba();
        readonly HttpRequestBase request=new PeticionPrueba();
        public ContextoPrueba(string login) {user=new GenericPrincipal(new GenericIdentity(login,"Forms"),new string[0]);}
        public override IPrincipal User {get{return user;}set{throw new NotSupportedException();}}
        public override HttpResponseBase Response {get{return response;}}
        public override HttpSessionStateBase Session {get{return session;}}
        public override HttpRequestBase Request {get{return request;}}
    }
    sealed class PeticionPrueba : HttpRequestBase {
        public override string ApplicationPath {get{return "/";}}
        public override string AppRelativeCurrentExecutionFilePath {get{return "~/Piloto/Index";}}
        public override string PathInfo {get{return "";}}
    }
    sealed class RespuestaPrueba : HttpResponseBase {
        readonly HttpCachePolicyBase cache=new CachePrueba();
        public override HttpCachePolicyBase Cache {get{return cache;}}
        public override int StatusCode {get;set;}
        public override bool TrySkipIisCustomErrors {get;set;}
        public override string ApplyAppPathModifier(string path) {return path;}
    }
    sealed class CachePrueba : HttpCachePolicyBase {
        public override void SetCacheability(HttpCacheability value) {}
        public override void SetNoStore() {}
    }
    sealed class SesionPrueba : HttpSessionStateBase {
        readonly Dictionary<string,object> values=new Dictionary<string,object>();
        public override object this[string name] {get{object value; return values.TryGetValue(name,out value)?value:null;}set{values[name]=value;}}
    }
    static PilotoRuta Ruta() {
        var r=new PilotoRuta { Id="DEMO-1", Estado="E", Placa="DEMO", Piloto="PILOTO DEMO", Centro="DEMO", Fecha=new DateTime(2026,1,1), PuedeCerrar=true };
        r.Documentos.Add(new PilotoDocumento {RowId=1,Tipo="FACTURA",Documento="D1",Bultos=2});
        r.Documentos.Add(new PilotoDocumento {RowId=2,Tipo="ENVIO",Documento="D2",Bultos=1});
        r.Version=PilotoReglas.Version(r); return r;
    }
    static PilotoCierre Cierre(PilotoRuta r) {
        return new PilotoCierre {RutaId=r.Id,Version=r.Version,Solicitud=Guid.NewGuid(),Documentos=new List<PilotoResultado> {
            new PilotoResultado {RowId=1,Visito=true,Entrega="ENTREGADO"},
            new PilotoResultado {RowId=2,Visito=false,Entrega="NO ENTREGADO",Motivo="CLIENTE CERRADO",Observaciones="El local estaba cerrado."}
        }};
    }
    public static int Main(string[] args) {
        try {
            if(args.Length==1 && args[0]=="config-invalida") {
                Rechaza(()=>PilotoRutaBL.ValidarConfiguracionPrueba(),503,"configuracion incompleta se rechaza antes de SQL");
                Rechaza(()=>new PilotoRutaDA(),503,"constructor no abre SQL con configuracion incompleta");
                Console.WriteLine("OK: configuracion invalida rechazada sin SQL."); return 0;
            }
            if(args.Length==1 && args[0]=="ruta-fija") {
                PilotoRutaBL.ValidarConfiguracionPrueba();
                Check(PilotoRutaBL.ConsultaRutaFija,"alcance por ruta sin placa");
                Check(PilotoRutaDA.PruebasSoloLectura,"normaliza espacios de flags");
                ProbarAccesoSesion(true);
                Rechaza(()=>new PilotoRutaDA().Cerrar("consulta_demo",null),403,"ruta fija tambien bloquea cierres");
                Console.WriteLine("OK: "+comprobaciones+" comprobaciones de ruta fija."); return 0;
            }
            if(args.Length==1 && args[0]=="sesion-normal") {
                var http=new ContextoPrueba("consulta_demo");
                var controller=new PilotoController();
                controller.ControllerContext=new ControllerContext(http,new RouteData(),controller);
                var indexAction=new ReflectedActionDescriptor(typeof(PilotoController).GetMethod("Index"),"Index",new ReflectedControllerDescriptor(typeof(PilotoController)));
                var filter=new ActionExecutingContext(controller.ControllerContext,indexAction,new Dictionary<string,object>());
                typeof(PilotoController).GetMethod("OnActionExecuting",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(controller,new object[]{filter});
                Check(http.Response.StatusCode==403 && !(filter.Result is RedirectToRouteResult),"sesion normal sin marcador no entra ni genera bucle");
                Check(object.Equals(controller.ViewData["SesionVencida"],true),"sesion vencida diferenciada de instalacion");
                Check(object.Equals(controller.ViewData["ConfigurarPrueba"],false),"sesion vencida no aconseja activar pruebas");
                http.Session["Pilotos.Autenticado"]="consulta_demo";
                filter.Result=null;
                typeof(PilotoController).GetMethod("OnActionExecuting",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(controller,new object[]{filter});
                Check(filter.Result==null,"sesion piloto validada puede continuar");
                Console.WriteLine("OK: "+comprobaciones+" comprobaciones de sesion normal."); return 0;
            }
            if(args.Length==1 && args[0]=="consulta-temporal") {
                ProbarAccesoSesion(true);
                Check(PilotoRutaDA.PruebasSoloLectura,"modo de consulta activo");
                Check(!PilotoRutaDA.Habilitado,"no requiere habilitar el modulo normal");
                Check(!PilotoRutaDA.CierreHabilitado,"ignora PermitirCierre=true durante pruebas");
                Check(PilotoRutaBL.PermiteUsuarioPrueba("CONSULTA_DEMO"),"usuario permitido sin rol ni sesion especial");
                Check(!PilotoRutaBL.PermiteUsuarioPrueba("OTRO"),"no habilita otros usuarios");
                Check(!PilotoRutaBL.PermiteUsuarioPrueba(null),"no habilita anonimos");
                Rechaza(()=>new PilotoRutaDA().Cerrar("CONSULTA_DEMO",null),403,"DAL rechaza cierre antes de conectar SQL");
                Rechaza(()=>new PilotoRutaBL().Cerrar("CONSULTA_DEMO",null),403,"BLL no permite saltar cierre bloqueado");
                Rechaza(()=>new PilotoRutaDA().Listar("consulta_demo",DateTime.Today,DateTime.Today,1,"cualquier estado"),400,"vista desconocida rechazada antes de SQL");
                Rechaza(()=>new PilotoRutaDA().Listar("consulta_demo",DateTime.Today.AddDays(-31),DateTime.Today,1,"activas"),400,"ventana de activas acotada antes de SQL");
                Console.WriteLine("OK: "+comprobaciones+" comprobaciones de consulta temporal; sin conexiones SQL."); return 0;
            }
            ProbarAccesoSesion(false);
            Check(PilotoReglas.VistaRutas(null)=="activas","vista inicial prioriza activas");
            Check(PilotoReglas.VistaRutas("activas")=="activas","vista activa explicita");
            Check(PilotoReglas.VistaRutas("historial")=="historial","historial separado");
            Rechaza(()=>PilotoReglas.VistaRutas("todas"),400,"no acepta ampliacion de estados por URL");
            Rechaza(()=>PilotoReglas.VistaRutas("' OR 1=1 --"),400,"filtro no acepta SQL");
            var grande=new PilotoRuta {TotalDocumentos=201,PuedeCerrar=true};
            PilotoReglas.PrepararConsulta(grande,9);
            Check(!grande.PuedeCerrar && grande.PaginaDocumentos==9 && grande.TamanoPaginaDocumentos==25 && !grande.HayMasDocumentos,"ruta de 201 permite consultar ultima pagina sin habilitar cierre");
            Rechaza(()=>PilotoReglas.PrepararConsulta(grande,10),404,"pagina posterior al ultimo documento");
            Rechaza(()=>PilotoReglas.PrepararConsulta(grande,0),400,"pagina cero");
            Rechaza(()=>PilotoReglas.PrepararConsulta(grande,int.MaxValue),400,"pagina excesiva no desborda offset");
            var editable=new PilotoRuta {TotalDocumentos=200,PuedeCerrar=true};
            PilotoReglas.PrepararConsulta(editable,2);
            Check(editable.PuedeCerrar && editable.PaginaDocumentos==1 && editable.TamanoPaginaDocumentos==200 && !editable.HayMasDocumentos,"formulario siempre contiene todos los documentos");
            var consulta=new PilotoRuta {TotalDocumentos=26,PuedeCerrar=false};
            PilotoReglas.PrepararConsulta(consulta,1); Check(consulta.HayMasDocumentos && !consulta.PuedeCerrar,"lectura paginada nunca activa cierre");
            PilotoReglas.PrepararConsulta(consulta,2); Check(!consulta.HayMasDocumentos,"ultima pagina parcial");
            var vacia=new PilotoRuta {TotalDocumentos=0};
            PilotoReglas.PrepararConsulta(vacia,1); Check(!vacia.HayMasDocumentos,"ruta sin documentos consultable");
            var r=Ruta(); var c=Cierre(r);
            PilotoReglas.ValidarCierre(r,c); Check(c.Documentos[1].Entrega=="NO ENTREGADO","preserva fallo al cerrar");
            c.Documentos[1].Entrega="INCIDENCIA"; PilotoReglas.ValidarCierre(r,c); Check(true,"incidencia permite cierre con motivo");
            foreach(var estado in new[]{"A","C","X",null,"Z"}) {
                r=Ruta(); c=Cierre(r); r.Estado=estado; Rechaza(()=>PilotoReglas.ValidarCierre(r,c),409,"estado "+estado);
            }
            r=Ruta(); c=Cierre(r); c.Version="obsoleta"; Rechaza(()=>PilotoReglas.ValidarCierre(r,c),409,"version obsoleta");
            c=Cierre(r); c.RutaId="OTRA"; Rechaza(()=>PilotoReglas.ValidarCierre(r,c),400,"ruta ajena");
            c=Cierre(r); c.Solicitud=Guid.Empty; Rechaza(()=>PilotoReglas.ValidarCierre(r,c),400,"solicitud vacia");
            c=Cierre(r); c.Documentos.RemoveAt(1); Rechaza(()=>PilotoReglas.ValidarCierre(r,c),400,"documento omitido");
            c=Cierre(r); c.Documentos[1].RowId=1; Rechaza(()=>PilotoReglas.ValidarCierre(r,c),400,"documento duplicado");
            c=Cierre(r); c.Documentos[1].RowId=999; Rechaza(()=>PilotoReglas.ValidarCierre(r,c),400,"documento ajeno");
            c=Cierre(r); c.Documentos[1].Motivo=" "; Rechaza(()=>PilotoReglas.ValidarCierre(r,c),400,"fallo sin motivo");
            c=Cierre(r); c.Documentos[1].Motivo="MOTIVO INVENTADO"; Rechaza(()=>PilotoReglas.ValidarCierre(r,c),400,"motivo fuera del catalogo");
            c=Cierre(r); c.Documentos[1].Observaciones=" "; Rechaza(()=>PilotoReglas.ValidarCierre(r,c),400,"fallo sin observacion");
            c=Cierre(r); c.Documentos[1].Observaciones=new string('x',501); Rechaza(()=>PilotoReglas.ValidarCierre(r,c),400,"observacion larga");
            c=Cierre(r); c.Documentos[0].Visito=false; Rechaza(()=>PilotoReglas.ValidarCierre(r,c),400,"entregado sin visita");
            c=Cierre(r); c.Documentos[0].Visito=null; Rechaza(()=>PilotoReglas.ValidarCierre(r,c),400,"visita omitida");
            c=Cierre(r); c.Documentos[0].Entrega="X"; Rechaza(()=>PilotoReglas.ValidarCierre(r,c),400,"resultado no permitido");
            c=Cierre(r); c.Documentos[1].Motivo=new string('x',151); Rechaza(()=>PilotoReglas.ValidarCierre(r,c),400,"motivo largo");
            var version=r.Version; r.Placa="OTRO"; Check(PilotoReglas.Version(r)!=version,"cambio de vehiculo invalida formulario");
            r=Ruta(); r.Documentos[0].Observaciones="Cambio en oficina"; Check(PilotoReglas.Version(r)!=version,"observaciones invalida formulario");
            r=Ruta(); r.Documentos[0].Entrega="INCIDENCIA"; Check(PilotoReglas.Version(r)!=version,"monitoreo invalida formulario");
            r=Ruta(); c=Cierre(r); var huella=PilotoReglas.HuellaSolicitud(c);
            c.Documentos.Reverse(); Check(PilotoReglas.HuellaSolicitud(c)==huella,"reordenar no cambia idempotencia");
            c.Documentos[0].Motivo="Otro"; Check(PilotoReglas.HuellaSolicitud(c)!=huella,"reintento diferente no coincide");
            c=Cierre(r); huella=PilotoReglas.HuellaSolicitud(c); c.Documentos[1].Observaciones="Otra explicación"; Check(PilotoReglas.HuellaSolicitud(c)!=huella,"observacion participa en idempotencia");
            c=Cierre(r); c.Documentos[0].Motivo="OTRO"; c.Documentos[0].Observaciones="No debe guardarse"; PilotoReglas.ValidarCierre(r,c);
            Check(c.Documentos[0].Motivo==null && c.Documentos[0].Observaciones==null,"entregado limpia motivo y observacion nuevos");
            Rechaza(()=>PilotoReglas.HuellaSolicitud(null),400,"null request");
            c.Documentos=Enumerable.Range(1,201).Select(i=>new PilotoResultado { RowId=i }).ToList();
            Rechaza(()=>PilotoReglas.HuellaSolicitud(c),400,"limite de formulario");
            r=Ruta(); r.Documentos.Clear(); c=Cierre(r); c.Documentos.Clear(); Rechaza(()=>PilotoReglas.ValidarCierre(r,c),400,"ruta vacia");
            var now=new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc);
            var d=new PilotoDesafio("DEMO",now); Check(d.Codigo.Length==6,"codigo de seis digitos");
            Check(d.Verificar(d.Codigo,now),"codigo valido"); Check(!d.Verificar(d.Codigo,now),"codigo de un solo uso");
            d=new PilotoDesafio("DEMO",now); Check(!d.Verificar(d.Codigo,now.AddMinutes(5)),"vence a los cinco minutos");
            d=new PilotoDesafio("DEMO",now); for(int i=0;i<5;i++) Check(!d.Verificar("incorrecto",now),"intento incorrecto");
            Check(!d.Verificar(d.Codigo,now),"bloqueo tras cinco intentos");
            var posted=new NameValueCollection { {"RutaId","DEMO-1"},{"Solicitud",Guid.NewGuid().ToString()}, {"Version",version},
                {"Documentos[0].RowId","1"},{"Documentos[0].Visito","true"},{"Documentos[0].Entrega","ENTREGADO"},
                {"Documentos[1].RowId","2"},{"Documentos[1].Visito","false"},{"Documentos[1].Entrega","NO ENTREGADO"},{"Documentos[1].Motivo","CLIENTE CERRADO"},{"Documentos[1].Observaciones","Se encontró el local cerrado."},
                {"Login","OTRO"},{"Piloto","OTRO"},{"Estado","X"} };
            var binding=new ModelBindingContext {ModelMetadata=ModelMetadataProviders.Current.GetMetadataForType(null,typeof(PilotoCierre)),ModelName="",ValueProvider=new NameValueCollectionValueProvider(posted,CultureInfo.InvariantCulture)};
            var model=(PilotoCierre)new DefaultModelBinder().BindModel(new ControllerContext(),binding);
            Check(binding.ModelState.IsValid && model.Documentos.Count==2 && model.Documentos[1].Visito==false,"contrato real MVC preserva false");
            PilotoReglas.ValidarCierre(Ruta(),model);
            Check(typeof(PilotoCierre).GetProperty("Login")==null && typeof(PilotoCierre).GetProperty("Estado")==null,"identidad y estado no se enlazan");
            var action=typeof(PilotoController).GetMethod("Completar");
            Check(action.IsDefined(typeof(HttpPostAttribute),true) && action.IsDefined(typeof(ValidateAntiForgeryTokenAttribute),true),"cierre POST y CSRF");
            Check(typeof(PilotoController).IsDefined(typeof(AuthorizeAttribute),true),"controlador autenticado");
            Check(typeof(PilotoController).GetMethod("Anular")==null,"sin accion anular");
            var admin=new PilotoAdminGuardar {UsuarioId=1,EmpleadoRowId=2,CodigoOperador=" operador ",Activo=true,
                Centros=new[]{" PC ","pc",""},Placas=new[]{" C-001 ","c-001"},Motivo=" Alta inicial "};
            PilotoAdminReglas.Validar(admin);
            Check(admin.CodigoOperador=="operador" && admin.Centros.Length==1 && admin.Placas.Length==1,"administracion normaliza seleccion sin duplicados");
            admin.Centros=new string[0]; Rechaza(()=>PilotoAdminReglas.Validar(admin),400,"vinculo activo exige centro");
            admin.Centros=new[]{"PC"}; admin.Placas=new string[0]; Rechaza(()=>PilotoAdminReglas.Validar(admin),400,"vinculo activo exige vehiculo");
            admin.Activo=false; admin.EmpleadoRowId=null; admin.CodigoOperador=null; PilotoAdminReglas.Validar(admin); Check(true,"desactivacion no depende de catalogos obsoletos");
            admin.Motivo=" "; Rechaza(()=>PilotoAdminReglas.Validar(admin),400,"cambio administrativo exige motivo");
            admin.Motivo="Cambio"; admin.Activo=true; admin.EmpleadoRowId=2; admin.CodigoOperador="operador"; admin.Centros=new[]{new string('x',16)}; admin.Placas=new[]{"C-001"}; Rechaza(()=>PilotoAdminReglas.Validar(admin),400,"centro administrativo demasiado largo");
            var adminPost=typeof(PilotoController).GetMethods().Single(m=>m.Name=="Configurar" && m.IsDefined(typeof(HttpPostAttribute),true));
            Check(adminPost.IsDefined(typeof(ValidateAntiForgeryTokenAttribute),true),"configuracion POST usa antiforgery");
            var permiso=(DiamDev.Give.UI.App_Start.PermisoAttribute)adminPost.GetCustomAttributes(typeof(DiamDev.Give.UI.App_Start.PermisoAttribute),true).Single();
            Check(permiso.Permiso=="Pilotos.Configurar","configuracion requiere permiso dedicado");
            var adminHttp=new ContextoPrueba("consulta_demo"); var adminController=new PilotoController();
            adminController.ControllerContext=new ControllerContext(adminHttp,new RouteData(),adminController);
            var adminAction=new ReflectedActionDescriptor(typeof(PilotoController).GetMethod("Administracion"),"Administracion",new ReflectedControllerDescriptor(typeof(PilotoController)));
            var adminFilter=new ActionExecutingContext(adminController.ControllerContext,adminAction,new Dictionary<string,object>());
            typeof(PilotoController).GetMethod("OnActionExecuting",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(adminController,new object[]{adminFilter});
            Check(adminFilter.Result==null && object.Equals(adminController.ViewData["EsAdministracion"],true),"administracion no exige sesion especial de piloto");
            Console.WriteLine("OK: "+comprobaciones+" comprobaciones de reglas, paginacion, formulario y desafio."); return 0;
        } catch(Exception e) { Console.Error.WriteLine(e); return 1; }
    }
}
