using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Usuario")]
    public class Usuario
    {

        [Key, Column(name: "Usuario_Id")]
        [DatabaseGenerated(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None)]
        public long UsuarioId { get; set; }

        [Required(ErrorMessage = "El usuario es requerido")]
        [StringLength(50)]
        public string Login { get; set; }

        [Required(ErrorMessage = "El password es requerido")]
        [StringLength(150)]
        public string Password { get; set; }

        [StringLength(150)]
        [Column("Password_Android")]
        public string PasswordAndroid { get; set; }

        [NotMapped]
        public string NuevoPassword { get; set; }

        [Required(ErrorMessage = "El Nombre es Requerido")]
        [StringLength(200)]
        public string Nombre { get; set; }

        public DateTime Fecha { get; set; }

        [Column("Fecha_Ultima_Actividad")]
        public DateTime? FechaUltimaActividad { get; set; }

        [Column("Reiniciar_Password")]
        public bool ReiniciarPassword { get; set; }

        [Required]
        [Column("Autenticar_Site")]
        public bool AutenticarSite { get; set; }

        [Required]
        [Column("Autenticar_Android")]
        public bool AutenticarAndroid { get; set; }

        public bool Activo { get; set; }

        public int Correlativo { get; set; }

        public List<UsuarioRol> Roles { get; set; }

        public List<UsuarioAgencia> Agencias { get; set; } 

        [NotMapped]
        public List<RolPermiso> RolesPermiso { get; set; }
    }
}
