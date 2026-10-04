using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DiamDev.Give.BLL;
using DiamDev.Give.Entities;
using DiamDev.Give.UI.App_Start;
using DiamDev.Give.UI.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace DiamDev.Give.UI.Controllers
{
    [Authorize]
    [HandleError]
    public class ClientesCrmController : Controller
    {
        private const string PermisoVer = "Control.Clientes.Ver";
        private const string PermisoCrear = "Control.Clientes.Crear";
        private const string PermisoDashboard = "Control.Clientes.Dashboard";
        private const string PermisoAdministrar = "Control.Clientes.Administrar";
        private const string PermisoCarteraGlobal = "Control.Clientes.CarteraGlobal";
        private readonly ClienteCrmBLL _crm = new ClienteCrmBLL();
        private readonly UsuarioEmpresaBL _usuarioEmpresa = new UsuarioEmpresaBL();

        [Permiso(PermisoVer)]
        public ActionResult Index(string empresa, string estado, string filtro)
        {
            if (string.IsNullOrWhiteSpace(User.Identity.Name)) return SinAcceso();
            CustomHelper.setTitle("Clientes", "Mis solicitudes");
            if (!EmpresaValidaOBlanca(empresa)) return SinAcceso();
            ViewBag.Empresas = TodasEmpresas();
            ViewBag.Empresa = empresa;
            ViewBag.Estado = estado;
            ViewBag.Filtro = filtro;
            ViewBag.PuedeCrear = TienePermiso(PermisoCrear);
            var solicitudes = _crm.ListarSolicitudes(empresa, estado, filtro, User.Identity.Name);
            _crm.RegistrarEvento("MODULO", null, empresa, User.Identity.Name, "LISTAR_PROPIAS",
                string.Format("Estado={0}; Filtro={1}", estado, filtro), Ip());
            return View(solicitudes);
        }

        [Permiso(PermisoCrear)]
        public ActionResult Nueva()
        {
            _crm.RegistrarEvento("MODULO", null, null, User.Identity.Name, "ABRIR_SOLICITUD_NUEVA", null, Ip());
            CustomHelper.setTitle("Clientes", "Nueva solicitud");
            return View("Editor", PrepararEditor(new ClienteCrmEditorViewModel { PasoActual = 1 }));
        }

        [HttpGet]
        [Permiso(PermisoCrear)]
        public JsonResult BuscarClientesSap(string empresa, string codigoOperador, string filtro)
        {
            try
            {
                string agente = ValidarOperador(empresa, codigoOperador);
                var clientes = _crm.BuscarClientesSap(empresa, agente, filtro);
                _crm.RegistrarEvento("MODULO", null, empresa, User.Identity.Name,
                    "BUSCAR_CLIENTE_SAP", "Agente=" + codigoOperador, Ip());
                return Json(new { ok = true, clientes = clientes.Select(c => new {
                    codigo = c.CardCode, nombre = c.CardName, nit = c.LicTradNum,
                    direccion = c.Address, correo = c.Email, moneda = c.Currency
                }) }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, mensaje =
                    ex is InvalidOperationException || ex is UnauthorizedAccessException
                        ? ex.Message : "No fue posible consultar clientes en SAP. Contacte a Sistemas." },
                    JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult TiposNegocioSap(string empresa)
        {
            bool puedeAdministrar = TienePermiso(PermisoAdministrar);
            bool empresaAsignada = TienePermiso(PermisoCrear) &&
                EmpresasUsuario().Any(x => x.Empresa == empresa);
            if (!puedeAdministrar && !empresaAsignada)
            {
                Response.StatusCode = 403;
                return Json(new { ok = false, mensaje = "No tiene acceso a esta empresa." },
                    JsonRequestBehavior.AllowGet);
            }
            try
            {
                var grupos = _crm.TiposNegocioSap(empresa);
                _crm.RegistrarEvento("MODULO", null, empresa, User.Identity.Name,
                    "LISTAR_TIPOS_NEGOCIO_SAP", null, Ip());
                return Json(new { ok = true, grupos = grupos.Select(x => new {
                    codigo = x.GroupCode, nombre = x.GroupName
                }) }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, mensaje = ex is InvalidOperationException
                    ? ex.Message : "No fue posible cargar los tipos de negocio de SAP." },
                    JsonRequestBehavior.AllowGet);
            }
        }

        [Permiso(PermisoCrear)]
        public ActionResult EditarSolicitud(long id, int? paso)
        {
            var s = _crm.ObtenerSolicitud(id);
            if (s == null) return HttpNotFound();
            if (!PuedeEditarSolicitud(s))
                return SinAcceso();
            _crm.RegistrarEvento("SOLICITUD", id, s.Empresa, User.Identity.Name, "ABRIR_EDICION", null, Ip());
            CustomHelper.setTitle("Clientes", "Editar solicitud");
            return View("Editor", PrepararEditor(new ClienteCrmEditorViewModel {
                SolicitudId = s.Id, Version = s.Version, Empresa = s.Empresa,
                CodigoOperador = s.CodigoOperador, Estado = s.Estado,
                Ficha = s.Ficha, Archivos = s.Archivos,
                PasoActual = Math.Max(1, Math.Min(paso ?? (s.Ficha.PasoCompletado + 1),
                    Math.Min(6, s.Ficha.PasoCompletado + 1)))
            }));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso(PermisoCrear)]
        public ActionResult GuardarSolicitud(ClienteCrmEditorViewModel modelo, string accion)
        {
            bool enviar = string.Equals(accion, "ENVIAR", StringComparison.OrdinalIgnoreCase);
            bool continuar = string.Equals(accion, "CONTINUAR", StringComparison.OrdinalIgnoreCase);
            try
            {
                ClienteCrmSolicitud existente = null;
                if (modelo.SolicitudId > 0)
                {
                    existente = _crm.ObtenerSolicitud(modelo.SolicitudId);
                    if (existente == null) return HttpNotFound();
                    if (!PuedeEditarSolicitud(existente)) return SinAcceso();
                    if (!string.Equals(existente.Empresa, modelo.Empresa,
                        StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("No se puede cambiar la empresa de una solicitud existente.");
                }
                if (modelo.Ficha == null) throw new InvalidOperationException("Complete la ficha de cliente.");
                int completado = existente == null ? 0 : existente.Ficha.PasoCompletado;
                if (modelo.PasoActual < 1 || modelo.PasoActual > 6 ||
                    modelo.PasoActual > Math.Min(6, completado + 1))
                    throw new InvalidOperationException("Complete los pasos anteriores antes de continuar.");
                if (enviar && (modelo.PasoActual != 6 || completado < 5))
                    throw new InvalidOperationException("Complete los seis pasos antes de enviar.");
                if (continuar && modelo.PasoActual == 6)
                    throw new InvalidOperationException("Envíe la solicitud desde el último paso.");
                if (continuar || enviar)
                    ClienteCrmBLL.ValidarPasos(modelo.Ficha, Math.Min(modelo.PasoActual, 5));
                modelo.Ficha.PasoCompletado = continuar
                    ? Math.Max(completado, modelo.PasoActual) : completado;
                if (existente != null) ConservarDatosLegados(existente.Ficha, modelo.Ficha);
                var agente = ValidarOperador(modelo.Empresa, modelo.CodigoOperador);
                string codigoSapOrigen = modelo.Ficha.CodigoSapOrigen;
                string codigoAnterior = existente == null ? null : existente.Ficha.CodigoSapOrigen;
                if (!string.IsNullOrWhiteSpace(codigoSapOrigen) &&
                    !string.Equals(codigoSapOrigen, codigoAnterior, StringComparison.OrdinalIgnoreCase))
                {
                    var clienteSap = _crm.BuscarClientesSap(modelo.Empresa, agente, codigoSapOrigen)
                        .FirstOrDefault(c => string.Equals(c.CardCode, codigoSapOrigen,
                            StringComparison.OrdinalIgnoreCase));
                    if (clienteSap == null)
                        throw new InvalidOperationException("El cliente SAP seleccionado ya no está disponible para este agente.");
                }
                long id = _crm.GuardarSolicitud(new ClienteCrmSolicitud {
                    Id = modelo.SolicitudId, Version = modelo.Version, Empresa = modelo.Empresa,
                    CodigoOperador = modelo.CodigoOperador, Agente = agente,
                    Ficha = modelo.Ficha
                }, LeerArchivos(), enviar, User.Identity.Name, Ip());
                if (continuar)
                    return RedirectToAction("EditarSolicitud", new { id = id, paso = modelo.PasoActual + 1 });
                TempData["CrmExito"] = enviar ? "Solicitud enviada a Créditos." : "Borrador guardado.";
                return RedirectToAction("Solicitud", new { id = id });
            }
            catch (Exception ex)
            {
                modelo.Error = Mensaje(ex);
                if (modelo.SolicitudId > 0)
                    modelo.Archivos = _crm.Archivos(modelo.SolicitudId, true, modelo.Empresa);
                return View("Editor", PrepararEditor(modelo));
            }
        }

        public ActionResult Solicitud(long id)
        {
            var s = _crm.ObtenerSolicitud(id);
            if (s == null) return HttpNotFound();
            bool dashboard = TienePermiso(PermisoDashboard);
            bool propia = TienePermiso(PermisoVer) && EsPropietario(s);
            if ((!dashboard && !propia) ||
                (s.Estado == EstadosSolicitudCliente.Borrador && !propia))
                return SinAcceso();
            _crm.RegistrarEvento("SOLICITUD", id, s.Empresa, User.Identity.Name, "VER", null, Ip());
            CustomHelper.setTitle("Clientes", "Detalle de solicitud");
            return View("Detalle", new ClienteCrmDetalleViewModel {
                Solicitud = s, Archivos = s.Archivos,
                Eventos = _crm.Eventos("SOLICITUD", id),
                EsVistaCreditos = dashboard,
                PuedeAbrirClienteVinculado = s.ClienteId.HasValue &&
                    PuedeAbrirCliente(s.ClienteId.Value, s.Empresa),
                PuedeResolver = dashboard && s.Estado == EstadosSolicitudCliente.Enviada,
                PuedeEditar = propia && PuedeEditarSolicitud(s)
            });
        }

        [Permiso(PermisoDashboard)]
        public ActionResult Dashboard(string empresa, string estado, string filtro)
        {
            if (!EmpresaValidaOBlanca(empresa)) return SinAcceso();
            CustomHelper.setTitle("Clientes", "Dashboard");
            var todas = _crm.ListarSolicitudes(empresa, null, filtro, null)
                .Where(x => x.Estado != EstadosSolicitudCliente.Borrador).ToList();
            var clientes = _crm.ListarClientes(empresa, filtro, false);
            var modelo = new ClienteCrmDashboardViewModel {
                Empresa = empresa, Estado = estado, Filtro = filtro,
                PuedeAdministrar = TienePermiso(PermisoAdministrar),
                PuedeVerCartera = PuedeVerCartera(),
                PuedeVerCarteraGlobal = TienePermiso(PermisoCarteraGlobal),
                Solicitudes = string.IsNullOrWhiteSpace(estado) ? todas : todas.Where(x => x.Estado == estado).ToList(),
                Clientes = clientes,
                Pendientes = todas.Count(x => x.Estado == EstadosSolicitudCliente.Enviada),
                Aprobadas = todas.Count(x => x.Estado == EstadosSolicitudCliente.Aprobada),
                Rechazadas = todas.Count(x => x.Estado == EstadosSolicitudCliente.Rechazada),
                Resumenes = clientes.GroupBy(x => new { x.Empresa,
                    Moneda = string.IsNullOrWhiteSpace(x.Ficha.MonedaIndicadores) ? "Sin moneda" : x.Ficha.MonedaIndicadores })
                    .Select(g => new ClienteCrmResumenNegocio {
                        Empresa = g.Key.Empresa, Moneda = g.Key.Moneda,
                        Clientes = g.Count(), CompranActualmente = g.Count(x => x.Ficha.CompraActualmente),
                        Ventas12Meses = g.Sum(x => x.Ficha.Venta12Meses ?? 0),
                        PotencialAnual = g.Sum(x => x.Ficha.PotencialAnual ?? 0),
                        Objetivo12Meses = g.Sum(x => x.Ficha.Objetivo12Meses ?? 0)
                    }).OrderBy(x => x.Empresa).ThenBy(x => x.Moneda).ToList()
            };
            _crm.RegistrarEvento("MODULO", null, empresa, User.Identity.Name, "VER_DASHBOARD",
                string.Format("Estado={0}; Filtro={1}", estado, filtro), Ip());
            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso(PermisoDashboard)]
        public ActionResult ResolverSolicitud(long id, int version, string decision, string motivo)
        {
            try
            {
                bool aprobar = decision == "APROBAR";
                if (!aprobar && decision != "RECHAZAR")
                    throw new InvalidOperationException("Seleccione una decisión válida.");
                _crm.ResolverSolicitud(id, version, aprobar, motivo, User.Identity.Name, Ip());
                TempData["CrmExito"] = aprobar ? "Solicitud aprobada y ficha creada en SQL." : "Solicitud rechazada.";
            }
            catch (Exception ex) { TempData["CrmError"] = Mensaje(ex); }
            return RedirectToAction("Solicitud", new { id = id });
        }

        public ActionResult Ficha(long id, string empresa)
        {
            if (!EmpresaValidaOBlanca(empresa) || string.IsNullOrWhiteSpace(empresa))
                return SinAcceso();
            var c = _crm.ObtenerCliente(id, empresa);
            if (c == null) return HttpNotFound();
            bool global = TienePermiso(PermisoCarteraGlobal);
            if (!global && (!PuedeVerCartera() || !_crm.ClientePropio(id, empresa, User.Identity.Name)))
                return SinAcceso();
            _crm.RegistrarEvento("CLIENTE", id, empresa, User.Identity.Name, "VER_FICHA", null, Ip());
            CustomHelper.setTitle("Clientes", "Ficha de cliente");
            return View("Detalle", new ClienteCrmDetalleViewModel {
                Cliente = c, Archivos = _crm.Archivos(id, false, empresa),
                Eventos = _crm.Eventos("CLIENTE", id, empresa),
                EsVistaCreditos = TienePermiso(PermisoDashboard),
                EmpresasCliente = new[] { "BOLIK", "FAES", "GRACO" }
                    .Where(x => global || _crm.ClientePropio(id, x, User.Identity.Name))
                    .Select(x => _crm.ObtenerCliente(id, x)).Where(x => x != null).ToList(),
                PuedeEditar = TienePermiso(PermisoAdministrar)
            });
        }

        [Permiso(PermisoAdministrar)]
        public ActionResult NuevoCliente()
        {
            _crm.RegistrarEvento("MODULO", null, null, User.Identity.Name, "ABRIR_CLIENTE_NUEVO", null, Ip());
            CustomHelper.setTitle("Clientes", "Nuevo cliente CRM");
            return View("Editor", PrepararEditor(new ClienteCrmEditorViewModel { EsCliente = true, Activo = true }));
        }

        [Permiso(PermisoAdministrar)]
        public ActionResult EditarCliente(long id, string empresa)
        {
            if (!PuedeAbrirCliente(id, empresa)) return SinAcceso();
            var c = _crm.ObtenerCliente(id, empresa);
            if (c == null) return HttpNotFound();
            _crm.RegistrarEvento("CLIENTE", id, empresa, User.Identity.Name, "ABRIR_EDICION", null, Ip());
            CustomHelper.setTitle("Clientes", "Editar ficha");
            return View("Editor", PrepararEditor(new ClienteCrmEditorViewModel {
                EsCliente = true, ClienteId = id, Empresa = empresa, Version = c.Version,
                CodigoSap = c.CodigoSap, Activo = c.Activo,
                Ficha = c.Ficha, Archivos = _crm.Archivos(id, false, empresa)
            }));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso(PermisoAdministrar)]
        public ActionResult GuardarCliente(ClienteCrmEditorViewModel modelo)
        {
            if (modelo.ClienteId > 0 && !PuedeAbrirCliente(modelo.ClienteId, modelo.Empresa))
                return SinAcceso();
            try
            {
                if (modelo.ClienteId > 0)
                {
                    var anterior = _crm.ObtenerCliente(modelo.ClienteId, modelo.Empresa);
                    if (anterior == null) return HttpNotFound();
                    ConservarDatosLegados(anterior.Ficha, modelo.Ficha);
                }
                long id = _crm.GuardarCliente(new ClienteCrmCliente {
                    Id = modelo.ClienteId, Empresa = modelo.Empresa, Version = modelo.Version,
                    CodigoSap = modelo.CodigoSap, Activo = modelo.Activo || modelo.ClienteId == 0,
                    Ficha = modelo.Ficha
                }, LeerArchivos(), User.Identity.Name, Ip());
                TempData["CrmExito"] = "Ficha guardada en SQL Server.";
                return RedirectToAction("Ficha", new { id = id, empresa = modelo.Empresa });
            }
            catch (Exception ex)
            {
                modelo.EsCliente = true;
                modelo.Error = Mensaje(ex);
                if (modelo.ClienteId > 0)
                    modelo.Archivos = _crm.Archivos(modelo.ClienteId, false, modelo.Empresa);
                return View("Editor", PrepararEditor(modelo));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso(PermisoAdministrar)]
        public ActionResult CambiarActivo(long id, string empresa, int version, bool activo)
        {
            if (!PuedeAbrirCliente(id, empresa)) return SinAcceso();
            try
            {
                _crm.CambiarActivo(id, empresa, version, activo, User.Identity.Name, Ip());
                TempData["CrmExito"] = activo ? "Cliente reactivado." : "Cliente desactivado.";
            }
            catch (Exception ex) { TempData["CrmError"] = Mensaje(ex); }
            return RedirectToAction("Ficha", new { id = id, empresa = empresa });
        }

        public ActionResult Clientes(string empresa, string filtro, bool incluirInactivos = false)
        {
            if (!PuedeVerCartera() || !EmpresaValidaOBlanca(empresa)) return SinAcceso();
            CustomHelper.setTitle("Clientes", "Cartera CRM");
            bool global = TienePermiso(PermisoCarteraGlobal);
            if (!global && string.IsNullOrWhiteSpace(User.Identity.Name)) return SinAcceso();
            ViewBag.Empresa = empresa; ViewBag.Filtro = filtro;
            ViewBag.IncluirInactivos = incluirInactivos;
            ViewBag.PuedeAdministrar = TienePermiso(PermisoAdministrar);
            ViewBag.PuedeDashboard = TienePermiso(PermisoDashboard);
            ViewBag.EsGlobal = global;
            var lista = _crm.ListarClientes(empresa, filtro, incluirInactivos,
                global ? null : User.Identity.Name);
            _crm.RegistrarEvento("MODULO", null, empresa, User.Identity.Name, "LISTAR_CLIENTES", filtro, Ip());
            return View(lista);
        }

        public ActionResult Archivo(long id, long entidadId, bool esSolicitud, string empresa)
        {
            if (!_crm.ArchivoPertenece(id, entidadId, esSolicitud, empresa)) return HttpNotFound();
            if (esSolicitud)
            {
                var s = _crm.ObtenerSolicitud(entidadId);
                bool propia = s != null && TienePermiso(PermisoVer) && EsPropietario(s);
                if (s == null || (s.Estado == EstadosSolicitudCliente.Borrador && !propia) ||
                    (!TienePermiso(PermisoDashboard) && !propia))
                    return SinAcceso();
                empresa = s.Empresa;
            }
            else
            {
                var c = _crm.ObtenerCliente(entidadId, empresa);
                if (c == null) return HttpNotFound();
                if (!PuedeAbrirCliente(entidadId, empresa)) return SinAcceso();
            }
            var archivo = _crm.ObtenerArchivo(id);
            if (archivo == null) return HttpNotFound();
            _crm.RegistrarEvento(esSolicitud ? "SOLICITUD" : "CLIENTE", entidadId, empresa,
                User.Identity.Name, "DESCARGAR_ARCHIVO", archivo.Tipo, Ip());
            return File(archivo.Contenido, archivo.ContentType, archivo.Nombre);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso(PermisoDashboard)]
        public ActionResult Exportar(long[] ids)
        {
            var seleccion = new HashSet<long>((ids ?? new long[0]).Where(x => x > 0));
            if (seleccion.Count == 0)
            {
                TempData["CrmError"] = "Seleccione al menos una solicitud para exportar.";
                return RedirectToAction("Dashboard");
            }
            var filas = _crm.ListarSolicitudes(null, null, null, null)
                .Where(x => x.Estado != EstadosSolicitudCliente.Borrador && seleccion.Contains(x.Id)).ToList();
            using (var paquete = new ExcelPackage())
            {
                var hoja = paquete.Workbook.Worksheets.Add("Solicitudes");
                string[] columnas = { "Solicitud", "Empresa", "Estado", "Razón social", "NIT o DPI",
                    "Agente", "Condición de pago", "Creada", "Compra actualmente", "Moneda",
                    "Venta últimos 12 meses", "Potencial anual", "Objetivo 12 meses",
                    "Oportunidad", "Forecast", "Próximo paso", "Motivo de rechazo" };
                for (int i = 0; i < columnas.Length; i++) hoja.Cells[1, i + 1].Value = columnas[i];
                for (int i = 0; i < filas.Count; i++)
                {
                    var s = filas[i]; var f = s.Ficha; int r = i + 2;
                    object[] valores = { s.Id, EtiquetaEmpresa(s.Empresa), s.Estado, f.RazonSocial,
                        f.NitDpi, s.Agente, f.CondicionPago, s.CreadoEn, f.CompraActualmente ? "Sí" : "No",
                        f.MonedaIndicadores, f.Venta12Meses, f.PotencialAnual, f.Objetivo12Meses,
                        f.OportunidadCrecimiento, f.Forecast, f.ProximoPaso, s.MotivoRechazo };
                    for (int j = 0; j < valores.Length; j++) hoja.Cells[r, j + 1].Value = valores[j];
                }
                using (var rango = hoja.Cells[1, 1, 1, columnas.Length])
                {
                    rango.Style.Font.Bold = true;
                    rango.Style.Font.Color.SetColor(System.Drawing.Color.White);
                    rango.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    rango.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(13, 50, 88));
                }
                hoja.Cells[hoja.Dimension.Address].AutoFitColumns();
                hoja.View.FreezePanes(2, 1);
                hoja.Cells[2, 8, Math.Max(2, filas.Count + 1), 8].Style.Numberformat.Format = "yyyy-mm-dd hh:mm";
                hoja.Cells[2, 11, Math.Max(2, filas.Count + 1), 13].Style.Numberformat.Format = "#,##0.00";
                _crm.RegistrarEvento("MODULO", null, null, User.Identity.Name, "EXPORTAR",
                    "Solicitudes: " + string.Join(",", filas.Select(x => x.Id)), Ip());
                return File(paquete.GetAsByteArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "Clientes-CRM-" + DateTime.Today.ToString("yyyyMMdd") + ".xlsx");
            }
        }

        private ClienteCrmEditorViewModel PrepararEditor(ClienteCrmEditorViewModel m)
        {
            m = m ?? new ClienteCrmEditorViewModel();
            if (m.Ficha == null) m.Ficha = new ClienteCrmFicha();
            if (m.Error == null && (m.SolicitudId > 0 || m.ClienteId > 0))
            {
                if (m.Ficha.Contactos == null) m.Ficha.Contactos = new List<ClienteCrmContacto>();
                if (m.Ficha.Contactos.Count == 0 &&
                    !string.IsNullOrWhiteSpace(m.Ficha.ContactoPrincipal))
                    m.Ficha.Contactos.Add(new ClienteCrmContacto {
                        Area = "Principal", Nombre = m.Ficha.ContactoPrincipal,
                        Puesto = m.Ficha.CargoPrincipal, Telefono = m.Ficha.TelefonoPrincipal,
                        Correo = m.Ficha.CorreoPrincipal
                    });
                var principal = m.Ficha.Contactos.FirstOrDefault();
                if (principal != null)
                {
                    if (string.IsNullOrWhiteSpace(principal.TomadorDecision))
                        principal.TomadorDecision = m.Ficha.TomadorDecision;
                    if (string.IsNullOrWhiteSpace(principal.InfluenciadorTecnico))
                        principal.InfluenciadorTecnico = m.Ficha.InfluenciadorTecnico;
                }
                if (m.SolicitudId > 0 && m.Ficha.PasoCompletado == 0)
                    CompletarRespuestasLegadas(m.Ficha);
            }
            m.Empresas = m.EsCliente ? TodasEmpresas() : EmpresasUsuario();
            if (m.Archivos == null) m.Archivos = new List<ClienteCrmArchivo>();
            return m;
        }

        private static void CompletarRespuestasLegadas(ClienteCrmFicha f)
        {
            if (f.CambioRazonSocialRespuesta == null)
                f.CambioRazonSocialRespuesta = f.CambioRazonSocial ? "SI" : "NO";
            if (f.TeleventasRespuesta == null) f.TeleventasRespuesta = f.Televentas ? "SI" : "NO";
            if (f.VentaMostradorRespuesta == null)
                f.VentaMostradorRespuesta = f.VentaMostrador ? "SI" : "NO";
            if (f.VentaInstitucionalRespuesta == null)
                f.VentaInstitucionalRespuesta = f.VentaInstitucional ? "SI" : "NO";
            if (f.EcommerceRespuesta == null) f.EcommerceRespuesta = f.Ecommerce ? "SI" : "NO";
            if (f.CompraActualmenteRespuesta == null)
                f.CompraActualmenteRespuesta = f.CompraActualmente ? "SI" : "NO";
            foreach (var d in f.Direcciones ?? new List<ClienteCrmDireccion>())
            {
                if (d.RequiereCitaRespuesta == null)
                    d.RequiereCitaRespuesta = d.RequiereCita ? "SI" : "NO";
                if (d.ActivaRespuesta == null) d.ActivaRespuesta = d.Activa ? "SI" : "NO";
            }
        }

        private static void ConservarDatosLegados(ClienteCrmFicha anterior, ClienteCrmFicha actual)
        {
            if (anterior == null || actual == null) return;
            actual.ContactoPrincipal = anterior.ContactoPrincipal;
            actual.CargoPrincipal = anterior.CargoPrincipal;
            actual.TelefonoPrincipal = anterior.TelefonoPrincipal;
            actual.CorreoPrincipal = anterior.CorreoPrincipal;
            actual.TomadorDecision = anterior.TomadorDecision;
            actual.InfluenciadorTecnico = anterior.InfluenciadorTecnico;
            actual.ResponsableCompras = anterior.ResponsableCompras;
            actual.ContactoPagos = anterior.ContactoPagos;
            actual.CanalPreferido = anterior.CanalPreferido;
            actual.ObservacionesRelacion = anterior.ObservacionesRelacion;
        }

        private List<ClienteCrmEmpresaOpcion> EmpresasUsuario()
        {
            return _usuarioEmpresa.ObtenerPorUsuarioId(CustomHelper.getUserId())
                .Where(x => !string.IsNullOrWhiteSpace(x.Codigo))
                .GroupBy(x => _usuarioEmpresa.GetEmpresaNombre(x.EmpresaId))
                .Where(x => x.Key != "DESCONOCIDA")
                .Select(x => new ClienteCrmEmpresaOpcion {
                    Empresa = x.Key, Etiqueta = EtiquetaEmpresa(x.Key),
                    Agentes = x.Select(a => new ClienteCrmAgenteOpcion {
                        Codigo = a.Codigo.Trim(), Nombre = _usuarioEmpresa.ParseCodigo(a.Codigo).AgenteNombre
                    }).GroupBy(a => a.Codigo, StringComparer.OrdinalIgnoreCase)
                      .Select(a => a.First()).OrderBy(a => a.Nombre).ToList()
                }).OrderBy(x => x.Etiqueta).ToList();
        }

        private static List<ClienteCrmEmpresaOpcion> TodasEmpresas()
        {
            return new List<ClienteCrmEmpresaOpcion> {
                new ClienteCrmEmpresaOpcion { Empresa = "BOLIK", Etiqueta = "Bolik" },
                new ClienteCrmEmpresaOpcion { Empresa = "FAES", Etiqueta = "Escocesa" },
                new ClienteCrmEmpresaOpcion { Empresa = "GRACO", Etiqueta = "Graco Pack" }
            };
        }

        private string ValidarOperador(string empresa, string codigo)
        {
            var opcion = EmpresasUsuario().FirstOrDefault(x => x.Empresa == empresa);
            var agente = opcion == null ? null : opcion.Agentes.FirstOrDefault(x => x.Codigo == codigo);
            if (agente == null || string.IsNullOrWhiteSpace(agente.Nombre))
                throw new UnauthorizedAccessException("El agente no está asignado a su usuario para esta empresa.");
            return agente.Nombre;
        }

        private bool EsPropietario(ClienteCrmSolicitud s)
        {
            return string.Equals(s.CreadoPor, User.Identity.Name, StringComparison.OrdinalIgnoreCase);
        }

        private bool PuedeEditarSolicitud(ClienteCrmSolicitud s)
        {
            return TienePermiso(PermisoCrear) && EsPropietario(s) &&
                (s.Estado == EstadosSolicitudCliente.Borrador ||
                 s.Estado == EstadosSolicitudCliente.Rechazada) &&
                EmpresasUsuario().Any(x => x.Empresa == s.Empresa &&
                    x.Agentes.Any(a => a.Codigo == s.CodigoOperador));
        }

        private bool PuedeVerCartera()
        {
            return TienePermiso(PermisoVer) || TienePermiso(PermisoAdministrar) ||
                TienePermiso(PermisoCarteraGlobal);
        }

        private bool PuedeAbrirCliente(long id, string empresa)
        {
            return TienePermiso(PermisoCarteraGlobal) ||
                (PuedeVerCartera() && _crm.ClientePropio(id, empresa, User.Identity.Name));
        }

        private static bool TienePermiso(string permiso) { return CustomHelper.Permiso(permiso); }

        private ActionResult SinAcceso() { return RedirectToAction("NoAccess", "Seguridad"); }

        private static bool EmpresaValidaOBlanca(string empresa)
        {
            return string.IsNullOrWhiteSpace(empresa) || new[] { "BOLIK", "FAES", "GRACO" }
                .Contains(empresa, StringComparer.OrdinalIgnoreCase);
        }

        private static string EtiquetaEmpresa(string empresa)
        {
            return empresa == "FAES" ? "Escocesa" : empresa == "GRACO" ? "Graco Pack" : "Bolik";
        }

        private List<ClienteCrmArchivo> LeerArchivos()
        {
            var lista = new List<ClienteCrmArchivo>();
            var campos = new Dictionary<string, string> {
                { "Rtu", "RTU" }, { "Dpi", "DPI" },
                { "Nombramiento", "NOMBRAMIENTO" },
                { "PatenteComercio", "PATENTE_COMERCIO" },
                { "PatenteSociedad", "PATENTE_SOCIEDAD" }
            };
            foreach (var campo in campos)
            {
                var archivo = Request.Files[campo.Key];
                if (archivo == null || archivo.ContentLength == 0) continue;
                if (archivo.ContentLength > 5242880)
                    throw new InvalidOperationException("Cada documento debe ser menor de 5 MB.");
                byte[] contenido;
                using (var ms = new MemoryStream())
                {
                    archivo.InputStream.CopyTo(ms);
                    contenido = ms.ToArray();
                }
                string mime;
                if (contenido.Length >= 4 && contenido[0] == 0x25 && contenido[1] == 0x50 &&
                    contenido[2] == 0x44 && contenido[3] == 0x46) mime = "application/pdf";
                else if (contenido.Length >= 8 && contenido[0] == 0x89 && contenido[1] == 0x50 &&
                    contenido[2] == 0x4e && contenido[3] == 0x47) mime = "image/png";
                else if (contenido.Length >= 3 && contenido[0] == 0xff && contenido[1] == 0xd8 &&
                    contenido[2] == 0xff) mime = "image/jpeg";
                else throw new InvalidOperationException("Solo se admiten archivos PDF, PNG o JPG válidos.");
                string nombre = Path.GetFileName(archivo.FileName ?? "").Replace("\r", "").Replace("\n", "");
                if (nombre.Length > 255) nombre = nombre.Substring(nombre.Length - 255);
                if (nombre.Length == 0) nombre = campo.Value +
                    (mime == "application/pdf" ? ".pdf" : mime == "image/png" ? ".png" : ".jpg");
                lista.Add(new ClienteCrmArchivo {
                    Tipo = campo.Value, Nombre = nombre,
                    ContentType = mime, Tamano = contenido.Length, Contenido = contenido
                });
            }
            return lista;
        }

        private string Ip() { return Request.UserHostAddress; }

        private static string Mensaje(Exception ex)
        {
            if (ex is InvalidOperationException || ex is UnauthorizedAccessException) return ex.Message;
            return "No fue posible completar la operación. Revise la conexión SQL y contacte a Sistemas.";
        }
    }
}
