using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("OfertaDelivery")]
    public class OfertaDelivery
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("Oferta_Id")]
        public int OfertaId { get; set; }

        [Required(ErrorMessage = "El nombre de la Oferta Es Requerido")]
        [StringLength(300)]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "Descripcion de de la Oferta Es Requerido")]
        [StringLength(300)]
        public string Descripcion { get; set; }

        public DateTime Fecha { get; set; }

        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd-MM-yyyy}")]
        public DateTime FechaInicioOferta { get; set; }

        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd-MM-yyyy}")]
        public DateTime FechaFinOferta { get; set; }

        [Column("Usr_Creo")]
        public long UsrCreo { get; set; }

        [ForeignKey("UsrCreo")]
        public Usuario UsuarioCreo { get; set; }

        [Column("ProductoBase")]
        [StringLength(50)]
        public string ProductoBaseId { get; set; }

        [ForeignKey("ProductoBaseId")]
        public Producto ProductoBase { get; set; }


    }
}
