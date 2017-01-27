using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Producto_Precio")]
    public class ProductoPrecio
    {
        [Key, Column(name: "Producto_Id", Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [StringLength(50)]
        public string ProductoId { get; set; }

        [ForeignKey("ProductoId")]
        public Producto Producto { get; set; }

        [Key, Column(name: "Precio_Id", Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int PrecioId { get; set; }

        [ForeignKey("PrecioId")]
        public Precio Precio { get; set; }

        [NotMapped]
        public string Nombre { get; set; }

        public decimal Valor { get; set; }
    }
}
