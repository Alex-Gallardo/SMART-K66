using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Producto_Nivel_Precio")]
    public class ProductoNivelPrecio
    {
        [Key, Column(name: "Nivel_Id", Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int NivelId { get; set; }

        [Key, Column(name: "Producto_Id", Order = 1)]
        public string ProductoId { get; set; }

        [ForeignKey("ProductoId")]
        public Producto Producto { get; set; }

        public int Inicial { get; set; }

        public int Final { get; set; }

        public decimal Precio { get; set; }
    }
}
