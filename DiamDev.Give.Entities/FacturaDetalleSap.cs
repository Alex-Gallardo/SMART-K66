namespace DiamDev.Give.Entities
{
    /// <summary>
    /// Renglón original de una factura de clientes en SAP Business One
    /// (OINV + INV1). Es información de consulta: no se persiste en SMART-K66.
    /// </summary>
    public class FacturaDetalleSap
    {
        public string Documento { get; set; }
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
    }
}
