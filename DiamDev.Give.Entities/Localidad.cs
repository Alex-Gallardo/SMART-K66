using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Localidad")]
    public  class Localidad
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("Localidad_Id")]
        public long LocalidadId { get; set; }

        [Required(ErrorMessage = "El nombre de la localidad es requerido")]
        [StringLength(300)]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "Descripcion de la localidad es requerido")]
        public string Descripcion { get; set; }

        public Decimal CostoEnvio { get; set; }
        
        public bool Activo { get; set; }

        [Column("Municipio_Id")]
        public long MunicipioId { get; set; }

        [ForeignKey("MunicipioId")]
        public Municipio Municipio { get; set; }


        [Column("Agencia_Id")]
        public long? AgenciaId { get; set; }

        [ForeignKey("AgenciaId")]
        public Agencia Agencia { get; set; }

    }
}
