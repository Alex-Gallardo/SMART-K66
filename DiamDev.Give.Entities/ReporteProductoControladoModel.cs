using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class ReporteProductoControladoModel
    {
        public string Agencia { get; set; }

        public string Nit { get; set; }

        public string Cliente { get; set; }

        public DateTime Fecha { get; set; }

        public string Serie { get; set; }

        public long Factura { get; set; }

        public string Codigo { get; set; }

        public string Producto { get; set; }

        public decimal Cantidad { get; set; }
    }
}
