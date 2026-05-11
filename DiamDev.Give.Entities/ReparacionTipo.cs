using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Reparacion_Tipo")]
    public class ReparacionTipo
    {
        [Key]
        [Column("Tipo_Id")]
        public int TipoId { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; }
    }
}
