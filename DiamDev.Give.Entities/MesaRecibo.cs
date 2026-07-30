using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiamDev.Give.Entities
{
    [Table("Mesa_Recibo")]
    public class MesaRecibo
    {
        [Key, Column(name: "Mesa_Id", Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]      
        public long MesaId { get; set; }

        [ForeignKey("MesaId")]
        public Mesa Mesa { get; set; }

        [Key, Column(name: "Recibo_Id", Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long ReciboId { get; set; }

        [ForeignKey("ReciboId")]
        public Recibo Recibo { get; set; }

        [Column("Pendiente_Pago")]
        public bool PendientePago { get; set; }
    }
}
