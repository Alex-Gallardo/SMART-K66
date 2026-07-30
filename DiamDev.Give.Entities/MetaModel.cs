using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class MetaModel
    {
        public Vendedor Vendedor { get; set; }

        public VendedorMeta Meta { get; set; }

        public VendedorMetaxDia MetaxDia { get; set; }

        public decimal Comision { get; set; }

        public decimal MontoMeta { get; set; }

        public decimal MontoVenta { get; set; }

        public decimal MontoFaltante { get; set; }

        public bool VentaxDia { get; set; }
    }
}
