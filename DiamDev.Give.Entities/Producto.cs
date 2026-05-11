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

        [Column("Empresa_Id")]
        public long? EmpresaId { get; set; }

        [ForeignKey("EmpresaId")]
        public Empresa Empresa { get; set; }

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

        [Column("Nombre_Alternativo_1")]
        public string NombreAlternativo1 { get; set; }

        [Column("Nombre_Alternativo_2")]
        public string NombreAlternativo2 { get; set; }

        public string Descripcion { get; set; }
        
        public int Minimo { get; set; }

        public int Maximo { get; set; }       

        public decimal Cantidad { get; set; }

        [NotMapped]
        public decimal Existencia { get; set; }

        [NotMapped]
        public decimal PrecioActual { get; set; }

        [Column("Tiene_Identificador")]
        public bool TieneIdentificador { get; set; }

        [NotMapped]
        public int EnvaseId { get; set; }
        
        [Column("Tiene_Envase")]
        public bool TieneEnvase { get; set; }

        [Column("Cantidad_Envase")]
        public int?  CantidadEnvase { get; set; }

        [Column("Tiene_Lote")]
        public bool TieneLote { get; set; }
                
        public bool Activo { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }

        public string FotografiaApp { get; set; }

        [NotMapped]
        public bool Eliminar { get; set; }

        public List<ProductoPrecio> Precios { get; set; }

        [NotMapped]
        public decimal Costo { get; set; }

        [NotMapped]
        public decimal PrecioCostoDescuento { get; set; }
        
        [NotMapped]
        public List<Producto> Productos { get; set; }

        public List<ProductoFotografia> Imagenes { get; set; }

        public List<ProductoPrecioCostoHistorial> Compras { get; set; }

        public List<ProductoNivelPrecio> Niveles { get; set; }

        [NotMapped]
        public ProductoFotografia Fotografia { get; set; }
    }
}
