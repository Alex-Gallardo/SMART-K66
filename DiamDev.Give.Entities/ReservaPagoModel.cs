using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class ReservaPagoModel
    {
        public long ReservaId { get; set; }

        public long FormaId { get; set; }
        
        public decimal Monto { get; set; }
    }
}
