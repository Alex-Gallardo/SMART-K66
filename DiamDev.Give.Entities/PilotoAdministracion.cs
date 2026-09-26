using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.Entities
{
    public sealed class PilotoAdminLista
    {
        public string Buscar { get; set; }
        public List<PilotoAdminUsuario> Usuarios { get; set; }
        public PilotoAdminLista() { Usuarios = new List<PilotoAdminUsuario>(); }
    }

    public sealed class PilotoAdminUsuario
    {
        public long UsuarioId { get; set; }
        public string Login { get; set; }
        public string Nombre { get; set; }
        public bool UsuarioActivo { get; set; }
        public bool AutenticarSite { get; set; }
        public bool TieneRolPiloto { get; set; }
        public bool? VinculoActivo { get; set; }
        public int? EmpleadoRowId { get; set; }
        public string Piloto { get; set; }
        public string CodigoOperador { get; set; }
        public int Centros { get; set; }
        public int Vehiculos { get; set; }
        public bool Listo { get { return UsuarioActivo && AutenticarSite && TieneRolPiloto && VinculoActivo == true && Centros > 0 && Vehiculos > 0; } }
    }

    public sealed class PilotoAdminEmpleado
    {
        public int RowId { get; set; }
        public string Nombre { get; set; }
        public bool Activo { get; set; }
        public bool Seleccionado { get; set; }
    }

    public sealed class PilotoAdminOpcion
    {
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public bool Activo { get; set; }
        public bool Seleccionado { get; set; }
    }

    public sealed class PilotoAdminGuardar
    {
        public long UsuarioId { get; set; }
        public int? EmpleadoRowId { get; set; }
        public string CodigoOperador { get; set; }
        public bool Activo { get; set; }
        public string[] Centros { get; set; }
        public string[] Placas { get; set; }
        public string Motivo { get; set; }
    }

    public sealed class PilotoAdminEdicion
    {
        public PilotoAdminUsuario Usuario { get; set; }
        public PilotoAdminGuardar Formulario { get; set; }
        public List<PilotoAdminEmpleado> Empleados { get; set; }
        public List<PilotoAdminOpcion> Centros { get; set; }
        public List<PilotoAdminOpcion> Vehiculos { get; set; }
        public List<PilotoRuta> Rutas { get; set; }
        public PilotoAdminEdicion()
        {
            Empleados = new List<PilotoAdminEmpleado>(); Centros = new List<PilotoAdminOpcion>();
            Vehiculos = new List<PilotoAdminOpcion>(); Rutas = new List<PilotoRuta>();
        }
    }

    public static class PilotoAdminReglas
    {
        public const string PermisoAdministrar = "Pilotos.Administrar";
        public const int MaxCentros = 20;
        public const int MaxVehiculos = 50;

        public static void ValidarAcceso(bool cuentaDisponible, bool tienePermiso)
        {
            if(!cuentaDisponible) throw new PilotoException(403,"La cuenta debe ser única, estar activa y habilitada para ingresar al sitio.");
            if(!tienePermiso) throw new PilotoException(403,"No tienes permiso para administrar pilotos. Solicita el acceso al administrador.");
        }

        public static void Normalizar(PilotoAdminGuardar modelo)
        {
            if (modelo == null) return;
            modelo.CodigoOperador = Limpiar(modelo.CodigoOperador);
            modelo.Motivo = Limpiar(modelo.Motivo);
            modelo.Centros = NormalizarLista(modelo.Centros);
            modelo.Placas = NormalizarLista(modelo.Placas);
        }

        public static void Validar(PilotoAdminGuardar modelo)
        {
            Normalizar(modelo);
            if (modelo == null || modelo.UsuarioId <= 0) throw new PilotoException(400, "Selecciona un usuario válido.");
            if (modelo.Activo && (!modelo.EmpleadoRowId.HasValue || modelo.EmpleadoRowId.Value <= 0)) throw new PilotoException(400, "Selecciona el empleado piloto de APK66.");
            if (modelo.Activo && (string.IsNullOrEmpty(modelo.CodigoOperador) || modelo.CodigoOperador.Length > 15))
                throw new PilotoException(400, "Indica un código de operador de hasta 15 caracteres.");
            if (string.IsNullOrEmpty(modelo.Motivo) || modelo.Motivo.Length > 250)
                throw new PilotoException(400, "Describe el motivo del cambio en hasta 250 caracteres.");
            if (modelo.Centros.Length > MaxCentros || modelo.Placas.Length > MaxVehiculos)
                throw new PilotoException(400, "La selección supera el límite permitido.");
            if (modelo.Activo && (modelo.Centros.Any(v => v.Length > 15) || modelo.Placas.Any(v => v.Length > 15)))
                throw new PilotoException(400, "Centros y placas admiten hasta 15 caracteres.");
            if (modelo.Activo && (modelo.Centros.Length == 0 || modelo.Placas.Length == 0))
                throw new PilotoException(400, "Un vínculo activo necesita al menos un centro y un vehículo.");
        }

        private static string[] NormalizarLista(string[] valores)
        {
            if (valores == null) return new string[0];
            return valores.Select(Limpiar).Where(v => !string.IsNullOrEmpty(v))
                .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(v => v, StringComparer.OrdinalIgnoreCase).ToArray();
        }

        private static string Limpiar(string valor) { return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim(); }
    }
}
