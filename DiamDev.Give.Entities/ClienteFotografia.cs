using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Cliente_Fotografia")]
    public class ClienteFotografia
    {
        [Key, Column(name: "Fotografia_Id", Order = 0)]
        public int FotografiaId { get; set; }

        [Key, Column(name: "Cliente_Id", Order = 1)]
        public long ClienteId { get; set; }

        [ForeignKey("ClienteId")]
        public Cliente Cliente { get; set; }

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
