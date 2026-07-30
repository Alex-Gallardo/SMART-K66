using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Movimiento_Estado")]
    public class MovimientoEstado
    {
        [Key]
        [Column("Movimiento_Estado_Id")]
        public int MovimientoEstadoId { get; set; }
        
        [Required]
        [StringLength(250)]
        public string Nombre { get; set; }
    }
}
