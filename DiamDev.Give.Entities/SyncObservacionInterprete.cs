using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace DiamDev.Give.Entities
{
    /// <summary>
    /// Resultado de traducir un SYNC_OBSERVACION técnico a lenguaje humano.
    /// Es un POCO de PRESENTACIÓN: no se guarda en la BD, se arma al vuelo.
    /// (En TS sería: interface SyncObservacionLegible { ... })
    /// </summary>
    public class SyncObservacionLegible
    {
        /// <summary>true = se reconoció el formato y hay traducción confiable.
        /// false = mostrar el texto original (fallback), NUNCA inventar.</summary>
        public bool Interpretado { get; set; }

        /// <summary>Etiqueta técnica detectada: DESC, CONCIL, etc.</summary>
        public string Etiqueta { get; set; }

        /// <summary>Titular en una línea. Ej: "Faltan Q 10.00 por aplicar en SAP."</summary>
        public string Titulo { get; set; }

        /// <summary>Desglose, una idea por renglón.</summary>
        public List<string> Lineas { get; set; }

        /// <summary>Qué debe hacer Créditos. Uso INTERNO (tooltip), no va al papel
        /// que recibe el cliente.</summary>
        public string Accion { get; set; }

        /// <summary>Fecha/hora de la última revisión del sincronizador, tal como venía.</summary>
        public string FechaRevision { get; set; }

        /// <summary>El texto crudo de la BD, intacto. Para soporte y auditoría.</summary>
        public string Original { get; set; }

        public SyncObservacionLegible()
        {
            Interpretado = false;
            Etiqueta = "";
            Titulo = "";
            Lineas = new List<string>();
            Accion = "";
            FechaRevision = "";
            Original = "";
        }

        /// <summary>
        /// Versión de una sola cadena, con saltos de línea reales. Sirve para
        /// tooltips del atributo title (que NO acepta HTML, pero sí "\n").
        /// </summary>
        public string TextoPlano
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                if (!string.IsNullOrEmpty(Titulo)) sb.AppendLine(Titulo);
                for (int i = 0; i < Lineas.Count; i++) sb.AppendLine("• " + Lineas[i]);
                if (!string.IsNullOrEmpty(Accion)) { sb.AppendLine(); sb.AppendLine(Accion); }
                return sb.ToString().TrimEnd();
            }
        }
    }

    /// <summary>
    /// Traductor de SYNC_OBSERVACION (log del Sincronizador) a lenguaje que
    /// entienda Créditos y un agente de ventas.
    ///
    /// REGLA DE ORO: esta clase SOLO LEE. El texto de la BD no se modifica
    /// nunca. Si el formato del mensaje cambia y ya no se reconoce,
    /// Interpretado = false y la vista cae al texto original: se degrada,
    /// no se rompe ni miente.
    ///
    /// Formato de entrada esperado (ejemplo real):
    ///   [DESC] Descuadre (GTQ): SQL=6,494.50 vs SAP activo=6,484.50, dif=10.00.
    ///   Pago(s) anulado(s) en SAP: ninguno detectado. Recibido sin aplicar: 10.00.
    ///   29/07/2026 14:24.
    /// </summary>
    public static class SyncObservacionInterprete
    {
        // Regex estáticos y compilados: se construyen UNA vez por AppDomain.
        // (Crear un Regex en cada llamada dentro de un foreach de impresión de
        // lote sería un desperdicio medible.)
        private const RegexOptions OPC =
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled;

        private static readonly Regex RxEtiqueta = new Regex(@"^\s*\[(?<v>[A-Z_]+)\]", OPC);
        private static readonly Regex RxMoneda = new Regex(@"descuadre\s*\(\s*(?<v>[A-Z]{3})\s*\)", OPC);
        private static readonly Regex RxSql = new Regex(@"\bSQL\s*=\s*(?<v>-?[\d.,]+)", OPC);
        private static readonly Regex RxSap = new Regex(@"\bSAP\s*(?:activo)?\s*=\s*(?<v>-?[\d.,]+)", OPC);
        private static readonly Regex RxDif = new Regex(@"\bdif(?:erencia)?\s*=\s*(?<v>-?[\d.,]+)", OPC);
        private static readonly Regex RxAnulados = new Regex(@"anulado\(s\)\s*en\s*SAP\s*:\s*(?<v>[^.]*)", OPC);
        private static readonly Regex RxSinAplicar = new Regex(@"sin\s*aplicar\s*:\s*(?<v>-?[\d.,]+)", OPC);
        private static readonly Regex RxFecha = new Regex(@"(?<v>\d{1,2}/\d{1,2}/\d{4}\s+\d{1,2}:\d{2})", OPC);
        private static readonly Regex RxAnuladoEnSap = new Regex(@"anulado\s+en\s+SAP", OPC);
        // ── Variantes confirmadas contra datos de producción (2026-07-29) ──
        // Los DocNum/DocEntry se capturan como STRING, no como decimal: son
        // identificadores, no dinero. Formatearlos con "N2" daría "1,019,443.00".
        private static readonly Regex RxReapuntado = new Regex(@"re-?apuntado\s+en\s+SAP", OPC);
        private static readonly Regex RxDocNumCambio = new Regex(@"DocNum\s*(?<a>\d+)\s*-\s*>\s*(?<b>\d+)", OPC);
        private static readonly Regex RxDocEntryCambio = new Regex(@"DocEntry\s*(?<a>\d+)\s*-\s*>\s*(?<b>\d+)", OPC);
        private static readonly Regex RxEraDocNum = new Regex(@"Era\s+DocNum\s*(?<v>\d+)", OPC);
        private static readonly Regex RxRegresadoPend = new Regex(@"Regresado\s+a\s+PENDIENTE", OPC);

        // Medio centavo: por debajo de eso no hay dinero, hay redondeo.
        // Mismo criterio que EPSILON en el front y que el BLL.
        private const decimal EPSILON = 0.005m;

        /// <summary>
        /// Residuo tolerable al derivar equivalentes entre monedas: 1 centavo.
        /// Espejo de ReciboCajaBLL.TOLERANCIA_CONVERSION. Una diferencia de este
        /// tamaño contra SAP es aritmética de redondeo, no dinero faltante.
        /// ⚠ PENDIENTE DE CONFIRMAR contra la tolerancia del propio Sincronizador.
        /// </summary>
        private const decimal TOLERANCIA_REDONDEO = 0.01m;

        /// <summary>
        /// Traduce el log técnico a lenguaje humano.
        ///
        /// El TIPO de mensaje se identifica por su CONTENIDO, de lo más
        /// específico a lo más genérico. El syncEstado es solo CONTEXTO
        /// (opcional): sirve para desambiguar, nunca para decidir el tipo.
        ///
        /// ★ Antes esta función asumía que SYNC_ESTADO=OPERADO implicaba
        /// "nota de conciliación". Falso: el mensaje OPERADO más común en
        /// producción es "Re-apuntado en SAP", que significa otra cosa.
        ///
        /// Variantes confirmadas en POS-SmartK66_DEV al 2026-07-29:
        ///   OPERADO   → "Re-apuntado en SAP: DocEntry a->b, DocNum a->b (fecha)."
        ///   DESCUADRE → "[DESC] Descuadre (GTQ): SQL=... vs SAP activo=..., dif=..."
        ///   PENDIENTE → "Anulado en SAP (sin cobro activo). Era DocNum n/DocEntry n..."
        /// </summary>
        public static SyncObservacionLegible Interpretar(string observacion, string syncEstado = null)
        {
            SyncObservacionLegible r = new SyncObservacionLegible();
            r.Original = (observacion ?? "").Trim();
            if (r.Original.Length == 0) return r;

            Match mTag = RxEtiqueta.Match(r.Original);
            r.Etiqueta = mTag.Success ? mTag.Groups["v"].Value.ToUpperInvariant() : "";

            string estado = (syncEstado ?? "").Trim().ToUpperInvariant();
            string simbolo = Simbolo(Grupo(RxMoneda, r.Original, "GTQ"));
            r.FechaRevision = Grupo(RxFecha, r.Original, "");

            // ══════════════════════════════════════════════════════════
            // CASO A — RE-APUNTADO: cambió el pago en SAP (estado OPERADO)
            // ══════════════════════════════════════════════════════════
            // Créditos anuló el pago y lo volvió a crear; el sincronizador
            // detectó el nuevo y actualizó el recibo. NO es un problema.
            if (RxReapuntado.IsMatch(r.Original))
            {
                Match dn = RxDocNumCambio.Match(r.Original);
                Match de = RxDocEntryCambio.Match(r.Original);

                r.Titulo = "Recibo operado en SAP. El número de pago cambió.";
                r.Lineas.Add("El pago original fue anulado en SAP y Créditos lo volvió a crear. "
                           + "El sistema ya apunta al pago vigente.");

                if (dn.Success)
                    r.Lineas.Add("Pago anterior: No. " + dn.Groups["a"].Value
                               + "  ·  Pago vigente: No. " + dn.Groups["b"].Value + ".");
                else if (de.Success)
                    r.Lineas.Add("Referencia interna de SAP: " + de.Groups["a"].Value
                               + " → " + de.Groups["b"].Value + ".");

                r.Accion = "No requiere acción: el recibo está correctamente aplicado. "
                         + "Si tiene una impresión anterior, el «No. Recibo SAP» que aparece "
                         + "ahí quedó obsoleto — vuelva a imprimir el recibo.";

                AgregarFecha(r, "Actualizado el");
                r.Interpretado = true;
                return r;
            }

            // ══════════════════════════════════════════════════════════
            // CASO B — PAGO ANULADO EN SAP, recibo devuelto a PENDIENTE
            // ══════════════════════════════════════════════════════════
            // OJO con el regex: "anulado en SAP" (con espacio) NO coincide con
            // "anulado(s) en SAP:" del mensaje de DESCUADRE, que lleva paréntesis
            // pegado. Por eso este caso no le roba el turno al CASO C.
            if (RxAnuladoEnSap.IsMatch(r.Original))
            {
                Match era = RxEraDocNum.Match(r.Original);

                r.Titulo = "El pago de este recibo fue anulado en SAP.";
                r.Lineas.Add("El dinero sí fue recibido en caja, pero el pago ya no existe "
                           + "en el sistema contable.");

                if (era.Success)
                    r.Lineas.Add("Pago anulado: No. " + era.Groups["v"].Value + ".");

                if (RxRegresadoPend.IsMatch(r.Original) || estado == "PENDIENTE")
                    r.Lineas.Add("El recibo regresó a PENDIENTE: espera que Créditos "
                               + "vuelva a aplicarlo.");

                r.Accion = "Créditos debe volver a aplicar el pago en SAP. "
                         + "El recibo conserva su validez como comprobante de pago.";

                AgregarFecha(r, "Detectado el");
                r.Interpretado = true;
                return r;
            }

            // ══════════════════════════════════════════════════════════
            // CASO C — NOTA DE CONCILIACIÓN [CONCIL]
            // ══════════════════════════════════════════════════════════
            // ⚠ Sin datos reales: 0 filas en producción al 2026-07-29.
            // PintarBadgeSync la contempla y el Sincronizador lleva un contador
            // "Conciliados", así que puede aparecer. Redacción DELIBERADAMENTE
            // genérica: no invento un desglose de un formato que nunca vi.
            if (r.Etiqueta == "CONCIL")
            {
                r.Titulo = "Recibo operado en SAP, con observación de conciliación.";
                r.Lineas.Add("El pago ya fue aplicado por Créditos en SAP.");
                r.Accion = "No requiere acción del agente. La nota queda como historial "
                         + "del recibo para control de Créditos.";
                AgregarFecha(r);
                r.Interpretado = true;
                return r;
            }

            // ══════════════════════════════════════════════════════════
            // CASO D — DESCUADRE CON MONTOS ([DESC])
            // ══════════════════════════════════════════════════════════
            decimal? recibo = Numero(RxSql, r.Original);        // lo que dice el recibo
            decimal? sap = Numero(RxSap, r.Original);           // lo aplicado en SAP
            decimal? dif = Numero(RxDif, r.Original);
            decimal? sinAplicar = Numero(RxSinAplicar, r.Original);
            string anulados = Grupo(RxAnulados, r.Original, "").Trim();

            // Si el mensaje no trae "dif=" pero sí los dos montos, la derivamos.
            // Nunca al revés: preferimos el dato explícito del sincronizador.
            if (!dif.HasValue && recibo.HasValue && sap.HasValue)
                dif = recibo.Value - sap.Value;

            if (recibo.HasValue || sap.HasValue || dif.HasValue)
            {
                decimal d = dif.HasValue ? dif.Value : 0m;
                decimal abs = Math.Abs(d);

                if (abs > EPSILON && abs <= TOLERANCIA_REDONDEO)
                {
                    // Un centavo no se le reclama a nadie: es aritmética.
                    r.Titulo = "Diferencia mínima de " + Mon(simbolo, abs)
                             + " frente a SAP (redondeo).";
                    r.Accion = "No hay dinero faltante: es residuo de conversión de moneda. "
                             + "El sincronizador reintenta automáticamente; si el recibo no "
                             + "se libera, avise a Sistemas.";
                }
                else if (d > EPSILON)
                {
                    r.Titulo = "Faltan " + Mon(simbolo, d) + " por aplicar en SAP.";
                    r.Accion = "Créditos debe aplicar la diferencia en SAP. El recibo se "
                             + "libera automáticamente cuando los montos vuelvan a cuadrar. "
                             + "Mientras esté en descuadre, el recibo NO puede anularse.";
                }
                else if (d < -EPSILON)
                {
                    r.Titulo = "En SAP hay " + Mon(simbolo, abs)
                             + " aplicados de más frente a este recibo.";
                    r.Accion = "Créditos debe revisar los pagos aplicados en SAP para este "
                             + "recibo. Mientras esté en descuadre, el recibo NO puede anularse.";
                }
                else
                {
                    r.Titulo = "El recibo está en revisión con SAP.";
                    r.Accion = "El sincronizador volverá a verificarlo automáticamente.";
                }

                if (recibo.HasValue)
                    r.Lineas.Add("Este recibo registra " + Mon(simbolo, recibo.Value)
                               + " recibidos en caja.");

                if (sap.HasValue)
                    r.Lineas.Add("En SAP hay " + Mon(simbolo, sap.Value)
                               + " aplicados a documentos del cliente.");

                if (dif.HasValue)
                    r.Lineas.Add("Diferencia: " + Mon(simbolo, abs)
                               + (d < -EPSILON ? " aplicados en exceso en SAP."
                                               : " sin aplicar en SAP."));

                if (anulados.Length > 0)
                {
                    r.Lineas.Add(EsNinguno(anulados)
                        ? "No se detectaron pagos anulados en SAP."
                        : "Pago(s) anulado(s) en SAP: " + HumanizarDocNums(anulados) + ".");
                }

                // "Recibido sin aplicar" solo se menciona si APORTA algo distinto
                // a la diferencia. En los datos reales SIEMPRE son iguales, así que
                // en la práctica esta línea nunca sale: repetir el mismo número con
                // otro nombre hace pensar que son dos problemas separados.
                if (sinAplicar.HasValue && Math.Abs(sinAplicar.Value - abs) > EPSILON)
                {
                    r.Lineas.Add("Monto recibido en caja que todavía no se aplica: "
                               + Mon(simbolo, sinAplicar.Value) + ".");
                }

                AgregarFecha(r);
                r.Interpretado = true;
                return r;
            }

            // ── CASO E: formato no reconocido → la vista muestra el original ──
            return r;
        }

        /// <summary>
        /// "DocNum 1019443" → "No. 1019443". Funciona con listas de cualquier
        /// separador, porque solo reemplaza la palabra técnica.
        /// "DocNum" no le dice nada a un agente de ventas; "No." sí.
        /// </summary>
        private static string HumanizarDocNums(string txt)
        {
            return Regex.Replace(txt ?? "", @"Doc(?:Num|Entry)\s*", "No. ",
                                 RegexOptions.IgnoreCase);
        }

        private static void AgregarFecha(SyncObservacionLegible r,
                                        string etiqueta = "Última revisión automática:")
        {
            if (string.IsNullOrEmpty(r.FechaRevision)) return;
            // "29/07/2026 14:24" → "29/07/2026 a las 14:24"
            r.Lineas.Add(etiqueta.TrimEnd(':') + ": "
                       + r.FechaRevision.Replace(" ", " a las ") + ".");
        }

        private static string Grupo(Regex rx, string txt, string porDefecto)
        {
            Match m = rx.Match(txt);
            return m.Success ? m.Groups["v"].Value : porDefecto;
        }

        private static decimal? Numero(Regex rx, string txt)
        {
            Match m = rx.Match(txt);
            if (!m.Success) return null;
            return ParsearDecimal(m.Groups["v"].Value);
        }

        /// <summary>
        /// Convierte "6,494.50" (o "6.494,50", o "10.00.") a decimal.
        /// El regex se come el punto final de la oración, así que hay que
        /// limpiar la cola antes de parsear.
        /// </summary>
        public static decimal? ParsearDecimal(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            s = s.Trim().TrimEnd('.', ',', ' ');

            // Heurística de separador decimal: si la última coma va DESPUÉS del
            // último punto, la coma es el decimal (formato europeo). Defensa por
            // si algún día el sincronizador corre con otra cultura del servidor.
            int ultPunto = s.LastIndexOf('.');
            int ultComa = s.LastIndexOf(',');
            s = (ultComa > ultPunto)
                ? s.Replace(".", "").Replace(',', '.')
                : s.Replace(",", "");

            decimal v;
            return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out v)
                 ? (decimal?)v
                 : null;
        }

        private static bool EsNinguno(string txt)
        {
            string t = (txt ?? "").ToLowerInvariant();
            return t.Length == 0
                || t.IndexOf("ninguno", StringComparison.Ordinal) >= 0
                || t.IndexOf("ninguna", StringComparison.Ordinal) >= 0
                || t.IndexOf("no se detect", StringComparison.Ordinal) >= 0;
        }

        private static string Simbolo(string moneda)
        {
            string m = (moneda ?? "").Trim().ToUpperInvariant();
            if (m == "USD") return "$";
            if (m == "GTQ" || m.Length == 0) return "Q";
            return m;
        }

        private static string Mon(string simbolo, decimal v)
        {
            // InvariantCulture con "N2" da "6,494.50": coma de miles, punto
            // decimal. Es el formato que ya usa toda la impresión.
            return simbolo + " " + v.ToString("N2", CultureInfo.InvariantCulture);
        }
    }
}