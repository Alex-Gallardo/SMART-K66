using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Producto_Precio_Costo_Historial")]
    public class ProductoPrecioCostoHistorial
    {
        [Key]
        [Column("Historial_Id")]
        public long HistorialId { get; set; }

        [Column("Proveedor_Id")]
        public long ProveedorId { get; set; }

        [ForeignKey("ProveedorId")]
        public Proveedor Proveedor { get; set; }

        [Column("Producto_Id")]
        [StringLength(50)]
        public string  ProductoId { get; set; }

        [ForeignKey("ProductoId")]
        public Producto Producto { get; set; }

        [Column("Precio_Costo_Actual")]
        public decimal PrecioCostoActual { get; set; }

        [Column("Precio_Costo_Nuevo")]
        public decimal PrecioCostoNuevo { get; set; }

        [Column("Precio_Costo_Promedio")]
        public decimal PrecioCostoPromedio { get; set; }

        public decimal Cantidad { get; set; }

        [Column("Ingreso_Id")]
        public long IngresoId { get; set; }

        public DateTime Fecha { get; set; }
    }
}
