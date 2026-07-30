using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Reparacion_Estado")]
    public class ReparacionEstado
    {
        [Key]
        public int EstadoId { get; set; }

        [Required]
        [StringLength(200)]
        public string Nombre { get; set; }
    }
}
