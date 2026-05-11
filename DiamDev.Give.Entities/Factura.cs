using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Factura")]
    public class Factura
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Factura_Id")]
        public long FacturaId { get; set; }

        [Column("Empresa_Id")]
        public long? EmpresaId { get; set; }

        [ForeignKey("EmpresaId")]
        public Empresa Empresa { get; set; }

        [Column("Tipo_Id")]
        public int? TipoId { get; set; }

        [ForeignKey("TipoId")]
        public FacturaTipo Tipo { get; set; }

        [Column("Agencia_Id")]
        public long AgenciaId { get; set; }

        [ForeignKey("AgenciaId")]
        public Agencia Agencia { get; set; }

        [Column("Serie_Id")]
        public long SerieId { get; set; }

        [ForeignKey("SerieId")]
        public Serie Serie { get; set; }

        [Column("Vendedor_Id")]
        public long? VendedorId { get; set; }

        [ForeignKey("VendedorId")]
        public Vendedor Vendedor { get; set; }

        [Column("Cliente_Id")]
        public long ClienteId { get; set; }

        [ForeignKey("ClienteId")]
        public Cliente Cliente { get; set; }

        [Column("Nota_Credito_Id")]
        public long? NotaCreditoId { get; set; }

        [NotMapped]
        public long? PedidoId { get; set; }

        [NotMapped]
        public long? ReservaId { get; set; }
        
        public string Comentario { get; set; }

        public int Descuento { get; set; }

        [NotMapped]
        public decimal DescuentoTotal { get; set; }

        [NotMapped]
        public decimal Total { get; set; }

        [Column("No_Factura")]
        public long NoFactura { get; set; }

        public bool Anulada { get; set; }
        
        public bool Empleado { get; set; }

        [NotMapped]
        public int RepartoId { get; set; }
        
        public bool Reparto { get; set; }

        public bool Pagada { get; set; }

        public bool Despachado { get; set; }

        [Column("Transporte_Id")]
        public long? TransporteId { get; set; }

        [ForeignKey("TransporteId")]
        public Transporte Transporte { get; set; }

        [Column("Entregado_Transporte")]
        public bool EntregadoTransporte { get; set; }
                                
        [Column("Usr_Creo")]
        public long UsrCreo { get; set; }

        [ForeignKey("UsrCreo")]
        public Usuario UsuarioCreo { get; set; }      

        [Column("Usr_Anular")]
        public long? UsrAnular { get; set; }

        [ForeignKey("UsrAnular")]
        public Usuario UsuarioAnular { get; set; }

        [Column("Fecha_Anular")]
        public DateTime? FechaAnular { get; set; }
       
        [Column("Usr_Despacho")]
        public long? UsrDespacho { get; set; }

        [ForeignKey("UsrDespacho")]
        public Usuario UsuarioDespacho { get; set; }

        [Column("Fecha_Hora_Despacho")]
        public DateTime? FechaHoraDespacho { get; set; }

        public DateTime Fecha { get; set; }

        [Column("Fecha_Hora_Factura")]
        public DateTime? FechaHoraFactura { get; set; }

        public bool Credito { get; set; }
        
        [Column("Dia_Credito")]
        public int DiaCredito { get; set; }

        [Column("Producto_Lote")]
        public bool ProductoLote { get; set; }

        public int Correlativo { get; set; }

        //FEL
        public bool Infile { get; set; }

        [Column("Factura_Electronica")]
        public bool FacturaElectronica { get; set; }

        [Column("Cantidad_Errores_FEL")]
        public int CantidadErroresFEL { get; set; }

        [Column("Descripcion_FEL")]
        public string DescripcionFEL { get; set; }

        [Column("Fecha_Hora_Certificacion_FEL")]
        public string FechaHoraCertificacionFEL { get; set; }

        [Column("Numero_FEL")]
        public string NumeroFEL { get; set; }

        [Column("Serie_FEL")]
        public string SerieFEL { get; set; }

        [Column("UUID_FEL")]
        public string UUIDFEL { get; set; }

        [Column("XML_Certificado_FEL")]
        public string XMLCertificadoFEL { get; set; }

        [Column("Json_FEL")]
        public string JsonFEL { get; set; }

        //Anulacion
        [Column("Descripcion_Anular_FEL")]
        public string DescripcionAnularFEL { get; set; }

        [Column("Fecha_Hora_Certificacion_Anular_FEL")]
        public string FechaHoraCertificacionAnularFEL { get; set; }

        [Column("XML_Certificado_Anular_FEL")]
        public string XMLCertificadoAnularFEL { get; set; }

        [Column("Json_Anular_FEL")]
        public string JsonAnularFEL { get; set; }

        public long? ReciboId { get; set; }

        [NotMapped]
        public long TicketId { get; set; }

        [NotMapped]
        public string Ticket { get; set; }

        [NotMapped]
        public bool ServicioCliente { get; set; }
                
        [NotMapped]
        public string Documento { get; set; }
        
        [NotMapped]
        public string FormaPago { get; set; }

        [NotMapped]
        public decimal Abono { get; set; }

        [NotMapped]
        public bool HabilitarCheck { get; set; }

        [NotMapped]
        public bool NotaCredito { get; set; }

        public List<FacturaDetalle> Detalles { get; set; }

        public List<FacturaLote> Lotes { get; set; }
        
        public List<FacturaFormaPago> Pagos { get; set; }
    }
}
