using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Producto_Fotografia")]
    public class ProductoFotografia
    {
        [Key, Column(name: "Fotografia_Id", Order = 0)]
        public int FotografiaId { get; set; }

        [Key, Column(name: "Producto_Id", Order = 1)]
        [StringLength(50)]
        public string ProductoId { get; set; }

        [ForeignKey("ProductoId")]
        public Producto Producto { get; set; }

        [StringLength(200)]
        public string Nombre { get; set; }

        [NotMapped]
        public string Remoto { get; set; }

        [StringLength(150)]
        public string ContentType { get; set; }

        public int Length { get; set; }

        public byte[] Content { get; set; }
    }
}
