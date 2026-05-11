using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiamDev.Give.Entities
{
    [Table("Producto_Alerta_K66")]
    public class ProductoAlertaK66
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Alerta_Id")]
        public long AlertaId { get; set; }
        
        public string Nombre { get; set; }

        public string Mensaje { get; set; }

        [Column("Rango_Inicial")]
        public int RangoInicial { get; set; }

        [Column("Rango_Final")]
        public int RangoFinal { get; set; }        

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }
    }
}
