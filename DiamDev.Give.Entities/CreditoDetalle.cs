using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Credito_Detalle")]
    public class CreditoDetalle
    {
        [Key, Column(name: "Detalle_Id", Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int DetalleId { get; set; }

        [Key, Column(name: "Credito_Id", Order = 1)]
        public long CreditoId { get; set; }

        [ForeignKey("CreditoId")]
        public Credito Credito { get; set; }

        [Column("Producto_Id")]
        public string ProductoId { get; set; }

        [ForeignKey("ProductoId")]
        public Producto Producto { get; set; }

        [Column("Unidad_Id")]
        public long UnidadId { get; set; }

        [ForeignKey("UnidadId")]
        public Unidad Unidad { get; set; }

        public decimal? Descuento { get; set; }

        [NotMapped]
        public decimal Existencia { get; set; }

        public decimal Cantidad { get; set; }

        [Column("Precio_Costo")]
        public decimal PrecioCosto { get; set; }

        public decimal Precio { get; set; }
    }
}
