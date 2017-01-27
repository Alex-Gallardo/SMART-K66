using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Diario")]
    public class Diario
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Diario_Id")]
        public long DiarioId { get; set; }

        [Required(ErrorMessage = "La descripción del diario es requerida")]
        public string Descripcion { get; set; }

        [Column("Partida_Id")]
        public int PartidaId { get; set; }

        public bool General { get; set; }

        [Column("Fecha_Documento")]
        public DateTime FechaDocumento { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }

        [Column("Usr_Creo")]
        public long UsrCreo { get; set; }

        [ForeignKey("UsrCreo")]
        public Usuario UsuarioCreo { get; set; }

        public List<DiarioAgencia> Agencias { get; set; }

        public List<DiarioDetalle> Detalles { get; set; }
    }
}
