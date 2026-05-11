using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Reparacion")]
    public class Reparacion
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Reparacion_Id")]
        public long ReparacionId { get; set; }

        [Column("Agencia_Id")]
        public long AgenciaId { get; set; }

        [ForeignKey("AgenciaId")]
        public Agencia Agencia { get; set; }

        [Column("Cliente_Id")]
        public long ClienteId { get; set; }

        [ForeignKey("ClienteId")]
        public Cliente Cliente { get; set; }

        [Column("Departamento_Id")]
        public long DepartamentoId { get; set; }

        [ForeignKey("DepartamentoId")]
        public Departamento Departamento { get; set; }

        [Column("Estado_Id")]
        public int EstadoId { get; set; }

        [ForeignKey("EstadoId")]
        public ReparacionEstado Estado { get; set; }

        [Column("Tipo_Id")]
        public int? TipoId { get; set; }

        [ForeignKey("TipoId")]
        public ReparacionTipo Tipo { get; set; }

        [StringLength(50)]
        public string Serie { get; set; }

        [StringLength(50)]
        public string Factura { get; set; }

        public string Marca { get; set; }

        public string Falla { get; set; }

        public string IMEI { get; set; }

        public string Descripcion { get; set; }

        public string Garantia { get; set; }

        public string Comentario { get; set; }

        [NotMapped]
        public decimal CostoProducto { get; set; }

        [NotMapped]
        public decimal Costo { get; set; }

        [Column("Costo_Servicio")]
        public decimal CostoServicio { get; set; }

        public int Descuento { get; set; }

        [NotMapped]
        public int DiasGames { get; set; }

        [Column("Usr_Creo")]
        public long UsrCreo { get; set; }

        [ForeignKey("UsrCreo")]
        public Usuario UsuarioCreo { get; set; }

        [Column("Usr_Asignado")]
        public long? UsrAsignado { get; set; }

        [ForeignKey("UsrAsignado")]
        public Usuario UsuarioAsignado { get; set; }

        [Column("Usr_Entrega")]
        public long? UsrEntrega { get; set; }

        [ForeignKey("UsrEntrega")]
        public Usuario UsuarioEntrega { get; set; }

        [Column("Usr_Anular")]
        public long? UsrAnular { get; set; }

        [ForeignKey("UsrAnular")]
        public Usuario UsuarioAnular { get; set; }

        [Column("Fecha_Inicia_Reparacion")]
        public DateTime? FechaIniciaReparacion { get; set; }

        [Column("Fecha_Finaliza_Reparacion")]
        public DateTime? FechaFinalizaReparacion { get; set; }

        [Column("Fecha_Cancelacion")]
        public DateTime? FechaCancelacion { get; set; }

        [Column("Fecha_Entrega")]
        public DateTime FechaEntrega { get; set; }      

        [Column("Fecha_Anular")]
        public DateTime? FechaAnular { get; set; }

        public bool Operado { get; set; }

        public bool Anulada { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }

        public List<ReparacionServicio> Servicios { get; set; }

        public List<ReparacionPieza> Piezas { get; set; }

        public List<ReparacionAnotacion> Comentarios { get; set; }

        public List<ReparacionFotografia> Imagenes { get; set; }

        public List<ReparacionFormaPago> Pagos { get; set; }

        public List<ReparacionPoliticaCategoria> Politicas { get; set; }
    }
}
