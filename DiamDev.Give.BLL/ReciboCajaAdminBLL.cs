using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using DiamDev.Give.DAL;
using DiamDev.Give.Entities;

namespace DiamDev.Give.BLL
{
    /// <summary>
    /// Lógica del Dashboard de Supervisión y del mantenimiento de series.
    /// Toda la validación de seguridad de NUMERACION vive AQUÍ (servidor),
    /// no en el JS: el front solo da feedback.
    /// </summary>
    public class ReciboCajaAdminBLL
    {
        private static readonly string[] EMPRESAS_VALIDAS = { "GRACO", "FAES", "BOLIK" };
        private const int DIAS_UMBRAL_FALLBACK = 3;

        private readonly ReciboCajaAdminDA _da = new ReciboCajaAdminDA();

        /// <summary>Umbral de "pendiente envejecido" (Web.config → DashboardDiasEnvejecido).</summary>
        public int DiasUmbral
        {
            get
            {
                string raw = ConfigurationManager.AppSettings["DashboardDiasEnvejecido"];
                return (int.TryParse(raw, out int v) && v > 0) ? v : DIAS_UMBRAL_FALLBACK;
            }
        }

        // ─── DASHBOARD ────────────────────────────────
        public DashboardResumenRecibos ObtenerResumen(string empresa) =>
            _da.ObtenerResumen(NormalizarEmpresa(empresa), DiasUmbral);

        public List<DashboardFilaRecibo> ObtenerDetalle(string empresa, string situacion,
            string fechaIni, string fechaFin, bool incluirOperados)
        {
            // Parseo defensivo de fechas: string vacío o basura → sin filtro
            DateTime? fIni = DateTime.TryParse(fechaIni, out DateTime fi) ? fi : (DateTime?)null;
            DateTime? fFin = DateTime.TryParse(fechaFin, out DateTime ff) ? ff : (DateTime?)null;

            // Rango invertido (desde > hasta): lo corregimos en silencio intercambiando
            if (fIni.HasValue && fFin.HasValue && fIni > fFin)
            {
                var tmp = fIni; fIni = fFin; fFin = tmp;
            }

            var filas = _da.ObtenerDetalle(NormalizarEmpresa(empresa), DiasUmbral,
                                           fIni, fFin, incluirOperados);

            var sit = (situacion ?? "").Trim().ToUpper();
            if (sit.Length == 0 || sit == "TODOS") return filas;
            return filas.Where(f => f.Situacion == sit).ToList();
        }

        // ─── SERIES ───────────────────────────────────
        public List<ReciboCajaSerie> ObtenerSeries() => _da.ObtenerSeries();

        /// <summary>
        /// Alta o edición con todas las reglas de protección del correlativo:
        ///  1. Empresa válida; DEPTO y SERIE obligatorios (normalizados a MAYÚSCULAS).
        ///  2. SERIE termina en '-' (convención de todas las existentes; se auto-agrega).
        ///  3. (EMPRESA, DEPTO) único y (EMPRESA, SERIE) único.
        ///  4. NUMERACION nunca por debajo del máximo correlativo YA emitido:
        ///     el próximo INSERT generaría un ID_RECIBO duplicado (choque de PK).
        ///  5. Al EDITAR una serie que cambia de prefijo, se re-valida contra el
        ///     máximo del prefijo NUEVO.
        /// </summary>
        public ResultadoRecibo GuardarSerie(ReciboCajaSerie s)
        {
            // 1. Normalización y obligatorios
            s.Empresa = (s.Empresa ?? "").Trim().ToUpper();
            s.Depto = (s.Depto ?? "").Trim().ToUpper();
            s.Serie = (s.Serie ?? "").Trim().ToUpper();
            s.SerieNc = (s.SerieNc ?? "").Trim().ToUpper();

            if (!EMPRESAS_VALIDAS.Contains(s.Empresa))
                return ResultadoRecibo.Error("Empresa inválida. Use GRACO, FAES o BOLIK.");
            if (s.Depto.Length == 0)
                return ResultadoRecibo.Error("El Depto/Responsable es obligatorio.");
            if (s.Serie.Length == 0)
                return ResultadoRecibo.Error("La Serie es obligatoria.");
            if (s.Numeracion < 0 || s.NumeracionNc < 0)
                return ResultadoRecibo.Error("La numeración no puede ser negativa.");

            // 2. Convención: la serie siempre termina en guion (RG12-, BB01-, ...)
            if (!s.Serie.EndsWith("-")) s.Serie += "-";
            if (s.SerieNc.Length > 0 && !s.SerieNc.EndsWith("-")) s.SerieNc += "-";

            // 3. Unicidad
            if (_da.ExisteEmpresaDepto(s.Empresa, s.Depto, s.RowId))
                return ResultadoRecibo.Error(
                    $"Ya existe una serie para {s.Empresa} / {s.Depto}.");
            if (_da.ExisteSerie(s.Empresa, s.Serie, s.RowId))
                return ResultadoRecibo.Error(
                    $"El prefijo de serie '{s.Serie}' ya está en uso en {s.Empresa}.");

            // 4/5. Protección del correlativo contra el histórico REAL
            int maxUsado = _da.ObtenerMaxUsado(s.Empresa, s.Serie);
            if (s.Numeracion < maxUsado)
                return ResultadoRecibo.Error(
                    $"Numeración inválida: ya existen recibos emitidos hasta " +
                    $"{s.Serie}{maxUsado:00000}. La numeración debe ser {maxUsado} o mayor " +
                    $"(el próximo recibo sería {s.Serie}{(maxUsado + 1):00000}).");

            if (s.RowId > 0)
            {
                var actual = _da.ObtenerSeriePorRowId(s.RowId);
                if (actual == null)
                    return ResultadoRecibo.Error("La serie que intenta editar ya no existe.");
                _da.ActualizarSerie(s);
                return ResultadoRecibo.Ok(s.ProximoId);
            }

            _da.InsertarSerie(s);
            return ResultadoRecibo.Ok(s.ProximoId);
        }

        /// <summary>
        /// Eliminar solo series VÍRGENES (sin recibos emitidos). Si ya emitió,
        /// borrarla rompería la trazabilidad del correlativo — se bloquea.
        /// </summary>
        public ResultadoRecibo EliminarSerie(int rowId)
        {
            var s = _da.ObtenerSeriePorRowId(rowId);
            if (s == null)
                return ResultadoRecibo.Error("La serie no existe.");

            int maxUsado = _da.ObtenerMaxUsado(s.Empresa, s.Serie);
            if (maxUsado > 0)
                return ResultadoRecibo.Error(
                    $"No se puede eliminar: la serie {s.Serie} ya emitió recibos " +
                    $"(último: {s.Serie}{maxUsado:00000}). Elimínela solo si nunca ha emitido.");

            _da.EliminarSerie(rowId);
            return ResultadoRecibo.Ok(s.Serie);
        }

        private static string NormalizarEmpresa(string empresa)
        {
            var e = (empresa ?? "").Trim().ToUpper();
            return EMPRESAS_VALIDAS.Contains(e) ? e : "";
        }
    }
}