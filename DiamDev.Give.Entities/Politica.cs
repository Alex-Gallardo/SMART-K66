using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Politica")]
    public class Politica
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Politica_Id")]
        public long PoliticaId { get; set; }

        [Column("Tipo_Id")]
        public int TipoId { get; set; }

        [ForeignKey("TipoId")]
        public PoliticaTipo Tipo { get; set; }

        [Required(ErrorMessage = "El nombre de la politica es requerido")]
        public string Nombre { get; set; }

        public bool Activo { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }
    }
}
