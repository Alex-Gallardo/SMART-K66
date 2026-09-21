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
            var previews=new Dictionary<string,string>(); Type layout=null;
            RouteTable.Routes.MapRoute("Default","{controller}/{action}/{id}",new {action="Index",id=UrlParameter.Optional});
            foreach(var relative in new[]{"Piloto/Index.cshtml","Piloto/Detalle.cshtml","Piloto/Error.cshtml","Seguridad/PilotoToken.cshtml","Shared/_PilotoLayout.cshtml"}) {
                string file=Path.Combine(root,"DiamDev.Give.UI/Views/"+relative);
                var host=new MvcWebPageRazorHost("~/Views/"+relative,file);
                host.NamespaceImports.Add("System"); host.NamespaceImports.Add("System.Linq");
                host.NamespaceImports.Add("System.Web.Mvc"); host.NamespaceImports.Add("System.Web.Mvc.Html");
                var engine=new RazorTemplateEngine(host);
                System.Web.Razor.GeneratorResults code;
                var source=File.ReadAllText(file);
                if(args.Length>1 && relative=="Shared/_PilotoLayout.cshtml") source=source.Replace("@RenderBody()","@Html.Raw(ViewBag.PreviewBody)").Replace("@RenderSection(\"scripts\", required: false)","");
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
                        if(relative=="Piloto/Index.cshtml") {
                            previews["index"]=RazorPreview.Render(type,RazorPreview.Lista());
                            previews["fija"]=RazorPreview.Render(type,RazorPreview.Lista(true));
                            previews["vacia"]=RazorPreview.Render(type,RazorPreview.Lista(false,true));
                            previews["historial"]=RazorPreview.Render(type,RazorPreview.Lista(false,false,"historial"));
                        }
                        if(relative=="Piloto/Detalle.cshtml") {
                            previews["detalle"]=RazorPreview.Render(type,RazorPreview.Ruta());
                            previews["detalle-historial"]=RazorPreview.Render(type,RazorPreview.Ruta(),null,"historial");
                        }
                        if(relative=="Piloto/Error.cshtml") previews["error"]=RazorPreview.Render(type,null);
                    }
                }
                Console.WriteLine("OK Razor: "+relative);
            }
            if(args.Length>1) foreach(var preview in previews) File.WriteAllText(Path.Combine(bin,preview.Key+".html"),RazorPreview.Render(layout,null,preview.Value));
            return 0;
        } catch(Exception e) {Console.Error.WriteLine(e); return 1;}
    }
}
