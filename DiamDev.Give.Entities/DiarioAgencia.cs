using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Diario_Agencia")]
    public class DiarioAgencia
    {
        [Key, Column(name: "Diario_Id", Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long DiarioId { get; set; }

        [ForeignKey("DiarioId")]
        public Diario Diario { get; set; }

        [Key, Column(name: "Agencia_Id", Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long AgenciaId { get; set; }

        [ForeignKey("AgenciaId")]
        public Agencia Agencia { get; set; }
    }
}
