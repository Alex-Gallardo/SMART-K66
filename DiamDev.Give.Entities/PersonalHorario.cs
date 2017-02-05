using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Personal_Horario")]
    public class PersonalHorario
    {
        [Key,Column(name:"Personal_Id", Order = 0)]
        public long PersonalId { get; set; }

        [ForeignKey("PersonalId")]
        public Personal Personal { get; set; }

        [Key, Column(name: "Fecha", Order = 1)]
        public DateTime Fecha { get; set; }

        public DateTime Entrada { get; set; }

        public DateTime? Salida { get; set; }
    }
}
