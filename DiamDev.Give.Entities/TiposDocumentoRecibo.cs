using System;
using System.Collections.Generic;

namespace DiamDev.Give.Entities
{
    /// <summary>
    /// Catálogo y reglas de los TIPO_DOC del detalle de un recibo (REC_CAJA_DET).
    ///
    /// ¿Por qué en Entities y no en el BLL? Porque la regla "este tipo no lleva
    /// documento de referencia" la necesitan TRES capas a la vez: la vista (para
    /// bloquear campos), el DAL (para grabar NULL en vez de '') y el BLL (para no
    /// consultar un catálogo que no existe). Entities es la única capa que las tres
    /// referencian, así que es el único lugar donde puede vivir sin duplicarse.
    ///
    /// Equivalente TS:
    ///   export const SIN_DOCUMENTO = new Set(["ANTICIPO","SALDO PENDIENTE","DIFERENCIA"]);
    /// El HashSet con OrdinalIgnoreCase es literalmente eso, pero case-insensitive.
    /// </summary>
    public static class TiposDocumentoRecibo
    {
        public const string Factura = "FACTURA";
        public const string Pedido = "PEDIDO";
        public const string Anticipo = "ANTICIPO";
        public const string SaldoPendiente = "SALDO PENDIENTE";
        public const string Diferencia = "DIFERENCIA";
        public const string NotaCredito = "NOTA DE CREDITO";

        /// <summary>
        /// Tipos que NO apuntan a un documento real de SAP.
        /// Consecuencias (las tres van juntas, siempre):
        ///   1. NO_DOCUMENTO, FECHA_DOC y STATUS se graban NULL.
        ///   2. No se puede abrir el modal de búsqueda (no hay catálogo).
        ///   3. En la UI solo son capturables MONTO y MONEDA.
        /// </summary>
        private static readonly HashSet<string> _sinDocumento =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Anticipo,
                SaldoPendiente,
                Diferencia
            };

        /// <summary>
        /// Tipos cuyo catálogo de documentos disponibles vive en SAP HANA
        /// (vista RC_FACTURAS_REC_CAJ). El resto va a SQL o no se consulta.
        /// </summary>
        private static readonly HashSet<string> _consultablesHana =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Factura,
                Pedido
            };

        /// <summary>¿Este tipo va sin documento de referencia? (ANTICIPO, DIFERENCIA...)</summary>
        public static bool EsSinDocumento(string tipoDoc)
        {
            return _sinDocumento.Contains((tipoDoc ?? "").Trim());
        }

        /// <summary>¿El catálogo de este tipo se busca en HANA?</summary>
        public static bool EsConsultableHana(string tipoDoc)
        {
            return _consultablesHana.Contains((tipoDoc ?? "").Trim());
        }
    }
}