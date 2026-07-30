using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiamDev.Give.Entities
{
    [Table("Pedido_K66")]
    public class PedidoK66
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Pedido_Id")]
        public long PedidoId { get; set; }

        [Column("Empresa_Id")]
        public long EmpresaId { get; set; }

        [ForeignKey("EmpresaId")]
        public Empresa Empresa { get; set; }

        [Column("Tipo_Pedido_Id")]
        public string TipoPedidoId { get; set; }

        [Column("Estado_Id")]
        public int? EstadoId { get; set; }

        [ForeignKey("EstadoId")]
        public EstadoSmartK66 Estado { get; set; }

        [Column("CUSTOMER_ORDER_ROWID")]
        public int? CUSTOMERORDERROWID { get; set; }

        [Column("CUSTOMER_ORDER_ID")]
        public string CUSTOMERORDERID { get; set; }

        [Column("ID_K66")]
        public string IDK66 { get; set; }

        public string Nit { get; set; }

        public string Nombre { get; set; }

        [Column("Direccion_Id")]
        public int? DireccionId { get; set; }

        public string Direccion { get; set; }

        [Column("Direccion_Entrega")]
        public string DireccionEntrega { get; set; }

        [Column("Orden_Compra_Cliente")]
        public string OrdenCompraCliente { get; set; }

        [Column("Observaciones_Generales")]
        public string ObservacionesGenerales { get; set; }

        [Column("Comentario_Aprobacion")]
        public string ComentarioAprobacion { get; set; }

        [Column("Documento_Orden_Compra_Respaldo")]
        public string DocumentoOrdenCompraRespaldo { get; set; }

        [Column("Termino_Entrega")]
        public string TerminoEntrega { get; set; }

        public string Vendedor { get; set; }

        [Column("Impuesto_TAX")]
        public string ImpuestoTAX { get; set; }

        public string Moneda { get; set; }

        [Column("Responsable_Id")]
        public long ResponsableId { get; set; }

        [ForeignKey("ResponsableId")]
        public Usuario Responsable { get; set; }

        [Column("Responsable_Aprobacion_Id")]
        public long? ResponsableAprobacionId { get; set; }

        [ForeignKey("ResponsableAprobacionId")]
        public Usuario ResponsableAprobacion { get; set; }

        public bool Sincronizado { get; set; }

        [Column("Fecha_Hora_Pedido")]
        public DateTime? FechaHoraPedido { get; set; }

        [Column("Fecha_Hora_Ultimo_Intento")]
        public DateTime? FechaHoraUltimoIntento { get; set; }

        [Column("Fecha_Hora_Sincronizacion")]
        public DateTime? FechaHoraSincronizacion { get; set; }

        [Column("Fecha_Prometida")]
        public DateTime? FechaPrometida { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }

        public List<PedidoDetalleK66> Detalles { get; set; }

        public List<PedidoDocumentoImportanteK66> DImportantes { get; set; }

        [NotMapped]
        public ProductoFotografia Documento { get; set; }

        [NotMapped]
        public List<ProductoFotografia> Documentos { get; set; }

        [NotMapped]
        public string EstadoK66 { get; set; }

    }
}