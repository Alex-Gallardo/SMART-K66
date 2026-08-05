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

        /// <summary>
        /// SYNC_OBSERVACION tal como está en la BD al momento de la lectura.
        ///
        /// Se trae en ObtenerRecibosParaRevision para poder decidir EN MEMORIA
        /// si hay una marca [CONCIL] que limpiar, en vez de mandar un UPDATE a
        /// la BD para que ella lo averigüe con su WHERE ... LIKE '[[]CONCIL]%'.
        ///
        /// Cuesta ~200 bytes por fila en un SELECT que ya se estaba haciendo,
        /// y ahorra ~240,000 UPDATE diarios que afectaban CERO filas.
        /// (Medido 2026-08-05: 1.4M operaciones en 11 días sobre una tabla
        ///  de 871 filas.)
        /// </summary>
        public string SyncObservacion { get; set; }
    }
}