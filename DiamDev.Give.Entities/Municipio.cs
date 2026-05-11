using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Municipio")]
    public  class Municipio
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("Municipio_Id")]
        public long MunicipioId { get; set; }

        [Required(ErrorMessage = "El nombre del municipio es requerido")]
        [StringLength(300)]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "Descripcion del Municipio es requerido")]
        public string Descripcion { get; set; }

        public bool Activo { get; set; }


    }
}
