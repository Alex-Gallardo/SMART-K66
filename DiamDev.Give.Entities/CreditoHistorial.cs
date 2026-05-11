using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.Entities
{
    public class CreditoHistorial
    {
        public long CreditoId { get; set; }

        public string Tipo { get; set; }

        public string Agencia { get; set; }

        public string Cliente { get; set; }

        public string Descripcion { get; set; }

        public DateTime FechaInicial { get; set; }

        public DateTime FechaFinal { get; set; }

        public DateTime Fecha { get; set; }

        public bool Finalizado { get; set; }

        public decimal MontoCredito { get; set; }

        public decimal MontoCancelado { get; set; }
    }
}
