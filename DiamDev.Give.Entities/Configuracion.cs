using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Configuracion")]
    public class Configuracion
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Configuracion_Id")]
        public long ConfiguracionId { get; set; }

        [Column("Configuracion_Padre_Id")]
        public long? ConfiguracionPadreId { get; set; }

        [StringLength(250)]
        public string Nombre { get; set; }

        [StringLength(200)]
        public string Identificador { get; set; }

        [StringLength(200)]
        public string Valor { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }
    }
}
