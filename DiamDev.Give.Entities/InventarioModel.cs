using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class InventarioModel
    {
        public long AgenciaId { get; set; }

        public string Agencia { get; set; }
        
        public string ProductoId { get; set; }

        public string Codigo { get; set; }
        
        public string Nombre { get; set; }

        public string Unidad { get; set; }

        public string Marca { get; set; }

        public decimal PrecioVenta { get; set; }
        
        public decimal PrecioValidar { get; set; }
       
        public bool Activo { get; set; }
        
        public decimal Precio { get; set; }

        public decimal PrecioCosto { get; set; }

        public decimal CantidadMinima { get; set; }

        public decimal Cantidad { get; set; }

        public decimal Existencia { get; set; }

        public string Proveedor { get; set; }
        
        public bool TieneLote { get; set; }
    }
}
