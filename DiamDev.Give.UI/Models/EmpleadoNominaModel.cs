using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DiamDev.Give.UI.Models
{
    public class EmpleadoNominaModel
    {
        public long PersonalId { get; set; }

        public string Nombre { get; set; }

        public string Puesto { get; set; }

        public int Dias { get; set; }

        public decimal Sueldo { get; set; }

        public decimal Bonificacion { get; set; }

        public decimal OtrosIngresos { get; set; }

        public decimal IGSS { get; set; }

        public decimal OtrosDescuentos { get; set; }

        public decimal SubTotal { get; set; }
    }
}