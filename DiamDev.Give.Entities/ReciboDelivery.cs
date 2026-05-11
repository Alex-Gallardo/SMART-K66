using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiamDev.Give.Entities
{
    [Table("Recibo_Delivery")]
    public class ReciboDelivery
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Recibo_Id")]
        public long ReciboId { get; set; }      

        public bool Operado { get; set; }

        [Column("Fecha_Operado")]
        public DateTime? FechaOperado { get; set; }

        [Column("Fecha")]
        public DateTime Fecha { get; set; }
    }
}
