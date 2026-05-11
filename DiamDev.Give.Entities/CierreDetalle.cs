using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Cierre_Detalle")]
    public class CierreDetalle
    {
        [Key, Column(name: "Detalle_Id", Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int DetalleId { get; set; }

        [Key, Column(name: "Cierre_Id", Order = 1)]
        public long CierreId { get; set; }

        [ForeignKey("CierreId")]
        public Cierre Cierre { get; set; }

        [Column(name: "Forma_Pago_Id")]
        public long FormaPagoId { get; set; }

        [ForeignKey("FormaPagoId")]
        public FormaPago FormaPago { get; set; }

        [Column("Monto_Sistema")]
        public decimal MontoSistema { get; set; }

        [Column("Monto_Cajero")]
        public decimal MontoCajero { get; set; }

        [NotMapped]
        public decimal Faltante { get; set; }

        [NotMapped]
        public decimal Sobrante { get; set; }
    }
}
