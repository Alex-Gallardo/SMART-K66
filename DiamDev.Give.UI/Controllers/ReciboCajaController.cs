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

            // Resolvemos planta sin reventar: si el usuario no está vinculado
            // en APK66 (ej: 'admin'), mostramos vacío y el guardado lo validará.
            string planta = "";
            try { planta = _bll.ObtenerPlantaPorLogin(login); }
            catch { planta = ""; }  // no vinculado → la vista lo muestra vacío

            var model = new ReciboCajaIndexViewModel
            {
                UsuarioActual = login,
                PlantaUsuario = planta
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
                string depto = _bll.ObtenerPlantaPorLogin(login);   // lanza error claro si no hay vínculo
                string usuario = _bll.ObtenerIdUsrPorLogin(login);  // ID_USR canónico de APK66 (mayúsculas)

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

                var resultado = _bll.GuardarRecibo(enc, depto);
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
        // Abre vista de impresión en nueva pestaña
        // ─────────────────────────────────────────────
        /// [Permiso("Control.ReciboCaja.Ver")]
        public ActionResult Imprimir(string idRecibo, string empresa)
        {
            var rec = _bll.BuscarRecibo(idRecibo, empresa);
            if (rec == null) return HttpNotFound("Recibo no encontrado.");
            return View(rec);
        }
    }
}