using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.Routing;
using Microsoft.CSharp;
using System.Web.Mvc.Razor;
using System.Web.Razor;

internal static class RazorCompile
{
    public static int Main(string[] args)
    {
        try {
            string root=args[0], bin=Path.GetDirectoryName(typeof(RazorCompile).Assembly.Location);
            RazorPreview.AssetsRoot=root;
            var previews=new Dictionary<string,string>(); Type layout=null;Type panelContenido=null;
            RouteTable.Routes.MapRoute("Default","{controller}/{action}/{id}",new {action="Index",id=UrlParameter.Optional});
            foreach(var relative in new[]{"Piloto/Index.cshtml","Piloto/Detalle.cshtml","Piloto/Error.cshtml","Piloto/Administracion.cshtml","Piloto/Configurar.cshtml","Piloto/EditarRuta.cshtml","Piloto/AdministracionError.cshtml","Seguridad/PilotoToken.cshtml","Seguridad/GaritaToken.cshtml","Piloto/_PanelContenido.cshtml","Piloto/Panel.cshtml","Piloto/PanelDetalle.cshtml","Piloto/PanelError.cshtml","Shared/_PilotoLayout.cshtml","Shared/_Layout.cshtml","Garita/Index.cshtml","Garita/Error.cshtml","Garita/Administrar.cshtml","Shared/_GaritaLayout.cshtml"}) {
                string file=Path.Combine(root,"DiamDev.Give.UI/Views/"+relative);
                var host=new MvcWebPageRazorHost("~/Views/"+relative,file);
                host.NamespaceImports.Add("System"); host.NamespaceImports.Add("System.Linq");
                host.NamespaceImports.Add("System.Web.Mvc"); host.NamespaceImports.Add("System.Web.Mvc.Html");
                var engine=new RazorTemplateEngine(host);
                System.Web.Razor.GeneratorResults code;
                var source=File.ReadAllText(file);
                if(args.Length>1 && relative=="Shared/_PilotoLayout.cshtml") source=source.Replace("@RenderBody()","@Html.Raw(ViewBag.PreviewBody)").Replace("@RenderSection(\"scripts\", required: false)","");
                if(relative=="Shared/_Layout.cshtml") source=source.Replace("DiamDev.Give.UI.App_Start.CustomHelper.getAgenciaNombre()","\"Demo\"");
                if(args.Length>1 && relative=="Piloto/Panel.cshtml") source=source.Replace("@Html.Partial(\"_PanelContenido\",Model)","@Html.Raw(ViewBag.PanelBody)");
                if(args.Length>1 && relative.StartsWith("Piloto/Panel")) source=source.Replace("@section scripts { <script src=\"@Url.Content(\"~/Scripts/pilotos-panel.js\")\"></script> }","");
                if(args.Length>1 && relative=="Piloto/Configurar.cshtml") source=source.Replace("@section scripts { <script src=\"@Url.Content(\"~/Scripts/pilotos.js\")\"></script> }","");
                if(args.Length>1 && relative=="Piloto/EditarRuta.cshtml") source=source.Replace("@section scripts { <script src=\"@Url.Content(\"~/Scripts/pilotos-admin-ruta.js\")\"></script> }","");
                using(var reader=new StringReader(source)) code=engine.GenerateCode(reader);
                if(!code.Success) { foreach(var e in code.ParserErrors) Console.Error.WriteLine(relative+": "+e); return 1; }
                using(var compiler=new CSharpCodeProvider()) {
                    var options=new CompilerParameters {GenerateInMemory=true};
                    foreach(var name in new[]{"System.dll","System.Core.dll","Microsoft.CSharp.dll","System.Web.dll"}) options.ReferencedAssemblies.Add(name);
                    foreach(var name in new[]{"System.Web.Mvc.dll","System.Web.WebPages.dll","System.Web.Helpers.dll","PilotoTests.exe"}) options.ReferencedAssemblies.Add(Path.Combine(bin,name));
                    var result=compiler.CompileAssemblyFromDom(options,code.GeneratedCode);
                    if(result.Errors.HasErrors) { foreach(CompilerError e in result.Errors) Console.Error.WriteLine(relative+": "+e); return 1; }
                    if(args.Length>1) {
                        var type=result.CompiledAssembly.GetTypes().First(t=>typeof(WebViewPage).IsAssignableFrom(t));
                        if(relative=="Shared/_PilotoLayout.cshtml") layout=type;
                        if(relative=="Piloto/_PanelContenido.cshtml") panelContenido=type;
                        if(relative=="Piloto/Panel.cshtml") foreach(var seccion in new[]{"resumen","rutas","pilotos","documentos","actividad","alertas"}) {
                            var modelo=RazorPreview.Panel(seccion);previews["panel-"+seccion]=RazorPreview.Render(type,modelo,RazorPreview.Render(panelContenido,modelo));
                            if(seccion=="pilotos") {var admin=RazorPreview.Panel(seccion,true);previews["panel-admin"]=RazorPreview.Render(type,admin,RazorPreview.Render(panelContenido,admin));}
                        }
                        if(relative=="Piloto/Panel.cshtml") for(var pagina=1;pagina<=2;pagina++) {
                            var modelo=RazorPreview.PanelPaginado(pagina);
                            previews["panel-pagina-"+pagina]=RazorPreview.Render(type,modelo,RazorPreview.Render(panelContenido,modelo));
                        }
                        if(relative=="Piloto/Panel.cshtml") {var empresa=RazorPreview.PanelEmpresaPendiente();previews["panel-empresa-pendiente"]=RazorPreview.Render(type,empresa,RazorPreview.Render(panelContenido,empresa));}
                        if(relative=="Piloto/PanelDetalle.cshtml") previews["panel-detalle"]=RazorPreview.Render(type,RazorPreview.Panel());
                        if(relative=="Piloto/PanelDetalle.cshtml") previews["panel-detalle-empresa-pendiente"]=RazorPreview.Render(type,RazorPreview.PanelEmpresaPendiente());
                        if(relative=="Piloto/PanelError.cshtml") previews["panel-error"]=RazorPreview.Render(type,RazorPreview.Panel());
                        if(relative=="Piloto/Index.cshtml") {
                            previews["index"]=RazorPreview.Render(type,RazorPreview.Lista());
                            previews["fija"]=RazorPreview.Render(type,RazorPreview.Lista(true));
                            previews["vacia"]=RazorPreview.Render(type,RazorPreview.Lista(false,true));
                            previews["historial"]=RazorPreview.Render(type,RazorPreview.Lista(false,false,"historial"));
                            previews["index-empresa-sin-placa"]=RazorPreview.Render(type,RazorPreview.ListaEmpresa(false));
                            previews["index-empresa"]=RazorPreview.Render(type,RazorPreview.ListaEmpresa(true));
                        }
                        if(relative=="Piloto/Detalle.cshtml") {
                            previews["detalle"]=RazorPreview.Render(type,RazorPreview.Ruta());
                            previews["detalle-cierre"]=RazorPreview.Render(type,RazorPreview.RutaEditable(),null,"activas",true);
                            previews["detalle-borrador"]=RazorPreview.Render(type,RazorPreview.RutaConBorrador(),null,"activas",true);
                            previews["detalle-historial"]=RazorPreview.Render(type,RazorPreview.Ruta(),null,"historial");
                            previews["detalle-empresa-lista"]=RazorPreview.Render(type,RazorPreview.RutaEmpresa(true),null,"activas",true);
                            previews["detalle-empresa-pendiente"]=RazorPreview.Render(type,RazorPreview.RutaEmpresa(false),null,"activas",true);
                            previews["detalle-empresa-sin-permiso"]=RazorPreview.Render(type,RazorPreview.RutaEmpresa(true,false),null,"activas",true);
                            previews["detalle-empresa-grande"]=RazorPreview.Render(type,RazorPreview.RutaGrande(),null,"activas",true);
                        }
                        if(relative=="Piloto/Administracion.cshtml") previews["administracion"]=RazorPreview.Render(type,RazorPreview.AdminLista());
                        if(relative=="Piloto/Configurar.cshtml") {previews["configurar"]=RazorPreview.Render(type,RazorPreview.AdminEdicion());previews["configurar-empresa"]=RazorPreview.Render(type,RazorPreview.AdminEdicionEmpresa());}
                        if(relative=="Piloto/EditarRuta.cshtml") {previews["editar-ruta"]=RazorPreview.Render(type,RazorPreview.AdminRuta());previews["editar-ruta-empresa"]=RazorPreview.Render(type,RazorPreview.AdminRutaEmpresa());}
                        if(relative=="Piloto/AdministracionError.cshtml") previews["administracion-error"]=RazorPreview.Render(type,null);
                        if(relative=="Piloto/Error.cshtml") previews["error"]=RazorPreview.Render(type,null);
                        if(relative=="Garita/Index.cshtml") {
                            previews["garita-vacia"]=RazorPreview.Render(type,new DiamDev.Give.Entities.GaritaRuta());
                            var busqueda=new DiamDev.Give.Entities.GaritaRuta{PlacaBusqueda="C-123ABC",BusquedaRealizada=true,Pagina=1,TotalRutas=51,TotalCerradas=51,PaginaCerradas=1};
                            busqueda.Resultados.Add(new DiamDev.Give.Entities.GaritaRutaResumen{Id="PP26-GARITA",Placa="C-123ABC",Centro="PALIN",Piloto="PILOTO DEMO",Fecha=new DateTime(2026,10,20),Documentos=1,Clientes=1,Bultos=4});
                            busqueda.Resultados.Add(new DiamDev.Give.Entities.GaritaRutaResumen{Id="PP26-OTRA",Placa="C-123ABC",Centro="PALIN",Piloto="OTRO PILOTO",Fecha=new DateTime(2026,10,19),Documentos=2,Clientes=1,Bultos=8,SalidaRegistrada=true});
                            busqueda.ResultadosCerradas.Add(new DiamDev.Give.Entities.GaritaRutaResumen{Id="PP26-CERRADA",Placa="C-123ABC",Centro="PALIN",Piloto="PILOTO DEMO",Fecha=new DateTime(2026,10,18),Documentos=2,Clientes=1,Bultos=8,SalidaRegistrada=true});
                            previews["garita-lista"]=RazorPreview.Render(type,busqueda);
                            previews["garita-sin-resultados"]=RazorPreview.Render(type,new DiamDev.Give.Entities.GaritaRuta{PlacaBusqueda="X-000",BusquedaRealizada=true,Pagina=1});
                            var garita=new DiamDev.Give.Entities.GaritaRuta{Id="PP26-GARITA",Estado="E",Centro="PALIN",Piloto="PILOTO DEMO",Placa="C-123ABC",PlacaBusqueda="C-123ABC",Fecha=new DateTime(2026,10,20),VehiculoDisponible=true};
                            garita.Documentos.Add(new DiamDev.Give.Entities.PilotoDocumento{RowId=1,Tipo="FACTURA",Empresa="GRACO",Documento="FAC-001",Cliente="CLIENTE UNO",Direccion="GUATEMALA",Bultos=4});
                            garita.Version=DiamDev.Give.Entities.GaritaReglas.Version(garita);
                            garita.Version=DiamDev.Give.Entities.GaritaReglas.Version(garita);
                            garita.Diferencia="Faltan dos bultos";previews["garita-diferencia"]=RazorPreview.Render(type,garita);
                            garita.Diferencia=null;previews["garita-en-ruta"]=RazorPreview.Render(type,garita);
                            garita.EsRutaEmpresa=true;garita.ConductorReal=null;previews["garita-empresa-pendiente"]=RazorPreview.Render(type,garita);
                            garita.ConductorReal="JUAN PEREZ";garita.DocumentoConductor="L-1234";garita.Version=DiamDev.Give.Entities.GaritaReglas.Version(garita);previews["garita-empresa-asignada"]=RazorPreview.Render(type,garita);
                            garita.EsRutaEmpresa=false;garita.ConductorReal=null;garita.DocumentoConductor=null;
                            garita.SalidaUtc=new DateTime(2026,10,20,15,30,0,DateTimeKind.Utc);garita.Agente="GUARDIA";garita.TieneFotoSalida=true;previews["garita-salida"]=RazorPreview.Render(type,garita);
                            garita.Estado="C";garita.HoraSalidaApk=new TimeSpan(9,30,0);garita.Version=DiamDev.Give.Entities.GaritaReglas.Version(garita);previews["garita-cerrada"]=RazorPreview.Render(type,garita);
                            garita.HoraEntradaApk=new TimeSpan(17,15,0);garita.EntradaUtc=new DateTime(2026,10,20,23,15,0,DateTimeKind.Utc);garita.AgenteEntrada="GUARDIA";garita.Version=DiamDev.Give.Entities.GaritaReglas.Version(garita);previews["garita-entrada"]=RazorPreview.Render(type,garita);
                        }
                        if(relative=="Garita/Administrar.cshtml") {
                            var admin=new DiamDev.Give.Entities.GaritaAdministracion{Login="agente_demo",UsuarioId=1,Nombre="Agente Demo"};
                            admin.Centros.Add("PALIN");admin.Disponibles.Add("PALIN");admin.Disponibles.Add("GUATEMALA");
                            admin.Incidencias.Add(new DiamDev.Give.Entities.GaritaIncidencia{RutaId="PP26-GARITA",Centro="PALIN",Descripcion="Faltan dos bultos",FechaUtc=new DateTime(2026,10,20,15,30,0,DateTimeKind.Utc)});
                            previews["garita-admin"]=RazorPreview.Render(type,admin);
                        }
                    }
                }
                Console.WriteLine("OK Razor: "+relative);
            }
            if(args.Length>1) foreach(var preview in previews) {
                object model=preview.Key.StartsWith("panel-") ? (object)RazorPreview.Panel() : preview.Key=="administracion" ? (object)RazorPreview.AdminLista() : preview.Key=="configurar" ? (object)RazorPreview.AdminEdicion() : preview.Key=="configurar-empresa" ? (object)RazorPreview.AdminEdicionEmpresa() : preview.Key=="editar-ruta" ? (object)RazorPreview.AdminRuta() : preview.Key=="editar-ruta-empresa" ? (object)RazorPreview.AdminRutaEmpresa() : null;
                bool backoffice=preview.Key.StartsWith("panel-") || preview.Key=="administracion" || preview.Key.StartsWith("configurar") || preview.Key.StartsWith("editar-ruta") || preview.Key=="administracion-error";
                var html=preview.Key.StartsWith("garita-") ? preview.Value : backoffice ? RazorPreview.Backoffice(preview.Value,preview.Key.StartsWith("panel-")) : RazorPreview.Render(layout,model,preview.Value,"activas",preview.Key=="detalle-cierre" || preview.Key=="detalle-borrador" || preview.Key.StartsWith("detalle-empresa-"));
                if(preview.Key.StartsWith("editar-ruta")) html=html.Replace("/Scripts/pilotos.js","/Scripts/pilotos-admin-ruta.js");
                File.WriteAllText(Path.Combine(bin,preview.Key+".html"),html);
            }
            return 0;
        } catch(Exception e) {Console.Error.WriteLine(e); return 1;}
    }
}
