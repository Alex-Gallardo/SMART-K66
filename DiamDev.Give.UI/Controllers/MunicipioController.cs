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
    public class MunicipioController : Controller
    {
        [Permiso("Control.Municipio.Listado")]
        public ActionResult Index(int? page)
        {
            CustomHelper.setTitle("Municipio", "Listado");

            List<Municipio> Agencias = new List<Municipio>();

            try
            {
                Agencias = new MunicipioBL().ObtenerListado(true);
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

        [Permiso("Control.Municipio.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Municipio", "Nuevo");

            string strAtributo = "checked='checked'";

            ViewBag.activoSi = strAtributo;
            ViewBag.activoNo = "";

            return View();
        }

        [HttpPost]
        [Permiso("Control.Municipio.Crear")]
        public ActionResult Crear(Municipio modelo, bool activo)
        {

            if (ModelState.IsValid)
            {
                                string strMensaje = new MunicipioBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Municipio-Success"] = strMensaje;
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

        [Permiso("Control.Municipio.Editar")]
        public ActionResult Editar(long id)
        {
            Municipio AgenciaActual = new MunicipioBL().ObtenerPorId(id);

            if (AgenciaActual == null || AgenciaActual.MunicipioId == 0)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Municipio", "Editar");

            string strAtributo = "checked='checked'";

            ViewBag.activoSi = AgenciaActual.Activo == true ? strAtributo : "";
            ViewBag.activoNo = AgenciaActual.Activo == false ? strAtributo : "";

            return View(AgenciaActual);
        }

        [HttpPost]
        [Permiso("Control.Municipio.Editar")]
        public ActionResult Editar(Municipio modelo, bool activo)
        {

            if (ModelState.IsValid)
            {
                modelo.Activo = activo;
                string strMensaje = new MunicipioBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Municipio-Success"] = strMensaje;
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