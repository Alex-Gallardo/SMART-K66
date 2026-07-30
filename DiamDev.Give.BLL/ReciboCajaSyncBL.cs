using System;
using System.Collections.Generic;
using System.Linq;
using DiamDev.Give.DAL;
using DiamDev.Give.Entities;

namespace DiamDev.Give.BLL
{
    /// <summary>
    /// Orquesta la sincronización de recibos:
    ///  - Pasada normal : PENDIENTE -> OPERADO (créditos ya operó en SAP).
    ///  - Pasada inversa: revisa OPERADO y DESCUADRE contra SAP:
    ///      * 0 pagos activos          -> anulación TOTAL   -> PENDIENTE
    ///      * activos que NO cuadran   -> anulación PARCIAL -> DESCUADRE
    ///      * activos que SÍ cuadran   -> OPERADO (sana el DESCUADRE si venía de ahí)
    ///    Un recibo puede tener N pagos ORCT (Créditos crea manuales con el mismo
    ///    U_Recibocaja_Webapp): la conciliación SUMA todos los activos.
    /// </summary>
    public class ReciboCajaSyncBL
    {
        private static readonly string[] EMPRESAS = { "GRACO", "FAES", "BOLIK" };

        // Tolerancia de conciliación (App.config -> SyncToleranciaMonto). Default 0.05.
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
            public int Anulados { get; set; }        // anulación TOTAL -> PENDIENTE
            public int Reapuntados { get; set; }
            public int Conciliados { get; set; }     // revisados por conciliación
            public int Descuadrados { get; set; }    // transiciones NUEVAS a DESCUADRE
            public int Sanados { get; set; }         // DESCUADRE -> OPERADO (self-healing)
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

        // ── Pasada normal: PENDIENTE -> OPERADO (sin cambios) ──────────────
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

        // ── Pasada inversa: anulación total / parcial / reapunte / sanación ─
        private void RevisarAnulaciones(string empresa, ResultadoSync res)
        {
            // OPERADO **y** DESCUADRE: los descuadrados también se re-revisan
            // para poder sanarse solos cuando Créditos re-aplica el pago.
            List<ReciboRevisionSql> revisar = _sql.ObtenerRecibosParaRevision(empresa);
            if (revisar.Count == 0) return;

            res.OperadosRevisados += revisar.Count;

            var ids = revisar.Select(o => o.IdRecibo).ToList();

            // UN viaje a HANA por lote: TODOS los ORCT (activos y anulados),
            // con montos RCT2 y facturas aplicadas ya resueltos.
            List<SapPagoDetalle> pagos = _hana.ObtenerPagosSapDetalle(empresa, ids);
            var pagosPorRecibo = pagos
                .GroupBy(p => p.IdRecibo, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            Dictionary<string, ReciboMontoSql> datosSql =
                _sql.ObtenerDatosConciliacion(empresa, ids);

            var sinCambio = new List<string>();

            foreach (var op in revisar)
            {
                try
                {
                    pagosPorRecibo.TryGetValue(op.IdRecibo, out var pagosRecibo);
                    pagosRecibo = pagosRecibo ?? new List<SapPagoDetalle>();

                    // Bitácora: registrar TODO lo visto en SAP (activos y anulados)
                    _sql.UpsertSapDocs(op.IdRecibo, empresa, pagosRecibo);

                    var activos = pagosRecibo.Where(p => !p.Canceled).ToList();

                    // ── CASO 1: anulación TOTAL → PENDIENTE (regla de negocio) ──
                    if (activos.Count == 0)
                    {
                        string obsAnul = string.Format(
                            "Anulado en SAP (sin cobro activo). Era DocNum {0}/DocEntry {1}. " +
                            "Regresado a PENDIENTE {2:dd/MM/yyyy HH:mm}.",
                            op.SapDocNum, op.SapDocEntry, DateTime.Now);

                        _sql.LimpiarMarcasDetalle(op.IdRecibo, empresa);
                        _sql.RegresarReciboAPendiente(op.IdRecibo, empresa, obsAnul);
                        res.Anulados++;
                        continue;
                    }

                    // ── Reapuntar SAP_DOCENTRY al pago activo más reciente ──
                    var vigente = activos.OrderByDescending(p => p.DocEntry).First();
                    if (vigente.DocEntry != op.SapDocEntry)
                    {
                        var sap = new SapCobroAplicado
                        {
                            IdRecibo = op.IdRecibo,
                            SapDocEntry = vigente.DocEntry,
                            SapDocNum = vigente.DocNum,
                            FechaPago = vigente.FechaPago
                        };
                        string obsRe = string.Format(
                            "Re-apuntado en SAP: DocEntry {0}->{1}, DocNum {2}->{3} ({4:dd/MM/yyyy HH:mm}).",
                            op.SapDocEntry, vigente.DocEntry,
                            op.SapDocNum, vigente.DocNum, DateTime.Now);
                        _sql.ActualizarReferenciasSap(sap, empresa, obsRe);
                        res.Reapuntados++;
                    }

                    // ── CASO 2/3: conciliar sumando TODOS los pagos activos ──
                    bool quedoOperadoCuadrado = ConciliarRecibo(
                        op, empresa, activos, pagosRecibo, datosSql, res);

                    if (quedoOperadoCuadrado) sinCambio.Add(op.IdRecibo);
                }
                catch (Exception ex)
                {
                    res.Errores.Add(string.Format("[{0}] inversa {1}: {2}",
                        empresa, op.IdRecibo, ex.Message));
                }
            }

            _sql.MarcarUltimoCheckLote(sinCambio, empresa, "OPERADO");
        }

        // ── Conciliación de un recibo (multi-ORCT) ─────────────────────────
        // Devuelve true si el recibo quedó OPERADO cuadrado sin transición
        // (para el lote de "último check"); false en cualquier otro caso.
        private bool ConciliarRecibo(ReciboRevisionSql op, string empresa,
                                     List<SapPagoDetalle> activos,
                                     List<SapPagoDetalle> todos,
                                     Dictionary<string, ReciboMontoSql> datosSql,
                                     ResultadoSync res)
        {
            res.Conciliados++;

            if (!datosSql.TryGetValue(op.IdRecibo, out var sql)) return false; // sin datos, no concilio

            bool esUSD = string.Equals((sql.Moneda ?? "").Trim(), "USD",
                                       StringComparison.OrdinalIgnoreCase);
            bool eraDescuadre = string.Equals(op.SyncEstado, "DESCUADRE",
                                              StringComparison.OrdinalIgnoreCase);

            // Suma de TODOS los pagos activos (RCT2; anticipos por total ORCT)
            decimal montoSap = activos.Sum(p => p.MontoEfectivo(esUSD));
            decimal montoSql = sql.MontoTDoc;
            decimal diferencia = Math.Abs(montoSql - montoSap);

            // ── Cuadra ──
            if (diferencia <= Tolerancia)
            {
                if (eraDescuadre)
                {
                    // Self-healing: Créditos ya re-aplicó → DESCUADRE -> OPERADO
                    _sql.LimpiarMarcasDetalle(op.IdRecibo, empresa);
                    _sql.MarcarReciboCuadrado(op.IdRecibo, empresa, string.Format(
                        "Descuadre resuelto: SQL={0:N2} = SAP={1:N2} ({2} pago(s) activo(s)) {3:dd/MM/yyyy HH:mm}.",
                        montoSql, montoSap, activos.Count, DateTime.Now));
                    res.Sanados++;
                    return false;
                }

                // Ya estaba OPERADO y cuadra: limpiar bandera previa si había
                _sql.MarcarConciliacion(op.IdRecibo, empresa, null);
                return true;
            }

            // ── NO cuadra: anulación parcial (u otra causa) → DESCUADRE ──
            var facturasActivas = activos
                .SelectMany(p => p.FacturasAplicadas)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var anulados = todos.Where(p => p.Canceled)
                                .Select(p => p.DocNum.ToString())
                                .ToList();

            string obs = string.Format(
                "[DESC] Descuadre ({0}): SQL={1:N2} vs SAP activo={2:N2}, dif={3:N2}. " +
                "Pago(s) anulado(s) en SAP: {4}. Recibido sin aplicar: {3:N2}. {5:dd/MM/yyyy HH:mm}.",
                esUSD ? "USD" : "GTQ", montoSql, montoSap, diferencia,
                anulados.Count == 0 ? "ninguno detectado" : "DocNum " + string.Join(", ", anulados),
                DateTime.Now);

            _sql.MarcarLineasAnuladas(op.IdRecibo, empresa, facturasActivas);
            _sql.MarcarReciboDescuadre(op.IdRecibo, empresa, obs);

            if (!eraDescuadre) res.Descuadrados++;   // solo contar la transición nueva
            return false;
        }
    }
}