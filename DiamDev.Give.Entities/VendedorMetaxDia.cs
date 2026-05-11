using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Vendedor_Meta_x_Dia")]
    public class VendedorMetaxDia
    {
        [Key, Column(name: "Meta_Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid MetaId { get; set; }

        [Column("Vendedor_Id")]
        public long VendedorId { get; set; }

        [ForeignKey("VendedorId")]
        public Vendedor Vendedor { get; set; }

        public DateTime Fecha { get; set; }

        [Column("Monto_x_Dia")]
        public decimal MontoxDia { get; set; }     

        [Column("Responsable_Id")]
        public long ResponsableId { get; set; }

        [ForeignKey("ResponsableId")]
        public Usuario Responsable { get; set; }
    }
}
