using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Vendedor_Agencia")]
    public class VendedorAgencia
    {
        [Key, Column(name: "Vendedor_Id", Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long VendedorId { get; set; }

        [ForeignKey("VendedorId")]
        public Vendedor Vendedor { get; set; }
        
        [Key, Column(name: "Agencia_Id", Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long AgenciaId { get; set; }

        [ForeignKey("AgenciaId")]
        public Agencia Agencia { get; set; }      
    }
}
