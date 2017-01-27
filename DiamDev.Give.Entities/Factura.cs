using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Factura")]
    public class Factura
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Factura_Id")]
        public long FacturaId { get; set; }

        [Column("Agencia_Id")]
        public long AgenciaId { get; set; }

        [ForeignKey("AgenciaId")]
        public Agencia Agencia { get; set; }

        [Column("Serie_Id")]
        public long SerieId { get; set; }

        [ForeignKey("SerieId")]
        public Serie Serie { get; set; }

        [Column("Vendedor_Id")]
        public long VendedorId { get; set; }

        [ForeignKey("VendedorId")]
        public Vendedor Vendedor { get; set; }

        [Column("Cliente_Id")]
        public long ClienteId { get; set; }

        [ForeignKey("ClienteId")]
        public Cliente Cliente { get; set; }       

        public string Comentario { get; set; }

        public int Descuento { get; set; }

        [NotMapped]
        public decimal DescuentoTotal { get; set; }

        [NotMapped]
        public decimal Total { get; set; }

        [Column("No_Factura")]
        public long NoFactura { get; set; }

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

        [Column("Factura_Electronica")]
        public bool FacturaElectronica { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }

        public List<FacturaDetalle> Detalles { get; set; }

        public List<FacturaFormaPago> Pagos { get; set; }
    }
}
