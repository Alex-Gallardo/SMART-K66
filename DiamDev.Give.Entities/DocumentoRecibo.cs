using System;

namespace DiamDev.Give.Entities
{
    /// <summary>
    /// Resultado de consultar MA_RECC_DOCTOS.
    /// Representa un documento disponible para cobrar (factura, pedido, etc.)
    /// </summary>
    public class DocumentoRecibo
    {
        public string NoDocumento { get; set; }
        public DateTime FechaDoc { get; set; }
        public decimal MontoFact { get; set; }
        public decimal Pagado { get; set; }
        public decimal Saldo => MontoFact - Pagado;  // calculado
        public string Moneda { get; set; }
        public string FelSerie { get; set; }
        public string FelNumero { get; set; }
    }
}