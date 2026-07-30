using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Rol")]
    public class Rol
    {
        [Key, Column(name: "Rol_Id")]
        public int RolId { get; set; }

        [Required]
        [StringLength(150)]
        public string Nombre { get; set; }

        public List<RolPermiso> Permisos { get; set; }

        [NotMapped]
        public List<Permiso> PermisoIds { get; set; }
    }
}
