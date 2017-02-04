using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Personal")]
    public class Personal
    {
        [Key, Column(name: "Personal_Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long PersonalId { get; set; }
      
        [StringLength(300)]
        [Required(ErrorMessage = "El nombre del Personal es requerido")]
        public string Nombre { get; set; }

        [StringLength(500)]
        [Required(ErrorMessage = "La dirección del Personal es requerida")]
        public string Direccion { get; set; }
        
        [StringLength(20)]
        [Column("No_Telefono")]
        public string NoTelefono { get; set; }

        [StringLength(20)]
        [Column("No_Telefono_Alterno")]
        public string NoTelefonoAlterno { get; set; }

        [StringLength(20)]
        [Column("No_Celular_Principal")]
        public string NoCelularPrincipal { get; set; }

        [StringLength(20)]
        [Column("No_Celular_Alterno")]
        public string NoCelularAlterno { get; set; }

        [StringLength(100)]
        public string Email { get; set; }

        public bool Activo { get; set; }

        public byte[] Huella { get; set; }

        [Column("Template_Bytes")]
        public byte[] TemplateBytes { get; set; }

        [Column("Template_Size")]
        public int TemplateSize { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }

        public List<PersonalHorario> Horarios { get; set; }
    }
}
