using System;
using System.Collections.Generic;

namespace DiamDev.Give.Entities
{
    /// <summary>
    /// Estados de un borrador NC.
    ///
    /// ⚠️ SI LEES DATOS LEGADOS DE APK66, ATENCIÓN.
    /// La tabla vieja usaba códigos de un carácter y el comentario del propio
    /// stored procedure rec_borr_existe está EQUIVOCADO:
    ///
    ///     El comentario dice:  A = AUTORIZADO,  R = RECHAZADO
    ///     El código hace:      AUTORIZADO -> 'R',  RECHAZADO -> 'X',
    ///                          y 'A' es el estado inicial (pendiente)
    ///
    /// Lo confirma rec_borr_listar_seg:
    ///     CASE WHEN STATUS='R' THEN 'AUTORIZADO' ELSE 'RECHAZADO' END
    ///
    /// El esquema nuevo guarda texto para que nadie tenga que leer esto.
    /// </summary>
    public static class EstadosBorradorNc
    {
        public const string Pendiente = "PENDIENTE";
        public const string Autorizado = "AUTORIZADO";
        public const string Rechazado = "RECHAZADO";
        public const string Anulado = "ANULADO";

        private static readonly HashSet<string> _validos =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { Pendiente, Autorizado, Rechazado, Anulado };

        public static bool EsValido(string estado) =>
            !string.IsNullOrWhiteSpace(estado) && _validos.Contains(estado.Trim());

        /// <summary>Estados que mantienen comprometido el importe de la factura.</summary>
        public static bool ComprometeSaldo(string estado) =>
            Pendiente.Equals(estado, StringComparison.OrdinalIgnoreCase) ||
            Autorizado.Equals(estado, StringComparison.OrdinalIgnoreCase);

        /// <summary>Traduce el STATUS de un carácter de APK66. Ver advertencia arriba.</summary>
        public static string DesdeLegado(string status, string tipoAuto)
        {
            switch ((status ?? "").Trim().ToUpperInvariant())
            {
                case "A": return Pendiente;
                case "R": return Autorizado;
                case "X":
                    return "RECHAZADO".Equals((tipoAuto ?? "").Trim(),
                               StringComparison.OrdinalIgnoreCase)
                           ? Rechazado : Anulado;
                default: return Pendiente;
            }
        }

        public static IEnumerable<string> Todos() => _validos;
    }
}