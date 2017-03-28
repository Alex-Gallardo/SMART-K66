using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiamDev.Give.Entities
{
    [Table("Kardex")]
    public class RegistroKardex
    {
        public int Id { get; set; }

        public DateTime FechaHora { get; set; }

        [Required]
        [StringLength(50)]
        public string ProductoId { get; set; }

        [StringLength(250)]
        public string ProductoCodigo { get; set; }

        public long MarcaId { get; set; }

        [StringLength(300)]
        public string MarcaNombre { get; set; }

        [StringLength(500)]
        public string Descripcion { get; set; }

        public DateTime Fecha { get; set; }

        [StringLength(200)]
        public string DocumentoNumero { get; set; }

        [StringLength(500)]
        public string Concepto { get; set; }

        public long AgenciaId { get; set; }

        [StringLength(300)]
        public string AgenciaNombre { get; set; }

        [StringLength(50)]
        public string TipoRegistro { get; set; }

        public decimal IngresoBodega { get; set; }

        public decimal IngresoTienda { get; set; }

        public decimal IngresoTotal { get; set; }

        public decimal IngresoCostoBodega { get; set; }

        public decimal IngresoCostoTienda { get; set; }

        public decimal IngresoCostoTotal { get; set; }

        public decimal SalidaCostoBodega { get; set; }

        public decimal SalidaCostoTienda { get; set; }

        public decimal SalidaCostoTotal { get; set; }

        public decimal SalodaCantidadBodega { get; set; }

        public decimal SalidaCantidadTienda { get; set; }

        public decimal SalidaCantidadTotal { get; set; }

        public decimal ExistenciaFinalBodega { get; set; }

        public decimal ExistenciaFinalTienda { get; set; }

        public decimal ExistenciaFinalTotal { get; set; }

    }
}
