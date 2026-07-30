using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiamDev.Give.Entities
{
    [Table("Paquete")]
    public class Paquete
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Paquete_Id")]
        public long PaqueteId { get; set; }

        public string Nombre { get; set; }

        public string Descripcion { get; set; }

        [Column("Cantidad_DTE")]
        public int CantidadDTE { get; set; }

        public decimal Costo { get; set; }

        public decimal Precio { get; set; }

        public int Vigencia { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }
    }
}
