using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using DiamDev.Give.BLL;
using DiamDev.Give.Entities;
using DiamDev.Give.UI.Models;
using DiamDev.Give.UI.App_Start;

namespace DiamDev.Give.UI.Controllers
{
    [Authorize]
    // [Seguridad]
    [HandleError]
    public class ReciboCajaController : Controller
    {
        private readonly ReciboCajaBLL _bll = new ReciboCajaBLL();


        // ─────────────────────────────────────────────
        // GET /ReciboCaja/
        // ─────────────────────────────────────────────
        // [Permiso("Control.ReciboCaja.Ver")]
        public ActionResult Index()
        {
            CustomHelper.setTitle("Recibos de Caja", "Ingreso");

            // El DEPTO ya no se resuelve por usuario logueado (RecibosCaja_UsuarioDepto,
            // retirada del flujo): ahora depende del OPERADOR elegido en "Operar como"
            // (Usuario_Empresa.DEPTO_RECIBO). La tarjeta del header la pinta el JS
            // vía PintarCardOperador() al seleccionar operador.
            var model = new ReciboCajaIndexViewModel
            {
                UsuarioActual = User.Identity.Name,
                PlantaUsuario = ""   // legado: el ViewModel la conserva, pero ya nadie la usa
            };

            return View(model);
        }

        // ─────────────────────────────────────────────
        // GET /ReciboCaja/GetEmpresasUsuario
        // Devuelve solo las empresas (GRACO/FAES/BOLIK) que el usuario
        // tiene asignadas en Usuario_Empresa. El select se llena con esto.
        // ─────────────────────────────────────────────
        [HttpGet]
        // [Permiso("Control.ReciboCaja.Ver")]
        public JsonResult GetEmpresasUsuario()
        {
            try
            {
                long usuarioId = CustomHelper.getUserId();
                var empresas = _bll.ObtenerEmpresasUsuario(usuarioId);
                return Json(new { ok = true, data = empresas }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, msg = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // ─────────────────────────────────────────────
        // GET /ReciboCaja/ObtenerTipoCambioDia?empresa=GRACO
        // Devuelve el TC USD del día para mostrarlo en la UI (referencia).
        // ─────────────────────────────────────────────
        [HttpGet]
        public JsonResult ObtenerTipoCambioDia(string empresa)
        {
            try
            {
                decimal tc = _bll.ObtenerTipoCambioDia(empresa ?? "");
                return Json(new { ok = true, tipoCambio = tc }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, msg = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // ─────────────────────────────────────────────
        // GET /ReciboCaja/BuscarClientes
        // Llamado por AJAX (typeahead del campo cliente)
        // ─────────────────────────────────────────────
        [HttpGet]
        // [Permiso("Control.ReciboCaja.Ver")]
        public JsonResult BuscarClientes(string empresa, string agente, string filtro)
        {
            try
            {
                var lista = _bll.BuscarClientes(empresa ?? "", agente ?? "", filtro ?? "");
                return Json(new { ok = true, data = lista }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, msg = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // ─────────────────────────────────────────────
        // GET /ReciboCaja/ObtenerDocumentos
        // Llamado por AJAX al abrir el modal de búsqueda de documentos.
        // Además de los documentos, devuelve los ANTICIPOS en tránsito del
        // cliente (barra informativa del modal).
        // ─────────────────────────────────────────────
        [HttpGet]
        // [Permiso("Control.ReciboCaja.Ver")]
        public JsonResult ObtenerDocumentos(string empresa, string clienteId, string tipoDoc)
        {
            try
            {
                var docs = _bll.ObtenerDocumentos(empresa ?? "", clienteId ?? "", tipoDoc ?? "");
                var anticipos = _bll.ObtenerAnticiposTransito(empresa ?? "", clienteId ?? "");
                return Json(new { ok = true, data = docs, anticipos },
                            JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, msg = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // ─────────────────────────────────────────────
        // POST /ReciboCaja/Guardar
        // ─────────────────────────────────────────────
        [HttpPost]
        // [Permiso("Control.ReciboCaja.Guardar")]
        public JsonResult Guardar(GuardarReciboRequest request)
        {
            try
            {
                string login = User.Identity.Name;
                long usuarioId = CustomHelper.getUserId();
                // El DEPTO/serie ahora es DEL OPERADOR elegido, no del usuario logueado.
                // Valida: pertenencia del código, SERIE_SAP asignado y serie existente.
                string depto = _bll.ObtenerDeptoOperador(usuarioId, request.IdEmpresa, request.CodigoUsuario);
                string usuario = login;                              // grabamos el login POS
                string ip = Request.UserHostAddress;                 // para analytics

                // Mapear ViewModel → Entity
                var enc = new ReciboCajaEncabezado
                {
                    IdEmpresa = request.IdEmpresa,
                    IdCliente = request.IdCliente,
                    NombreCliente = request.NombreCliente,
                    Direccion = request.Direccion,
                    Nit = request.Nit,
                    Agente = request.Agente,
                    Correo = request.Correo,
                    Moneda = request.Moneda,
                    RecFisico = request.RecFisico,
                    CodigoUsuario = request.CodigoUsuario,   // ← NUEVO: código de Usuario_Empresa elegido
                    Usuario = usuario,
                    FechaRecibo = DateTime.TryParse(request.FechaRecibo, out var fd) ? fd : DateTime.Today,

                    Cobros = request.Cobros?.Select(c => new ReciboCajaCobro
                    {
                        TipoCobro = c.TipoCobro,
                        Banco = c.Banco,
                        NoDocumento = c.NoDocumento,
                        Monto = c.Monto,
                        Moneda = c.Moneda,
                        FechaDoc = DateTime.TryParse(c.FechaDoc, out var fc) ? fc : (DateTime?)null
                    }).ToList() ?? new List<ReciboCajaCobro>(),

                    Documentos = request.Documentos?.Select(d => new ReciboCajaDetalle
                    {
                        TipoDoc = d.TipoDoc,
                        NoDocumento = d.NoDocumento,
                        Status = d.Status,
                        Monto = d.Monto,
                        Moneda = d.Moneda,
                        MontoFact = d.MontoFact,
                        Pagado = d.Pagado,
                        FelSerie = d.FelSerie,
                        FelNumero = d.FelNumero,
                        FechaDoc = DateTime.TryParse(d.FechaDoc, out var fdd) ? fdd : (DateTime?)null
                    }).ToList() ?? new List<ReciboCajaDetalle>()
                };

                var resultado = _bll.GuardarRecibo(enc, depto, usuarioId, login, ip);
                return Json(new { ok = resultado.Exito, msg = resultado.Mensaje, idRecibo = resultado.IdRecibo });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, msg = "Error inesperado: " + ex.Message });
            }
        }

        // ─────────────────────────────────────────────
        // GET /ReciboCaja/BuscarRecibo
        // ─────────────────────────────────────────────
        [HttpGet]
        // [Permiso("Control.ReciboCaja.Ver")]
        public JsonResult BuscarRecibo(string idRecibo, string empresa)
        {
            try
            {
                var rec = _bll.BuscarRecibo(idRecibo ?? "", empresa ?? "");
                if (rec == null)
                    return Json(new { ok = false, msg = "Recibo no encontrado." }, JsonRequestBehavior.AllowGet);

                return Json(new { ok = true, data = rec }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, msg = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // ─────────────────────────────────────────────
        // GET /ReciboCaja/Imprimir/{idRecibo}/{empresa}
        // Abre vista de impresión en nueva pestaña.
        // BLOQUEA recibos en DESCUADRE (validación de servidor: el disabled
        // del botón en la vista es solo cosmético, esto es lo que manda).
        // ─────────────────────────────────────────────
        /// [Permiso("Control.ReciboCaja.Ver")]
        public ActionResult Imprimir(string idRecibo, string empresa)
        {
            var rec = _bll.BuscarRecibo(idRecibo, empresa);
            if (rec == null) return HttpNotFound("Recibo no encontrado.");

            if ("DESCUADRE".Equals(rec.SyncEstado ?? "", StringComparison.OrdinalIgnoreCase))
            {
                string html =
                    "<!DOCTYPE html><html><head><meta charset='utf-8'>" +
                    "<title>Impresión bloqueada</title>" +
                    "<style>body{font-family:Arial,sans-serif;background:#f4f6f7;display:flex;" +
                    "align-items:center;justify-content:center;height:100vh;margin:0}" +
                    ".box{background:#fff;border-top:5px solid #e74c3c;border-radius:8px;" +
                    "box-shadow:0 8px 30px rgba(0,0,0,.12);padding:30px 36px;max-width:580px}" +
                    "h2{color:#a93226;margin-top:0}p{color:#5d6d7e;font-size:14px;line-height:1.5}" +
                    "code{background:#fdf2f0;color:#a93226;padding:2px 6px;border-radius:4px}" +
                    ".obs{background:#fef9e7;border:1px solid #f1c40f;border-radius:5px;" +
                    "padding:10px 12px;font-size:12.5px;color:#7d6608}</style></head><body>" +
                    "<div class='box'><h2>⚠ Impresión bloqueada</h2>" +
                    "<p>El recibo <code>" + Server.HtmlEncode(idRecibo ?? "") + "</code> tiene un " +
                    "<strong>descuadre con SAP</strong>: parte del pago fue anulado y aún no ha " +
                    "sido re-aplicado por Créditos.</p>" +
                    "<div class='obs'>" + Server.HtmlEncode(rec.SyncObservacion ?? "") + "</div>" +
                    "<p style='margin-top:14px;'>Cuando Créditos re-aplique el monto en SAP " +
                    "(con el mismo recibo en <code>U_Recibocaja_Webapp</code>), el sincronizador " +
                    "liberará la impresión automáticamente.</p></div></body></html>";

                return Content(html, "text/html");
            }

            return View(rec);
        }

        // ─────────────────────────────────────────────
        // POST /ReciboCaja/Anular
        // Las reglas viven en el BLL; el disabled del botón es cosmético.
        // ─────────────────────────────────────────────
        [HttpPost]
        // [Permiso("Control.ReciboCaja.Anular")]
        public JsonResult Anular(string idRecibo, string empresa, string motivo)
        {
            try
            {
                string login = User.Identity.Name;
                long usuarioId = CustomHelper.getUserId();
                string ip = Request.UserHostAddress;

                var r = _bll.AnularRecibo(idRecibo ?? "", empresa ?? "", motivo ?? "",
                                          usuarioId, login, ip);
                return Json(new { ok = r.Exito, msg = r.Mensaje });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, msg = "Error inesperado: " + ex.Message });
            }
        }

        // ─────────────────────────────────────────────
        // GET /ReciboCaja/ImprimirLote?ids=RG12-07520|GRACO,RG12-07521|GRACO
        // Junta varios recibos en UN documento imprimible (un recibo por página).
        // Re-valida en servidor: omite DESCUADRES y no encontrados, y lo informa.
        // ─────────────────────────────────────────────
        // [Permiso("Control.ReciboCaja.Ver")]
        public ActionResult ImprimirLote(string ids)
        {
            var recibos = new List<ReciboCajaEncabezado>();
            var omitidos = new List<string>();

            var pares = (ids ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                   .Take(50);   // tope de seguridad, espejo del front

            foreach (var par in pares)
            {
                var partes = par.Split('|');
                if (partes.Length != 2) continue;
                string id = partes[0].Trim(), emp = partes[1].Trim();

                var rec = _bll.BuscarRecibo(id, emp);
                if (rec == null)
                {
                    omitidos.Add(id + " (no encontrado)");
                }
                else if ("DESCUADRE".Equals(rec.SyncEstado ?? "", StringComparison.OrdinalIgnoreCase))
                {
                    omitidos.Add(id + " (descuadre con SAP: impresión bloqueada)");
                }
                else
                {
                    recibos.Add(rec);
                }
            }

            if (recibos.Count == 0)
                return Content("<html><body style='font-family:Arial;padding:40px;'>" +
                    "<h3>⚠ Nada para imprimir</h3><p>Ningún recibo del lote es imprimible:</p><ul><li>" +
                    string.Join("</li><li>", omitidos.Select(Server.HtmlEncode)) +
                    "</li></ul></body></html>", "text/html");

            ViewBag.Omitidos = omitidos;
            return View(recibos);
        }

        private readonly ReciboCajaAdminBLL _admin = new ReciboCajaAdminBLL();

        // ═════════════════════════════════════════════
        //  DASHBOARD DE SUPERVISIÓN
        // ═════════════════════════════════════════════

        /// <summary>Permiso que otorga visión global del dashboard (sin filtro por operador).</summary>
        private const string PERMISO_DASHBOARD_GLOBAL = "Control.ReciboCaja.DashboardGlobal";

        /// <summary>
        /// ¿El usuario ve TODO el dashboard, o solo sus operadores de Usuario_Empresa?
        ///
        /// Dos mecanismos, en OR:
        ///   1. PERMISO (principal): pregunta por CAPACIDAD, no por identidad. Si
        ///      mañana nace el rol "CREDITOS SR", se le asigna el permiso en la BD
        ///      y funciona sin tocar código.
        ///   2. NOMBRE DE ROL en Web.config (red de seguridad): mientras el permiso
        ///      no exista en la BD, Permiso() devuelve false y NADIE sería global —
        ///      Créditos abriría un dashboard vacío. Esta lista evita ese hueco.
        ///      Cuando el permiso esté creado y probado, vacía la clave del config.
        ///
        /// Los try/catch son deliberados: si el subsistema de permisos falla, la
        /// respuesta correcta es "no es global" (FALLA CERRADO), no una pantalla
        /// de error ni —mucho peor— acceso total.
        /// </summary>
        private bool EsGlobalActual()
        {
            // 1) Permiso explícito
            try
            {
                if (CustomHelper.Permiso(PERMISO_DASHBOARD_GLOBAL)) return true;
            }
            catch { /* permiso inexistente o subsistema caído → no es global */ }

            // 2) Fallback por nombre de rol (Web.config → DashboardRolesGlobales)
            try
            {
                return _admin.EsRolGlobal(RolUsuarioActual());
            }
            catch { return false; }
        }

        /// <summary>
        /// ⚠ PENDIENTE: nombre del rol del usuario logueado, para el fallback de
        /// Web.config. Devuelve "" mientras no se conecte, y "" nunca es global
        /// → todos quedan restringidos a su alcance. Es el default seguro.
        ///
        /// No lo cableé a ciegas: CustomHelper no expone el rol, y adivinar el
        /// nombre de la propiedad en la entidad Usuario es exactamente el error
        /// que ya nos costó una ronda con INVOICE_DATE / CURRENCY_ID.
        /// Se completa en cuanto veamos Usuario.cs.
        /// </summary>
        private string RolUsuarioActual()
        {
            return "";
        }

        /// <summary>Alcance del usuario logueado. Un solo punto de construcción.</summary>
        private AlcanceRecibos AlcanceActual()
        {
            return _admin.ObtenerAlcance(CustomHelper.getUserId(), EsGlobalActual());
        }

        // [Permiso("Control.ReciboCaja.Dashboard")]
        public ActionResult Dashboard()
        {
            CustomHelper.setTitle("Recibos de Caja", "Supervisión");
            ViewBag.DiasUmbral = _admin.DiasUmbral;

            var alcance = AlcanceActual();
            ViewBag.AlcanceGlobal = alcance.Global;
            ViewBag.AlcanceTexto = alcance.Descripcion;

            return View();
        }

        [HttpGet]
        // [Permiso("Control.ReciboCaja.Dashboard")]
        public JsonResult GetDashboardResumen(string empresa)
        {
            try
            {
                var alcance = AlcanceActual();
                var r = _admin.ObtenerResumen(empresa, alcance);
                return Json(new
                {
                    ok = true,
                    data = r,
                    global = alcance.Global,
                    operadores = alcance.Pares.Count,
                    sinAcceso = alcance.SinAcceso,
                    empresas = alcance.Empresas,
                    alcanceTexto = alcance.Descripcion
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, msg = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        // [Permiso("Control.ReciboCaja.Dashboard")]
        public JsonResult GetDashboardDetalle(string empresa, string situacion,
             string fechaIni, string fechaFin,
             bool incluirOperados = false, bool incluirAnulados = false)
        {
            try
            {
                // El alcance se resuelve SIEMPRE en el servidor. Nunca viaja por
                // querystring: el front es cosmético y se puede falsificar desde F12.
                var alcance = AlcanceActual();

                var filas = _admin.ObtenerDetalle(empresa, situacion,
                                                  fechaIni, fechaFin,
                                                  incluirOperados, incluirAnulados,
                                                  alcance);
                return Json(new { ok = true, data = filas }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, msg = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // ═════════════════════════════════════════════
        //  MANTENIMIENTO DE SERIES
        // ═════════════════════════════════════════════
        // [Permiso("Control.ReciboCaja.Series")]
        public ActionResult Series()
        {
            CustomHelper.setTitle("Recibos de Caja", "Series de Numeración");
            return View();
        }

        [HttpGet]
        // [Permiso("Control.ReciboCaja.Series")]
        public JsonResult GetSeries()
        {
            try
            {
                var lista = _admin.ObtenerSeries();
                // Proyección explícita: incluye los calculados (ProximoId, Inconsistente)
                var data = lista.Select(s => new
                {
                    s.RowId,
                    s.Empresa,
                    s.Depto,
                    s.Serie,
                    s.Numeracion,
                    s.SerieNc,
                    s.NumeracionNc,
                    s.MaxUsado,
                    s.ProximoId,
                    s.Inconsistente
                }).ToList();
                return Json(new { ok = true, data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, msg = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        // [Permiso("Control.ReciboCaja.Series")]
        public JsonResult GuardarSerie(ReciboCajaSerie request)
        {
            try
            {
                var r = _admin.GuardarSerie(request ?? new ReciboCajaSerie());
                return Json(new
                {
                    ok = r.Exito,
                    msg = r.Exito
                    ? "Serie guardada. Próximo recibo: " + r.IdRecibo
                    : r.Mensaje
                });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, msg = "Error inesperado: " + ex.Message });
            }
        }

        [HttpPost]
        // [Permiso("Control.ReciboCaja.Series")]
        public JsonResult EliminarSerie(int rowId)
        {
            try
            {
                var r = _admin.EliminarSerie(rowId);
                return Json(new { ok = r.Exito, msg = r.Exito ? "Serie eliminada." : r.Mensaje });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, msg = "Error inesperado: " + ex.Message });
            }
        }

        // TEMPORAL — borrar después de validar. Prueba ObtenerTipoCambio por la ruta real (HanaHelper).
        [HttpGet]
        public JsonResult TestTC(string empresa)
        {
            try
            {
                var hana = new DiamDev.Give.DAL.HanaRepository();
                decimal tc = hana.ObtenerTipoCambio(empresa ?? "GRACO", null);
                return Json(new { ok = true, empresa, tipoCambio = tc }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, msg = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // TEMPORAL — valida el cálculo dual. Borrar después.
        [HttpGet]
        public JsonResult TestDual(decimal monto, string moneda, decimal tc)
        {
            try
            {
                var d = DiamDev.Give.BLL.ReciboCajaBLL.CalcularMontosDuales(monto, moneda, tc);
                return Json(new
                {
                    ok = true,
                    original = new { monto, moneda },
                    tc,
                    gtq = d.Gtq,
                    usd = d.Usd
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, msg = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // Helper local: desentierra toda la cadena de inner exceptions
        private static System.Collections.Generic.List<string> Cadena(Exception ex)
        {
            var msgs = new System.Collections.Generic.List<string>();
            var e = ex;
            while (e != null) { msgs.Add(e.Message); e = e.InnerException; }
            return msgs;
        }
    }
}