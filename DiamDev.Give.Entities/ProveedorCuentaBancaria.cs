using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Proveedor_Cuenta_Bancaria")]
    public class ProveedorCuentaBancaria
    {
        [Key, Column(name: "Detalle_Id", Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int DetalleId { get; set; }

        [Key, Column(name: "Proveedor_Id", Order = 1)]
        public long ProveedorId { get; set; }

        [ForeignKey("ProveedorId")]
        public Proveedor Proveedor { get; set; }

        [Column("Banco_Id")]
        public long BancoId { get; set; }

        [ForeignKey("BancoId")]
        public Banco Banco { get; set; }

        [StringLength(150)]
        public string Cuenta { get; set; }
    }
}
