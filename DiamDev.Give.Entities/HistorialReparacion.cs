using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class HistorialReparacion
    {
        public long ReparacionId { get; set; }

        public string Agencia { get; set; }

        public DateTime Fecha { get; set; }

        public DateTime FechaAsignacion { get; set; }

        public DateTime? FechaFinalizacion { get; set; }
        
        public string Cliente { get; set; }

        public string Tecnico { get; set; }
                
        public decimal Total { get; set; }
    }
}
