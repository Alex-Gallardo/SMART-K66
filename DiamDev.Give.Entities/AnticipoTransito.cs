namespace DiamDev.Give.Entities
{
    /// <summary>
    /// Resumen de anticipos EN TRÁNSITO de un cliente (recibos activos aún no
    /// operados en SAP). Alimenta la barra informativa del modal de documentos.
    /// En TS: type AnticipoTransito = { gtq: number; usd: number; recibos: string }
    /// </summary>
    public class AnticipoTransito
    {
        public decimal Gtq { get; set; }        // suma de duales MONTO_GTQ
        public decimal Usd { get; set; }        // suma de duales MONTO_USD
        public string Recibos { get; set; }     // "RG07-03416, RG07-03420"
        public int Cantidad { get; set; }
    }
}