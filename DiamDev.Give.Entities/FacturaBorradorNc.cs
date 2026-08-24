using System.Collections.Generic;

namespace DiamDev.Give.Entities
{
    /// <summary>
    /// Fila de RC_FACTURAS_BORRNC (SAP HANA): facturas de un cliente contra
    /// las que se puede emitir NC.
    ///
    /// Los campos calculados al final NO vienen de HANA: los llena el BLL
    /// cruzando con los borradores locales y con las NC previas. Son la mejora
    /// visible sobre el desktop, que obligaba al usuario a agregar la línea y
    /// esperar un MessageBox para descubrir que el monto no cabía.
    /// </summary>
    public class FacturaBorradorNc
    {
        public string DocNum { get; set; }
        public System.DateTime DocDate { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string SlpName { get; set; }
        public string Moneda { get; set; }
        public decimal DocTotal { get; set; }
        public decimal Pagado { get; set; }        // PaidToDate
        public string SerieFel { get; set; }
        public string NumeroFel { get; set; }

        // ── Calculados por el BLL ──

        /// <summary>Comprometido en borradores PENDIENTE o AUTORIZADO.</summary>
        public decimal Acumulado { get; set; }

        /// <summary>IDs de esos borradores, para el tooltip.</summary>
        public string BorradoresRelacionados { get; set; }

        /// <summary>Suma de NC ya emitidas en SAP contra esta factura.</summary>
        public decimal NcPreviaSap { get; set; }

        /// <summary>NC previas en detalle, para mostrárselas al autorizador.</summary>
        public List<NotaCreditoPreviaSap> NotasPrevias { get; set; }

        /// <summary>Tope duro de la regla R4: DocTotal − Acumulado.</summary>
        public decimal Disponible { get; set; }

        /// <summary>Tope si además se descuentan las NC de SAP. Solo advertencia.</summary>
        public decimal DisponibleNeto { get; set; }

        /// <summary>Pagada por completo: la NC generará saldo a favor del cliente.</summary>
        public bool GeneraSaldoAFavor { get; set; }

        public FacturaBorradorNc()
        {
            NotasPrevias = new List<NotaCreditoPreviaSap>();
        }
    }
}