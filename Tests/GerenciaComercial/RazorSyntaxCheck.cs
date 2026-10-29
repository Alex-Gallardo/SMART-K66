using System;
using System.IO;
using System.Web.Mvc.Razor;
using System.Web.Razor;

internal static class RazorSyntaxCheck
{
    public static int Main(string[] args)
    {
        foreach (string relative in new[] { "GerenciaComercial/Index.cshtml", "Shared/_Layout.cshtml" })
        {
            string path = Path.Combine(args[0], "DiamDev.Give.UI", "Views", relative.Replace('/', Path.DirectorySeparatorChar));
            var host = new MvcWebPageRazorHost("~/Views/" + relative, path);
            GeneratorResults result;
            using (var reader = new StreamReader(path)) result = new RazorTemplateEngine(host).GenerateCode(reader);
            if (!result.Success)
            {
                foreach (var error in result.ParserErrors) Console.Error.WriteLine(relative + ": " + error);
                return 1;
            }
            Console.WriteLine("OK Razor: " + relative);
        }
        return 0;
    }
}
