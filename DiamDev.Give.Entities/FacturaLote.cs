using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Factura_Lote")]
    public class FacturaLote
    {
        [Key, Column(name: "Detalle_Id", Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int DetalleId { get; set; }

        [Key, Column(name: "Factura_Id", Order = 1)]
        public long FacturaId { get; set; }

        [ForeignKey("FacturaId")]
        public Factura Factura { get; set; }

        [Column("Producto_Id")]
        [StringLength(50)]
        public string ProductoId { get; set; }

        [ForeignKey("ProductoId")]
        public Producto Producto { get; set; }

        [StringLength(100)]
        public string Lote { get; set; }

        [Column("Fecha_Vencimiento")]
        public DateTime FechaVencimiento { get; set; }

        public decimal Cantidad { get; set; }      
    }
}
