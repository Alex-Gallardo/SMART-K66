using System;
using System.Collections.Generic;

namespace DiamDev.Give.Entities
{
    /// <summary>Fila de REC_CAJA_SERIES + el máximo correlativo real ya usado.</summary>
    public class ReciboCajaSerie
    {
        public int RowId { get; set; }
        public string Empresa { get; set; }
        public string Depto { get; set; }
        public string Serie { get; set; }
        public int Numeracion { get; set; }
        public string SerieNc { get; set; }        // reservado a futuro
        public int NumeracionNc { get; set; }      // reservado a futuro

        /// <summary>Máximo sufijo numérico ya emitido en REC_CAJA_ENC con esta serie.
        /// 0 = la serie nunca ha emitido recibos.</summary>
        public int MaxUsado { get; set; }

        /// <summary>Próximo ID que generará esta serie (SERIE + 5 dígitos).</summary>
        public string ProximoId
        {
            get { return (Serie ?? "") + (Numeracion + 1).ToString("00000"); }
        }

        /// <summary>true si NUMERACION quedó por debajo de lo ya emitido → el próximo
        /// INSERT chocaría con un ID existente. Debe corregirse de inmediato.</summary>
        public bool Inconsistente
        {
            get { return Numeracion < MaxUsado; }
        }
    }

    /// <summary>Tarjetas de resumen del dashboard de supervisión.</summary>
    public class DashboardResumenRecibos
    {
        public int Descuadres { get; set; }
        public decimal DescuadresMontoGtq { get; set; }
        public int PendientesTotal { get; set; }
        public int PendientesEnvejecidos { get; set; }
        public int PendientesAnulados { get; set; }
        public int OperadosHoy { get; set; }
        public int OperadosSemana { get; set; }
        public int DiasUmbral { get; set; }
    }

    /// <summary>Fila del grid de detalle del dashboard.</summary>
    public class DashboardFilaRecibo
    {
        public string IdRecibo { get; set; }
        public string IdEmpresa { get; set; }
        public string NombreCliente { get; set; }
        public string Usuario { get; set; }
        public decimal MontoGtq { get; set; }
        public string SyncEstado { get; set; }
        public string SyncObservacion { get; set; }
        public string FechaRegistro { get; set; }   // ya formateada yyyy-MM-dd
        public int DiasAntiguedad { get; set; }

        /// <summary>Clasificación para el filtro/badge:
        /// DESCUADRE | ANULADO_SAP | ENVEJECIDO | PENDIENTE</summary>
        public string Situacion { get; set; }
    }
}