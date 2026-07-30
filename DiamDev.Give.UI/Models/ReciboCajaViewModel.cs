using System;
using System.Collections.Generic;
using DiamDev.Give.Entities;

namespace DiamDev.Give.UI.Models
{
    // ViewModel para cargar la vista inicial
    public class ReciboCajaIndexViewModel
    {
        public string PlantaUsuario { get; set; }
        public string UsuarioActual { get; set; }
    }

    // ─── ViewModels de entrada para acciones AJAX ───

    public class GuardarReciboRequest
    {
        public string IdEmpresa { get; set; }
        public string FechaRecibo { get; set; }  // "yyyy-MM-dd"
        public string IdCliente { get; set; }
        public string NombreCliente { get; set; }
        public string Direccion { get; set; }
        public string Nit { get; set; }
        public string Agente { get; set; }
        public string Correo { get; set; }
        public string Moneda { get; set; }
        public string RecFisico { get; set; }
        public string CodigoUsuario { get; set; }

        public List<CobroRequest> Cobros { get; set; }
        public List<DocumentoRequest> Documentos { get; set; }
    }

    public class CobroRequest
    {
        public string TipoCobro { get; set; }
        public string Banco { get; set; }
        public string FechaDoc { get; set; }  // "yyyy-MM-dd" o vacío
        public string NoDocumento { get; set; }
        public decimal Monto { get; set; }
        public string Moneda { get; set; }
    }

    public class DocumentoRequest
    {
        public string TipoDoc { get; set; }
        public string NoDocumento { get; set; }
        public string FechaDoc { get; set; }
        public string Status { get; set; }
        public decimal Monto { get; set; }
        public string Moneda { get; set; }
        public decimal MontoFact { get; set; }
        public decimal Pagado { get; set; }
        public string FelSerie { get; set; }
        public string FelNumero { get; set; }
    }
}