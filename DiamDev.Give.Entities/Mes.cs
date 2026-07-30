using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Mes")]
    public class Mes
    {
        [Key]
        [Column("Mes_Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int MesId { get; set; }

        [StringLength(200)]
        public string Nombre { get; set; }
    }
}
