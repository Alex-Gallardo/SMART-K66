using System;
using System.Collections.Generic;
using System.Linq;
using DiamDev.Give.DAL;
using DiamDev.Give.Entities;

namespace DiamDev.Give.BLL
{
    /// <summary>
    /// Orquesta la sincronización de recibos:
    ///  - Pasada normal:  PENDIENTE -> OPERADO (créditos ya operó en SAP).
    ///  - Pasada inversa: OPERADO   -> PENDIENTE si se anuló en SAP (Opción A),
    ///                     o re-apunta DocEntry si se anuló+rehízo.
    /// La lógica vive aquí (BLL); el Sincronizador solo invoca Ejecutar().
    /// </summary>
    public class ReciboCajaSyncBL
    {
        private static readonly string[] EMPRESAS = { "GRACO", "FAES", "BOLIK" };
        // El tamaño de lote es configuración operativa (App.config -> SyncLoteRecibos),
        // resuelta por el DA. No se duplica aquí.
        private readonly ReciboCajaSyncDA _sql = new ReciboCajaSyncDA();
        private readonly HanaRepository _hana = new HanaRepository();

        /// <summary>Resumen de una corrida, para que el Sincronizador lo loguee.</summary>
        public class ResultadoSync
        {
            public int Revisados { get; set; }          // pendientes mirados
            public int Operados { get; set; }           // PENDIENTE -> OPERADO (nuevos)
            public int OperadosRevisados { get; set; }  // OPERADO re-verificados contra SAP
            public int Anulados { get; set; }           // OPERADO -> PENDIENTE (anulados en SAP)
            public int Reapuntados { get; set; }        // DocEntry actualizado (anuló+rehízo)
            public List<string> Errores { get; } = new List<string>();
        }

        /// <summary>Procesa las 3 empresas. Devuelve el resumen agregado.</summary>
        public ResultadoSync Ejecutar()
        {
            var res = new ResultadoSync();
            foreach (string empresa in EMPRESAS)
            {
                try
                {
                    ProcesarEmpresa(empresa, res);
                }
                catch (Exception ex)
                {
                    // Una empresa que falla NO debe tumbar a las otras dos.
                    res.Errores.Add(string.Format("[{0}] {1}", empresa, ex.Message));
                }
            }
            return res;
        }

        private void ProcesarEmpresa(string empresa, ResultadoSync res)
        {
            ProcesarPendientes(empresa, res);   // pasada normal
            RevisarAnulaciones(empresa, res);   // pasada inversa
        }

        // ── Pasada normal: PENDIENTE -> OPERADO ────────────────────────────
        private void ProcesarPendientes(string empresa, ResultadoSync res)
        {
            // 1. Lote de pendientes (cola rotativa). Sin 2º argumento -> lote de App.config.
            List<string> pendientes = _sql.ObtenerRecibosPendientes(empresa);
            if (pendientes.Count == 0) return;

            res.Revisados += pendientes.Count;

            // 2. SAP: cuáles ya están operados (ORCT, Canceled='N')
            List<SapCobroAplicado> operados = _hana.ObtenerCobrosOperados(empresa, pendientes);

            // 3. Marcar OPERADO los que SAP confirmó
            var idsOperados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var cobro in operados)
            {
                try
                {
                    _sql.MarcarReciboOperado(cobro, empresa);
                    idsOperados.Add(cobro.IdRecibo);
                    res.Operados++;
                }
                catch (Exception ex)
                {
                    res.Errores.Add(string.Format("[{0}] {1}: {2}",
                        empresa, cobro.IdRecibo, ex.Message));
                }
            }

            // 4. Los que NO aparecieron en SAP: sellar SYNC_ULTIMO_CHECK para que roten.
            var noOperados = pendientes.Where(id => !idsOperados.Contains(id)).ToList();
            _sql.MarcarUltimoCheckLote(noOperados, empresa);   // estado default = PENDIENTE
        }

        // ── Pasada inversa: OPERADO -> anulado / rehecho / sin cambio ──────
        private void RevisarAnulaciones(string empresa, ResultadoSync res)
        {
            // 1. Lote de OPERADOS (cola rotativa) con su DocEntry/DocNum guardado en SQL.
            List<SapCobroAplicado> operadosSql = _sql.ObtenerRecibosOperados(empresa);
            if (operadosSql.Count == 0) return;

            res.OperadosRevisados += operadosSql.Count;

            // 2. SAP: de esos IDs, cuáles siguen activos hoy (misma query que la pasada normal).
            var ids = operadosSql.Select(o => o.IdRecibo).ToList();
            List<SapCobroAplicado> activosSap = _hana.ObtenerCobrosOperados(empresa, ids);

            // Index por ID para lookup O(1) (como un Map<string, cobro> en JS).
            var activosPorId = activosSap.ToDictionary(
                a => a.IdRecibo, a => a, StringComparer.OrdinalIgnoreCase);

            var sinCambio = new List<string>();

            foreach (var op in operadosSql)
            {
                try
                {
                    if (activosPorId.TryGetValue(op.IdRecibo, out var sap))
                    {
                        // Sigue activo en SAP.
                        if (sap.SapDocEntry != op.SapDocEntry)
                        {
                            // Anuló + rehízo: re-apuntar al pago vigente.
                            string obs = string.Format(
                                "Re-apuntado en SAP: DocEntry {0}->{1}, DocNum {2}->{3} ({4:dd/MM/yyyy HH:mm}).",
                                op.SapDocEntry, sap.SapDocEntry,
                                op.SapDocNum, sap.SapDocNum, DateTime.Now);
                            _sql.ActualizarReferenciasSap(sap, empresa, obs);
                            res.Reapuntados++;
                        }
                        else
                        {
                            // Igualito: solo rota.
                            sinCambio.Add(op.IdRecibo);
                        }
                    }
                    else
                    {
                        // Ya NO está activo en SAP -> anulado -> Opción A: regresar a PENDIENTE.
                        string obs = string.Format(
                            "Anulado en SAP (sin cobro activo). Era DocNum {0}/DocEntry {1}. " +
                            "Regresado a PENDIENTE {2:dd/MM/yyyy HH:mm}.",
                            op.SapDocNum, op.SapDocEntry, DateTime.Now);
                        _sql.RegresarReciboAPendiente(op.IdRecibo, empresa, obs);
                        res.Anulados++;
                    }
                }
                catch (Exception ex)
                {
                    res.Errores.Add(string.Format("[{0}] inversa {1}: {2}",
                        empresa, op.IdRecibo, ex.Message));
                }
            }

            // Los confirmados sin cambio: sellar SYNC_ULTIMO_CHECK para que roten (estado OPERADO).
            _sql.MarcarUltimoCheckLote(sinCambio, empresa, "OPERADO");
        }
    }
}