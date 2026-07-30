using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiamDev.Give.Entities
{
    [Table("Moneda")]
    public class Moneda
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Moneda_Id")]
        public long MonedaId { get; set; }

        public string Codigo { get; set; }

        public string Nombre { get; set; }

        public string Descripcion { get; set; }

        public string Simbolo { get; set; }

        [Column("Tipo_De_Cambio_Compra")]
        public decimal TipoDeCambioCompra { get; set; }

        [Column("Tipo_De_Cambio_Venta")]
        public decimal TipoDeCambioVenta { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }
    }
}
