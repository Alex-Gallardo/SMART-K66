using System;
using System.Collections.Generic;
using System.Linq;
using DiamDev.Give.Entities;

namespace DiamDev.Give.BLL
{
    public static class ReciboCajaOperadores
    {
        /// <summary>Conserva cada operador asignado y prefiere su registro con depto.</summary>
        public static List<UsuarioEmpresa> Elegir(IEnumerable<UsuarioEmpresa> registros)
        {
            return registros
                .Where(r => !string.IsNullOrWhiteSpace(r.Codigo))
                .GroupBy(r => r.Codigo.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.OrderByDescending(r => !string.IsNullOrWhiteSpace(r.DEPTO_RECIBO)).First())
                .ToList();
        }
    }
}
