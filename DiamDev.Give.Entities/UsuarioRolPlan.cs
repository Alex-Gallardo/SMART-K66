using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.Entities
{
    public sealed class UsuarioRolPlan
    {
        public int[] Agregar { get; private set; }
        public int[] Quitar { get; private set; }

        public static UsuarioRolPlan Crear(IEnumerable<int> actuales, IEnumerable<int> solicitados)
        {
            if (actuales == null || solicitados == null) throw new ArgumentNullException(actuales == null ? "actuales" : "solicitados");
            var antes = new HashSet<int>(actuales);
            var despues = new HashSet<int>(solicitados);
            if (despues.Count == 0 || despues.Any(id => id <= 0)) throw new ArgumentException("Selecciona al menos un rol válido.", "solicitados");
            return new UsuarioRolPlan
            {
                Agregar = despues.Except(antes).OrderBy(id => id).ToArray(),
                Quitar = antes.Except(despues).OrderBy(id => id).ToArray()
            };
        }
    }
}
