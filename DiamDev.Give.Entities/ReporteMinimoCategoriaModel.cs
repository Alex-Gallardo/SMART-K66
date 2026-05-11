using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class ReporteMinimoCategoriaModel
    {
        public string Agencia { get; set; }

        public string Codigo { get; set; }

        public string Producto { get; set; }

        public decimal Existencia { get; set; }
        
        public int Minimo { get; set; }
    }
}
