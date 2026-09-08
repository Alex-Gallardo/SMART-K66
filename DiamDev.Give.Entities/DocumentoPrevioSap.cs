using System;
using System.Collections.Generic;

namespace DiamDev.Give.Entities
{
    /// <summary>
    /// Nota de crédito o devolución de clientes emitida previamente en SAP y
    /// relacionada con una factura incluida en un borrador NC.
    /// </summary>
    public class DocumentoPrevioSap
    {
        public DocumentoPrevioSap()
        {
            Lineas = new List<DocumentoPrevioDetalleSap>();
        }

        public string Clase { get; set; }             // NOTA_CREDITO / DEVOLUCION
        public string TiposOrigen { get; set; }       // Tipos hallados en INF_VRC_FACRNC
        public string Factura { get; set; }
        public string Documento { get; set; }
        public int DocEntry { get; set; }
        public string ObjType { get; set; }
        public string Estado { get; set; }
        public bool Cancelado { get; set; }
        public string TipoDocumento { get; set; }
        public DateTime Fecha { get; set; }
        public DateTime FechaDocumento { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string Referencia { get; set; }
        public string Moneda { get; set; }
        public decimal TipoCambio { get; set; }
        public decimal Total { get; set; }
        public string Origen { get; set; }
        public string Comentarios { get; set; }
        public string SerieFel { get; set; }
        public string NumeroFel { get; set; }
        public string UrlPdf { get; set; }
        public List<DocumentoPrevioDetalleSap> Lineas { get; set; }
    }

    /// <summary>Renglón original de ORIN/RIN1 o ORDN/RDN1.</summary>
    public class DocumentoPrevioDetalleSap
    {
        public int NumeroLinea { get; set; }
        public string CodigoArticulo { get; set; }
        public string Descripcion { get; set; }
        public decimal Cantidad { get; set; }
        public string UnidadMedida { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal DescuentoPorcentaje { get; set; }
        public decimal Subtotal { get; set; }
        public string CodigoImpuesto { get; set; }
        public decimal ImpuestoPorcentaje { get; set; }
        public decimal Impuesto { get; set; }
        public decimal Total { get; set; }
        public string Moneda { get; set; }
        public string Bodega { get; set; }
        public string EstadoLinea { get; set; }
        public int TipoBase { get; set; }
        public int EntradaBase { get; set; }
        public int LineaBase { get; set; }
        public string ReferenciaBase { get; set; }
    }
}
