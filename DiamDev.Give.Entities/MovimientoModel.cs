using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class MovimientoModel
    {
        public long MovimientoId { get; set; }

        public string Categoria { get; set; }

        public string Agencia { get; set; }

        public long Id { get; set; }

        public string Nombre { get; set; }

        public string Descripcion { get; set; }

        public int Descuento { get; set; }

        public decimal Total { get; set; }

        public long UsuarioId { get; set; }

        public string Usuario { get; set; }

        public string Forma { get; set; }
       
        public DateTime Fecha { get; set; }

        public decimal Cantidad { get; set; }

        public decimal Precio { get; set; }
    }
}
