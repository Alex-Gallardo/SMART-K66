using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Movimiento_Forma_Pago")]
    public class MovimientoFormaPago
    {
        [Key, Column(name: "Movimiento_Id", Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long MovimientoId { get; set; }

        [ForeignKey("MovimientoId")]
        public Movimiento Movimiento { get; set; }

        [Key, Column(name: "Forma_Pago_Id", Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long FormaPagoId { get; set; }

        [ForeignKey("FormaPagoId")]
        public FormaPago FormaPago { get; set; }

        public decimal Valor { get; set; }

        public string Nota { get; set; }
    }
}
