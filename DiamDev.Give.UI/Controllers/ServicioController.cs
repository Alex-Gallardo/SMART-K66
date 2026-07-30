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
    public class ServicioController : Controller
    {
        // GET: Servicio
        [Permiso("Control.Servicio.Ver_Listado")]
        public ActionResult Index(int? page)
        {
            CustomHelper.setTitle("Servicio", "Listado");

            List<Servicio> Servicios = new List<Servicio>();

            try
            {
                Servicios = new ServicioBL().ObtenerListado(true);
            }
            catch (Exception ex)
            {
                ViewBag.Error = string.Format("Message: {0} StackTrace: {1}", ex.Message, ex.StackTrace);
                return View("~/Views/Shared/Error.cshtml");
            }

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View(Servicios.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Servicio.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Servicio", "Nuevo");

            string strAtributo = "checked='checked'";

            ViewBag.ServicioSi = strAtributo;
            ViewBag.ServicioNo = "";

            return View();
        }

        [HttpPost]
        [Permiso("Control.Servicio.Crear")]
        public ActionResult Crear(Servicio modelo, bool servicio)
        {

            if (ModelState.IsValid)
            {

                modelo.Activo = servicio;
                string strMensaje = new ServicioBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Servicio-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }

            }

            string strAtributo = "checked='checked'";

            ViewBag.ServicioSi = servicio == true ? strAtributo : "";
            ViewBag.ServicioNo = servicio == false ? strAtributo : "";

            return View(modelo);
        }

        [Permiso("Control.Servicio.Editar")]
        public ActionResult Editar(long id)
        {
            Servicio ServicioActual = new ServicioBL().ObtenerPorId(id);

            if (ServicioActual == null || ServicioActual.ServicioId == 0)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Servicio", "Editar");

            string strAtributo = "checked='checked'";

            ViewBag.ServicioSi = ServicioActual.Activo == true ? strAtributo : "";
            ViewBag.ServicioNo = ServicioActual.Activo == false ? strAtributo : "";

            return View(ServicioActual);
        }

        [HttpPost]
        [Permiso("Control.Servicio.Editar")]
        public ActionResult Editar(Servicio modelo, bool servicio)
        {

            if (ModelState.IsValid)
            {
                modelo.Activo = servicio;
                string strMensaje = new ServicioBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Servicio-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }

            }

            string strAtributo = "checked='checked'";

            ViewBag.ServicioSi = servicio == true ? strAtributo : "";
            ViewBag.ServicioNo = servicio == false ? strAtributo : "";

            return View(modelo);
        }
    }
}