using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class ReporteProveedorProducto
    {
        public long MovimientoId { get; set; }

        public string Documento { get; set; }

        public string Proveedor { get; set; }

        public string Codigo { get; set; }

        public string Categoria { get; set; }

        public string Producto { get; set; }

        public DateTime Fecha { get; set; }

        public decimal Costo { get; set; }

        public decimal Cantidad { get; set; }
    }
}
