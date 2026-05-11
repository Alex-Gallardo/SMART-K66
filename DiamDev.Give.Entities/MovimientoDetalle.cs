using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Movimiento_Detalle")]
    public class MovimientoDetalle
    {
        [Key, Column(name: "Detalle_Id", Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int DetalleId { get; set; }

        [Key, Column(name: "Movimiento_Id", Order = 1)]
        public long MovimientoId { get; set; }

        [ForeignKey("MovimientoId")]
        public Movimiento Movimiento { get; set; }
        
        [Column("Producto_Id")]
        [StringLength(50)]
        public string ProductoId { get; set; }

        [ForeignKey("ProductoId")]
        public Producto Producto { get; set; }

        [Column("Unidad_Id")]
        public long UnidadId { get; set; }

        [ForeignKey("UnidadId")]
        public Unidad Unidad { get; set; }

        [NotMapped]
        public decimal Existencia { get; set; }

        public decimal Cantidad { get; set; }

        [Column("Precio_Costo")]
        public decimal PrecioCosto { get; set; }

        public decimal Precio { get; set; }

        public int Minimo { get; set; }

        public int Maximo { get; set; }

        [StringLength(100)]
        public string ID { get; set; }

        [StringLength(100)]
        public string Lote { get; set; }

        [Column("Fecha_Vencimiento_Lote")]
        public DateTime? FechaVencimientoLote { get; set; }
    }
}
