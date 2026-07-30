using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Nota_Credito")]
    public class NotaCredito
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Credito_Id")]
        public long CreditoId { get; set; }

        [Column("Agencia_Id")]
        public long AgenciaId { get; set; }

        [ForeignKey("AgenciaId")]
        public Agencia Agencia { get; set; }

        [Column("Cliente_Id")]
        public long ClienteId { get; set; }

        [ForeignKey("ClienteId")]
        public Cliente Cliente { get; set; }       

        [Column("Factura_Id")]
        public long? FacturaId { get; set; }

        [ForeignKey("FacturaId")]
        public Factura Factura { get; set; }
        
        [StringLength(15)]
        public string Serie { get; set; }

        [StringLength(30)]
        [Column("No_Nota_Credito")]
        public string NoNotaCredito { get; set; }
                       
        public decimal Monto { get; set; }

        public string Nota { get; set; }

        public bool Devolucion { get; set; }

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

        public string Comentario { get; set; }
        
        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }
        
        public List<NotaCreditoFormaPago> Pagos { get; set; }

        [NotMapped]
        public int TipoId { get; set; }

        [NotMapped]
        public long? SerieId { get; set; }

        [NotMapped]
        public string NoFactura { get; set; }
    }
}
