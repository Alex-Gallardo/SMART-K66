using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class ReporteVentaxTipoCliente
    {
        public string Agencia { get; set; }

        public string Tipo { get; set; }

        public string Cliente { get; set; }

        public DateTime Fecha { get; set; }

        public string Factura { get; set; }

        public string Formas { get; set; }
        
        public decimal Total { get; set; }
    }
}
