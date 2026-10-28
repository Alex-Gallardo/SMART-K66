using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DiamDev.Give.Entities
{
    public sealed class PilotoLiquidacionFiltro
    {
        public string Desde { get; set; }
        public string Hasta { get; set; }
        public string Buscar { get; set; }
        public bool VerLiquidadas { get; set; }
        public int Pagina { get; set; }
        public DateTime FechaDesde { get; private set; }
        public DateTime FechaFin { get; private set; }

        public void Validar(DateTime hoy)
        {
            if(string.IsNullOrWhiteSpace(Desde)) Desde=hoy.AddDays(-30).ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
            if(string.IsNullOrWhiteSpace(Hasta)) Hasta=hoy.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
            DateTime inicio,fin;
            if(!DateTime.TryParseExact(Desde,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out inicio) ||
               !DateTime.TryParseExact(Hasta,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out fin) ||
               fin<inicio || (fin-inicio).TotalDays>366 || fin==DateTime.MaxValue.Date)
                throw new PilotoException(400,"Selecciona un período válido de hasta 366 días.");
            FechaDesde=inicio;FechaFin=fin.AddDays(1);
            Buscar=(Buscar??"").Trim();
            if(Buscar.Length>80) throw new PilotoException(400,"La búsqueda admite hasta 80 caracteres.");
            if(Pagina==0) Pagina=1;
            if(Pagina<1 || Pagina>1000) throw new PilotoException(400,"Selecciona una página válida.");
        }
    }

    public sealed class PilotoLiquidacionDocumento
    {
        public string RutaId { get; set; }
        public int RowId { get; set; }
        public string Tipo { get; set; }
        public string Numero { get; set; }
        public string Cliente { get; set; }
        public string Direccion { get; set; }
        public decimal Bultos { get; set; }
        public string Resultado { get; set; }
        public string Motivo { get; set; }
        public string Observacion { get; set; }
        public string UsuarioLiquida { get; set; }
        public DateTime? FechaLiquida { get; set; }
        public bool Liquidado { get { return !string.IsNullOrWhiteSpace(UsuarioLiquida) && FechaLiquida.HasValue; } }
        public bool Inconsistente { get { return string.IsNullOrWhiteSpace(UsuarioLiquida)!=!FechaLiquida.HasValue; } }
    }

    public sealed class PilotoLiquidacionRuta
    {
        public string Id { get; set; }
        public DateTime Fecha { get; set; }
        public string Estado { get; set; }
        public bool Liquidada { get; set; }
        public string Piloto { get; set; }
        public string Placa { get; set; }
        public string Centro { get; set; }
        public TimeSpan? HoraSalida { get; set; }
        public TimeSpan? HoraEntrada { get; set; }
        public string UsuarioLiquida { get; set; }
        public DateTime? FechaLiquida { get; set; }
        public int TotalDocumentos { get; set; }
        public int DocumentosLiquidados { get; set; }
        public int DocumentosConResultado { get; set; }
        public int DocumentosInconsistentes { get; set; }
        public List<PilotoLiquidacionDocumento> Documentos { get; set; }
        public PilotoLiquidacionRuta() { Documentos=new List<PilotoLiquidacionDocumento>(); }
        public bool PuedeLiquidarDocumentos { get { return Estado=="C" && !Liquidada && HoraEntrada.HasValue && TotalDocumentos>0 && DocumentosInconsistentes==0; } }
        public bool PuedeLiquidarRuta { get { return PuedeLiquidarDocumentos && DocumentosConResultado==TotalDocumentos && DocumentosLiquidados==TotalDocumentos; } }
    }

    public sealed class PilotoLiquidacionLista
    {
        public PilotoLiquidacionFiltro Filtro { get; set; }
        public List<PilotoLiquidacionRuta> Rutas { get; set; }
        public int Total { get; set; }
        public PilotoLiquidacionLista() { Rutas=new List<PilotoLiquidacionRuta>(); }
    }

    public sealed class PilotoLiquidacionReporte
    {
        public List<PilotoLiquidacionRuta> Rutas { get; set; }
        public PilotoLiquidacionReporte() { Rutas=new List<PilotoLiquidacionRuta>(); }
    }

    public static class PilotoLiquidacionReglas
    {
        public const string Permiso="Pilotos.Liquidar";
        public static string Id(string id)
        {
            id=(id??"").Trim();
            if(id.Length==0 || id.Length>15) throw new PilotoException(400,"Selecciona una ruta válida.");
            return id;
        }
        public static int[] Filas(int[] ids)
        {
            var unicos=(ids??new int[0]).Distinct().ToArray();
            if(unicos.Length==0 || unicos.Length>100 || unicos.Any(x=>x<=0)) throw new PilotoException(400,"Selecciona entre 1 y 100 documentos.");
            return unicos;
        }
        public static string[] Rutas(string[] ids)
        {
            var unicas=(ids??new string[0]).Select(Id).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if(unicas.Length==0 || unicas.Length>100) throw new PilotoException(400,"Selecciona entre 1 y 100 rutas.");
            return unicas;
        }
        public static void ValidarDocumentos(PilotoLiquidacionRuta ruta)
        {
            if(ruta==null || !ruta.PuedeLiquidarDocumentos)
                throw new PilotoException(409,"La ruta debe estar Cerrada, con entrada de Garita y sin datos de liquidación inconsistentes.");
        }
        public static void ValidarRuta(PilotoLiquidacionRuta ruta)
        {
            ValidarDocumentos(ruta);
            if(!ruta.PuedeLiquidarRuta) throw new PilotoException(409,"Todos los documentos deben tener resultado y estar liquidados antes de liquidar la ruta.");
        }
    }
}
