using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Recibo_Detalle")]
    public class ReciboDetalle
    {
        [Key, Column(name: "Detalle_Id", Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int DetalleId { get; set; }

        [Key, Column(name: "Recibo_Id", Order = 1)]
        public long ReciboId { get; set; }

        [ForeignKey("ReciboId")]
        public Recibo Recibo { get; set; }
        
        [Column("Producto_Id")]
        [StringLength(50)]
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

        [StringLength(100)]
        public string ID { get; set; }
    }
}
