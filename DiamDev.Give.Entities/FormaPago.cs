using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Forma_Pago")]
    public class FormaPago
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Forma_Pago_Id")]
        public long FormaPagoId { get; set; }

        [Column("Empresa_Id")]
        public long? EmpresaId { get; set; }

        [ForeignKey("EmpresaId")]
        public Empresa Empresa { get; set; }

        [Required(ErrorMessage = "El nombre de la forma de pago es requerida")]
        [StringLength(300)]
        public string Nombre { get; set; }

        [NotMapped]
        public decimal Valor { get; set; }

        [NotMapped]
        public decimal MontoCajero { get; set; }
        
        [NotMapped]
        public decimal Diferencia { get; set; }
        
        public bool Activo { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }
    }
}
