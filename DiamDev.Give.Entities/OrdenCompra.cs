using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiamDev.Give.Entities
{
    [Table("Orden_Compra")]
    public class OrdenCompra
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Orden_Id")]
        public long OrdenId { get; set; }

        [Column("Agencia_Id")]
        public long AgenciaId { get; set; }

        [ForeignKey("AgenciaId")]
        public Agencia Agencia { get; set; }

        [Column("Proveedor_Id")]
        public long ProveedorId { get; set; }

        [ForeignKey("ProveedorId")]
        public Proveedor Proveedor { get; set; }

        [Column("Moneda_Id")]
        public long MonedaId { get; set; }

        [ForeignKey("MonedaId")]
        public Moneda Moneda { get; set; }

        public string Observaciones { get; set; }

        public string Comentario { get; set; }

        [Column("Fotografia_Orden")]
        public string FotografiaOrden { get; set; }

        public bool Operado { get; set; }

        [Column("Usr_Creo")]
        public long UsrCreo { get; set; }

        [ForeignKey("UsrCreo")]
        public Usuario UsuarioCreo { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }

        public List<OrdenCompraDetalle> Detalles { get; set; }

        [NotMapped]
        public ProductoFotografia Fotografia { get; set; }
    }
}
