namespace DiamDev.Give.Entities
{
    /// <summary>
    /// Suma de lo aplicado en RCT2 para un pago (DocEntry de ORCT),
    /// en ambas monedas. Se usa en la conciliación de montos.
    /// </summary>
    public class MontoAplicadoSap
    {
        public int DocEntry { get; set; }
        public decimal MontoGTQ { get; set; }   // SUM(SumApplied)  - moneda local
        public decimal MontoUSD { get; set; }   // SUM(AppliedFC)   - moneda extranjera
    }
}