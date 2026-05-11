using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Personal")]
    public class Personal
    {
        [Key, Column(name: "Personal_Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long PersonalId { get; set; }

        [Column("Puesto_Id")]
        public long PuestoId { get; set; }

        [ForeignKey("PuestoId")]
        public Puesto Puesto { get; set; }
      
        [StringLength(300)]
        [Required(ErrorMessage = "El nombre del Personal es requerido")]
        public string Nombre { get; set; }

        [StringLength(500)]
        [Required(ErrorMessage = "La dirección del Personal es requerida")]
        public string Direccion { get; set; }
        
        [StringLength(20)]
        [Column("No_Telefono")]
        public string NoTelefono { get; set; }

        [StringLength(20)]
        [Column("No_Telefono_Alterno")]
        public string NoTelefonoAlterno { get; set; }

        [StringLength(20)]
        [Column("No_Celular_Principal")]
        public string NoCelularPrincipal { get; set; }

        [StringLength(20)]
        [Column("No_Celular_Alterno")]
        public string NoCelularAlterno { get; set; }       

        [StringLength(100)]
        public string Email { get; set; }

        [Column("Fecha_Nacimiento")]
        public DateTime? FechaNacimiento { get; set; }

        [StringLength(20)]
        public string DPI { get; set; }

        [StringLength(20)]
        public string Nit { get; set; }

        [StringLength(50)]
        [Column("Licencia_Vehiculo")]
        public string LicenciaVehiculo { get; set; }

        [StringLength(50)]
        [Column("Licencia_Moto")]
        public string LicenciaMoto { get; set; }

        [StringLength(50)]
        [Column("No_Afiliacion_IGSS")]
        public string NoAfiliacionIGSS { get; set; }

        [Column("Fecha_Ingreso")]
        public DateTime? FechaIngreso { get; set; }

        [Column("Fecha_Egreso")]
        public DateTime? FechaEgreso { get; set; }

        [Column("Banco_Id")]
        public long? BancoId { get; set; }

        [ForeignKey("BancoId")]
        public Banco Banco { get; set; }

        [StringLength(100)]
        public string Planilla { get; set; }

        [StringLength(50)]
        public string Contrato { get; set; }

        public decimal Sueldo { get; set; }

        public decimal Bonificacion { get; set; }

        public bool IGSS { get; set; }

        [StringLength(500)]
        [Column("Motivo_Egreso")]
        public string MotivoEgreso { get; set; }

        public bool Activo { get; set; }

        public byte[] Huella { get; set; }

        [Column("Template_Bytes")]
        public byte[] TemplateBytes { get; set; }

        [Column("Template_Size")]
        public int TemplateSize { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }

        public List<Anotacion> Anotaciones { get; set; }

        public List<PersonalHorario> Horarios { get; set; }
    }
}
