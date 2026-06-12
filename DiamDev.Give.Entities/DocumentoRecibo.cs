using System;

namespace DiamDev.Give.Entities
{
    /// <summary>
    /// Documento disponible para cobrar desde MA_RECC_DOCTOS (APK66).
    /// Columnas reales confirmadas: DOCTO, INVOICE_DATE, INVOICE_STATUS,
    /// CURRENCY_ID, MONTO_FACT, PAGADO.
    /// Nota: FEL (Serie/Número) no está en esta tabla — viene de SAP HANA
    /// a través de FrmFacturarasCL en el sistema desktop.
    /// </summary>
    public class DocumentoRecibo
    {
        public string NoDocumento { get; set; }
        public DateTime FechaDoc { get; set; }
        public string Status { get; set; }  // INVOICE_STATUS
        public decimal MontoFact { get; set; }
        public decimal Pagado { get; set; }
        public decimal Saldo => MontoFact - Pagado;  // calculado
        public string Moneda { get; set; }           // CURRENCY_ID
        public string FelSerie { get; set; }           // no en tabla
        public string FelNumero { get; set; }           // no en tabla
    }
}