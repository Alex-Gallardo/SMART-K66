using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Movimiento")]
    public class Movimiento
    {
        [Key, Column(name: "Movimiento_Id")]
        [DatabaseGenerated(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None)]
        public long MovimientoId { get; set; }

        [Column("Agencia_Id")]
        public long AgenciaId { get; set; }

        [ForeignKey("AgenciaId")]
        public Agencia Agencia { get; set; }

        [Column("Movimiento_Tipo_Id")]
        public int MovimientoTipoId { get; set; }

        [ForeignKey("MovimientoTipoId")]
        public MovimientoTipo MovimientoTipo { get; set; }

        [Column("Proveedor_Id")]
        public long? ProveedorId { get; set; }

        [ForeignKey("ProveedorId")]
        public Proveedor Proveedor { get; set; }

        [Column("Cliente_Id")]
        public long? ClienteId { get; set; }

        [ForeignKey("ClienteId")]
        public Cliente Cliente { get; set; }

        [Required]
        public string Descripcion { get; set; }

        public int Descuento { get; set; }

        [NotMapped]
        public decimal DescuentoTotal { get; set; }

        [NotMapped]
        public decimal Total { get; set; }

        public bool Operado { get; set; }

        public DateTime Fecha { get; set; }

        [Column("Usr_Creo")]
        public long UsrCreo { get; set; }

        [ForeignKey("UsrCreo")]
        public Usuario UsuarioCreo { get; set; }

        public int Correlativo { get; set; }

        public List<MovimientoDetalle> Detalles { get; set; }

        public List<MovimientoFormaPago> Pagos { get; set; }
    }
}
