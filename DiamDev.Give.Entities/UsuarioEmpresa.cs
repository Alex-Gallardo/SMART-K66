using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiamDev.Give.Entities
{
    [Table("Usuario_Empresa")]
    public class UsuarioEmpresa
    {
        [Key, Column(name: "Usuario_Id", Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public Usuario Usuario { get; set; }

        [Key, Column(name: "Empresa_Id", Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long EmpresaId { get; set; }

        [ForeignKey("EmpresaId")]
        public Empresa Empresa { get; set; }

        [Key, Column(name: "Codigo", Order = 2)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string Codigo { get; set; }

        [Column("SERIE_SAP")]
        public string SERIE_SAP { get; set; }
        /// <summary>
        /// DEPTO de la serie de recibos de caja para este operador.
        /// Empata con REC_CAJA_SERIES.DEPTO (junto con la EMPRESA).
        /// NULL = este operador NO emite recibos (se oculta del select "Operar como").
        /// </summary>
        public string DEPTO_RECIBO { get; set; }
    }
}
