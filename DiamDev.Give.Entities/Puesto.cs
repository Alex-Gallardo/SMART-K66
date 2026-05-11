using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Puesto")]
    public class Puesto
    {
        [Key, Column(name: "Puesto_Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long PuestoId { get; set; }

        [Required]
        [StringLength(200)]
        public string Nombre { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }
    }
}
