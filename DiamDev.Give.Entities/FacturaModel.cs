using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class FacturaModel
    {
        public long FacturaId { get; set; }

        public DateTime Fecha { get; set; }

        public string Agencia { get; set; }

        public long ClienteId { get; set; }

        public string Nombre { get; set; }

        public string Tipo { get; set; }

        public int Dias { get; set; }
              
        public string Forma { get; set; }
                
        public string Documento { get; set; }

        public string Usuario { get; set; }
        
        public decimal Descuento { get; set; }

        public decimal Total { get; set; }

        public decimal TotalLiquido { get; set; }

        public bool Anulada { get; set; }

    }
}
