using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiamDev.Give.Entities
{
    [Table("Empresa_Producto_Especial")]
    public class EmpresaProductoEspecial
    {
        [Key, Column(name: "Especial_Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid EspecialId { get; set; }

        [Column("Empresa_Id")]
        public long EmpresaId { get; set; }

        [ForeignKey("EmpresaId")]
        public Empresa Empresa { get; set; }

        public string Codigo { get; set; }

        public string Nombre { get; set; }

        public string Unidad { get; set; }

        public DateTime Fecha { get; set; }

        [Column("Responsable_Id")]
        public long ResponsableId { get; set; }

        [ForeignKey("ResponsableId")]
        public Usuario Responsable { get; set; }
    }
}
