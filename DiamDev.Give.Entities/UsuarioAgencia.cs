using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Usuario_Agencia")]
    public class UsuarioAgencia
    {
        [Key, Column(name: "Usuario_Id", Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public Usuario Usuario { get; set; }

        [Key, Column(name: "Agencia_Id", Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long AgenciaId { get; set; }

        [ForeignKey("AgenciaId")]
        public Agencia Agencia { get; set; }
    }
}
