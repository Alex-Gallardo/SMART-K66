using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Laboratorio")]
    public class Laboratorio
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Laboratorio_Id")]
        public long LaboratorioId { get; set; }

        [Column("Agencia_Id")]
        public long AgenciaId { get; set; }

        [ForeignKey("AgenciaId")]
        public Agencia Agencia { get; set; }

        [Column("Producto_Base_Id")]
        [StringLength(50)]
        public string ProductoBaseId { get; set; }

        [ForeignKey("ProductoBaseId")]
        public Producto ProductoBase { get; set; }

        [Column("Cantidad_Base")]
        public decimal CantidadBase { get; set; }

        [Column("Producto_Destino_Id")]
        [StringLength(50)]
        public string ProductoDestinoId { get; set; }

        [ForeignKey("ProductoDestinoId")]
        public Producto ProductoDestino { get; set; }
        
        [Column("Cantidad_Destino")]
        public decimal CantidadDestino { get; set; }

        [Column("Usr_Creo")]
        public long UsrCreo { get; set; }

        [ForeignKey("UsrCreo")]
        public Usuario UsuarioCreo { get; set; }      

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }
    }
}
