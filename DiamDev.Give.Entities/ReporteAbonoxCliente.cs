using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class ReporteAbonoxCliente
    {
        public long ReciboId { get; set; }

        public string Agencia { get; set; }

        public DateTime Fecha { get; set; }

        public string Cliente { get; set; }

        public string Responsable { get; set; }
        
        public decimal Monto { get; set; }
    }
}
