using System;
using System.Collections.Generic;

namespace DiamDev.Give.Entities
{
    // ═══════════════════════════════════════════════════════════
    //  ANALYTICS DE RECIBOS DE CAJA
    //  Fuente única: dbo.analyticsRecibos (bitácora append-only).
    //  OJO: esta pantalla mide COMPORTAMIENTO, no contabilidad.
    //  "MontoCreado" es dinero que se registró en el periodo, NO
    //  dinero vigente hoy. Para la verdad contable está el Dashboard.
    // ═══════════════════════════════════════════════════════════

    /// <summary>Filtro de entrada. FechaIni/FechaFin en null = "todo".</summary>
    public class AnalyticsFiltro
    {
        public string Empresa { get; set; }      // "" = todas
        public DateTime? FechaIni { get; set; }  // inclusive
        public DateTime? FechaFin { get; set; }  // EXCLUSIVO (< FechaFin)

        public AnalyticsFiltro() { Empresa = ""; }
    }

    /// <summary>Tarjetas superiores de la vista.</summary>
    public class AnalyticsResumen
    {
        public int Creados { get; set; }
        public int Anulados { get; set; }
        public decimal MontoCreado { get; set; }
        public decimal MontoAnulado { get; set; }
        public decimal TicketPromedio { get; set; }
        public int UsuariosActivos { get; set; }
        public int DeptosActivos { get; set; }
        public int IpsDistintas { get; set; }
        public int EnUsd { get; set; }
        public int Impresos { get; set; }
        public int Editados { get; set; }
        public int Errores { get; set; }
        public int Rechazos { get; set; }
        public int MonedaRara { get; set; }      // Moneda fuera de GTQ/USD  ← el '##'
        public decimal MontoMonedaRara { get; set; }
        public DateTime? Primero { get; set; }
        public DateTime? Ultimo { get; set; }

        /// <summary>Anulados / Creados del MISMO periodo. Es una tasa de
        /// actividad, no de cohorte: un recibo puede crearse en junio y
        /// anularse en julio. Se documenta en el tooltip de la tarjeta.</summary>
        public decimal TasaAnulacion { get; set; }
    }

    public class AnalyticsSerieDia
    {
        public DateTime Dia { get; set; }
        public string IdEmpresa { get; set; }
        public int Recibos { get; set; }
        public decimal Monto { get; set; }
    }

    public class AnalyticsUsuario
    {
        public string UsuarioLogin { get; set; }
        public string Depto { get; set; }
        public int Deptos { get; set; }
        public int Creados { get; set; }
        public decimal MontoCreado { get; set; }
        public int Anulados { get; set; }
        public int Empresas { get; set; }
        public decimal Ticket { get; set; }
        public DateTime? Ultimo { get; set; }
    }

    public class AnalyticsEmpresa
    {
        public string IdEmpresa { get; set; }
        public int Recibos { get; set; }
        public decimal Monto { get; set; }
    }

    /// <summary>Celda del mapa de calor. DiaSemana 0 = lunes .. 6 = domingo.</summary>
    public class AnalyticsHeatCelda
    {
        public int DiaSemana { get; set; }
        public int Hora { get; set; }
        public int Recibos { get; set; }
    }

    public class AnalyticsAnulacion
    {
        public string IdRecibo { get; set; }
        public string IdEmpresa { get; set; }
        public string UsuarioLogin { get; set; }
        public string Depto { get; set; }
        public decimal MontoGtq { get; set; }
        public DateTime FechaEvento { get; set; }
        public string Motivo { get; set; }
        public string EstadoAlAnular { get; set; }
    }

    /// <summary>Una IP con su actividad agregada y su geolocalización (si ya se resolvió).</summary>
    public class AnalyticsAcceso
    {
        public string Ip { get; set; }
        public int Eventos { get; set; }
        public int Usuarios { get; set; }
        public string ListaUsuarios { get; set; }
        public DateTime? Primero { get; set; }
        public DateTime? Ultimo { get; set; }

        // Geo (null mientras no se resuelva)
        public string Pais { get; set; }
        public string CodigoPais { get; set; }
        public string Region { get; set; }
        public string Ciudad { get; set; }
        public decimal? Latitud { get; set; }
        public decimal? Longitud { get; set; }
        public string Isp { get; set; }
        public bool EsMovil { get; set; }
        public bool EsProxy { get; set; }
        public string EstadoGeo { get; set; }    // OK | PRIVADA | FALLO | null
    }

    /// <summary>Fila de la caché analyticsGeoIp.</summary>
    public class GeoIp
    {
        public string Ip { get; set; }
        public string Pais { get; set; }
        public string CodigoPais { get; set; }
        public string Region { get; set; }
        public string Ciudad { get; set; }
        public decimal? Latitud { get; set; }
        public decimal? Longitud { get; set; }
        public string Isp { get; set; }
        public string Organizacion { get; set; }
        public bool EsMovil { get; set; }
        public bool EsProxy { get; set; }
        public bool EsHosting { get; set; }
        public string Estado { get; set; }
        public string Mensaje { get; set; }
        public string Origen { get; set; }
    }

    /// <summary>Contenedor: todo lo que la vista necesita en UN viaje a la BD.</summary>
    public class AnalyticsPaquete
    {
        public AnalyticsResumen Resumen { get; set; }
        public List<AnalyticsSerieDia> Serie { get; set; }
        public List<AnalyticsUsuario> Usuarios { get; set; }
        public List<AnalyticsEmpresa> Empresas { get; set; }
        public List<AnalyticsHeatCelda> Heat { get; set; }
        public List<AnalyticsAnulacion> Anulaciones { get; set; }
        public List<AnalyticsAcceso> Accesos { get; set; }

        public AnalyticsPaquete()
        {
            Resumen = new AnalyticsResumen();
            Serie = new List<AnalyticsSerieDia>();
            Usuarios = new List<AnalyticsUsuario>();
            Empresas = new List<AnalyticsEmpresa>();
            Heat = new List<AnalyticsHeatCelda>();
            Anulaciones = new List<AnalyticsAnulacion>();
            Accesos = new List<AnalyticsAcceso>();
        }
    }

    /// <summary>Resultado de una corrida del resolvedor de geolocalización.</summary>
    public class GeoResultado
    {
        public bool Exito { get; set; }
        public int Pendientes { get; set; }
        public int Resueltas { get; set; }
        public int Privadas { get; set; }
        public int Fallidas { get; set; }
        public string Mensaje { get; set; }
    }
}