using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiamDev.Give.Entities
{
    [Table("Cliente")]
    public class Cliente
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Cliente_Id")]
        public long ClienteId { get; set; }

        [Column("Empresa_Id")]
        public long? EmpresaId { get; set; }

        [ForeignKey("EmpresaId")]
        public Empresa Empresa { get; set; }

        [Column("Region_Id")]
        public long? RegionId { get; set; }

        [ForeignKey("RegionId")]
        public Region Region { get; set; }

        [Column("Vendedor_Id")]
        public long? VendedorId { get; set; }

        [ForeignKey("VendedorId")]
        public Vendedor Vendedor { get; set; }

        [Column("Tipo_Id")]
        public long? TipoId { get; set; }

        [ForeignKey("TipoId")]
        public ClienteTipo Tipo { get; set; }

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

        [Column("Latitud")]
        [StringLength(100)]
        public string Latitud { get; set; }

        [Column("Longitud")]
        [StringLength(100)]
        public string Longitud { get; set; }

        [Column("Pass")]
        [StringLength(100)]

        public string Pass { get; set; }


        public int Descuento { get; set; }

        public bool Vip { get; set; }

        public bool Activo { get; set; }

        [Column("Dias_Credito")]
        public int? DiasCredito { get; set; }

        [Column("Limite_Credito")]
        public decimal? LimiteCredito { get; set; }

        [NotMapped]
        public decimal Credito { get; set; }

        //DATOS DE CONTACTO
        [Column("Nombre_Contacto")]
        public string NombreContacto { get; set; }

        [Column("Telefono_Contacto")]
        public string TelefonoContacto { get; set; }

        [Column("Celular_Contacto")]
        public string CelularContacto { get; set; }

        [Column("Correo_Contacto")]
        public string CorreoContacto { get; set; }

        [Column("Nota_Contacto")]
        public string NotaContacto { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }

        public List<ClienteFotografia> Imagenes { get; set; }

        [NotMapped]
        public List<Factura> Facturas { get; set; }
        
        [NotMapped]
        public List<Recibo> Recibos { get; set; }

        public List<ClienteContacto> Contactos { get; set; }

        public List<DireccionCliente> Direcciones { get; set; }
    }
}
