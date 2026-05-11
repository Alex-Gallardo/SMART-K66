using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class CuentaModel
    {
        public string ProductoId { get; set; }

        public long UnidadId { get; set; }

        public string Producto { get; set; }

        public decimal Cantidad { get; set; }

        public decimal Precio { get; set; }
    }
}
