using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Unidad_Conversion")]
    public class UnidadConversion
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Conversion_Id")]
        public long ConversionId { get; set; }

        [Column("Operacion_Id")]
        public int OperacionId { get; set; }

        [ForeignKey("OperacionId")]
        public UnidadOperacion Operacion { get; set; }
        
        [Column("Unidad_Base_Id")]
        public long UnidadBaseId { get; set; }

        [ForeignKey("UnidadBaseId")]
        public Unidad UnidadBase { get; set; }

        [Column("Cantidad_Base")]
        public decimal CantidadBase { get; set; }

        [Column("Unidad_Destino_Id")]
        public long UnidadDestinoId { get; set; }

        [ForeignKey("UnidadDestinoId")]
        public Unidad UnidadDestino { get; set; }

        [Column("Cantidad_Destino")]
        public decimal CantidadDestino { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }
    }
}
