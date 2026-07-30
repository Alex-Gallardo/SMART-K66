using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class MensajePedido
    {
        public int MensajeId { get; set; }

        public long ClienteId { get; set; }

        public long VendedorId { get; set; }
        
        public string Mensaje { get; set; }
        
        public string Pago { get; set; }
        
        public decimal Pendiente { get; set; }
    }
}
