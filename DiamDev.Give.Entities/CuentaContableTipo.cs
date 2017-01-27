using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Cuenta_Contable_Tipo")]
    public class CuentaContableTipo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Tipo_Id")]
        public long TipoId { get; set; }

        [Required(ErrorMessage = "El nombre de la cuenta tipo es requerido")]
        public string Nombre { get; set; }

        public bool Activo { get; set; }
    }
}
