using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Movimiento_Categoria")]
    public class MovimientoCategoria
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Movimiento_Categoria_Id")]
        public int MovimientoCategoriaId { get; set; }
        
        [Required]
        [StringLength(250)]
        public string Nombre { get; set; }
        
        public bool Ingreso { get; set; }
    }
}
