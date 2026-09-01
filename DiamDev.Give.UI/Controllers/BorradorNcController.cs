using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
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

        private readonly BorradorNcBLL _bll = new BorradorNcBLL();
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
                    if (enlace == null ||
                        (string.IsNullOrWhiteSpace(enlace.Titulo) &&
                         string.IsNullOrWhiteSpace(enlace.Url))) continue;

                    adjuntos.Add(new BorradorNcAdjunto
                    {
                        Tipo = TiposAdjuntoBorradorNc.Enlace,
                        Nombre = enlace.Titulo,
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

        [BorradorNcPermiso(PERMISO_VER)]
        public ActionResult Imprimir(string empresa, string idBorrador)
        {
            ValidarEmpresa(empresa);
            var enc = _bll.ObtenerPorId(empresa, idBorrador);
            if (enc == null) return HttpNotFound("Borrador no encontrado.");
            if (!PuedeImprimir(enc)) return new HttpUnauthorizedResult();
            return View(enc);
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
            ValidarEmpresa(empresa);
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
            return PuedeConsultarSeguimiento(enc) ||
                   TienePermiso(PERMISO_AUTORIZAR) ||
                   TienePermiso(PERMISO_ANULAR);
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
                    Productos = productos.Select(producto =>
                        new BorradorNcProductoFacturaViewModel
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
                        }).ToList()
                };
            }).ToList();
        }

        private class ContextoConsulta
        {
            public string Empresa { get; set; }
            public List<string> Agentes { get; set; }
        }
    }
}
