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

        private const int TOPE_FILAS_FALLBACK = 2000;

        /// <summary>
        /// Tope de filas del grid (Web.config → DashboardTopeFilas).
        ///
        /// Era 500 hardcodeado en el SQL. El problema no era el número: era que
        /// vivía en un string SQL, así que subirlo exigía recompilar y publicar.
        /// Ahora es config, igual que DashboardDiasEnvejecido.
        ///
        /// 2000 no es arbitrario: hoy el peor universo real (vivos + operados)
        /// son 857 filas y crece ~950/mes. 2000 cubre con holgura hasta que
        /// llegue la paginación real. NO es la solución definitiva — es el
        /// piso que evita perder datos mientras tanto.
        /// </summary>
        public int TopeFilas
        {
            get
            {
                string raw = ConfigurationManager.AppSettings["DashboardTopeFilas"];
                return (int.TryParse(raw, out int v) && v >= 100) ? v : TOPE_FILAS_FALLBACK;
            }
        }

        // ═════════════════════════════════════════════
        //  ALCANCE POR USUARIO_EMPRESA
        // ═════════════════════════════════════════════

        /// <summary>Id numérico de Usuario_Empresa → clave string de recibos.</summary>
        private static readonly Dictionary<long, string> EMPRESAS_POR_ID =
            new Dictionary<long, string>
            {
                { UsuarioEmpresaBL.ID_GRACO, "GRACO" },
                { UsuarioEmpresaBL.ID_FAES,  "FAES"  },
                { UsuarioEmpresaBL.ID_BOLIK, "BOLIK" }
            };

        /// <summary>
        /// Roles que ven TODO el dashboard, desde Web.config:
        ///   &lt;add key="DashboardRolesGlobales" value="CREDITOS,ADMINISTRACION,..." /&gt;
        /// En config y no en código para que agregar un rol no obligue a
        /// recompilar y republicar. Mismo criterio que DashboardDiasEnvejecido.
        /// Si la clave no existe o el rol viene vacío → NO es global (falla cerrado).
        /// </summary>
        public bool EsRolGlobal(string rol)
        {
            if (string.IsNullOrWhiteSpace(rol)) return false;

            string raw = ConfigurationManager.AppSettings["DashboardRolesGlobales"] ?? "";
            if (raw.Trim().Length == 0) return false;

            string r = rol.Trim();
            return raw.Split(',')
                      .Select(x => x.Trim())
                      .Any(x => x.Length > 0 &&
                                string.Equals(x, r, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Resuelve QUÉ puede ver el usuario en el dashboard.
        ///
        /// Se compara el PAR (empresa, código), no el código suelto: la misma
        /// persona tiene código distinto por empresa (FAES '13-PABLO GAITAN' vs
        /// GRACO '15-PABLO GAITAN') y códigos iguales existen en empresas
        /// distintas ('2-GERENCIA'). Comparar solo el código perdería datos
        /// propios y expondría ajenos.
        ///
        /// A diferencia de ObtenerEmpresasUsuario, aquí NO se exige DEPTO_RECIBO:
        /// ese requisito es para EMITIR (necesita serie de numeración), no para
        /// CONSULTAR. Un código sin depto pero con recibos históricos debe verlos.
        ///
        /// Si el usuario no tiene ningún par → SinAcceso = true y el DA aplica
        /// "AND 1 = 0". Es intencional: sin asignaciones, no ve nada.
        /// </summary>
        /// <param name="esGlobal">
        /// Ya resuelto por el Controller (permiso + fallback de rol). El BLL no
        /// consulta permisos: eso es contexto HTTP/sesión y vive en la capa UI.
        /// Mantener esa frontera es lo que permite testear el BLL sin un request.
        /// </param>
        public AlcanceRecibos ObtenerAlcance(long usuarioId, bool esGlobal)
        {
            var alcance = new AlcanceRecibos { Global = esGlobal };
            if (alcance.Global) return alcance;

            var registros = new UsuarioEmpresaDA().ObtenerPorUsuarioId(usuarioId);

            foreach (var r in registros)
            {
                if (!EMPRESAS_POR_ID.TryGetValue(r.EmpresaId, out string emp)) continue;

                string cod = (r.Codigo ?? "").Trim();
                if (cod.Length == 0) continue;   // sin código no puede haber match

                // Distinct manual por (empresa, código): Codigo es parte de la PK
                // compuesta, pero pueden venir repetidos por otras columnas.
                bool yaEsta = alcance.Pares.Any(p =>
                    string.Equals(p.Empresa, emp, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(p.Codigo, cod, StringComparison.OrdinalIgnoreCase));

                if (!yaEsta)
                    alcance.Pares.Add(new AlcancePar { Empresa = emp, Codigo = cod });
            }

            return alcance;
        }

        // ─── DASHBOARD ────────────────────────────────
        public DashboardResumenRecibos ObtenerResumen(string empresa, AlcanceRecibos alcance) =>
            _da.ObtenerResumen(NormalizarEmpresa(empresa), DiasUmbral, alcance);

        /// <summary>
        /// Detalle del dashboard. Reparto de responsabilidades:
        ///   - El DA decide QUÉ UNIVERSO traer (vivos / +operados / +anulados),
        ///     porque eso vive en el WHERE y no se puede filtrar después.
        ///   - El BLL filtra por SITUACIÓN en memoria, porque la situación es
        ///     una clasificación DERIVADA que ya calculó el DA fila por fila.
        ///
        /// ⚠ DEUDA TÉCNICA CONOCIDA — leer antes de tocar:
        /// El filtro por situación corre DESPUÉS del TOP del servidor. Si el
        /// universo supera el tope, la situación se filtra sobre una MUESTRA,
        /// no sobre el total, y el grid muestra menos de lo que dice la card.
        ///
        /// Medido el 2026-08-05 en producción:
        ///   - Vivos (PENDIENTE+DESCUADRE): 23 filas → sin riesgo. Es una cola
        ///     de trabajo autolimpiante, no crece de forma acumulativa.
        ///   - Con OPERADO: 857 filas → CON el TOP 500 anterior se perdían 357.
        ///   - Con ANULADO: iban al grupo 3 del ORDER BY, detrás de 834
        ///     operados → no llegaba ninguno al grid.
        ///
        /// Mitigación actual: tope configurable (2000) + bandera 'truncado' que
        /// el front muestra al usuario. Solución definitiva: clasificar la
        /// situación EN SQL y paginar (P3 del plan de escalabilidad).
        /// </summary>
        public List<DashboardFilaRecibo> ObtenerDetalle(
            string empresa, string situacion,
            string fechaIni, string fechaFin,
            bool incluirOperados, bool incluirAnulados,
            AlcanceRecibos alcance,
            out bool truncado)
        {
            DateTime? fIni = ParseFechaDash(fechaIni);
            DateTime? fFin = ParseFechaDash(fechaFin);

            situacion = (situacion ?? "TODOS").Trim().ToUpperInvariant();
            if (situacion.Length == 0) situacion = "TODOS";

            // La card dice "ANTIGUO", el DA clasifica como "ENVEJECIDO".
            if (situacion == "ANTIGUO") situacion = "ENVEJECIDO";

            bool soloAnulados = (situacion == "ANULADO");
            bool traerAnulados = soloAnulados || incluirAnulados;

            // ★ NUEVO: si se pide una situación concreta que SOLO existe entre
            // los operados, no tiene sentido arrastrar todo el universo vivo.
            // Mismo razonamiento que ya se aplicó a soloAnulados: recortar el
            // universo antes del TOP, no filtrar después de él.
            bool soloOperados = (situacion == "OPERADO");
            bool traerOperados = soloOperados || incluirOperados;

            var filas = _da.ObtenerDetalle(empresa ?? "", DiasUmbral,
                                           fIni, fFin,
                                           traerOperados, traerAnulados, soloAnulados,
                                           alcance, TopeFilas, out truncado);

            if (situacion == "TODOS") return filas;

            return filas
                .Where(f => string.Equals(f.Situacion, situacion,
                                          StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>Convierte "yyyy-MM-dd" del front a DateTime? (vacío = sin filtro).</summary>
        private static DateTime? ParseFechaDash(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            DateTime d;
            return DateTime.TryParse(s.Trim(), out d) ? d : (DateTime?)null;
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