using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Reparacion_Politica_Categoria")]
    public class ReparacionPoliticaCategoria
    {
        [Key, Column("Reparacion_Id", Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long ReparacionId { get; set; }

        [Key, Column("Politica_Categoria_Id", Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long PoliticaCategoriaId { get; set; }

        [ForeignKey("PoliticaCategoriaId")]
        public PoliticaCategoria Politica { get; set; }

        [Column("Orden_Id")]
        public int OrdenId { get; set; }
    }
}
