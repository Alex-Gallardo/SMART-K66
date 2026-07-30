using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiamDev.Give.Entities
{
    [Table("Pedido_Tipo_K66")]
    public class PedidoTipoK66
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("Tipo_Id")]
        public Guid TipoId { get; set; }

        [Column("Empresa_Id")]
        public long EmpresaId { get; set; }

        [ForeignKey("EmpresaId")]
        public Empresa Empresa { get; set; }

        public string Nombre { get; set; }

        public string Descripcion { get; set; }

        [Column("Codigo_Intregracion_1")]
        public string CodigoIntregracion1 { get; set; }

        [Column("Codigo_Intregracion_2")]
        public string CodigoIntregracion2 { get; set; }

        [Column("Responsable_Id")]
        public long ResponsableId { get; set; }

        [ForeignKey("ResponsableId")]
        public Usuario Responsable { get; set; }  

        public DateTime Fecha { get; set; }      
    }
}
