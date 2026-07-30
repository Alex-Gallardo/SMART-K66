using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiamDev.Give.Entities
{
    [Table("Factura_Nota_Credito_Detalle")]
    public class FacturaNotaCreditoDetalle
    {
        [Key, Column(name: "Detalle_Id", Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int DetalleId { get; set; }

        [Key, Column(name: "Factura_Id", Order = 1)]
        public long FacturaId { get; set; }

        [ForeignKey("FacturaId")]
        public FacturaNotaCredito Factura { get; set; }

        [Column("Producto_Id")]
        [StringLength(50)]
        public string ProductoId { get; set; }

        [ForeignKey("ProductoId")]
        public Producto Producto { get; set; }        

        [Column("Unidad_Id")]
        public long UnidadId { get; set; }

        [ForeignKey("UnidadId")]
        public Unidad Unidad { get; set; }

        [StringLength(400)]
        public string Nombre { get; set; }
        
        public decimal Cantidad { get; set; }      

        public decimal Precio { get; set; }       
    }
}
