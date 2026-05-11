using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Producto_Inventario_ID")]
    public class ProductoInventarioID
    {
        [Key, Column(name: "Producto_Id", Order = 0)]
        [StringLength(50)]
        public string ProductoId { get; set; }

        [ForeignKey("ProductoId")]
        public Producto Producto { get; set; }

        [Key, Column(name: "Agencia_Id", Order = 1)]
        public long? AgenciaId { get; set; }

        [ForeignKey("AgenciaId")]
        public Agencia Agencia { get; set; }

        [Key, Column(name: "ID", Order = 2)]
        public string ID { get; set; }

        public bool Operado { get; set; }
    }
}
