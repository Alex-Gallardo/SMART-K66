using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Recibo_Envase")]
    public class ReciboEnvase
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Recibo_Envase_Id")]
        public long ReciboEnvaseId { get; set; }

        [Column("Recibo_Id")]
        public long ReciboId { get; set; }

        [ForeignKey("ReciboId")]
        public Recibo Recibo { get; set; }

        [Column("Agencia_Id")]
        public long AgenciaId { get; set; }

        [ForeignKey("AgenciaId")]
        public Agencia Agencia { get; set; }        
                
        [Column("Usr_Creo")]
        public long UsrCreo { get; set; }

        [ForeignKey("UsrCreo")]
        public Usuario UsuarioCreo { get; set; }      

        [Column("Usr_Recibe")]
        public long? UsrRecibe { get; set; }

        [ForeignKey("UsrRecibe")]
        public Usuario UsuarioRecibe { get; set; }

        [Column("Fecha_Recibe")]
        public DateTime? FechaRecibe { get; set; }    

        public DateTime Fecha { get; set; }      

        public int Correlativo { get; set; }

        public List<ReciboEnvaseDetalle> Detalles { get; set; }
    }
}
