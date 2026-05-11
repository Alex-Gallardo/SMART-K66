using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class ClienteHistorial
    {
        public Cliente Cliente { get; set; }
        
        public List<Recibo> Recibos { get; set; }
    }
}
