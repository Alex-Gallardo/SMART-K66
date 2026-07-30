using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class ProductoConsultaModel
    {
        public string ProductoId { get; set; }
        
        public string Nombre { get; set; }

        public decimal Existencia { get; set; }
    }
}
