using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace DiamDev.Give.Entities
{
    public class CuentaGeneral
    {
        public long AgenciaId { get; set; }

        public long MesaId { get; set; }

        public long ClienteId { get; set; }

        public string Mesa { get; set; }

        public string Nit { get; set; }

        public string Nombre { get; set; }

        public string Direccion { get; set; }

        public string Telefono { get; set; }

        public string Token { get; set; }

        public List<CuentaModel> Cuentas { get; set; }
    }
}
