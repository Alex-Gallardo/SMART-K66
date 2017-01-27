using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Precio")]
    public class Precio
    {
        [Key]
        [Column("Precio_Id")]
        public int PrecioId { get; set; }

        [Required(ErrorMessage = "El nombre del precio es requerido")]
        [StringLength(300)]
        public string Nombre { get; set; }

        public bool Activo { get; set; }
    }
}
