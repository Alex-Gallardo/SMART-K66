using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Categoria_Gasto")]
    public class CategoriaGasto
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Categoria_Id")]
        public long CategoriaId { get; set; }

        [Required(ErrorMessage = "El nombre de la categoria es requerido")]
        [StringLength(200)]
        public string Nombre { get; set; }

        [StringLength(500)]
        public string Descripcion { get; set; }
        
        public bool Activo { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }
    }
}
