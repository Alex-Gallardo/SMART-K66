using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Movimiento_Tipo")]
    public class MovimientoTipo
    {
        [Key]
        [Column("Movimiento_Tipo_Id")]
        public int MovimientoTipoId { get; set; }

        [Required]
        [StringLength(250)]
        public string Nombre { get; set; }
    }
}
