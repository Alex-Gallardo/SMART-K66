using System.Collections.Generic;

namespace DiamDev.Give.Entities
{
    /// <summary>
    /// Respuesta de las operaciones del BLL. Copia de ResultadoRecibo, que es
    /// el patrón que ya usa el proyecto.
    /// </summary>
    public class ResultadoBorradorNc
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; }
        public string IdBorrador { get; set; }

        /// <summary>Avisos que no impiden guardar pero el usuario debe ver
        /// (factura ya pagada, NC previas en SAP...).</summary>
        public List<string> Advertencias { get; set; }

        public ResultadoBorradorNc()
        {
            Advertencias = new List<string>();
        }

        public static ResultadoBorradorNc Error(string msg) =>
            new ResultadoBorradorNc { Exito = false, Mensaje = msg };

        public static ResultadoBorradorNc Ok(string id = "", string msg = null) =>
            new ResultadoBorradorNc
            {
                Exito = true,
                Mensaje = msg ?? "Operación realizada correctamente.",
                IdBorrador = id
            };
    }
}
