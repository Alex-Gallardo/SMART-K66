using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class ReporteVentaxProductoDiaVendedor
    {
        public string Agencia { get; set; }

        public string Vendedor { get; set; }

        public string ProductoId { get; set; }

        public string Codigo { get; set; }

        public string Nombre { get; set; }

        public string Marca { get; set; }

        public decimal Cantidad { get; set; }

        public decimal Costo { get; set; }

        public decimal Venta { get; set; }

        public decimal Promedio { get; set; }

        public DateTime Fecha { get; set; }
    }
}
