using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Vendedor")]
    public class Vendedor
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Vendedor_Id")]
        public long VendedorId { get; set; }

        [Column("Empresa_Id")]
        public long? EmpresaId { get; set; }

        [ForeignKey("EmpresaId")]
        public Empresa Empresa { get; set; }

        [StringLength(300)]
        [Required(ErrorMessage = "El nombre del vendedor es requerido")]
        public string Nombre { get; set; }

        public bool Activo { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }

        public List<VendedorAgencia> Agencias { get; set; }

        public List<VendedorEscala> Escalas { get; set; }

        public List<VendedorMeta> Metas { get; set; }

        public List<VendedorMetaxDia> MetasxDia { get; set; }

        [NotMapped]
        public long ResponsableId { get; set; }
    }
}
