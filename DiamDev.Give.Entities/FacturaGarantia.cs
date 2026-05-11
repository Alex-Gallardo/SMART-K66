using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class FacturaGarantia
    {
        public int MensajeId { get; set; }

        public long FacturaId { get; set; }

        public string Cliente { get; set; }

        public List<Producto> Productos { get; set; }
    }
}
