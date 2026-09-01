using System;
using System.Collections.Generic;

namespace DiamDev.Give.Entities
{
    /// <summary>
    /// Encabezado de un borrador de nota de crédito (BORR_NC_ENC).
    /// Equivale al FrmBorradores del desktop.
    ///
    /// Diferencias con el legado REC_CAJA_BORR_ENC:
    ///   - Total es decimal, no string.
    ///   - STATUS de un carácter -> Estado con texto legible.
    ///   - USR_AUTO / USR_ANULA / MOT_AUTO / TIPO_AUTO (cuatro columnas que se
    ///     llenaban distinto según el desenlace) -> un solo juego
    ///     ResueltoPor / FechaResolucion / MotivoResolucion.
    /// </summary>
    public class BorradorNcEncabezado
    {
        public string IdBorrador { get; set; }
        public string IdEmpresa { get; set; }
        public DateTime Fecha { get; set; }

        // Foto del cliente al capturar, no referencia viva.
        public string IdCliente { get; set; }      // CardCode
        public string Nombre { get; set; }         // CardName
        public string Nit { get; set; }            // LicTradNum
        public string Direccion { get; set; }
        public string Correo { get; set; }
        public string Agente { get; set; }         // SlpName

        public string Moneda { get; set; }
        public decimal Total { get; set; }

        public string Estado { get; set; }
        public string IdUsr { get; set; }          // login que capturó
        public string Depto { get; set; }          // informativo, opcional
        public string CodigoOperador { get; set; } // Usuario_Empresa.Codigo
        public DateTime? Registro { get; set; }

        public string ResueltoPor { get; set; }
        public DateTime? FechaResolucion { get; set; }
        public string MotivoResolucion { get; set; }

        // Indicador agregado por las consultas de bandeja. No es una columna del
        // encabezado; evita una consulta por fila para pintar alertas y KPIs.
        public bool TieneNcPrevia { get; set; }

        public List<BorradorNcDetalle> Detalles { get; set; }
        public List<BorradorNcAdjunto> Adjuntos { get; set; }

        public BorradorNcEncabezado()
        {
            Detalles = new List<BorradorNcDetalle>();
            Adjuntos = new List<BorradorNcAdjunto>();
            Estado = EstadosBorradorNc.Pendiente;
            Fecha = DateTime.Today;
        }

        // Atajos para no repetir comparaciones de string en Razor
        public bool EsPendiente => EstadosBorradorNc.Pendiente == Estado;
        public bool EsAutorizado => EstadosBorradorNc.Autorizado == Estado;
        public bool EsRechazado => EstadosBorradorNc.Rechazado == Estado;
        public bool EsAnulado => EstadosBorradorNc.Anulado == Estado;
    }
}
