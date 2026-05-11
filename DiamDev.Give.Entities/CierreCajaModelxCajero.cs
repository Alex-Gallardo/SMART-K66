using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class CierreCajaModelxCajero
    {
        public Usuario Cajero { get; set; }

        public List<FormaPago> Formas { get; set; }

        public List<Cierre> Cierres { get; set; }

        public List<CorteCaja> Cortes { get; set; }
                
        public decimal TotalGastos { get; set; }

        public decimal TotalRetiros { get; set; }

        public decimal Sobrante { get; set; }
                
        public decimal Faltante { get; set; }
                                
        public bool Operado { get; set; }
    }
}
