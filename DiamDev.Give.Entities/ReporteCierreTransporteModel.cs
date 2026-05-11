using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class ReporteCierreTransporteModel
    {
        public string Nit { get; set; }

        public string Cliente { get; set; }

        public string Direccion { get; set; }

        public string Serie { get; set; }

        public long Factura { get; set; }

        public string Transporte { get; set; }

        public DateTime Fecha { get; set; }

        public DateTime FechaHoraFactura { get; set; }

        public decimal TotalFactura { get; set; }

        public decimal TotalMensajero { get; set; }
    }
}
