using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiamDev.Give.Entities
{
    [Table("Descuento_K66")]
    public class DescuentoK66
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("Descuento_Id")]
        public Guid DescuentoId { get; set; }

        [Column("Empresa_Id")]
        public long EmpresaId { get; set; }

        [ForeignKey("EmpresaId")]
        public Empresa Empresa { get; set; }           

        [Column("ID_K66")]
        public string IDK66 { get; set; }

        public string Nit { get; set; }

        public string Nombre { get; set; }

        [Column("Direccion_Id")]
        public int? DireccionId { get; set; }

        public string Direccion { get; set; }

        [Column("Producto_Id")]
        public string ProductoId { get; set; }

        public string Producto { get; set; }

        public decimal Descuento { get; set; }

        [Column("Responsable_Id")]
        public long ResponsableId { get; set; }

        [ForeignKey("ResponsableId")]
        public Usuario Responsable { get; set; }  

        public DateTime Fecha { get; set; }      
    }
}
