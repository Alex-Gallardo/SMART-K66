using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class ProductoInventarioModel
    {
        public string ProductoId { get; set; }       

        public long AgenciaId { get; set; }

        public string Agencia { get; set; }

        public string Codigo { get; set; }

        public string Nombre { get; set; }

        public string Unidad { get; set; }

        public decimal Existencia { get; set; }

        public decimal Precio { get; set; }
    }
}
