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
    ///  - Pasada inversa: OPERADO -> PENDIENTE si se anuló; re-apunta DocEntry si
    ///                     se anuló+rehízo; y CONCILIA montos (SQL vs RCT2) para los
    ///                     que siguen activos.
    /// La conciliación NO cambia SYNC_ESTADO: solo marca SYNC_OBSERVACION.
    /// </summary>
    public class ReciboCajaSyncBL
    {
        private static readonly string[] EMPRESAS = { "GRACO", "FAES", "BOLIK" };

        // Tolerancia de conciliación (App.config -> SyncToleranciaMonto). Default 0.05.
        // Absorbe redondeo acumulado sin dejar pasar descuadres reales (de quetzales).
        private const decimal TOLERANCIA_FALLBACK = 0.05m;

        private readonly ReciboCajaSyncDA _sql = new ReciboCajaSyncDA();
        private readonly HanaRepository _hana = new HanaRepository();

        private decimal Tolerancia
        {
            get
            {
                string raw = System.Configuration.ConfigurationManager.AppSettings["SyncToleranciaMonto"];
                return (decimal.TryParse(raw, out decimal v) && v >= 0) ? v : TOLERANCIA_FALLBACK;
            }
        }

        public class ResultadoSync
        {
            public int Revisados { get; set; }
            public int Operados { get; set; }
            public int OperadosRevisados { get; set; }
            public int Anulados { get; set; }
            public int Reapuntados { get; set; }
            public int Conciliados { get; set; }     // revisados por conciliación
            public int Descuadrados { get; set; }     // marcados con bandera [CONCIL]
            public List<string> Errores { get; } = new List<string>();
        }

        public ResultadoSync Ejecutar()
        {
            var res = new ResultadoSync();
            foreach (string empresa in EMPRESAS)
            {
                try { ProcesarEmpresa(empresa, res); }
                catch (Exception ex)
                {
                    res.Errores.Add(string.Format("[{0}] {1}", empresa, ex.Message));
                }
            }
            return res;
        }

        private void ProcesarEmpresa(string empresa, ResultadoSync res)
        {
            ProcesarPendientes(empresa, res);   // pasada normal
            RevisarAnulaciones(empresa, res);   // pasada inversa + conciliación
        }

        // ── Pasada normal: PENDIENTE -> OPERADO ────────────────────────────
        private void ProcesarPendientes(string empresa, ResultadoSync res)
        {
            List<string> pendientes = _sql.ObtenerRecibosPendientes(empresa);
            if (pendientes.Count == 0) return;

            res.Revisados += pendientes.Count;

            List<SapCobroAplicado> operados = _hana.ObtenerCobrosOperados(empresa, pendientes);

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

            var noOperados = pendientes.Where(id => !idsOperados.Contains(id)).ToList();
            _sql.MarcarUltimoCheckLote(noOperados, empresa);
        }

        // ── Pasada inversa: anulación / reapuntado / conciliación ──────────
        private void RevisarAnulaciones(string empresa, ResultadoSync res)
        {
            List<SapCobroAplicado> operadosSql = _sql.ObtenerRecibosOperados(empresa);
            if (operadosSql.Count == 0) return;

            res.OperadosRevisados += operadosSql.Count;

            var ids = operadosSql.Select(o => o.IdRecibo).ToList();
            List<SapCobroAplicado> activosSap = _hana.ObtenerCobrosOperados(empresa, ids);
            var activosPorId = activosSap.ToDictionary(
                a => a.IdRecibo, a => a, StringComparer.OrdinalIgnoreCase);

            // Para conciliar: junto los DocEntry de los que siguen activos y pido RCT2 en UN viaje.
            var docEntriesActivos = activosSap.Select(a => a.SapDocEntry).Distinct().ToList();
            Dictionary<int, MontoAplicadoSap> montosSap =
                _hana.ObtenerMontosAplicados(empresa, docEntriesActivos);

            // Necesito la moneda y el MONTO_T_DOC de cada recibo para conciliar.
            // Los traigo del SQL en un solo lote (método nuevo, ver abajo).
            Dictionary<string, ReciboMontoSql> datosSql =
                _sql.ObtenerDatosConciliacion(empresa, ids);

            var sinCambio = new List<string>();

            foreach (var op in operadosSql)
            {
                try
                {
                    if (activosPorId.TryGetValue(op.IdRecibo, out var sap))
                    {
                        // Sigue activo. ¿Cambió el DocEntry? -> reapuntar.
                        if (sap.SapDocEntry != op.SapDocEntry)
                        {
                            string obs = string.Format(
                                "Re-apuntado en SAP: DocEntry {0}->{1}, DocNum {2}->{3} ({4:dd/MM/yyyy HH:mm}).",
                                op.SapDocEntry, sap.SapDocEntry,
                                op.SapDocNum, sap.SapDocNum, DateTime.Now);
                            _sql.ActualizarReferenciasSap(sap, empresa, obs);
                            res.Reapuntados++;
                        }

                        // Conciliar montos (use el DocEntry vigente = el de SAP).
                        ConciliarRecibo(op.IdRecibo, empresa, sap.SapDocEntry,
                                        datosSql, montosSap, res);

                        sinCambio.Add(op.IdRecibo);
                    }
                    else
                    {
                        // No está activo -> anulado -> Opción A: regresar a PENDIENTE.
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

            _sql.MarcarUltimoCheckLote(sinCambio, empresa, "OPERADO");
        }

        // ── Conciliación de un recibo ──────────────────────────────────────
        private void ConciliarRecibo(string idRecibo, string empresa, int docEntry,
                                     Dictionary<string, ReciboMontoSql> datosSql,
                                     Dictionary<int, MontoAplicadoSap> montosSap,
                                     ResultadoSync res)
        {
            res.Conciliados++;

            if (!datosSql.TryGetValue(idRecibo, out var sql)) return; // sin datos, no concilio

            // Anticipo: RCT2 vacío -> no aparece en montosSap. Conciliamos contra el
            // total del pago no aplicado a facturas. Por ahora, si no hay líneas RCT2,
            // NO marcamos descuadre (el pago existe y está activo; el monto va por ORCT).
            if (!montosSap.TryGetValue(docEntry, out var msap))
            {
                _sql.MarcarConciliacion(idRecibo, empresa, null); // limpia bandera [CONCIL] si había
                return;
            }

            // Elegir la columna de SAP según la moneda del recibo.
            bool esUSD = string.Equals((sql.Moneda ?? "").Trim(), "USD",
                                       StringComparison.OrdinalIgnoreCase);
            decimal montoSapMoneda = esUSD ? msap.MontoUSD : msap.MontoGTQ;
            decimal montoSql = sql.MontoTDoc;

            decimal diferencia = Math.Abs(montoSql - montoSapMoneda);

            if (diferencia <= Tolerancia)
            {
                // Cuadra: limpiar cualquier bandera [CONCIL] previa.
                _sql.MarcarConciliacion(idRecibo, empresa, null);
            }
            else
            {
                string obs = string.Format(
                    "Descuadre montos ({0}): SQL={1:N2} vs SAP={2:N2}, dif={3:N2} ({4:dd/MM/yyyy HH:mm}).",
                    esUSD ? "USD" : "GTQ", montoSql, montoSapMoneda, diferencia, DateTime.Now);
                _sql.MarcarConciliacion(idRecibo, empresa, obs);
                res.Descuadrados++;
            }
        }
    }
}