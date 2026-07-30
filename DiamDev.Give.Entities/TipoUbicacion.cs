using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiamDev.Give.Entities
{
    [Table("Tipo_Ubicacion")]
    public class TipoUbicacion
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Tipo_Id")]
        public long TipoId { get; set; }

        [Required(ErrorMessage = "El nombre del tipo de ubicacion es requerido")]
        [StringLength(300)]
        public string Nombre { get; set; }

        public string Descripcion { get; set; }

        public bool Activo { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }
    }
}
