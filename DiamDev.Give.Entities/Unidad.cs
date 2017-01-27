using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Unidad")]
    public class Unidad
    {
        [Key, Column(name: "Unidad_Id")]
        [DatabaseGenerated(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None)]
        public long UnidadId { get; set; }

        [Required(ErrorMessage = "El codigo del producto es requerido por INFILE")]
        [StringLength(100)]
        public string Codigo { get; set; }

        [Required(ErrorMessage = "El Nombre es Requerido")]
        [StringLength(500)]
        public string Nombre { get; set; }

        public bool Activo { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }
    }
}
