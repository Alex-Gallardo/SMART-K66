using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiamDev.Give.Entities
{
    [Table("Mesa")]
    public class Mesa
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Mesa_Id")]
        public long MesaId { get; set; }

        [Column("Agencia_Id")]
        public long? AgenciaId { get; set; }

        [ForeignKey("AgenciaId")]
        public Agencia Agencia { get; set; }

        [Column("Tipo_Ubicacion_Id")]
        public long TipoUbicacionId { get; set; }

        [ForeignKey("TipoUbicacionId")]
        public TipoUbicacion TipoUbicacion { get; set; }

        [Required(ErrorMessage = "El nombre de la mesa es requerido")]
        [StringLength(300)]
        public string Nombre { get; set; }

        public string Descripcion { get; set; }

        public bool Ocupado { get; set; }

        public bool Activo { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }

        [NotMapped]
        public string Token { get; set; }
    }
}
