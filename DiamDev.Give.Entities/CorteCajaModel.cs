using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class CorteCajaModel
    {
        public decimal TotalRecibos { get; set; }

        public decimal TotalAbonos { get; set; }

        public decimal TotalGastos { get; set; }
        
        public decimal TotalRetiros { get; set; }
        
        public decimal Disponible { get; set; }
    }
}
