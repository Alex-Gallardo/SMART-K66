using System;
using System.Collections.Generic;
using System.Configuration;
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
        private readonly ClienteCrmBLL _crm = new ClienteCrmBLL();
        private readonly UsuarioEmpresaBL _usuarioEmpresa = new UsuarioEmpresaBL();

        [Permiso(PermisoVer)]
        public ActionResult Index(string empresa, string estado, string filtro)
        {
            CustomHelper.setTitle("Clientes", "Mis solicitudes");
            var empresas = EmpresasUsuario();
            if (!string.IsNullOrWhiteSpace(empresa) && !empresas.Any(x => x.Empresa == empresa))
                return new HttpUnauthorizedResult();
            ViewBag.Empresas = empresas;
            ViewBag.Empresa = empresa;
            ViewBag.Estado = estado;
            ViewBag.Filtro = filtro;
            var solicitudes = _crm.ListarSolicitudes(empresa, estado, filtro, User.Identity.Name)
                .Where(x => empresas.Any(e => e.Empresa == x.Empresa)).ToList();
            _crm.RegistrarEvento("MODULO", null, empresa, User.Identity.Name, "LISTAR_PROPIAS",
                string.Format("Estado={0}; Filtro={1}", estado, filtro), Ip());
            return View(solicitudes);
        }

        [Permiso(PermisoCrear)]
        public ActionResult Nueva()
        {
            if (!EsVendedor()) return new HttpUnauthorizedResult();
            _crm.RegistrarEvento("MODULO", null, null, User.Identity.Name, "ABRIR_SOLICITUD_NUEVA", null, Ip());
            CustomHelper.setTitle("Clientes", "Nueva solicitud");
            return View("Editor", PrepararEditor(new ClienteCrmEditorViewModel()));
        }

        [Permiso(PermisoCrear)]
        public ActionResult EditarSolicitud(long id)
        {
            if (!EsVendedor()) return new HttpUnauthorizedResult();
            var s = _crm.ObtenerSolicitud(id);
            if (s == null) return HttpNotFound();
            if (!EsPropietario(s) ||
                (s.Estado != EstadosSolicitudCliente.Borrador && s.Estado != EstadosSolicitudCliente.Rechazada))
                return new HttpUnauthorizedResult();
            _crm.RegistrarEvento("SOLICITUD", id, s.Empresa, User.Identity.Name, "ABRIR_EDICION", null, Ip());
            CustomHelper.setTitle("Clientes", "Editar solicitud");
            return View("Editor", PrepararEditor(new ClienteCrmEditorViewModel {
                SolicitudId = s.Id, Version = s.Version, Empresa = s.Empresa,
                CodigoOperador = s.CodigoOperador, Estado = s.Estado,
                Ficha = s.Ficha, Archivos = s.Archivos
            }));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso(PermisoCrear)]
        public ActionResult GuardarSolicitud(ClienteCrmEditorViewModel modelo, string accion)
        {
            if (!EsVendedor()) return new HttpUnauthorizedResult();
            bool enviar = string.Equals(accion, "ENVIAR", StringComparison.OrdinalIgnoreCase);
            try
            {
                if (modelo.SolicitudId > 0)
                {
                    var existente = _crm.ObtenerSolicitud(modelo.SolicitudId);
                    if (existente == null) return HttpNotFound();
                    if (!string.Equals(existente.Empresa, modelo.Empresa,
                        StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("No se puede cambiar la empresa de una solicitud existente.");
                }
                var agente = ValidarOperador(modelo.Empresa, modelo.CodigoOperador);
                long id = _crm.GuardarSolicitud(new ClienteCrmSolicitud {
                    Id = modelo.SolicitudId, Version = modelo.Version, Empresa = modelo.Empresa,
                    CodigoOperador = modelo.CodigoOperador, Agente = agente,
                    Ficha = modelo.Ficha
                }, LeerArchivos(), enviar, User.Identity.Name, Ip());
                TempData["CrmExito"] = enviar ? "Solicitud enviada a Créditos." : "Borrador guardado.";
                return RedirectToAction("Solicitud", new { id = id });
            }
            catch (Exception ex)
            {
                modelo.Error = Mensaje(ex);
                return View("Editor", PrepararEditor(modelo));
            }
        }

        public ActionResult Solicitud(long id)
        {
            var s = _crm.ObtenerSolicitud(id);
            if (s == null) return HttpNotFound();
            bool creditos = EsCreditos() && TienePermiso(PermisoDashboard);
            if (creditos && s.Estado == EstadosSolicitudCliente.Borrador && !EsPropietario(s))
                return new HttpUnauthorizedResult();
            if (!creditos && (!TienePermiso(PermisoVer) || !EsPropietario(s)))
                return new HttpUnauthorizedResult();
            _crm.RegistrarEvento("SOLICITUD", id, s.Empresa, User.Identity.Name, "VER", null, Ip());
            CustomHelper.setTitle("Clientes", "Detalle de solicitud");
            return View("Detalle", new ClienteCrmDetalleViewModel {
                Solicitud = s, Archivos = s.Archivos,
                Eventos = _crm.Eventos("SOLICITUD", id),
                EsVistaCreditos = creditos,
                PuedeResolver = creditos && s.Estado == EstadosSolicitudCliente.Enviada,
                PuedeEditar = !creditos && EsPropietario(s) &&
                    (s.Estado == EstadosSolicitudCliente.Borrador || s.Estado == EstadosSolicitudCliente.Rechazada)
            });
        }

        [Permiso(PermisoDashboard)]
        public ActionResult Dashboard(string empresa, string estado, string filtro)
        {
            if (!EsCreditos()) return new HttpUnauthorizedResult();
            if (!EmpresaValidaOBlanca(empresa)) return new HttpUnauthorizedResult();
            CustomHelper.setTitle("Clientes", "Dashboard de Créditos");
            var todas = _crm.ListarSolicitudes(empresa, null, filtro, null)
                .Where(x => x.Estado != EstadosSolicitudCliente.Borrador).ToList();
            var clientes = _crm.ListarClientes(empresa, filtro, false);
            var modelo = new ClienteCrmDashboardViewModel {
                Empresa = empresa, Estado = estado, Filtro = filtro,
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
            if (!EsCreditos()) return new HttpUnauthorizedResult();
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
                return new HttpUnauthorizedResult();
            var c = _crm.ObtenerCliente(id, empresa);
            if (c == null) return HttpNotFound();
            bool creditos = EsCreditos() && TienePermiso(PermisoDashboard);
            var propias = creditos ? new List<ClienteCrmSolicitud>() :
                _crm.ListarSolicitudes(empresa, EstadosSolicitudCliente.Aprobada, null, User.Identity.Name);
            if (!creditos && (!TienePermiso(PermisoVer) || !propias.Any(x => x.ClienteId == id)))
                return new HttpUnauthorizedResult();
            _crm.RegistrarEvento("CLIENTE", id, empresa, User.Identity.Name, "VER_FICHA", null, Ip());
            CustomHelper.setTitle("Clientes", "Ficha de cliente");
            return View("Detalle", new ClienteCrmDetalleViewModel {
                Cliente = c, Archivos = _crm.Archivos(id, false, empresa),
                Eventos = _crm.Eventos("CLIENTE", id),
                EsVistaCreditos = creditos,
                EmpresasCliente = creditos ? new[] { "BOLIK", "FAES", "GRACO" }
                    .Select(x => _crm.ObtenerCliente(id, x)).Where(x => x != null).ToList()
                    : new List<ClienteCrmCliente> { c },
                PuedeEditar = creditos && TienePermiso(PermisoAdministrar)
            });
        }

        [Permiso(PermisoAdministrar)]
        public ActionResult NuevoCliente()
        {
            if (!EsCreditos()) return new HttpUnauthorizedResult();
            _crm.RegistrarEvento("MODULO", null, null, User.Identity.Name, "ABRIR_CLIENTE_NUEVO", null, Ip());
            CustomHelper.setTitle("Clientes", "Nuevo cliente CRM");
            return View("Editor", PrepararEditor(new ClienteCrmEditorViewModel { EsCliente = true, Activo = true }));
        }

        [Permiso(PermisoAdministrar)]
        public ActionResult EditarCliente(long id, string empresa)
        {
            if (!EsCreditos()) return new HttpUnauthorizedResult();
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
            if (!EsCreditos()) return new HttpUnauthorizedResult();
            try
            {
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
                return View("Editor", PrepararEditor(modelo));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso(PermisoAdministrar)]
        public ActionResult CambiarActivo(long id, string empresa, int version, bool activo)
        {
            if (!EsCreditos()) return new HttpUnauthorizedResult();
            try
            {
                _crm.CambiarActivo(id, empresa, version, activo, User.Identity.Name, Ip());
                TempData["CrmExito"] = activo ? "Cliente reactivado." : "Cliente desactivado.";
            }
            catch (Exception ex) { TempData["CrmError"] = Mensaje(ex); }
            return RedirectToAction("Ficha", new { id = id, empresa = empresa });
        }

        [Permiso(PermisoDashboard)]
        public ActionResult Clientes(string empresa, string filtro, bool incluirInactivos = false)
        {
            if (!EsCreditos() || !EmpresaValidaOBlanca(empresa)) return new HttpUnauthorizedResult();
            CustomHelper.setTitle("Clientes", "Cartera CRM");
            ViewBag.Empresa = empresa; ViewBag.Filtro = filtro; ViewBag.IncluirInactivos = incluirInactivos;
            var lista = _crm.ListarClientes(empresa, filtro, incluirInactivos);
            _crm.RegistrarEvento("MODULO", null, empresa, User.Identity.Name, "LISTAR_CLIENTES", filtro, Ip());
            return View(lista);
        }

        public ActionResult Archivo(long id, long entidadId, bool esSolicitud, string empresa)
        {
            bool creditos = EsCreditos() && TienePermiso(PermisoDashboard);
            if (!_crm.ArchivoPertenece(id, entidadId, esSolicitud, empresa)) return HttpNotFound();
            if (esSolicitud)
            {
                var s = _crm.ObtenerSolicitud(entidadId);
                if (s == null || (s.Estado == EstadosSolicitudCliente.Borrador && !EsPropietario(s)) ||
                    (!creditos && (!TienePermiso(PermisoVer) || !EsPropietario(s))))
                    return new HttpUnauthorizedResult();
                empresa = s.Empresa;
            }
            else
            {
                var c = _crm.ObtenerCliente(entidadId, empresa);
                if (c == null) return HttpNotFound();
                if (!creditos && (!TienePermiso(PermisoVer) || !_crm.ListarSolicitudes(empresa, EstadosSolicitudCliente.Aprobada,
                    null, User.Identity.Name).Any(x => x.ClienteId == entidadId)))
                    return new HttpUnauthorizedResult();
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
            if (!EsCreditos()) return new HttpUnauthorizedResult();
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
            m.Empresas = m.EsCliente ? TodasEmpresas() : EmpresasUsuario();
            if (m.Archivos == null) m.Archivos = new List<ClienteCrmArchivo>();
            return m;
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
            return string.Equals(s.CreadoPor, User.Identity.Name, StringComparison.OrdinalIgnoreCase) &&
                EmpresasUsuario().Any(x => x.Empresa == s.Empresa);
        }

        private bool EsVendedor()
        {
            return new RolBL().UsuarioTieneRol(User.Identity.Name, "VendedorK66");
        }

        private bool EsCreditos()
        {
            string roles = ConfigurationManager.AppSettings["ClientesCrm.RolesCreditos"];
            if (string.IsNullOrWhiteSpace(roles)) roles = "CREDITOS,CREDITOS GERENCIA";
            var rolBl = new RolBL();
            return roles.Split(',').Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .Any(x => rolBl.UsuarioTieneRol(User.Identity.Name, x));
        }

        private static bool TienePermiso(string permiso) { return CustomHelper.Permiso(permiso); }

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
