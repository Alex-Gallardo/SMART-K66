using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.SqlServer.TransactSql.ScriptDom;
internal static class SqlCompile {
    public static int Main(string[] files) {
        int count=0;
        foreach(var file in files) {
            IList<ParseError> errors;
            var parser=new TSql150Parser(true);
            TSqlFragment tree;
            using(var reader=File.OpenText(file)) tree=parser.Parse(reader,out errors);
            foreach(var error in errors) Console.Error.WriteLine(Path.GetFileName(file)+":"+error.Line+" "+error.Message);
            if(errors.Count>0) return 1;
            // Valida tambien CREATE PROCEDURE anidado en sp_executesql.
            var visitor=new ProcedureLiteral(); tree.Accept(visitor);
            foreach(var literal in visitor.Values) {
                parser.Parse(new StringReader(literal),out errors);
                foreach(var error in errors) Console.Error.WriteLine(Path.GetFileName(file)+" SQL dinamico:"+error.Line+" "+error.Message);
                if(errors.Count>0) return 1;
            }
            count++; Console.WriteLine("OK SQL150: "+Path.GetFileName(file));
        }
        Console.WriteLine("Sintaxis: "+count+" archivos; no se ejecutaron consultas SQL."); return 0;
    }
    sealed class ProcedureLiteral : TSqlFragmentVisitor {
        public List<string> Values=new List<string>();
        public override void ExplicitVisit(StringLiteral node) {
            if(node.Value.TrimStart().StartsWith("CREATE PROCEDURE",StringComparison.OrdinalIgnoreCase)) Values.Add(node.Value);
        }
    }
}
