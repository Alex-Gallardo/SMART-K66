using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.BLL
{
    public class SerieAgenciaFacturaBL
    {

        private GiveContext db;

        public SerieAgenciaFacturaBL()
        {
            this.db = new GiveContext();

        }
        public string Guardar(SerieAgenciaFactura modelo)
        {
            return "";
        }

    }
}
