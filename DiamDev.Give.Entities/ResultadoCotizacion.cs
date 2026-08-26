namespace DiamDev.Give.Entities
{
    public class ResultadoCotizacion
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; }
        public string IdCotizacion { get; set; }

        public static ResultadoCotizacion Ok(string id)
        {
            return new ResultadoCotizacion
            {
                Exito = true,
                IdCotizacion = id,
                Mensaje = "Cotización " + id + " creada correctamente."
            };
        }

        public static ResultadoCotizacion Error(string mensaje)
        {
            return new ResultadoCotizacion { Exito = false, Mensaje = mensaje };
        }
    }
}
