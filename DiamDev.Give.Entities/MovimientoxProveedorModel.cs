using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class MovimientoxProveedorModel
    {
        public long MovimientoId { get; set; }

        public string Documento { get; set; }
        
        public DateTime Fecha { get; set; }

        public int? DiasCredito { get; set; }

        public DateTime? FechaVencimiento { get; set; }
        
        public decimal Monto { get; set; }
    }
}
