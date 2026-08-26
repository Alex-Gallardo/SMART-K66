using System;
using System.Collections.Generic;

namespace DiamDev.Give.Entities
{
    /// <summary>Documento raíz del módulo Cotizaciones.</summary>
    public class CotizacionEncabezado
    {
        public CotizacionEncabezado()
        {
            Detalles = new List<CotizacionDetalle>();
            Fecha = DateTime.Today;
            ValidaHasta = DateTime.Today.AddDays(15);
            Estado = EstadosCotizacion.Vigente;
        }

        public string IdCotizacion { get; set; }
        public string IdEmpresa { get; set; }
        public DateTime Fecha { get; set; }
        public DateTime ValidaHasta { get; set; }

        // Foto del cliente SAP.
        public string IdCliente { get; set; }
        public string NombreCliente { get; set; }
        public string Nit { get; set; }
        public string Direccion { get; set; }
        public string Correo { get; set; }

        // Asignación que autorizó el contexto comercial.
        public string CodigoOperador { get; set; }
        public string Agente { get; set; }
        public string Moneda { get; set; }

        public string CondicionesPago { get; set; }
        public string TiempoEntrega { get; set; }
        public string Observaciones { get; set; }

        public decimal ImporteBruto { get; set; }
        public decimal DescuentoTotal { get; set; }
        public decimal Subtotal { get; set; }
        public decimal ImpuestoTotal { get; set; }
        public decimal Total { get; set; }

        public string Estado { get; set; }
        public string IdUsr { get; set; }
        public DateTime? Registro { get; set; }
        public string AnuladoPor { get; set; }
        public DateTime? FechaAnulacion { get; set; }
        public string MotivoAnulacion { get; set; }

        public List<CotizacionDetalle> Detalles { get; set; }
    }

    public static class EstadosCotizacion
    {
        public const string Vigente = "VIGENTE";
        public const string Vencida = "VENCIDA";
        public const string Anulada = "ANULADA";
    }
}
