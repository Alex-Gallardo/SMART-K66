using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Politica_Tipo")]
    public class PoliticaTipo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Politica_Tipo_Id")]
        public int PoliticaTipoId { get; set; }

        [Required(ErrorMessage = "El nombre del tipo de politica es requerido")]
        public string Nombre { get; set; }      
    }
}
