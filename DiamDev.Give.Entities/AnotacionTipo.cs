using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Anotacion_Tipo")]
    public class AnotacionTipo
    {
        [Key, Column(name: "Tipo_Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long TipoId { get; set; }

        [Required]
        [StringLength(200)]
        public string Nombre { get; set; }

        public bool Descuento { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }
    }
}
