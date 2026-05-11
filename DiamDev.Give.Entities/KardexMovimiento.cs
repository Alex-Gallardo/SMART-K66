using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Kardex_Movimiento")]
    public class KardexMovimiento
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Column("Agencia_Id")]
        public long AgenciaId { get; set; }

        [ForeignKey("AgenciaId")]
        public Agencia Agencia { get; set; }

        [Column("Tipo_Id")]
        public int TipoId { get; set; }

        [ForeignKey("TipoId")]
        public KardexMovimientoTipo Tipo { get; set; }

        public DateTime Fecha { get; set; }

        [Column("Fecha_Hora")]
        public DateTime FechaHora { get; set; }

        [Column("Producto_Id")]
        public string ProductoId { get; set; }

        [ForeignKey("ProductoId")]
        public Producto Producto { get; set; }

        [Column("Unidad_Id")]
        public long UnidadId { get; set; }

        [ForeignKey("UnidadId")]
        public Unidad Unidad { get; set; }

        [Column("Documento_Id")]
        public long DocumentoId { get; set; }

        public decimal Cantidad { get; set; }

        public decimal Precio { get; set; }

        [Column("Existencia_Actual")]
        public decimal ExistenciaActual { get; set; }

        [Column("Existencia_Final")]
        public decimal ExistenciaFinal { get; set; }

        [Column("Responsable_Id")]
        public long ResponsableId { get; set; }

        [ForeignKey("ResponsableId")]
        public Usuario Responsable { get; set; }
    }
}
