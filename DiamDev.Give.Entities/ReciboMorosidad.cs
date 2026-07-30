using System;

namespace DiamDev.Give.Entities
{
    public class ReciboMorosidad
    {
        public long ReciboId { get; set; }

        public string Cliente { get; set; }

        public decimal Total { get; set; }

        public decimal Pagado { get; set; }

        public DateTime Fecha { get; set; }

        public int Dias { get; set; }

        public int Recibo { get; set; }
    }
}
