using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DiamDev.Give.Entities
{
    /// <summary>
    /// Resultado de interpretar SYNC_OBSERVACION en lenguaje de negocio.
    /// En TS sería una interface: { titular: string; detalle: string; ... }
    /// </summary>
    public class ObservacionSyncInfo
    {
        /// <summary>Resumen de una línea. Vacío si no se reconoció el patrón.</summary>
        public string Titular { get; set; }

        /// <summary>Explicación en prosa, sin jerga de sistema.</summary>
        public string Detalle { get; set; }

        /// <summary>Qué hacer. SOLO para pantalla: es instrucción interna, no va al papel.</summary>
        public string Accion { get; set; }

        /// <summary>Cuándo lo detectó el sincronizador, ya redactado.</summary>
        public string Fecha { get; set; }

        /// <summary>El texto TAL CUAL está en la BD. Nunca se modifica: es el respaldo auditable.</summary>
        public string Original { get; set; }

        /// <summary>false = no se pudo parsear y Detalle trae el original. Nunca ocultamos información.</summary>
        public bool Reconocido { get; set; }

        /// <summary>"alerta" | "aviso" | "ok" | "info" — para elegir color en la UI.</summary>
        public string Severidad { get; set; }

        /// <summary>
        /// Todo junto en texto plano, listo para un atributo title="...".
        /// OJO: sin comillas dobles a propósito — una comilla interna cortaría
        /// el atributo HTML a la mitad (ya nos pasó en el Dashboard).
        /// </summary>
        public string TextoPlano
        {
            get
            {
                string t = "";
                if (!string.IsNullOrWhiteSpace(Titular)) t += Titular + "\n\n";
                if (!string.IsNullOrWhiteSpace(Detalle)) t += Detalle;
                if (!string.IsNullOrWhiteSpace(Accion)) t += "\n\n➤ " + Accion;
                if (!string.IsNullOrWhiteSpace(Fecha)) t += "\n\n" + Fecha;
                return t.Trim().Replace("\"", "'");
            }
        }
    }

    /// <summary>
    /// ═══════════════════════════════════════════════════════════════
    /// INTÉRPRETE DE SYNC_OBSERVACION
    /// ═══════════════════════════════════════════════════════════════
    /// El Sincronizador graba mensajes pensados para diagnóstico técnico:
    ///
    ///   [DESC] Descuadre (GTQ): SQL=6,494.50 vs SAP activo=6,484.50,
    ///   dif=10.00. Pago(s) anulado(s) en SAP: ninguno detectado.
    ///   Recibido sin aplicar: 10.00. 29/07/2026 14:24.
    ///
    /// Créditos y los agentes de ventas no leen eso. Esta clase lo traduce
    /// SIN TOCAR LA BD: el original se conserva intacto en SYNC_OBSERVACION
    /// (es el respaldo auditable) y solo se reescribe al mostrarlo.
    ///
    /// Regla de oro: si no reconoce el patrón, devuelve el texto original.
    /// Un intérprete que oculta lo que no entiende es peor que no tener
    /// intérprete — Créditos se quedaría sin el dato para investigar.
    /// ═══════════════════════════════════════════════════════════════
    /// </summary>
    public static class ObservacionSync
    {
        // ── Helpers de extracción ────────────────────────────────────

        private static string Cap(string texto, string patron)
        {
            if (string.IsNullOrWhiteSpace(texto)) return "";
            var m = Regex.Match(texto, patron, RegexOptions.IgnoreCase);
            return (m.Success && m.Groups.Count > 1) ? m.Groups[1].Value.Trim() : "";
        }

        /// <summary>
        /// Convierte "6,494.50" a 6494.50m.
        ///
        /// Defensivo a propósito: el Sincronizador formatea con ToString("N2"),
        /// que depende de la cultura del SERVIDOR. En es-GT / en-US da
        /// "6,494.50"; si algún día ese servicio corre bajo es-ES daría
        /// "6.494,50". Probamos ambas interpretaciones antes de rendirnos.
        /// </summary>
        private static decimal Monto(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0m;
            decimal v;

            // Formato Guatemala/US: coma = miles, punto = decimal
            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out v))
                return v;

            // Formato europeo: punto = miles, coma = decimal
            if (decimal.TryParse(s, NumberStyles.Number,
                                 CultureInfo.GetCultureInfo("es-ES"), out v))
                return v;

            return 0m;
        }

        /// <summary>Formatea un monto con su símbolo: "Q 6,494.50" / "$ 131.21".</summary>
        private static string Fmt(decimal v, string moneda)
        {
            string simbolo = "USD".Equals(moneda, StringComparison.OrdinalIgnoreCase) ? "$" : "Q";
            return simbolo + " " + v.ToString("N2");
        }

        /// <summary>Quita el prefijo técnico [DESC] / [ERR] / [OK] del inicio.</summary>
        private static string SinPrefijo(string obs)
        {
            return Regex.Replace((obs ?? "").Trim(), @"^\[[A-Z_]+\]\s*", "");
        }

        /// <summary>
        /// Extrae la marca de tiempo del final ("29/07/2026 14:24.") y la
        /// redacta. Devuelve "" si el mensaje no la trae.
        /// </summary>
        private static string FechaRevision(string obs)
        {
            var m = Regex.Match(obs ?? "",
                @"(\d{1,2}/\d{1,2}/\d{4})\s+(\d{1,2}:\d{2})(?::\d{2})?\s*\.?\s*$");
            if (!m.Success) return "";
            return "Revisión del sistema: " + m.Groups[1].Value +
                   " a las " + m.Groups[2].Value;
        }

        // ── API pública ──────────────────────────────────────────────

        /// <summary>
        /// Interpreta la observación. 'syncEstado' es opcional y solo ayuda a
        /// desambiguar cuando el texto viene vacío o sin prefijo reconocible.
        /// </summary>
        public static ObservacionSyncInfo Interpretar(string obs, string syncEstado = null)
        {
            string original = (obs ?? "").Trim();
            string estado = (syncEstado ?? "").Trim().ToUpperInvariant();

            var info = new ObservacionSyncInfo
            {
                Original = original,
                Reconocido = false,
                Severidad = "info",
                Titular = "",
                Detalle = "",
                Accion = "",
                Fecha = FechaRevision(original)
            };

            // ── Sin texto: nos guiamos por el estado ──
            if (original.Length == 0)
            {
                switch (estado)
                {
                    case "PENDIENTE":
                        info.Detalle = "El recibo está en cola para aplicarse en SAP.";
                        info.Severidad = "info";
                        info.Reconocido = true;
                        break;
                    case "OPERADO":
                        info.Detalle = "El pago fue aplicado correctamente en SAP.";
                        info.Severidad = "ok";
                        info.Reconocido = true;
                        break;
                    case "HISTORICO":
                        info.Detalle = "Recibo migrado del sistema anterior. No se sincroniza con SAP.";
                        info.Severidad = "info";
                        info.Reconocido = true;
                        break;
                    default:
                        info.Detalle = "";
                        break;
                }
                return info;
            }

            // ═══════════════════════════════════════════════════════
            // RAMA 1 — DESCUADRE (el caso que más se consulta)
            // ═══════════════════════════════════════════════════════
            bool esDescuadre = estado == "DESCUADRE" ||
                               Regex.IsMatch(original, @"descuadre", RegexOptions.IgnoreCase);

            if (esDescuadre && Regex.IsMatch(original, @"SQL\s*=", RegexOptions.IgnoreCase))
            {
                string moneda = Cap(original, @"descuadre\s*\((USD|GTQ)\)");
                if (moneda.Length == 0) moneda = "GTQ";

                decimal enRecibo = Monto(Cap(original, @"SQL\s*=\s*([\d.,]+)"));
                decimal enSap = Monto(Cap(original, @"SAP\s+activo\s*=\s*([\d.,]+)"));
                decimal diferencia = Monto(Cap(original, @"dif\s*=\s*([\d.,]+)"));
                decimal sinAplicar = Monto(Cap(original, @"[Rr]ecibido\s+sin\s+aplicar:\s*([\d.,]+)"));

                string anulados = Cap(original,
                    @"[Pp]ago\(s\)\s+anulado\(s\)\s+en\s+SAP:\s*([^.]+)\.");
                // "ninguno detectado" / "ninguno" / vacío = NO hubo anulaciones
                bool hayAnulados = anulados.Length > 0 &&
                                   !Regex.IsMatch(anulados, @"ninguno|ning[uú]n|no\s+detect",
                                                  RegexOptions.IgnoreCase);
                // "DocNum 12345" se lee mejor como "No. 12345"
                string docsAnulados = hayAnulados
                    ? Regex.Replace(anulados, @"DocNum", "No.", RegexOptions.IgnoreCase)
                    : "";

                info.Reconocido = true;
                info.Severidad = "alerta";

                info.Titular = diferencia > 0m
                    ? "Faltan " + Fmt(diferencia, moneda) + " por aplicar en SAP"
                    : "El monto del recibo no coincide con lo aplicado en SAP";

                // Base común: la comparación, en palabras.
                string detalle =
                    "El recibo es por " + Fmt(enRecibo, moneda) + " y en SAP hay " +
                    Fmt(enSap, moneda) + " aplicados a documentos.";

                // ★ Las dos causas raíz se redactan distinto porque la acción
                // que exigen es distinta. Este es el valor real del intérprete:
                // no solo traduce, DIAGNOSTICA.
                if (hayAnulados)
                {
                    detalle += " Se anuló en SAP el pago " + docsAnulados +
                               ", y ese monto todavía no se ha vuelto a aplicar.";
                    info.Accion = "Créditos debe volver a aplicar " +
                                  Fmt(diferencia, moneda) +
                                  " en SAP usando el mismo número de recibo.";
                }
                else
                {
                    detalle += " La diferencia de " + Fmt(diferencia, moneda) +
                               " se recibió en caja pero todavía no se aplicó a ninguna " +
                               "factura. No se detectaron pagos anulados en SAP.";
                    info.Accion = "Revisar si ese monto corresponde a un anticipo o a una " +
                                  "diferencia, y aplicarlo en SAP al documento que corresponda.";
                }

                // Solo lo mencionamos si aporta un dato nuevo (≠ diferencia).
                if (sinAplicar > 0m && sinAplicar != diferencia)
                    detalle += " Monto recibido sin aplicar: " + Fmt(sinAplicar, moneda) + ".";

                info.Detalle = detalle;
                return info;
            }

            // ═══════════════════════════════════════════════════════
            // RAMA 2 — ERROR DE SINCRONIZACIÓN
            // ═══════════════════════════════════════════════════════
            if (Regex.IsMatch(original, @"^\[ERR", RegexOptions.IgnoreCase) ||
                Regex.IsMatch(original, @"\berror\b|excepci[oó]n", RegexOptions.IgnoreCase))
            {
                info.Reconocido = true;
                info.Severidad = "alerta";
                info.Titular = "El recibo no pudo aplicarse en SAP";
                info.Detalle = "El sistema intentó aplicar el pago y SAP lo rechazó. " +
                               "El dinero está registrado en caja, pero el pago no existe " +
                               "todavía en SAP. Detalle técnico: " + SinPrefijo(original);
                info.Accion = "Avisar a Sistemas con el número de recibo.";
                return info;
            }

            // ═══════════════════════════════════════════════════════
            // RAMA 3 — OPERADO CORRECTAMENTE
            // ═══════════════════════════════════════════════════════
            if (estado == "OPERADO" ||
                Regex.IsMatch(original, @"^\[OK", RegexOptions.IgnoreCase))
            {
                info.Reconocido = true;
                info.Severidad = "ok";
                info.Titular = "Pago aplicado en SAP";
                info.Detalle = "El monto del recibo coincide con lo aplicado en SAP. " +
                               "No requiere ninguna acción.";
                return info;
            }

            // ═══════════════════════════════════════════════════════
            // FALLBACK — patrón desconocido: mostramos el original
            // ═══════════════════════════════════════════════════════
            // Le quitamos solo el prefijo técnico. Preferimos un texto crudo
            // a un texto ausente: si el Sincronizador cambia un mensaje, la
            // información sigue llegando a Créditos aunque sin traducir.
            info.Detalle = SinPrefijo(original);
            info.Severidad = estado == "DESCUADRE" ? "alerta" : "info";
            return info;
        }
    }
}