using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Garantia")]
    public class Garantia
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Garantia_Id")]
        public long GarantiaId { get; set; }

        [Column("Documento_Id")]
        public int DocumentoId { get; set; }

        [ForeignKey("DocumentoId")]
        public GarantiaDocumento Documento { get; set; }

        [Column("Factura_Id")]
        public long? FacturaId { get; set; }

        [ForeignKey("FacturaId")]
        public Factura Factura { get; set; }

        [Column("Recibo_Id")]
        public long? ReciboId { get; set; }

        [ForeignKey("ReciboId")]
        public Recibo Recibo { get; set; }
              
        public string Observaciones { get; set; }

        [Column("Usr_Creo")]
        public long UsrCreo { get; set; }

        [ForeignKey("UsrCreo")]
        public Usuario UsuarioCreo { get; set; }

        [Column("Usr_Entrega")]
        public long? UsrEntrega { get; set; }

        [ForeignKey("UsrEntrega")]
        public Usuario UsuarioEntrega { get; set; }

        [Column("Fecha_Entrega")]
        public DateTime? FechaEntrega { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }
        
        public List<GarantiaDetalle> Detalles { get; set; }
    }
}
