using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("TipoServicioCliente")]
    public class ServicioClienteTipo
    {
        [Key, Column(name: "id")]
        public int ID { get; set; }

        [Column("nombretipo")]
        [StringLength(150)]
        public string Nombre { get; set; }
    }
}
