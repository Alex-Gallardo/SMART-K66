using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DiamDev.Give.Entities
{
    public sealed class GaritaRuta
    {
        public string Id { get; set; }
        public DateTime Fecha { get; set; }
        public string Piloto { get; set; }
        public bool EsRutaEmpresa { get; set; }
        public string ConductorReal { get; set; }
        public string DocumentoConductor { get; set; }
        public string Placa { get; set; }
        public string Centro { get; set; }
        public string Estado { get; set; }
        public bool Liquidada { get; set; }
        public string Transporte { get; set; }
        public string Vehiculo { get; set; }
        public bool VehiculoDisponible { get; set; }
        public string Observacion { get; set; }
        public string Version { get; set; }
        public DateTime? SalidaUtc { get; set; }
        public DateTime? EntradaUtc { get; set; }
        public TimeSpan? HoraSalidaApk { get; set; }
        public TimeSpan? HoraEntradaApk { get; set; }
        public string AgenteEntrada { get; set; }
        public string GuardDecision { get; set; }
        public string Diferencia { get; set; }
        public string Agente { get; set; }
        public string Precinto { get; set; }
        public string PlacaBusqueda { get; set; }
        public int Pagina { get; set; }
        public int TotalRutas { get; set; }
        public int PaginaAbiertas { get; set; }
        public int TotalAbiertas { get; set; }
        public int PaginaCerradas { get; set; }
        public int TotalCerradas { get; set; }
        public bool BusquedaRealizada { get; set; }
        public bool TieneFotoSalida { get; set; }
        public List<GaritaRutaResumen> Resultados { get; set; }
        public List<GaritaRutaResumen> ResultadosAbiertas { get; set; }
        public List<GaritaRutaResumen> ResultadosCerradas { get; set; }
        public List<PilotoDocumento> Documentos { get; set; }
        public GaritaRuta() { Documentos=new List<PilotoDocumento>(); Resultados=new List<GaritaRutaResumen>(); ResultadosAbiertas=new List<GaritaRutaResumen>(); ResultadosCerradas=new List<GaritaRutaResumen>(); }
        public bool PuedeReportarDiferencia { get { return (Estado=="A" || Estado=="E") && !Liquidada && !SalidaUtc.HasValue && !HoraSalidaApk.HasValue && Diferencia==null; } }
        public bool PuedeRegistrarSalida { get { return PuedeReportarDiferencia && VehiculoDisponible && !string.IsNullOrWhiteSpace(Piloto) && !string.IsNullOrWhiteSpace(Placa) && !string.IsNullOrWhiteSpace(Centro) && Documentos.Count>0 && !Documentos.Any(d=>d.Visito.HasValue || !string.IsNullOrWhiteSpace(d.Entrega)); } }
        public bool PuedeRegistrarEntrada { get { return Estado=="C" && !Liquidada && !HoraEntradaApk.HasValue && !string.IsNullOrWhiteSpace(Placa) && !string.IsNullOrWhiteSpace(Centro); } }
        public int Clientes { get { return Documentos.Select(d=>d.Cliente??"").Distinct(StringComparer.OrdinalIgnoreCase).Count(); } }
        public decimal Bultos { get { return Documentos.Sum(d=>d.Bultos); } }
        public DateTime? SalidaLocal { get { return SalidaUtc.HasValue ? SalidaUtc.Value.AddHours(-6) : (DateTime?)null; } }
    }

    public sealed class GaritaRutaResumen
    {
        public string Id { get; set; }
        public DateTime Fecha { get; set; }
        public string Placa { get; set; }
        public string Piloto { get; set; }
        public string ConductorReal { get; set; }
        public string Centro { get; set; }
        public int Documentos { get; set; }
        public int Clientes { get; set; }
        public decimal Bultos { get; set; }
        public bool SalidaRegistrada { get; set; }
        public bool DiferenciaPendiente { get; set; }
    }

    public sealed class GaritaFoto
    {
        public string ContentType { get; set; }
        public byte[] Contenido { get; set; }
    }

    public sealed class GaritaRegistroSalida
    {
        public string RutaId { get; set; }
        public string Version { get; set; }
        public string PlacaBusqueda { get; set; }
        public bool CargaVerificada { get; set; }
        public string Observacion { get; set; }
        public string FotoNombre { get; set; }
        public byte[] FotoContenido { get; set; }
        public Guid Solicitud { get; set; }
    }

    public sealed class GaritaDiferencia
    {
        public string RutaId { get; set; }
        public string PlacaBusqueda { get; set; }
        public string Version { get; set; }
        public string Descripcion { get; set; }
        public Guid Solicitud { get; set; }
    }

    public sealed class GaritaRegistroEntrada
    {
        public string RutaId { get; set; }
        public string PlacaBusqueda { get; set; }
        public string Version { get; set; }
        public Guid Solicitud { get; set; }
    }

    public sealed class GaritaAdministracion
    {
        public string Login { get; set; }
        public long UsuarioId { get; set; }
        public string Nombre { get; set; }
        public string RutaId { get; set; }
        public List<string> Centros { get; set; }
        public List<string> Disponibles { get; set; }
        public List<GaritaIncidencia> Incidencias { get; set; }
        public GaritaAdministracion() { Centros=new List<string>();Disponibles=new List<string>();Incidencias=new List<GaritaIncidencia>(); }
    }
    public sealed class GaritaIncidencia
    {
        public string RutaId { get; set; }
        public string Centro { get; set; }
        public string Descripcion { get; set; }
        public DateTime FechaUtc { get; set; }
    }

    public static class GaritaReglas
    {
        // Se conserva el permiso POS existente; registrar salida avanza A a E.
        public const string Ver="Garita.Rutas.Ver", RegistrarSalida="Garita.Rutas.ConfirmarSalida", Administrar="Garita.Administrar";
        // El límite de antigüedad aplica solo al listado de cerradas, no al detalle ni al registro de entrada.
        public static DateTime CerradasDesde(DateTime hoyLocal) { return hoyLocal.Date.AddDays(-3); }
        public static DateTime CerradasAntesDe(DateTime hoyLocal) { return hoyLocal.Date.AddDays(1); }
        public static string Id(string id)
        {
            id=(id??"").Trim();
            if(id.Length==0 || id.Length>15 || id.Any(char.IsControl)) throw new PilotoException(400,"Escribe un número de ruta válido (máximo 15 caracteres).");
            return id;
        }
        public static string Placa(string placa)
        {
            placa=(placa??"").Trim().ToUpperInvariant();
            if(placa.Length==0 || placa.Length>15 || placa.Any(char.IsControl)) throw new PilotoException(400,"Escribe una placa válida (máximo 15 caracteres).");
            return placa;
        }
        public static void ValidarSalida(GaritaRegistroSalida c,GaritaRuta r)
        {
            if(c==null || c.RutaId!=r.Id || c.Solicitud==Guid.Empty) throw new PilotoException(400,"Vuelve a abrir la ruta e intenta de nuevo.");
            if(!r.PuedeRegistrarSalida) throw new PilotoException(409,"La ruta debe estar Abierta o En ruta, sin salida previa ni diferencias y lista para salir. Consulta a Distribución.");
            if(c.Version!=r.Version) throw new PilotoException(409,"Los datos de la ruta cambiaron. Revísalos antes de registrar la hora de salida.");
            if(!c.CargaVerificada) throw new PilotoException(400,"Marca la verificación de clientes, facturas y bultos antes de registrar la hora de salida.");
            if(c.FotoContenido!=null) PilotoReglas.ValidarImagen(c.FotoNombre,c.FotoContenido);
            c.Observacion=Limpiar(c.Observacion,250);
        }
        public static void ValidarEntrada(GaritaRegistroEntrada c,GaritaRuta r)
        {
            if(c==null || c.RutaId!=r.Id || c.Solicitud==Guid.Empty) throw new PilotoException(400,"Vuelve a abrir la ruta e intenta de nuevo.");
            if(!r.PuedeRegistrarEntrada) throw new PilotoException(409,"La ruta debe estar Cerrada, sin liquidar y sin hora de entrada. Consulta a Distribución.");
            if(c.Version!=r.Version) throw new PilotoException(409,"La ruta cambió. Revísala antes de registrar la entrada.");
        }
        public static string Limpiar(string valor,int max)
        {
            valor=(valor??"").Replace("\r\n"," ").Replace('\r',' ').Replace('\n',' ').Trim();
            if(valor.Length>max || valor.Any(char.IsControl)) throw new PilotoException(400,"El texto supera el límite o contiene caracteres inválidos.");
            return valor;
        }
        public static string Version(GaritaRuta r)
        {
            var b=new StringBuilder();
            Action<string> add=s=>{s=s??"";b.Append(s.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(s);};
            add(r.Id);add(r.Fecha.ToString("O",CultureInfo.InvariantCulture));add(r.Piloto);add(r.Placa);add(r.Centro);add(r.Estado);add(r.Liquidada?"1":"0");add(r.Observacion);add(r.Transporte);add(r.Vehiculo);add(r.VehiculoDisponible?"1":"0");
            add(r.HoraSalidaApk.HasValue?r.HoraSalidaApk.Value.ToString("c",CultureInfo.InvariantCulture):null);
            add(r.HoraEntradaApk.HasValue?r.HoraEntradaApk.Value.ToString("c",CultureInfo.InvariantCulture):null);
            if(r.EsRutaEmpresa){add(r.ConductorReal);add(r.DocumentoConductor);}
            foreach(var d in r.Documentos.OrderBy(x=>x.RowId)) {add(d.RowId.ToString(CultureInfo.InvariantCulture));add(d.Tipo);add(d.Empresa);add(d.Documento);add(d.Cliente);add(d.Direccion);add(d.Bultos.ToString(CultureInfo.InvariantCulture));add(d.Visito.HasValue?(d.Visito.Value?"1":"0"):"");add(d.Entrega);}
            using(var sha=SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(b.ToString())));
        }
    }
}
