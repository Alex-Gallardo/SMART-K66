using System;
using System.Collections.Generic;

namespace DiamDev.Give.Entities
{
    public class REQUEST_DATA
    {
        public int Respuesta { get; set; }
        public string Codigo { get; set; }
        public string Procesador { get; set; }
        public IList<object> Mensaje { get; set; }
        public string Descripcion { get; set; }
        public DateTime Fecha { get; set; }

    }
    public class RESPONSE
    {
        public string PAIS { get; set; }
        public string NIT { get; set; }
        public string NOMBRE { get; set; }
        public string Direccion { get; set; }
        public string DEPARTAMENTO { get; set; }
        public string MUNICIPIO { get; set; }

    }
    public class ClienteDigifact
    {
        public IList<REQUEST_DATA> REQUEST_DATA { get; set; }
        public IList<RESPONSE> RESPONSE { get; set; }
    }
}
