using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class ConteoIngresos
    {
        public int CantidadIngresos { get; set; }
        
        public int CantidadIngresosxID { get; set; }

        public int CantidadPedidosSinOperar { get; set; }

        public int CantidadRecibosSinDespachar { get; set; }

        public int CantidadFacturasSinDespachar { get; set; }

        public int CantidadCuentaxCobrar { get; set; }
        
        public int CantidadEnvasesxRecibir { get; set; }
        
        public int CantidadPedidosCotizacion { get; set; }
    }
}
