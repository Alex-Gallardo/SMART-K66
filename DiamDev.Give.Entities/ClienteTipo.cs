using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Cliente_Tipo")]
    public class ClienteTipo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Tipo_Id")]
        public long TipoId { get; set; }

        [StringLength(300)]
        [Required(ErrorMessage = "El nombre del tipo del cliente es requerido")]
        public string Nombre { get; set; }

        [StringLength(500)]
        public string Descripcion { get; set; }

        [StringLength(500)]
        public string Motivo { get; set; }

        [Column("Porcentaje_Descuento")]
        public int PorcentajeDescuento { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }
    }
}
