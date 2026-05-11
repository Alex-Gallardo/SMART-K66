using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiamDev.Give.Entities
{
    [Table("Agencia")]
    public class Agencia
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Agencia_Id")]
        public long AgenciaId { get; set; }

        [Column("Codigo_Establecimiento")]
        public long? CodigoEstablecimiento { get; set; }

        [Required(ErrorMessage = "El nombre de la agencia es requerido")]
        [StringLength(300)]
        public string Nombre { get; set; }

        public string Direccion { get; set; }

        [Column("EsDelivery_Domicilio")]
        public bool EsDeliveryDomicilio { get; set; }

        public bool Activo { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }
    }
}
