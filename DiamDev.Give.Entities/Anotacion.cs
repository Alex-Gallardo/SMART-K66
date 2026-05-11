using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Anotacion")]
    public class Anotacion
    {
        [Key, Column(name: "Anotacion_Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long AnotacionId { get; set; }

        [Column("Personal_Id")]
        public long PersonalId { get; set; }

        [ForeignKey("PersonalId")]
        public Personal Personal { get; set; }

        [Column("Tipo_Id")]
        public long TipoId { get; set; }

        [ForeignKey("TipoId")]
        public AnotacionTipo Tipo { get; set; }

        [Column("Fecha_Inicial")]
        public DateTime FechaInicial { get; set; }

        [Column("Fecha_Final")]
        public DateTime FechaFinal { get; set; }

        [StringLength(500)]
        public string Descripcion { get; set; }

        public decimal Monto { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }
    }
}
