using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Credito_Pago")]
    public class CreditoPago
    {
        [Key, Column(name: "Detalle_Id", Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int DetalleId { get; set; }

        [Key, Column(name: "Credito_Id", Order = 1)]
        public long CreditoId { get; set; }

        [ForeignKey("CreditoId")]
        public Credito Credito { get; set; }

        [Column(name: "Forma_Pago_Id")]
        public long FormaPagoId { get; set; }

        [ForeignKey("FormaPagoId")]
        public FormaPago FormaPago { get; set; }

        public decimal Valor { get; set; }
              
        public string Nota { get; set; }

        public DateTime Fecha { get; set; }

        [Column("Usr_Operacion_Id")]
        public long UsrOperacionId { get; set; }

        [ForeignKey("UsrOperacionId")]
        public Usuario UsuarioOperacion { get; set; }
    }
}
