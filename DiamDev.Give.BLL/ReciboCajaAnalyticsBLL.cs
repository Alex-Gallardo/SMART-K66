using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using Newtonsoft.Json;

namespace DiamDev.Give.BLL
{
    public class ReciboCajaAnalyticsBLL
    {
        private readonly AnalyticsRecibosDA _da = new AnalyticsRecibosDA();

        // ═══════════════════════════════════════════════════════
        //  PERIODOS
        // ═══════════════════════════════════════════════════════
        /// <summary>
        /// Traduce el preset del front a un rango real. Se resuelve EN EL
        /// SERVIDOR a propósito: si lo calculara el JS, el rango dependería
        /// del reloj de la PC del usuario y dos personas verían números
        /// distintos con el mismo filtro.
        ///
        /// FechaFin es EXCLUSIVA (mañana a las 00:00) para que un recibo
        /// creado hoy a las 16:30 entre en "hoy". Comparar con "<= hoy"
        /// significa "<= hoy 00:00:00" y se perdería todo el día.
        /// </summary>
        public AnalyticsFiltro ResolverPeriodo(string preset, string empresa)
        {
            var f = new AnalyticsFiltro { Empresa = (empresa ?? "").Trim() };
            DateTime hoy = DateTime.Today;

            switch ((preset ?? "7d").ToLowerInvariant())
            {
                case "7d":
                    f.FechaIni = hoy.AddDays(-6);
                    f.FechaFin = hoy.AddDays(1);
                    break;
                case "mes":
                    f.FechaIni = new DateTime(hoy.Year, hoy.Month, 1);
                    f.FechaFin = hoy.AddDays(1);
                    break;
                case "todo":
                    f.FechaIni = null;
                    f.FechaFin = null;
                    break;
                default:
                    f.FechaIni = hoy.AddDays(-6);
                    f.FechaFin = hoy.AddDays(1);
                    break;
            }
            return f;
        }

        public AnalyticsPaquete Obtener(string preset, string empresa)
        {
            return _da.ObtenerPaquete(ResolverPeriodo(preset, empresa));
        }

        // ═══════════════════════════════════════════════════════
        //  GEOLOCALIZACIÓN
        // ═══════════════════════════════════════════════════════
        //  Servicio: ip-api.com, endpoint /batch (hasta 100 IPs por llamada).
        //  Plan gratuito: HTTP (no HTTPS) y rate limit por minuto. Por eso
        //  esto NO corre automático: lo dispara un botón, una vez, y el
        //  resultado queda cacheado en analyticsGeoIp para siempre.
        //
        //  Configurable en Web.config (opcional):
        //    <add key="GeoIpUrl" value="http://ip-api.com/batch" />
        //    <add key="GeoIpMaxLote" value="100" />
        // ═══════════════════════════════════════════════════════

        private const string URL_DEFAULT = "http://ip-api.com/batch";
        private const string CAMPOS =
            "?fields=status,message,country,countryCode,regionName,city,lat,lon,isp,org,mobile,proxy,hosting,query";

        private class IpApiRespuesta
        {
            [JsonProperty("status")] public string Status { get; set; }
            [JsonProperty("message")] public string Message { get; set; }
            [JsonProperty("country")] public string Country { get; set; }
            [JsonProperty("countryCode")] public string CountryCode { get; set; }
            [JsonProperty("regionName")] public string RegionName { get; set; }
            [JsonProperty("city")] public string City { get; set; }
            [JsonProperty("lat")] public decimal? Lat { get; set; }
            [JsonProperty("lon")] public decimal? Lon { get; set; }
            [JsonProperty("isp")] public string Isp { get; set; }
            [JsonProperty("org")] public string Org { get; set; }
            [JsonProperty("mobile")] public bool Mobile { get; set; }
            [JsonProperty("proxy")] public bool Proxy { get; set; }
            [JsonProperty("hosting")] public bool Hosting { get; set; }
            [JsonProperty("query")] public string Query { get; set; }
        }

        /// <summary>
        /// Una IP privada (192.168.x, 10.x, 172.16-31.x, 127.x, 169.254.x)
        /// no tiene geolocalización posible. La marcamos sin gastar una
        /// llamada al servicio, y como queda cacheada nunca se reintenta.
        /// </summary>
        private static bool EsPrivada(string ip)
        {
            IPAddress addr;
            if (!IPAddress.TryParse(ip, out addr)) return true;
            if (IPAddress.IsLoopback(addr)) return true;

            byte[] b = addr.GetAddressBytes();
            if (b.Length != 4) return false;               // IPv6 → que decida la API
            if (b[0] == 10) return true;
            if (b[0] == 127) return true;
            if (b[0] == 192 && b[1] == 168) return true;
            if (b[0] == 172 && b[1] >= 16 && b[1] <= 31) return true;
            if (b[0] == 169 && b[1] == 254) return true;
            return false;
        }

        private static int LeerInt(string clave, int porDefecto)
        {
            int v;
            return int.TryParse(ConfigurationManager.AppSettings[clave], out v) && v > 0
                ? v : porDefecto;
        }

        /// <summary>
        /// Resuelve las IPs que aún no están en caché. Idempotente: correrlo
        /// dos veces seguidas no hace nada la segunda vez.
        /// </summary>
        public GeoResultado ResolverPendientes()
        {
            var res = new GeoResultado();
            int maxLote = LeerInt("GeoIpMaxLote", 100);
            if (maxLote > 100) maxLote = 100;             // tope duro del endpoint

            List<string> ips;
            try
            {
                ips = _da.ObtenerIpsPendientes(maxLote);
            }
            catch (Exception ex)
            {
                res.Mensaje = "No se pudo leer las IPs pendientes: " + ex.Message;
                return res;
            }

            res.Pendientes = ips.Count;
            if (ips.Count == 0)
            {
                res.Exito = true;
                res.Mensaje = "No hay IPs pendientes: todas están resueltas.";
                return res;
            }

            var aGuardar = new List<GeoIp>();

            // Privadas: se resuelven localmente, sin red.
            var privadas = ips.Where(EsPrivada).ToList();
            foreach (var ip in privadas)
                aGuardar.Add(new GeoIp
                {
                    Ip = ip,
                    Estado = "PRIVADA",
                    Mensaje = "Rango privado o loopback: sin geolocalización pública.",
                    Origen = "local"
                });
            res.Privadas = privadas.Count;

            var publicas = ips.Where(i => !EsPrivada(i)).ToList();

            if (publicas.Count > 0)
            {
                try
                {
                    string url = (ConfigurationManager.AppSettings["GeoIpUrl"] ?? URL_DEFAULT) + CAMPOS;

                    // Patrón síncrono del proyecto: HttpClient + GetAwaiter().GetResult().
                    // El timeout corto es deliberado: si el servidor no tiene
                    // salida a internet, quiero fallar en 20s, no colgar el App Pool.
                    using (var client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromSeconds(20);
                        var body = new StringContent(
                            JsonConvert.SerializeObject(publicas), Encoding.UTF8, "application/json");

                        HttpResponseMessage resp = client.PostAsync(url, body)
                                                        .GetAwaiter().GetResult();
                        resp.EnsureSuccessStatusCode();

                        string json = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        var lista = JsonConvert.DeserializeObject<List<IpApiRespuesta>>(json)
                                    ?? new List<IpApiRespuesta>();

                        foreach (var r in lista)
                        {
                            bool ok = "success".Equals(r.Status, StringComparison.OrdinalIgnoreCase);
                            aGuardar.Add(new GeoIp
                            {
                                Ip = r.Query,
                                Pais = ok ? r.Country : null,
                                CodigoPais = ok ? r.CountryCode : null,
                                Region = ok ? r.RegionName : null,
                                Ciudad = ok ? r.City : null,
                                Latitud = ok ? r.Lat : null,
                                Longitud = ok ? r.Lon : null,
                                Isp = ok ? r.Isp : null,
                                Organizacion = ok ? r.Org : null,
                                EsMovil = ok && r.Mobile,
                                EsProxy = ok && r.Proxy,
                                EsHosting = ok && r.Hosting,
                                Estado = ok ? "OK" : "FALLO",
                                Mensaje = ok ? null : (r.Message ?? "El servicio no resolvió la IP."),
                                Origen = "ip-api"
                            });
                            if (ok) res.Resueltas++; else res.Fallidas++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Falla de red: guardamos lo que sí tenemos (las privadas) y
                    // devolvemos el motivo. NO marcamos las públicas como FALLO:
                    // si las cacheáramos, no se reintentarían nunca más.
                    if (aGuardar.Count > 0)
                    { try { _da.GuardarGeo(aGuardar); } catch { } }

                    res.Mensaje = "No se pudo contactar el servicio de geolocalización. " +
                                  "Verificá que el servidor tenga salida a internet por el puerto 80. " +
                                  "Detalle: " + ex.Message;
                    return res;
                }
            }

            try
            {
                _da.GuardarGeo(aGuardar);
            }
            catch (Exception ex)
            {
                res.Mensaje = "Se resolvieron las IPs pero falló el guardado: " + ex.Message;
                return res;
            }

            res.Exito = true;
            res.Mensaje = string.Format(
                "{0} resuelta(s), {1} privada(s), {2} sin resolver.",
                res.Resueltas, res.Privadas, res.Fallidas);
            return res;
        }



        // ═══════════════════════════════════════════════════════
        //  REGISTRO DE EVENTOS
        // ═══════════════════════════════════════════════════════
        //  Wrapper sobre APK66Context.RegistrarEventoAnalytics, que ya existe
        //  y ya es a prueba de balas (traga sus propias excepciones: un fallo
        //  de log NUNCA debe tumbar la operación que lo generó).
        //
        //  Acá agregamos lo que el DAL no sabe: armar el payload y resolver
        //  el depto. Todo dentro de try/catch por la misma razón.

        /// <summary>
        /// Evento con recibo completo en mano (IMPRESO, EDITADO).
        /// El payload es un resumen, NO el snapshot entero: el snapshot ya
        /// quedó guardado en el evento CREADO y duplicarlo solo infla la tabla.
        /// </summary>
        public void RegistrarEvento(string evento, ReciboCajaEncabezado rec,
                                    long usuarioId, string login, string ip,
                                    string detalle = null)
        {
            if (rec == null) return;
            try
            {

                string payload = JsonConvert.SerializeObject(new
                {
                    rec.NombreCliente,
                    rec.IdCliente,
                    rec.CodigoUsuario,
                    rec.Status,
                    rec.SyncEstado,
                    MontoTRec = rec.MontoTotalRecibo,
                    Detalle = detalle
                });

                using (var db = new APK66Context())
                {
                    // El depto se resuelve con la MISMA conexión del pool que el
                    // insert, en vez de abrir una aparte solo para la consulta.
                    string depto = db.ObtenerDeptoDeRecibo(rec.IdRecibo, rec.IdEmpresa);

                    db.RegistrarEventoAnalytics(
                        evento, rec.IdRecibo, rec.IdEmpresa,
                        string.IsNullOrEmpty(depto) ? null : depto,
                        usuarioId, login,
                        rec.Moneda, rec.TipoCambio,
                        rec.MontoTotalRecGtq, rec.MontoTotalRecUsd, rec.SaldoGtq,
                        payload, ip);
                }
            }
            catch { /* el log nunca tumba la operación */ }
        }

        /// <summary>
        /// Evento sin recibo (ERROR_GUARDADO): el recibo no llegó a existir.
        /// Por eso IdRecibo va NULL y los montos en 0 — no hay nada que medir,
        /// lo que interesa es el motivo y quién lo provocó.
        /// </summary>
        public void RegistrarEventoSimple(string evento, string idEmpresa,
                                          long usuarioId, string login, string ip,
                                          string motivo, object contexto = null)
        {
            try
            {
                string payload = JsonConvert.SerializeObject(new
                {
                    Motivo = (motivo ?? "").Length > 900
                             ? motivo.Substring(0, 900) : motivo,
                    Contexto = contexto
                });

                using (var db = new APK66Context())
                {
                    db.RegistrarEventoAnalytics(
                        evento, null, idEmpresa, null,
                        usuarioId, login,
                        null, null, 0m, 0m, 0m,
                        payload, ip);
                }
            }
            catch { }
        }
    }
}