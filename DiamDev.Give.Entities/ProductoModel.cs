using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class ProductoModel
    {
        public long SolicitudId { get; set; }

        public string ProductoId { get; set; }

        public string Agencia { get; set; }

        public string Nombre { get; set; }

        public DateTime Fecha { get; set; }

        public decimal Cantidad { get; set; }

        public decimal PrecioCosto { get; set; }

        public decimal PrecioVenta { get; set; }
    }
}
