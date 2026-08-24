using System;
using System.Collections.Generic;

namespace DiamDev.Give.Entities
{
    /// <summary>
    /// Conceptos válidos de una línea. Venían hardcodeados en el combo
    /// CbbConcepto de FrmBorradores.Designer.cs.
    ///
    /// Vive en Entities —igual que TiposDocumentoRecibo— porque lo necesitan
    /// la vista (llenar el combo), el BLL (validar) y el esquema (el CHECK).
    /// Una sola fuente de verdad.
    /// </summary>
    public static class ConceptosBorradorNc
    {
        public const string Devolucion = "DEVOLUCION";
        public const string Descuento = "DESCUENTO";
        public const string Otros = "OTROS";

        private static readonly HashSet<string> _validos =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { Devolucion, Descuento, Otros };

        public static bool EsValido(string concepto) =>
            !string.IsNullOrWhiteSpace(concepto) && _validos.Contains(concepto.Trim());

        public static IEnumerable<string> Todos() => _validos;
    }
}