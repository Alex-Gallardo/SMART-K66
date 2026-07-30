using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class UsuarioEmpresaBL
    {
        // ── CATÁLOGO DE EMPRESAS ──────────────────────────────────────────────
        public const long ID_BOLIK = 20210705001L;
        public const long ID_FAES = 20210705003L;
        public const long ID_GRACO = 20210705004L;

        private static readonly Dictionary<long, string> _nombres =
            new Dictionary<long, string>
            {
                { ID_BOLIK, "BOLIK"  },
                { ID_FAES,  "FAES"   },
                { ID_GRACO, "GRACO"  }
            };

        // Nombre de base de datos HANA correspondiente a cada empresa.
        // Este valor es el que se pasa a AplicarConexionHana(rpt, databaseOverride).
        private static readonly Dictionary<long, string> _hanaDb =
            new Dictionary<long, string>
            {
                { ID_BOLIK, "SBOBOLIK"  },
                { ID_FAES,  "SBOESCOCESA"   },
                { ID_GRACO, "SBO_GRACO" }
            };

        // ── CONSULTAS ─────────────────────────────────────────────────────────

        public List<UsuarioEmpresa> ObtenerPorUsuarioId(long usuarioId)
        {
            return new UsuarioEmpresaDA().ObtenerPorUsuarioId(usuarioId);
        }

        // ── HELPERS ───────────────────────────────────────────────────────────

        public string GetEmpresaNombre(long empresaId) =>
            _nombres.ContainsKey(empresaId) ? _nombres[empresaId] : "DESCONOCIDA";

        public string GetHanaDb(long empresaId) =>
            _hanaDb.ContainsKey(empresaId) ? _hanaDb[empresaId] : null;

        /// <summary>
        /// Separa el campo Codigo en dos partes usando el primer guión como delimitador.
        ///
        /// Ejemplos:
        ///   "12-RAUL DIAZ"  → SapId = "12",  AgenteNombre = "RAUL DIAZ"
        ///   "3-MANUEL"      → SapId = "3",   AgenteNombre = "MANUEL"
        ///   "RUBIDIO"       → SapId = "",    AgenteNombre = "RUBIDIO"   (sin guión)
        ///   null / vacío    → SapId = "",    AgenteNombre = ""
        /// </summary>
        public CodigoParsed ParseCodigo(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                return new CodigoParsed { SapId = string.Empty, AgenteNombre = string.Empty };

            int idx = codigo.IndexOf('-');
            if (idx > 0)
            {
                return new CodigoParsed
                {
                    SapId = codigo.Substring(0, idx).Trim(),
                    AgenteNombre = codigo.Substring(idx + 1).Trim()
                };
            }

            // Sin guión: tratamos todo como nombre de agente
            return new CodigoParsed
            {
                SapId = string.Empty,
                AgenteNombre = codigo.Trim()
            };
        }
    }

    /// <summary>
    /// DTO resultado de ParseCodigo.
    /// En TypeScript sería: type CodigoParsed = { sapId: string; agenteNombre: string }
    /// </summary>
    public class CodigoParsed
    {
        public string SapId { get; set; }
        public string AgenteNombre { get; set; }
    }
}