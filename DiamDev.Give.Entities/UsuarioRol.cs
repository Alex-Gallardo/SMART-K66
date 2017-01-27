using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Usuario_Rol")]
    public class UsuarioRol
    {
        [Key, Column(name: "Usuario_Id", Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public Usuario Usuario { get; set; }

        [Key, Column(name: "Rol_Id", Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int RolId { get; set; }

        [ForeignKey("RolId")]
        public Rol Rol { get; set; }
    }
}
