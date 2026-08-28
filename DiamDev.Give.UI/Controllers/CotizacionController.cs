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
    public class CotizacionController : Controller
    {
        private const string PermisoVer = "Control.Cotizaciones.Ver";
        private const string PermisoCrear = "Control.Cotizaciones.Crear";
        private const string PermisoAnular = "Control.Cotizaciones.Anular";
        private const string PermisoVerTodos = "Control.Cotizaciones.VerTodos";

        private readonly CotizacionBLL _bll = new CotizacionBLL();
        private readonly UsuarioEmpresaBL _usuarioEmpresa = new UsuarioEmpresaBL();

        [CotizacionPermiso(PermisoVer)]
        public ActionResult Index()
        {
            CustomHelper.setTitle("Cotizaciones", "Creación y seguimiento");
            return View(CrearModeloInicial());
        }

        [HttpGet]
        [CotizacionPermiso(PermisoVer)]
        public JsonResult BuscarClientes(string empresa, string codigoOperador, string filtro)
        {
            return JsonGet(delegate
            {
                string agente = ResolverAgente(empresa, codigoOperador);
                return _bll.BuscarClientes(empresa, agente, filtro ?? "");
            });
        }

        [HttpGet]
        [CotizacionPermiso(PermisoVer)]
        public JsonResult BuscarProductos(
            string empresa, string codigoOperador, string clienteId,
            string filtro, int pagina = 1, int tamano = 100)
        {
            return JsonGet(delegate
            {
                if (string.IsNullOrWhiteSpace(clienteId))
                    throw new InvalidOperationException(
                        "Seleccione un cliente antes de buscar productos.");
                string agente = ResolverAgente(empresa, codigoOperador);
                return _bll.BuscarProductos(
                    empresa, agente, clienteId, filtro ?? "", pagina, tamano);
            });
        }

        [HttpGet]
        [CotizacionPermiso(PermisoVer)]
        public JsonResult ObtenerPrecio(
            string empresa, string codigoOperador, string clienteId,
            string itemCode, decimal cantidad)
        {
            return JsonGet(delegate
            {
                string agente = ResolverAgente(empresa, codigoOperador);
                var producto = _bll.ObtenerPrecioProducto(
                    empresa, agente, clienteId, itemCode, cantidad);
                if (producto == null)
                    throw new InvalidOperationException(
                        "El producto ya no está disponible para venta en SAP.");
                return producto;
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CotizacionPermiso(PermisoCrear)]
        public JsonResult Guardar(GuardarCotizacionRequest request)
        {
            try
            {
                if (request == null)
                    return Json(new { ok = false, msg = "No se recibió la cotización." });

                var asignacion = ValidarOperador(
                    request.IdEmpresa, request.CodigoOperador);
                DateTime fecha;
                DateTime validaHasta;
                if (!TryFecha(request.Fecha, out fecha))
                    return Json(new { ok = false, msg = "La fecha de emisión no es válida." });
                if (!TryFecha(request.ValidaHasta, out validaHasta))
                    return Json(new { ok = false, msg = "La fecha de validez no es válida." });

                string agente = _usuarioEmpresa.ParseCodigo(
                    asignacion.Codigo).AgenteNombre;
                var enc = new CotizacionEncabezado
                {
                    IdEmpresa = (request.IdEmpresa ?? "").Trim(),
                    Fecha = fecha,
                    ValidaHasta = validaHasta,
                    IdCliente = request.IdCliente,
                    CodigoOperador = asignacion.Codigo,
                    Agente = agente,
                    Moneda = request.Moneda,
                    CondicionesPago = request.CondicionesPago,
                    TiempoEntrega = request.TiempoEntrega,
                    Observaciones = request.Observaciones,
                    Detalles = (request.Detalles ?? new List<CotizacionDetalleRequest>())
                        .Select(x => new CotizacionDetalle
                        {
                            ItemCode = x.ItemCode,
                            Descripcion = x.Descripcion,
                            Cantidad = x.Cantidad,
                            PrecioUnitario = x.PrecioUnitario,
                            DescuentoPorcentaje = x.DescuentoPorcentaje,
                            ImpuestoPorcentaje = x.ImpuestoPorcentaje
                        }).ToList()
                };

                var resultado = _bll.Guardar(enc, User.Identity.Name);
                return Json(new
                {
                    ok = resultado.Exito,
                    msg = resultado.Mensaje,
                    idCotizacion = resultado.IdCotizacion
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { ok = false, msg = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Json(new { ok = false, msg = ex.Message });
            }
            catch (Exception)
            {
                return Json(new { ok = false, msg = "No fue posible crear la cotización. Contacte a Sistemas." });
            }
        }

        [HttpGet]
        [CotizacionPermiso(PermisoVer)]
        public JsonResult Listar(
            string empresa = null, string estado = null, string desde = null,
            string hasta = null, string filtro = null)
        {
            return JsonGet(delegate
            {
                DateTime f;
                DateTime? fechaDesde = TryFecha(desde, out f) ? (DateTime?)f : null;
                DateTime? fechaHasta = TryFecha(hasta, out f) ? (DateTime?)f : null;
                bool verTodos = TienePermiso(PermisoVerTodos);
                string usuario = verTodos ? null : User.Identity.Name;
                var filas = new List<CotizacionEncabezado>();

                foreach (string nombre in EmpresasConsulta(empresa))
                {
                    filas.AddRange(_bll.Listar(
                        nombre, estado, usuario, null,
                        fechaDesde, fechaHasta, filtro));
                }

                return filas.GroupBy(x => x.IdEmpresa + "|" + x.IdCotizacion,
                                     StringComparer.OrdinalIgnoreCase)
                            .Select(x => x.First())
                            .OrderByDescending(x => x.Fecha)
                            .ThenByDescending(x => x.IdCotizacion)
                            .Select(ProyectarResumen)
                            .ToList();
            });
        }

        [HttpGet]
        [CotizacionPermiso(PermisoVer)]
        public JsonResult ObtenerDetalle(string empresa, string idCotizacion)
        {
            return JsonGet(delegate
            {
                ValidarEmpresa(empresa);
                var enc = _bll.ObtenerPorId(empresa, idCotizacion);
                if (enc == null) throw new InvalidOperationException("Cotización no encontrada.");
                if (!PuedeConsultar(enc))
                    throw new UnauthorizedAccessException("No tiene acceso a esta cotización.");
                return ProyectarDocumento(enc);
            });
        }

        [CotizacionPermiso(PermisoVer)]
        public ActionResult Imprimir(string empresa, string idCotizacion)
        {
            ValidarEmpresa(empresa);
            var enc = _bll.ObtenerPorId(empresa, idCotizacion);
            if (enc == null) return HttpNotFound("Cotización no encontrada.");
            if (!PuedeConsultar(enc)) return new HttpUnauthorizedResult();
            ViewBag.TotalEnLetras = CotizacionBLL.TotalEnLetras(
                enc.Total, enc.Moneda);
            return View(enc);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CotizacionPermiso(PermisoAnular)]
        public JsonResult Anular(AnularCotizacionRequest request)
        {
            try
            {
                if (request == null)
                    return Json(new { ok = false, msg = "No se recibió la anulación." });
                ValidarEmpresa(request.Empresa);
                var enc = _bll.ObtenerPorId(request.Empresa, request.IdCotizacion);
                if (enc == null) return Json(new { ok = false, msg = "Cotización no encontrada." });
                if (!PuedeConsultar(enc))
                    return Json(new { ok = false, msg = "No tiene acceso a esta cotización." });

                var resultado = _bll.Anular(
                    request.Empresa, request.IdCotizacion,
                    User.Identity.Name, request.Motivo);
                return Json(new { ok = resultado.Exito, msg = resultado.Mensaje });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { ok = false, msg = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Json(new { ok = false, msg = ex.Message });
            }
            catch (Exception)
            {
                return Json(new { ok = false, msg = "No fue posible anular la cotización. Contacte a Sistemas." });
            }
        }

        private CotizacionIndexViewModel CrearModeloInicial()
        {
            var modelo = new CotizacionIndexViewModel
            {
                UsuarioActual = User.Identity.Name,
                PuedeVerTodos = TienePermiso(PermisoVerTodos),
                PuedeAnular = TienePermiso(PermisoAnular)
            };

            modelo.Empresas = Asignaciones()
                .Where(x => !string.IsNullOrWhiteSpace(x.Codigo))
                .GroupBy(x => x.EmpresaId)
                .Select(g => new CotizacionEmpresaViewModel
                {
                    EmpresaId = g.Key,
                    Nombre = _usuarioEmpresa.GetEmpresaNombre(g.Key),
                    Operadores = g.GroupBy(x => x.Codigo.Trim(),
                                           StringComparer.OrdinalIgnoreCase)
                        .Select(x => x.First())
                        .Select(x =>
                        {
                            var codigo = _usuarioEmpresa.ParseCodigo(x.Codigo);
                            return new CotizacionOperadorViewModel
                            {
                                Codigo = x.Codigo.Trim(),
                                Agente = codigo.AgenteNombre
                            };
                        })
                        .Where(x => !string.IsNullOrWhiteSpace(x.Agente))
                        .OrderBy(x => OrdenOperador(x.Codigo))
                        .ThenBy(x => x.Codigo)
                        .ToList()
                })
                .Where(x => x.Nombre != "DESCONOCIDA" && x.Operadores.Count > 0)
                .OrderBy(x => x.Nombre)
                .ToList();

            return modelo;
        }

        private List<UsuarioEmpresa> Asignaciones()
        {
            return _usuarioEmpresa.ObtenerPorUsuarioId(CustomHelper.getUserId());
        }

        private UsuarioEmpresa ValidarEmpresa(string empresa)
        {
            string valor = (empresa ?? "").Trim();
            var asignacion = Asignaciones().FirstOrDefault(x =>
                string.Equals(_usuarioEmpresa.GetEmpresaNombre(x.EmpresaId), valor,
                              StringComparison.OrdinalIgnoreCase));
            if (asignacion == null)
                throw new UnauthorizedAccessException(
                    "La empresa no está asignada al usuario actual.");
            return asignacion;
        }

        private UsuarioEmpresa ValidarOperador(string empresa, string codigoOperador)
        {
            string emp = (empresa ?? "").Trim();
            string codigo = (codigoOperador ?? "").Trim();
            if (codigo.Length == 0)
                throw new InvalidOperationException("Seleccione el agente con el que operará.");

            var asignacion = Asignaciones().FirstOrDefault(x =>
                string.Equals(_usuarioEmpresa.GetEmpresaNombre(x.EmpresaId), emp,
                              StringComparison.OrdinalIgnoreCase) &&
                string.Equals((x.Codigo ?? "").Trim(), codigo,
                              StringComparison.OrdinalIgnoreCase));
            if (asignacion == null)
                throw new UnauthorizedAccessException(
                    "El agente seleccionado no está asignado al usuario para esta empresa.");
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

        private IEnumerable<string> EmpresasConsulta(string empresa)
        {
            if (!string.IsNullOrWhiteSpace(empresa))
            {
                ValidarEmpresa(empresa);
                return new[] { empresa.Trim() };
            }
            return Asignaciones().Select(x => _usuarioEmpresa.GetEmpresaNombre(x.EmpresaId))
                .Where(x => x != "DESCONOCIDA")
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private bool PuedeConsultar(CotizacionEncabezado enc)
        {
            return TienePermiso(PermisoVerTodos) ||
                   string.Equals(enc.IdUsr, User.Identity.Name,
                                 StringComparison.OrdinalIgnoreCase);
        }

        private static bool TienePermiso(string permiso)
        {
            return CotizacionPermisoAttribute.OmitirPermisos || CustomHelper.Permiso(permiso);
        }

        private JsonResult JsonGet(Func<object> consulta)
        {
            try
            {
                return Json(new { ok = true, data = consulta() },
                            JsonRequestBehavior.AllowGet);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { ok = false, msg = ex.Message },
                            JsonRequestBehavior.AllowGet);
            }
            catch (InvalidOperationException ex)
            {
                return Json(new { ok = false, msg = ex.Message },
                            JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new
                {
                    ok = false,
                    msg = "No fue posible completar la consulta. Contacte a Sistemas."
                }, JsonRequestBehavior.AllowGet);
            }
        }

        private static bool TryFecha(string valor, out DateTime fecha)
        {
            return DateTime.TryParseExact(valor ?? "", "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha);
        }

        private static int OrdenOperador(string codigo)
        {
            string valor = (codigo ?? "").Trim();
            int separador = valor.IndexOf('-');
            int numero;
            return separador > 0 && int.TryParse(
                valor.Substring(0, separador), out numero) ? numero : int.MaxValue;
        }

        private static CotizacionListaItemViewModel ProyectarResumen(
            CotizacionEncabezado x)
        {
            return new CotizacionListaItemViewModel
            {
                IdCotizacion = x.IdCotizacion,
                IdEmpresa = x.IdEmpresa,
                Fecha = x.Fecha.ToString("yyyy-MM-dd"),
                ValidaHasta = x.ValidaHasta.ToString("yyyy-MM-dd"),
                IdCliente = x.IdCliente,
                NombreCliente = x.NombreCliente,
                Agente = x.Agente,
                Moneda = x.Moneda,
                Subtotal = x.Subtotal,
                ImpuestoTotal = x.ImpuestoTotal,
                Total = x.Total,
                Estado = x.Estado,
                IdUsr = x.IdUsr,
                Registro = x.Registro.HasValue
                    ? x.Registro.Value.ToString("yyyy-MM-dd HH:mm") : ""
            };
        }

        private static CotizacionDocumentoViewModel ProyectarDocumento(
            CotizacionEncabezado x)
        {
            var r = ProyectarResumen(x);
            return new CotizacionDocumentoViewModel
            {
                IdCotizacion = r.IdCotizacion,
                IdEmpresa = r.IdEmpresa,
                Fecha = r.Fecha,
                ValidaHasta = r.ValidaHasta,
                IdCliente = r.IdCliente,
                NombreCliente = r.NombreCliente,
                Agente = r.Agente,
                Moneda = r.Moneda,
                Subtotal = r.Subtotal,
                ImpuestoTotal = r.ImpuestoTotal,
                Total = r.Total,
                Estado = r.Estado,
                IdUsr = r.IdUsr,
                Registro = r.Registro,
                Nit = x.Nit,
                Direccion = x.Direccion,
                Correo = x.Correo,
                CodigoOperador = x.CodigoOperador,
                CondicionesPago = x.CondicionesPago,
                TiempoEntrega = x.TiempoEntrega,
                Observaciones = x.Observaciones,
                ImporteBruto = x.ImporteBruto,
                DescuentoTotal = x.DescuentoTotal,
                AnuladoPor = x.AnuladoPor,
                FechaAnulacion = x.FechaAnulacion.HasValue
                    ? x.FechaAnulacion.Value.ToString("yyyy-MM-dd HH:mm") : "",
                MotivoAnulacion = x.MotivoAnulacion,
                Detalles = (x.Detalles ?? new List<CotizacionDetalle>()).Select(d =>
                    new CotizacionDetalleViewModel
                    {
                        Linea = d.Linea,
                        ItemCode = d.ItemCode,
                        ItemName = d.ItemName,
                        Descripcion = d.Descripcion,
                        Grupo = d.Grupo,
                        Unidad = d.Unidad,
                        Existencia = d.Existencia,
                        Disponible = d.Disponible,
                        Cantidad = d.Cantidad,
                        PrecioLista = d.PrecioLista,
                        PrecioUnitario = d.PrecioUnitario,
                        DescuentoPorcentaje = d.DescuentoPorcentaje,
                        GrupoImpuesto = d.GrupoImpuesto,
                        ImpuestoPorcentaje = d.ImpuestoPorcentaje,
                        Subtotal = d.Subtotal,
                        ImpuestoMonto = d.ImpuestoMonto,
                        Total = d.Total
                    }).ToList()
            };
        }
    }
}
