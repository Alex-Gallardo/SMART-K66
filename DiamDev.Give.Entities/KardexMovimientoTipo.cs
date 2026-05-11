using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Kardex_Movimiento_Tipo")]
    public class KardexMovimientoTipo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Tipo_Id")]
        public int TipoId { get; set; }

        [StringLength(200)]
        public string Nombre { get; set; }
    }
}
