using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Nomina_Tipo")]
    public class NominaTipo
    {
        [Key]
        [Column("Tipo_Id")]
        public int TipoId { get; set; }

        [StringLength(150)]
        public string Nombre { get; set; }
    }
}
