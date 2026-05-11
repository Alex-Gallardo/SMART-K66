using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Transporte")]
    public class Transporte
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Transporte_Id")]
        public long TransporteId { get; set; }

        [StringLength(500)]
        public string Nombre { get; set; }

        [StringLength(1000)]
        public string Descripcion { get; set; }

        [StringLength(1000)]
        [Column("Descripcion_Empaque")]
        public string DescripcionEmpaque { get; set; }

        [StringLength(200)]
        [Column("Sitio_Web")]
        public string SitioWeb { get; set; }

        [StringLength(500)]
        public string Contacto { get; set; }

        [StringLength(20)]
        [Column("No_Telefono")]
        public string NoTelefono { get; set; }

        [StringLength(20)]
        public string Nit { get; set; }

        [Column("Nombre_Pago")]
        public string NombrePago { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }
    }
}
