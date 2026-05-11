using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Reparacion_Pieza")]
    public class ReparacionPieza
    {
        [Key, Column(name: "Reparacion_Id", Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long ReparacionId { get; set; }

        [Key, Column(name: "Producto_Id", Order = 1)]
        [StringLength(50)]
        public string ProductoId { get; set; }

        [ForeignKey("ProductoId")]
        public Producto Producto { get; set; }

        public int Cantidad { get; set; }

        public decimal Precio { get; set; }
    }
}
