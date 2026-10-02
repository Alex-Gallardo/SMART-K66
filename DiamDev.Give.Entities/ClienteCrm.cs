using System;
using System.Collections.Generic;

namespace DiamDev.Give.Entities
{
    public static class EstadosSolicitudCliente
    {
        public const string Borrador = "BORRADOR";
        public const string Enviada = "ENVIADA";
        public const string Aprobada = "APROBADA";
        public const string Rechazada = "RECHAZADA";
    }

    public class ClienteCrmContacto
    {
        public string Area { get; set; }
        public string Nombre { get; set; }
        public string Puesto { get; set; }
        public string Telefono { get; set; }
        public string Correo { get; set; }
        public string TomadorDecision { get; set; }
        public string InfluenciadorTecnico { get; set; }
    }

    public class ClienteCrmDireccion
    {
        public string Nombre { get; set; }
        public string Direccion { get; set; }
        public string Referencia { get; set; }
        public string Modalidad { get; set; }
        public string HorarioSemana { get; set; }
        public string HorarioFinSemana { get; set; }
        public bool RequiereCita { get; set; }
        public bool Activa { get; set; }
        public string RequiereCitaRespuesta { get; set; }
        public string ActivaRespuesta { get; set; }
    }

    /// <summary>La ficha se conserva completa como fotografía versionada en SQL.</summary>
    public class ClienteCrmFicha
    {
        public ClienteCrmFicha()
        {
            Contactos = new List<ClienteCrmContacto>();
            Direcciones = new List<ClienteCrmDireccion>();
        }

        // Creación y facturación.
        public string RazonSocial { get; set; }
        public string NombreComercial { get; set; }
        public string NitDpi { get; set; }
        public string TipoNegocio { get; set; }
        public string DireccionFiscal { get; set; }
        public string TipoOperacion { get; set; }
        public string CorreoFactura { get; set; }
        public string CondicionPago { get; set; }
        public string MetodoPago { get; set; }
        public string TemporadaPago { get; set; }
        public string TramiteContrasena { get; set; }
        public bool CambioRazonSocial { get; set; }
        public string CambioRazonSocialRespuesta { get; set; }
        public string CodigoSapOrigen { get; set; }
        public int PasoCompletado { get; set; }
        public List<ClienteCrmContacto> Contactos { get; set; }
        public List<ClienteCrmDireccion> Direcciones { get; set; }

        // Contactos y unidad de decisión.
        public string ContactoPrincipal { get; set; }
        public string CargoPrincipal { get; set; }
        public string TelefonoPrincipal { get; set; }
        public string CorreoPrincipal { get; set; }
        public string TomadorDecision { get; set; }
        public string InfluenciadorTecnico { get; set; }
        public string ResponsableCompras { get; set; }
        public string ContactoPagos { get; set; }
        public string CanalPreferido { get; set; }
        public string ObservacionesRelacion { get; set; }

        // Perfil del negocio y cobertura.
        public string CoberturaGeografica { get; set; }
        public string RegionesMayorVenta { get; set; }
        public string SucursalesMayorVenta { get; set; }
        public int? VendedoresCampo { get; set; }
        public int? VendedoresTienda { get; set; }
        public bool Televentas { get; set; }
        public bool VentaMostrador { get; set; }
        public bool VentaInstitucional { get; set; }
        public bool Ecommerce { get; set; }
        public string TeleventasRespuesta { get; set; }
        public string VentaMostradorRespuesta { get; set; }
        public string VentaInstitucionalRespuesta { get; set; }
        public string EcommerceRespuesta { get; set; }
        public string ClientesFinales { get; set; }
        public string TemporadasDemanda { get; set; }
        public string ProyectosEventos { get; set; }

        // Inteligencia comercial.
        public string NecesidadPrincipal { get; set; }
        public string ValorProveedor { get; set; }
        public string MotivoCompra { get; set; }
        public string ObjecionPrincipal { get; set; }
        public string CompetidorPrincipal { get; set; }
        public string FortalezaCompetidor { get; set; }
        public string DebilidadCompetidor { get; set; }
        public string InformacionMercado { get; set; }
        public string RiesgoComercial { get; set; }
        public string Satisfaccion { get; set; }

        // Desarrollo comercial de la empresa elegida.
        public bool CompraActualmente { get; set; }
        public string CompraActualmenteRespuesta { get; set; }
        public string MonedaIndicadores { get; set; }
        public decimal? Venta12Meses { get; set; }
        public decimal? PotencialAnual { get; set; }
        public decimal? Objetivo12Meses { get; set; }
        public string OportunidadCrecimiento { get; set; }
        public string CompetidorNegocio { get; set; }
        public string Forecast { get; set; }
        public string ProximoPaso { get; set; }
        public string ObservacionesComerciales { get; set; }
    }

    public class ClienteCrmSolicitud
    {
        public long Id { get; set; }
        public long? ClienteId { get; set; }
        public string Empresa { get; set; }
        public string CodigoOperador { get; set; }
        public string Agente { get; set; }
        public string Estado { get; set; }
        public string CreadoPor { get; set; }
        public DateTime CreadoEn { get; set; }
        public DateTime? EnviadoEn { get; set; }
        public DateTime? ResueltoEn { get; set; }
        public string ResueltoPor { get; set; }
        public string MotivoRechazo { get; set; }
        public int Version { get; set; }
        public ClienteCrmFicha Ficha { get; set; }
        public string FichaJson { get; set; }
        public List<ClienteCrmArchivo> Archivos { get; set; }
    }

    public class ClienteCrmCliente
    {
        public long Id { get; set; }
        public string Empresa { get; set; }
        public string CodigoSap { get; set; }
        public bool Activo { get; set; }
        public DateTime ActualizadoEn { get; set; }
        public int Version { get; set; }
        public ClienteCrmFicha Ficha { get; set; }
        public string FichaJson { get; set; }
    }

    public class ClienteCrmArchivo
    {
        public long Id { get; set; }
        public string Tipo { get; set; }
        public string Nombre { get; set; }
        public string ContentType { get; set; }
        public int Tamano { get; set; }
        public byte[] Contenido { get; set; }
    }

    public class ClienteCrmEvento
    {
        public long Id { get; set; }
        public DateTime Fecha { get; set; }
        public string Usuario { get; set; }
        public string Accion { get; set; }
        public string Detalle { get; set; }
    }
}
