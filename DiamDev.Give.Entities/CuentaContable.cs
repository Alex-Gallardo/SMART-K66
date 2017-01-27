using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Cuenta_Contable")]
    public class CuentaContable
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Cuenta_Id")]
        public long CuentaId { get; set; }

        [Column("Cuenta_Padre_Id")]
        public long? CuentaPadreId { get; set; }

        [Column("Tipo_Id")]
        public long TipoId { get; set; }

        [ForeignKey("TipoId")]
        public CuentaContableTipo Tipo { get; set; }

        [Required(ErrorMessage = "El número de la cuenta es requerido")]
        public string Cuenta { get; set; }

        [Required(ErrorMessage = "El nombre de la cuenta es requerido")]
        public string Nombre { get; set; }

        public string Descripcion { get; set; }

        public bool Activo { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }
    }
}
