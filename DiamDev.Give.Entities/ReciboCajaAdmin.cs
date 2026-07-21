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

        /// <summary>Recibos regresados a PENDIENTE por anulación TOTAL del pago
        /// en SAP (SYNC_ESTADO). NO confundir con AnuladosMes.</summary>
        public int PendientesAnulados { get; set; }

        public int OperadosHoy { get; set; }
        public int OperadosSemana { get; set; }
        public int DiasUmbral { get; set; }

        // ── Anulaciones desde la web (STATUS = 'X') ──
        // Métrica del MES EN CURSO, no histórica: un contador acumulado
        // ("3,412 anulados desde siempre") no es accionable para nadie.
        // Se cuenta por FECHA_ANULACION, que es cuando ocurrió el hecho.

        /// <summary>Recibos anulados en la web durante el mes en curso.</summary>
        public int AnuladosMes { get; set; }

        /// <summary>Monto GTQ que representan esos anulados (MONTO_T_DOC_GTQ).</summary>
        public decimal AnuladosMesMontoGtq { get; set; }
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
        /// DESCUADRE | ANULADO_SAP | ENVEJECIDO | PENDIENTE | OPERADO | ANULADO
        ///
        /// OJO con los dos "anulados", son cosas distintas:
        ///   ANULADO_SAP → SYNC_ESTADO: el PAGO se anuló en SAP, el recibo sigue
        ///                 vivo y espera re-operación.
        ///   ANULADO     → STATUS='X': el RECIBO se anuló desde la web. Está
        ///                 muerto, no se sincroniza, solo se audita.</summary>
        public string Situacion { get; set; }

        // ── Auditoría de anulación (solo se llenan si Situacion == "ANULADO") ──

        /// <summary>Login del usuario que anuló el recibo.</summary>
        public string AnuladoPor { get; set; }

        /// <summary>Fecha/hora de la anulación, ya formateada (yyyy-MM-dd HH:mm).
        /// Vacía en anulados previos a la auditoría.</summary>
        public string FechaAnulacion { get; set; }

        /// <summary>Motivo capturado al anular (columna MOTIVO, nvarchar(150)).</summary>
        public string MotivoAnulacion { get; set; }
    }

    /// <summary>
    /// Alcance de datos del dashboard para el usuario logueado.
    ///
    /// Regla de oro: FALLA CERRADO. Si no hay pares y no es global,
    /// el usuario no ve NADA. Devolver "sin filtro" cuando no se pudo
    /// resolver el alcance sería exponer los datos de todos.
    ///
    /// En TS: type Alcance = { global: boolean; pares: {empresa,codigo}[] }
    /// </summary>
    public class AlcanceRecibos
    {
        /// <summary>true = rol supervisor (CREDITOS, GERENCIA...): ve todo, sin filtro.</summary>
        public bool Global { get; set; }

        /// <summary>Pares (empresa, código) que el usuario tiene en Usuario_Empresa.</summary>
        public List<AlcancePar> Pares { get; set; } = new List<AlcancePar>();

        /// <summary>true = no es global y no tiene ningún par → no debe ver nada.</summary>
        public bool SinAcceso
        {
            get { return !Global && (Pares == null || Pares.Count == 0); }
        }

        /// <summary>Texto para la UI: "Global" o "3 operador(es)".</summary>
        public string Descripcion
        {
            get
            {
                if (Global) return "Global";
                if (SinAcceso) return "Sin operadores asignados";
                return Pares.Count + " operador(es)";
            }
        }

        /// <summary>
        /// Empresas visibles para este alcance, sin repetir. La usa la vista para
        /// pintar SOLO los botones que tienen sentido: un usuario únicamente de
        /// GRACO no debe poder hacer clic en "Bolik" y quedarse viendo un grid
        /// vacío sin entender por qué.
        ///
        /// Es cosmética, no seguridad: el servidor filtra igual aunque alguien
        /// mande empresa=BOLIK desde F12.
        ///
        /// Sin LINQ a propósito — el proyecto Entities no lo importa y no quiero
        /// agregarle dependencias a una capa que debe ser POCO puro.
        /// </summary>
        public List<string> Empresas
        {
            get
            {
                var lista = new List<string>();

                if (Global)
                {
                    lista.Add("GRACO"); lista.Add("FAES"); lista.Add("BOLIK");
                    return lista;
                }

                if (Pares == null) return lista;

                foreach (var p in Pares)
                {
                    string emp = (p.Empresa ?? "").Trim();
                    if (emp.Length == 0) continue;

                    bool ya = false;
                    foreach (var e in lista)
                        if (string.Equals(e, emp, StringComparison.OrdinalIgnoreCase))
                        { ya = true; break; }

                    if (!ya) lista.Add(emp);
                }
                return lista;
            }
        }
    }

    /// <summary>
    /// Un par (empresa, código) del alcance.
    ///
    /// Se compara el PAR COMPLETO, nunca el código suelto: la misma persona
    /// tiene códigos distintos en cada empresa (FAES '13-PABLO GAITAN' vs
    /// GRACO '15-PABLO GAITAN'), y códigos iguales pueden existir en empresas
    /// distintas ('2-GERENCIA' en FAES y en GRACO). Comparar solo el código
    /// perdería datos propios y expondría datos ajenos a la vez.
    /// </summary>
    public class AlcancePar
    {
        public string Empresa { get; set; }
        public string Codigo { get; set; }
    }
}