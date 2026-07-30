using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Recibo_Forma_Pago")]
    public class ReciboFormaPago
    {
        [Key, Column(name: "Detalle_Id", Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int DetalleId { get; set; }

        [Key, Column(name: "Recibo_Id", Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long ReciboId { get; set; }

        [ForeignKey("ReciboId")]
        public Recibo Recibo { get; set; }

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
