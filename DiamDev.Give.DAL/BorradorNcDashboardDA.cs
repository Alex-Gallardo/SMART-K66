using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using DiamDev.Give.Entities;

namespace DiamDev.Give.DAL
{
    public class BorradorNcDashboardDA
    {
        private readonly string _conn;

        public BorradorNcDashboardDA()
        {
            _conn = ResolverCadenaConexion();
        }

        public BorradorNcDashboardPagina Consultar(
            BorradorNcDashboardFiltro filtro, BorradorNcDashboardAlcance alcance,
            int diasVencido)
        {
            filtro = filtro ?? new BorradorNcDashboardFiltro();
            var pagina = new BorradorNcDashboardPagina
            {
                Pagina = filtro.Pagina,
                TamanoPagina = filtro.TamanoPagina
            };

            using (var cn = new SqlConnection(_conn))
            {
                cn.Open();
                pagina.TotalFilas = Contar(cn, filtro, alcance);
                pagina.Filas = ConsultarFilas(cn, filtro, alcance);
                pagina.Resumen = ConsultarResumen(cn, filtro, alcance, diasVencido);
                CargarOpciones(cn, alcance, pagina);
            }
            return pagina;
        }

        public List<BorradorNcDashboardFactura> ConsultarFacturas(
            BorradorNcDashboardFiltro filtro, BorradorNcDashboardAlcance alcance,
            int maximoBorradores)
        {
            var lista = new List<BorradorNcDashboardFactura>();
            var parametros = new List<SqlParameter>();
            string where = ConstruirWhere(filtro ?? new BorradorNcDashboardFiltro(),
                                          alcance, parametros);
            string sql = @"
                ;WITH BorradoresExportados AS
                (
                    SELECT TOP (@maximo) E.ID_EMPRESA, E.ID_BORRADOR
                    FROM dbo.BORR_NC_ENC E
                    WHERE " + where + @"
                    ORDER BY COALESCE(E.REGISTRO,E.FECHA) DESC, E.ID_BORRADOR DESC
                )
                SELECT D.ID_EMPRESA, D.ID_BORRADOR, D.DOCUMENTO, D.FECHA_DOC,
                       D.CONCEPTO, D.DESCRIPCION, D.MONEDA, D.TOTAL_FACT, D.IMPORTE
                FROM dbo.BORR_NC_DET D
                INNER JOIN BorradoresExportados E
                    ON E.ID_EMPRESA=D.ID_EMPRESA AND E.ID_BORRADOR=D.ID_BORRADOR
                ORDER BY D.ID_EMPRESA, D.ID_BORRADOR, D.ROWID;";

            using (var cn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(sql, cn))
            {
                AgregarParametros(cmd, parametros);
                cmd.Parameters.Add("@maximo", SqlDbType.Int).Value = maximoBorradores;
                cn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        lista.Add(new BorradorNcDashboardFactura
                        {
                            IdEmpresa = Texto(r["ID_EMPRESA"]),
                            IdBorrador = Texto(r["ID_BORRADOR"]),
                            Documento = Texto(r["DOCUMENTO"]),
                            FechaDocumento = Convert.ToDateTime(r["FECHA_DOC"]),
                            Concepto = Texto(r["CONCEPTO"]),
                            Descripcion = Texto(r["DESCRIPCION"]),
                            Moneda = Texto(r["MONEDA"]),
                            TotalFactura = Decimal(r["TOTAL_FACT"]),
                            ImporteSolicitado = Decimal(r["IMPORTE"])
                        });
                    }
                }
            }
            return lista;
        }

        public void RegistrarEvento(string empresa, string idBorrador,
                                    string evento, string usuario,
                                    string detalle, string ip)
        {
            const string sql = @"
                INSERT dbo.BORR_NC_BITACORA
                    (ID_EMPRESA, ID_BORRADOR, EVENTO, USUARIO, DETALLE, IP)
                VALUES (@empresa, @idBorrador, @evento, @usuario, @detalle, @ip);";
            using (var cn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@empresa", SqlDbType.NVarChar, 15).Value = empresa ?? "";
                cmd.Parameters.Add("@idBorrador", SqlDbType.NVarChar, 20).Value = idBorrador ?? "";
                cmd.Parameters.Add("@evento", SqlDbType.VarChar, 30).Value = evento ?? "";
                cmd.Parameters.Add("@usuario", SqlDbType.NVarChar, 50).Value = usuario ?? "";
                cmd.Parameters.Add("@detalle", SqlDbType.NVarChar, 1000).Value = Nulo(detalle);
                cmd.Parameters.Add("@ip", SqlDbType.NVarChar, 45).Value = Nulo(ip);
                cn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public List<BorradorNcBitacora> ConsultarBitacora(string empresa, string idBorrador)
        {
            const string sql = @"
                SELECT EVENTO_ID, EVENTO, ESTADO_ANTERIOR, ESTADO_NUEVO,
                       USUARIO, IP, DETALLE, REGISTRO
                FROM dbo.BORR_NC_BITACORA
                WHERE ID_EMPRESA=@empresa AND ID_BORRADOR=@idBorrador
                ORDER BY REGISTRO DESC, EVENTO_ID DESC;";
            var lista = new List<BorradorNcBitacora>();
            using (var cn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@empresa", SqlDbType.NVarChar, 15).Value = empresa ?? "";
                cmd.Parameters.Add("@idBorrador", SqlDbType.NVarChar, 20).Value = idBorrador ?? "";
                cn.Open();
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) lista.Add(new BorradorNcBitacora
                    {
                        EventoId = Convert.ToInt64(r["EVENTO_ID"]),
                        Evento = Texto(r["EVENTO"]), EstadoAnterior = Texto(r["ESTADO_ANTERIOR"]),
                        EstadoNuevo = Texto(r["ESTADO_NUEVO"]), Usuario = Texto(r["USUARIO"]),
                        Ip = Texto(r["IP"]), Detalle = Texto(r["DETALLE"]),
                        Registro = Convert.ToDateTime(r["REGISTRO"])
                    });
            }
            return lista;
        }

        private static int Contar(SqlConnection cn, BorradorNcDashboardFiltro filtro,
                                  BorradorNcDashboardAlcance alcance)
        {
            var parametros = new List<SqlParameter>();
            string where = ConstruirWhere(filtro, alcance, parametros);
            using (var cmd = new SqlCommand(
                "SELECT COUNT_BIG(1) FROM dbo.BORR_NC_ENC E WHERE " + where, cn))
            {
                AgregarParametros(cmd, parametros);
                long total = Convert.ToInt64(cmd.ExecuteScalar());
                return total > int.MaxValue ? int.MaxValue : (int)total;
            }
        }

        private static List<BorradorNcDashboardFila> ConsultarFilas(
            SqlConnection cn, BorradorNcDashboardFiltro filtro,
            BorradorNcDashboardAlcance alcance)
        {
            var parametros = new List<SqlParameter>();
            string where = ConstruirWhere(filtro, alcance, parametros);
            string orden = ColumnaOrden(filtro.Orden) +
                (string.Equals(filtro.Direccion, "ASC", StringComparison.OrdinalIgnoreCase)
                    ? " ASC" : " DESC") + ", E.ID_BORRADOR DESC";
            string sql = @"
                SELECT E.ID_BORRADOR, E.ID_EMPRESA, E.FECHA, E.REGISTRO,
                       E.ID_CLIENTE, E.NOMBRE, E.NIT, E.AGENTE, E.MONEDA,
                       E.TOTAL, E.ESTADO, E.ID_USR, E.RESUELTO_POR,
                       E.FECHA_RESOLUCION,
                       (SELECT COUNT(*) FROM dbo.BORR_NC_DET D
                         WHERE D.ID_EMPRESA=E.ID_EMPRESA AND D.ID_BORRADOR=E.ID_BORRADOR) FACTURAS,
                       (SELECT COUNT(*) FROM dbo.BORR_NC_ADJUNTO A
                         WHERE A.ID_EMPRESA=E.ID_EMPRESA AND A.ID_BORRADOR=E.ID_BORRADOR) ADJUNTOS,
                       CAST(CASE WHEN EXISTS (
                           SELECT 1 FROM dbo.BORR_NC_DET D
                           WHERE D.ID_EMPRESA=E.ID_EMPRESA
                             AND D.ID_BORRADOR=E.ID_BORRADOR
                             AND D.NC_PREVIA_SAP > 0) THEN 1 ELSE 0 END AS bit) ANTECEDENTES
                FROM dbo.BORR_NC_ENC E
                WHERE " + where + @"
                ORDER BY " + orden + @"
                OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;";

            var lista = new List<BorradorNcDashboardFila>();
            using (var cmd = new SqlCommand(sql, cn))
            {
                AgregarParametros(cmd, parametros);
                cmd.Parameters.Add("@offset", SqlDbType.Int).Value =
                    (filtro.Pagina - 1) * filtro.TamanoPagina;
                cmd.Parameters.Add("@pageSize", SqlDbType.Int).Value = filtro.TamanoPagina;
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read()) lista.Add(LeerFila(r));
                }
            }
            return lista;
        }

        private static BorradorNcDashboardResumen ConsultarResumen(
            SqlConnection cn, BorradorNcDashboardFiltro filtro,
            BorradorNcDashboardAlcance alcance, int diasVencido)
        {
            var parametros = new List<SqlParameter>();
            string estadoSeleccionado = filtro.Estado;
            filtro.Estado = null;
            string where;
            try { where = ConstruirWhere(filtro, alcance, parametros); }
            finally { filtro.Estado = estadoSeleccionado; }
            string sql = @"
                ;WITH Antecedentes AS
                (
                    SELECT D.ID_EMPRESA, D.ID_BORRADOR
                    FROM dbo.BORR_NC_DET D
                    WHERE D.NC_PREVIA_SAP > 0
                    GROUP BY D.ID_EMPRESA, D.ID_BORRADOR
                ),
                Adjuntos AS
                (
                    SELECT A.ID_EMPRESA, A.ID_BORRADOR
                    FROM dbo.BORR_NC_ADJUNTO A
                    GROUP BY A.ID_EMPRESA, A.ID_BORRADOR
                )
                SELECT COUNT(*) TOTAL,
                       SUM(CASE WHEN E.ESTADO='PENDIENTE' THEN 1 ELSE 0 END) PENDIENTES,
                       SUM(CASE WHEN E.ESTADO='PENDIENTE' AND COALESCE(E.REGISTRO,E.FECHA) < DATEADD(day,-@dias,SYSDATETIME()) THEN 1 ELSE 0 END) VENCIDOS,
                       SUM(CASE WHEN E.ESTADO='AUTORIZADO' THEN 1 ELSE 0 END) AUTORIZADOS,
                       SUM(CASE WHEN E.ESTADO='RECHAZADO' THEN 1 ELSE 0 END) RECHAZADOS,
                       SUM(CASE WHEN E.ESTADO='ANULADO' THEN 1 ELSE 0 END) ANULADOS,
                       SUM(CASE WHEN P.ID_BORRADOR IS NOT NULL THEN 1 ELSE 0 END) ANTECEDENTES,
                       SUM(CASE WHEN J.ID_BORRADOR IS NOT NULL THEN 1 ELSE 0 END) ADJUNTOS,
                       AVG(CASE WHEN E.FECHA_RESOLUCION IS NOT NULL THEN CAST(DATEDIFF(minute,COALESCE(E.REGISTRO,E.FECHA),E.FECHA_RESOLUCION) AS decimal(18,2))/60 END) HORAS,
                       MIN(CASE WHEN E.ESTADO='PENDIENTE' THEN COALESCE(E.REGISTRO,E.FECHA) END) MAS_ANTIGUO
                FROM dbo.BORR_NC_ENC E
                LEFT JOIN Antecedentes P
                    ON P.ID_EMPRESA=E.ID_EMPRESA AND P.ID_BORRADOR=E.ID_BORRADOR
                LEFT JOIN Adjuntos J
                    ON J.ID_EMPRESA=E.ID_EMPRESA AND J.ID_BORRADOR=E.ID_BORRADOR
                WHERE " + where + @";
                SELECT E.MONEDA, SUM(E.TOTAL) TOTAL
                FROM dbo.BORR_NC_ENC E WHERE " + where + @"
                GROUP BY E.MONEDA ORDER BY E.MONEDA;";

            var resumen = new BorradorNcDashboardResumen();
            using (var cmd = new SqlCommand(sql, cn))
            {
                AgregarParametros(cmd, parametros);
                cmd.Parameters.Add("@dias", SqlDbType.Int).Value = diasVencido;
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        resumen.Total = Entero(r["TOTAL"]);
                        resumen.Pendientes = Entero(r["PENDIENTES"]);
                        resumen.PendientesVencidos = Entero(r["VENCIDOS"]);
                        resumen.Autorizados = Entero(r["AUTORIZADOS"]);
                        resumen.Rechazados = Entero(r["RECHAZADOS"]);
                        resumen.Anulados = Entero(r["ANULADOS"]);
                        resumen.ConAntecedentesSap = Entero(r["ANTECEDENTES"]);
                        resumen.ConAdjuntos = Entero(r["ADJUNTOS"]);
                        resumen.HorasPromedioResolucion = NullableDecimal(r["HORAS"]);
                        resumen.PendienteMasAntiguo = NullableFecha(r["MAS_ANTIGUO"]);
                    }
                    if (r.NextResult())
                        while (r.Read()) resumen.TotalesPorMoneda.Add(
                            new BorradorNcDashboardTotalMoneda
                            {
                                Moneda = Texto(r["MONEDA"]),
                                Total = Decimal(r["TOTAL"])
                            });
                }
            }
            return resumen;
        }

        private static void CargarOpciones(SqlConnection cn,
                                           BorradorNcDashboardAlcance alcance,
                                           BorradorNcDashboardPagina pagina)
        {
            var parametros = new List<SqlParameter>();
            string scope = ConstruirAlcance(alcance, parametros);
            string sql = @"
                SELECT DISTINCT ID_EMPRESA VALOR FROM dbo.BORR_NC_ENC E WHERE " + scope + @" ORDER BY VALOR;
                SELECT DISTINCT AGENTE VALOR FROM dbo.BORR_NC_ENC E WHERE " + scope + @" AND NULLIF(LTRIM(RTRIM(AGENTE)),'') IS NOT NULL ORDER BY VALOR;
                SELECT DISTINCT ID_USR VALOR FROM dbo.BORR_NC_ENC E WHERE " + scope + @" AND NULLIF(LTRIM(RTRIM(ID_USR)),'') IS NOT NULL ORDER BY VALOR;
                SELECT DISTINCT RESUELTO_POR VALOR FROM dbo.BORR_NC_ENC E WHERE " + scope + @" AND NULLIF(LTRIM(RTRIM(RESUELTO_POR)),'') IS NOT NULL ORDER BY VALOR;
                SELECT DISTINCT MONEDA VALOR FROM dbo.BORR_NC_ENC E WHERE " + scope + @" AND NULLIF(LTRIM(RTRIM(MONEDA)),'') IS NOT NULL ORDER BY VALOR;";
            using (var cmd = new SqlCommand(sql, cn))
            {
                AgregarParametros(cmd, parametros);
                using (var r = cmd.ExecuteReader())
                {
                    LeerOpciones(r, pagina.Empresas);
                    if (r.NextResult()) LeerOpciones(r, pagina.Agentes);
                    if (r.NextResult()) LeerOpciones(r, pagina.Creadores);
                    if (r.NextResult()) LeerOpciones(r, pagina.Resolutores);
                    if (r.NextResult()) LeerOpciones(r, pagina.Monedas);
                }
            }
        }

        private static string ConstruirWhere(BorradorNcDashboardFiltro filtro,
                                             BorradorNcDashboardAlcance alcance,
                                             List<SqlParameter> parametros)
        {
            var partes = new List<string> { ConstruirAlcance(alcance, parametros) };
            AgregarIgual(partes, parametros, "E.ID_EMPRESA", "@empresa", filtro.Empresa, 15);
            AgregarIgual(partes, parametros, "E.ESTADO", "@estado", filtro.Estado, 20, true);
            AgregarIgual(partes, parametros, "E.AGENTE", "@agente", filtro.Agente, 155);
            AgregarIgual(partes, parametros, "E.ID_USR", "@creador", filtro.Creador, 50);
            AgregarIgual(partes, parametros, "E.RESUELTO_POR", "@resuelto", filtro.ResueltoPor, 50);
            AgregarIgual(partes, parametros, "E.MONEDA", "@moneda", filtro.Moneda, 5, true);

            string campoFecha = string.Equals(filtro.CampoFecha, "RESOLUCION",
                StringComparison.OrdinalIgnoreCase)
                ? "E.FECHA_RESOLUCION" : "COALESCE(E.REGISTRO,E.FECHA)";
            if (filtro.Desde.HasValue)
            {
                partes.Add(campoFecha + " >= @desde");
                parametros.Add(new SqlParameter("@desde", SqlDbType.DateTime2)
                    { Value = filtro.Desde.Value.Date });
            }
            if (filtro.Hasta.HasValue)
            {
                partes.Add(campoFecha + " < DATEADD(day,1,@hasta)");
                parametros.Add(new SqlParameter("@hasta", SqlDbType.DateTime2)
                    { Value = filtro.Hasta.Value.Date });
            }
            if (!string.IsNullOrWhiteSpace(filtro.Cliente))
            {
                partes.Add("(E.ID_CLIENTE LIKE @cliente OR E.NOMBRE LIKE @cliente OR E.NIT LIKE @cliente)");
                parametros.Add(new SqlParameter("@cliente", SqlDbType.NVarChar, 210)
                    { Value = "%" + filtro.Cliente.Trim() + "%" });
            }
            if (!string.IsNullOrWhiteSpace(filtro.Texto))
            {
                partes.Add("(E.ID_BORRADOR LIKE @texto OR E.ID_CLIENTE LIKE @texto OR E.NOMBRE LIKE @texto OR E.NIT LIKE @texto OR E.AGENTE LIKE @texto OR EXISTS (SELECT 1 FROM dbo.BORR_NC_DET DX WHERE DX.ID_EMPRESA=E.ID_EMPRESA AND DX.ID_BORRADOR=E.ID_BORRADOR AND DX.DOCUMENTO LIKE @texto))");
                parametros.Add(new SqlParameter("@texto", SqlDbType.NVarChar, 210)
                    { Value = "%" + filtro.Texto.Trim() + "%" });
            }
            AgregarExiste(partes, filtro.ConAdjuntos,
                "SELECT 1 FROM dbo.BORR_NC_ADJUNTO A WHERE A.ID_EMPRESA=E.ID_EMPRESA AND A.ID_BORRADOR=E.ID_BORRADOR");
            AgregarExiste(partes, filtro.ConAntecedentesSap,
                "SELECT 1 FROM dbo.BORR_NC_DET D WHERE D.ID_EMPRESA=E.ID_EMPRESA AND D.ID_BORRADOR=E.ID_BORRADOR AND D.NC_PREVIA_SAP>0");
            return string.Join(" AND ", partes);
        }

        private static string ConstruirAlcance(BorradorNcDashboardAlcance alcance,
                                               List<SqlParameter> parametros)
        {
            if (alcance != null && alcance.Global) return "1=1";
            var partes = new List<string>();
            string usuario = alcance == null ? "" : (alcance.Usuario ?? "").Trim();
            if (usuario.Length > 0)
            {
                partes.Add("E.ID_USR=@alcanceUsuario");
                parametros.Add(new SqlParameter("@alcanceUsuario", SqlDbType.NVarChar, 50)
                    { Value = usuario });
            }
            int empresaIndice = 0;
            foreach (var par in (alcance == null
                ? new Dictionary<string, List<string>>() : alcance.AgentesPorEmpresa))
            {
                var agentes = (par.Value ?? new List<string>()).Where(x =>
                    !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if (agentes.Count == 0) continue;
                string pe = "@alcanceEmpresa" + empresaIndice;
                parametros.Add(new SqlParameter(pe, SqlDbType.NVarChar, 15)
                    { Value = par.Key });
                var nombres = new List<string>();
                for (int i = 0; i < agentes.Count; i++)
                {
                    string pa = "@alcanceAgente" + empresaIndice + "_" + i;
                    parametros.Add(new SqlParameter(pa, SqlDbType.NVarChar, 155)
                        { Value = agentes[i].Trim() });
                    nombres.Add(pa);
                }
                partes.Add("(E.ID_EMPRESA=" + pe + " AND E.AGENTE IN (" +
                           string.Join(",", nombres) + "))");
                empresaIndice++;
            }
            return partes.Count == 0 ? "1=0" : "(" + string.Join(" OR ", partes) + ")";
        }

        private static void AgregarIgual(List<string> partes, List<SqlParameter> parametros,
                                         string columna, string nombre, string valor,
                                         int longitud, bool varchar = false)
        {
            if (string.IsNullOrWhiteSpace(valor)) return;
            partes.Add(columna + "=" + nombre);
            parametros.Add(new SqlParameter(nombre,
                varchar ? SqlDbType.VarChar : SqlDbType.NVarChar, longitud)
                { Value = valor.Trim() });
        }

        private static void AgregarExiste(List<string> partes, bool? valor, string consulta)
        {
            if (!valor.HasValue) return;
            partes.Add((valor.Value ? "EXISTS (" : "NOT EXISTS (") + consulta + ")");
        }

        private static string ColumnaOrden(string orden)
        {
            switch ((orden ?? "").Trim().ToUpperInvariant())
            {
                case "BORRADOR": return "E.ID_BORRADOR";
                case "EMPRESA": return "E.ID_EMPRESA";
                case "CLIENTE": return "E.NOMBRE";
                case "AGENTE": return "E.AGENTE";
                case "ESTADO": return "E.ESTADO";
                case "TOTAL": return "E.TOTAL";
                case "RESOLUCION": return "E.FECHA_RESOLUCION";
                default: return "COALESCE(E.REGISTRO,E.FECHA)";
            }
        }

        private static BorradorNcDashboardFila LeerFila(IDataRecord r)
        {
            return new BorradorNcDashboardFila
            {
                IdBorrador = Texto(r["ID_BORRADOR"]), IdEmpresa = Texto(r["ID_EMPRESA"]),
                Fecha = Convert.ToDateTime(r["FECHA"]), Registro = NullableFecha(r["REGISTRO"]),
                IdCliente = Texto(r["ID_CLIENTE"]), Nombre = Texto(r["NOMBRE"]), Nit = Texto(r["NIT"]),
                Agente = Texto(r["AGENTE"]), Moneda = Texto(r["MONEDA"]), Total = Decimal(r["TOTAL"]),
                Estado = Texto(r["ESTADO"]), IdUsr = Texto(r["ID_USR"]),
                ResueltoPor = Texto(r["RESUELTO_POR"]), FechaResolucion = NullableFecha(r["FECHA_RESOLUCION"]),
                Facturas = Entero(r["FACTURAS"]), Adjuntos = Entero(r["ADJUNTOS"]),
                TieneAntecedentesSap = Convert.ToBoolean(r["ANTECEDENTES"])
            };
        }

        private static void LeerOpciones(IDataReader r, List<string> destino)
        {
            while (r.Read()) destino.Add(Texto(r["VALOR"]));
        }

        private static void AgregarParametros(SqlCommand cmd, IEnumerable<SqlParameter> parametros)
        {
            foreach (var p in parametros)
                cmd.Parameters.Add(new SqlParameter(p.ParameterName, p.SqlDbType, p.Size)
                    { Value = p.Value });
        }

        private static string ResolverCadenaConexion()
        {
            var perfil = ConfigurationManager.ConnectionStrings["BorradorNcContext"];
            if (perfil == null || string.IsNullOrWhiteSpace(perfil.ConnectionString))
                throw new ConfigurationErrorsException("No existe BorradorNcContext.");
            var propiedades = new DbConnectionStringBuilder { ConnectionString = perfil.ConnectionString.Trim() };
            object alias;
            if (!propiedades.TryGetValue("Alias", out alias))
                return new SqlConnectionStringBuilder(perfil.ConnectionString).ConnectionString;
            var origen = ConfigurationManager.ConnectionStrings[Convert.ToString(alias).Trim()];
            if (origen == null) throw new ConfigurationErrorsException("El alias de BorradorNcContext no existe.");
            var resultado = new SqlConnectionStringBuilder(origen.ConnectionString);
            propiedades.Remove("Alias");
            foreach (string clave in propiedades.Keys) resultado[clave] = propiedades[clave];
            return resultado.ConnectionString;
        }

        private static object Nulo(string valor) => string.IsNullOrWhiteSpace(valor) ? (object)DBNull.Value : valor.Trim();
        private static string Texto(object valor) => valor == null || valor == DBNull.Value ? "" : Convert.ToString(valor);
        private static decimal Decimal(object valor) => valor == null || valor == DBNull.Value ? 0m : Convert.ToDecimal(valor);
        private static decimal? NullableDecimal(object valor) => valor == null || valor == DBNull.Value ? (decimal?)null : Convert.ToDecimal(valor);
        private static int Entero(object valor) => valor == null || valor == DBNull.Value ? 0 : Convert.ToInt32(valor);
        private static DateTime? NullableFecha(object valor) => valor == null || valor == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(valor);
    }
}
