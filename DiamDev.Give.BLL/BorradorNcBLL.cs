using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using DiamDev.Give.DAL;
using DiamDev.Give.Entities;

namespace DiamDev.Give.BLL
{
    /// <summary>
    /// Lógica de negocio del módulo Borradores de Nota de Crédito.
    /// ★ VERSIÓN FINAL — reemplaza a cualquier versión anterior.
    ///
    /// Esta capa NO existía en el desktop: su NBorradoresNC era un passthrough
    /// de una línea, y todas las reglas vivían dentro de los eventos de los
    /// botones del formulario. Aquí es donde de verdad mejora la migración:
    /// las reglas quedan en un solo lugar, probables sin levantar la UI, y del
    /// lado del servidor, que es el único que no se puede saltar.
    ///
    /// Las 11 reglas están numeradas (R1..R11) para poder rastrearlas contra
    /// el plan de implementación.
    /// </summary>
    public class BorradorNcBLL
    {
        private const string PERMISO_AUTORIZAR = "Control.BorradorNC.Autorizar";
        private const string PERMISO_ANULAR = "Control.BorradorNC.Anular";

        private readonly BorradorNcDA _da = new BorradorNcDA();
        private readonly HanaRepository _hana = new HanaRepository();
        private readonly RolBL _roles = new RolBL();

        /// <summary>
        /// Tolerancia de comparación monetaria. SAP entrega decimales con 6
        /// posiciones y nosotros guardamos 3; comparar por igualdad exacta
        /// produce rechazos por redondeos invisibles para el usuario.
        /// </summary>
        private const decimal TOLERANCIA = 0.005m;

        /// <summary>
        /// ¿Las NC ya emitidas en SAP BLOQUEAN o solo ADVIERTEN?
        ///
        /// Por defecto advierten, y la razón es la incertidumbre sobre el dato:
        /// la vista INF_VRC_FACRNC no expone si una NC fue anulada en SAP, ni
        /// sabemos si una misma NC puede aparecer repartida entre varias
        /// facturas. Bloquear con un dato ambiguo impediría devoluciones
        /// legítimas en silencio, lo cual es peor que dejar pasar una y que el
        /// autorizador —que sí ve la lista— la detenga.
        ///
        /// Cuando esas dos preguntas se resuelvan con contabilidad, poner
        /// &lt;add key="BorradorNC.BloquearPorNcPrevia" value="true" /&gt;
        /// en el Web.config lo convierte en regla dura, sin recompilar.
        /// </summary>
        private static bool BloquearPorNcPrevia =>
            string.Equals(ConfigurationManager.AppSettings["BorradorNC.BloquearPorNcPrevia"],
                          "true", StringComparison.OrdinalIgnoreCase);

        // Temporal para pruebas. Cuando el appSetting cambie a false, el BLL
        // vuelve a exigir los permisos incluso si alguien lo invoca sin MVC.
        private static bool OmitirPermisos =>
            string.Equals(ConfigurationManager.AppSettings["BorradorNC.OmitirPermisos"],
                          "true", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Interruptor temporal mientras Créditos define la regla definitiva.
        /// Ausente o false: el selector muestra únicamente facturas con saldo
        /// pendiente. true: recupera el comportamiento anterior.
        /// </summary>
        private static bool MostrarFacturasPagadas =>
            string.Equals(ConfigurationManager.AppSettings["BorradorNC.MostrarFacturasPagadas"],
                          "true", StringComparison.OrdinalIgnoreCase);

        // =====================================================================
        //  CONSULTA DE FACTURAS  (HANA + datos locales)
        // =====================================================================

        /// <summary>
        /// Facturas del cliente disponibles para NC, enriquecidas con lo ya
        /// comprometido en borradores locales y con las NC previas de SAP.
        ///
        /// Es la mejora más visible sobre el desktop: allá el usuario elegía la
        /// factura, llenaba el importe, presionaba Agregar y RECIÉN ahí
        /// descubría —por un MessageBox— que el monto no cabía. Aquí lo ve
        /// antes de elegir.
        ///
        ///   Disponible      = DocTotal − Acumulado local        (tope duro, R4)
        ///   DisponibleNeto  = Disponible − NC previas en SAP    (advertencia)
        ///
        /// PaidToDate no reduce el tope. Temporalmente, las facturas pagadas
        /// por completo se excluyen de este selector mediante configuración.
        /// Cualquier diferencia, por pequeña que sea, mantiene la factura
        /// abierta: no se aplica tolerancia a esta comparación.
        /// </summary>
        public List<FacturaBorradorNc> BuscarFacturas(
            string empresa, string clienteId, string agente, string filtro)
        {
            var facturas = _hana.ObtenerFacturasBorradorNc(empresa, clienteId, agente, filtro);

            if (!MostrarFacturasPagadas)
                facturas = facturas.Where(EsFacturaAbierta).ToList();

            if (facturas.Count == 0) return facturas;

            var docs = facturas.Select(f => f.DocNum).ToList();

            // Dos consultas por lote, no dos por cada factura.
            var acumulados = _da.ObtenerAcumuladoDocumentos(empresa, docs);
            var ncPrevias = _hana.ObtenerNotasCreditoPrevias(empresa, docs);

            foreach (var f in facturas)
            {
                string doc = f.DocNum ?? "";

                decimal acum;
                f.Acumulado = acumulados.TryGetValue(doc, out acum) ? acum : 0m;

                List<NotaCreditoPreviaSap> notas;
                if (ncPrevias.TryGetValue(doc, out notas))
                {
                    f.NotasPrevias = notas;
                    f.NcPreviaSap = notas.Sum(n => n.Total);
                }

                f.Disponible = f.DocTotal - f.Acumulado;
                f.DisponibleNeto = f.Disponible - f.NcPreviaSap;
                f.GeneraSaldoAFavor = f.Pagado >= f.DocTotal - TOLERANCIA;

                if (f.Acumulado > 0)
                    f.BorradoresRelacionados = string.Join(", ",
                        _da.ObtenerBorradoresConDocumento(empresa, doc));
            }

            return facturas;
        }

        /// <summary>
        /// Una factura conserva estado abierto mientras exista cualquier saldo
        /// pendiente. Solo la igualdad o un pago superior al total la cierran.
        /// </summary>
        private static bool EsFacturaAbierta(FacturaBorradorNc factura)
        {
            return factura != null && factura.Pagado < factura.DocTotal;
        }

        /// <summary>
        /// Estado de UNA factura. Lo usa la UI mientras el usuario teclea el
        /// importe, para avisar en vivo antes de que intente agregar la línea.
        /// </summary>
        public FacturaBorradorNc ObtenerEstadoFactura(
            string empresa, string documento, decimal docTotal, decimal pagado)
        {
            var notas = _hana.ObtenerNotasCreditoPrevias(empresa, documento);

            var f = new FacturaBorradorNc
            {
                DocNum = documento,
                DocTotal = docTotal,
                Pagado = pagado,
                Acumulado = _da.ObtenerAcumuladoDocumento(empresa, documento),
                NotasPrevias = notas,
                NcPreviaSap = notas.Sum(n => n.Total)
            };

            f.Disponible = f.DocTotal - f.Acumulado;
            f.DisponibleNeto = f.Disponible - f.NcPreviaSap;
            f.GeneraSaldoAFavor = f.Pagado >= f.DocTotal - TOLERANCIA;
            f.BorradoresRelacionados = f.Acumulado > 0
                ? string.Join(", ", _da.ObtenerBorradoresConDocumento(empresa, documento))
                : null;

            return f;
        }

        // =====================================================================
        //  GUARDADO
        // =====================================================================

        /// <summary>
        /// Valida las 11 reglas y guarda. Devuelve el ID generado, más las
        /// advertencias que el usuario debe ver aunque no impidan guardar.
        /// </summary>
        public ResultadoBorradorNc GuardarBorrador(
            BorradorNcEncabezado enc, string loginUsuario)
        {
            if (enc == null)
                return ResultadoBorradorNc.Error("No se recibió información del borrador.");

            var advertencias = new List<string>();

            // ── R6: encabezado completo ──────────────────────────────────────
            if (string.IsNullOrWhiteSpace(enc.IdEmpresa))
                return ResultadoBorradorNc.Error("Debe seleccionar una empresa.");
            if (string.IsNullOrWhiteSpace(enc.IdCliente))
                return ResultadoBorradorNc.Error("Debe seleccionar un cliente.");
            if (string.IsNullOrWhiteSpace(enc.Agente))
                return ResultadoBorradorNc.Error("No se pudo determinar el agente del cliente.");
            if (string.IsNullOrWhiteSpace(enc.Moneda))
                return ResultadoBorradorNc.Error("Debe seleccionar una moneda.");
            if (enc.Detalles == null || enc.Detalles.Count == 0)
                return ResultadoBorradorNc.Error("El borrador debe tener al menos una línea.");

            enc.IdEmpresa = enc.IdEmpresa.Trim().ToUpperInvariant();
            enc.IdCliente = enc.IdCliente.Trim();
            enc.Agente = enc.Agente.Trim();
            enc.Moneda = enc.Moneda.Trim().ToUpperInvariant();
            enc.IdUsr = (loginUsuario ?? "").Trim();
            enc.Depto = Limpiar(enc.Depto);
            enc.CodigoOperador = Limpiar(enc.CodigoOperador);

            if (enc.IdEmpresa.Length > 15)
                return ResultadoBorradorNc.Error("La empresa excede los 15 caracteres permitidos.");
            if (enc.IdCliente.Length > 20)
                return ResultadoBorradorNc.Error("El código del cliente excede los 20 caracteres permitidos.");
            if (enc.Agente.Length > 155)
                return ResultadoBorradorNc.Error("El agente excede los 155 caracteres permitidos.");
            if (enc.Moneda.Length > 5)
                return ResultadoBorradorNc.Error("La moneda excede los 5 caracteres permitidos.");
            if (enc.IdUsr.Length == 0 || enc.IdUsr.Length > 50)
                return ResultadoBorradorNc.Error("No se pudo identificar correctamente al usuario que captura.");
            if (Longitud(enc.Depto) > 50 || Longitud(enc.CodigoOperador) > 50)
                return ResultadoBorradorNc.Error(
                    "La asignación de empresa del usuario contiene datos que exceden el tamaño permitido.");

            // El navegador solo propone estos datos. Antes de aplicar las reglas,
            // se reconstruyen desde HANA para impedir que una petición manipulada
            // cambie el cliente, el total, la fecha o la moneda de una factura.
            var clienteSap = _hana.BuscarClientes(enc.IdEmpresa, enc.Agente)
                .FirstOrDefault(c => string.Equals(c.CardCode, enc.IdCliente,
                                                   StringComparison.OrdinalIgnoreCase));
            if (clienteSap == null)
                return ResultadoBorradorNc.Error(
                    "El cliente ya no está disponible para el agente seleccionado en SAP.");

            enc.IdCliente = Limpiar(clienteSap.CardCode);
            enc.Nombre = Limpiar(clienteSap.CardName);
            enc.Nit = Limpiar(clienteSap.LicTradNum);
            enc.Agente = string.IsNullOrWhiteSpace(clienteSap.SlpName)
                ? enc.Agente.Trim() : clienteSap.SlpName.Trim();
            enc.Direccion = string.IsNullOrWhiteSpace(enc.Direccion)
                ? Limpiar(clienteSap.Address) : enc.Direccion.Trim();
            enc.Correo = string.IsNullOrWhiteSpace(enc.Correo)
                ? Limpiar(clienteSap.Email) : enc.Correo.Trim();

            if (string.IsNullOrWhiteSpace(enc.IdCliente) || string.IsNullOrWhiteSpace(enc.Nombre))
                return ResultadoBorradorNc.Error(
                    "El cliente no tiene código y nombre completos en SAP.");
            if (string.IsNullOrWhiteSpace(enc.Nit))
                return ResultadoBorradorNc.Error(
                    "El cliente no tiene NIT registrado en SAP. Corríjalo antes de continuar.");

            if (Longitud(enc.IdCliente) > 20)
                return ResultadoBorradorNc.Error("El código del cliente de SAP excede 20 caracteres.");
            if (Longitud(enc.Nombre) > 200)
                return ResultadoBorradorNc.Error("El nombre del cliente de SAP excede 200 caracteres.");
            if (Longitud(enc.Nit) > 50)
                return ResultadoBorradorNc.Error("El NIT del cliente de SAP excede 50 caracteres.");
            if (Longitud(enc.Agente) > 155)
                return ResultadoBorradorNc.Error("El nombre del agente de SAP excede 155 caracteres.");
            if (Longitud(enc.Direccion) > 200)
                return ResultadoBorradorNc.Error("La dirección no puede exceder 200 caracteres.");
            if (Longitud(enc.Correo) > 100)
                return ResultadoBorradorNc.Error("El correo no puede exceder 100 caracteres.");

            var facturasSap = _hana.ObtenerFacturasBorradorNc(
                enc.IdEmpresa, enc.IdCliente, enc.Agente, "")
                .GroupBy(f => (f.DocNum ?? "").Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var detalle in enc.Detalles)
            {
                string documento = (detalle.Documento ?? "").Trim();
                FacturaBorradorNc facturaSap;
                if (documento.Length == 0 || !facturasSap.TryGetValue(documento, out facturaSap))
                    return ResultadoBorradorNc.Error(string.Format(
                        "El documento {0} no pertenece al cliente o ya no está disponible en SAP.",
                        documento.Length == 0 ? "indicado" : documento));

                detalle.Documento = facturaSap.DocNum;
                detalle.FechaDoc = facturaSap.DocDate;
                detalle.SerieFel = facturaSap.SerieFel;
                detalle.NumeroFel = facturaSap.NumeroFel;
                detalle.TotalFactura = facturaSap.DocTotal;
                detalle.Pagado = facturaSap.Pagado;
                detalle.Moneda = facturaSap.Moneda;
            }

            // Una sola ida a HANA para todas las líneas. Además de reducir la
            // latencia, todas las advertencias del borrador se calculan sobre la
            // misma fotografía temporal de INF_VRC_FACRNC.
            var notasPreviasSap = _hana.ObtenerNotasCreditoPrevias(
                enc.IdEmpresa,
                enc.Detalles.Select(d => d.Documento)
                    .Distinct(StringComparer.OrdinalIgnoreCase).ToList());

            // ── R8: la serie debe existir ANTES de abrir la transacción ──────
            if (!_da.ExisteSerie(enc.IdEmpresa))
                return ResultadoBorradorNc.Error(string.Format(
                    "No hay serie de borradores configurada para {0}. " +
                    "Pídale al administrador que la registre.", enc.IdEmpresa));

            // ── R2: sin documentos repetidos en el mismo borrador ────────────
            var repetido = enc.Detalles
                .GroupBy(d => (d.Documento ?? "").Trim(), StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(g => g.Count() > 1);
            if (repetido != null)
                return ResultadoBorradorNc.Error(string.Format(
                    "El documento {0} está agregado más de una vez.", repetido.Key));

            // ── Validación línea por línea ───────────────────────────────────
            foreach (var d in enc.Detalles)
            {
                string doc = (d.Documento ?? "").Trim();

                // R5: línea completa
                if (!ConceptosBorradorNc.EsValido(d.Concepto))
                    return ResultadoBorradorNc.Error(string.Format(
                        "Concepto no válido: '{0}'. Use DEVOLUCION, DESCUENTO u OTROS.",
                        d.Concepto));
                if (doc.Length == 0)
                    return ResultadoBorradorNc.Error("Hay una línea sin número de documento.");
                if (string.IsNullOrWhiteSpace(d.Descripcion))
                    return ResultadoBorradorNc.Error(string.Format(
                        "La línea del documento {0} no tiene descripción.", doc));
                d.Descripcion = d.Descripcion.Trim();
                if (Longitud(d.Descripcion) > 500)
                    return ResultadoBorradorNc.Error(string.Format(
                        "La descripción del documento {0} no puede exceder 500 caracteres.", doc));
                // El esquema persiste tres decimales. Normalizar antes de validar
                // y sumar evita que TOTAL difiera de la suma del detalle por
                // redondeos independientes en SQL Server.
                d.Importe = decimal.Round(d.Importe, 3, MidpointRounding.AwayFromZero);
                if (d.Importe <= 0)
                    return ResultadoBorradorNc.Error(string.Format(
                        "El importe del documento {0} debe ser mayor a cero.", doc));
                if (d.TotalFactura <= 0)
                    return ResultadoBorradorNc.Error(string.Format(
                        "El documento {0} no trae el total de la factura.", doc));

                // R1: la fecha del documento no puede ser posterior a la del borrador
                if (d.FechaDoc.Date > enc.Fecha.Date)
                    return ResultadoBorradorNc.Error(string.Format(
                        "La fecha del documento {0} ({1:dd/MM/yyyy}) es posterior a la del " +
                        "borrador ({2:dd/MM/yyyy}).", doc, d.FechaDoc, enc.Fecha));

                // R11: una sola moneda por borrador.
                // En el desktop, elegir una factura sobrescribía la moneda del
                // encabezado (CbbMoneda.Text = Variables.Moneda), así que se
                // podían mezclar GTQ y USD y el total los sumaba como iguales.
                string monedaLinea = string.IsNullOrWhiteSpace(d.Moneda)
                                        ? enc.Moneda : d.Moneda.Trim();
                if (!string.Equals(monedaLinea, enc.Moneda, StringComparison.OrdinalIgnoreCase))
                    return ResultadoBorradorNc.Error(string.Format(
                        "El documento {0} está en {1} y el borrador es en {2}. " +
                        "Un borrador no puede mezclar monedas: haga uno por cada una.",
                        doc, monedaLinea, enc.Moneda));

                // ── R3 + R4: el tope duro es el valor de la factura menos lo
                //    ya comprometido en NUESTROS borradores. Datos propios,
                //    certeza total, se bloquea sin dudar.
                decimal acumulado = _da.ObtenerAcumuladoDocumento(enc.IdEmpresa, doc);
                decimal disponible = d.TotalFactura - acumulado;

                if (d.Importe - disponible > TOLERANCIA)
                {
                    var otros = _da.ObtenerBorradoresConDocumento(enc.IdEmpresa, doc);

                    // R3: si el problema viene de otros borradores, decir cuáles.
                    // El legado solo decía "el documento ya existe en otro
                    // borrador y/o sobrepasa el valor de la factura", sin
                    // indicar cuál, dejando al usuario sin forma de resolverlo.
                    string detalle = otros.Count > 0
                        ? string.Format(" Ya hay {0:N2} comprometido en: {1}.",
                                        acumulado, string.Join(", ", otros))
                        : string.Empty;

                    return ResultadoBorradorNc.Error(string.Format(
                        "El importe de {0:N2} sobrepasa lo disponible del documento {1} " +
                        "({2:N2}).{3}", d.Importe, doc,
                        disponible < 0 ? 0 : disponible, detalle));
                }

                // ── NC previas en SAP: advertencia o bloqueo, según config ───
                List<NotaCreditoPreviaSap> notasPrevias;
                if (!notasPreviasSap.TryGetValue(doc, out notasPrevias))
                    notasPrevias = new List<NotaCreditoPreviaSap>();
                d.NcPreviaSap = notasPrevias.Sum(n => n.Total);

                if (d.NcPreviaSap > 0)
                {
                    decimal neto = disponible - d.NcPreviaSap;

                    if (d.Importe - neto > TOLERANCIA)
                    {
                        string msg = string.Format(
                            "El documento {0} ya tiene {1:N2} en notas de crédito emitidas en " +
                            "SAP ({2}). Considerándolas, lo disponible sería {3:N2}.",
                            doc, d.NcPreviaSap,
                            string.Join(", ", notasPrevias.Select(n => "NC " + n.Nota)),
                            neto < 0 ? 0 : neto);

                        if (BloquearPorNcPrevia)
                            return ResultadoBorradorNc.Error(msg);

                        advertencias.Add(msg);
                    }
                    else
                    {
                        advertencias.Add(string.Format(
                            "El documento {0} ya tiene {1:N2} en NC previas de SAP.",
                            doc, d.NcPreviaSap));
                    }
                }

                // ── Factura ya pagada: no bloquea, pero hay que decirlo ──────
                if (d.Pagado >= d.TotalFactura - TOLERANCIA)
                    advertencias.Add(string.Format(
                        "El documento {0} ya está pagado por completo: la nota de crédito " +
                        "generará saldo a favor del cliente.", doc));

                d.Documento = doc;
                d.IdEmpresa = enc.IdEmpresa;
                d.Concepto = d.Concepto.Trim().ToUpperInvariant();
                d.Moneda = enc.Moneda;
            }

            // ── El total lo calcula el servidor ──────────────────────────────
            // El desktop lo tomaba de un TextBox y lo enviaba como string.
            // Nunca confiamos en un total que venga del navegador.
            enc.Total = enc.Detalles.Sum(d => d.Importe);
            enc.Estado = EstadosBorradorNc.Pendiente;

            if (enc.Total <= 0)
                return ResultadoBorradorNc.Error("El total del borrador debe ser mayor a cero.");

            try
            {
                _da.GuardarBorradorCompleto(enc);

                var ok = ResultadoBorradorNc.Ok(enc.IdBorrador, string.Format(
                    "Borrador {0} creado. Queda pendiente de autorización.", enc.IdBorrador));
                ok.Advertencias = advertencias;
                return ok;
            }
            catch (Exception ex)
            {
                if (ex is BorradorNcDisponibilidadException)
                    return ResultadoBorradorNc.Error(ex.Message);

                // La UNIQUE de documento por borrador es la última defensa:
                // si dos peticiones simultáneas pasaran la validación, truena aquí.
                if (ex.Message.IndexOf("UQ_BORR_NC_DET_DOC",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                    return ResultadoBorradorNc.Error(
                        "El borrador tiene un documento repetido. Revise el detalle.");
                throw;
            }
        }

        // =====================================================================
        //  RESOLUCIÓN
        // =====================================================================

        /// <summary>
        /// Autoriza o rechaza un borrador PENDIENTE.
        ///
        /// El permiso ya se validó en el Controller con [Permiso], pero el BLL
        /// no debe confiar en su llamador: si mañana alguien invoca esto desde
        /// un job o desde otro controller, las reglas siguen puestas.
        /// </summary>
        public ResultadoBorradorNc Resolver(
            string empresa, string idBorrador, string usuario,
            string accion, string motivo)
        {
            // R9 también se defiende aquí. El atributo MVC sigue siendo la
            // primera barrera, pero un job o controller futuro no puede saltarse
            // el permiso invocando directamente esta capa.
            if (!OmitirPermisos &&
                !_roles.AutorizacionPermisoPorUsuario(usuario, PERMISO_AUTORIZAR))
                return ResultadoBorradorNc.Error(
                    "El usuario no tiene permiso para autorizar o rechazar borradores.");

            if (string.IsNullOrWhiteSpace(empresa) || string.IsNullOrWhiteSpace(idBorrador))
                return ResultadoBorradorNc.Error("Falta identificar el borrador.");

            string estado = (accion ?? "").Trim().ToUpperInvariant();

            if (estado != EstadosBorradorNc.Autorizado &&
                estado != EstadosBorradorNc.Rechazado)
                return ResultadoBorradorNc.Error("Acción no válida.");

            // R10: rechazar exige motivo; autorizar no.
            if (estado == EstadosBorradorNc.Rechazado && string.IsNullOrWhiteSpace(motivo))
                return ResultadoBorradorNc.Error("Debe indicar el motivo del rechazo.");
            if (Longitud(motivo) > 1000)
                return ResultadoBorradorNc.Error("El motivo o comentario no puede exceder 1000 caracteres.");

            int filas = _da.Resolver(empresa, idBorrador, estado, usuario, motivo);

            if (filas == 0)
            {
                // El "AND ESTADO='PENDIENTE'" del UPDATE es el candado optimista.
                // 0 filas = alguien más ya lo resolvió. Le decimos quién, en vez
                // de dejar al usuario adivinando por qué no pasó nada.
                var actual = _da.ObtenerPorId(empresa, idBorrador);
                if (actual == null)
                    return ResultadoBorradorNc.Error("El borrador no existe.");

                return ResultadoBorradorNc.Error(string.Format(
                    "Este borrador ya fue {0} por {1} el {2:dd/MM/yyyy HH:mm}.",
                    actual.Estado.ToLower(), actual.ResueltoPor, actual.FechaResolucion));
            }

            return ResultadoBorradorNc.Ok(idBorrador,
                estado == EstadosBorradorNc.Autorizado
                    ? "Borrador autorizado."
                    : "Borrador rechazado.");
        }

        /// <summary>
        /// Anula un borrador YA AUTORIZADO. Funcionalidad nueva: el desktop
        /// solo podía rechazar lo pendiente, así que un borrador autorizado por
        /// error se quedaba comprometiendo el saldo de la factura para siempre.
        /// Requiere el permiso Control.BorradorNC.Anular.
        /// </summary>
        public ResultadoBorradorNc Anular(
            string empresa, string idBorrador, string usuario, string motivo)
        {
            if (!OmitirPermisos &&
                !_roles.AutorizacionPermisoPorUsuario(usuario, PERMISO_ANULAR))
                return ResultadoBorradorNc.Error(
                    "El usuario no tiene permiso para anular borradores.");

            if (string.IsNullOrWhiteSpace(motivo))
                return ResultadoBorradorNc.Error("Debe indicar el motivo de la anulación.");
            if (Longitud(motivo) > 1000)
                return ResultadoBorradorNc.Error("El motivo de la anulación no puede exceder 1000 caracteres.");

            int filas = _da.Anular(empresa, idBorrador, usuario, motivo);

            if (filas == 0)
            {
                var actual = _da.ObtenerPorId(empresa, idBorrador);
                if (actual == null)
                    return ResultadoBorradorNc.Error("El borrador no existe.");

                return ResultadoBorradorNc.Error(string.Format(
                    "Solo se pueden anular borradores autorizados. Este está {0}.",
                    actual.Estado.ToLower()));
            }

            return ResultadoBorradorNc.Ok(idBorrador, "Borrador anulado.");
        }

        // =====================================================================
        //  LISTADOS
        // =====================================================================

        /// <summary>
        /// Borradores pendientes visibles para el usuario (pestaña de captura).
        ///
        /// El legado tenía esto partido entre rec_borr_listar —que duplicaba su
        /// cuerpo para el caso AGENTE— y el parámetro @tipo, que en realidad era
        /// el departamento. Aquí se decide por permiso:
        ///
        ///   - puedeVerTodos        → todos
        ///   - el usuario es AGENTE → los de su nombre de agente
        ///   - resto                → los que él capturó
        /// </summary>
        public List<BorradorNcEncabezado> ListarPendientes(
            string login, bool puedeVerTodos, string agente, string empresa = null)
        {
            if (puedeVerTodos)
                return _da.Listar(empresa: empresa, estado: EstadosBorradorNc.Pendiente);

            if (!string.IsNullOrWhiteSpace(agente))
                return _da.Listar(empresa: empresa,
                                  estado: EstadosBorradorNc.Pendiente, agente: agente);

            return _da.Listar(empresa: empresa,
                              estado: EstadosBorradorNc.Pendiente, idUsr: login);
        }

        /// <summary>
        /// Pestaña de Seguimiento: lo ya resuelto. Equivale a
        /// rec_borr_listar_seg, que filtraba STATUS IN ('R','X').
        /// </summary>
        public List<BorradorNcEncabezado> ListarSeguimiento(
            string login, bool puedeVerTodos, string agente,
            string empresa = null, DateTime? desde = null, DateTime? hasta = null)
        {
            var resueltos = new[] { EstadosBorradorNc.Autorizado,
                                    EstadosBorradorNc.Rechazado,
                                    EstadosBorradorNc.Anulado };
            var todos = new List<BorradorNcEncabezado>();

            foreach (var estado in resueltos)
            {
                if (puedeVerTodos)
                    todos.AddRange(_da.Listar(empresa, estado, null, null, desde, hasta));
                else if (!string.IsNullOrWhiteSpace(agente))
                    todos.AddRange(_da.Listar(empresa, estado, null, agente, desde, hasta));
                else
                    todos.AddRange(_da.Listar(empresa, estado, login, null, desde, hasta));
            }

            return todos.OrderByDescending(b => b.Fecha)
                        .ThenByDescending(b => b.IdBorrador)
                        .ToList();
        }

        /// <summary>Bandeja de autorización: todos los pendientes.</summary>
        public List<BorradorNcEncabezado> ListarParaAutorizar(string empresa = null) =>
            _da.Listar(empresa: empresa, estado: EstadosBorradorNc.Pendiente);

        /// <summary>Un borrador con su detalle. Null si no existe.</summary>
        public BorradorNcEncabezado ObtenerPorId(string empresa, string idBorrador) =>
            _da.ObtenerPorId(empresa, idBorrador);

        /// <summary>
        /// Renglones originales en SAP de todas las facturas asociadas al
        /// borrador. La consulta es por lote para no hacer una llamada HANA por
        /// cada documento.
        /// </summary>
        public List<FacturaDetalleSap> ObtenerDetallesFacturas(
            string empresa, string clienteId, IEnumerable<string> documentos) =>
            _hana.ObtenerDetallesFacturas(empresa, clienteId, documentos);

        /// <summary>Prefijo de la serie, solo para mostrarlo en la UI.</summary>
        public string ObtenerPrefijoSerie(string empresa) =>
            _da.ObtenerPrefijoSerie(empresa);

        /// <summary>
        /// NC previas de SAP contra un documento. Lo usa la pantalla de
        /// autorización para mostrarle al aprobador qué se emitió antes —era
        /// una pestaña completa en FrmAutorizaciones del desktop.
        /// </summary>
        public List<NotaCreditoPreviaSap> ObtenerNotasCreditoPrevias(
            string empresa, string documento) =>
            _hana.ObtenerNotasCreditoPrevias(empresa, documento);

        // =====================================================================
        //  CLIENTES  (reutiliza lo que ya existe para recibos)
        // =====================================================================

        /// <summary>
        /// Clientes del agente en SAP. El SP INF_CLIENTES_REC es el mismo que
        /// usa ReciboCaja, así que se reaprovecha HanaRepository.BuscarClientes.
        /// El filtro de texto se aplica en memoria: el SP no lo acepta y la
        /// lista de un agente cabe de sobra.
        /// </summary>
        public List<ClienteHana> BuscarClientes(string empresa, string agente, string filtro)
        {
            var clientes = _hana.BuscarClientes(empresa, agente);
            if (string.IsNullOrWhiteSpace(filtro)) return clientes.Take(50).ToList();

            string f = filtro.Trim();
            return clientes.Where(c => Contiene(c.CardName, f) ||
                                       Contiene(c.CardCode, f) ||
                                       Contiene(c.LicTradNum, f))
                            .Take(50)
                            .ToList();
        }

        private static bool Contiene(string texto, string busqueda) =>
            !string.IsNullOrEmpty(texto) &&
            CultureInfo.InvariantCulture.CompareInfo.IndexOf(
                texto, busqueda, CompareOptions.IgnoreCase) >= 0;

        private static string Limpiar(string valor) =>
            string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

        private static int Longitud(string valor) => valor == null ? 0 : valor.Length;
    }
}
