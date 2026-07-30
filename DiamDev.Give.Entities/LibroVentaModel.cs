using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class LibroVentaModel   
    {
        public DateTime Fecha { get; set; }

        public long AgenciaId { get; set; }

        public string Agencia { get; set; }

        public string TipoDocumento { get; set; }

        public string TipoTransaccion { get; set; }

        public long SerieId { get; set; }

        public string Serie { get; set; }

        public string NoFactura { get; set; }

        public long ClienteId { get; set; }

        public string Nit { get; set; }

        public string Nombre { get; set; }      

        public decimal Total { get; set; }

        public decimal TotalSinIva { get; set; }
    }
}
