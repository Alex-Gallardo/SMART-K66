using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Politica_Categoria_Politica")]
    public class PoliticaCategoriaPolitica
    {
        [Key, Column(name: "Politica_Categoria_Id", Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long PoliticaCategoriaId { get; set; }

        [ForeignKey("PoliticaCategoriaId")]
        public PoliticaCategoria PoliticaCategoria { get; set; }

        [Key, Column(name: "Politica_Id", Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long PoliticaId { get; set; }

        [ForeignKey("PoliticaId")]
        public Politica Politica { get; set; }
    }
}
