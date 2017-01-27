using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class InventarioModel
    {
        public long ProductoId { get; set; }

        public string Nombre { get; set; }

        public decimal Precio { get; set; }

        public decimal PrecioCosto { get; set; }

        public decimal CantidadMinima { get; set; }

        public decimal Cantidad { get; set; }

        public decimal Existencia { get; set; }
    }
}
