namespace DiamDev.Give.Entities
{
    public class ProductoK66
    {
        public string WarehouseId { get; set; }

        public string LocationId { get; set; }

        public string ID { get; set; }      

        public string Nombre { get; set; }

        public string Descripcion { get; set; }

        public string Unidad { get; set; }

        public decimal Existencia { get; set; }

        public decimal InventarioTotal { get; set; }

        public decimal InventarioComprometido { get; set; }

        public decimal InventarioDisponible { get; set; }

        public decimal Precio { get; set; }

        public decimal PrecioOriginal { get; set; }

        public decimal Descuento { get; set; }
    }
}
