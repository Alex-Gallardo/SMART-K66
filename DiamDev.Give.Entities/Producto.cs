using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Producto")]
    public class Producto
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Producto_Id")]
        [StringLength(50)]
        public string ProductoId { get; set; }

        [Column("Producto_Padre_Id")]
        [StringLength(50)]
        public string ProductoPadreId { get; set; }

        [Column("Categoria_Id")]
        public long CategoriaId { get; set; }

        [ForeignKey("CategoriaId")]
        public ProductoCategoria Categoria { get; set; }

        [Column("Marca_Id")]
        public long MarcaId { get; set; }

        [ForeignKey("MarcaId")]
        public Marca Marca { get; set; }

        [Column("Unidad_Id")]
        public long UnidadId { get; set; }

        [ForeignKey("UnidadId")]
        public Unidad Unidad { get; set; }

        [StringLength(250)]
        public string Codigo { get; set; }

        [Required]
        public string Nombre { get; set; }

        public string Descripcion { get; set; }
        
        public int Minimo { get; set; }

        public int Maximo { get; set; }       

        public decimal Cantidad { get; set; }

        [NotMapped]
        public decimal Existencia { get; set; }

        [NotMapped]
        public decimal PrecioActual { get; set; }
                
        public bool Activo { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }

        public List<ProductoPrecio> Precios { get; set; }

        [NotMapped]
        public List<Producto> Productos { get; set; }

        public List<ProductoFotografia> Imagenes { get; set; }
    }
}
