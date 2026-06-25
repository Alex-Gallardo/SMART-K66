using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiamDev.Give.Entities
{
    /// <summary>
    /// Mapea un usuario de POS (Usuario_Id) a su DEPTO de serie de recibos.
    /// Mapeo explícito de tabla y columnas para que EF no use convenciones.
    /// </summary>
    [Table("RecibosCaja_UsuarioDepto")]
    public class RecibosCajaUsuarioDepto
    {
        [Key]
        [Column("UsuarioId")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]  // la PK NO es identity
        public long UsuarioId { get; set; }

        [Column("Depto")]
        public string Depto { get; set; }

        [Column("Activo")]
        public bool Activo { get; set; }
    }
}