using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Diario_Detalle")]
    public class DiarioDetalle
    {
        [Key, Column(name: "Detalle_Id", Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int DetalleId { get; set; }

        [Key, Column(name: "Diario_Id", Order = 1)]
        public long DiarioId { get; set; }

        [ForeignKey("DiarioId")]
        public Diario Diario { get; set; }

        [Column("Cuenta_Id")]
        public long CuentaId { get; set; }

        [ForeignKey("CuentaId")]
        public CuentaContable Cuenta { get; set; }

        public decimal Debe { get; set; }

        public decimal Haber { get; set; }
    }
}
