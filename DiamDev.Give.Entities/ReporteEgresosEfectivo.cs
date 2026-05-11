using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class ReporteEgresosEfectivo
    {
        public long GastoId { get; set; }

        public DateTime Fecha { get; set; }

        public string Agencia { get; set; }

        public string Categoria { get; set; }

        public string Concepto { get; set; }

        public string Documento { get; set; }

        public string Responsable { get; set; }
    
        public decimal Monto { get; set; }
    }
}
