using System;

namespace DiamDev.Give.Entities
{
    public class ReciboFechaPagoEstimadaModel
    {
        public long ReciboId { get; set; }

        public bool FacturaEnlazada { get; set; }

        public string Factura { get; set; }

        public string Vendedor { get; set; }

        public DateTime Fecha { get; set; }

        public string Cliente { get; set; }

        public decimal Total { get; set; }

        public decimal Abono { get; set; }

        public DateTime FechaPagoEstimada { get; set; }
    }
}
