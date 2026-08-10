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

        // Prefijo de las notas de conciliación. DEBE coincidir EXACTAMENTE con el
        // que antepone ReciboCajaSyncDA.MarcarConciliacion. Si se desincronizan,
        // la comparación "¿cambió la observación?" nunca da igual y volvemos a
        // un UPDATE por ciclo por recibo.
        private const string PREFIJO_CONCIL = "[CONCIL] ";

        // REC_CAJA_ENC.SYNC_OBSERVACION es nvarchar(200). Un mensaje más largo hace
        // que SQL Server lance "String or binary data would be truncated", el catch
        // por recibo se lo traga, y ESE recibo no se marca. En silencio.
        private const int MAX_OBSERVACION = 200;

        /// <summary>Recorta a 'max' caracteres para que el UPDATE nunca truene.</summary>
        private static string RecortarA(string texto, int max)
        {
            if (string.IsNullOrEmpty(texto) || texto.Length <= max) return texto;
            return texto.Substring(0, max - 3) + "...";
        }

        /// <summary>Recorta a MAX_OBSERVACION para que el UPDATE nunca truene.</summary>
        private static string Recortar(string texto)
        {
            if (string.IsNullOrEmpty(texto) || texto.Length <= MAX_OBSERVACION) return texto;
            return texto.Substring(0, MAX_OBSERVACION - 3) + "...";
        }

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
            //
            // ★ Desde el fix de la cola rotativa, esto ya NO trae todo el
            // histórico: trae un lote acotado (App.config -> SyncLoteRevision),
            // con los DESCUADRE siempre al frente.
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

            // ★ Bitácora de TODO lo visto en SAP, en UNA conexión para todo el
            // lote. Antes se llamaba UpsertSapDocs dentro del foreach: una
            // SqlConnection nueva por recibo (~845 aperturas por ciclo).
            // Los errores se acumulan igual que antes, solo que devueltos.
            res.Errores.AddRange(_sql.UpsertSapDocsLote(empresa, pagosPorRecibo));

            foreach (var op in revisar)
            {
                try
                {
                    pagosPorRecibo.TryGetValue(op.IdRecibo, out var pagosRecibo);
                    pagosRecibo = pagosRecibo ?? new List<SapPagoDetalle>();

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
                    ConciliarRecibo(op, empresa, activos, pagosRecibo, datosSql, res);
                }
                catch (Exception ex)
                {
                    res.Errores.Add(string.Format("[{0}] inversa {1}: {2}",
                        empresa, op.IdRecibo, ex.Message));
                }
            }

            // ★ FIX (inanición): se sella el LOTE COMPLETO, no solo los que
            // cuadraron sin cambio.
            //
            // Antes se acumulaba una lista 'sinCambio' y solo esos se sellaban.
            // Sin TOP eso era inofensivo. CON la cola rotativa es un bug: un
            // recibo que sale por 'continue' (anulación total), por un return
            // temprano de ConciliarRecibo (sin datos en datosSql), o por
            // excepción, nunca sellaría SYNC_ULTIMO_CHECK — y al ordenar la
            // cola por esa columna quedaría clavado AL FRENTE para siempre,
            // bloqueando a los que vienen atrás. Head-of-line blocking clásico.
            //
            // Pasar 'ids' completo es seguro: MarcarUltimoCheckLote filtra por
            // SYNC_ESTADO = 'OPERADO', así que los que transicionaron en esta
            // vuelta (a PENDIENTE por anulación, o a DESCUADRE) quedan fuera —
            // y esos ya sellaron su propio SYNC_ULTIMO_CHECK en su UPDATE.
            _sql.MarcarUltimoCheckLote(ids, empresa, "OPERADO");
        }

        // ── Conciliación de un recibo (multi-ORCT) ─────────────────────────
        //
        // DOS NIVELES:
        //
        //   NIVEL 1 — DINERO (única autoridad para OPERADO/DESCUADRE)
        //     SUM(ORCT.DocTotal) de pagos activos  vs  REC_CAJA_ENC.MONTO_T_REC
        //     Ambos significan "cuánto dinero entró".
        //
        //   NIVEL 2 — APLICACIÓN (informativo, NO decide estado)
        //     DocTotal - SUM(RCT2) = lo que quedó A CUENTA del cliente.
        //     En SAP eso NO es un error: es un saldo a favor que se concilia
        //     después (conciliación interna: OITR/ITR1 + JDT1.BalDueCred).
        //
        // ⚠ EL NIVEL 2 SOLO SE EVALÚA SI SAP REPORTÓ LÍNEAS RCT2.
        // Sin ellas hay DOS escenarios que desde aquí son indistinguibles:
        //     a) anticipo puro          -> no hay nada que conciliar
        //     b) la consulta RCT2 falló -> "aplicado 0.00" sería MENTIRA
        // En ambos casos no se escribe nada. El estado ya lo resolvió el
        // Nivel 1, que NO depende de RCT2.
        //
        // ⚠ La nota [CONCIL] NO lleva fecha A PROPÓSITO. Si la llevara, el
        // texto cambiaría cada minuto y la comparación de abajo nunca daría
        // igual: un UPDATE por recibo por ciclo, para siempre.
        // SYNC_ULTIMO_CHECK ya guarda cuándo se verificó, que es su trabajo.
        //
        // Devuelve true si el recibo quedó OPERADO cuadrado y SIN escribir
        // nada (va al lote de "último check"); false en cualquier otro caso.
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
            string codMon = esUSD ? "USD" : "GTQ";
            string sim = esUSD ? "$" : "Q";

            // ══ NIVEL 1 — DINERO ══
            decimal recibidoSap = activos.Sum(p => p.MontoRecibido(esUSD));
            decimal recibidoSql = sql.MontoTRec;
            decimal diferencia = Math.Abs(recibidoSql - recibidoSap);

            // ══ NIVEL 2 — APLICACIÓN (solo si HAY datos de RCT2) ══
            bool hayDatosRct2 = activos.Any(p => p.TieneLineasRct2);
            decimal aCuenta = activos.Sum(p => p.MontoACuenta(esUSD));
            decimal aplicado = activos.Sum(p => p.MontoAplicado(esUSD));

            // ── CUADRA: el dinero está registrado en SAP ──
            if (diferencia <= Tolerancia)
            {
                // ══════════════════════════════════════════════════════════
                // NIVEL 2 DESACTIVADO PERMANENTEMENTE (validado 2026-08-10)
                // ══════════════════════════════════════════════════════════
                // El cálculo DocTotal - RCT2 NO puede medir "monto a cuenta"
                // porque en SAP hay DOS rutas de aplicación y RCT2 solo ve una:
                //
                //   Ruta A) Aplicación al crear el pago  -> RCT2 SÍ se llena
                //   Ruta B) Reconciliación interna       -> RCT2 queda VACÍO
                //           (OITR/ITR1; se refleja en JDT1.BalDueCred)
                //
                // Créditos usa la ruta B cuando la factura es posterior al pago
                // y siempre para recibos contra PEDIDO (cuenta Anticipos Cliente
                // #21202001 local / #21202002 expo). Un PEDIDO además nunca
                // aparece en RCT2: el filtro InvType=13 solo trae facturas OINV.
                //
                // Evidencia: RB10-01089 (FACTURA 1007003) y RB01-00669
                // (PEDIDO 8000762) tenían RCT2 vacío, pero JDT1.BalDueCred = 0
                // y OINV.DocStatus = 'C' -> el pago SÍ estaba aplicado.
                // La nota "a cuenta" era falsa.
                //
                // No se reimplementa con JDT1: Créditos reconcilia DIARIO, así
                // que el saldo a cuenta vive horas. La nota se escribiría y se
                // borraría sola cada día, con un viaje extra a HANA por ciclo
                // para ~955 recibos, y sin nada accionable para nadie.
                //
                // El NIVEL 1 (SUM(ORCT.DocTotal) vs MONTO_T_REC) NO se ve
                // afectado: sigue siendo la autoridad de OPERADO/DESCUADRE.
                //
                // ⚠ NOTA: IntrnMatch de JDT1 NO sirve como indicador. Desde
                // SAP B1 8.8 la reconciliación vive en OITR/ITR1 y ese campo
                // quedó legacy (queda en 0 aunque el saldo esté conciliado).
                // El campo confiable es BalDueCred.
                const bool NIVEL2_ACTIVO = false;

                string nota = (NIVEL2_ACTIVO && hayDatosRct2 && aCuenta > Tolerancia)
                    ? RecortarA(string.Format(
                        "Conciliación ({0}): a cuenta {1} {2:N2} de {1} {3:N2} recibidos " +
                        "(aplicado {1} {4:N2}).",
                        codMon, sim, aCuenta, recibidoSap, aplicado),
                        MAX_OBSERVACION - PREFIJO_CONCIL.Length)
                    : null;

                if (eraDescuadre)
                {
                    // Self-healing: DESCUADRE -> OPERADO. Transición de estado:
                    // se escribe UNA vez, por eso aquí sí puede llevar fecha.
                    string obsSano = (nota != null)
                        ? PREFIJO_CONCIL + nota
                        : RecortarA(string.Format(
                            "Descuadre resuelto: {0} {1:N2} = SAP ({2} pago(s) activo(s)). " +
                            "{3:dd/MM/yyyy HH:mm}.",
                            sim, recibidoSql, activos.Count, DateTime.Now), MAX_OBSERVACION);

                    _sql.LimpiarMarcasDetalle(op.IdRecibo, empresa);
                    _sql.MarcarReciboCuadrado(op.IdRecibo, empresa, obsSano);
                    res.Sanados++;
                    return false;
                }

                // Ya estaba OPERADO: escribir SOLO si la observación cambia.
                bool tieneMarcaConcil = (op.SyncObservacion ?? "")
                    .StartsWith(PREFIJO_CONCIL, StringComparison.OrdinalIgnoreCase);

                if (nota == null)
                {
                    // Sin datos de RCT2 no afirmamos NADA sobre el nivel 2,
                    // ni siquiera para limpiar: si el vacío viene de una
                    // consulta fallida, limpiar y re-escribir en el siguiente
                    // ciclo produce el parpadeo que estamos matando.
                    if (!hayDatosRct2) return true;

                    // Con datos y sin saldo a cuenta: el recibo se aplicó
                    // completo. Solo limpiamos si la marca es NUESTRA
                    // (una observación de re-apunte se respeta).
                    if (!tieneMarcaConcil) return true;
                    _sql.MarcarConciliacion(op.IdRecibo, empresa, null);
                    return false;
                }

                if (string.Equals(op.SyncObservacion ?? "", PREFIJO_CONCIL + nota,
                                  StringComparison.Ordinal))
                    return true;   // idéntica -> lote de último check, sin escribir

                _sql.MarcarConciliacion(op.IdRecibo, empresa, nota);
                return false;
            }

            // ── NO CUADRA: falta dinero en SAP -> DESCUADRE real ──
            // Formato conservado (incluye "dif=") para no romper los intérpretes.
            var facturasActivas = activos
                .SelectMany(p => p.FacturasAplicadas)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var anulados = todos.Where(p => p.Canceled)
                                .Select(p => p.DocNum.ToString())
                                .ToList();

            string obs = RecortarA(string.Format(
                "[DESC] Descuadre ({0}): SQL={1:N2} vs SAP activo={2:N2}, dif={3:N2}. " +
                "Pago(s) anulado(s) en SAP: {4}. {5:dd/MM/yyyy HH:mm}.",
                codMon, recibidoSql, recibidoSap, diferencia,
                anulados.Count == 0 ? "ninguno detectado" : "DocNum " + string.Join(", ", anulados),
                DateTime.Now), MAX_OBSERVACION);

            _sql.MarcarLineasAnuladas(op.IdRecibo, empresa, facturasActivas);
            _sql.MarcarReciboDescuadre(op.IdRecibo, empresa, obs);

            if (!eraDescuadre) res.Descuadrados++;
            return false;
        }
    }
}