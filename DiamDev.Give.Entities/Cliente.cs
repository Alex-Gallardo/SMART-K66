using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Cliente")]
    public class Cliente
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Cliente_Id")]
        public long ClienteId { get; set; }

        [StringLength(20)]
        public string Nit { get; set; }

        [StringLength(300)]
        [Required(ErrorMessage = "El nombre del Cliente es requerido")]
        public string Nombre { get; set; }

        [StringLength(500)]
        [Required(ErrorMessage = "La dirección del Cliente es requerida")]
        public string Direccion { get; set; }

        [StringLength(20)]
        public string DPI { get; set; }

        [StringLength(20)]
        [Column("No_Telefono")]       
        public string NoTelefono { get; set; }

        [Column("Email_Cliente")]
        [StringLength(100)]
        public string EmailCliente { get; set; }

        public int Descuento { get; set; }

        public bool Vip { get; set; }

        public bool Activo { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }     
    }
}
