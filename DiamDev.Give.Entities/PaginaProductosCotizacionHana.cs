using System.Collections.Generic;

namespace DiamDev.Give.Entities
{
    /// <summary>Página navegable del catálogo de artículos de SAP.</summary>
    public class PaginaProductosCotizacionHana
    {
        public PaginaProductosCotizacionHana()
        {
            Items = new List<ProductoCotizacionHana>();
        }

        public int Pagina { get; set; }
        public int Tamano { get; set; }
        public bool TieneAnterior { get; set; }
        public bool TieneMas { get; set; }
        public List<ProductoCotizacionHana> Items { get; set; }
    }
}
