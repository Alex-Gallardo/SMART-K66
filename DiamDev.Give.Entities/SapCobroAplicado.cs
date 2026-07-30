using System;

namespace DiamDev.Give.Entities
{
    /// <summary>
    /// Un pago de SAP (ORCT) que lleva estampado nuestro ID_RECIBO
    /// en el campo de usuario U_Recibocaja_Webapp.
    /// Equivale en TS a: interface SapCobroAplicado { ... }
    /// </summary>
    public class SapCobroAplicado
    {
        public string IdRecibo { get; set; }  // ORCT.U_Recibocaja_Webapp  (= REC_CAJA_ENC.ID_RECIBO)
        public int SapDocEntry { get; set; }  // ORCT.DocEntry  (PK interna del pago)
        public int SapDocNum { get; set; }  // ORCT.DocNum    (número visible, ej. 1018704)
        public string CardCode { get; set; }  // ORCT.CardCode  (cliente SAP)
        public DateTime? FechaPago { get; set; }  // ORCT.DocDate
    }
}