using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

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
        public bool CuentaEmpresa { get; set; }
        public string NombreEmpresa { get; set; }
        public int Centros { get; set; }
        public int Vehiculos { get; set; }
        public bool Listo { get { return UsuarioActivo && AutenticarSite && TieneRolPiloto && VinculoActivo == true && (CuentaEmpresa ? Vehiculos > 0 : EmpleadoRowId.HasValue && !string.IsNullOrWhiteSpace(Piloto)); } }
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
        public bool CuentaEmpresa { get; set; }
        public string NombreEmpresa { get; set; }
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
            modelo.NombreEmpresa = Limpiar(modelo.NombreEmpresa);
            modelo.Centros = NormalizarLista(modelo.Centros);
            modelo.Placas = NormalizarLista(modelo.Placas);
        }

        public static void Validar(PilotoAdminGuardar modelo)
        {
            Normalizar(modelo);
            if (modelo == null || modelo.UsuarioId <= 0) throw new PilotoException(400, "Selecciona un usuario válido.");
            if (modelo.CuentaEmpresa) { modelo.EmpleadoRowId=null; modelo.Centros=new string[0]; }
            if (modelo.Activo && !modelo.CuentaEmpresa && (!modelo.EmpleadoRowId.HasValue || modelo.EmpleadoRowId.Value <= 0)) throw new PilotoException(400, "Selecciona el empleado piloto de APK66.");
            if (modelo.Activo && (string.IsNullOrEmpty(modelo.CodigoOperador) || modelo.CodigoOperador.Length > 15))
                throw new PilotoException(400, "Indica un código de operador de hasta 15 caracteres.");
            if (string.IsNullOrEmpty(modelo.Motivo) || modelo.Motivo.Length > 250)
                throw new PilotoException(400, "Describe el motivo del cambio en hasta 250 caracteres.");
            if (modelo.CuentaEmpresa && (string.IsNullOrWhiteSpace(modelo.NombreEmpresa) || modelo.NombreEmpresa.Length > 120 || modelo.NombreEmpresa.Any(char.IsControl)))
                throw new PilotoException(400, "Indica el nombre de la empresa (máximo 120 caracteres).");
            if (modelo.Centros.Length > MaxCentros || modelo.Placas.Length > MaxVehiculos)
                throw new PilotoException(400, "La selección supera el límite permitido.");
            if (modelo.Activo && (modelo.Centros.Any(v => v.Length > 15) || modelo.Placas.Any(v => v.Length > 15)))
                throw new PilotoException(400, "Centros y placas admiten hasta 15 caracteres.");
            if (modelo.Activo && modelo.CuentaEmpresa && modelo.Placas.Length == 0)
                throw new PilotoException(400, "Una cuenta empresarial activa necesita al menos un vehículo.");
        }

        private static string[] NormalizarLista(string[] valores)
        {
            if (valores == null) return new string[0];
            return valores.Select(Limpiar).Where(v => !string.IsNullOrEmpty(v))
                .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(v => v, StringComparer.OrdinalIgnoreCase).ToArray();
        }

        private static string Limpiar(string valor) { return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim(); }
    }

    public static class PilotoEmpresaReglas
    {
        public static bool PuedeAsignarConductor(bool salidaRegistrada,string nombre,string documento)
        {
            return !salidaRegistrada || string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(documento);
        }
        public static bool NitCoincide(string login)
        {
            if(string.IsNullOrWhiteSpace(login) || login.Length>50) return false;
            var valor=login.Trim().ToUpperInvariant();
            return System.Text.RegularExpressions.Regex.IsMatch(valor,@"^[0-9]{1,12}-?[0-9K]$");
        }
        public static string Conductor(string valor)
        {
            valor=(valor??"").Trim();
            if(valor.Length<5 || valor.Length>120 || valor.Any(char.IsControl))
                throw new PilotoException(400,"Indica el nombre completo del conductor (5 a 120 caracteres).");
            return valor;
        }
        public static string Documento(string valor)
        {
            valor=(valor??"").Trim();
            if(valor.Length<4 || valor.Length>30 || valor.Any(char.IsControl))
                throw new PilotoException(400,"Indica el documento o licencia del conductor (4 a 30 caracteres).");
            return valor;
        }
        public static string SeleccionarPlaca(IEnumerable<string> autorizadas,string solicitada)
        {
            solicitada=(solicitada??"").Trim();
            if(solicitada.Length==0) return null;
            if(solicitada.Length>15 || solicitada.Any(char.IsControl)) throw new PilotoException(400,"Selecciona una placa válida.");
            var placa=(autorizadas??Enumerable.Empty<string>()).FirstOrDefault(p=>string.Equals(p,solicitada,StringComparison.OrdinalIgnoreCase));
            if(placa==null) throw new PilotoException(403,"Esta placa no está vinculada a tu empresa.");
            return placa;
        }
    }

    public sealed class PilotoConductorGuardar
    {
        public string RutaId { get; set; }
        public string Version { get; set; }
        public long EmpresaUsuarioId { get; set; }
        public string Nombre { get; set; }
        public string Documento { get; set; }
        public string Motivo { get; set; }
    }

    public sealed class PilotoAdminRutaGuardar
    {
        public string RutaId { get; set; }
        public string Version { get; set; }
        public DateTime Fecha { get; set; }
        public string Piloto { get; set; }
        public string Centro { get; set; }
        public string Placa { get; set; }
        public string Estado { get; set; }
        public string ObservacionGeneral { get; set; }
        public string TipoRuta { get; set; }
        public string Ayudante { get; set; }
        public string Ayudante2 { get; set; }
        public string Despacho { get; set; }
        public string Revisor { get; set; }
        public bool Custodio { get; set; }
        public string TipoCustodio { get; set; }
        public string Observaciones { get; set; }
        public TimeSpan? HoraEncendido { get; set; }
        public TimeSpan? HoraSalida { get; set; }
        public TimeSpan? HoraEntrada { get; set; }
        public string PlantaDestino { get; set; }
        public string Motivo { get; set; }
    }

    public sealed class PilotoAdminDocumentoGuardar
    {
        public string RutaId { get; set; }
        public int RowId { get; set; }
        public string Version { get; set; }
        public string Tipo { get; set; }
        public string Empresa { get; set; }
        public DateTime? FechaDocumento { get; set; }
        public string Documento { get; set; }
        public string FelSerie { get; set; }
        public string FelNumero { get; set; }
        public decimal? Monto { get; set; }
        public string Cliente { get; set; }
        public string Agente { get; set; }
        public string NombreDespacho { get; set; }
        public string Direccion { get; set; }
        public decimal Bultos { get; set; }
        public string Preparo { get; set; }
        public bool? Multientrega { get; set; }
        public bool? Visito { get; set; }
        public string Entrega { get; set; }
        public TimeSpan? HoraSalida { get; set; }
        public TimeSpan? HoraEntrada { get; set; }
        public string Observacion { get; set; }
        public string MotivoNoEntrega { get; set; }
        public string ObservacionLiquidacion { get; set; }
        public string UsuarioLiquida { get; set; }
        public DateTime? FechaLiquidacion { get; set; }
        public string MotivoCambio { get; set; }
    }

    public sealed class PilotoAdminDocumentoEdicion
    {
        public PilotoAdminDocumentoGuardar Formulario { get; set; }
        public string EstadoRuta { get; set; }
        public bool RutaLiquidada { get; set; }
    }

    public static class PilotoAdminDocumentoReglas
    {
        public static string Version(PilotoAdminDocumentoGuardar d,string rutaVersion)
        {
            if(d==null) return null;
            var valores=new[]{rutaVersion,d.RutaId,d.RowId.ToString(),d.Tipo,d.Empresa,d.FechaDocumento.ToString(),d.Documento,d.FelSerie,d.FelNumero,d.Monto.ToString(),d.Cliente,d.Agente,d.NombreDespacho,d.Direccion,d.Bultos.ToString(),d.Preparo,d.Multientrega.ToString(),d.Visito.ToString(),d.Entrega,d.HoraSalida.ToString(),d.HoraEntrada.ToString(),d.Observacion,d.MotivoNoEntrega,d.ObservacionLiquidacion,d.UsuarioLiquida,d.FechaLiquidacion.ToString()};
            using(var sha=SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\u001f",valores.Select(v=>v??"")))));
        }
        public static void Validar(PilotoAdminDocumentoGuardar d,bool cerrado)
        {
            if(d==null || string.IsNullOrWhiteSpace(d.RutaId) || d.RutaId.Length>15 || d.RowId<=0 || string.IsNullOrWhiteSpace(d.Version)) throw new PilotoException(400,"Selecciona un documento existente y actualiza la página.");
            d.Tipo=(d.Tipo??"").Trim();d.Empresa=(d.Empresa??"").Trim();d.Documento=(d.Documento??"").Trim();d.Cliente=(d.Cliente??"").Trim();d.Agente=(d.Agente??"").Trim();d.Direccion=(d.Direccion??"").Trim();
            d.FelSerie=(d.FelSerie??"").Trim();d.FelNumero=(d.FelNumero??"").Trim();d.NombreDespacho=(d.NombreDespacho??"").Trim();d.Preparo=(d.Preparo??"").Trim();d.Observacion=(d.Observacion??"").Trim();d.MotivoNoEntrega=(d.MotivoNoEntrega??"").Trim().ToUpperInvariant();d.ObservacionLiquidacion=(d.ObservacionLiquidacion??"").Trim();d.MotivoCambio=(d.MotivoCambio??"").Trim();
            if(d.Tipo.Length==0 || d.Tipo.Length>15 || d.Empresa.Length==0 || d.Empresa.Length>15 || d.Documento.Length>15 ||
               d.Agente.Length==0 || d.Agente.Length>50 || d.Cliente.Length>350 || d.Direccion.Length>350 || d.FelSerie.Length>150 || d.FelNumero.Length>150 ||
               d.NombreDespacho.Length>250 || d.Preparo.Length>150 || d.Observacion.Length>2000 || d.ObservacionLiquidacion.Length>2000 ||
               d.Bultos<0 || d.Monto<0 || d.HoraSalida>=TimeSpan.FromDays(1) || d.HoraEntrada>=TimeSpan.FromDays(1) ||
               d.MotivoCambio.Length<5 || d.MotivoCambio.Length>250 || new[]{d.Tipo,d.Empresa,d.Documento,d.Agente,d.Cliente,d.Direccion,d.MotivoCambio}.Any(x=>x.Any(c=>char.IsControl(c) && c!='\r' && c!='\n')))
                throw new PilotoException(400,"Revisa la identificación, cantidades y motivo del documento.");
            d.Entrega=(d.Entrega??"").Trim().ToUpperInvariant();
            if(d.Entrega.Length==0) {d.Entrega=null;d.Visito=null;d.MotivoNoEntrega=null;if(cerrado) throw new PilotoException(400,"Una ruta cerrada o liquidada requiere resultado para cada documento.");}
            else
            {
                var resultado=new PilotoResultado {RowId=d.RowId,Visito=d.Visito,Entrega=d.Entrega,Motivo=d.MotivoNoEntrega,Observaciones=d.Observacion};
                PilotoReglas.ValidarResultado(resultado);
                if(d.Entrega=="ENTREGADO") d.MotivoNoEntrega=null;
                if(d.Visito==false && (d.HoraEntrada.HasValue || d.HoraSalida.HasValue)) throw new PilotoException(400,"Un documento no visitado no puede tener horas de visita.");
            }
        }
    }

    public sealed class PilotoAdminRutaEdicion
    {
        public PilotoRuta Ruta { get; set; }
        public PilotoAdminRutaGuardar Formulario { get; set; }
        public bool Liquidada { get; set; }
        public int Documentos { get; set; }
        public List<PilotoAdminEmpleado> Pilotos { get; set; }
        public List<PilotoAdminOpcion> Centros { get; set; }
        public List<PilotoAdminOpcion> Vehiculos { get; set; }
        public List<PilotoAdminRutaCambio> Historial { get; set; }
        public string ConductorReal { get; set; }
        public string DocumentoConductor { get; set; }
        public string VersionConductor { get; set; }
        public long EmpresaUsuarioId { get; set; }
        public bool EsRutaEmpresa { get; set; }
        public List<PilotoAdminRutaCambio> HistorialConductor { get; set; }
        public List<PilotoAdminDocumentoGuardar> Detalles { get; set; }
        public string CamposTecnicos { get; set; }
        public int PaginaDocumentos { get; set; }
        public int TotalDocumentosFiltrados { get; set; }
        public string BuscarDocumento { get; set; }
        public PilotoAdminRutaEdicion()
        {
            Pilotos=new List<PilotoAdminEmpleado>();Centros=new List<PilotoAdminOpcion>();Vehiculos=new List<PilotoAdminOpcion>();Historial=new List<PilotoAdminRutaCambio>();HistorialConductor=new List<PilotoAdminRutaCambio>();Detalles=new List<PilotoAdminDocumentoGuardar>();PaginaDocumentos=1;
        }
        public bool PuedeEditar { get { return Ruta!=null && new[]{"B","A","E","C","L","X"}.Contains(Ruta.Estado); } }
    }

    public sealed class PilotoAdminRutaCambio
    {
        public DateTime FechaUtc { get; set; }
        public string Actor { get; set; }
        public string Motivo { get; set; }
        public string Antes { get; set; }
        public string Despues { get; set; }
    }

    public static class PilotoAdminRutaReglas
    {
        public static string Version(PilotoRuta ruta,bool liquidada,int documentos)
        {
            var texto=string.Join("\u001f",new[]{ruta.Id,ruta.Fecha.Ticks.ToString(),ruta.Piloto,ruta.Centro,ruta.Placa,ruta.Estado,ruta.ObservacionGeneral,ruta.TipoRuta,ruta.Ayudante,ruta.Ayudante2,ruta.Despacho,ruta.Revisor,ruta.Custodio.ToString(),ruta.TipoCustodio,ruta.Observaciones,ruta.HoraEncendido.ToString(),ruta.HoraSalida.ToString(),ruta.HoraEntrada.ToString(),ruta.PlantaDestino,liquidada.ToString(),documentos.ToString()}.Select(v=>v??""));
            using(var sha=SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(texto)));
        }
        public static void Validar(PilotoAdminRutaGuardar m)
        {
            if(m==null || string.IsNullOrWhiteSpace(m.RutaId) || m.RutaId.Length>15 || string.IsNullOrWhiteSpace(m.Version) || m.Version.Length>100)
                throw new PilotoException(400,"La ruta o su versión no son válidas.");
            m.Piloto=(m.Piloto??"").Trim();m.Centro=(m.Centro??"").Trim();m.Placa=(m.Placa??"").Trim();
            m.ObservacionGeneral=(m.ObservacionGeneral??"").Trim();m.Motivo=(m.Motivo??"").Trim();
            m.TipoRuta=(m.TipoRuta??"").Trim();m.Ayudante=(m.Ayudante??"").Trim();m.Ayudante2=(m.Ayudante2??"").Trim();m.Despacho=(m.Despacho??"").Trim();m.Revisor=(m.Revisor??"").Trim();m.TipoCustodio=(m.TipoCustodio??"").Trim();m.Observaciones=(m.Observaciones??"").Trim();m.PlantaDestino=(m.PlantaDestino??"").Trim();
            if(m.Fecha.Year<2000 || m.Fecha.Year>2100 || m.Piloto.Length==0 || m.Piloto.Length>90 || m.Centro.Length==0 || m.Centro.Length>15 || m.Placa.Length==0 || m.Placa.Length>15 ||
               m.ObservacionGeneral.Length>500 || m.Motivo.Length==0 || m.Motivo.Length>250 || !new[]{"B","A","E","C","L","X"}.Contains(m.Estado) ||
               m.TipoRuta.Length>50 || m.Ayudante.Length==0 || m.Ayudante.Length>90 || m.Ayudante2.Length>90 || m.Despacho.Length==0 || m.Despacho.Length>90 || m.Revisor.Length==0 || m.Revisor.Length>90 || m.TipoCustodio.Length>50 || m.PlantaDestino.Length>100 || m.Observaciones.Length>2000 ||
               (m.Custodio && m.TipoCustodio.Length==0) || m.HoraEncendido>=TimeSpan.FromDays(1) || m.HoraSalida>=TimeSpan.FromDays(1) || m.HoraEntrada>=TimeSpan.FromDays(1) ||
               new[]{m.Piloto,m.Centro,m.Placa}.Any(v=>v.Any(char.IsControl)) ||
               new[]{m.ObservacionGeneral,m.Motivo}.Any(v=>v.Any(c=>char.IsControl(c) && c!='\r' && c!='\n')))
                throw new PilotoException(400,"Revisa la fecha, asignación, estado, observación y motivo de la ruta.");
        }
    }
}
