using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using DiamDev.Give.BLL;
using DiamDev.Give.Entities;
using DiamDev.Give.UI.App_Start;
using DiamDev.Give.UI.Models;
using PagedList;

namespace DiamDev.Give.UI.Controllers
{
    [Authorize]
    [Seguridad]
    [HandleError]
    public class UnidadController : Controller
    {        
        // GET: /Unidad/
        [Permiso("Control.Unidad.Ver_Listado")]
        public ActionResult Index(int? page)
        {
            CustomHelper.setTitle("Unidad de Medida", "Listado");

            List<Unidad> Unidades = new List<Unidad>();

            try
            {
                Unidades = new UnidadBL().ObtenerListado(true);
            }
            catch (Exception ex)
            {
                ViewBag.Error = string.Format("Message: {0} StackTrace: {1}", ex.Message, ex.StackTrace);
                return View("~/Views/Shared/Error.cshtml");
            }

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View(Unidades.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Unidad.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Unidad de Medida", "Nueva");

            string strAtributo = "checked='checked'";

            ViewBag.activoSi = strAtributo;
            ViewBag.activoNo = "";

            return View();
        }

        [HttpPost]
        [Permiso("Control.Unidad.Crear")]
        public ActionResult Crear(Unidad modelo, bool activo)
        {
            modelo.Cantidad = 0;
            if (ModelState.IsValid)
            {
                modelo.Activo = activo;
                string strMensaje = new UnidadBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Unidad-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }

            string strAtributo = "checked='checked'";

            ViewBag.activoSi = activo == true ? strAtributo : "";
            ViewBag.activoNo = activo == false ? strAtributo : "";

            return View(modelo);
        }

        [Permiso("Control.Unidad.Editar")]
        public ActionResult Editar(long id)
        {
            Unidad UnidadActual = new UnidadBL().ObtenerPorId(id);

            if (UnidadActual == null || UnidadActual.UnidadId == 0)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Unidad de Medida", "Editar");

            string strAtributo = "checked='checked'";

            ViewBag.activoSi = UnidadActual.Activo == true ? strAtributo : "";
            ViewBag.activoNo = UnidadActual.Activo == false ? strAtributo : "";

            return View(UnidadActual);
        }

        [HttpPost]
        [Permiso("Control.Unidad.Editar")]
        public ActionResult Editar(Unidad modelo, bool activo)
        {
            modelo.Cantidad = 0;
            if (ModelState.IsValid)
            {
                modelo.Activo = activo;
                string strMensaje = new UnidadBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Unidad-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }

            string strAtributo = "checked='checked'";

            ViewBag.activoSi = activo == true ? strAtributo : "";
            ViewBag.activoNo = activo == false ? strAtributo : "";

            return View(modelo);
        }

        [ActionName("ObtenerUnidadxConversion")]
        public JsonResult ObtenerUnidadxConversion(string id, long empresaId)
        {
            IList _result = new List<SelectListItem>();
            _result = new UnidadBL().ObtenerUnidadxConversion(id, CustomHelper.getUserId(), empresaId).Select(m => new SelectListItem() { Text = m.Unidad, Value = m.UnidadId.ToString() }).ToList();
            return Json(_result, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerUnidadxId")]
        public JsonResult ObtenerUnidadxId(int id, long empresaId)
        {
            if (id > 0)
            {
                UnidadK66 UnidadK66Actual = new UnidadBL().ObtenerUnidadxID(id, CustomHelper.getUserId(), empresaId);
                if (UnidadK66Actual != null)
                {
                    return Json(new { Operacion = true, Data = UnidadK66Actual }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }
    }
}