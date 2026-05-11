using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Factura_Tipo")]
    public class FacturaTipo
    {
        [Key, Column(name: "Factura_Tipo_Id")]
        public int FacturaTipoId { get; set; }
        
        [Required]
        [StringLength(150)]
        public string Nombre { get; set; }
    }
}
