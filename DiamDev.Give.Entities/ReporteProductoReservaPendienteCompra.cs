using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class ReporteProductoReservaPendienteCompra
    {
        public string Marca { get; set; }

        public string Codigo { get; set; }

        public string Producto { get; set; }

        public decimal Cantidad { get; set; }

        public decimal TotalPagado { get; set; }

        public decimal Existencia { get; set; }
    }
}
