using System;

namespace DiamDev.Give.Entities
{
    /// <summary>
    /// Foto comercial de un artículo de SAP al momento de crear la cotización.
    /// Los importes se calculan en el servidor y se persisten para conservar el
    /// documento aun cuando después cambien descripción, precio o existencia.
    /// </summary>
    public class CotizacionDetalle
    {
        public long RowId { get; set; }
        public int Linea { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string Descripcion { get; set; }
        public string Grupo { get; set; }
        public string Unidad { get; set; }
        public int ListaPrecio { get; set; }
        public decimal Existencia { get; set; }
        public decimal Disponible { get; set; }
        public decimal Cantidad { get; set; }
        /// <summary>Precio final con IVA devuelto por SAP.</summary>
        public decimal PrecioLista { get; set; }
        /// <summary>Precio final con IVA usado en la cotización.</summary>
        public decimal PrecioUnitario { get; set; }
        public decimal DescuentoPorcentaje { get; set; }
        public string GrupoImpuesto { get; set; }
        public decimal ImpuestoPorcentaje { get; set; }
        public decimal ImporteBruto { get; set; }
        public decimal DescuentoMonto { get; set; }
        public decimal Subtotal { get; set; }
        /// <summary>
        /// Se conserva por compatibilidad histórica. Para nuevas cotizaciones
        /// es cero porque PrecioUnitario ya incluye IVA.
        /// </summary>
        public decimal ImpuestoMonto { get; set; }
        public decimal Total { get; set; }
    }
}
