using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using DiamDev.Give.BLL;
using DiamDev.Give.Entities;
using DiamDev.Give.UI.App_Start;
using DiamDev.Give.UI.Models;

namespace DiamDev.Give.UI.Controllers
{
    [Authorize]
    [HandleError]
    public class BorradorNcController : Controller
    {
        private const string PERMISO_VER = "Control.BorradorNC.Ver";
        private const string PERMISO_GUARDAR = "Control.BorradorNC.Guardar";
        private const string PERMISO_AUTORIZAR = "Control.BorradorNC.Autorizar";
        private const string PERMISO_ANULAR = "Control.BorradorNC.Anular";
        private const string PERMISO_VER_TODOS = "Control.BorradorNC.VerTodos";
        private const string PERMISO_DASHBOARD = "Control.BorradorNC.Dashboard";

        private static bool HabilitarEnlaces =>
            string.Equals(ConfigurationManager.AppSettings["BorradorNC.HabilitarEnlaces"],
                          "true", StringComparison.OrdinalIgnoreCase);

        private readonly BorradorNcBLL _bll = new BorradorNcBLL();
        private readonly BorradorNcDashboardBLL _dashboard = new BorradorNcDashboardBLL();
        private readonly UsuarioEmpresaBL _usuarioEmpresa = new UsuarioEmpresaBL();
        private readonly RolBL _roles = new RolBL();

        [BorradorNcPermiso(PERMISO_VER)]
        public ActionResult Index()
        {
            CustomHelper.setTitle("Borradores de nota de crédito", "Captura y seguimiento");
            return View(CrearModeloInicial());
        }

        [HttpGet]
        [BorradorNcPermiso(PERMISO_VER)]
        public JsonResult BuscarClientes(string empresa, string codigoOperador, string filtro)
        {
            return JsonGet(() =>
            {
                string agenteEfectivo = ResolverAgente(empresa, codigoOperador);
                return _bll.BuscarClientes(empresa, agenteEfectivo, filtro ?? "");
            });
        }

        [HttpGet]
        [BorradorNcPermiso(PERMISO_VER)]
        public JsonResult BuscarFacturas(string empresa, string clienteId,
                                         string codigoOperador, string filtro)
        {
            return JsonGet(() =>
            {
                if (string.IsNullOrWhiteSpace(clienteId))
                    throw new InvalidOperationException("Seleccione un cliente antes de buscar facturas.");

                string agenteEfectivo = ResolverAgente(empresa, codigoOperador);
                return _bll.BuscarFacturas(empresa, clienteId, agenteEfectivo, filtro ?? "");
            });
        }

        [HttpGet]
        [BorradorNcPermiso(PERMISO_VER)]
        public ActionResult DetalleFactura(string empresa, string clienteId,
                                           string codigoOperador, string documento)
        {
            if (string.IsNullOrWhiteSpace(clienteId) ||
                string.IsNullOrWhiteSpace(documento))
                return HttpNotFound("No se indicó una factura válida.");

            var factura = ObtenerFacturaAccesible(
                empresa, clienteId, codigoOperador, documento);

            if (factura == null)
                return HttpNotFound(
                    "La factura no existe, ya no está abierta o no pertenece al cliente y agente seleccionados.");

            string urlPdf = _bll.ObtenerUrlPdfFactura(
                empresa, factura.CardCode, factura.DocNum);
            var productos = _bll.ObtenerDetallesFacturas(
                empresa, factura.CardCode, new[] { factura.DocNum });
            var modelo = new BorradorNcFacturaConsultaViewModel
            {
                Empresa = (empresa ?? "").Trim(),
                Documento = factura.DocNum,
                CodigoOperador = (codigoOperador ?? "").Trim(),
                FechaDoc = factura.DocDate.ToString("yyyy-MM-dd"),
                ClienteId = factura.CardCode,
                ClienteNombre = factura.CardName,
                Agente = factura.SlpName,
                Moneda = factura.Moneda,
                SerieFel = factura.SerieFel,
                NumeroFel = factura.NumeroFel,
                TotalFactura = factura.DocTotal,
                Pagado = factura.Pagado,
                SaldoSap = Math.Max(0m, factura.DocTotal - factura.Pagado),
                Acumulado = factura.Acumulado,
                NcPreviaSap = factura.NcPreviaSap,
                Disponible = Math.Max(0m, factura.Disponible),
                DisponibleNeto = Math.Max(0m, factura.DisponibleNeto),
                PdfFacturaDisponible = !string.IsNullOrWhiteSpace(urlPdf),
                Productos = productos
                    .OrderBy(x => x.NumeroLinea)
                    .Select(ProyectarProductoFactura)
                    .ToList()
            };

            CustomHelper.setTitle(
                "Factura " + factura.DocNum,
                "Detalle de productos y servicios");
            return View(modelo);
        }

        [HttpGet]
        public ActionResult DetalleFacturaBorrador(
            string empresa, string idBorrador, string documento,
            string origen = null)
        {
            var enc = _bll.ObtenerPorId(empresa, idBorrador);
            if (enc == null) return HttpNotFound("Borrador no encontrado.");
            if (!PuedeConsultarFacturaBorrador(enc))
                return new HttpUnauthorizedResult();

            var factura = BuscarDetalleFactura(enc, documento);
            if (factura == null)
                return HttpNotFound("La factura no pertenece al borrador indicado.");

            bool desdeAutorizaciones =
                string.Equals(origen, "autorizaciones",
                              StringComparison.OrdinalIgnoreCase) &&
                TienePermiso(PERMISO_AUTORIZAR);
            var modelo = CrearModeloFacturaBorrador(
                enc, factura, desdeAutorizaciones);

            CustomHelper.setTitle(
                "Factura " + factura.Documento,
                "Detalle de productos y servicios");
            return View("DetalleFactura", modelo);
        }

        [HttpGet]
        public ActionResult DetalleDocumentoPrevio(
            string empresa, string idBorrador, string factura,
            string documento, string clase, string origen = null)
        {
            var enc = _bll.ObtenerPorId(empresa, idBorrador);
            if (enc == null) return HttpNotFound("Borrador no encontrado.");
            if (!PuedeConsultarFacturaBorrador(enc))
                return new HttpUnauthorizedResult();

            var previo = BuscarDocumentoPrevio(
                enc, factura, documento, clase);
            if (previo == null)
                return HttpNotFound(
                    "El documento previo no está relacionado con las facturas del borrador.");

            var detalle = _bll.ObtenerDetalleDocumentoPrevio(
                enc.IdEmpresa, previo.Clase, previo.DocEntry, enc.IdCliente);
            if (detalle == null)
                return HttpNotFound("El documento ya no está disponible en SAP.");

            detalle.Factura = previo.Factura;
            detalle.TiposOrigen = previo.TiposOrigen;
            bool desdeAutorizaciones =
                string.Equals(origen, "autorizaciones",
                              StringComparison.OrdinalIgnoreCase) &&
                TienePermiso(PERMISO_AUTORIZAR);
            var modelo = ProyectarDetalleDocumentoPrevio(
                enc, detalle, desdeAutorizaciones);

            CustomHelper.setTitle(
                modelo.ClaseTexto + " " + modelo.Documento,
                "Detalle del documento previo en SAP");
            return View(modelo);
        }

        [HttpGet]
        [BorradorNcPermiso(PERMISO_VER)]
        public ActionResult AbrirFacturaSap(string empresa, string clienteId,
                                            string codigoOperador, string documento)
        {
            if (string.IsNullOrWhiteSpace(clienteId) ||
                string.IsNullOrWhiteSpace(documento))
                return HttpNotFound("No se indicó una factura válida.");

            var factura = ObtenerFacturaAccesible(
                empresa, clienteId, codigoOperador, documento);
            if (factura == null)
                return HttpNotFound(
                    "La factura no existe, ya no está abierta o no pertenece al cliente y agente seleccionados.");

            string urlPdf = _bll.ObtenerUrlPdfFactura(
                empresa, factura.CardCode, factura.DocNum);
            if (string.IsNullOrWhiteSpace(urlPdf))
                return HttpNotFound(
                    "SAP no tiene una dirección válida para el PDF de esta factura.");

            return Redirect(urlPdf);
        }

        [HttpGet]
        public ActionResult AbrirFacturaSapBorrador(
            string empresa, string idBorrador, string documento)
        {
            var enc = _bll.ObtenerPorId(empresa, idBorrador);
            if (enc == null) return HttpNotFound("Borrador no encontrado.");
            if (!PuedeConsultarFacturaBorrador(enc))
                return new HttpUnauthorizedResult();

            var factura = BuscarDetalleFactura(enc, documento);
            if (factura == null)
                return HttpNotFound("La factura no pertenece al borrador indicado.");

            string urlPdf = _bll.ObtenerUrlPdfFactura(
                enc.IdEmpresa, enc.IdCliente, factura.Documento);
            if (string.IsNullOrWhiteSpace(urlPdf))
                return HttpNotFound(
                    "SAP no tiene una dirección válida para el PDF de esta factura.");

            return Redirect(urlPdf);
        }

        [HttpGet]
        public ActionResult AbrirDocumentoPrevioSap(
            string empresa, string idBorrador, string factura,
            string documento, string clase)
        {
            var enc = _bll.ObtenerPorId(empresa, idBorrador);
            if (enc == null) return HttpNotFound("Borrador no encontrado.");
            if (!PuedeConsultarFacturaBorrador(enc))
                return new HttpUnauthorizedResult();

            var previo = BuscarDocumentoPrevio(
                enc, factura, documento, clase);
            if (previo == null)
                return HttpNotFound(
                    "El documento previo no está relacionado con las facturas del borrador.");

            string urlPdf = _bll.ObtenerUrlPdfDocumentoPrevio(
                enc.IdEmpresa, previo.Clase, previo.DocEntry, enc.IdCliente);
            if (string.IsNullOrWhiteSpace(urlPdf))
                return HttpNotFound(
                    "SAP no tiene una dirección válida para el PDF de este documento.");

            return Redirect(urlPdf);
        }

        [HttpGet]
        [BorradorNcPermiso(PERMISO_VER)]
        public JsonResult ObtenerEstadoFactura(string empresa, string documento,
                                               decimal docTotal, decimal pagado)
        {
            return JsonGet(() =>
            {
                ValidarEmpresa(empresa);
                return _bll.ObtenerEstadoFactura(empresa, documento, docTotal, pagado);
            });
        }

        [HttpGet]
        [BorradorNcPermiso(PERMISO_VER)]
        public JsonResult ObtenerSerie(string empresa)
        {
            return JsonGet(() =>
            {
                ValidarEmpresa(empresa);
                return new { Prefijo = _bll.ObtenerPrefijoSerie(empresa) };
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [BorradorNcPermiso(PERMISO_GUARDAR)]
        public JsonResult Guardar(
            GuardarBorradorNcRequest request,
            IEnumerable<HttpPostedFileBase> archivos)
        {
            try
            {
                if (request == null)
                    return Json(new { ok = false, msg = "No se recibió información del borrador." });

                if (!HabilitarEnlaces &&
                    (request.Enlaces ?? new List<BorradorNcEnlaceRequest>())
                        .Any(x => x != null && !string.IsNullOrWhiteSpace(x.Url)))
                    return Json(new
                    {
                        ok = false,
                        msg = "La captura de enlaces está deshabilitada temporalmente."
                    });

                var asignacion = ValidarOperador(request.IdEmpresa, request.CodigoOperador);
                DateTime fecha;
                if (!TryFecha(request.Fecha, out fecha))
                    return Json(new { ok = false, msg = "La fecha del borrador no es válida." });

                var detalles = new List<BorradorNcDetalle>();
                foreach (var item in request.Detalles ?? new List<BorradorNcDetalleRequest>())
                {
                    DateTime fechaDoc;
                    if (!TryFecha(item.FechaDoc, out fechaDoc))
                        return Json(new
                        {
                            ok = false,
                            msg = "La fecha del documento " + (item.Documento ?? "") + " no es válida."
                        });

                    detalles.Add(new BorradorNcDetalle
                    {
                        Concepto = item.Concepto,
                        Documento = item.Documento,
                        FechaDoc = fechaDoc,
                        SerieFel = item.SerieFel,
                        NumeroFel = item.NumeroFel,
                        TotalFactura = item.TotalFactura,
                        Pagado = item.Pagado,
                        NcPreviaSap = item.NcPreviaSap,
                        Moneda = item.Moneda,
                        Descripcion = item.Descripcion,
                        Importe = item.Importe
                    });
                }

                var codigo = _usuarioEmpresa.ParseCodigo(asignacion.Codigo);
                string agente = codigo.AgenteNombre;

                var adjuntos = new List<BorradorNcAdjunto>();
                foreach (var enlace in request.Enlaces ?? new List<BorradorNcEnlaceRequest>())
                {
                    if (enlace == null || string.IsNullOrWhiteSpace(enlace.Url)) continue;

                    adjuntos.Add(new BorradorNcAdjunto
                    {
                        Tipo = TiposAdjuntoBorradorNc.Enlace,
                        Url = enlace.Url
                    });
                }

                var archivosRecibidos = (archivos ?? Enumerable.Empty<HttpPostedFileBase>())
                    .Where(x => x != null && x.ContentLength > 0)
                    .ToList();
                if (archivosRecibidos.Count > BorradorNcBLL.MaximoArchivosAdjuntos)
                    return Json(new
                    {
                        ok = false,
                        msg = "Puede adjuntar como máximo " +
                              BorradorNcBLL.MaximoArchivosAdjuntos + " archivos."
                    });

                long totalArchivos = 0;
                foreach (var archivo in archivosRecibidos)
                {
                    if (archivo.ContentLength > BorradorNcBLL.MaximoBytesPorArchivo)
                        return Json(new
                        {
                            ok = false,
                            msg = "El archivo " + Path.GetFileName(archivo.FileName) +
                                  " excede el límite de 10 MB."
                        });

                    totalArchivos += archivo.ContentLength;
                    if (totalArchivos > BorradorNcBLL.MaximoBytesAdjuntos)
                        return Json(new
                        {
                            ok = false,
                            msg = "Los archivos adjuntos exceden el límite total de 25 MB."
                        });

                    byte[] contenido;
                    using (var memoria = new MemoryStream())
                    {
                        archivo.InputStream.CopyTo(memoria);
                        contenido = memoria.ToArray();
                    }

                    adjuntos.Add(new BorradorNcAdjunto
                    {
                        Tipo = TiposAdjuntoBorradorNc.Archivo,
                        Nombre = Path.GetFileName(archivo.FileName),
                        ContentType = archivo.ContentType,
                        Contenido = contenido
                    });
                }

                var enc = new BorradorNcEncabezado
                {
                    IdEmpresa = request.IdEmpresa,
                    Fecha = fecha,
                    IdCliente = request.IdCliente,
                    Nombre = request.Nombre,
                    Nit = request.Nit,
                    Direccion = request.Direccion,
                    Correo = request.Correo,
                    Agente = agente,
                    Moneda = request.Moneda,
                    Depto = asignacion.DEPTO_RECIBO,
                    CodigoOperador = asignacion.Codigo,
                    Detalles = detalles,
                    Adjuntos = adjuntos
                };

                var resultado = _bll.GuardarBorrador(enc, User.Identity.Name);
                return Json(new
                {
                    ok = resultado.Exito,
                    msg = resultado.Mensaje,
                    idBorrador = resultado.IdBorrador,
                    advertencias = resultado.Advertencias ?? new List<string>()
                });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, msg = "Error inesperado: " + ex.Message });
            }
        }

        [HttpGet]
        [BorradorNcPermiso(PERMISO_VER)]
        public JsonResult Listar(string empresa = null)
        {
            return JsonGet(() =>
            {
                var filas = new List<BorradorNcEncabezado>();
                foreach (var contexto in ContextosConsulta(empresa))
                {
                    filas.AddRange(_bll.ListarPendientes(
                        User.Identity.Name,
                        contexto.Agentes,
                        contexto.Empresa));
                }
                return ProyectarLista(SinDuplicados(filas));
            });
        }

        [HttpGet]
        [BorradorNcPermiso(PERMISO_VER)]
        public JsonResult ListarSeguimiento(string empresa = null, string desde = null, string hasta = null)
        {
            return JsonGet(() =>
            {
                DateTime fechaDesde;
                DateTime fechaHasta;
                DateTime? fDesde = TryFecha(desde, out fechaDesde) ? (DateTime?)fechaDesde : null;
                DateTime? fHasta = TryFecha(hasta, out fechaHasta) ? (DateTime?)fechaHasta : null;

                var filas = new List<BorradorNcEncabezado>();
                foreach (var contexto in ContextosConsulta(empresa))
                {
                    filas.AddRange(_bll.ListarSeguimiento(
                        User.Identity.Name,
                        contexto.Agentes,
                        contexto.Empresa,
                        fDesde,
                        fHasta));
                }
                return ProyectarLista(SinDuplicados(filas));
            });
        }

        [HttpGet]
        [BorradorNcPermiso(PERMISO_VER)]
        public JsonResult ObtenerDetalle(string empresa, string idBorrador)
        {
            return JsonGet(() =>
            {
                ValidarEmpresa(empresa);
                var enc = _bll.ObtenerPorId(empresa, idBorrador);
                if (enc == null) throw new InvalidOperationException("Borrador no encontrado.");
                if (!PuedeConsultarSeguimiento(enc))
                    throw new UnauthorizedAccessException("No tiene acceso a este borrador.");
                return ProyectarDocumento(enc);
            });
        }

        [HttpGet]
        [BorradorNcPermiso(PERMISO_VER)]
        public JsonResult ObtenerDetallesFacturas(string empresa, string idBorrador)
        {
            return JsonGet(() =>
            {
                ValidarEmpresa(empresa);
                var enc = _bll.ObtenerPorId(empresa, idBorrador);
                if (enc == null) throw new InvalidOperationException("Borrador no encontrado.");
                if (!PuedeConsultarSeguimiento(enc))
                    throw new UnauthorizedAccessException("No tiene acceso a este borrador.");

                return ProyectarContenidoFacturas(enc);
            });
        }

        [HttpGet]
        [BorradorNcPermiso(PERMISO_VER)]
        public JsonResult ObtenerDocumentosPrevios(
            string empresa, string idBorrador)
        {
            return JsonGet(() =>
            {
                ValidarEmpresa(empresa);
                var enc = _bll.ObtenerPorId(empresa, idBorrador);
                if (enc == null) throw new InvalidOperationException("Borrador no encontrado.");
                if (!PuedeConsultarSeguimiento(enc))
                    throw new UnauthorizedAccessException("No tiene acceso a este borrador.");

                return ProyectarDocumentosPrevios(enc);
            });
        }

        [BorradorNcPermiso(PERMISO_AUTORIZAR)]
        public ActionResult Autorizaciones()
        {
            CustomHelper.setTitle("Autorización de borradores NC", "Bandeja de pendientes");
            return View(CrearModeloInicial());
        }

        [HttpGet]
        [BorradorNcPermiso(PERMISO_AUTORIZAR)]
        public JsonResult ListarPendientes(string empresa = null)
        {
            return JsonGet(() =>
            {
                var filas = new List<BorradorNcEncabezado>();
                foreach (var nombre in EmpresasConsulta(empresa))
                    filas.AddRange(_bll.ListarParaAutorizar(nombre));
                return ProyectarLista(SinDuplicados(filas));
            });
        }

        [HttpGet]
        [BorradorNcPermiso(PERMISO_AUTORIZAR)]
        public JsonResult ObtenerDetalleAutorizacion(string empresa, string idBorrador)
        {
            return JsonGet(() =>
            {
                ValidarEmpresa(empresa);
                var enc = _bll.ObtenerPorId(empresa, idBorrador);
                if (enc == null) throw new InvalidOperationException("Borrador no encontrado.");
                return ProyectarDocumento(enc);
            });
        }

        [HttpGet]
        [BorradorNcPermiso(PERMISO_AUTORIZAR)]
        public JsonResult ObtenerDetallesFacturasAutorizacion(string empresa, string idBorrador)
        {
            return JsonGet(() =>
            {
                ValidarEmpresa(empresa);
                var enc = _bll.ObtenerPorId(empresa, idBorrador);
                if (enc == null) throw new InvalidOperationException("Borrador no encontrado.");
                return ProyectarContenidoFacturas(enc);
            });
        }

        [HttpGet]
        [BorradorNcPermiso(PERMISO_AUTORIZAR)]
        public JsonResult ObtenerDocumentosPreviosAutorizacion(
            string empresa, string idBorrador)
        {
            return JsonGet(() =>
            {
                ValidarEmpresa(empresa);
                var enc = _bll.ObtenerPorId(empresa, idBorrador);
                if (enc == null) throw new InvalidOperationException("Borrador no encontrado.");
                if (!string.Equals(enc.Estado, EstadosBorradorNc.Pendiente,
                                   StringComparison.OrdinalIgnoreCase))
                    throw new UnauthorizedAccessException(
                        "El borrador ya no está pendiente de autorización.");

                return ProyectarDocumentosPrevios(enc);
            });
        }

        [HttpGet]
        [BorradorNcPermiso(PERMISO_AUTORIZAR)]
        public JsonResult ObtenerNotasCreditoPrevias(string empresa, string documento)
        {
            return JsonGet(() =>
            {
                ValidarEmpresa(empresa);
                return _bll.ObtenerNotasCreditoPrevias(empresa, documento);
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [BorradorNcPermiso(PERMISO_AUTORIZAR)]
        public new JsonResult Resolver(ResolverBorradorNcRequest request)
        {
            try
            {
                if (request == null)
                    return Json(new { ok = false, msg = "No se recibió la decisión." });
                ValidarEmpresa(request.Empresa);
                var resultado = _bll.Resolver(request.Empresa, request.IdBorrador,
                                              User.Identity.Name, request.Accion, request.Motivo);
                return Json(new { ok = resultado.Exito, msg = resultado.Mensaje });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, msg = "Error inesperado: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [BorradorNcPermiso(PERMISO_ANULAR)]
        public JsonResult Anular(AnularBorradorNcRequest request)
        {
            try
            {
                if (request == null)
                    return Json(new { ok = false, msg = "No se recibió la anulación." });
                ValidarEmpresa(request.Empresa);
                var resultado = _bll.Anular(request.Empresa, request.IdBorrador,
                                            User.Identity.Name, request.Motivo);
                return Json(new { ok = resultado.Exito, msg = resultado.Mensaje });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, msg = "Error inesperado: " + ex.Message });
            }
        }

        public ActionResult Imprimir(string empresa, string idBorrador)
        {
            var enc = _bll.ObtenerPorId(empresa, idBorrador);
            if (enc == null) return HttpNotFound("Borrador no encontrado.");
            if (!PuedeImprimir(enc)) return new HttpUnauthorizedResult();
            if (TienePermiso(PERMISO_DASHBOARD))
                _dashboard.RegistrarEvento(enc.IdEmpresa, enc.IdBorrador, "IMPRESO",
                    User.Identity.Name, "Impresión interna individual", Request.UserHostAddress);
            return View(enc);
        }

        [BorradorNcPermiso(PERMISO_DASHBOARD)]
        public ActionResult DashboardBNC()
        {
            CustomHelper.setTitle("Dashboard Borradores NC", "Seguimiento y auditoría de Créditos");
            return View(new BorradorNcDashboardViewModel
            {
                AlcanceGlobal = TienePermiso(PERMISO_VER_TODOS),
                MaximoImpresion = _dashboard.MaximoImpresion,
                UsuarioActual = User.Identity.Name
            });
        }

        [HttpGet]
        [BorradorNcPermiso(PERMISO_DASHBOARD)]
        public JsonResult ConsultarDashboardBNC(BorradorNcDashboardFiltro filtro)
        {
            return JsonGet(() => _dashboard.Consultar(
                filtro ?? new BorradorNcDashboardFiltro(), CrearAlcanceDashboard()));
        }

        [HttpGet]
        [BorradorNcPermiso(PERMISO_DASHBOARD)]
        public JsonResult ObtenerBitacoraBNC(string empresa, string idBorrador)
        {
            return JsonGet(() =>
            {
                var enc = _bll.ObtenerPorId(empresa, idBorrador);
                if (enc == null) throw new InvalidOperationException("Borrador no encontrado.");
                if (!PuedeConsultarDashboard(enc))
                    throw new UnauthorizedAccessException("No tiene acceso a este borrador.");
                return _dashboard.ConsultarBitacora(empresa, idBorrador);
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [BorradorNcPermiso(PERMISO_DASHBOARD)]
        public ActionResult ExportarDashboardBNC(BorradorNcDashboardFiltro filtro, string[] claves)
        {
            filtro = filtro ?? new BorradorNcDashboardFiltro();
            var alcance = CrearAlcanceDashboard();
            var pagina = _dashboard.ConsultarParaExportar(filtro, alcance);
            var facturas = _dashboard.ConsultarFacturas(filtro, alcance);
            var seleccion = new HashSet<string>(
                (claves ?? new string[0]).Where(x => !string.IsNullOrWhiteSpace(x)),
                StringComparer.OrdinalIgnoreCase);
            if (seleccion.Count > 0)
            {
                pagina.Filas = pagina.Filas.Where(x => seleccion.Contains(
                    x.IdEmpresa + "|" + x.IdBorrador)).ToList();
                facturas = facturas.Where(x => seleccion.Contains(
                    x.IdEmpresa + "|" + x.IdBorrador)).ToList();
            }

            using (var paquete = new ExcelPackage())
            {
                var borradores = paquete.Workbook.Worksheets.Add("Borradores");
                string[] encabezados = { "Empresa", "Borrador", "Fecha", "Registro", "Estado",
                    "Cliente", "Nombre", "NIT", "Agente", "Moneda", "Total", "Creado por",
                    "Resuelto por", "Fecha resolución", "Facturas", "Adjuntos", "Antecedentes SAP" };
                for (int c = 0; c < encabezados.Length; c++) borradores.Cells[1, c + 1].Value = encabezados[c];
                int fila = 2;
                foreach (var item in pagina.Filas)
                {
                    borradores.Cells[fila, 1].Value = item.IdEmpresa;
                    borradores.Cells[fila, 2].Value = item.IdBorrador;
                    borradores.Cells[fila, 3].Value = item.Fecha;
                    borradores.Cells[fila, 4].Value = item.Registro;
                    borradores.Cells[fila, 5].Value = item.Estado;
                    borradores.Cells[fila, 6].Value = item.IdCliente;
                    borradores.Cells[fila, 7].Value = item.Nombre;
                    borradores.Cells[fila, 8].Value = item.Nit;
                    borradores.Cells[fila, 9].Value = item.Agente;
                    borradores.Cells[fila, 10].Value = item.Moneda;
                    borradores.Cells[fila, 11].Value = item.Total;
                    borradores.Cells[fila, 12].Value = item.IdUsr;
                    borradores.Cells[fila, 13].Value = item.ResueltoPor;
                    borradores.Cells[fila, 14].Value = item.FechaResolucion;
                    borradores.Cells[fila, 15].Value = item.Facturas;
                    borradores.Cells[fila, 16].Value = item.Adjuntos;
                    borradores.Cells[fila, 17].Value = item.TieneAntecedentesSap ? "Sí" : "No";
                    fila++;
                }
                FormatearHojaExcel(borradores, encabezados.Length, pagina.Filas.Count + 1);
                borradores.Column(3).Style.Numberformat.Format = "dd/mm/yyyy";
                borradores.Column(4).Style.Numberformat.Format = "dd/mm/yyyy hh:mm";
                borradores.Column(11).Style.Numberformat.Format = "#,##0.00";
                borradores.Column(14).Style.Numberformat.Format = "dd/mm/yyyy hh:mm";

                var detalle = paquete.Workbook.Worksheets.Add("Facturas");
                string[] encabezadosFactura = { "Empresa", "Borrador", "Documento", "Fecha",
                    "Concepto", "Descripción", "Moneda", "Total factura", "Importe solicitado" };
                for (int c = 0; c < encabezadosFactura.Length; c++) detalle.Cells[1, c + 1].Value = encabezadosFactura[c];
                fila = 2;
                foreach (var item in facturas)
                {
                    detalle.Cells[fila, 1].Value = item.IdEmpresa;
                    detalle.Cells[fila, 2].Value = item.IdBorrador;
                    detalle.Cells[fila, 3].Value = item.Documento;
                    detalle.Cells[fila, 4].Value = item.FechaDocumento;
                    detalle.Cells[fila, 5].Value = item.Concepto;
                    detalle.Cells[fila, 6].Value = item.Descripcion;
                    detalle.Cells[fila, 7].Value = item.Moneda;
                    detalle.Cells[fila, 8].Value = item.TotalFactura;
                    detalle.Cells[fila, 9].Value = item.ImporteSolicitado;
                    fila++;
                }
                FormatearHojaExcel(detalle, encabezadosFactura.Length, facturas.Count + 1);
                detalle.Column(4).Style.Numberformat.Format = "dd/mm/yyyy";
                detalle.Column(8).Style.Numberformat.Format = "#,##0.00";
                detalle.Column(9).Style.Numberformat.Format = "#,##0.00";

                _dashboard.RegistrarEvento(filtro.Empresa ?? "*", "*", "EXPORTADO",
                    User.Identity.Name, pagina.Filas.Count + " borradores exportados",
                    Request.UserHostAddress);
                return File(paquete.GetAsByteArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "BorradoresNC_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".xlsx");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [BorradorNcPermiso(PERMISO_DASHBOARD)]
        public ActionResult ImprimirLoteBNC(string[] claves)
        {
            var unicas = (claves ?? new string[0]).Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (unicas.Count == 0) return new HttpStatusCodeResult(400, "Seleccione al menos un borrador.");
            if (unicas.Count > _dashboard.MaximoImpresion)
                return new HttpStatusCodeResult(400, "El máximo por lote es " + _dashboard.MaximoImpresion + ".");

            var documentos = new List<BorradorNcEncabezado>();
            foreach (string clave in unicas)
            {
                string[] partes = clave.Split(new[] { '|' }, 2);
                if (partes.Length != 2) return new HttpStatusCodeResult(400, "Selección no válida.");
                var enc = _bll.ObtenerPorId(partes[0], partes[1]);
                if (enc == null || !PuedeConsultarDashboard(enc)) return new HttpUnauthorizedResult();
                documentos.Add(enc);
            }
            foreach (var enc in documentos)
                _dashboard.RegistrarEvento(enc.IdEmpresa, enc.IdBorrador, "IMPRESO_LOTE",
                    User.Identity.Name, "Impresión interna por lote", Request.UserHostAddress);
            return View("ImprimirLote", documentos);
        }

        private BorradorNcIndexViewModel CrearModeloInicial()
        {
            var modelo = new BorradorNcIndexViewModel
            {
                UsuarioActual = User.Identity.Name,
                EsAgente = EsAgente(),
                PuedeVerTodos = TienePermiso(PERMISO_VER_TODOS),
                PuedeAutorizar = TienePermiso(PERMISO_AUTORIZAR),
                PuedeAnular = TienePermiso(PERMISO_ANULAR),
                PermiteAdjuntarEnlaces = HabilitarEnlaces,
                Conceptos = ConceptosBorradorNc.Todos().OrderBy(x => x).ToList()
            };

            modelo.Empresas = Asignaciones()
                .Where(x => !string.IsNullOrWhiteSpace(x.Codigo))
                .GroupBy(x => x.EmpresaId)
                .Select(g =>
                {
                    return new BorradorNcEmpresaViewModel
                    {
                        EmpresaId = g.Key,
                        Nombre = _usuarioEmpresa.GetEmpresaNombre(g.Key),
                        Operadores = g
                            .GroupBy(x => x.Codigo.Trim(), StringComparer.OrdinalIgnoreCase)
                            .Select(x => x.First())
                            .Select(x =>
                            {
                                var codigo = _usuarioEmpresa.ParseCodigo(x.Codigo);
                                return new BorradorNcOperadorViewModel
                                {
                                    Codigo = x.Codigo.Trim(),
                                    Agente = codigo.AgenteNombre,
                                    Depto = (x.DEPTO_RECIBO ?? "").Trim()
                                };
                            })
                            .Where(x => !string.IsNullOrWhiteSpace(x.Agente))
                            .OrderBy(x => OrdenOperador(x.Codigo))
                            .ThenBy(x => x.Codigo)
                            .ToList()
                    };
                })
                .Where(x => x.Nombre != "DESCONOCIDA" && x.Operadores.Count > 0)
                .OrderBy(x => x.Nombre)
                .ToList();

            return modelo;
        }

        private bool EsAgente()
        {
            return _roles.UsuarioTieneRol(User.Identity.Name, "AGENTE");
        }

        private List<UsuarioEmpresa> Asignaciones()
        {
            return _usuarioEmpresa.ObtenerPorUsuarioId(CustomHelper.getUserId());
        }

        private UsuarioEmpresa ValidarEmpresa(string empresa)
        {
            string normalizada = (empresa ?? "").Trim();
            var asignacion = Asignaciones().FirstOrDefault(x =>
                string.Equals(_usuarioEmpresa.GetEmpresaNombre(x.EmpresaId), normalizada,
                              StringComparison.OrdinalIgnoreCase));
            if (asignacion == null)
                throw new UnauthorizedAccessException("La empresa no está asignada al usuario actual.");
            return asignacion;
        }

        private UsuarioEmpresa ValidarOperador(string empresa, string codigoOperador)
        {
            string empresaNormalizada = (empresa ?? "").Trim();
            string codigoNormalizado = (codigoOperador ?? "").Trim();
            if (codigoNormalizado.Length == 0)
                throw new InvalidOperationException("Seleccione el agente con el que operará.");

            var asignacion = Asignaciones().FirstOrDefault(x =>
                string.Equals(_usuarioEmpresa.GetEmpresaNombre(x.EmpresaId), empresaNormalizada,
                              StringComparison.OrdinalIgnoreCase) &&
                string.Equals((x.Codigo ?? "").Trim(), codigoNormalizado,
                              StringComparison.OrdinalIgnoreCase));
            if (asignacion == null)
                throw new UnauthorizedAccessException(
                    "El agente seleccionado no está asignado al usuario actual para esta empresa.");

            return asignacion;
        }

        private string ResolverAgente(string empresa, string codigoOperador)
        {
            var asignacion = ValidarOperador(empresa, codigoOperador);
            string agente = _usuarioEmpresa.ParseCodigo(asignacion.Codigo).AgenteNombre;
            if (string.IsNullOrWhiteSpace(agente))
                throw new InvalidOperationException(
                    "El código seleccionado no tiene un agente SAP configurado.");
            return agente;
        }

        private FacturaBorradorNc ObtenerFacturaAccesible(
            string empresa, string clienteId, string codigoOperador,
            string documento)
        {
            string agenteEfectivo = ResolverAgente(empresa, codigoOperador);
            string clienteNormalizado = (clienteId ?? "").Trim();
            string documentoNormalizado = (documento ?? "").Trim();

            return _bll.BuscarFacturas(
                    empresa, clienteNormalizado, agenteEfectivo,
                    documentoNormalizado)
                .FirstOrDefault(x =>
                    string.Equals((x.DocNum ?? "").Trim(), documentoNormalizado,
                                  StringComparison.OrdinalIgnoreCase) &&
                    string.Equals((x.CardCode ?? "").Trim(), clienteNormalizado,
                                  StringComparison.OrdinalIgnoreCase));
        }

        private bool PuedeConsultarFacturaBorrador(BorradorNcEncabezado enc)
        {
            if (TienePermiso(PERMISO_DASHBOARD) && PuedeConsultarDashboard(enc)) return true;
            if (TienePermiso(PERMISO_AUTORIZAR) &&
                string.Equals(enc.Estado, EstadosBorradorNc.Pendiente,
                              StringComparison.OrdinalIgnoreCase)) return true;
            try
            {
                bool puedeDesdeSeguimiento =
                    TienePermiso(PERMISO_VER) && PuedeConsultarSeguimiento(enc);
                return puedeDesdeSeguimiento;
            }
            catch (UnauthorizedAccessException) { return false; }
        }

        private static BorradorNcDetalle BuscarDetalleFactura(
            BorradorNcEncabezado enc, string documento)
        {
            string documentoNormalizado = (documento ?? "").Trim();
            if (documentoNormalizado.Length == 0) return null;

            return (enc.Detalles ?? new List<BorradorNcDetalle>())
                .FirstOrDefault(x => string.Equals(
                    (x.Documento ?? "").Trim(), documentoNormalizado,
                    StringComparison.OrdinalIgnoreCase));
        }

        private DocumentoPrevioSap BuscarDocumentoPrevio(
            BorradorNcEncabezado enc, string factura, string documento,
            string clase)
        {
            var facturaGuardada = BuscarDetalleFactura(enc, factura);
            if (facturaGuardada == null) return null;

            string documentoNormalizado = (documento ?? "").Trim();
            string claseNormalizada = (clase ?? "").Trim();
            if (documentoNormalizado.Length == 0 || claseNormalizada.Length == 0)
                return null;

            return _bll.ObtenerDocumentosPrevios(
                    enc.IdEmpresa, new[] { facturaGuardada.Documento })
                .FirstOrDefault(x =>
                    string.Equals((x.Documento ?? "").Trim(),
                                  documentoNormalizado,
                                  StringComparison.OrdinalIgnoreCase) &&
                    string.Equals((x.Clase ?? "").Trim(),
                                  claseNormalizada,
                                  StringComparison.OrdinalIgnoreCase));
        }

        private BorradorNcFacturaConsultaViewModel CrearModeloFacturaBorrador(
            BorradorNcEncabezado enc, BorradorNcDetalle factura,
            bool desdeAutorizaciones)
        {
            var estado = _bll.ObtenerEstadoFactura(
                enc.IdEmpresa, factura.Documento,
                factura.TotalFactura, factura.Pagado);
            decimal importeDocumento = (enc.Detalles ?? new List<BorradorNcDetalle>())
                .Where(x => string.Equals(
                    (x.Documento ?? "").Trim(),
                    (factura.Documento ?? "").Trim(),
                    StringComparison.OrdinalIgnoreCase))
                .Sum(x => x.Importe);
            bool borradorComprometeSaldo =
                EstadosBorradorNc.ComprometeSaldo(enc.Estado);
            decimal acumuladoOtros = Math.Max(
                0m,
                estado.Acumulado -
                (borradorComprometeSaldo ? importeDocumento : 0m));
            decimal disponible = Math.Max(
                0m, factura.TotalFactura - acumuladoOtros);
            string urlPdf = _bll.ObtenerUrlPdfFactura(
                enc.IdEmpresa, enc.IdCliente, factura.Documento);
            var productos = _bll.ObtenerDetallesFacturas(
                enc.IdEmpresa, enc.IdCliente, new[] { factura.Documento });

            return new BorradorNcFacturaConsultaViewModel
            {
                Empresa = enc.IdEmpresa,
                IdBorrador = enc.IdBorrador,
                Documento = factura.Documento,
                CodigoOperador = enc.CodigoOperador,
                FechaDoc = factura.FechaDoc.ToString("yyyy-MM-dd"),
                ClienteId = enc.IdCliente,
                ClienteNombre = enc.Nombre,
                Agente = enc.Agente,
                Moneda = string.IsNullOrWhiteSpace(factura.Moneda)
                    ? enc.Moneda
                    : factura.Moneda,
                SerieFel = factura.SerieFel,
                NumeroFel = factura.NumeroFel,
                TotalFactura = factura.TotalFactura,
                Pagado = factura.Pagado,
                SaldoSap = Math.Max(
                    0m, factura.TotalFactura - factura.Pagado),
                Acumulado = acumuladoOtros,
                NcPreviaSap = estado.NcPreviaSap,
                Disponible = disponible,
                DisponibleNeto = Math.Max(
                    0m, disponible - estado.NcPreviaSap),
                PdfFacturaDisponible = !string.IsNullOrWhiteSpace(urlPdf),
                DesdeAutorizaciones = desdeAutorizaciones,
                Productos = productos
                    .OrderBy(x => x.NumeroLinea)
                    .Select(ProyectarProductoFactura)
                    .ToList()
            };
        }

        private static int OrdenOperador(string codigo)
        {
            string valor = (codigo ?? "").Trim();
            int separador = valor.IndexOf('-');
            int numero;
            return separador > 0 && int.TryParse(valor.Substring(0, separador), out numero)
                ? numero
                : int.MaxValue;
        }

        private IEnumerable<ContextoConsulta> ContextosConsulta(string empresa)
        {
            var asignaciones = string.IsNullOrWhiteSpace(empresa)
                ? Asignaciones()
                : Asignaciones().Where(x =>
                    string.Equals(_usuarioEmpresa.GetEmpresaNombre(x.EmpresaId), empresa.Trim(),
                                  StringComparison.OrdinalIgnoreCase)).ToList();

            if (!string.IsNullOrWhiteSpace(empresa) && asignaciones.Count == 0)
                throw new UnauthorizedAccessException(
                    "La empresa no está asignada al usuario actual.");

            return asignaciones.Select(x => new
                {
                    Empresa = _usuarioEmpresa.GetEmpresaNombre(x.EmpresaId),
                    Agente = _usuarioEmpresa.ParseCodigo(x.Codigo).AgenteNombre
                })
                .Where(x => x.Empresa != "DESCONOCIDA")
                .GroupBy(x => x.Empresa, StringComparer.OrdinalIgnoreCase)
                .Select(g => new ContextoConsulta
                {
                    Empresa = g.Key,
                    Agentes = g.Select(x => (x.Agente ?? "").Trim())
                               .Where(x => x.Length > 0)
                               .Distinct(StringComparer.OrdinalIgnoreCase)
                               .ToList()
                });
        }

        private IEnumerable<string> EmpresasConsulta(string empresa)
        {
            if (!string.IsNullOrWhiteSpace(empresa))
            {
                ValidarEmpresa(empresa);
                return new[] { empresa };
            }

            return Asignaciones().Select(x => _usuarioEmpresa.GetEmpresaNombre(x.EmpresaId))
                .Where(x => x != "DESCONOCIDA")
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private bool PuedeConsultarSeguimiento(BorradorNcEncabezado enc)
        {
            if (string.Equals(enc.IdUsr, User.Identity.Name,
                              StringComparison.OrdinalIgnoreCase)) return true;

            return ContextosConsulta(enc.IdEmpresa)
                .SelectMany(x => x.Agentes)
                .Any(x => string.Equals(x, enc.Agente,
                                        StringComparison.OrdinalIgnoreCase));
        }

        [HttpGet]
        public ActionResult DescargarAdjunto(
            string empresa, string idBorrador, long adjuntoId, bool inline = false)
        {
            var enc = _bll.ObtenerPorId(empresa, idBorrador);
            if (enc == null) return HttpNotFound("Borrador no encontrado.");
            if (!PuedeImprimir(enc)) return new HttpUnauthorizedResult();

            var adjunto = _bll.ObtenerAdjunto(empresa, idBorrador, adjuntoId);
            if (adjunto == null || !adjunto.EsArchivo ||
                adjunto.Contenido == null || adjunto.Contenido.Length == 0)
                return HttpNotFound("Adjunto no encontrado.");

            string contentType = string.IsNullOrWhiteSpace(adjunto.ContentType)
                ? "application/octet-stream"
                : adjunto.ContentType;
            string nombre = NombreDescargaSeguro(adjunto.Nombre);
            bool mostrarEnLinea = inline &&
                (string.Equals(contentType, "application/pdf",
                               StringComparison.OrdinalIgnoreCase) ||
                 contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase));

            Response.Headers["X-Content-Type-Options"] = "nosniff";
            Response.Cache.SetCacheability(HttpCacheability.Private);
            Response.Cache.SetMaxAge(TimeSpan.Zero);

            if (!mostrarEnLinea)
                return File(adjunto.Contenido, contentType, nombre);

            Response.AddHeader(
                "Content-Disposition",
                "inline; filename=\"" + nombre.Replace("\"", "'") + "\"");
            return File(adjunto.Contenido, contentType);
        }

        private bool PuedeImprimir(BorradorNcEncabezado enc)
        {
            // La impresión también se usa en Autorizaciones. En Seguimiento se
            // respeta el alcance por creador/agente; los permisos operativos
            // conservan el acceso requerido por sus flujos específicos.
            if (TienePermiso(PERMISO_DASHBOARD) && PuedeConsultarDashboard(enc)) return true;
            if (TienePermiso(PERMISO_AUTORIZAR) || TienePermiso(PERMISO_ANULAR)) return true;
            try { return PuedeConsultarSeguimiento(enc); }
            catch (UnauthorizedAccessException) { return false; }
        }

        private static bool TienePermiso(string permiso)
        {
            return BorradorNcPermisoAttribute.OmitirPermisos || CustomHelper.Permiso(permiso);
        }

        private JsonResult JsonGet(Func<object> consulta)
        {
            try
            {
                return Json(new { ok = true, data = consulta() }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, msg = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        private static bool TryFecha(string valor, out DateTime fecha)
        {
            return DateTime.TryParseExact(valor ?? "", "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha);
        }

        private static IEnumerable<BorradorNcEncabezado> SinDuplicados(IEnumerable<BorradorNcEncabezado> filas)
        {
            return filas.GroupBy(x => x.IdEmpresa + "|" + x.IdBorrador,
                                 StringComparer.OrdinalIgnoreCase)
                        .Select(x => x.First())
                        .OrderByDescending(x => x.Fecha)
                        .ThenByDescending(x => x.IdBorrador);
        }

        private static List<BorradorNcListaItemViewModel> ProyectarLista(IEnumerable<BorradorNcEncabezado> filas)
        {
            return filas.Select(ProyectarResumen).ToList();
        }

        private static BorradorNcListaItemViewModel ProyectarResumen(BorradorNcEncabezado x)
        {
            return new BorradorNcListaItemViewModel
            {
                IdBorrador = x.IdBorrador,
                IdEmpresa = x.IdEmpresa,
                Fecha = x.Fecha.ToString("yyyy-MM-dd"),
                IdCliente = x.IdCliente,
                Nombre = x.Nombre,
                Agente = x.Agente,
                Moneda = x.Moneda,
                Total = x.Total,
                Estado = x.Estado,
                IdUsr = x.IdUsr,
                Registro = x.Registro.HasValue ? x.Registro.Value.ToString("yyyy-MM-dd HH:mm") : "",
                ResueltoPor = x.ResueltoPor,
                FechaResolucion = x.FechaResolucion.HasValue
                    ? x.FechaResolucion.Value.ToString("yyyy-MM-dd HH:mm") : "",
                MotivoResolucion = x.MotivoResolucion,
                TieneNcPrevia = x.TieneNcPrevia
            };
        }

        private static BorradorNcDocumentoViewModel ProyectarDocumento(BorradorNcEncabezado x)
        {
            var resumen = ProyectarResumen(x);
            return new BorradorNcDocumentoViewModel
            {
                IdBorrador = resumen.IdBorrador,
                IdEmpresa = resumen.IdEmpresa,
                Fecha = resumen.Fecha,
                IdCliente = resumen.IdCliente,
                Nombre = resumen.Nombre,
                Agente = resumen.Agente,
                Moneda = resumen.Moneda,
                Total = resumen.Total,
                Estado = resumen.Estado,
                IdUsr = resumen.IdUsr,
                Registro = resumen.Registro,
                ResueltoPor = resumen.ResueltoPor,
                FechaResolucion = resumen.FechaResolucion,
                MotivoResolucion = resumen.MotivoResolucion,
                TieneNcPrevia = resumen.TieneNcPrevia,
                Nit = x.Nit,
                Direccion = x.Direccion,
                Correo = x.Correo,
                Depto = x.Depto,
                CodigoOperador = x.CodigoOperador,
                Detalles = (x.Detalles ?? new List<BorradorNcDetalle>()).Select(d =>
                    new BorradorNcDetalleViewModel
                    {
                        Concepto = d.Concepto,
                        Documento = d.Documento,
                        FechaDoc = d.FechaDoc.ToString("yyyy-MM-dd"),
                        SerieFel = d.SerieFel,
                        NumeroFel = d.NumeroFel,
                        TotalFactura = d.TotalFactura,
                        Pagado = d.Pagado,
                        NcPreviaSap = d.NcPreviaSap,
                        Moneda = d.Moneda,
                        Descripcion = d.Descripcion,
                        Importe = d.Importe
                    }).ToList(),
                Adjuntos = (x.Adjuntos ?? new List<BorradorNcAdjunto>()).Select(a =>
                    new BorradorNcAdjuntoViewModel
                    {
                        AdjuntoId = a.AdjuntoId,
                        Tipo = a.Tipo,
                        Nombre = a.Nombre,
                        Extension = a.Extension,
                        ContentType = a.ContentType,
                        Tamano = a.Tamano,
                        Url = a.Url,
                        Orden = a.Orden,
                        IdUsr = a.IdUsr,
                        Registro = a.Registro.HasValue
                            ? a.Registro.Value.ToString("yyyy-MM-dd HH:mm")
                            : "",
                        EsVisualizable = string.Equals(
                            a.ContentType, "application/pdf",
                            StringComparison.OrdinalIgnoreCase) ||
                            (!string.IsNullOrWhiteSpace(a.ContentType) &&
                             a.ContentType.StartsWith(
                                 "image/", StringComparison.OrdinalIgnoreCase))
                    }).ToList()
            };
        }

        private static string NombreDescargaSeguro(string nombre)
        {
            string limpio = Path.GetFileName(nombre ?? "adjunto")
                .Replace("\r", "")
                .Replace("\n", "")
                .Replace("\\", "_")
                .Replace("/", "_");
            return string.IsNullOrWhiteSpace(limpio) ? "adjunto" : limpio;
        }

        private List<BorradorNcFacturaContenidoViewModel> ProyectarContenidoFacturas(
            BorradorNcEncabezado enc)
        {
            var facturas = (enc.Detalles ?? new List<BorradorNcDetalle>())
                .Where(x => !string.IsNullOrWhiteSpace(x.Documento))
                .GroupBy(x => x.Documento, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .ToList();

            var documentos = facturas.Select(x => x.Documento).ToList();
            var renglones = _bll.ObtenerDetallesFacturas(
                enc.IdEmpresa, enc.IdCliente, documentos);
            var productosPorDocumento = renglones
                .GroupBy(x => x.Documento ?? "", StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.OrderBy(y => y.NumeroLinea).ToList(),
                              StringComparer.OrdinalIgnoreCase);

            return facturas.Select(factura =>
            {
                List<FacturaDetalleSap> productos;
                if (!productosPorDocumento.TryGetValue(factura.Documento, out productos))
                    productos = new List<FacturaDetalleSap>();

                return new BorradorNcFacturaContenidoViewModel
                {
                    Documento = factura.Documento,
                    FechaDoc = factura.FechaDoc.ToString("yyyy-MM-dd"),
                    SerieFel = factura.SerieFel,
                    NumeroFel = factura.NumeroFel,
                    Moneda = factura.Moneda,
                    TotalFactura = factura.TotalFactura,
                    Pagado = factura.Pagado,
                    ImporteSolicitado = factura.Importe,
                    Concepto = factura.Concepto,
                    DescripcionSolicitud = factura.Descripcion,
                    Productos = productos.Select(ProyectarProductoFactura).ToList()
                };
            }).ToList();
        }

        private List<BorradorNcDocumentoPrevioResumenViewModel> ProyectarDocumentosPrevios(
            BorradorNcEncabezado enc)
        {
            var facturas = (enc.Detalles ?? new List<BorradorNcDetalle>())
                .Select(x => (x.Documento ?? "").Trim())
                .Where(x => x.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return _bll.ObtenerDocumentosPrevios(enc.IdEmpresa, facturas)
                .Select(x => new BorradorNcDocumentoPrevioResumenViewModel
                {
                    Clase = x.Clase,
                    ClaseTexto = TextoClaseDocumentoPrevio(x.Clase),
                    TiposOrigen = x.TiposOrigen,
                    Factura = x.Factura,
                    Documento = x.Documento,
                    Fecha = x.Fecha.ToString("yyyy-MM-dd"),
                    Moneda = x.Moneda,
                    Total = x.Total,
                    Comentarios = string.IsNullOrWhiteSpace(x.Comentarios)
                        ? x.Origen
                        : x.Comentarios,
                    Cancelado = x.Cancelado
                })
                .ToList();
        }

        private static BorradorNcDocumentoPrevioViewModel ProyectarDetalleDocumentoPrevio(
            BorradorNcEncabezado enc, DocumentoPrevioSap documento,
            bool desdeAutorizaciones)
        {
            return new BorradorNcDocumentoPrevioViewModel
            {
                Empresa = enc.IdEmpresa,
                IdBorrador = enc.IdBorrador,
                Factura = documento.Factura,
                Clase = documento.Clase,
                ClaseTexto = TextoClaseDocumentoPrevio(documento.Clase),
                TiposOrigen = documento.TiposOrigen,
                Documento = documento.Documento,
                EstadoSap = TextoEstadoDocumentoPrevio(documento),
                Cancelado = documento.Cancelado,
                TipoDocumento = documento.TipoDocumento,
                Fecha = documento.Fecha.ToString("yyyy-MM-dd"),
                FechaDocumento = documento.FechaDocumento.ToString("yyyy-MM-dd"),
                ClienteId = documento.CardCode,
                ClienteNombre = documento.CardName,
                Referencia = documento.Referencia,
                Moneda = documento.Moneda,
                TipoCambio = documento.TipoCambio,
                Total = documento.Total,
                Origen = documento.Origen,
                Comentarios = documento.Comentarios,
                SerieFel = documento.SerieFel,
                NumeroFel = documento.NumeroFel,
                PdfDisponible = UrlWebValida(documento.UrlPdf),
                DesdeAutorizaciones = desdeAutorizaciones,
                Productos = (documento.Lineas ?? new List<DocumentoPrevioDetalleSap>())
                    .OrderBy(x => x.NumeroLinea)
                    .Select(ProyectarProductoDocumentoPrevio)
                    .ToList()
            };
        }

        private static BorradorNcProductoFacturaViewModel ProyectarProductoDocumentoPrevio(
            DocumentoPrevioDetalleSap producto)
        {
            return new BorradorNcProductoFacturaViewModel
            {
                NumeroLinea = producto.NumeroLinea,
                Sku = producto.CodigoArticulo,
                EsServicio = string.IsNullOrWhiteSpace(producto.CodigoArticulo),
                Descripcion = producto.Descripcion,
                Cantidad = producto.Cantidad,
                UnidadMedida = producto.UnidadMedida,
                PrecioUnitario = producto.PrecioUnitario,
                DescuentoPorcentaje = producto.DescuentoPorcentaje,
                Subtotal = producto.Subtotal,
                CodigoImpuesto = producto.CodigoImpuesto,
                ImpuestoPorcentaje = producto.ImpuestoPorcentaje,
                Impuesto = producto.Impuesto,
                Total = producto.Total,
                Moneda = producto.Moneda,
                Bodega = producto.Bodega
            };
        }

        private static string TextoClaseDocumentoPrevio(string clase)
        {
            return string.Equals(clase, "DEVOLUCION",
                                 StringComparison.OrdinalIgnoreCase)
                ? "Devolución"
                : "Nota de crédito";
        }

        private static string TextoEstadoDocumentoPrevio(DocumentoPrevioSap documento)
        {
            if (documento.Cancelado) return "Cancelado";
            if (string.Equals(documento.Estado, "C", StringComparison.OrdinalIgnoreCase))
                return "Cerrado";
            if (string.Equals(documento.Estado, "O", StringComparison.OrdinalIgnoreCase))
                return "Abierto";
            return string.IsNullOrWhiteSpace(documento.Estado)
                ? "Sin estado"
                : documento.Estado;
        }

        private static bool UrlWebValida(string valor)
        {
            Uri uri;
            return Uri.TryCreate((valor ?? "").Trim(), UriKind.Absolute, out uri) &&
                   (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        private static BorradorNcProductoFacturaViewModel ProyectarProductoFactura(
            FacturaDetalleSap producto)
        {
            return new BorradorNcProductoFacturaViewModel
            {
                NumeroLinea = producto.NumeroLinea,
                Sku = producto.CodigoArticulo,
                EsServicio = string.IsNullOrWhiteSpace(producto.CodigoArticulo),
                Descripcion = producto.Descripcion,
                Cantidad = producto.Cantidad,
                UnidadMedida = producto.UnidadMedida,
                PrecioUnitario = producto.PrecioUnitario,
                DescuentoPorcentaje = producto.DescuentoPorcentaje,
                Subtotal = producto.Subtotal,
                CodigoImpuesto = producto.CodigoImpuesto,
                ImpuestoPorcentaje = producto.ImpuestoPorcentaje,
                Impuesto = producto.Impuesto,
                Total = producto.Total,
                Moneda = producto.Moneda,
                Bodega = producto.Bodega
            };
        }

        private BorradorNcDashboardAlcance CrearAlcanceDashboard()
        {
            var alcance = new BorradorNcDashboardAlcance
            {
                Global = TienePermiso(PERMISO_VER_TODOS),
                Usuario = User.Identity.Name
            };
            foreach (var grupo in Asignaciones().Where(x => !string.IsNullOrWhiteSpace(x.Codigo))
                .GroupBy(x => _usuarioEmpresa.GetEmpresaNombre(x.EmpresaId),
                         StringComparer.OrdinalIgnoreCase))
            {
                if (string.Equals(grupo.Key, "DESCONOCIDA", StringComparison.OrdinalIgnoreCase)) continue;
                alcance.AgentesPorEmpresa[grupo.Key] = grupo
                    .Select(x => _usuarioEmpresa.ParseCodigo(x.Codigo).AgenteNombre)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            return alcance;
        }

        private bool PuedeConsultarDashboard(BorradorNcEncabezado enc)
        {
            if (enc == null || !TienePermiso(PERMISO_DASHBOARD)) return false;
            if (TienePermiso(PERMISO_VER_TODOS)) return true;
            if (string.Equals(enc.IdUsr, User.Identity.Name, StringComparison.OrdinalIgnoreCase)) return true;
            var alcance = CrearAlcanceDashboard();
            List<string> agentes;
            return alcance.AgentesPorEmpresa.TryGetValue(enc.IdEmpresa ?? "", out agentes) &&
                   agentes.Any(x => string.Equals(x, enc.Agente, StringComparison.OrdinalIgnoreCase));
        }

        private static void FormatearHojaExcel(ExcelWorksheet hoja, int columnas, int filas)
        {
            using (var rango = hoja.Cells[1, 1, Math.Max(1, filas), columnas])
            {
                rango.Style.Font.Name = "Calibri";
                rango.Style.Font.Size = 10;
                rango.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            }
            using (var encabezado = hoja.Cells[1, 1, 1, columnas])
            {
                encabezado.Style.Font.Bold = true;
                encabezado.Style.Font.Color.SetColor(System.Drawing.Color.White);
                encabezado.Style.Fill.PatternType = ExcelFillStyle.Solid;
                encabezado.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(37, 99, 235));
                encabezado.AutoFilter = true;
            }
            hoja.View.FreezePanes(2, 1);
            hoja.Cells[1, 1, Math.Max(1, filas), columnas].AutoFitColumns(10, 42);
        }

        private class ContextoConsulta
        {
            public string Empresa { get; set; }
            public List<string> Agentes { get; set; }
        }
    }
}
