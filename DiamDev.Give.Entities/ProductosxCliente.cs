using System;

namespace DiamDev.Give.Entities
{
    public class ProductosxCliente
    {
        public DateTime Fecha { get; set; }

        public long ReciboId { get; set; }

        public string Documento { get; set; }

        public bool Factura { get; set; }

        public decimal Cantidad { get; set; }

        public decimal Precio { get; set; }
    }
}
