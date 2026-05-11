using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("DireccionCliente")]
    public  class DireccionCliente
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("Direccion_Id")]
        public int DireccionId { get; set; }

        [Required(ErrorMessage = "La Direccion es obligatoria")]
        [StringLength(300)]
        public string Direccion { get; set; }

           
        
        public decimal? Latitud { get; set; }
        public decimal? Longitud { get; set; }

        [Column("Cliente_Id")]
        public long ClienteId { get; set; }

        [ForeignKey("ClienteId")]
        public Cliente Cliente { get; set; }

        [Column("Localidad_Id")]
        public long? LocalidadId { get; set; }

        [ForeignKey("LocalidadId")]
        public Localidad Localid { get; set; }


    }
}
