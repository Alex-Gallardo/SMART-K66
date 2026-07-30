using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Reparacion_Servicio")]
    public class ReparacionServicio
    {
        [Key, Column(name: "Reparacion_Id", Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long ReparacionId { get; set; }

        [Key, Column(name: "Servicio_Id", Order = 1)]
        public long ServicioId { get; set; }

        [ForeignKey("ServicioId")]
        public Servicio Servicio { get; set; }

        public bool Estado { get; set; }

        public string Nota { get; set; }
    }
}
