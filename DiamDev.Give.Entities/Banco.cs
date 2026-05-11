using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Banco")]
    public class Banco
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Banco_Id")]
        public long BancoId { get; set; }

        [Required(ErrorMessage = "El nombre del banco es requerido")]
        [StringLength(300)]
        public string Nombre { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }
    }
}
