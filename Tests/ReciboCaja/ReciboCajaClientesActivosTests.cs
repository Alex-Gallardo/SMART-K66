using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Linq;
using DiamDev.Give.DAL;
using DiamDev.Give.Entities;

internal static class ReciboCajaClientesActivosTests
{
    private static int _pruebas;

    private static void Verificar(bool condicion, string caso)
    {
        if (!condicion) throw new Exception(caso);
        _pruebas++;
    }

    private static DataTable Respuesta(params string[] codigos)
    {
        var tabla = new DataTable();
        tabla.Columns.Add("CardCode", typeof(string));
        foreach (string codigo in codigos) tabla.Rows.Add(codigo);
        return tabla;
    }

    public static int Main()
    {
        var inactivo = new ClienteHana { CardCode = "CL0418", CardName = "INDUSTRIA PANIFICADORA EL ANGEL, S.A." };
        var activo = new ClienteHana { CardCode = "CL0406", CardName = inactivo.CardName, Currency = "GTQ" };
        int consultas = 0;
        var filtrados = ReciboCajaClientesActivos.Filtrar("SBO_GRACO",
            new[] { inactivo, activo }, (sql, parametros) => {
                consultas++;
                Verificar(sql.Contains("\"SBO_GRACO\".\"OCRD\"") &&
                    sql.Contains("\"CardType\" = 'C'") &&
                    sql.Contains("\"validFor\" = 'Y'"),
                    "La consulta debe exigir un cliente activo en la empresa elegida.");
                Verificar(parametros.Select(p => Convert.ToString(p.Value)).SequenceEqual(
                    new[] { "CL0418", "CL0406" }),
                    "Los códigos deben enviarse como parámetros posicionales.");
                return Respuesta("CL0406");
            });
        Verificar(consultas == 1 && filtrados.Count == 1 &&
            Object.ReferenceEquals(filtrados[0], activo),
            "Debe mostrar CL0406 y excluir CL0418 sin perder sus datos.");

        consultas = 0;
        filtrados = ReciboCajaClientesActivos.Filtrar("SBO_GRACO",
            new ClienteHana[0], (sql, parametros) => {
                consultas++;
                return Respuesta();
            });
        Verificar(filtrados.Count == 0 && consultas == 0,
            "Una lista vacía no debe consultar HANA.");

        var muchos = Enumerable.Range(0, 35).Select(i =>
            new ClienteHana { CardCode = "CL" + i.ToString("D4") }).ToList();
        filtrados = ReciboCajaClientesActivos.Filtrar("SBO_GRACO", muchos,
            (sql, parametros) => Respuesta("CL0034"));
        Verificar(filtrados.Count == 1 && filtrados[0].CardCode == "CL0034",
            "El filtro debe conservar un cliente activo al final del conjunto.");

        var loteGrande = Enumerable.Range(0, 201).Select(i =>
            new ClienteHana { CardCode = "X" + i }).ToList();
        var tamanos = new List<int>();
        filtrados = ReciboCajaClientesActivos.Filtrar("SBO_GRACO", loteGrande,
            (sql, parametros) => {
                tamanos.Add(parametros.Length);
                return Respuesta(parametros.Select(p => Convert.ToString(p.Value)).ToArray());
            });
        Verificar(tamanos.SequenceEqual(new[] { 200, 1 }) && filtrados.Count == 201,
            "Debe consultar en lotes y conservar el orden original.");

        bool fallo = false;
        try {
            ReciboCajaClientesActivos.Filtrar("SBO_GRACO", new[] { activo },
                (sql, parametros) => { throw new Exception("HANA no disponible"); });
        } catch (Exception ex) { fallo = ex.Message == "HANA no disponible"; }
        Verificar(fallo, "Un error de HANA no debe mostrar clientes sin verificar.");

        Console.WriteLine("OK: " + _pruebas + " verificaciones de clientes activos en recibos.");
        return 0;
    }
}
