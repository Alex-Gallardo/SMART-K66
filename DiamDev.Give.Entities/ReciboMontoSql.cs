namespace DiamDev.Give.Entities
{
    /// <summary>Datos mínimos de un recibo en SQL para conciliar montos.</summary>
    public class ReciboMontoSql
    {
        public string IdRecibo { get; set; }
        public string Moneda { get; set; }      // "GTQ" | "USD"
        public decimal MontoTDoc { get; set; }  // total documentos aplicados (en su moneda)

        /// <summary>
        /// REC_CAJA_ENC.MONTO_T_REC — dinero efectivamente RECIBIDO (total de cobros).
        /// Es el equivalente conceptual de ORCT.DocTotal y la única medida válida
        /// para decidir OPERADO/DESCUADRE. MontoTDoc queda como referencia informativa.
        /// </summary>
        public decimal MontoTRec { get; set; }
    }
}