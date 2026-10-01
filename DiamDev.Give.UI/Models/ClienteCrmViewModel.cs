using System.Collections.Generic;
using DiamDev.Give.Entities;

namespace DiamDev.Give.UI.Models
{
    public class ClienteCrmEmpresaOpcion
    {
        public string Empresa { get; set; }
        public string Etiqueta { get; set; }
        public List<ClienteCrmAgenteOpcion> Agentes { get; set; }
    }

    public class ClienteCrmAgenteOpcion
    {
        public string Codigo { get; set; }
        public string Nombre { get; set; }
    }

    public class ClienteCrmEditorViewModel
    {
        public ClienteCrmEditorViewModel()
        {
            Ficha = new ClienteCrmFicha();
            Empresas = new List<ClienteCrmEmpresaOpcion>();
            Archivos = new List<ClienteCrmArchivo>();
        }

        public long SolicitudId { get; set; }
        public long ClienteId { get; set; }
        public int Version { get; set; }
        public string Empresa { get; set; }
        public string CodigoOperador { get; set; }
        public string CodigoSap { get; set; }
        public bool Activo { get; set; }
        public bool EsCliente { get; set; }
        public string Estado { get; set; }
        public ClienteCrmFicha Ficha { get; set; }
        public List<ClienteCrmEmpresaOpcion> Empresas { get; set; }
        public List<ClienteCrmArchivo> Archivos { get; set; }
        public string Error { get; set; }
    }

    public class ClienteCrmDetalleViewModel
    {
        public ClienteCrmSolicitud Solicitud { get; set; }
        public ClienteCrmCliente Cliente { get; set; }
        public List<ClienteCrmArchivo> Archivos { get; set; }
        public List<ClienteCrmCliente> EmpresasCliente { get; set; }
        public List<ClienteCrmEvento> Eventos { get; set; }
        public bool EsVistaCreditos { get; set; }
        public bool PuedeAbrirClienteVinculado { get; set; }
        public bool PuedeResolver { get; set; }
        public bool PuedeEditar { get; set; }
    }

    public class ClienteCrmDashboardViewModel
    {
        public ClienteCrmDashboardViewModel()
        {
            Solicitudes = new List<ClienteCrmSolicitud>();
            Clientes = new List<ClienteCrmCliente>();
            Resumenes = new List<ClienteCrmResumenNegocio>();
        }
        public string Empresa { get; set; }
        public string Estado { get; set; }
        public string Filtro { get; set; }
        public bool PuedeAdministrar { get; set; }
        public bool PuedeVerCartera { get; set; }
        public bool PuedeVerCarteraGlobal { get; set; }
        public List<ClienteCrmSolicitud> Solicitudes { get; set; }
        public List<ClienteCrmCliente> Clientes { get; set; }
        public int Pendientes { get; set; }
        public int Aprobadas { get; set; }
        public int Rechazadas { get; set; }
        public List<ClienteCrmResumenNegocio> Resumenes { get; set; }
    }

    public class ClienteCrmResumenNegocio
    {
        public string Empresa { get; set; }
        public string Moneda { get; set; }
        public int Clientes { get; set; }
        public int CompranActualmente { get; set; }
        public decimal Ventas12Meses { get; set; }
        public decimal PotencialAnual { get; set; }
        public decimal Objetivo12Meses { get; set; }
    }
}
