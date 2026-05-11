using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Proveedor_Movimiento_Tipo")]
    public class ProveedorMovimientoTipo
    {
        [Key]
        [Column("Tipo_Id")]
        public int TipoId { get; set; }

        [Required]
        [StringLength(250)]
        public string Nombre { get; set; }
    }
}
