using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class VentaModel
    {
        public string Id { get; set; }

        public string Codigo { get; set; }

        public long MarcaId { get; set; }

        public string Marca { get; set; }

        public string Descripcion { get; set; }

        public long SerieId { get; set; }

        public string Serie { get; set; }

        public long NoFactura { get; set; }

        public long AgenciaId { get; set; }

        public string Agencia { get; set; }

        public string Vendedor { get; set; }

        public decimal Total { get; set; }

        public decimal CostoIva { get; set; }

        public decimal PrecioIva { get; set; }
        
        public decimal Descuento { get; set; }

        public long FacturaId { get; set; }
                
        public string Concepto { get; set; }
        
        public DateTime Fecha { get; set; }
        
        public int Dias { get; set; }
        
        public decimal Cantidad { get; set; }

        public bool Estado { get; set; }
    }
}
