using System.Collections.Generic;

namespace DiamDev.Give.Entities
{
    /// <summary>
    /// Compromiso de un documento (factura/pedido) en recibos "en tránsito":
    ///  - Recibos PENDIENTES (aún no operados en SAP).
    ///  - Líneas ANULADO_SAP de recibos en DESCUADRE (el pago se revirtió en SAP,
    ///    pero el dinero YA fue recibido en caja por ese recibo).
    /// En TS: { monto: number; recibos: string[] }
    /// </summary>
    public class PendienteDocumento
    {
        public decimal Monto { get; set; }

        /// <summary>Recibos que comprometen este documento, con su estado.
        /// Ej: ["RG12-07510 (PENDIENTE)", "RG12-07522 (DESCUADRE)"]</summary>
        public List<string> Recibos { get; set; } = new List<string>();
    }
}