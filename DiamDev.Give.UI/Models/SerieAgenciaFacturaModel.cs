using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DiamDev.Give.UI.Models
{
    public class SerieAgenciaFacturaModel
    {
        public int Factura { get; set; }
        public Serie SerieId { get; set; }
        public Serie Serie { get; set; }
        public Agencia AgenciaId { get; set; }
        public Agencia Agencia { get; set; }
        public bool Operada { get; set; }
    }
}