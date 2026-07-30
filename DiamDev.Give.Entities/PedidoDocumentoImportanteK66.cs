using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiamDev.Give.Entities
{
    [Table("Pedido_Documento_Importante_K66")]
    public class PedidoDocumentoImportanteK66
    {
        [Key, Column(name:"Documento_Id", Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]      
        public int DocumentoId { get; set; }

        [Key, Column(name: "Pedido_Id", Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long PedidoId { get; set; }

        [ForeignKey("PedidoId")]
        public PedidoK66 Pedido { get; set; }

        public string Nombre { get; set; }

        public string FotografiaApp { get; set; }
    }
}