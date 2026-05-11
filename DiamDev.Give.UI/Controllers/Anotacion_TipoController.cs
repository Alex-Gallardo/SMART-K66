using System;
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
    public class Anotacion_TipoController : Controller
    {
        // GET: Anotacion_Tipo
        [Permiso("Control.Anotacion_Tipo.Ver_Listado")]
        public ActionResult Index(int? page, string search)
        {
            CustomHelper.setTitle("Tipo de Anotacion", "Listado");

            List<AnotacionTipo> AnotacionTipos = new List<AnotacionTipo>();

            try
            {
                if (!string.IsNullOrWhiteSpace(search) && search != null)
                {
                    AnotacionTipos = new AnotacionTipoBL().Buscar(search).ToList();
                }
                else
                {
                    AnotacionTipos = new AnotacionTipoBL().ObtenerListado().ToList();
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = string.Format("Message: {0} StackTrace: {1}", ex.Message, ex.StackTrace);
                return View("~/Views/Shared/Error.cshtml");
            }

            ViewBag.Search = search;

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View(AnotacionTipos.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Anotacion_Tipo.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Tipo de Anotacion", "Nuevo");

            string strAtributo = "checked='checked'";

            ViewBag.DescuentoSi = "";
            ViewBag.DescuentoNo = strAtributo;

            return View();
        }

        [Permiso("Control.Anotacion_Tipo.Crear")]
        [HttpPost]
        public ActionResult Crear(AnotacionTipo modelo, bool descuento)
        {
            if (ModelState.IsValid)
            {
                modelo.Descuento = descuento;
                string strMensaje = new AnotacionTipoBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Anotacion_Tipo-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }

            }

            string strAtributo = "checked='checked'";

            ViewBag.DescuentoSi = descuento == true ? strAtributo : "";
            ViewBag.DescuentoNo = descuento == false ? strAtributo : "";

            return View(modelo);
        }

        [Permiso("Control.Anotacion_Tipo.Editar")]
        public ActionResult Editar(long id)
        {
            AnotacionTipo AnotacionTipoActual = new AnotacionTipoBL().ObtenerPorId(id);

            if (AnotacionTipoActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Tipo de Anotacion", "Editar");

            string strAtributo = "checked='checked'";

            ViewBag.DescuentoSi = AnotacionTipoActual.Descuento == true ? strAtributo : "";
            ViewBag.DescuentoNo = AnotacionTipoActual.Descuento == false ? strAtributo : "";

            return View(AnotacionTipoActual);
        }

        [Permiso("Control.Anotacion_Tipo.Editar")]
        [HttpPost]
        public ActionResult Editar(AnotacionTipo modelo, bool descuento)
        {
            if (ModelState.IsValid)
            {
                string strMensaje = new AnotacionTipoBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Anotacion_Tipo-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }

            string strAtributo = "checked='checked'";

            ViewBag.DescuentoSi = descuento == true ? strAtributo : "";
            ViewBag.DescuentoNo = descuento == false ? strAtributo : "";

            return View(modelo);
        }
    }
}