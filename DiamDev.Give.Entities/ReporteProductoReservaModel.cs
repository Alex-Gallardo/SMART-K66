using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class ReporteProductoReservaModel
    {
        public string Agencia { get; set; }

        public DateTime Fecha { get; set; }

        public long ReservaId { get; set; }

        public string Cliente { get; set; }

        public string Categoria { get; set; }

        public string Producto { get; set; }

        public decimal Cantidad { get; set; }

        public decimal Total { get; set; }

        public decimal TotalPagado { get; set; }
    }
}
