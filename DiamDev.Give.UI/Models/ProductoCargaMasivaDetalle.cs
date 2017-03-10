using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DiamDev.Give.UI.Models
{
    public class ProductoCargaMasivaDetalle
    {
        public string Bodega { get; set; }
        public double Cantidad { get; set; }
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public double Costo { get; set; }
        public double Id { get; set; }
        public string Marca { get; set; }
        public double Max { get; set; }
        public double Min { get; set; }
        public string Modificacion { get; set; }
        public double PrecioVenta { get; set; }
        public double RentP { get; set; }
        public double RentQ { get; set; }
    }
}