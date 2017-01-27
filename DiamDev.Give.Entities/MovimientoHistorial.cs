using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class MovimientoHistorial
    {
        public long MovimientoId { get; set; }

        public long ProveedorId { get; set; }

        public string Proveedor { get; set; }

        public string Descripcion { get; set; }

        public DateTime Fecha { get; set; }

        public decimal Cantidad { get; set; }

        public decimal Precio { get; set; }
    }
}
