using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiamDev.Give.Entities
{
    [Table("Estado_Smart_K66")]
    public class EstadoSmartK66
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Estado_Id")]
        public int EstadoId { get; set; }
        
        public string Nombre { get; set; }

        public string Descripcion { get; set; }
    }
}
