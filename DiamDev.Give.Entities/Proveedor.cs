using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Proveedor")]
    public class Proveedor
    {
        [Key]
        [DatabaseGenerated(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None)]
        [Column("Proveedor_Id")]
        public long ProveedorId { get; set; }

        [StringLength(20)]
        public string Nit { get; set; }

        [StringLength(300)]
        [Required(ErrorMessage = "El nombre del proveedor es requerido")]
        public string Nombre { get; set; }

        [StringLength(300)]
        [Column("Nombre_Cheque")]
        public string NombreCheque { get; set; }

        [StringLength(500)]
        [Required(ErrorMessage = "La dirección del proveedor es requerida")]
        public string Direccion { get; set; }

        [StringLength(20)]
        [Column("No_Telefono_Oficina")]
        [Required(ErrorMessage = "El teléfono de oficina es requerido")]
        public string NoTelefonoOficina { get; set; }

        [Column("Email_Proveedor")]
        [StringLength(100)]
        public string EmailProveedor { get; set; }

        [StringLength(300)]
        public string Patente { get; set; }

        [StringLength(300)]
        public string Contacto { get; set; }

        [Column("No_Telefono_Contacto")]
        [StringLength(20)]
        public string NoTelefonoContacto { get; set; }

        [Column("Email_Contacto")]
        [StringLength(100)]
        public string EmailContacto { get; set; }

        public bool Activo { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }

        public List<ProveedorProducto> Productos { get; set; }

        [NotMapped]
        public List<MovimientoHistorial> IngresoHistorial { get; set; }
    }
}
