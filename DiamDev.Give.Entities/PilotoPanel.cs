using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace DiamDev.Give.Entities
{
    public sealed class PilotoPanelFiltro
    {
        public string Desde { get; set; }
        public string Hasta { get; set; }
        public string Estado { get; set; }
        public string Liquidacion { get; set; }
        public string Piloto { get; set; }
        public long? Usuario { get; set; }
        public string Centro { get; set; }
        public string Placa { get; set; }
        public string Avance { get; set; }
        public string Resultado { get; set; }
        public string Buscar { get; set; }
        public string Evento { get; set; }
        public string Seccion { get; set; }
        public int Pagina { get; set; }
        public DateTime Inicio { get; private set; }
        public DateTime Fin { get; private set; }
        public PilotoPanelFiltro() { Pagina=1; }
        public void Validar(DateTime hoy)
        {
            Desde=string.IsNullOrWhiteSpace(Desde) ? hoy.ToString("yyyy-MM-dd") : Desde;
            Hasta=string.IsNullOrWhiteSpace(Hasta) ? hoy.ToString("yyyy-MM-dd") : Hasta;
            DateTime a,b;
            if(!DateTime.TryParseExact(Desde,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out a) ||
               !DateTime.TryParseExact(Hasta,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out b) ||
               b<a || (b-a).TotalDays>=31 || b==DateTime.MaxValue.Date)
                throw new PilotoException(400,"Selecciona un período válido de hasta 31 días.");
            Inicio=a; Fin=b.AddDays(1);
            Estado=Opcion(Estado,"todos",new[]{"todos","A","E","C","X"});
            Liquidacion=Opcion(Liquidacion,"no",new[]{"no","si","todas"});
            Avance=Opcion(Avance,"todos",new[]{"todos","sin","parcial","listo"});
            Resultado=Opcion(Resultado,"todos",new[]{"todos","PENDIENTE","ENTREGADO","NO ENTREGADO","INCIDENCIA"});
            Evento=Opcion(Evento,"todos",new[]{"todos","Cliente","Foto documento","Foto cliente","Cierre POS","Vínculo","Centro","Vehículo"});
            Seccion=Opcion(Seccion,"resumen",new[]{"resumen","rutas","pilotos","documentos","actividad","alertas"});
            Piloto=Texto(Piloto,90); Centro=Texto(Centro,15); Placa=Texto(Placa,15); Buscar=Texto(Buscar,100);
            if(Usuario.HasValue && Usuario.Value<=0 || Pagina<1 || Pagina>10000) throw new PilotoException(400,"Selecciona un usuario y una página válidos.");
        }
        private static string Opcion(string valor,string defecto,string[] opciones)
        {
            valor=string.IsNullOrWhiteSpace(valor) ? defecto : valor.Trim();
            if(!opciones.Contains(valor)) throw new PilotoException(400,"Uno de los filtros no es válido.");
            return valor;
        }
        private static string Texto(string valor,int limite)
        {
            valor=(valor??"").Trim();
            if(valor.Length>limite || valor.Any(char.IsControl)) throw new PilotoException(400,"La búsqueda supera el tamaño permitido o contiene caracteres inválidos.");
            return valor;
        }
        public Dictionary<string,object> Valores(string seccion=null,int? pagina=null,string estado=null,string resultado=null,string avance=null)
        {
            return new Dictionary<string,object> {{"Desde",Desde},{"Hasta",Hasta},{"Estado",estado??Estado},{"Liquidacion",Liquidacion},{"Piloto",Piloto},{"Usuario",Usuario},{"Centro",Centro},{"Placa",Placa},
                {"Avance",avance??Avance},{"Resultado",resultado??Resultado},{"Buscar",Buscar},{"Evento",Evento},{"Seccion",seccion??Seccion},{"Pagina",pagina??1}};
        }
    }
    public sealed class PilotoPanelPiloto
    {
        public long Id { get; set; }
        public string Login { get; set; }
        public string Nombre { get; set; }
        public string Empleado { get; set; }
        public string Operador { get; set; }
        public bool CuentaActiva { get; set; }
        public bool RolPiloto { get; set; }
        public bool PermisoVer { get; set; }
        public bool PermisoConfirmar { get; set; }
        public bool VinculoActivo { get; set; }
        public bool EmpleadoActivo { get; set; }
        public bool EmpleadoUnico { get; set; }
        public List<string> Centros { get; set; }
        public List<string> Placas { get; set; }
        public List<string> PlacasDisponibles { get; set; }
        public List<string> Alertas { get; set; }
        public int Rutas { get; set; }
        public PilotoPanelPiloto() { Centros=new List<string>();Placas=new List<string>();PlacasDisponibles=new List<string>();Alertas=new List<string>(); }
        public bool PuedeConsultar { get { return CuentaActiva && RolPiloto && PermisoVer && VinculoActivo && EmpleadoActivo && EmpleadoUnico && !string.IsNullOrWhiteSpace(Operador); } }
        public bool Coincide(PilotoRuta r) { return PuedeConsultar && string.Equals(Empleado,r.Piloto,StringComparison.OrdinalIgnoreCase) && Centros.Contains(r.Centro,StringComparer.OrdinalIgnoreCase) && PlacasDisponibles.Contains(r.Placa,StringComparer.OrdinalIgnoreCase); }
        public void Revisar()
        {
            Alertas.Clear();
            if(!CuentaActiva) Alertas.Add("Cuenta inactiva o sin acceso al sitio.");
            if(!RolPiloto) Alertas.Add("Falta el rol PILOTO.");
            if(!PermisoVer || !PermisoConfirmar) Alertas.Add("Faltan permisos de consulta o confirmación de rutas.");
            if(!VinculoActivo) Alertas.Add("Vínculo de empleado inactivo o ausente.");
            if(string.IsNullOrWhiteSpace(Operador)) Alertas.Add("Falta el código de operador.");
            if(!EmpleadoActivo) Alertas.Add("Empleado no disponible como piloto activo.");
            if(!EmpleadoUnico && !string.IsNullOrWhiteSpace(Empleado)) Alertas.Add("El nombre del empleado es ambiguo en APK66.");
            if(Centros.Count==0) Alertas.Add("No tiene centros autorizados.");
            if(Placas.Count==0) Alertas.Add("No tiene vehículos autorizados.");
            else if(Placas.Except(PlacasDisponibles,StringComparer.OrdinalIgnoreCase).Any()) Alertas.Add("Tiene vehículos autorizados inactivos o ausentes en APK66.");
        }
    }
    public sealed class PilotoPanelCliente
    {
        public int Primero { get; set; }
        public string Nombre { get; set; }
        public string Direccion { get; set; }
        public bool Completado { get; set; }
        public DateTime? FechaUtc { get; set; }
        public bool Foto { get; set; }
        public List<PilotoPanelDocumento> Documentos { get; set; }
        public PilotoPanelCliente() { Documentos=new List<PilotoPanelDocumento>(); }
    }
    public sealed class PilotoPanelDocumento
    {
        public string Ruta { get; set; }
        public PilotoDocumento Datos { get; set; }
        public string Fuente { get; set; }
        public bool Foto { get; set; }
        public string Resultado { get { return string.IsNullOrWhiteSpace(Datos.Entrega) ? "PENDIENTE" : Datos.Entrega.Trim().ToUpperInvariant(); } }
    }
    public sealed class PilotoPanelRuta
    {
        public PilotoRuta Datos { get; set; }
        public bool Liquidada { get; set; }
        public bool VehiculoActivo { get; set; }
        public string OperadorCierre { get; set; }
        public DateTime? FechaCierre { get; set; }
        public DateTime? ActividadUtc { get; set; }
        public List<PilotoPanelPiloto> Pilotos { get; set; }
        public List<PilotoPanelCliente> Clientes { get; set; }
        public List<string> Alertas { get; set; }
        public PilotoPanelRuta() { Pilotos=new List<PilotoPanelPiloto>();Clientes=new List<PilotoPanelCliente>();Alertas=new List<string>(); }
        public int Completados { get { return Clientes.Count(c=>c.Completado); } }
        public int Porcentaje { get { return Clientes.Count==0 ? 0 : Completados*100/Clientes.Count; } }
        public bool ListaParaCierre { get { return Datos.Estado=="E" && !Liquidada && Clientes.Count>0 && Clientes.All(c=>c.Completado); } }
        public IEnumerable<PilotoPanelDocumento> Documentos { get { return Clientes.SelectMany(c=>c.Documentos); } }
    }
    public sealed class PilotoPanelActividad
    {
        public string Tipo { get; set; }
        public string Ruta { get; set; }
        public long Usuario { get; set; }
        public string Actor { get; set; }
        public string Descripcion { get; set; }
        public DateTime FechaUtc { get; set; }
        public string Clave { get; set; }
    }
    public sealed class PilotoPanelModelo
    {
        public PilotoPanelFiltro Filtro { get; set; }
        public bool PuedeAdministrar { get; set; }
        public string Catalogo { get; set; }
        public DateTime ActualizadoUtc { get; set; }
        public List<PilotoPanelRuta> Rutas { get; set; }
        public List<PilotoPanelPiloto> Pilotos { get; set; }
        public List<PilotoPanelActividad> Actividad { get; set; }
        public List<string> Centros { get; set; }
        public List<string> Placas { get; set; }
        public List<string> NombresPiloto { get; set; }
        public PilotoPanelRuta Detalle { get; set; }
        public int TotalFilas { get; set; }
        public List<string> Avisos { get; set; }
        public PilotoPanelModelo() { Rutas=new List<PilotoPanelRuta>();Pilotos=new List<PilotoPanelPiloto>();Actividad=new List<PilotoPanelActividad>();Centros=new List<string>();Placas=new List<string>();NombresPiloto=new List<string>();Avisos=new List<string>(); }
    }
    public static class PilotoPanelReglas
    {
        public const string Permiso="Pilotos.Monitorear";
        public const int TamanoPagina=25;
        public static string Grupo(PilotoDocumento d) { return (d.Cliente??"").Trim().ToUpperInvariant()+"\u001f"+(d.Direccion??"").Trim().ToUpperInvariant(); }
        public static void ExigirAcceso(bool cuenta,bool permiso) { if(!cuenta || !permiso) throw new PilotoException(403,"Necesitas el permiso Pilotos.Monitorear y una cuenta activa para acceder al Panel de distribución."); }
        public static bool AplicarBorrador(PilotoPanelRuta r,long usuario,int primero,string version,string xml,bool completado,DateTime fecha,bool origenVerificado=false)
        {
            if(!origenVerificado || r.Liquidada || r.Datos.Estado!="E" || r.Pilotos.Count!=1 || r.Pilotos[0].Id!=usuario || version!=r.Datos.Version || !completado) return false;
            var cliente=r.Clientes.SingleOrDefault(c=>c.Primero==primero);
            if(cliente==null) return false;
            List<PilotoResultado> resultados;
            try {
                var raiz=XElement.Parse(xml); if(raiz.Name!="documentos") return false;
                resultados=raiz.Elements("d").Select(d=>new PilotoResultado {RowId=int.Parse((string)d.Attribute("id")),Visito=string.IsNullOrEmpty((string)d.Element("visito")) ? (bool?)null : bool.Parse((string)d.Element("visito")),Entrega=(string)d.Element("entrega"),Motivo=(string)d.Element("motivo"),Observaciones=(string)d.Element("observaciones")}).ToList();
                if(!resultados.Select(d=>d.RowId).OrderBy(x=>x).SequenceEqual(cliente.Documentos.Select(d=>d.Datos.RowId).OrderBy(x=>x))) return false;
                foreach(var d in resultados) PilotoReglas.ValidarResultado(d);
            } catch(Exception e) { if(e is FormatException || e is System.Xml.XmlException || e is InvalidOperationException || e is ArgumentException || e is PilotoException || e is OverflowException) return false; throw; }
            foreach(var d in cliente.Documentos) {
                var b=resultados.Single(x=>x.RowId==d.Datos.RowId);
                d.Datos.Visito=b.Visito;d.Datos.Entrega=b.Entrega;d.Datos.Motivo=b.Motivo;d.Datos.ObservacionPiloto=b.Observaciones;d.Fuente="POS · pendiente de cierre";
            }
            cliente.Completado=true;cliente.FechaUtc=fecha;
            if(!r.ActividadUtc.HasValue || r.ActividadUtc<fecha) r.ActividadUtc=fecha;
            return true;
        }
        public static bool Coincide(PilotoPanelRuta r,PilotoPanelFiltro f)
        {
            return (f.Avance=="todos" || f.Avance=="sin" && r.Completados==0 || f.Avance=="parcial" && r.Completados>0 && r.Completados<r.Clientes.Count || f.Avance=="listo" && r.ListaParaCierre) &&
                (f.Resultado=="todos" || r.Documentos.Any(d=>d.Resultado==f.Resultado)) &&
                (!f.Usuario.HasValue || r.Pilotos.Any(p=>p.Id==f.Usuario)) &&
                (string.IsNullOrEmpty(f.Buscar) || Contiene(r.Datos.Id,f.Buscar) || r.Documentos.Any(d=>Contiene(d.Datos.Documento,f.Buscar) || Contiene(d.Datos.Cliente,f.Buscar)));
        }
        public static bool Contiene(string valor,string buscar) { return (valor??"").IndexOf(buscar??"",StringComparison.OrdinalIgnoreCase)>=0; }
        public static string Csv(string valor)
        {
            valor=valor??"";
            if(valor.TrimStart().Length>0 && "=+-@".Contains(valor.TrimStart()[0])) valor="'"+valor;
            return "\""+valor.Replace("\"","\"\"")+"\"";
        }
        public static DateTime HoraGuatemala(DateTime utc) { return DateTime.SpecifyKind(utc,DateTimeKind.Utc).AddHours(-6); }
    }
}
