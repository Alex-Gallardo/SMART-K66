using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class CorteCajaHistorial
    {
        public List<Recibo> Recibos { get; set; }

        public List<Factura> Facturas { get; set; }
        
        public List<Recibo> Abonos { get; set; }

        public List<Factura> FacturaAbonos { get; set; }

        public List<Reserva> Reservas { get; set; }

        public List<Reserva> ReservaAbonos { get; set; }

        public List<Gasto> Gastos { get; set; }

        public List<CorteCaja> Cortes { get; set; }
    }
}
