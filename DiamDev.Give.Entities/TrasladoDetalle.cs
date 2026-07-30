using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Traslado_Detalle")]
    public class TrasladoDetalle
    {
        [Key, Column(name: "Detalle_Id", Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int DetalleId { get; set; }

        [Key, Column(name: "Traslado_Id", Order = 1)]
        public long TrasladoId { get; set; }

        [ForeignKey("TrasladoId")]
        public Traslado Traslado { get; set; }

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
    }
}
