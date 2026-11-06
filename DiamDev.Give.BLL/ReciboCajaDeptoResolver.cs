using System;

namespace DiamDev.Give.BLL
{
    /// <summary>Resuelve el depto explícito o el depto anterior cuando tiene serie real.</summary>
    public static class ReciboCajaDeptoResolver
    {
        public static string Resolver(string deptoAsignado, string agenteNombre,
                                      Func<string, bool> existeSerie)
        {
            string depto = (deptoAsignado ?? "").Trim();
            if (depto.Length > 0) return depto;

            string anterior = (agenteNombre ?? "").Trim();
            return anterior.Length > 0 && existeSerie(anterior) ? anterior : "";
        }
    }
}
