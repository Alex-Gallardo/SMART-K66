using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class KardexMovimientoModel
    {
        public int TipoId { get; set; }

        public string Tipo { get; set; }

        public string Agencia { get; set; }

        public DateTime Fecha { get; set; }

        public DateTime FechaHora { get; set; }

        public string Producto { get; set; }

        public long DocumentoId { get; set; }

        public decimal Cantidad { get; set; }

        public decimal Precio { get; set; }

        public decimal ExistenciaActual { get; set; }
        
        public decimal ExistenciaFinal { get; set; }
    }
}
