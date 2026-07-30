using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class ReporteProductoLote
    {
        public string Agencia { get; set; }

        public string ProductoId { get; set; }
        
        public string Codigo { get; set; }

        public string Producto { get; set; }

        public string Lote { get; set; }

        public DateTime FechaVencimiento { get; set; }
        
        public decimal Cantidad { get; set; }
    }
}
