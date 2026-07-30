using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class ReporteVentaComisionxVendedorConfigurable
    {
        public string Vendedor { get; set; }

        public DateTime Fecha { get; set; }

        public string Factura { get; set; }

        public string Producto { get; set; }

        public decimal Cantidad { get; set; }

        public decimal Precio { get; set; }

        public decimal Total { get; set; }

        public decimal Comision { get; set; }
        
        public int Valido { get; set; }
    }
}
