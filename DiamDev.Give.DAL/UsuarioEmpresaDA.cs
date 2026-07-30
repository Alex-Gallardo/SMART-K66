using DiamDev.Give.Entities;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.DAL
{
    /// <summary>
    /// Acceso a datos de Usuario_Empresa usando Entity Framework (GiveContext).
    /// Podemos usar EF porque la entidad ya existe con sus atributos y navegación.
    /// </summary>
    public class UsuarioEmpresaDA
    {
        /// <summary>
        /// Retorna TODOS los registros del usuario.
        /// OJO: puede haber múltiples filas por (UsuarioId, EmpresaId)
        /// si el usuario tiene más de un Codigo en la misma empresa —
        /// porque Codigo es parte de la PK compuesta.
        /// </summary>
        public List<UsuarioEmpresa> ObtenerPorUsuarioId(long usuarioId)
        {
            using (var ctx = new GiveContext())
            {
                return ctx.UsuarioEmpresas
                    .Where(ue => ue.UsuarioId == usuarioId)
                    .ToList();
            }
        }
    }
}