using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiamDev.Give.Entities
{
    [Table("Tipo_Compra")]
    public class TipoCompra
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Tipo_Id")]
        public long TipoId { get; set; }

        [Required(ErrorMessage = "El nombre del tipo de compra es requerido")]
        [StringLength(300)]
        public string Nombre { get; set; }     

        public bool Activo { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }
    }
}
