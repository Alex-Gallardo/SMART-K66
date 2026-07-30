using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class EtiquetaModel
    {
        public string ProductoId{ get; set; }

        public string Codigo { get; set; }

        public string Barra { get; set; }

        public string Descripcion { get; set; }

        public decimal Precio { get; set; }
        
        public decimal Copia { get; set; }
    }
}
