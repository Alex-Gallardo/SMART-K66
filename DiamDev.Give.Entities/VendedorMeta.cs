using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Vendedor_Meta")]
    public class VendedorMeta
    {
        [Key, Column(name: "Meta_Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid MetaId { get; set; }

        [Column("Vendedor_Id")]
        public long VendedorId { get; set; }

        [ForeignKey("VendedorId")]
        public Vendedor Vendedor { get; set; }

        [Column("Mes_Id")]
        public int MesId { get; set; }

        [ForeignKey("MesId")]
        public Mes Mes { get; set; }

        public int Anio { get; set; }

        [Column("Monto_Mensual_Meta")]
        public decimal MontoMensualMeta { get; set; }

        [Column("Monto_Mensual_Real")]
        public decimal MontoMensualReal { get; set; }

        [Column("Responsable_Id")]
        public long ResponsableId { get; set; }

        [ForeignKey("ResponsableId")]
        public Usuario Responsable { get; set; }
    }
}
