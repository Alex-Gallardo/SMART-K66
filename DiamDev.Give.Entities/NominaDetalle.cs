using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Nomina_Detalle")]
    public class NominaDetalle
    {
        [Key, Column(name: "Detalle_Id", Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int DetalleId { get; set; }

        [Key, Column(name: "Nomina_Id", Order = 1)]
        public long NominaId { get; set; }

        [ForeignKey("NominaId")]
        public Nomina Nomina { get; set; }

        [Column("Personal_Id")]
        public long PersonalId { get; set; }

        [ForeignKey("PersonalId")]
        public Personal Personal { get; set; }

        [StringLength(200)]
        public string Puesto { get; set; }

        public int Dias { get; set; }

        public decimal Sueldo { get; set; }

        public decimal Bonificacion { get; set; }

        [Column("Otros_Ingresos")]
        public decimal OtrosIngresos { get; set; }

        public decimal IGSS { get; set; }

        [Column("Otros_Descuentos")]
        public decimal OtrosDescuentos { get; set; }
    }
}
