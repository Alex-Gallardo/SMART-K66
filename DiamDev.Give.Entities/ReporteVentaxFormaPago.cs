using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class ReporteVentaxFormaPago
    {
        public string Agencia { get; set; }

        public DateTime Fecha { get; set; }

        public string Documento { get; set; }

        public string Nit { get; set; }

        public string Nombre { get; set; }

        public string Forma { get; set; }

        public string Nota { get; set; }
        
        public decimal Monto { get; set; }
    }
}
