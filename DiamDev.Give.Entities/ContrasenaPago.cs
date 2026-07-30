using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Contrasena_Pago")]
    public class ContrasenaPago
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Contrasena_Id")]
        public long ContrasenaId { get; set; }

        [Column("Proveedor_Id")]
        public long ProveedorId { get; set; }

        [ForeignKey("ProveedorId")]
        public Proveedor Proveedor { get; set; }

        [Column("Forma_Id")]
        public long FormaId { get; set; }

        [ForeignKey("FormaId")]
        public FormaPago Pago { get; set; }

        [StringLength(150)]
        public string Documento { get; set; }

        [Column("Fecha_Pago")]
        public DateTime FechaPago { get; set; }

        [StringLength(500)]
        public string Comentario { get; set; }

        public decimal Monto { get; set; }

        [Column("Usr_Creo")]
        public long UsrCreo { get; set; }

        [ForeignKey("UsrCreo")]
        public Usuario UsuarioCreo { get; set; }
        
        public bool Operado { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }
    }
}
