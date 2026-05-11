using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Reparacion_Fotografia")]
    public class ReparacionFotografia
    {
        [Key, Column(name: "Fotografia_Id", Order = 0)]
        public int FotografiaId { get; set; }

        [Key, Column(name: "Reparacion_Id", Order = 1)]
        public long ReparacionId { get; set; }

        [ForeignKey("ReparacionId")]
        public Reparacion Reparacion { get; set; }

        [StringLength(200)]
        public string Nombre { get; set; }

        [NotMapped]
        public string Remoto { get; set; }

        [StringLength(150)]
        public string ContentType { get; set; }

        public int Length { get; set; }

        public byte[] Content { get; set; }
    }
}
