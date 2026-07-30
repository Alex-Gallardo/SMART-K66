using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Egreso_Detalle")]
    public class EgresoDetalle
    {
        [Key, Column(name: "Detalle_Id", Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int DetalleId { get; set; }

        [Key, Column(name: "Egreso_Id", Order = 1)]
        public long EgresoId { get; set; }

        [ForeignKey("EgresoId")]
        public Egreso Egreso { get; set; }
        
        [Column("Producto_Id")]
        [StringLength(50)]
        public string ProductoId { get; set; }

        [ForeignKey("ProductoId")]
        public Producto Producto { get; set; }

        [Column("Unidad_Id")]
        public long UnidadId { get; set; }

        [ForeignKey("UnidadId")]
        public Unidad Unidad { get; set; }

        public decimal Cantidad { get; set; }

        [StringLength(100)]
        public string ID { get; set; }

        [Column("Precio_Costo")]
        public decimal PrecioCosto { get; set; }
    }
}
