using System.Linq;
using DiamDev.Give.Entities;

namespace DiamDev.Give.DAL
{
    /// <summary>
    /// Acceso a datos del mapeo usuario→DEPTO de serie, vía EF (GiveContext).
    /// Mismo patrón que UsuarioEmpresaDA: abre contexto, consulta, cierra.
    /// </summary>
    public class RecibosCajaUsuarioDeptoDA
    {
        /// <summary>
        /// Devuelve el DEPTO de serie del usuario, o string.Empty si no está
        /// mapeado o está inactivo. El caller (BLL) traduce el vacío en un
        /// error claro ANTES de tocar la transacción del correlativo.
        /// </summary>
        public string ObtenerDeptoPorUsuarioId(long usuarioId)
        {
            // Usa RecibosContext (BD del módulo), NO el GiveContext de producción.
            using (var ctx = new GiveContext("RecibosContext"))
            {
                var map = ctx.RecibosCajaUsuarioDeptos
                    .FirstOrDefault(x => x.UsuarioId == usuarioId && x.Activo);

                return map?.Depto ?? string.Empty;
            }
        }
    }
}