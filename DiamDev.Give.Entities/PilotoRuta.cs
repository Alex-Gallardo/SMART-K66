using System;
using System.Collections.Generic;

namespace DiamDev.Give.Entities
{
    public sealed class PilotoRuta
    {
        public string Id { get; set; }
        public DateTime Fecha { get; set; }
        public string Placa { get; set; }
        public string Transporte { get; set; }
        public string Vehiculo { get; set; }
        public int TotalDocumentos { get; set; }
        public int PaginaDocumentos { get; set; }
        public int TamanoPaginaDocumentos { get; set; }
        public bool HayMasDocumentos { get { return (long)PaginaDocumentos * TamanoPaginaDocumentos < TotalDocumentos; } }
        public string Piloto { get; set; }
        public string Centro { get; set; }
        public string Estado { get; set; }
        public string Version { get; set; }
        public Guid Solicitud { get; set; }
        public bool PuedeCerrar { get; set; }
        public List<PilotoDocumento> Documentos { get; set; }
        public PilotoRuta() { Documentos = new List<PilotoDocumento>(); }
        public string EstadoNombre
        {
            get { return Estado == "A" ? "Abierta" : Estado == "E" ? "En ruta" : Estado == "C" ? "Cerrada" : Estado == "X" ? "Anulada" : "Sin clasificar"; }
        }
    }

    public sealed class PilotoDocumento
    {
        public int RowId { get; set; }
        public string Tipo { get; set; }
        public string Empresa { get; set; }
        public string Documento { get; set; }
        public string Cliente { get; set; }
        public string Direccion { get; set; }
        public decimal Bultos { get; set; }
        public bool? Visito { get; set; }
        public string Entrega { get; set; }
        public string Motivo { get; set; }
        public string Observaciones { get; set; }
        // Valor temporal del formulario; no forma parte del estado persistido ni de la version.
        public string ObservacionPiloto { get; set; }
        public TimeSpan? Entrada { get; set; }
        public TimeSpan? Salida { get; set; }
    }

    // Solo se aceptan resultados; identidad, vehiculo y datos comerciales salen del servidor.
    public sealed class PilotoResultado
    {
        public int RowId { get; set; }
        public bool? Visito { get; set; }
        public string Entrega { get; set; }
        public string Motivo { get; set; }
        public string Observaciones { get; set; }
    }

    public sealed class PilotoCierre
    {
        public string RutaId { get; set; }
        public string Version { get; set; }
        public Guid Solicitud { get; set; }
        public List<PilotoResultado> Documentos { get; set; }
    }

    public sealed class PilotoLista
    {
        public string Vista { get; set; }
        public bool RutaFija { get; set; }
        public List<PilotoRuta> Rutas { get; set; }
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public bool HayMas { get; set; }
        public int Pagina { get; set; }
    }

    public sealed class PilotoException : Exception
    {
        public int StatusCode { get; private set; }
        public PilotoException(int statusCode, string message) : base(message) { StatusCode = statusCode; }
    }
}
