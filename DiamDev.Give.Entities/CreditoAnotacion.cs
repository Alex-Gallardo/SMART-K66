using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Credito_Anotacion")]
    public class CreditoAnotacion
    {       
        [Key, Column(name: "Anotacion_Id", Order = 0)]
        [DatabaseGenerated(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None)]
        public int AnotacionId { get; set; }

        [Key, Column(name: "Credito_Id", Order = 1)]       
        public long CreditoId { get; set; }

        [ForeignKey("CreditoId")]
        public Credito Credito { get; set; }

        [Required]
        public string Comentario { get; set; }

        [Column("Fecha_Anotacion")]
        public DateTime FechaAnotacion { get; set; }

        [Column("Usr_Anotacion")]
        public long UsrAnotacion { get; set; }

        [ForeignKey("UsrAnotacion")]
        public Usuario UsuarioAnotacion { get; set; }
    }
}
