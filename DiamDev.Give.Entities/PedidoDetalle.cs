using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Pedido_Detalle")]
    public class PedidoDetalle
    {
        [Key, Column(name: "Detalle_Id", Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int DetalleId { get; set; }

        [Key, Column(name: "Pedido_Id", Order = 1)]
        public long PedidoId { get; set; }

        [ForeignKey("PedidoId")]
        public Pedido Pedido { get; set; }
        
        [Column("Producto_Id")]
        public string ProductoId { get; set; }

        [ForeignKey("ProductoId")]
        public Producto Producto { get; set; }
               
        [Column("Unidad_Id")]
        public long UnidadId { get; set; }

        [ForeignKey("UnidadId")]
        public Unidad Unidad { get; set; }

        [StringLength(400)]
        public string Nombre { get; set; }

        public decimal? Descuento { get; set; }

        [NotMapped]
        public decimal Existencia { get; set; }

        public decimal Cantidad { get; set; }

        [Column("Precio_Costo")]
        public decimal PrecioCosto { get; set; }

        public decimal Precio { get; set; }
    }
}
