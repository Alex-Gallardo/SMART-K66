using System;

namespace DiamDev.Give.Entities
{
    /// <summary>
    /// Equivale a REC_CAJA_COBRO. 
    /// Un registro por cada fila del grid izquierdo (cheque, efectivo, transferencia...)
    /// </summary>
    public class ReciboCajaCobro
    {
        public string IdRecibo { get; set; }
        public string IdEmpresa { get; set; }
        public string TipoCobro { get; set; }
        public string Banco { get; set; }  // NULL si TipoCobro = EFECTIVO
        public DateTime? FechaDoc { get; set; }  // NULL si TipoCobro = EFECTIVO
        public string NoDocumento { get; set; }  // NULL si TipoCobro = EFECTIVO
        public decimal Monto { get; set; }
        public string Moneda { get; set; }
    }
}