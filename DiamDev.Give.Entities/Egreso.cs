using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Egreso")]
    public class Egreso
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Egreso_Id")]
        public long EgresoId { get; set; }

        [Column("Agencia_Id")]
        public long AgenciaId { get; set; }

        [ForeignKey("AgenciaId")]
        public Agencia Agencia { get; set; }
              
        public string Observaciones { get; set; }

        [Column("Usr_Inicial")]
        public long UsrInicial { get; set; }

        [ForeignKey("UsrInicial")]
        public Usuario UsuarioInicial { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }

        public List<EgresoDetalle> Detalles { get; set; }
    }
}
