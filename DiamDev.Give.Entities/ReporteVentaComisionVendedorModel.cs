using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class ReporteVentaComisionVendedorModel
    {
        public DateTime Fecha { get; set; }

        public string Nit { get; set; }

        public string Cliente { get; set; }

        public string Serie { get; set; }

        public long Factura { get; set; }

        public string Vendedor { get; set; }

        public decimal SubTotal { get; set; }

        public decimal Total { get; set; }
        
        public decimal Comision { get; set; }
    }
}
