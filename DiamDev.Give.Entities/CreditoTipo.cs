using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace DiamDev.Give.Entities
{
    [Table("Credito_Tipo")]
    public class CreditoTipo
    {
        [Key, Column(name: "Credito_Tipo_Id")]
        public int CreditoTipoId { get; set; }

        [Required]
        [StringLength(150)]
        public string Nombre { get; set; }
    }
}
