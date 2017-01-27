using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Rol_Permiso")]
    public class RolPermiso
    {
        [Key, Column(name: "Rol_Id", Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int RolId { get; set; }

        [Key, Column(name: "Permiso_Id", Order = 1)]
        [StringLength(100)]
        public string PermisoId { get; set; }

        [ForeignKey("PermisoId")]
        public Permiso Permiso { get; set; }
    }
}
