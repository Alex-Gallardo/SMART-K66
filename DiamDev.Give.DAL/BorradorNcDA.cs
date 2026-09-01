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
    /// <summary>
    /// El disponible cambió entre la validación de negocio y la escritura. Se
    /// diferencia de una falla técnica para que la UI pueda pedir al usuario que
    /// actualice la factura, sin presentar un mensaje de motor de base de datos.
    /// </summary>
    public sealed class BorradorNcDisponibilidadException : InvalidOperationException
    {
        public BorradorNcDisponibilidadException(string message) : base(message) { }
    }

    /// <summary>
    /// Acceso a datos del módulo Borradores de Nota de Crédito.
    /// ★ VERSIÓN FINAL — reemplaza a cualquier versión anterior.
    ///
    /// ADO.NET directo contra el connection string exclusivo
    /// "BorradorNcContext", SQL parametrizado escrito aquí (no stored
    /// procedures), y transacción explícita donde hay más de una escritura.
    /// El contexto admite heredar credenciales de otra conexión y sobrescribir
    /// solo la base, para aislar este módulo sin alterar RecibosContext.
    ///
    /// ¿Por qué no seguimos llamando los SP legados (rec_borr_*)? Porque su
    /// lógica vivía solo en el servidor, sin versionar, y tenía defectos que
    /// había que corregir de todas formas — sobre todo la generación del
    /// correlativo. Con el SQL en el repositorio queda revisable en un PR.
    /// </summary>
    public class BorradorNcDA : IDisposable
    {
        private const string CONNECTION_NAME = "BorradorNcContext";
        private const string ALIAS_KEY = "Alias";

        private readonly string _conn;

        public BorradorNcDA()
        {
            _conn = ResolverCadenaConexion();
        }

        /// <summary>
        /// BorradorNcContext puede contener una cadena SQL normal o un perfil
        /// aislado como:
        ///
        ///   Alias=RecibosContext;Initial Catalog=POS-SmartK66
        ///
        /// En el segundo caso reutiliza servidor y credenciales del alias, pero
        /// aplica únicamente las propiedades declaradas en BorradorNcContext.
        /// Así se evita duplicar secretos y cambiar RecibosContext no obliga a
        /// mover los borradores de base accidentalmente.
        /// </summary>
        private static string ResolverCadenaConexion()
        {
            var perfil = ConfigurationManager.ConnectionStrings[CONNECTION_NAME];
            if (perfil == null || string.IsNullOrWhiteSpace(perfil.ConnectionString))
                throw new ConfigurationErrorsException(
                    "No existe una cadena de conexión válida llamada '" +
                    CONNECTION_NAME + "'.");

            try
            {
                var propiedades = new DbConnectionStringBuilder
                {
                    ConnectionString = perfil.ConnectionString.Trim()
                };

                object nombreAlias;
                if (!propiedades.TryGetValue(ALIAS_KEY, out nombreAlias))
                    return new SqlConnectionStringBuilder(
                        perfil.ConnectionString).ConnectionString;

                string alias = Convert.ToString(nombreAlias)?.Trim();
                if (string.IsNullOrWhiteSpace(alias) ||
                    string.Equals(alias, CONNECTION_NAME,
                                  StringComparison.OrdinalIgnoreCase))
                    throw new ConfigurationErrorsException(
                        "El alias de BorradorNcContext no es válido.");

                var origen = ConfigurationManager.ConnectionStrings[alias];
                if (origen == null || string.IsNullOrWhiteSpace(origen.ConnectionString))
                    throw new ConfigurationErrorsException(
                        "No existe la cadena base indicada por BorradorNcContext.");

                var resultado = new SqlConnectionStringBuilder(origen.ConnectionString);
                propiedades.Remove(ALIAS_KEY);

                foreach (string clave in propiedades.Keys)
                    resultado[clave] = propiedades[clave];

                return resultado.ConnectionString;
            }
            catch (ArgumentException ex)
            {
                throw new ConfigurationErrorsException(
                    "La configuración de BorradorNcContext no es una cadena SQL válida.",
                    ex);
            }
        }

        // =====================================================================
        //  SERIES  (una por empresa — decisión de negocio confirmada)
        // =====================================================================

        /// <summary>
        /// ¿Hay serie activa para la empresa? Se valida ANTES de abrir la
        /// transacción del guardado: sin ella el correlativo saldría NULL y el
        /// usuario vería un error de motor en vez de una explicación.
        /// </summary>
        public bool ExisteSerie(string empresa)
        {
            const string sql = @"
                SELECT COUNT(*) FROM dbo.BORR_NC_SERIES
                WHERE EMPRESA = @empresa AND ACTIVO = 1;";

            using (var cn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@empresa", SqlDbType.NVarChar, 15).Value = empresa ?? "";
                cn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        /// <summary>
        /// Prefijo de la serie (ej. "BB01-"), solo para mostrarlo en la UI.
        /// NO consume correlativo: el número se asigna al guardar.
        ///
        /// Diferencia clave con el legado: rec_series_BN devolvía el SIGUIENTE
        /// ID COMPLETO, el desktop lo pintaba en pantalla y lo mandaba después
        /// al insertar. Entre una cosa y la otra —el tiempo que el usuario
        /// tardara en llenar el formulario— otro podía tomar el mismo número.
        /// Aquí la UI solo conoce el prefijo.
        /// </summary>
        public string ObtenerPrefijoSerie(string empresa)
        {
            const string sql = @"
                SELECT ISNULL(SERIE,'') FROM dbo.BORR_NC_SERIES
                WHERE EMPRESA = @empresa AND ACTIVO = 1;";

            using (var cn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@empresa", SqlDbType.NVarChar, 15).Value = empresa ?? "";
                cn.Open();
                return cmd.ExecuteScalar()?.ToString() ?? string.Empty;
            }
        }

        // =====================================================================
        //  ACUMULADO POR DOCUMENTO
        // =====================================================================

        /// <summary>
        /// Importe ya comprometido contra un documento en borradores vigentes
        /// (PENDIENTE o AUTORIZADO). RECHAZADO y ANULADO liberan su monto.
        /// Base de la regla R4.
        ///
        /// A diferencia del rec_borr_existe legado, NO filtra por cliente: una
        /// factura pertenece a un solo cliente, así que filtrar no cambia el
        /// resultado pero sí podría ocultar datos sucios (mismo DocNum en
        /// clientes distintos). Si eso existe, queremos verlo.
        /// </summary>
        public decimal ObtenerAcumuladoDocumento(string empresa, string documento)
        {
            const string sql = @"
                SELECT ISNULL(ACUMULADO,0) FROM dbo.VW_BORR_NC_ACUMULADO
                WHERE ID_EMPRESA = @empresa AND DOCUMENTO = @documento;";

            using (var cn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@empresa", SqlDbType.NVarChar, 15).Value = empresa ?? "";
                cmd.Parameters.Add("@documento", SqlDbType.NVarChar, 50).Value = documento ?? "";
                cn.Open();
                object r = cmd.ExecuteScalar();
                return r == null || r == DBNull.Value ? 0m : Convert.ToDecimal(r);
            }
        }

        /// <summary>
        /// Acumulado de varios documentos en UNA consulta. El modal de facturas
        /// puede traer decenas; una consulta por fila serían decenas de viajes.
        /// La lista IN se arma con parámetros generados (@d0, @d1...), nunca
        /// concatenando valores.
        /// </summary>
        public Dictionary<string, decimal> ObtenerAcumuladoDocumentos(
            string empresa, IList<string> documentos)
        {
            var mapa = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            var docs = (documentos ?? new List<string>())
                       .Where(d => !string.IsNullOrWhiteSpace(d))
                       .Select(d => d.Trim())
                       .Distinct(StringComparer.OrdinalIgnoreCase)
                       .ToList();
            if (docs.Count == 0) return mapa;

            string marcadores = string.Join(",", docs.Select((d, i) => "@d" + i));
            string sql = string.Format(@"
                SELECT DOCUMENTO, ACUMULADO FROM dbo.VW_BORR_NC_ACUMULADO
                WHERE ID_EMPRESA = @empresa AND DOCUMENTO IN ({0});", marcadores);

            using (var cn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@empresa", SqlDbType.NVarChar, 15).Value = empresa ?? "";
                for (int i = 0; i < docs.Count; i++)
                    cmd.Parameters.Add("@d" + i, SqlDbType.NVarChar, 50).Value = docs[i];

                cn.Open();
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        mapa[r["DOCUMENTO"].ToString().Trim()] = Val(r["ACUMULADO"]);
            }

            // Los documentos sin filas no aparecen en la vista: acumulado 0.
            foreach (var d in docs)
                if (!mapa.ContainsKey(d)) mapa[d] = 0m;

            return mapa;
        }

        /// <summary>
        /// ¿En qué otros borradores vigentes aparece este documento?
        /// Reemplaza al rec_borr_existe legado, que devolvía un simple bit.
        /// Devolver la lista permite decir "ya está en BB01-00042" en vez de
        /// "el documento ya existe", que dejaba al usuario sin salida.
        /// </summary>
        public List<string> ObtenerBorradoresConDocumento(string empresa, string documento)
        {
            const string sql = @"
                SELECT     D.ID_BORRADOR
                FROM       dbo.BORR_NC_DET D
                INNER JOIN dbo.BORR_NC_ENC E
                        ON E.ID_EMPRESA = D.ID_EMPRESA AND E.ID_BORRADOR = D.ID_BORRADOR
                WHERE      D.ID_EMPRESA = @empresa
                  AND      D.DOCUMENTO  = @documento
                  AND      E.ESTADO IN ('PENDIENTE','AUTORIZADO')
                ORDER BY   D.ID_BORRADOR;";

            var lista = new List<string>();
            using (var cn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@empresa", SqlDbType.NVarChar, 15).Value = empresa ?? "";
                cmd.Parameters.Add("@documento", SqlDbType.NVarChar, 50).Value = documento ?? "";
                cn.Open();
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) lista.Add(r[0].ToString());
            }
            return lista;
        }

        // =====================================================================
        //  GUARDADO  ★ el método crítico
        // =====================================================================

        /// <summary>
        /// Inserta encabezado + detalle en una transacción, generando el
        /// correlativo de forma atómica. Al terminar, enc.IdBorrador trae el ID.
        ///
        /// CÓMO SE EVITA LA CARRERA
        /// ------------------------
        /// El legado lo hacía en dos pasos, sin transacción:
        ///     rec_series_BN    -> SELECT del siguiente número
        ///     rec_borr_insert  -> INSERT con ese número + UPDATE de la serie
        /// Dos usuarios que abrieran el formulario a la vez recibían el mismo ID.
        ///
        /// Aquí un solo UPDATE incrementa Y asigna:
        ///     UPDATE ... SET NUMERACION = NUMERACION + 1,
        ///                    @id = SERIE + RIGHT(...)
        /// UPDLOCK toma el candado de escritura desde la lectura, así una
        /// segunda sesión espera en vez de leer el valor viejo. Todo dentro de
        /// la transacción: si el detalle falla se devuelve también el número y
        /// no quedan huecos en la numeración.
        ///
        /// La PK compuesta (ID_EMPRESA, ID_BORRADOR) es la red de seguridad:
        /// aunque la lógica fallara, la base rechaza el duplicado.
        /// </summary>
        public void GuardarBorradorCompleto(BorradorNcEncabezado enc)
        {
            if (enc == null) throw new ArgumentNullException(nameof(enc));
            if (enc.Detalles == null || enc.Detalles.Count == 0)
                throw new InvalidOperationException("El borrador no tiene líneas de detalle.");

            const string sqlEnc = @"
                DECLARE @id NVARCHAR(20);

                -- Toma el siguiente correlativo Y lo consume, atómicamente.
                UPDATE dbo.BORR_NC_SERIES WITH (UPDLOCK, HOLDLOCK)
                   SET NUMERACION = NUMERACION + 1,
                       @id = SERIE + RIGHT('0000' + CONVERT(NVARCHAR(10), NUMERACION + 1), 5)
                 WHERE EMPRESA = @empresa AND ACTIVO = 1;

                IF @id IS NULL
                    THROW 50001, 'No hay una serie activa configurada para esta empresa.', 1;

                INSERT INTO dbo.BORR_NC_ENC
                    (ID_BORRADOR, ID_EMPRESA, FECHA, ID_CLIENTE, NOMBRE, NIT,
                     DIRECCION, CORREO, AGENTE, MONEDA, TOTAL, ESTADO,
                     ID_USR, DEPTO, CODIGO_OPERADOR, REGISTRO)
                VALUES
                    (@id, @empresa, @fecha, @idCliente, @nombre, @nit,
                     @direccion, @correo, @agente, @moneda, @total, 'PENDIENTE',
                     @idUsr, @depto, @codigoOp, SYSDATETIME());

                SELECT @id;";

            const string sqlDet = @"
                INSERT INTO dbo.BORR_NC_DET
                    (ID_BORRADOR, ID_EMPRESA, CONCEPTO, DOCUMENTO, FECHA_DOC,
                     SERIE, NUMERO, TOTAL_FACT, PAGADO, NC_PREVIA_SAP,
                     MONEDA, DESCRIPCION, IMPORTE)
                VALUES
                    (@idBorr, @empresa, @concepto, @documento, @fechaDoc,
                     @serie, @numero, @totalFact, @pagado, @ncPrevia,
                     @moneda, @descripcion, @importe);";

            const string sqlAdjunto = @"
                INSERT INTO dbo.BORR_NC_ADJUNTO
                    (ID_BORRADOR, ID_EMPRESA, TIPO, NOMBRE, EXTENSION,
                     CONTENT_TYPE, TAMANO, CONTENIDO, URL, HASH_SHA256,
                     ORDEN, ID_USR, REGISTRO)
                VALUES
                    (@idBorr, @empresa, @tipo, @nombre, @extension,
                     @contentType, @tamano, @contenido, @url, @hash,
                     @orden, @idUsr, SYSDATETIME());";

            const string sqlAcumulado = @"
                SELECT ISNULL(SUM(D.IMPORTE), 0)
                FROM dbo.BORR_NC_DET D
                INNER JOIN dbo.BORR_NC_ENC E
                        ON E.ID_EMPRESA = D.ID_EMPRESA
                       AND E.ID_BORRADOR = D.ID_BORRADOR
                WHERE D.ID_EMPRESA = @empresa
                  AND D.DOCUMENTO = @documento
                  AND E.ESTADO IN ('PENDIENTE', 'AUTORIZADO');";

            using (var cn = new SqlConnection(_conn))
            {
                cn.Open();
                using (var tx = cn.BeginTransaction())
                {
                    try
                    {
                        // ── 1. Encabezado + correlativo ──────────────────────
                        using (var cmd = new SqlCommand(sqlEnc, cn, tx))
                        {
                            cmd.Parameters.Add("@empresa", SqlDbType.NVarChar, 15).Value = enc.IdEmpresa;
                            cmd.Parameters.Add("@fecha", SqlDbType.Date).Value = enc.Fecha.Date;
                            cmd.Parameters.Add("@idCliente", SqlDbType.NVarChar, 20).Value = enc.IdCliente;
                            cmd.Parameters.Add("@nombre", SqlDbType.NVarChar, 200).Value = enc.Nombre;
                            cmd.Parameters.Add("@nit", SqlDbType.NVarChar, 50).Value = Nulo(enc.Nit);
                            cmd.Parameters.Add("@direccion", SqlDbType.NVarChar, 200).Value = Nulo(enc.Direccion);
                            cmd.Parameters.Add("@correo", SqlDbType.NVarChar, 100).Value = Nulo(enc.Correo);
                            cmd.Parameters.Add("@agente", SqlDbType.NVarChar, 155).Value = enc.Agente;
                            cmd.Parameters.Add("@moneda", SqlDbType.NVarChar, 5).Value = enc.Moneda;
                            cmd.Parameters.Add("@idUsr", SqlDbType.NVarChar, 50).Value = enc.IdUsr;
                            cmd.Parameters.Add("@depto", SqlDbType.NVarChar, 50).Value = Nulo(enc.Depto);
                            cmd.Parameters.Add("@codigoOp", SqlDbType.NVarChar, 50).Value = Nulo(enc.CodigoOperador);

                            var pTotal = cmd.Parameters.Add("@total", SqlDbType.Decimal);
                            pTotal.Precision = 20; pTotal.Scale = 3; pTotal.Value = enc.Total;

                            enc.IdBorrador = cmd.ExecuteScalar()?.ToString();
                        }

                        if (string.IsNullOrWhiteSpace(enc.IdBorrador))
                            throw new InvalidOperationException(
                                "No se pudo generar el número de borrador.");

                        // ── 2. Revalidación dentro de la transacción ─────────
                        // El UPDATE de la serie mantiene un candado por empresa hasta
                        // el COMMIT. Por eso esta segunda lectura ve cualquier borrador
                        // que haya ganado la carrera después de la validación del BLL y
                        // serializa también la regla R4, no solo el correlativo.
                        foreach (var d in enc.Detalles)
                        {
                            decimal acumulado;
                            using (var cmd = new SqlCommand(sqlAcumulado, cn, tx))
                            {
                                cmd.Parameters.Add("@empresa", SqlDbType.NVarChar, 15).Value = enc.IdEmpresa;
                                cmd.Parameters.Add("@documento", SqlDbType.NVarChar, 50).Value = d.Documento;
                                acumulado = Convert.ToDecimal(cmd.ExecuteScalar());
                            }

                            decimal disponible = d.TotalFactura - acumulado;
                            if (d.Importe - disponible > 0.005m)
                            {
                                throw new BorradorNcDisponibilidadException(string.Format(
                                    "El disponible del documento {0} cambió mientras se guardaba. " +
                                    "Ahora quedan {1:N2}; actualice la factura e inténtelo de nuevo.",
                                    d.Documento, disponible < 0 ? 0 : disponible));
                            }
                        }

                        // ── 3. Detalle ───────────────────────────────────────
                        foreach (var d in enc.Detalles)
                        {
                            using (var cmd = new SqlCommand(sqlDet, cn, tx))
                            {
                                cmd.Parameters.Add("@idBorr", SqlDbType.NVarChar, 20).Value = enc.IdBorrador;
                                cmd.Parameters.Add("@empresa", SqlDbType.NVarChar, 15).Value = enc.IdEmpresa;
                                cmd.Parameters.Add("@concepto", SqlDbType.NVarChar, 20).Value = d.Concepto;
                                cmd.Parameters.Add("@documento", SqlDbType.NVarChar, 50).Value = d.Documento;
                                cmd.Parameters.Add("@fechaDoc", SqlDbType.Date).Value = d.FechaDoc.Date;
                                cmd.Parameters.Add("@serie", SqlDbType.NVarChar, 20).Value = Nulo(d.SerieFel);
                                cmd.Parameters.Add("@numero", SqlDbType.NVarChar, 150).Value = Nulo(d.NumeroFel);
                                cmd.Parameters.Add("@moneda", SqlDbType.NVarChar, 5).Value = d.Moneda ?? enc.Moneda;
                                cmd.Parameters.Add("@descripcion", SqlDbType.NVarChar, 500).Value = d.Descripcion;

                                var pTf = cmd.Parameters.Add("@totalFact", SqlDbType.Decimal);
                                pTf.Precision = 20; pTf.Scale = 3; pTf.Value = d.TotalFactura;

                                var pPg = cmd.Parameters.Add("@pagado", SqlDbType.Decimal);
                                pPg.Precision = 20; pPg.Scale = 3; pPg.Value = d.Pagado;

                                var pNc = cmd.Parameters.Add("@ncPrevia", SqlDbType.Decimal);
                                pNc.Precision = 20; pNc.Scale = 3; pNc.Value = d.NcPreviaSap;

                                var pIm = cmd.Parameters.Add("@importe", SqlDbType.Decimal);
                                pIm.Precision = 20; pIm.Scale = 3; pIm.Value = d.Importe;

                                cmd.ExecuteNonQuery();
                            }
                        }

                        // ── 4. Documentación opcional ───────────────────────
                        foreach (var adjunto in enc.Adjuntos ?? new List<BorradorNcAdjunto>())
                        {
                            using (var cmd = new SqlCommand(sqlAdjunto, cn, tx))
                            {
                                cmd.Parameters.Add("@idBorr", SqlDbType.NVarChar, 20).Value = enc.IdBorrador;
                                cmd.Parameters.Add("@empresa", SqlDbType.NVarChar, 15).Value = enc.IdEmpresa;
                                cmd.Parameters.Add("@tipo", SqlDbType.VarChar, 10).Value = adjunto.Tipo;
                                cmd.Parameters.Add("@nombre", SqlDbType.NVarChar, 255).Value = adjunto.Nombre;
                                cmd.Parameters.Add("@extension", SqlDbType.NVarChar, 10).Value = Nulo(adjunto.Extension);
                                cmd.Parameters.Add("@contentType", SqlDbType.NVarChar, 150).Value = Nulo(adjunto.ContentType);
                                cmd.Parameters.Add("@tamano", SqlDbType.BigInt).Value = adjunto.Tamano;
                                cmd.Parameters.Add("@contenido", SqlDbType.VarBinary, -1).Value
                                    = (object)adjunto.Contenido ?? DBNull.Value;
                                cmd.Parameters.Add("@url", SqlDbType.NVarChar, 2048).Value = Nulo(adjunto.Url);
                                cmd.Parameters.Add("@hash", SqlDbType.Binary, 32).Value = adjunto.HashSha256;
                                cmd.Parameters.Add("@orden", SqlDbType.SmallInt).Value = adjunto.Orden;
                                cmd.Parameters.Add("@idUsr", SqlDbType.NVarChar, 50).Value = enc.IdUsr;
                                cmd.ExecuteNonQuery();
                            }
                        }

                        tx.Commit();
                    }
                    catch
                    {
                        try { tx.Rollback(); } catch { /* la conexión ya murió */ }
                        throw;   // throw; — NUNCA throw ex; (borra el stack trace)
                    }
                }
            }
        }

        // =====================================================================
        //  RESOLUCIÓN
        // =====================================================================

        /// <summary>
        /// Pasa un borrador PENDIENTE a AUTORIZADO o RECHAZADO.
        ///
        /// El "AND ESTADO = 'PENDIENTE'" es el candado optimista: si otro
        /// usuario ya lo resolvió, este UPDATE afecta 0 filas y el BLL avisa,
        /// en vez de sobrescribir la decisión ajena.
        ///
        /// Mejora sobre el legado: rec_auto_borr guardaba al aprobador en
        /// USR_AUTO y al que rechaza en USR_ANULA —dos columnas para el mismo
        /// hecho— y descartaba el comentario cuando la decisión era AUTORIZADO.
        /// Aquí siempre se graban los tres campos.
        ///
        /// Devuelve filas afectadas (0 = ya no estaba pendiente).
        /// </summary>
        public int Resolver(string empresa, string idBorrador,
                            string nuevoEstado, string usuario, string motivo)
        {
            const string sql = @"
                UPDATE dbo.BORR_NC_ENC
                   SET ESTADO            = @estado,
                       RESUELTO_POR      = @usuario,
                       FECHA_RESOLUCION  = SYSDATETIME(),
                       MOTIVO_RESOLUCION = @motivo
                 WHERE ID_EMPRESA  = @empresa
                   AND ID_BORRADOR = @idBorr
                   AND ESTADO      = 'PENDIENTE';";

            using (var cn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@estado", SqlDbType.VarChar, 20).Value = nuevoEstado;
                cmd.Parameters.Add("@usuario", SqlDbType.NVarChar, 50).Value = usuario ?? "";
                cmd.Parameters.Add("@motivo", SqlDbType.NVarChar, 1000).Value = Nulo(motivo);
                cmd.Parameters.Add("@empresa", SqlDbType.NVarChar, 15).Value = empresa ?? "";
                cmd.Parameters.Add("@idBorr", SqlDbType.NVarChar, 20).Value = idBorrador ?? "";
                cn.Open();
                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Anula un borrador YA AUTORIZADO. Funcionalidad nueva: el desktop
        /// solo podía rechazar lo pendiente, así que un borrador autorizado por
        /// error se quedaba comprometiendo el saldo de la factura para siempre.
        /// Anular lo libera (ver VW_BORR_NC_ACUMULADO).
        /// </summary>
        public int Anular(string empresa, string idBorrador, string usuario, string motivo)
        {
            const string sql = @"
                UPDATE dbo.BORR_NC_ENC
                   SET ESTADO            = 'ANULADO',
                       RESUELTO_POR      = @usuario,
                       FECHA_RESOLUCION  = SYSDATETIME(),
                       MOTIVO_RESOLUCION = @motivo
                 WHERE ID_EMPRESA  = @empresa
                   AND ID_BORRADOR = @idBorr
                   AND ESTADO      = 'AUTORIZADO';";

            using (var cn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@usuario", SqlDbType.NVarChar, 50).Value = usuario ?? "";
                cmd.Parameters.Add("@motivo", SqlDbType.NVarChar, 1000).Value = motivo ?? "";
                cmd.Parameters.Add("@empresa", SqlDbType.NVarChar, 15).Value = empresa ?? "";
                cmd.Parameters.Add("@idBorr", SqlDbType.NVarChar, 20).Value = idBorrador ?? "";
                cn.Open();
                return cmd.ExecuteNonQuery();
            }
        }

        // =====================================================================
        //  CONSULTAS
        // =====================================================================

        private const string SELECT_ENC = @"
            SELECT ID_BORRADOR, ID_EMPRESA, FECHA, ID_CLIENTE, NOMBRE, NIT,
                   DIRECCION, CORREO, AGENTE, MONEDA, TOTAL, ESTADO,
                   ID_USR, DEPTO, CODIGO_OPERADOR, REGISTRO,
                   RESUELTO_POR, FECHA_RESOLUCION, MOTIVO_RESOLUCION,
                   CAST(CASE WHEN EXISTS (
                       SELECT 1 FROM dbo.BORR_NC_DET D
                       WHERE D.ID_EMPRESA = E.ID_EMPRESA
                         AND D.ID_BORRADOR = E.ID_BORRADOR
                         AND D.NC_PREVIA_SAP > 0
                   ) THEN 1 ELSE 0 END AS BIT) AS TIENE_NC_PREVIA
            FROM   dbo.BORR_NC_ENC E ";

        /// <summary>
        /// Listado con filtros combinables (null = sin filtrar).
        ///
        /// Reemplaza a rec_borr_listar, rec_borr_listar_seg, rec_auto_listar y
        /// rec_auto_listar_empr: los cuatro eran el mismo SELECT con distinto
        /// WHERE, y dos duplicaban su cuerpo para el caso AGENTE.
        ///
        /// El patrón "(@x IS NULL OR columna = @x)" deja que el optimizador
        /// resuelva. Para este volumen sobra, y evita armar SQL dinámico.
        /// </summary>
        public List<BorradorNcEncabezado> Listar(
            string empresa = null,
            string estado = null,
            string idUsr = null,
            string agente = null,
            DateTime? desde = null,
            DateTime? hasta = null)
        {
            string sql = SELECT_ENC + @"
                WHERE (@empresa IS NULL OR ID_EMPRESA = @empresa)
                  AND (@estado  IS NULL OR ESTADO     = @estado)
                  AND (@idUsr   IS NULL OR ID_USR     = @idUsr)
                  AND (@agente  IS NULL OR AGENTE     = @agente)
                  AND (@desde   IS NULL OR FECHA     >= @desde)
                  AND (@hasta   IS NULL OR FECHA     <= @hasta)
                ORDER BY FECHA DESC, ID_BORRADOR DESC;";

            var lista = new List<BorradorNcEncabezado>();

            using (var cn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@empresa", SqlDbType.NVarChar, 15).Value = Nulo(empresa);
                cmd.Parameters.Add("@estado", SqlDbType.VarChar, 20).Value = Nulo(estado);
                cmd.Parameters.Add("@idUsr", SqlDbType.NVarChar, 50).Value = Nulo(idUsr);
                cmd.Parameters.Add("@agente", SqlDbType.NVarChar, 155).Value = Nulo(agente);
                cmd.Parameters.Add("@desde", SqlDbType.Date).Value
                    = desde.HasValue ? (object)desde.Value.Date : DBNull.Value;
                cmd.Parameters.Add("@hasta", SqlDbType.Date).Value
                    = hasta.HasValue ? (object)hasta.Value.Date : DBNull.Value;

                cn.Open();
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) lista.Add(LeerEncabezado(r));
            }
            return lista;
        }

        /// <summary>
        /// Listado limitado al ámbito del usuario en Seguimiento: registros
        /// capturados por él o pertenecientes a cualquiera de sus agentes
        /// asignados. Los valores siempre se envían como parámetros SQL.
        /// </summary>
        public List<BorradorNcEncabezado> ListarVisibles(
            string empresa,
            string estado,
            string idUsr,
            IEnumerable<string> agentes,
            DateTime? desde = null,
            DateTime? hasta = null)
        {
            if (string.IsNullOrWhiteSpace(empresa))
                throw new ArgumentException(
                    "La empresa es obligatoria para consultar el seguimiento.", "empresa");

            var agentesNormalizados = (agentes ?? Enumerable.Empty<string>())
                .Select(x => (x ?? "").Trim())
                .Where(x => x.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var alcance = new List<string>();
            if (!string.IsNullOrWhiteSpace(idUsr)) alcance.Add("ID_USR = @idUsr");
            for (int i = 0; i < agentesNormalizados.Count; i++)
                alcance.Add("AGENTE = @agente" + i);

            if (alcance.Count == 0) return new List<BorradorNcEncabezado>();

            string sql = SELECT_ENC + @"
                WHERE ID_EMPRESA = @empresa
                  AND (@estado  IS NULL OR ESTADO     = @estado)
                  AND (@desde   IS NULL OR FECHA     >= @desde)
                  AND (@hasta   IS NULL OR FECHA     <= @hasta)
                  AND (" + string.Join(" OR ", alcance) + @")
                ORDER BY FECHA DESC, ID_BORRADOR DESC;";

            var lista = new List<BorradorNcEncabezado>();
            using (var cn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@empresa", SqlDbType.NVarChar, 15).Value = empresa.Trim();
                cmd.Parameters.Add("@estado", SqlDbType.VarChar, 20).Value = Nulo(estado);
                if (!string.IsNullOrWhiteSpace(idUsr))
                    cmd.Parameters.Add("@idUsr", SqlDbType.NVarChar, 50).Value = idUsr.Trim();
                for (int i = 0; i < agentesNormalizados.Count; i++)
                    cmd.Parameters.Add("@agente" + i, SqlDbType.NVarChar, 155).Value
                        = agentesNormalizados[i];
                cmd.Parameters.Add("@desde", SqlDbType.Date).Value
                    = desde.HasValue ? (object)desde.Value.Date : DBNull.Value;
                cmd.Parameters.Add("@hasta", SqlDbType.Date).Value
                    = hasta.HasValue ? (object)hasta.Value.Date : DBNull.Value;

                cn.Open();
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) lista.Add(LeerEncabezado(r));
            }
            return lista;
        }

        /// <summary>Un borrador con su detalle cargado. Null si no existe.</summary>
        public BorradorNcEncabezado ObtenerPorId(string empresa, string idBorrador)
        {
            BorradorNcEncabezado enc;

            using (var cn = new SqlConnection(_conn))
            {
                cn.Open();

                using (var cmd = new SqlCommand(
                    SELECT_ENC + " WHERE ID_EMPRESA = @empresa AND ID_BORRADOR = @idBorr;", cn))
                {
                    cmd.Parameters.Add("@empresa", SqlDbType.NVarChar, 15).Value = empresa ?? "";
                    cmd.Parameters.Add("@idBorr", SqlDbType.NVarChar, 20).Value = idBorrador ?? "";
                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read()) return null;
                        enc = LeerEncabezado(r);
                    }
                }

                const string sqlDet = @"
                    SELECT ID_EMPRESA, CONCEPTO, DOCUMENTO, FECHA_DOC, SERIE, NUMERO,
                           TOTAL_FACT, PAGADO, NC_PREVIA_SAP, MONEDA, DESCRIPCION, IMPORTE
                    FROM   dbo.BORR_NC_DET
                    WHERE  ID_EMPRESA = @empresa AND ID_BORRADOR = @idBorr
                    ORDER BY ROWID;";

                using (var cmd = new SqlCommand(sqlDet, cn))
                {
                    cmd.Parameters.Add("@empresa", SqlDbType.NVarChar, 15).Value = empresa ?? "";
                    cmd.Parameters.Add("@idBorr", SqlDbType.NVarChar, 20).Value = idBorrador ?? "";
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                            enc.Detalles.Add(new BorradorNcDetalle
                            {
                                IdEmpresa = Txt(r["ID_EMPRESA"]),
                                Concepto = Txt(r["CONCEPTO"]),
                                Documento = Txt(r["DOCUMENTO"]),
                                FechaDoc = Convert.ToDateTime(r["FECHA_DOC"]),
                                SerieFel = Txt(r["SERIE"]),
                                NumeroFel = Txt(r["NUMERO"]),
                                TotalFactura = Val(r["TOTAL_FACT"]),
                                Pagado = Val(r["PAGADO"]),
                                NcPreviaSap = Val(r["NC_PREVIA_SAP"]),
                                Moneda = Txt(r["MONEDA"]),
                                Descripcion = Txt(r["DESCRIPCION"]),
                                Importe = Val(r["IMPORTE"])
                            });
                    }
                }

                const string sqlAdjuntos = @"
                    SELECT ADJUNTO_ID, ID_BORRADOR, ID_EMPRESA, TIPO, NOMBRE,
                           EXTENSION, CONTENT_TYPE, TAMANO, URL, HASH_SHA256,
                           ORDEN, ID_USR, REGISTRO
                    FROM dbo.BORR_NC_ADJUNTO
                    WHERE ID_EMPRESA = @empresa AND ID_BORRADOR = @idBorr
                    ORDER BY ORDEN, ADJUNTO_ID;";

                using (var cmd = new SqlCommand(sqlAdjuntos, cn))
                {
                    cmd.Parameters.Add("@empresa", SqlDbType.NVarChar, 15).Value = empresa ?? "";
                    cmd.Parameters.Add("@idBorr", SqlDbType.NVarChar, 20).Value = idBorrador ?? "";
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read()) enc.Adjuntos.Add(LeerAdjunto(r, false));
                    }
                }
            }
            return enc;
        }

        public BorradorNcAdjunto ObtenerAdjunto(
            string empresa, string idBorrador, long adjuntoId)
        {
            const string sql = @"
                SELECT ADJUNTO_ID, ID_BORRADOR, ID_EMPRESA, TIPO, NOMBRE,
                       EXTENSION, CONTENT_TYPE, TAMANO, CONTENIDO, URL,
                       HASH_SHA256, ORDEN, ID_USR, REGISTRO
                FROM dbo.BORR_NC_ADJUNTO
                WHERE ID_EMPRESA = @empresa
                  AND ID_BORRADOR = @idBorr
                  AND ADJUNTO_ID = @adjuntoId;";

            using (var cn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@empresa", SqlDbType.NVarChar, 15).Value = empresa ?? "";
                cmd.Parameters.Add("@idBorr", SqlDbType.NVarChar, 20).Value = idBorrador ?? "";
                cmd.Parameters.Add("@adjuntoId", SqlDbType.BigInt).Value = adjuntoId;
                cn.Open();
                using (var r = cmd.ExecuteReader())
                    return r.Read() ? LeerAdjunto(r, true) : null;
            }
        }

        /// <summary>Solo el detalle — para refrescar la grilla sin recargar todo.</summary>
        public List<BorradorNcDetalle> ObtenerDetalle(string empresa, string idBorrador) =>
            ObtenerPorId(empresa, idBorrador)?.Detalles ?? new List<BorradorNcDetalle>();

        // =====================================================================
        //  HELPERS
        // =====================================================================

        private static BorradorNcEncabezado LeerEncabezado(IDataRecord r) =>
            new BorradorNcEncabezado
            {
                IdBorrador = Txt(r["ID_BORRADOR"]),
                IdEmpresa = Txt(r["ID_EMPRESA"]),
                Fecha = Convert.ToDateTime(r["FECHA"]),
                IdCliente = Txt(r["ID_CLIENTE"]),
                Nombre = Txt(r["NOMBRE"]),
                Nit = Txt(r["NIT"]),
                Direccion = Txt(r["DIRECCION"]),
                Correo = Txt(r["CORREO"]),
                Agente = Txt(r["AGENTE"]),
                Moneda = Txt(r["MONEDA"]),
                Total = Val(r["TOTAL"]),
                Estado = Txt(r["ESTADO"]),
                IdUsr = Txt(r["ID_USR"]),
                Depto = Txt(r["DEPTO"]),
                CodigoOperador = Txt(r["CODIGO_OPERADOR"]),
                Registro = r["REGISTRO"] != DBNull.Value
                                     ? (DateTime?)Convert.ToDateTime(r["REGISTRO"]) : null,
                ResueltoPor = Txt(r["RESUELTO_POR"]),
                FechaResolucion = r["FECHA_RESOLUCION"] != DBNull.Value
                                     ? (DateTime?)Convert.ToDateTime(r["FECHA_RESOLUCION"]) : null,
                MotivoResolucion = Txt(r["MOTIVO_RESOLUCION"]),
                TieneNcPrevia = Convert.ToBoolean(r["TIENE_NC_PREVIA"])
            };

        private static BorradorNcAdjunto LeerAdjunto(
            IDataRecord r, bool incluirContenido)
        {
            return new BorradorNcAdjunto
            {
                AdjuntoId = Convert.ToInt64(r["ADJUNTO_ID"]),
                IdBorrador = Txt(r["ID_BORRADOR"]),
                IdEmpresa = Txt(r["ID_EMPRESA"]),
                Tipo = Txt(r["TIPO"]),
                Nombre = Txt(r["NOMBRE"]),
                Extension = Txt(r["EXTENSION"]),
                ContentType = Txt(r["CONTENT_TYPE"]),
                Tamano = r["TAMANO"] != DBNull.Value
                    ? Convert.ToInt64(r["TAMANO"])
                    : 0L,
                Contenido = incluirContenido && r["CONTENIDO"] != DBNull.Value
                    ? (byte[])r["CONTENIDO"]
                    : null,
                Url = Txt(r["URL"]),
                HashSha256 = r["HASH_SHA256"] != DBNull.Value
                    ? (byte[])r["HASH_SHA256"]
                    : null,
                Orden = r["ORDEN"] != DBNull.Value
                    ? Convert.ToInt16(r["ORDEN"])
                    : (short)0,
                IdUsr = Txt(r["ID_USR"]),
                Registro = r["REGISTRO"] != DBNull.Value
                    ? (DateTime?)Convert.ToDateTime(r["REGISTRO"])
                    : null
            };
        }

        private static decimal Val(object o) =>
            o != null && o != DBNull.Value ? Convert.ToDecimal(o) : 0m;

        private static string Txt(object o) =>
            o != null && o != DBNull.Value ? Convert.ToString(o) : string.Empty;

        /// <summary>Cadena vacía o null → DBNull. Evita guardar '' donde va NULL.</summary>
        private static object Nulo(string s) =>
            string.IsNullOrWhiteSpace(s) ? (object)DBNull.Value : s.Trim();

        public void Dispose() { }
    }
}
