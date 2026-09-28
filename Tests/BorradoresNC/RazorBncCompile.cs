using System;
using System.CodeDom.Compiler;
using System.IO;
using Microsoft.CSharp;
using System.Web.Mvc.Razor;
using System.Web.Razor;

internal static class RazorBncCompile
{
    public static int Main(string[] args)
    {
        string root = args[0], bin = Path.GetDirectoryName(typeof(RazorBncCompile).Assembly.Location);
        foreach (var vista in new[] { "Index", "Autorizaciones", "DashboardBNC", "DetalleFactura", "DetalleDocumentoPrevio" })
        {
            string relativa = "BorradorNc/" + vista + ".cshtml";
            string file = Path.Combine(root, "DiamDev.Give.UI/Views/" + relativa);
            var host = new MvcWebPageRazorHost("~/Views/" + relativa, file);
            foreach (var ns in new[] { "System", "System.Linq", "System.Web.Mvc", "System.Web.Mvc.Html" })
                host.NamespaceImports.Add(ns);
            GeneratorResults generated;
            using (var reader = new StreamReader(file)) generated = new RazorTemplateEngine(host).GenerateCode(reader);
            if (!generated.Success)
            {
                foreach (var error in generated.ParserErrors) Console.Error.WriteLine(relativa + ": " + error);
                return 1;
            }
            using (var compiler = new CSharpCodeProvider())
            {
                var options = new CompilerParameters { GenerateInMemory = true };
                foreach (var name in new[] { "System.dll", "System.Core.dll", "Microsoft.CSharp.dll", "System.Web.dll" })
                    options.ReferencedAssemblies.Add(name);
                foreach (var name in new[] { "System.Web.Mvc.dll", "System.Web.WebPages.dll", "System.Web.Helpers.dll",
                    "DiamDev.Give.Entities.dll", "DiamDev.Give.DAL.dll", "DiamDev.Give.BLL.dll", "BorradorNc.UI.dll" })
                    options.ReferencedAssemblies.Add(Path.Combine(bin, name));
                var result = compiler.CompileAssemblyFromDom(options, generated.GeneratedCode);
                if (result.Errors.HasErrors)
                {
                    foreach (CompilerError error in result.Errors) Console.Error.WriteLine(relativa + ": " + error);
                    return 1;
                }
            }
            Console.WriteLine("OK Razor: " + relativa);
        }
        return 0;
    }
}
