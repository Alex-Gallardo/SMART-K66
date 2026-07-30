using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiamDev.Give.Entities
{
    [Table("Factura_Nota_Credito")]
    public class FacturaNotaCredito
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Factura_Id")]
        public long FacturaId { get; set; }

        public string Motivo { get; set; }

        //FEL
        public bool Infile { get; set; }     

        [Column("Cantidad_Errores_FEL")]
        public int CantidadErroresFEL { get; set; }

        [Column("Descripcion_FEL")]
        public string DescripcionFEL { get; set; }

        [Column("Fecha_Hora_Certificacion_FEL")]
        public string FechaHoraCertificacionFEL { get; set; }  
       
        [Column("XML_Certificado_FEL")]
        public string XMLCertificadoFEL { get; set; }

        [Column("Numero_FEL")]
        public string NumeroFEL { get; set; }

        [Column("Serie_FEL")]
        public string SerieFEL { get; set; }

        [Column("UUID_FEL")]
        public string UUIDFEL { get; set; }

        [Column("Json_FEL")]
        public string JsonFEL { get; set; }

        [Column("Usr_Creo")]
        public long UsrCreo { get; set; }

        [ForeignKey("UsrCreo")]
        public Usuario UsuarioCreo { get; set; }

        public DateTime Fecha { get; set; }

        [Column("Fecha_Hora_Nota_Credito")]
        public DateTime FechaHoraNotaCredito { get; set; }

        public List<FacturaNotaCreditoDetalle> Detalles { get; set; }     
    }
}
