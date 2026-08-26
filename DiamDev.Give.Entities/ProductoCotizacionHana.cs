namespace DiamDev.Give.Entities
{
    /// <summary>
    /// Proyección de producto desde SAP HANA para cotizar al cliente elegido.
    /// Precio y moneda corresponden a la lista configurada en OCRD.ListNum.
    /// </summary>
    public class ProductoCotizacionHana
    {
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string Grupo { get; set; }
        public string Unidad { get; set; }
        public int ListaPrecio { get; set; }
        public string Moneda { get; set; }
        public decimal Precio { get; set; }
        public string GrupoImpuesto { get; set; }
        public decimal ImpuestoPorcentaje { get; set; }
        public decimal Existencia { get; set; }
        public decimal Comprometido { get; set; }
        public decimal Pedido { get; set; }
        public decimal Disponible { get; set; }
    }
}
