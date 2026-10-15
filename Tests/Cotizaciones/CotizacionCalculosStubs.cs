// Sustitutos exclusivos para compilar las pruebas aisladas sin SAP ni SQL.
// No se incluyen en los proyectos de la aplicación.
using System;
using System.Collections.Generic;
using DiamDev.Give.Entities;

namespace DiamDev.Give.Entities
{
    public class ClienteHana
    {
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string LicTradNum { get; set; }
    }
}

namespace DiamDev.Give.DAL
{
    public class CotizacionDA
    {
        public bool ExisteSerie(string empresa) { return false; }
        public void GuardarCompleta(CotizacionEncabezado cotizacion) { }
        public List<CotizacionEncabezado> Listar(string empresa, string estado,
            string usuario, string agente, DateTime? desde, DateTime? hasta,
            string filtro) { return new List<CotizacionEncabezado>(); }
        public CotizacionEncabezado ObtenerPorId(string empresa, string id) { return null; }
        public int Anular(string empresa, string id, string usuario, string motivo) { return 0; }
    }

    public class HanaRepository
    {
        public List<ClienteHana> BuscarClientes(string empresa, string agente)
        { return new List<ClienteHana>(); }
        public PaginaProductosCotizacionHana BuscarProductosCotizacion(
            string empresa, string cliente, string filtro, int pagina, int tamano)
        { return new PaginaProductosCotizacionHana(); }
        public List<ProductoCotizacionHana> ObtenerProductosCotizacion(
            string empresa, string cliente, Dictionary<string, decimal> cantidades)
        { return new List<ProductoCotizacionHana>(); }
    }
}
