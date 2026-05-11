using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class CorrelativoModel
    {
        public long SerieId { get; set; }

        public long AgenciaId { get; set; }

        public long FacturaInicial { get; set; }

        public long FacturaFinal { get; set; }
    }
}
