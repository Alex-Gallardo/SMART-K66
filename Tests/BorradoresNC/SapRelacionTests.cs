using System;
using System.Collections.Generic;
using System.Linq;
using DiamDev.Give.BLL;
using DiamDev.Give.Entities;

internal static class SapRelacionTests
{
    static DocumentoPrevioSap Nota(string factura, int entry, string clase = "NOTA_CREDITO",
        string cliente = "CL1", bool cancelada = false)
    {
        return new DocumentoPrevioSap { Factura = factura, DocEntry = entry, CardCode = cliente,
            Clase = clase, Cancelado = cancelada, Documento = entry.ToString() };
    }
    static void Exigir(bool condicion, string mensaje)
    {
        if (!condicion) throw new Exception(mensaje);
    }
    public static int Main()
    {
        var borrador = new BorradorNcEncabezado { IdEmpresa = "GRACO", IdCliente = "CL1" };
        borrador.Detalles.Add(new BorradorNcDetalle { Documento = "100" });
        borrador.Detalles.Add(new BorradorNcDetalle { Documento = "200" });
        var documentos = new[] {
            Nota("100", 1), Nota("100", 1), // NC y NC RECON de la misma factura
            Nota("200", 1), // Una misma NC ligada a dos facturas del borrador
            Nota("100", 2, cancelada: true),
            Nota("100", 3, clase: "DEVOLUCION"),
            Nota("100", 4, cliente: "OTRO"), Nota("999", 5)
        };
        var notas = BorradorNcBLL.NotasCreditoDelBorrador(borrador, documentos);
        Exigir(notas.Count == 3, "Debe conservar las relaciones únicas y excluir otra clase/cliente/factura.");
        var unicas = notas.GroupBy(x => x.DocEntry).Select(g => g.First()).ToList();
        Exigir(unicas.Count(x => !x.Cancelado) == 1, "Una NC vigente no se cuenta dos veces entre facturas.");
        Exigir(unicas.Count(x => x.Cancelado) == 1, "Una NC cancelada no debe considerarse vigente.");
        Exigir(BorradorNcBLL.NotasCreditoDelBorrador(borrador, null).Count == 0, "Sin documentos.");
        Exigir(BorradorNcBLL.NotasCreditoDelBorrador(new BorradorNcEncabezado(), documentos).Count == 0,
            "Sin facturas no hay relaciones.");
        Exigir(borrador.Detalles.Count == 2, "La consulta no modifica las facturas del borrador.");
        Console.WriteLine("OK C#: NC por factura, duplicados, múltiples facturas, canceladas y aislamiento del cliente.");
        return 0;
    }
}
