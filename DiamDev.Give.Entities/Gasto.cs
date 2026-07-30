using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Gasto")]
    public class Gasto
    {
        [Key, Column(name: "Gasto_Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long GastoId { get; set; }

        [Column("Agencia_Id")]
        public long? AgenciaId { get; set; }

        [ForeignKey("AgenciaId")]
        public Agencia Agencia { get; set; }

        [Column("Proveedor_Id")]
        public long? ProveedorId { get; set; }

        [ForeignKey("ProveedorId")]
        public Proveedor Proveedor { get; set; }

        [Column("Tipo_Compra_Id")]
        public long? TipoCompraId { get; set; }

        [ForeignKey("TipoCompraId")]
        public TipoCompra TipoCompra { get; set; }

        [Column("Categoria_Id")]
        public long CategoriaId { get; set; }

        [ForeignKey("CategoriaId")]
        public CategoriaGasto Categoria { get; set; }

        [Column("Serie_Factura")]
        [StringLength(150)]
        public string SerieFactura { get; set; }

        [Column("Documento")]
        [StringLength(150)]
        public string Documento { get; set; }
     
        public string Concepto { get; set; }

        public decimal Monto { get; set; }       

        public decimal? IDP { get; set; }

        [Column("Fecha_Factura")]
        public DateTime FechaFactura { get; set; }

        [Column("Fecha_Libro")]
        public DateTime? FechaLibro { get; set; }

        public string Comentario { get; set; }

        public bool Anulada { get; set; }

        [Column("Usr_Creo")]
        public long UsrCreo { get; set; }

        [ForeignKey("UsrCreo")]
        public Usuario UsuarioCreo { get; set; }

        [Column("Usr_Anular")]
        public long? UsrAnular { get; set; }

        [ForeignKey("UsrAnular")]
        public Usuario UsuarioAnular { get; set; }

        public DateTime Fecha { get; set; }

        [Column("Fecha_Anular")]
        public DateTime? FechaAnular { get; set; }

        [Column("Fecha_Hora_Gasto")]
        public DateTime? FechaHoraGasto { get; set; }
        
        public int Correlativo { get; set; }

        public List<GastoFotografia> Fotografias { get; set; }
    }
}
