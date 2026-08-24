namespace DiamDev.Give.Entities
{
    /// <summary>
    /// Una línea del borrador (BORR_NC_DET): qué documento se acredita, por
    /// cuánto y por qué.
    /// </summary>
    public class BorradorNcDetalle
    {
        public string IdEmpresa { get; set; }
        public string Concepto { get; set; }
        public string Documento { get; set; }        // DocNum de SAP
        public System.DateTime FechaDoc { get; set; }
        public string SerieFel { get; set; }         // U_SERIE_FACE
        public string NumeroFel { get; set; }        // U_NUMERO_DOCUMENTO
        public decimal TotalFactura { get; set; }    // DocTotal

        /// <summary>PaidToDate de SAP al capturar. NO reduce el tope
        /// (una factura pagada admite NC por devolución), pero se guarda
        /// porque no se puede reconstruir después.</summary>
        public decimal Pagado { get; set; }

        /// <summary>Suma de NC previas en SAP (INF_VRC_FACRNC) al capturar.
        /// Advertencia, no bloqueo — ver la nota en BorradorNcBLL.</summary>
        public decimal NcPreviaSap { get; set; }

        public string Moneda { get; set; }
        public string Descripcion { get; set; }
        public decimal Importe { get; set; }
    }
}