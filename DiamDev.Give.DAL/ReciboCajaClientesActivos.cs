using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Linq;
using DiamDev.Give.Entities;

namespace DiamDev.Give.DAL
{
    /// <summary>Contrasta los clientes del procedimiento de recibos con el estado actual de OCRD.</summary>
    internal static class ReciboCajaClientesActivos
    {
        public static List<ClienteHana> Filtrar(string schema, IEnumerable<ClienteHana> candidatos,
            Func<string, OdbcParameter[], DataTable> consultar)
        {
            var clientes = (candidatos ?? Enumerable.Empty<ClienteHana>())
                .Where(c => c != null && !string.IsNullOrWhiteSpace(c.CardCode))
                .ToList();
            var codigos = clientes.Select(c => c.CardCode.Trim())
                .Distinct(StringComparer.Ordinal).ToList();
            if (codigos.Count == 0) return new List<ClienteHana>();

            var activos = new HashSet<string>(StringComparer.Ordinal);
            const int tamLote = 200;
            for (int i = 0; i < codigos.Count; i += tamLote)
            {
                var lote = codigos.Skip(i).Take(tamLote).ToList();
                string marcadores = string.Join(",", lote.Select(_ => "?"));
                string sql = string.Format(
                    "SELECT \"CardCode\" FROM \"{0}\".\"OCRD\" " +
                    "WHERE \"CardType\" = 'C' AND \"validFor\" = 'Y' " +
                    "AND \"CardCode\" IN ({1})", schema, marcadores);
                var parametros = lote.Select(codigo => new OdbcParameter {
                    OdbcType = OdbcType.NVarChar, Value = codigo
                }).ToArray();

                foreach (DataRow fila in consultar(sql, parametros).Rows)
                {
                    string codigo = Convert.ToString(fila["CardCode"]);
                    if (!string.IsNullOrWhiteSpace(codigo)) activos.Add(codigo.Trim());
                }
            }

            return clientes.Where(c => activos.Contains(c.CardCode.Trim())).ToList();
        }
    }
}
