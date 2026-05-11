using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class ProductoExistenciaModel
    {
        public string ID { get; set; }

        public string Codigo { get; set; }

        public long MarcaId { get; set; }

        public string Marca { get; set; }

        public string Descripcion { get; set; }

        public long AgenciaId { get; set; }

        public string Agencia { get; set; }

        public decimal Cantidad { get; set; }
        
        public decimal Total { get; set; }

        public decimal Costo { get; set; }

        public decimal Precio { get; set; }

        public int Minimo { get; set; }

        public int Maximo { get; set; }
        
        public string Estado { get; set; }
    }
}
