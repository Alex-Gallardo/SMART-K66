using System;
using System.Collections.Generic;

namespace DiamDev.Give.Entities
{
    /// <summary>
    /// Equivale a la tabla REC_CAJA_ENC en APK66.
    /// Contiene el encabezado del recibo + sus dos listados de detalle.
    /// Es como el "objeto raíz" — trae todo el recibo junto.
    /// </summary>
    public class ReciboCajaEncabezado
    {
        public string IdRecibo { get; set; }
        public string IdEmpresa { get; set; }
        public string IdCliente { get; set; }
        public string NombreCliente { get; set; }
        public string Direccion { get; set; }
        public string Nit { get; set; }
        public string Agente { get; set; }
        public string Correo { get; set; }
        public string Moneda { get; set; }
        public string Status { get; set; }
        public decimal MontoTotalRecibo { get; set; }
        public decimal MontoTotalDoc { get; set; }
        public decimal Saldo { get; set; }
        public string Usuario { get; set; }
        public DateTime FechaRecibo { get; set; }
        public DateTime FechaRegistro { get; set; }
        public string RecFisico { get; set; }

        // ── NUEVO: moneda base + tipo de cambio + totales duales ──
        public string MonedaBase { get; set; }        // 'GTQ'
        public decimal? TipoCambio { get; set; }       // GTQ por 1 USD, congelado
        public decimal MontoTotalRecGtq { get; set; }
        public decimal MontoTotalRecUsd { get; set; }
        public decimal MontoTotalDocGtq { get; set; }
        public decimal MontoTotalDocUsd { get; set; }
        public decimal SaldoGtq { get; set; }          // saldo que VALIDA
        public decimal SaldoUsd { get; set; }

        // Listas de detalle anidadas (como los dos DataGridView del desktop)
        public List<ReciboCajaCobro> Cobros { get; set; }
        public List<ReciboCajaDetalle> Documentos { get; set; }

        public ReciboCajaEncabezado()
        {
            Cobros = new List<ReciboCajaCobro>();
            Documentos = new List<ReciboCajaDetalle>();
            FechaRecibo = DateTime.Today;
            MonedaBase = "GTQ";   // ← agregar esta línea
        }
    }
}