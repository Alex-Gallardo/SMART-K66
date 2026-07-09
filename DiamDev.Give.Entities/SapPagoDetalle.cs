using System;
using System.Collections.Generic;

namespace DiamDev.Give.Entities
{
    /// <summary>
    /// Fotografía de UN pago recibido (ORCT) en SAP vinculado a un recibo
    /// vía U_Recibocaja_Webapp. Incluye los ANULADOS (Canceled='Y'):
    /// el sync necesita ver la historia completa, no solo los vivos.
    /// </summary>
    public class SapPagoDetalle
    {
        public string IdRecibo { get; set; }
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public bool Canceled { get; set; }
        public DateTime? FechaPago { get; set; }

        // Totales del pago según ORCT (fallback cuando NO hay líneas RCT2 = anticipo)
        public decimal DocTotalGTQ { get; set; }   // ORCT.DocTotal   (moneda local)
        public decimal DocTotalUSD { get; set; }   // ORCT.DocTotalFC (moneda extranjera)
        public string MonedaDoc { get; set; }      // ORCT.DocCurr ya normalizada (QTZ→GTQ)

        // Lo aplicado a facturas según RCT2 (0 si no hay líneas)
        public bool TieneLineasRct2 { get; set; }
        public decimal AplicadoGTQ { get; set; }
        public decimal AplicadoUSD { get; set; }

        // DocNum de las facturas (OINV) que este pago dejó aplicadas
        public List<string> FacturasAplicadas { get; set; } = new List<string>();

        /// <summary>
        /// Monto efectivo del pago en la moneda pedida:
        /// RCT2 si hay líneas aplicadas; si no (anticipo), el total del ORCT.
        /// </summary>
        public decimal MontoEfectivo(bool esUSD)
        {
            if (TieneLineasRct2) return esUSD ? AplicadoUSD : AplicadoGTQ;
            return esUSD ? DocTotalUSD : DocTotalGTQ;
        }
    }

    /// <summary>
    /// Fila mínima de REC_CAJA_ENC para la pasada inversa del sync
    /// (reemplaza el uso de SapCobroAplicado, que no traía SYNC_ESTADO).
    /// </summary>
    public class ReciboRevisionSql
    {
        public string IdRecibo { get; set; }
        public int SapDocEntry { get; set; }
        public int SapDocNum { get; set; }
        public string SyncEstado { get; set; }   // 'OPERADO' | 'DESCUADRE'
    }
}