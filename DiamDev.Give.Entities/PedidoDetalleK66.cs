using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiamDev.Give.Entities
{
    [Table("Pedido_Detalle_K66")]
    public class PedidoDetalleK66
    {
        [Key, Column(name: "Detalle_Id", Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int DetalleId { get; set; }

        [Key, Column(name: "Pedido_Id", Order = 1)]
        public long PedidoId { get; set; }

        [ForeignKey("PedidoId")]
        public PedidoK66 Pedido { get; set; }

        [Column("Producto_Id")]
        public string ProductoId { get; set; }

        [Column("WarehouseId")]
        public string WarehouseId { get; set; }

        public string Nombre { get; set; }

        public string Unidad { get; set; }

        public decimal Cantidad { get; set; }

        [Column("Precio_Original")]
        public decimal PrecioOriginal { get; set; }

        public decimal Precio { get; set; }

        public decimal Descuento { get; set; }

        [Column("Precio_Cambiado")]
        public bool PrecioCambiado { get; set; }

        [NotMapped]
        public decimal Existencia { get; set; }
    }
}
