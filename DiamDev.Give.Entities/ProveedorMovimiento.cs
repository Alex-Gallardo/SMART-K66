using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Proveedor_Movimiento")]
    public class ProveedorMovimiento
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Movimiento_Id")]
        public long MovimientoId { get; set; }

        [Column("Tipo_Id")]
        public int TipoId { get; set; }

        [ForeignKey("TipoId")]
        public ProveedorMovimientoTipo Tipo { get; set; }

        [Column("Proveedor_Id")]
        public long ProveedorId { get; set; }

        [ForeignKey("ProveedorId")]
        public Proveedor Proveedor { get; set; }

        [StringLength(150)]
        public string Documento { get; set; }

        [Column("Dias_Credito")]
        public int? DiasCredito { get; set; }

        [Column("Fecha_Movimiento")]
        public DateTime FechaMovimiento { get; set; }

        [Column("Fecha_Vencimiento")]
        public DateTime? FechaVencimiento { get; set; }

        public decimal Monto { get; set; }

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

        public string Observaciones { get; set; }
        
        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }

        public List<ProveedorMovimientoFotografia> Fotografias { get; set; }

        [NotMapped]
        public long CreditoId { get; set; }
    }
}
