using System;
using System.CodeDom.Compiler;
using System.IO;
using Microsoft.CSharp;
using System.Web.Mvc.Razor;
using System.Web.Razor;

internal static class RazorCompile
{
    public static int Main(string[] args)
    {
        try {
            string root=args[0], bin=Path.GetDirectoryName(typeof(RazorCompile).Assembly.Location);
            foreach(var relative in new[]{"Piloto/Index.cshtml","Piloto/Detalle.cshtml","Piloto/Error.cshtml","Seguridad/PilotoToken.cshtml","Shared/_PilotoLayout.cshtml"}) {
                string file=Path.Combine(root,"DiamDev.Give.UI/Views/"+relative);
                var host=new MvcWebPageRazorHost("~/Views/"+relative,file);
                host.NamespaceImports.Add("System"); host.NamespaceImports.Add("System.Linq");
                host.NamespaceImports.Add("System.Web.Mvc"); host.NamespaceImports.Add("System.Web.Mvc.Html");
                var engine=new RazorTemplateEngine(host);
                System.Web.Razor.GeneratorResults code;
                using(var reader=new StreamReader(file)) code=engine.GenerateCode(reader);
                if(!code.Success) { foreach(var e in code.ParserErrors) Console.Error.WriteLine(relative+": "+e); return 1; }
                using(var compiler=new CSharpCodeProvider()) {
                    var options=new CompilerParameters {GenerateInMemory=true};
                    foreach(var name in new[]{"System.dll","System.Core.dll","Microsoft.CSharp.dll","System.Web.dll"}) options.ReferencedAssemblies.Add(name);
                    foreach(var name in new[]{"System.Web.Mvc.dll","System.Web.WebPages.dll","System.Web.Helpers.dll","PilotoTests.exe"}) options.ReferencedAssemblies.Add(Path.Combine(bin,name));
                    var result=compiler.CompileAssemblyFromDom(options,code.GeneratedCode);
                    if(result.Errors.HasErrors) { foreach(CompilerError e in result.Errors) Console.Error.WriteLine(relative+": "+e); return 1; }
                }
                Console.WriteLine("OK Razor: "+relative);
            }
            return 0;
        } catch(Exception e) {Console.Error.WriteLine(e); return 1;}
    }
}
