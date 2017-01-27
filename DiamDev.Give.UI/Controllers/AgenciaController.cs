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
    public class AgenciaController : Controller
    {
        // GET: Agencia
        [Permiso("Control.Agencia.Ver_Listado")]
        public ActionResult Index(int? page)
        {
            CustomHelper.setTitle("Agencia", "Listado");

            List<Agencia> Agencias = new List<Agencia>();

            try
            {
                Agencias = new AgenciaBL().ObtenerListado(true);
            }
            catch (Exception ex)
            {
                ViewBag.Error = string.Format("Message: {0} StackTrace: {1}", ex.Message, ex.StackTrace);
                return View("~/Views/Shared/Error.cshtml");
            }

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View(Agencias.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Agencia.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Agencia", "Nueva");

            string strAtributo = "checked='checked'";

            ViewBag.activoSi = strAtributo;
            ViewBag.activoNo = "";

            return View();
        }

        [HttpPost]
        [Permiso("Control.Agencia.Crear")]
        public ActionResult Crear(Agencia modelo, bool activo)
        {

            if (ModelState.IsValid)
            {
                modelo.Activo = activo;
                string strMensaje = new AgenciaBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Agencia-Success"] = strMensaje;
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

        [Permiso("Control.Agencia.Editar")]
        public ActionResult Editar(long id)
        {
            Agencia AgenciaActual = new AgenciaBL().ObtenerPorId(id);

            if (AgenciaActual == null || AgenciaActual.AgenciaId == 0)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Agencia", "Editar");

            string strAtributo = "checked='checked'";

            ViewBag.activoSi = AgenciaActual.Activo == true ? strAtributo : "";
            ViewBag.activoNo = AgenciaActual.Activo == false ? strAtributo : "";

            return View(AgenciaActual);
        }

        [HttpPost]
        [Permiso("Control.Agencia.Editar")]
        public ActionResult Editar(Agencia modelo, bool activo)
        {

            if (ModelState.IsValid)
            {
                modelo.Activo = activo;
                string strMensaje = new AgenciaBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Agencia-Success"] = strMensaje;
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
    }
}