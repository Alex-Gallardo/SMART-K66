using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class ReporteProductoReservado
    {
        public string Agencia { get; set; }

        public string Cliente { get; set; }

        public long ReservaId { get; set; }

        public DateTime Fecha { get; set; }

        public DateTime FechaPrimerAbono { get; set; }

        public string Producto { get; set; }

        public decimal Cantidad { get; set; }

        public decimal MontoAbonado { get; set; }
        
        public bool Operado { get; set; }
    }
}
