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
            if(args.Length==1 && args[0]=="panel") return PanelTests.Run();
            if(args.Length==1 && args[0]=="garita") return GaritaTests.Run();
            if(args.Length==1 && args[0]=="empresa") return Empresa();
            if(args.Length==1 && args[0]=="sql-garita") return GaritaTests.Sql();
            if(args.Length==1 && args[0]=="sql-panel") return PanelTests.Sql();
            if(args.Length==1 && args[0]=="sql-admin") {
                Console.WriteLine(PilotoAdministracionDA.SqlAutorizacion);
                var tipo=typeof(PilotoAdministracionDA);
                var da=(PilotoAdministracionDA)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(tipo);
                tipo.GetField("apk",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(da,"[TEST_RUTAS]");
                var editarSql=(string)tipo.GetProperty("SqlActualizarRuta",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(da,null);
                Check(editarSql.Contains("STATUS IN(N'A',N'E')") && editarSql.Contains("ISNULL(LIQUIDADO,0)=0"),"Edición protegida por estado y liquidación");
                Console.WriteLine(editarSql); return 0;
            }
            if(args.Length==1 && args[0]=="sql-rutas") {
                // Construye los predicados reales sin abrir una conexión ni leer configuración.
                var tipo=typeof(PilotoRutaDA);
                var filtroListado=tipo.GetMethod("FiltroEstadoListado",BindingFlags.Static|BindingFlags.NonPublic);
                foreach(var empresa in new[]{false,true}) {
                    var activas=(string)filtroListado.Invoke(null,new object[]{false,empresa,"activas"});
                    var historial=(string)filtroListado.Invoke(null,new object[]{false,empresa,"historial"});
                    Check(activas.Contains("r.STATUS=N'E'") && !activas.Contains("N'A'"),"Listado activo solo incluye En ruta");
                    Check(historial.Contains("N'C',N'X'") && !historial.Contains("N'A'"),"Historial excluye Abierta");
                    Check(activas.Contains("FECHA_RUTA")!=empresa,"El límite de fecha depende del tipo de cuenta");
                }
                Check(!((string)filtroListado.Invoke(null,new object[]{true,true,"activas"})).Contains("N'A'"),"Ruta fija tampoco incluye Abierta");
                var da=(PilotoRutaDA)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(tipo);
                tipo.GetField("apk",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(da,"[TEST_RUTAS]");
                var horaSql=(string)tipo.GetProperty("SqlRegistrarHoraEntrega",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(da,null);
                Check(horaSql.Contains("COALESCE(MO_HR_ENTRADA,@hora)") && horaSql.Contains("MO_ENTREGA=N'ENTREGADO'"),"La hora conserva valores existentes y solo aplica a entregados");
                Console.WriteLine(horaSql);
                var accesoTipo=tipo.GetNestedType("Acceso",BindingFlags.NonPublic);
                foreach(var modo in new[]{"normal","empresa","placa","ruta"}) {
                    var acceso=Activator.CreateInstance(accesoTipo,true);
                    if(modo=="empresa") accesoTipo.GetField("Empresa").SetValue(acceso,true);
                    if(modo=="placa") accesoTipo.GetField("PlacaPrueba").SetValue(acceso,"DEMO");
                    if(modo=="ruta") accesoTipo.GetField("RutaPrueba").SetValue(acceso,"DEMO-1");
                    foreach(var metodo in new[]{"Alcance","AlcanceDetalle"}) {
                        var filtro=(string)tipo.GetMethod(metodo,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(da,new[]{acceso});
                        Check(filtro.Contains("ISNULL(r.LIQUIDADO,0)=0"),"liquidadas excluidas en "+modo+" / "+metodo);
                        if(metodo=="AlcanceDetalle") Check(filtro.StartsWith("r.STATUS IN(N'E',N'C',N'X') AND "),"abiertas sin acceso directo en "+modo);
                        if(modo=="empresa") Check(filtro.Contains("PilotoEmpresaPlaca") && filtro.Contains("ep.Usuario_Id=@usuario") && !filtro.Contains("r.PILOTO=@piloto") && !filtro.Contains("PilotoCentro"),"empresa autorizada por placa propietaria sin empleado ni centro");
                        if(modo=="normal") Check(filtro.Contains("r.PILOTO=@piloto") && filtro.Contains("PilotoCentro"),"piloto individual conserva empleado y centro");
                        Console.WriteLine("SELECT r.ID_RUTA FROM [TEST_RUTAS].dbo.RT_RUTAS r WHERE "+filtro+";");
                    }
                }
                return 0;
            }
            if(args.Length==1 && args[0]=="escrituras-bloqueadas") {
                var da=new PilotoRutaDA();
                Rechaza(()=>da.GuardarImagen("consulta_demo",null),403,"cierre apagado bloquea foto de documento antes de SQL");
                Rechaza(()=>da.GuardarImagenCliente("consulta_demo",null),403,"cierre apagado bloquea foto final antes de SQL");
                Rechaza(()=>da.GuardarBorrador("consulta_demo",null),403,"cierre apagado bloquea cliente antes de SQL");
                Rechaza(()=>da.Cerrar("consulta_demo",null),503,"cierre apagado bloquea cierre antes de SQL");
                Console.WriteLine("OK: "+comprobaciones+" escrituras bloqueadas sin SQL."); return 0;
            }
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
                Rechaza(()=>new PilotoRutaDA().GuardarImagen("CONSULTA_DEMO",null),403,"consulta temporal bloquea cargas antes de SQL");
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
            PilotoReglas.PrepararConsulta(grande,3);
            Check(grande.PuedeCerrar && grande.PaginaDocumentos==3 && grande.TamanoPaginaDocumentos==100,"ruta de 201 conserva cierre por bloques");
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
            Rechaza(()=>PilotoReglas.HuellaSolicitud(c),400,"formulario antiguo mantiene limite");
            c.Documentos=null;c.DesdeBorradores=true;
            var huellaGrande=PilotoReglas.HuellaCierreGuardado(c);
            Check(huellaGrande==PilotoReglas.HuellaCierreGuardado(c),"cierre por borradores mantiene idempotencia");
            c.Version="OTRA";Check(huellaGrande!=PilotoReglas.HuellaCierreGuardado(c),"version participa en idempotencia grande");
            var masiva=new PilotoRuta{TotalDocumentos=251,Estado="E",Id="MASIVA"};
            masiva.Documentos=Enumerable.Range(1,251).Select(i=>new PilotoDocumento{RowId=i,Cliente="MISMO CLIENTE",Direccion="DIRECCION"}).ToList();
            masiva.Version=PilotoReglas.Version(masiva);
            var partes=PilotoReglas.Grupos(masiva);
            Check(partes.Count==11 && partes.All(g=>g.Count<=25) && partes.SelectMany(g=>g).Select(doc=>doc.RowId).Distinct().Count()==251,"cliente grande se divide sin omitir documentos");
            masiva.Borradores=partes.Select(g=>new PilotoBorradorCliente{PrimerRowId=g[0].RowId,Completado=true,
                Documentos=g.Select(doc=>new PilotoResultado{RowId=doc.RowId,Visito=true,Entrega="ENTREGADO"}).ToList()}).ToList();
            Check(PilotoReglas.ResultadosGuardados(masiva).Count==251,"cierre grande reconstruye todos los resultados guardados");
            masiva.Borradores[10].Completado=false;Rechaza(()=>PilotoReglas.ResultadosGuardados(masiva),409,"bloque incompleto impide cierre");
            masiva.Borradores[10].Completado=true;masiva.Borradores[10].Documentos[0].RowId=999;
            Rechaza(()=>PilotoReglas.ResultadosGuardados(masiva),409,"documento ajeno en borrador impide cierre");
            masiva.Borradores[10].Documentos[0].RowId=251;masiva.Borradores.RemoveAt(0);
            Rechaza(()=>PilotoReglas.ResultadosGuardados(masiva),409,"bloque faltante impide cierre");
            var cierreMasivo=new PilotoCierre{RutaId=masiva.Id,Version=masiva.Version,Solicitud=Guid.NewGuid(),Documentos=masiva.Documentos.Select(doc=>new PilotoResultado{RowId=doc.RowId,Visito=true,Entrega="ENTREGADO"}).ToList()};
            PilotoReglas.ValidarCierre(masiva,cierreMasivo);Check(true,"se validan 251 resultados completos");
            cierreMasivo.Documentos.RemoveAt(0);Rechaza(()=>PilotoReglas.ValidarCierre(masiva,cierreMasivo),400,"cierre grande incompleto rechazado");
            var muchosClientes=new PilotoRuta{TotalDocumentos=251,PuedeCerrar=true};
            muchosClientes.Documentos=Enumerable.Range(1,251).Select(i=>new PilotoDocumento{RowId=i,Cliente="CLIENTE "+i,Direccion="DIRECCION"}).ToList();
            muchosClientes.TotalGrupos=PilotoReglas.Grupos(muchosClientes).Count;
            PilotoReglas.PrepararConsulta(muchosClientes,63);
            Check(muchosClientes.TotalGrupos==251 && muchosClientes.PaginaDocumentos==63 && !muchosClientes.HayMasDocumentos,
                "251 clientes pequeños llegan a la última página sin límite artificial de 200");
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
            var jpeg=new byte[]{0xFF,0xD8,0xFF,0xE0};
            Check(PilotoReglas.ValidarImagen("entrega.jpg",jpeg)=="image/jpeg","imagen JPG valida por firma");
            Rechaza(()=>PilotoReglas.ValidarImagen("falsa.png",jpeg),400,"extension y firma deben coincidir");
            Rechaza(()=>PilotoReglas.ValidarImagen("vacia.jpg",new byte[0]),400,"imagen vacia rechazada");
            Rechaza(()=>PilotoReglas.ValidarImagen("enorme.jpg",new byte[PilotoReglas.MaxImagenBytes+1]),400,"imagen excesiva rechazada");
            var upload=typeof(PilotoController).GetMethod("SubirImagen");
            Check(upload.IsDefined(typeof(HttpPostAttribute),true) && upload.IsDefined(typeof(ValidateAntiForgeryTokenAttribute),true),"carga de imagen POST con CSRF");
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
            PilotoAdminReglas.ValidarAcceso(true,true); Check(true,"cuenta disponible con permiso puede administrar");
            Rechaza(()=>PilotoAdminReglas.ValidarAcceso(true,false),403,"cuenta sin Pilotos.Administrar rechazada");
            Rechaza(()=>PilotoAdminReglas.ValidarAcceso(false,true),403,"cuenta inexistente, duplicada o inactiva rechazada aunque tenga permiso");
            Rechaza(()=>PilotoAdminReglas.ValidarAcceso(true,false),403,"revocacion de permiso no conserva autorizacion previa");
            var adminHttp=new ContextoPrueba("consulta_demo"); var adminController=new PilotoController();
            adminController.ControllerContext=new ControllerContext(adminHttp,new RouteData(),adminController);
            var adminAction=new ReflectedActionDescriptor(typeof(PilotoController).GetMethod("Administracion"),"Administracion",new ReflectedControllerDescriptor(typeof(PilotoController)));
            var adminFilter=new ActionExecutingContext(adminController.ControllerContext,adminAction,new Dictionary<string,object>());
            typeof(PilotoController).GetMethod("OnActionExecuting",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(adminController,new object[]{adminFilter});
            Check(adminFilter.Result==null && object.Equals(adminController.ViewData["EsAdministracion"],true),"administracion no exige sesion especial de piloto");
            Console.WriteLine("OK: "+comprobaciones+" comprobaciones de reglas, paginacion, formulario y desafio."); return 0;
        } catch(Exception e) { Console.Error.WriteLine(e); return 1; }
    }

    static int Empresa()
    {
        Check(PilotoEmpresaReglas.NitCoincide("1234567-8") && PilotoEmpresaReglas.NitCoincide("123456K"),"NIT válido como login POS");
        Check(!PilotoEmpresaReglas.NitCoincide("transportista") && !PilotoEmpresaReglas.NitCoincide(""),"cuenta empresarial exige NIT");
        Check(PilotoEmpresaReglas.Conductor("  JUAN PEREZ  ")=="JUAN PEREZ","nombre del conductor normalizado");
        Rechaza(()=>PilotoEmpresaReglas.Conductor("X"),400,"conductor incompleto");
        Rechaza(()=>PilotoEmpresaReglas.Documento("X"),400,"documento incompleto");
        Check(PilotoEmpresaReglas.PuedeAsignarConductor(true,null,null),"Distribución puede recuperar una ruta antigua que salió sin conductor si no tiene actividad");
        Check(!PilotoEmpresaReglas.PuedeAsignarConductor(true,"JUAN PEREZ","L-1234"),"conductor completo no se cambia tras salida de Garita");
        var placas=new[]{"C-123","P-789"};
        Check(PilotoEmpresaReglas.SeleccionarPlaca(placas," c-123 ")=="C-123","placa se normaliza dentro de la empresa");
        Check(PilotoEmpresaReglas.SeleccionarPlaca(placas,null)==null,"sin placa aún no se listan rutas");
        Rechaza(()=>PilotoEmpresaReglas.SeleccionarPlaca(placas,"OTRA"),403,"placa de otra empresa denegada");
        var config=new PilotoAdminGuardar{UsuarioId=1,EmpleadoRowId=2,CodigoOperador="OPERADOR",Centros=new[]{"PALIN"},Placas=new[]{"C-123"},Activo=true,CuentaEmpresa=true,Motivo="Alta empresa"};
        Rechaza(()=>PilotoAdminReglas.Validar(config),400,"empresa sin nombre rechazada");
        config.NombreEmpresa="Transporte Demo";PilotoAdminReglas.Validar(config);
        Check(config.NombreEmpresa=="Transporte Demo" && !config.EmpleadoRowId.HasValue && config.Centros.Length==0,"empresa válida usa placas y prescinde de empleado y centros");
        var empresaSinEmpleado=new PilotoAdminGuardar{UsuarioId=2,CodigoOperador="NIT123",Centros=new string[0],Placas=new[]{"C-456"},Activo=true,CuentaEmpresa=true,NombreEmpresa="Otra Empresa",Motivo="Alta empresa"};
        PilotoAdminReglas.Validar(empresaSinEmpleado);Check(true,"empresa activa no exige empleado ni centro");
        var usuarioEmpresa=new PilotoAdminUsuario{UsuarioActivo=true,AutenticarSite=true,TieneRolPiloto=true,VinculoActivo=true,CuentaEmpresa=true,Vehiculos=2,Centros=0};
        Check(usuarioEmpresa.Listo,"cuenta empresarial configurada con placas no exige centro");
        usuarioEmpresa.CuentaEmpresa=false;Check(!usuarioEmpresa.Listo,"piloto individual sí exige centro");
        empresaSinEmpleado.Placas=new string[0];Rechaza(()=>PilotoAdminReglas.Validar(empresaSinEmpleado),400,"empresa activa exige placa");
        var ruta=Ruta();ruta.CuentaEmpresa=true;ruta.ConductorReal="JUAN PEREZ";ruta.DocumentoConductor="L-1234";
        ruta.PermisoConfirmar=true;
        Check(PilotoReglas.PuedeOperar(ruta,true,false),"empresa asignada puede registrar clientes, fotos y cierre");
        ruta.DocumentoConductor=null;
        Check(PilotoReglas.PuedeOperar(ruta,true,false),"cuenta empresarial opera sin documento adicional");
        ruta.DocumentoConductor="L-1234";ruta.PermisoConfirmar=false;
        Check(!PilotoReglas.PuedeOperar(ruta,true,false),"cuenta sin permiso de confirmar queda en consulta");
        ruta.PermisoConfirmar=true;
        Check(!PilotoReglas.PuedeOperar(ruta,false,false) && !PilotoReglas.PuedeOperar(ruta,true,true),"cierre apagado o ruta liquidada bloquean escrituras");
        ruta.Estado="C";Check(!PilotoReglas.PuedeOperar(ruta,true,false),"ruta cerrada no permite entregas nuevas");
        ruta.Estado="E";ruta.CuentaEmpresa=false;ruta.ConductorReal=null;ruta.DocumentoConductor=null;
        Check(PilotoReglas.PuedeOperar(ruta,true,false),"piloto individual conserva operación sin asignación empresarial");
        ruta.CuentaEmpresa=true;ruta.ConductorReal="JUAN PEREZ";ruta.DocumentoConductor="L-1234";
        var version=PilotoReglas.Version(ruta);ruta.ConductorReal="PEDRO PEREZ";
        Check(PilotoReglas.Version(ruta)==version,"asignación opcional no invalida entregas guardadas");
        var garita=new GaritaRuta{Id="R-1",Fecha=new DateTime(2026,10,22),Estado="E",Piloto="EMPRESA DEMO",Placa="C-123",Centro="PALIN",VehiculoDisponible=true,EsRutaEmpresa=true};
        garita.Documentos.Add(new PilotoDocumento{RowId=1,Cliente="CLIENTE",Bultos=1});
        Check(garita.PuedeRegistrarSalida,"garita verifica ruta empresarial usando piloto de APK66");
        garita.ConductorReal="JUAN PEREZ";garita.DocumentoConductor="L-1234";
        Check(garita.PuedeRegistrarSalida,"garita admite ruta empresarial asignada");
        var versionGarita=GaritaReglas.Version(garita);garita.ConductorReal="PEDRO PEREZ";
        Check(GaritaReglas.Version(garita)!=versionGarita,"cambio de conductor invalida revisión de garita");
        var panel=new PilotoPanelPiloto{CuentaActiva=true,RolPiloto=true,PermisoVer=true,VinculoActivo=true,EmpleadoActivo=true,EmpleadoUnico=true,Operador="OPERADOR",EmpresaVigente=false};
        Check(!panel.PuedeConsultar,"empresa revocada no aparece con acceso operativo en panel");
        panel.CuentaEmpresa=true;panel.EmpresaVigente=true;panel.EmpleadoActivo=false;panel.EmpleadoUnico=false;panel.PlacasDisponibles.Add("C-123");
        Check(panel.PuedeConsultar && panel.Coincide(new PilotoRuta{Placa="C-123",Piloto="OTRO PILOTO",Centro="OTRO CENTRO"}),"empresa coincide por placa aunque piloto y centro difieran");
        Check(!panel.Coincide(new PilotoRuta{Placa="C-789",Piloto="OTRO PILOTO",Centro="OTRO CENTRO"}),"empresa no coincide con otra placa");
        var accion=typeof(PilotoController).GetMethod("AsignarConductor");
        Check(accion.IsDefined(typeof(HttpPostAttribute),true) && accion.IsDefined(typeof(ValidateAntiForgeryTokenAttribute),true),"asignación administrativa protegida contra CSRF");
        Console.WriteLine("OK empresa: "+comprobaciones+" comprobaciones");return 0;
    }
}
