using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Recibo_Envase_Detalle")]
    public class ReciboEnvaseDetalle
    {
        [Key, Column(name: "Detalle_Id", Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int DetalleId { get; set; }

        [Key, Column(name: "Recibo_Envase_Id", Order = 1)]
        public long ReciboEnvaseId { get; set; }

        [ForeignKey("ReciboEnvaseId")]
        public ReciboEnvase ReciboEnvase { get; set; }
        
        [Column("Producto_Id")]
        [StringLength(50)]
        public string ProductoId { get; set; }

        [ForeignKey("ProductoId")]
        public Producto Producto { get; set; }

        [Column("Unidad_Id")]
        public long UnidadId { get; set; }

        [ForeignKey("UnidadId")]
        public Unidad Unidad { get; set; }
      
        public decimal Cantidad { get; set; }

        [Column("Cantidad_Envase")]
        public int CantidadEnvase { get; set; }
    }
}
