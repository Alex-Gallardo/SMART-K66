using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class HorarioModel
    {
        public long PersonaId { get; set; }

        public string Nombre { get; set; }     

        public DateTime Fecha { get; set; }

        public TimeSpan Entrada { get; set; }

        public TimeSpan? Salida { get; set; }
        
        public TimeSpan Laborado { get; set; }
    }
}
