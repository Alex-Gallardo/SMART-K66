using System;
using System.Collections.Generic;

namespace DiamDev.Give.Entities
{
    public class BorradorNcDashboardFiltro
    {
        public string Empresa { get; set; }
        public string Estado { get; set; }
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public string CampoFecha { get; set; }
        public string Cliente { get; set; }
        public string Agente { get; set; }
        public string Creador { get; set; }
        public string ResueltoPor { get; set; }
        public string Moneda { get; set; }
        public bool? ConAdjuntos { get; set; }
        public bool? ConAntecedentesSap { get; set; }
        public string Texto { get; set; }
        public string Orden { get; set; }
        public string Direccion { get; set; }
        public int Pagina { get; set; }
        public int TamanoPagina { get; set; }
    }

    public class BorradorNcDashboardAlcance
    {
        public BorradorNcDashboardAlcance()
        {
            AgentesPorEmpresa = new Dictionary<string, List<string>>(
                StringComparer.OrdinalIgnoreCase);
        }

        public bool Global { get; set; }
        public string Usuario { get; set; }
        public Dictionary<string, List<string>> AgentesPorEmpresa { get; set; }
    }

    public class BorradorNcDashboardFila
    {
        public string IdBorrador { get; set; }
        public string IdEmpresa { get; set; }
        public DateTime Fecha { get; set; }
        public DateTime? Registro { get; set; }
        public string IdCliente { get; set; }
        public string Nombre { get; set; }
        public string Nit { get; set; }
        public string Agente { get; set; }
        public string Moneda { get; set; }
        public decimal Total { get; set; }
        public string Estado { get; set; }
        public string IdUsr { get; set; }
        public string ResueltoPor { get; set; }
        public DateTime? FechaResolucion { get; set; }
        public int Facturas { get; set; }
        public int Adjuntos { get; set; }
        public bool TieneAntecedentesSap { get; set; }
    }

    public class BorradorNcDashboardResumen
    {
        public BorradorNcDashboardResumen()
        {
            TotalesPorMoneda = new List<BorradorNcDashboardTotalMoneda>();
        }

        public int Total { get; set; }
        public int Pendientes { get; set; }
        public int PendientesVencidos { get; set; }
        public int Autorizados { get; set; }
        public int Rechazados { get; set; }
        public int Anulados { get; set; }
        public int ConAntecedentesSap { get; set; }
        public int ConAdjuntos { get; set; }
        public decimal? HorasPromedioResolucion { get; set; }
        public DateTime? PendienteMasAntiguo { get; set; }
        public List<BorradorNcDashboardTotalMoneda> TotalesPorMoneda { get; set; }
    }

    public class BorradorNcDashboardTotalMoneda
    {
        public string Moneda { get; set; }
        public decimal Total { get; set; }
    }

    public class BorradorNcDashboardPagina
    {
        public BorradorNcDashboardPagina()
        {
            Filas = new List<BorradorNcDashboardFila>();
            Resumen = new BorradorNcDashboardResumen();
            Empresas = new List<string>();
            Agentes = new List<string>();
            Creadores = new List<string>();
            Resolutores = new List<string>();
            Monedas = new List<string>();
        }

        public int Pagina { get; set; }
        public int TamanoPagina { get; set; }
        public int TotalFilas { get; set; }
        public List<BorradorNcDashboardFila> Filas { get; set; }
        public BorradorNcDashboardResumen Resumen { get; set; }
        public List<string> Empresas { get; set; }
        public List<string> Agentes { get; set; }
        public List<string> Creadores { get; set; }
        public List<string> Resolutores { get; set; }
        public List<string> Monedas { get; set; }
    }

    public class BorradorNcDashboardFactura
    {
        public string IdEmpresa { get; set; }
        public string IdBorrador { get; set; }
        public string Documento { get; set; }
        public DateTime FechaDocumento { get; set; }
        public string Concepto { get; set; }
        public string Descripcion { get; set; }
        public string Moneda { get; set; }
        public decimal TotalFactura { get; set; }
        public decimal ImporteSolicitado { get; set; }
    }

    public class BorradorNcBitacora
    {
        public long EventoId { get; set; }
        public string Evento { get; set; }
        public string EstadoAnterior { get; set; }
        public string EstadoNuevo { get; set; }
        public string Usuario { get; set; }
        public string Ip { get; set; }
        public string Detalle { get; set; }
        public DateTime Registro { get; set; }
    }
}
