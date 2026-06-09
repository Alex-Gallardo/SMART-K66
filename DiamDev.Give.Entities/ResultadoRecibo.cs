namespace DiamDev.Give.Entities
{
    /// <summary>
    /// Objeto de respuesta para operaciones del BLL.
    /// En JS/TS sería equivalente a un tipo { ok: boolean, message: string, data?: any }
    /// </summary>
    public class ResultadoRecibo
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; }
        public string IdRecibo { get; set; }  // Devuelve el ID generado al guardar

        public static ResultadoRecibo Error(string msg) => new ResultadoRecibo { Exito = false, Mensaje = msg };
        public static ResultadoRecibo Ok(string id = "") => new ResultadoRecibo { Exito = true, Mensaje = "Registro guardado exitosamente.", IdRecibo = id };
    }
}