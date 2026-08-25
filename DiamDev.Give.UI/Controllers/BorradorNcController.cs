using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
        public JsonResult Guardar(GuardarBorradorNcRequest request)
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
                    Detalles = detalles
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
                bool puedeVerTodos = TienePermiso(PERMISO_VER_TODOS);
                foreach (var contexto in ContextosConsulta(empresa))
                {
                    filas.AddRange(_bll.ListarPendientes(
                        User.Identity.Name,
                        puedeVerTodos,
                        contexto.Agente,
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
                bool puedeVerTodos = TienePermiso(PERMISO_VER_TODOS);
                foreach (var contexto in ContextosConsulta(empresa))
                {
                    filas.AddRange(_bll.ListarSeguimiento(
                        User.Identity.Name,
                        puedeVerTodos,
                        contexto.Agente,
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
                if (!PuedeConsultar(enc))
                    throw new UnauthorizedAccessException("No tiene acceso a este borrador.");
                return ProyectarDocumento(enc);
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
            if (!PuedeConsultar(enc)) return new HttpUnauthorizedResult();
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
            bool esAgente = EsAgente();
            var asignaciones = string.IsNullOrWhiteSpace(empresa)
                ? Asignaciones()
                : Asignaciones().Where(x =>
                    string.Equals(_usuarioEmpresa.GetEmpresaNombre(x.EmpresaId), empresa.Trim(),
                                  StringComparison.OrdinalIgnoreCase)).ToList();

            if (!string.IsNullOrWhiteSpace(empresa) && asignaciones.Count == 0)
                throw new UnauthorizedAccessException(
                    "La empresa no está asignada al usuario actual.");

            return asignaciones.Select(x => new ContextoConsulta
                {
                    Empresa = _usuarioEmpresa.GetEmpresaNombre(x.EmpresaId),
                    Agente = esAgente ? _usuarioEmpresa.ParseCodigo(x.Codigo).AgenteNombre : null
                })
                .Where(x => x.Empresa != "DESCONOCIDA")
                .GroupBy(x => x.Empresa + "|" + (x.Agente ?? ""), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First());
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

        private bool PuedeConsultar(BorradorNcEncabezado enc)
        {
            // Quien autoriza o anula necesita abrir e imprimir solicitudes de
            // otros capturadores dentro de sus empresas asignadas, aun cuando el
            // rol no tenga VerTodos por una configuración incompleta.
            if (TienePermiso(PERMISO_VER_TODOS) ||
                TienePermiso(PERMISO_AUTORIZAR) ||
                TienePermiso(PERMISO_ANULAR)) return true;
            if (EsAgente())
            {
                var agentes = Asignaciones()
                    .Where(x => string.Equals(_usuarioEmpresa.GetEmpresaNombre(x.EmpresaId), enc.IdEmpresa,
                                              StringComparison.OrdinalIgnoreCase))
                    .Select(x => _usuarioEmpresa.ParseCodigo(x.Codigo).AgenteNombre);
                return agentes.Any(x => string.Equals(x, enc.Agente, StringComparison.OrdinalIgnoreCase));
            }
            return string.Equals(enc.IdUsr, User.Identity.Name, StringComparison.OrdinalIgnoreCase);
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
                    }).ToList()
            };
        }

        private class ContextoConsulta
        {
            public string Empresa { get; set; }
            public string Agente { get; set; }
        }
    }
}
