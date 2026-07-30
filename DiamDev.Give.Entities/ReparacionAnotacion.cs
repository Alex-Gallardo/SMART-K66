using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Reparacion_Anotacion")]
    public class ReparacionAnotacion
    {
        [Key, Column(name: "Anotacion_Id", Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int AnotacionId { get; set; }

        [Key, Column(name: "Reparacion_Id", Order = 1)]
        public long ReparacionId { get; set; }

        [ForeignKey("ReparacionId")]
        public Reparacion Reparacion { get; set; }

        [Required]
        public string Comentario { get; set; }

        [Column("Fecha_Anotacion")]
        public DateTime FechaAnotacion { get; set; }

        [Column("Usr_Anotacion")]
        public long UsrAnotacion { get; set; }

        [ForeignKey("UsrAnotacion")]
        public Usuario UsuarioAnotacion { get; set; }
        
        public bool Visto { get; set; }
    }
}
