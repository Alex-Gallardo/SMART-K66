using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Nomina")]
    public class Nomina
    {
        [Key, Column(name: "Nomina_Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long NominaId { get; set; }

        [Column("Tipo_Id")]
        public int TipoId { get; set; }

        [ForeignKey("TipoId")]
        public NominaTipo Tipo { get; set; }

        [Column("Fecha_Inicial")]
        public DateTime FechaInicial { get; set; }

        [Column("Fecha_Final")]
        public DateTime FechaFinal { get; set; }

        public string Descripcion { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }

        public List<NominaDetalle> Detalles { get; set; }
    }
}
