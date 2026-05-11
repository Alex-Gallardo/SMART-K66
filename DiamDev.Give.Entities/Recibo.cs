using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Recibo")]
    public class Recibo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Recibo_Id")]
        public long ReciboId { get; set; }
        
        [Column("Tipo_Id")]
        public int? TipoId { get; set; }

        [ForeignKey("TipoId")]
        public ReciboTipo Tipo { get; set; }

        [Column("Agencia_Id")]
        public long AgenciaId { get; set; }

        [ForeignKey("AgenciaId")]
        public Agencia Agencia { get; set; }
              
        [Column("Vendedor_Id")]
        public long VendedorId { get; set; }

        [ForeignKey("VendedorId")]
        public Vendedor Vendedor { get; set; }

        [Column("Cliente_Id")]
        public long ClienteId { get; set; }

        [ForeignKey("ClienteId")]
        public Cliente Cliente { get; set; }

        [Column("Pedido_Id")]
        public long? PedidoId { get; set; }

        [ForeignKey("PedidoId")]
        public Pedido Pedido { get; set; }

        [NotMapped]
        public long? ReservaId { get; set; }

        public string Comentario { get; set; }

        [Column("Comentario_Pedido")]
        public string ComentarioPedido { get; set; }

        public int Descuento { get; set; }

        [NotMapped]
        public decimal DescuentoTotal { get; set; }

        [NotMapped]
        public decimal Total { get; set; }

        [NotMapped]
        public decimal Abono { get; set; }
                        
        public bool Anulada { get; set; }
        
        public bool Empleado { get; set; }

        [NotMapped]
        public int RepartoId { get; set; }
        
        public bool Reparto { get; set; }

        public bool Pagada { get; set; }

        public bool Despachado { get; set; }
        
        public bool Factura { get; set; }

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

        [Column("Fecha_Pago_Estimada")]
        public DateTime? FechaPagoEstimada { get; set; }

        public DateTime Fecha { get; set; }

        [Column("Fecha_Hora_Recibo")]
        public DateTime? FechaHoraRecibo { get; set; }

        [Column("Fecha_Hora_CocinaFin")]
        public DateTime? FechaHoraCocina{ get; set; }

        [Column("Fecha_Hora_Entrega_Programada")]
    
        public DateTime? FechaHoraEntregaProgramada { get; set; }

        public bool? Programada { get; set; }

        [Column("Usr_Cocina")]
        public long? UsrCocina { get; set; }

        [ForeignKey("UsrCocina")]
        public Usuario UsuarioCocina { get; set; }


        [Column("DireccionCliente")]
        public int? DireccionClienteId { get; set; }


        public bool Credito { get; set; }
        
        [Column("Dia_Credito")]
        public int DiaCredito { get; set; }

        [Column("Producto_Lote")]
        public bool ProductoLote { get; set; }

        public int Correlativo { get; set; }
                          
        [NotMapped]
        public string Documento { get; set; }
        
        [NotMapped]
        public string FormaPago { get; set; }

        [NotMapped]
        public bool HabilitarCheck { get; set; }

        [NotMapped]
        public long MesaId { get; set; }

        [NotMapped]
        public decimal Efectivo { get; set; }

        [NotMapped]
        public decimal Tarjeta { get; set; }

        [NotMapped]
        public string NoFactura { get; set; }

        [NotMapped]
        public bool FEL { get; set; }

        public List<ReciboDetalle> Detalles { get; set; }

        public List<ReciboLote> Lotes { get; set; }
        
        public List<ReciboFormaPago> Pagos { get; set; }
    }
}
