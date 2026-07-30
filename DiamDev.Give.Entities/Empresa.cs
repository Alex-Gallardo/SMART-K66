using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiamDev.Give.Entities
{
    [Table("Empresa")]
    public class Empresa
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Empresa_Id")]
        public long EmpresaId { get; set; }

        public string Nombre { get; set; }

        [Column("Nombre_Comercial")]
        public string NombreComercial { get; set; }

        [Column("Nombre_Contacto")]
        public string NombreContacto { get; set; }

        [Column("Telefono_Contacto")]
        public string TelefonoContacto { get; set; }

        [Column("Telefono_Contacto_2")]
        public string TelefonoContacto2 { get; set; }

        [Column("Correo_Contacto")]
        public string CorreoContacto { get; set; }

        [Column("Any_Desk_Id")]
        public int? AnyDeskId { get; set; }

        [Column("Nit_Emisor_DIGIFACT")]
        public string NitEmisorDIGIFACT { get; set; }

        [Column("Nombre_Comercial_DIGIFACT")]
        public string NombreComercialDIGIFACT { get; set; }

        [Column("Nombre_Emisor_DIGIFACT")]
        public string NombreEmisorDIGIFACT { get; set; }

        [Column("Direccion_Emisor_DIGIFACT")]
        public string DireccionEmisorDIGIFACT { get; set; }

        [Column("Codigo_Postal_Emisor_DIGIFACT")]
        public string CodigoPostalEmisorDIGIFACT { get; set; }

        [Column("Departamento_Emisor_DIGIFACT")]
        public string DepartamentoEmisorDIGIFACT { get; set; }

        [Column("Municipio_Emisor_DIGIFACT")]
        public string MunicipioEmisorDIGIFACT { get; set; }

        [Column("Pais_Emisor_DIGIFACT")]
        public string PaisEmisorDIGIFACT { get; set; }

        [Column("Codigo_Escenario_DIGIFACT")]
        public string CodigoEscenarioDIGIFACT { get; set; }

        [Column("Tipo_Frase_DIGIFACT")]
        public string TipoFraseDIGIFACT { get; set; }

        [Column("Afiliacion_Iva_DIGIFACT")]
        public string AfiliacionIvaDIGIFACT { get; set; }

        [Column("Usuario_DIGIFACT")]
        public string UsuarioDIGIFACT { get; set; }

        [Column("Password_DIGIFACT")]
        public string PasswordDIGIFACT { get; set; }

        [Column("Reporte_1")]
        public string Reporte1 { get; set; }

        [Column("Reporte_2")]
        public string Reporte2 { get; set; }

        [Column("Reporte_Cotizacion")]
        public string ReporteCotizacion { get; set; }

        [Column("Nombre_DB")]
        public string NombreDB { get; set; }

        [Column("Bodega_Activa")]
        public string BodegaActiva { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }

        public List<EmpresaBodegaActiva> Bodegas { get; set; }

        public List<EmpresaProductoEspecial> ProductosEspeciales { get; set; }

        [NotMapped]
        public List<Agencia> Agencias { get; set; }

        [NotMapped]
        public List<Usuario> Usuarios { get; set; }

        [NotMapped]
        public List<PaqueteEmpresa> Paquetes { get; set; }
    }
}
