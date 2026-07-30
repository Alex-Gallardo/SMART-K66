using System;

namespace DiamDev.Give.Entities
{
    /// <summary>
    /// Equivale a REC_CAJA_DET.
    /// Un registro por cada fila del grid derecho (facturas, anticipos, pedidos...)
    /// </summary>
    public class ReciboCajaDetalle
    {
        public string IdRecibo { get; set; }
        public string IdEmpresa { get; set; }
        public string TipoDoc { get; set; }
        public string NoDocumento { get; set; }  // NULL si ANTICIPO o SALDO PENDIENTE
        public DateTime? FechaDoc { get; set; }  // NULL si ANTICIPO o SALDO PENDIENTE
        public string Status { get; set; }  // NULL si ANTICIPO o SALDO PENDIENTE
        public decimal Monto { get; set; }
        public string Moneda { get; set; }
        public decimal MontoFact { get; set; }
        public decimal Pagado { get; set; }
        public string FelSerie { get; set; }
        public string FelNumero { get; set; }

        // ── NUEVO: dual-moneda del MONTO ──
        public decimal? TipoCambio { get; set; }
        public decimal MontoGtq { get; set; }
        public decimal MontoUsd { get; set; }
    }
}
