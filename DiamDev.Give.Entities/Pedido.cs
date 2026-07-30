using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Pedido")]
    public class Pedido
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Pedido_Id")]
        public long PedidoId { get; set; }

        [Column("Agencia_Id")]
        public long AgenciaId { get; set; }

        [ForeignKey("AgenciaId")]
        public Agencia Agencia { get; set; }

        [Column("Cliente_Id")]
        public long ClienteId { get; set; }

        [ForeignKey("ClienteId")]
        public Cliente Cliente { get; set; }

        [Column("Vendedor_Id")]
        public long? VendedorId { get; set; }

        [ForeignKey("VendedorId")]
        public Vendedor Vendedor { get; set; }
       
        [Required(ErrorMessage = "La descripción es requerida")]
        public string Descripcion { get; set; }

        [Column("Forma_Pago")]
        [StringLength(500)]
        public string FormaPago { get; set; }

        [Column("Tiempo_Entrega")]
        [StringLength(500)]
        public string TiempoEntrega { get; set; }

        [Column("Fotografia_Cotizacion")]
        public string FotografiaCotizacion { get; set; }

        public string Comentario { get; set; }

        public bool Operada { get; set; }

        public bool Anulada { get; set; }
        
        [Column("Usr_Creo")]
        public long UsrCreo { get; set; }

        [ForeignKey("UsrCreo")]
        public Usuario UsuarioCreo { get; set; }

        [Column("Usr_Opero")]
        public long? UsrOpero { get; set; }

        [ForeignKey("UsrOpero")]
        public Usuario UsuarioOpero { get; set; }

        [Column("Usr_Anular")]
        public long? UsrAnular { get; set; }

        [ForeignKey("UsrAnular")]
        public Usuario UsuarioAnular { get; set; }

        [Column("Fecha_Hora_Opero")]
        public DateTime? FechaHoraOpero { get; set; }

        [Column("Fecha_Hora_Creacion")]
        public DateTime? FechaHoraCreacion { get; set; }

        [Column("Fecha_Anular")]
        public DateTime? FechaAnular { get; set; }

        public bool Cotizacion { get; set; }
                
        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }

        public List<PedidoDetalle> Detalles { get; set; }   

        [NotMapped]
        public string Nombre { get; set; }

        [NotMapped]
        public ProductoFotografia Fotografia { get; set; }
    }
}
