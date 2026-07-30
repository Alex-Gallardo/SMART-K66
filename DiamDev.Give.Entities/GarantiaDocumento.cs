using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Garantia_Documento")]
    public class GarantiaDocumento
    {
        [Key]
        [Column("Documento_Id")]
        public int DocumentoId { get; set; }

        [Required]
        [StringLength(250)]
        public string Nombre { get; set; }
    }
}
