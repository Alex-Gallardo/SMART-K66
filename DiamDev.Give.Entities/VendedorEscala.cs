using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Vendedor_Escala")]
    public class VendedorEscala
    {
        [Key, Column(name: "Escala_Id", Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int EscalaId { get; set; }
        
        [Key, Column(name: "Vendedor_Id", Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long VendedorId { get; set; }

        [ForeignKey("VendedorId")]
        public Vendedor Vendedor { get; set; }
              
        public decimal Inicio { get; set; }
              
        public decimal Fin { get; set; }

        public decimal Porcentaje { get; set; }
    }
}
