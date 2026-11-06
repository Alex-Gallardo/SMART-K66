using System;
using DiamDev.Give.Entities;
using DiamDev.Give.BLL;

namespace DiamDev.Give.Entities
{
    // El selector se prueba con datos en memoria, sin consultar producción.
    public class UsuarioEmpresa
    {
        public string Codigo { get; set; }
        public string DEPTO_RECIBO { get; set; }
    }
}

internal static class ReciboCajaDeptoResolverTests
{
    private static int _pruebas;

    private static void Igual(string esperado, string actual, string caso)
    {
        if (actual != esperado)
            throw new Exception(caso + ": se esperaba '" + esperado + "', se obtuvo '" + actual + "'.");
        _pruebas++;
    }

    public static int Main()
    {
        Igual("RODOLFO", ReciboCajaDeptoResolver.Resolver(
            " RODOLFO ", "RODOLFO DIAZ", _ => {
                throw new Exception("No debe consultar la serie del nombre si hay depto explícito.");
            }),
            "El depto explícito conserva prioridad");

        Igual("RODOLFO", ReciboCajaDeptoResolver.Resolver(
            null, " RODOLFO ", depto => depto == "RODOLFO"),
            "El operador anterior funciona si tiene serie en su empresa");

        Igual("", ReciboCajaDeptoResolver.Resolver(
            null, "RODOLFO", _ => false),
            "Una serie de otra empresa no habilita el operador");

        Igual("", ReciboCajaDeptoResolver.Resolver(
            " ", "RODOLFO DIAZ", depto => depto == "RODOLFO"),
            "No se adivina un depto distinto del nombre anterior");

        Igual("", ReciboCajaDeptoResolver.Resolver(
            null, " ", _ => { throw new Exception("No debe consultar una serie vacía."); }),
            "Un código vacío no habilita el operador");

        var soloAsignado = ReciboCajaOperadores.Elegir(new[] {
            new UsuarioEmpresa { Codigo = "12-RODOLFO", DEPTO_RECIBO = null }
        });
        Igual("1", soloAsignado.Count.ToString(),
            "El operador asignado sin depto permanece en el selector");
        Igual("", ReciboCajaDeptoResolver.Resolver(soloAsignado[0].DEPTO_RECIBO,
            "RODOLFO", _ => false),
            "El operador sin serie permanece visible pero no puede emitir");

        var duplicados = ReciboCajaOperadores.Elegir(new[] {
            new UsuarioEmpresa { Codigo = "12-RODOLFO", DEPTO_RECIBO = null },
            new UsuarioEmpresa { Codigo = " 12-rodolfo ", DEPTO_RECIBO = "RODOLFO" }
        });
        Igual("1", duplicados.Count.ToString(), "Los códigos equivalentes se deduplican");
        Igual("RODOLFO", duplicados[0].DEPTO_RECIBO,
            "Se conserva el registro con depto aunque no sea el primero");

        Igual("0", ReciboCajaOperadores.Elegir(new[] {
            new UsuarioEmpresa { Codigo = "  ", DEPTO_RECIBO = "RODOLFO" }
        }).Count.ToString(), "Un código vacío no habilita operadores");

        Console.WriteLine("OK: " + _pruebas + " pruebas de resolución de depto.");
        return 0;
    }
}
