using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class DiarioModel
    {
        public long DiarioId { get; set; }

        public int PartidaId { get; set; }

        public string Agencia { get; set; }

        public string Descripcion { get; set; }

        public DateTime Fecha { get; set; }

        public long CuentaId { get; set; }

        public string Cuenta { get; set; }

        public decimal Debe { get; set; }

        public decimal Haber { get; set; }
    }
}
