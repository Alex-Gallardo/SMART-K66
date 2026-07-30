using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Credito")]
    public class Credito
    {
        [Key]
        [DatabaseGenerated(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None)]
        [Column("Credito_Id")]
        public long CreditoId { get; set; }

        [Column("Tipo_Id")]
        public int TipoId { get; set; }

        [ForeignKey("TipoId")]
        public CreditoTipo Tipo { get; set; }

        [Column("Agencia_Id")]
        public long AgenciaId { get; set; }

        [ForeignKey("AgenciaId")]
        public Agencia Agencia { get; set; }

        [Column("Cliente_Id")]
        public long? ClienteId { get; set; }

        [ForeignKey("ClienteId")]
        public Cliente Cliente { get; set; }       
                
        [StringLength(50)]
        public string Serie { get; set; }

        [StringLength(50)]
        public string Factura { get; set; }

        [Required(ErrorMessage = "La descripción es requerida")]
        public string Descripcion { get; set; }

        public string Comentario { get; set; }

        [Column("Fecha_Inicial")]
        public DateTime FechaInicial { get; set; }

        [Column("Fecha_Final")]
        public DateTime FechaFinal { get; set; }

        public bool Finalizado { get; set; }

        public bool Anulada { get; set; }

        [Column("Usr_Inicial")]
        public long UsrInicial { get; set; }

        [ForeignKey("UsrInicial")]
        public Usuario UsuarioInicial { get; set; }

        [Column("Usr_Final")]
        public long? UsrFinal { get; set; }

        [ForeignKey("UsrFinal")]
        public Usuario UsuarioFinal { get; set; }

        [Column("Usr_Anular")]
        public long? UsrAnular { get; set; }

        [ForeignKey("UsrAnular")]
        public Usuario UsuarioAnular { get; set; }

        [Column("Fecha_Cancelacion")]
        public DateTime? FechaCancelacion { get; set; }

        public DateTime Fecha { get; set; }

        [Column("Fecha_Anular")]
        public DateTime? FechaAnular { get; set; }

        public int Correlativo { get; set; }

        public List<CreditoDetalle> Detalles { get; set; }

        public List<CreditoAnotacion> Comentarios { get; set; }

        public List<CreditoPago> Pagos { get; set; }

    }
}
