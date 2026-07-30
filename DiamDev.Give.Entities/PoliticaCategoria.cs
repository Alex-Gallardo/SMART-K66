using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Politica_Categoria")]
    public class PoliticaCategoria
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Politica_Categoria_Id")]
        public long PoliticaCategoriaId { get; set; }

        [Column("Tipo_Id")]
        public int TipoId { get; set; }

        [ForeignKey("TipoId")]
        public PoliticaTipo Tipo { get; set; }

        [Required(ErrorMessage = "El nombre de la categoria de la politica es requerida")]
        public string Nombre { get; set; }

        public bool Activo { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }

        public List<PoliticaCategoriaPolitica> Politicas { get; set; }
    }
}
