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

            string login = User.Identity.Name;

            // El header muestra el MISMO depto que numerará el recibo
            // (RecibosCaja_UsuarioDepto en POS, vía ObtenerDeptoSerie), para que
            // lo que ve el usuario sea exactamente lo que se grabará.
            //
            // Se abandonó ObtenerPlantaPorLogin: apuntaba a REC_CAJA_USUARIOS,
            // tabla inexistente en esta BD (confirmado vía TestPlanta:
            // "Invalid object name 'REC_CAJA_USUARIOS'").
            string depto = "";
            try
            {
                long usuarioId = CustomHelper.getUserId();
                depto = _bll.ObtenerDeptoSerie(usuarioId);
            }
            catch
            {
                // Usuario sin DEPTO de serie asignado → header vacío.
                // El guardado lo vuelve a validar y dará un error claro si falta.
                depto = "";
            }

            var model = new ReciboCajaIndexViewModel
            {
                UsuarioActual = login,
                PlantaUsuario = depto
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
        // Llamado por AJAX al abrir el modal de búsqueda de documentos
        // ─────────────────────────────────────────────
        [HttpGet]
        // [Permiso("Control.ReciboCaja.Ver")]
        public JsonResult ObtenerDocumentos(string empresa, string clienteId, string tipoDoc)
        {
            try
            {
                var docs = _bll.ObtenerDocumentos(empresa ?? "", clienteId ?? "", tipoDoc ?? "");
                return Json(new { ok = true, data = docs }, JsonRequestBehavior.AllowGet);
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
                string depto = _bll.ObtenerDeptoSerie(usuarioId);   // ← nuevo (lee de POS)
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

        private readonly ReciboCajaAdminBLL _admin = new ReciboCajaAdminBLL();

        // ═════════════════════════════════════════════
        //  DASHBOARD DE SUPERVISIÓN
        // ═════════════════════════════════════════════
        // [Permiso("Control.ReciboCaja.Dashboard")]
        public ActionResult Dashboard()
        {
            CustomHelper.setTitle("Recibos de Caja", "Supervisión");
            ViewBag.DiasUmbral = _admin.DiasUmbral;
            return View();
        }

        [HttpGet]
        // [Permiso("Control.ReciboCaja.Dashboard")]
        public JsonResult GetDashboardResumen(string empresa)
        {
            try
            {
                var r = _admin.ObtenerResumen(empresa);
                return Json(new { ok = true, data = r }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, msg = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        // [Permiso("Control.ReciboCaja.Dashboard")]
        public JsonResult GetDashboardDetalle(string empresa, string situacion)
        {
            try
            {
                var filas = _admin.ObtenerDetalle(empresa, situacion);
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
        // TEMPORAL — valida que EF lee RecibosCaja_UsuarioDepto. Borrar después.
        // TEMPORAL — versión que muestra la inner exception real. Borrar después.
        [HttpGet]
        public JsonResult TestDepto()
        {
            try
            {
                long uid = DiamDev.Give.UI.App_Start.CustomHelper.getUserId();
                string depto = new DiamDev.Give.DAL.RecibosCajaUsuarioDeptoDA()
                                   .ObtenerDeptoPorUsuarioId(uid);
                return Json(new { ok = true, usuarioId = uid, depto }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // Desenterrar TODA la cadena de inner exceptions
                var msgs = new System.Collections.Generic.List<string>();
                var e = ex;
                while (e != null) { msgs.Add(e.Message); e = e.InnerException; }
                return Json(new { ok = false, cadena = msgs }, JsonRequestBehavior.AllowGet);
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

        // TEMPORAL — diagnostica por qué la planta sale vacía en el header. Borrar después.
        [HttpGet]
        public JsonResult TestPlanta()
        {
            var info = new System.Collections.Generic.Dictionary<string, object>();
            string login = User.Identity.Name;
            info["login"] = login;

            long uid = 0;
            try { uid = CustomHelper.getUserId(); info["usuarioId"] = uid; }
            catch (Exception ex) { info["usuarioId_error"] = ex.Message; }

            // Ruta A: la que usa el header HOY (por login → RT_USUARIOS.PLANTA, APK66)
            try { info["plantaPorLogin"] = _bll.ObtenerPlantaPorLogin(login); }
            catch (Exception ex) { info["plantaPorLogin_error"] = Cadena(ex); }

            // Ruta B: la que usa el GUARDADO (por usuarioId → RecibosCaja_UsuarioDepto, POS)
            try { info["deptoSerie"] = _bll.ObtenerDeptoSerie(uid); }
            catch (Exception ex) { info["deptoSerie_error"] = Cadena(ex); }

            return Json(new { ok = true, info }, JsonRequestBehavior.AllowGet);
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