namespace DiamDev.Give.Entities
{
    /// <summary>
    /// Fila de INF_VRC_FACRNC (SAP HANA): una nota de crédito o devolución
    /// ya emitida contra una factura.
    ///
    /// El desktop consultaba esta vista solo en la pantalla de autorización,
    /// como información para el ojo humano. Aquí además se usa para calcular
    /// el disponible neto y para dejar constancia en BORR_NC_DET.NC_PREVIA_SAP.
    /// </summary>
    public class NotaCreditoPreviaSap
    {
        public string Tipo { get; set; }             // 'NC', ...
        public string Factura { get; set; }          // DocNum de la factura
        public string Nota { get; set; }             // DocNum de la NC
        public System.DateTime Fecha { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string Moneda { get; set; }
        public decimal Total { get; set; }
        public string Origen { get; set; }           // JrnlMemo
        public string Comentarios { get; set; }      // Comments
    }
}