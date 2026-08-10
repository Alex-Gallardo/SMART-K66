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

        // Totales del pago según ORCT — DINERO EFECTIVAMENTE RECIBIDO
        public decimal DocTotalGTQ { get; set; }   // ORCT.DocTotal   (moneda local)
        public decimal DocTotalUSD { get; set; }   // ORCT.DocTotalFC (moneda extranjera)
        public string MonedaDoc { get; set; }      // ORCT.DocCurr ya normalizada (QTZ→GTQ)

        // Lo aplicado a facturas según RCT2 (0 si no hay líneas)
        public bool TieneLineasRct2 { get; set; }
        public decimal AplicadoGTQ { get; set; }
        public decimal AplicadoUSD { get; set; }

        // DocNum de las facturas (OINV) que este pago dejó aplicadas
        public List<string> FacturasAplicadas { get; set; } = new List<string>();

        // ══════════════════════════════════════════════════════════════
        // NIVEL 1 — DINERO (autoridad para OPERADO / DESCUADRE)
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Dinero efectivamente RECIBIDO en este pago (ORCT.DocTotal).
        /// Incluye TODO: lo aplicado a facturas + lo que quedó a cuenta.
        ///
        /// Se compara contra MONTO_T_REC del recibo. Es la única medida
        /// válida para decidir si el recibo está operado, porque es la
        /// única que representa "cuánto dinero registró Créditos en SAP".
        /// </summary>
        public decimal MontoRecibido(bool esUSD)
        {
            return esUSD ? DocTotalUSD : DocTotalGTQ;
        }

        // ══════════════════════════════════════════════════════════════
        // NIVEL 2 — APLICACIÓN (informativo, NO decide estado)
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Dinero APLICADO a facturas según RCT2. Cero si no hay líneas
        /// (pago 100% a cuenta / anticipo puro).
        /// </summary>
        public decimal MontoAplicado(bool esUSD)
        {
            if (!TieneLineasRct2) return 0m;
            return esUSD ? AplicadoUSD : AplicadoGTQ;
        }

        /// <summary>
        /// Dinero que quedó A CUENTA del cliente (anticipo / saldo a favor).
        ///
        /// No requiere leer ORCT.NoDocSum: es la diferencia entre lo recibido
        /// y lo aplicado. Cuando Créditos concilie ese saldo contra una factura
        /// futura (conciliación interna: OITR/ITR1 + JDT1), este valor NO cambia
        /// — y eso es correcto: el recibo de caja ya estaba operado desde el
        /// momento en que el ORCT se creó.
        /// </summary>
        public decimal MontoACuenta(bool esUSD)
        {
            decimal dif = MontoRecibido(esUSD) - MontoAplicado(esUSD);
            return dif > 0m ? dif : 0m;
        }

        // ══════════════════════════════════════════════════════════════
        // LEGACY — no usar para conciliar
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// ⚠️ NO USAR PARA CONCILIAR. Causa DESCUADRE en recibos mixtos
        /// (facturas + anticipo): al haber líneas RCT2 devuelve solo lo
        /// aplicado e ignora el monto a cuenta.
        ///
        /// Se conserva por compatibilidad. Para conciliar usar MontoRecibido().
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