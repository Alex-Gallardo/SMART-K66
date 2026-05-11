using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class VentaResumen
    {
        public long FormaId { get; set; }

        public long FacturaId { get; set; }

        public string Factura { get; set; }

        public DateTime Fecha { get; set; }

        public decimal Monto { get; set; }

        public decimal TC { get; set; }

        public decimal Efectivo { get; set; }

        public decimal EfectivoDolar { get; set; }

        public decimal Otros { get; set; }
    }
}
