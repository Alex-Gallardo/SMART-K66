using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    public class Warehouse
    {
        public string WarehouseId { get; set; }

        public string Nombre { get; set; }
    }

    public class ResponseContadorBodega
    {
        public int Contador { get; set; }

    }
}
