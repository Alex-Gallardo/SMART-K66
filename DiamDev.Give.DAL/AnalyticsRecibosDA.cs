using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using DiamDev.Give.Entities;

namespace DiamDev.Give.DAL
{
    /// <summary>
    /// Acceso a datos de la vista Analytics. ADO.NET puro (consulta analítica
    /// pesada, agrupamientos y JSON_VALUE: EF6 no aporta nada acá).
    ///
    /// ⚠️ Verificá que CONN coincida con el nombre que usa ReciboCajaAdminDA.
    /// Si tu proyecto lo llama distinto, cambialo SOLO en esta constante.
    /// </summary>
    public class AnalyticsRecibosDA
    {
        private const string CONN = "RecibosContext";

        private static string Cadena()
        {
            var cs = ConfigurationManager.ConnectionStrings[CONN];
            if (cs == null)
                throw new Exception("No existe la connection string '" + CONN + "' en Web.config.");
            return cs.ConnectionString;
        }

        // ── Lectores defensivos ────────────────────────────────
        // Equivalente C# de `data?.campo ?? valorPorDefecto` en TS.
        private static string S(IDataRecord r, string c)
        {
            int i = r.GetOrdinal(c);
            return r.IsDBNull(i) ? "" : Convert.ToString(r.GetValue(i));
        }
        private static int I(IDataRecord r, string c)
        {
            int i = r.GetOrdinal(c);
            return r.IsDBNull(i) ? 0 : Convert.ToInt32(r.GetValue(i));
        }
        private static decimal D(IDataRecord r, string c)
        {
            int i = r.GetOrdinal(c);
            return r.IsDBNull(i) ? 0m : Convert.ToDecimal(r.GetValue(i));
        }
        private static decimal? DN(IDataRecord r, string c)
        {
            int i = r.GetOrdinal(c);
            return r.IsDBNull(i) ? (decimal?)null : Convert.ToDecimal(r.GetValue(i));
        }
        private static DateTime? FN(IDataRecord r, string c)
        {
            int i = r.GetOrdinal(c);
            return r.IsDBNull(i) ? (DateTime?)null : Convert.ToDateTime(r.GetValue(i));
        }
        private static bool B(IDataRecord r, string c)
        {
            int i = r.GetOrdinal(c);
            return !r.IsDBNull(i) && Convert.ToBoolean(r.GetValue(i));
        }

        // ═══════════════════════════════════════════════════════
        //  CONSULTA PRINCIPAL — 7 result sets en un solo viaje
        // ═══════════════════════════════════════════════════════
        public AnalyticsPaquete ObtenerPaquete(AnalyticsFiltro f)
        {
            var p = new AnalyticsPaquete();

            const string SQL = @"
SET NOCOUNT ON;

/* ── #ev: universo filtrado UNA sola vez ──────────────────────────
   Normalizo nulos acá para no repetir ISNULL en siete SELECTs.
   PayloadJson NO se copia (es nvarchar(max)): para las anulaciones
   hago JOIN de vuelta por Id, que son 5 filas, no 350. */
SELECT  a.Id,
        a.Evento,
        a.IdRecibo,
        a.IdEmpresa,
        Depto        = ISNULL(NULLIF(LTRIM(RTRIM(a.Depto)),''), '(sin depto)'),
        UsuarioLogin = ISNULL(NULLIF(LTRIM(RTRIM(a.UsuarioLogin)),''), '(sin usuario)'),
        Moneda       = ISNULL(NULLIF(LTRIM(RTRIM(a.Moneda)),''), '(sin moneda)'),
        MontoGtq     = ISNULL(a.MontoGtq, 0),
        MontoUsd     = ISNULL(a.MontoUsd, 0),
        a.IpUsuario,
        a.FechaEvento
INTO    #ev
FROM    dbo.analyticsRecibos a
WHERE   (@fIni IS NULL OR a.FechaEvento >= @fIni)
  AND   (@fFin IS NULL OR a.FechaEvento <  @fFin)
  AND   (@empresa = '' OR a.IdEmpresa = @empresa);

CREATE NONCLUSTERED INDEX IX_ev ON #ev (Evento) INCLUDE (IdEmpresa, UsuarioLogin, MontoGtq);

/* ── RS1 · Resumen ─────────────────────────────────────────────── */
SELECT
  Creados         = SUM(CASE WHEN Evento='CREADO'  THEN 1 ELSE 0 END),
  Anulados        = SUM(CASE WHEN Evento='ANULADO' THEN 1 ELSE 0 END),
  MontoCreado     = SUM(CASE WHEN Evento='CREADO'  THEN MontoGtq ELSE 0 END),
  MontoAnulado    = SUM(CASE WHEN Evento='ANULADO' THEN MontoGtq ELSE 0 END),
  UsuariosActivos = COUNT(DISTINCT CASE WHEN Evento='CREADO' THEN UsuarioLogin END),
  DeptosActivos   = COUNT(DISTINCT CASE WHEN Evento='CREADO' THEN Depto END),
  IpsDistintas    = COUNT(DISTINCT IpUsuario),
  EnUsd           = SUM(CASE WHEN Evento='CREADO' AND Moneda='USD' THEN 1 ELSE 0 END),
  Impresos        = SUM(CASE WHEN Evento='IMPRESO'          THEN 1 ELSE 0 END),
  Editados        = SUM(CASE WHEN Evento='EDITADO'          THEN 1 ELSE 0 END),
  Errores         = SUM(CASE WHEN Evento='ERROR_GUARDADO'   THEN 1 ELSE 0 END),
  Rechazos        = SUM(CASE WHEN Evento='RECHAZO_GUARDADO' THEN 1 ELSE 0 END),
  MonedaRara      = SUM(CASE WHEN Evento='CREADO' AND Moneda NOT IN ('GTQ','USD') THEN 1 ELSE 0 END),
  MontoMonedaRara = SUM(CASE WHEN Evento='CREADO' AND Moneda NOT IN ('GTQ','USD') THEN MontoGtq ELSE 0 END),
  Primero         = MIN(FechaEvento),
  Ultimo          = MAX(FechaEvento)
FROM #ev;

/* ── RS2 · Serie diaria por empresa ────────────────────────────── */
SELECT  Dia     = CAST(FechaEvento AS date),
        IdEmpresa,
        Recibos = COUNT(*),
        Monto   = SUM(MontoGtq)
FROM    #ev
WHERE   Evento = 'CREADO'
GROUP BY CAST(FechaEvento AS date), IdEmpresa
ORDER BY 1, 2;

/* ── RS3 · Ranking de usuarios ─────────────────────────────────── */
SELECT  UsuarioLogin,
        Depto       = MAX(Depto),
        Deptos      = COUNT(DISTINCT Depto),
        Creados     = SUM(CASE WHEN Evento='CREADO'  THEN 1 ELSE 0 END),
        MontoCreado = SUM(CASE WHEN Evento='CREADO'  THEN MontoGtq ELSE 0 END),
        Anulados    = SUM(CASE WHEN Evento='ANULADO' THEN 1 ELSE 0 END),
        Empresas    = COUNT(DISTINCT IdEmpresa),
        Ultimo      = MAX(FechaEvento)
FROM    #ev
GROUP BY UsuarioLogin
ORDER BY MontoCreado DESC;

/* ── RS4 · Distribución por empresa ────────────────────────────── */
SELECT  IdEmpresa,
        Recibos = COUNT(*),
        Monto   = SUM(MontoGtq)
FROM    #ev
WHERE   Evento = 'CREADO'
GROUP BY IdEmpresa
ORDER BY 3 DESC;

/* ── RS5 · Mapa de calor hora × día de semana ───────────────────
   (DATEDIFF(DAY,0,fecha) % 7) da 0=lunes SIEMPRE, sin depender de
   @@DATEFIRST, que cambia según el idioma del login. DATEPART(WEEKDAY)
   habría dado resultados distintos en tu SSMS y en el App Pool. */
SELECT  DiaSemana = (DATEDIFF(DAY, 0, FechaEvento) % 7),
        Hora      = DATEPART(HOUR, FechaEvento),
        Recibos   = COUNT(*)
FROM    #ev
WHERE   Evento = 'CREADO'
GROUP BY (DATEDIFF(DAY, 0, FechaEvento) % 7), DATEPART(HOUR, FechaEvento)
ORDER BY 1, 2;

/* ── RS6 · Anulaciones con motivo extraído del JSON ─────────────
   ISJSON() como guardia: si algún payload viejo no es JSON válido,
   JSON_VALUE reventaría toda la consulta. Con el guardia devuelve NULL. */
SELECT  e.IdRecibo, e.IdEmpresa, e.UsuarioLogin, e.Depto,
        e.MontoGtq, e.FechaEvento,
        Motivo         = CASE WHEN ISJSON(a.PayloadJson)=1
                              THEN JSON_VALUE(a.PayloadJson, '$.Motivo') END,
        EstadoAlAnular = CASE WHEN ISJSON(a.PayloadJson)=1
                              THEN JSON_VALUE(a.PayloadJson, '$.SyncEstadoAlAnular') END
FROM    #ev e
JOIN    dbo.analyticsRecibos a ON a.Id = e.Id
WHERE   e.Evento = 'ANULADO'
ORDER BY e.FechaEvento DESC;

/* ── RS7 · Accesos por IP + geolocalización cacheada ────────────
   LEFT JOIN: si la IP nunca se resolvió, las columnas geo vienen NULL
   y la vista la pinta como 'Sin resolver'. Nada se rompe. */
SELECT  e.IpUsuario,
        Eventos       = COUNT(*),
        Usuarios      = COUNT(DISTINCT e.UsuarioLogin),
        ListaUsuarios = STUFF((SELECT DISTINCT ', ' + x.UsuarioLogin
                               FROM #ev x
                               WHERE x.IpUsuario = e.IpUsuario
                               FOR XML PATH('')), 1, 2, ''),
        Primero       = MIN(e.FechaEvento),
        Ultimo        = MAX(e.FechaEvento),
        g.Pais, g.CodigoPais, g.Region, g.Ciudad,
        g.Latitud, g.Longitud, g.Isp, g.EsMovil, g.EsProxy,
        EstadoGeo     = g.Estado
FROM    #ev e
LEFT JOIN dbo.analyticsGeoIp g ON g.Ip = e.IpUsuario
WHERE   e.IpUsuario IS NOT NULL AND LTRIM(RTRIM(e.IpUsuario)) <> ''
GROUP BY e.IpUsuario, g.Pais, g.CodigoPais, g.Region, g.Ciudad,
         g.Latitud, g.Longitud, g.Isp, g.EsMovil, g.EsProxy, g.Estado
ORDER BY COUNT(*) DESC;

DROP TABLE #ev;";

            using (var cn = new SqlConnection(Cadena()))
            using (var cmd = new SqlCommand(SQL, cn))
            {
                cmd.CommandTimeout = 60;
                cmd.Parameters.Add("@fIni", SqlDbType.DateTime2).Value =
                    (object)f.FechaIni ?? DBNull.Value;
                cmd.Parameters.Add("@fFin", SqlDbType.DateTime2).Value =
                    (object)f.FechaFin ?? DBNull.Value;
                cmd.Parameters.Add("@empresa", SqlDbType.NVarChar, 15).Value = f.Empresa ?? "";

                cn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    // RS1 · Resumen
                    if (rd.Read())
                    {
                        var r = p.Resumen;
                        r.Creados = I(rd, "Creados");
                        r.Anulados = I(rd, "Anulados");
                        r.MontoCreado = D(rd, "MontoCreado");
                        r.MontoAnulado = D(rd, "MontoAnulado");
                        r.UsuariosActivos = I(rd, "UsuariosActivos");
                        r.DeptosActivos = I(rd, "DeptosActivos");
                        r.IpsDistintas = I(rd, "IpsDistintas");
                        r.EnUsd = I(rd, "EnUsd");
                        r.Impresos = I(rd, "Impresos");
                        r.Editados = I(rd, "Editados");
                        r.Errores = I(rd, "Errores");
                        r.Rechazos = I(rd, "Rechazos");
                        r.MonedaRara = I(rd, "MonedaRara");
                        r.MontoMonedaRara = D(rd, "MontoMonedaRara");
                        r.Primero = FN(rd, "Primero");
                        r.Ultimo = FN(rd, "Ultimo");

                        r.TicketPromedio = r.Creados > 0
                            ? Math.Round(r.MontoCreado / r.Creados, 2) : 0m;
                        r.TasaAnulacion = r.Creados > 0
                            ? Math.Round((decimal)r.Anulados * 100m / r.Creados, 2) : 0m;
                    }

                    // RS2 · Serie
                    rd.NextResult();
                    while (rd.Read())
                        p.Serie.Add(new AnalyticsSerieDia
                        {
                            Dia = Convert.ToDateTime(rd["Dia"]),
                            IdEmpresa = S(rd, "IdEmpresa"),
                            Recibos = I(rd, "Recibos"),
                            Monto = D(rd, "Monto")
                        });

                    // RS3 · Usuarios
                    rd.NextResult();
                    while (rd.Read())
                    {
                        int creados = I(rd, "Creados");
                        decimal monto = D(rd, "MontoCreado");
                        p.Usuarios.Add(new AnalyticsUsuario
                        {
                            UsuarioLogin = S(rd, "UsuarioLogin"),
                            Depto = S(rd, "Depto"),
                            Deptos = I(rd, "Deptos"),
                            Creados = creados,
                            MontoCreado = monto,
                            Anulados = I(rd, "Anulados"),
                            Empresas = I(rd, "Empresas"),
                            Ticket = creados > 0 ? Math.Round(monto / creados, 2) : 0m,
                            Ultimo = FN(rd, "Ultimo")
                        });
                    }

                    // RS4 · Empresas
                    rd.NextResult();
                    while (rd.Read())
                        p.Empresas.Add(new AnalyticsEmpresa
                        {
                            IdEmpresa = S(rd, "IdEmpresa"),
                            Recibos = I(rd, "Recibos"),
                            Monto = D(rd, "Monto")
                        });

                    // RS5 · Heatmap
                    rd.NextResult();
                    while (rd.Read())
                        p.Heat.Add(new AnalyticsHeatCelda
                        {
                            DiaSemana = I(rd, "DiaSemana"),
                            Hora = I(rd, "Hora"),
                            Recibos = I(rd, "Recibos")
                        });

                    // RS6 · Anulaciones
                    rd.NextResult();
                    while (rd.Read())
                        p.Anulaciones.Add(new AnalyticsAnulacion
                        {
                            IdRecibo = S(rd, "IdRecibo"),
                            IdEmpresa = S(rd, "IdEmpresa"),
                            UsuarioLogin = S(rd, "UsuarioLogin"),
                            Depto = S(rd, "Depto"),
                            MontoGtq = D(rd, "MontoGtq"),
                            FechaEvento = Convert.ToDateTime(rd["FechaEvento"]),
                            Motivo = S(rd, "Motivo"),
                            EstadoAlAnular = S(rd, "EstadoAlAnular")
                        });

                    // RS7 · Accesos
                    rd.NextResult();
                    while (rd.Read())
                        p.Accesos.Add(new AnalyticsAcceso
                        {
                            Ip = S(rd, "IpUsuario"),
                            Eventos = I(rd, "Eventos"),
                            Usuarios = I(rd, "Usuarios"),
                            ListaUsuarios = S(rd, "ListaUsuarios"),
                            Primero = FN(rd, "Primero"),
                            Ultimo = FN(rd, "Ultimo"),
                            Pais = S(rd, "Pais"),
                            CodigoPais = S(rd, "CodigoPais"),
                            Region = S(rd, "Region"),
                            Ciudad = S(rd, "Ciudad"),
                            Latitud = DN(rd, "Latitud"),
                            Longitud = DN(rd, "Longitud"),
                            Isp = S(rd, "Isp"),
                            EsMovil = B(rd, "EsMovil"),
                            EsProxy = B(rd, "EsProxy"),
                            EstadoGeo = S(rd, "EstadoGeo")
                        });
                }
            }

            return p;
        }

        // ═══════════════════════════════════════════════════════
        //  GEO — caché de IPs
        // ═══════════════════════════════════════════════════════

        /// <summary>IPs presentes en la bitácora que aún NO tienen fila en la caché.</summary>
        public List<string> ObtenerIpsPendientes(int tope)
        {
            var lista = new List<string>();
            const string SQL = @"
SELECT TOP (@tope) a.IpUsuario
FROM   dbo.analyticsRecibos a
LEFT   JOIN dbo.analyticsGeoIp g ON g.Ip = a.IpUsuario
WHERE  a.IpUsuario IS NOT NULL
  AND  LTRIM(RTRIM(a.IpUsuario)) <> ''
  AND  g.Ip IS NULL
GROUP BY a.IpUsuario
ORDER BY COUNT(*) DESC;";

            using (var cn = new SqlConnection(Cadena()))
            using (var cmd = new SqlCommand(SQL, cn))
            {
                cmd.Parameters.Add("@tope", SqlDbType.Int).Value = tope;
                cn.Open();
                using (var rd = cmd.ExecuteReader())
                    while (rd.Read()) lista.Add(Convert.ToString(rd[0]));
            }
            return lista;
        }

        /// <summary>
        /// Guarda (o refresca) resoluciones. MERGE en vez de INSERT: si dos
        /// usuarios aprietan "Resolver" al mismo tiempo, el segundo actualiza
        /// en lugar de reventar por violación de PK.
        /// </summary>
        public int GuardarGeo(List<GeoIp> filas)
        {
            if (filas == null || filas.Count == 0) return 0;

            const string SQL = @"
MERGE dbo.analyticsGeoIp AS d
USING (SELECT @ip AS Ip) AS s ON d.Ip = s.Ip
WHEN MATCHED THEN UPDATE SET
     Pais=@pais, CodigoPais=@cp, Region=@reg, Ciudad=@ciu,
     Latitud=@lat, Longitud=@lon, Isp=@isp, Organizacion=@org,
     EsMovil=@mov, EsProxy=@pro, EsHosting=@hos,
     Estado=@est, Mensaje=@msg, Origen=@ori, FechaResuelta=SYSDATETIME()
WHEN NOT MATCHED THEN INSERT
     (Ip,Pais,CodigoPais,Region,Ciudad,Latitud,Longitud,Isp,Organizacion,
      EsMovil,EsProxy,EsHosting,Estado,Mensaje,Origen)
     VALUES (@ip,@pais,@cp,@reg,@ciu,@lat,@lon,@isp,@org,
             @mov,@pro,@hos,@est,@msg,@ori);";

            int n = 0;
            using (var cn = new SqlConnection(Cadena()))
            {
                cn.Open();
                using (var tx = cn.BeginTransaction())
                using (var cmd = new SqlCommand(SQL, cn, tx))
                {
                    cmd.Parameters.Add("@ip", SqlDbType.NVarChar, 45);
                    cmd.Parameters.Add("@pais", SqlDbType.NVarChar, 60);
                    cmd.Parameters.Add("@cp", SqlDbType.NVarChar, 5);
                    cmd.Parameters.Add("@reg", SqlDbType.NVarChar, 80);
                    cmd.Parameters.Add("@ciu", SqlDbType.NVarChar, 80);
                    cmd.Parameters.Add("@lat", SqlDbType.Decimal).Precision = 9;
                    cmd.Parameters["@lat"].Scale = 6;
                    cmd.Parameters.Add("@lon", SqlDbType.Decimal).Precision = 9;
                    cmd.Parameters["@lon"].Scale = 6;
                    cmd.Parameters.Add("@isp", SqlDbType.NVarChar, 150);
                    cmd.Parameters.Add("@org", SqlDbType.NVarChar, 150);
                    cmd.Parameters.Add("@mov", SqlDbType.Bit);
                    cmd.Parameters.Add("@pro", SqlDbType.Bit);
                    cmd.Parameters.Add("@hos", SqlDbType.Bit);
                    cmd.Parameters.Add("@est", SqlDbType.NVarChar, 20);
                    cmd.Parameters.Add("@msg", SqlDbType.NVarChar, 200);
                    cmd.Parameters.Add("@ori", SqlDbType.NVarChar, 30);

                    foreach (var g in filas)
                    {
                        cmd.Parameters["@ip"].Value = g.Ip ?? "";
                        cmd.Parameters["@pais"].Value = (object)g.Pais ?? DBNull.Value;
                        cmd.Parameters["@cp"].Value = (object)g.CodigoPais ?? DBNull.Value;
                        cmd.Parameters["@reg"].Value = (object)g.Region ?? DBNull.Value;
                        cmd.Parameters["@ciu"].Value = (object)g.Ciudad ?? DBNull.Value;
                        cmd.Parameters["@lat"].Value = (object)g.Latitud ?? DBNull.Value;
                        cmd.Parameters["@lon"].Value = (object)g.Longitud ?? DBNull.Value;
                        cmd.Parameters["@isp"].Value = (object)g.Isp ?? DBNull.Value;
                        cmd.Parameters["@org"].Value = (object)g.Organizacion ?? DBNull.Value;
                        cmd.Parameters["@mov"].Value = g.EsMovil;
                        cmd.Parameters["@pro"].Value = g.EsProxy;
                        cmd.Parameters["@hos"].Value = g.EsHosting;
                        cmd.Parameters["@est"].Value = g.Estado ?? "OK";
                        cmd.Parameters["@msg"].Value = (object)g.Mensaje ?? DBNull.Value;
                        cmd.Parameters["@ori"].Value = (object)g.Origen ?? DBNull.Value;
                        n += cmd.ExecuteNonQuery();
                    }
                    tx.Commit();
                }
            }
            return n;
        }
    }
}