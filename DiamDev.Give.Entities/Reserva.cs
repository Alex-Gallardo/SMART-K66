using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Reserva")]
    public class Reserva
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Reserva_Id")]
        public long ReservaId { get; set; }
      
        [Column("Agencia_Id")]
        public long AgenciaId { get; set; }

        [ForeignKey("AgenciaId")]
        public Agencia Agencia { get; set; }

        [Column("Cliente_Id")]
        public long ClienteId { get; set; }

        [ForeignKey("ClienteId")]
        public Cliente Cliente { get; set; }

        [StringLength(15)]
        public string Telefono { get; set; }
        
        public bool Operado { get; set; }
        
        public bool Anulada { get; set; }

        [Column("Usr_Creo")]
        public long UsrCreo { get; set; }

        [ForeignKey("UsrCreo")]
        public Usuario UsuarioCreo { get; set; }

        [Column("Usr_Anular")]
        public long? UsrAnular { get; set; }

        [ForeignKey("UsrAnular")]
        public Usuario UsuarioAnular { get; set; }

        [Column("Fecha_Anular")]
        public DateTime? FechaAnular { get; set; }

        public string Observaciones { get; set; }

        public string Comentario { get; set; }

        public DateTime Fecha { get; set; }

        [Column("Fecha_Hora_Reserva")]
        public DateTime? FechaHoraReserva { get; set; }
        
        public int Correlativo { get; set; }

        public List<ReservaDetalle> Detalles { get; set; }

        public List<ReservaPago> Pagos { get; set; }

        [NotMapped]
        public string Productos { get; set; }
    }
}
