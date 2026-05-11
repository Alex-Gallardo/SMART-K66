using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Recibo_Tipo")]
    public class ReciboTipo
    {
        [Key, Column(name: "Recibo_Tipo_Id")]
        public int ReciboTipoId { get; set; }
                
        [Required]
        [StringLength(150)]
        public string Nombre { get; set; }
    }
}
