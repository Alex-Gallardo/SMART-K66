using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Region")]
    public class Region
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Region_Id")]
        public long RegionId { get; set; }

        [Required(ErrorMessage = "El nombre de la region es requerido")]
        [StringLength(300)]
        public string Nombre { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }
    }
}
