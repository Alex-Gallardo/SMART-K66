using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Reserva_Detalle")]
    public class ReservaDetalle
    {
        [Key, Column(name: "Detalle_Id", Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int DetalleId { get; set; }

        [Key, Column(name: "Reserva_Id", Order = 1)]
        public long ReservaId { get; set; }

        [ForeignKey("ReservaId")]
        public Reserva Reserva { get; set; }
        
        [Column("Producto_Id")]
        public string ProductoId { get; set; }

        [ForeignKey("ProductoId")]
        public Producto Producto { get; set; }

        [Column("Unidad_Id")]
        public long UnidadId { get; set; }

        [ForeignKey("UnidadId")]
        public Unidad Unidad { get; set; }
   
        public decimal Cantidad { get; set; }
        
        public decimal Precio { get; set; }
    }
}
