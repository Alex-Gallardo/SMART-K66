using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Producto_Categoria")]
    public class ProductoCategoria
    {
        [Key, Column(name: "Producto_Categoria_Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long ProductoCategoriaId { get; set; }

        [Required(ErrorMessage = "El Nombre es Requerido")]
        [StringLength(250)]
        public string Nombre { get; set; }

        public bool Activo { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }

        public string FotografiaApp { get; set; }

        [NotMapped]
        public ProductoFotografia Fotografia { get; set; }
    }
}
