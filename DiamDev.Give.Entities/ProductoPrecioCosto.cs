using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Producto_Precio_Costo")]
    public class ProductoPrecioCosto
    {
        [Key, Column(name: "Producto_Id", Order = 0)]
        [StringLength(50)]
        public string ProductoId { get; set; }

        [ForeignKey("ProductoId")]
        public Producto Producto { get; set; }

        [Column("Precio_Costo")]
        public decimal PrecioCosto { get; set; }
    }
}
