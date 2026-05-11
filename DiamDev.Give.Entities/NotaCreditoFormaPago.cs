using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Nota_Credito_Forma_Pago")]
    public class NotaCreditoFormaPago
    {
        [Key, Column(name: "Credito_Id", Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
         public long CreditoId { get; set; }

        [ForeignKey("CreditoId")]
        public NotaCredito Credito { get; set; }
         
        [Key, Column(name: "Forma_Pago_Id", Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long FormaPagoId { get; set; }

        [ForeignKey("FormaPagoId")]
        public FormaPago FormaPago { get; set; }

        public decimal Valor { get; set; }

        public string Nota { get; set; }
    }
}
