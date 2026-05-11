using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Proveedor_Tipo")]
    public class ProveedorTipo
    {
        [Key, Column(name: "Tipo_Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int TipoId { get; set; }
                
        [Required]
        [StringLength(150)]
        public string Nombre { get; set; }
    }
}
