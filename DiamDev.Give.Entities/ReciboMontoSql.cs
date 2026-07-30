namespace DiamDev.Give.Entities
{
    /// <summary>Datos mínimos de un recibo en SQL para conciliar montos.</summary>
    public class ReciboMontoSql
    {
        public string IdRecibo { get; set; }
        public string Moneda { get; set; }      // "GTQ" | "USD"
        public decimal MontoTDoc { get; set; }  // total documentos aplicados (en su moneda)
    }
}