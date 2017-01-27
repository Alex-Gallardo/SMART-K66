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
    public class MarcaController : Controller
    {
        // GET: /Marca/
        [Permiso("Control.Marca.Ver_Listado")]
        public ActionResult Index(int? page)
        {
            CustomHelper.setTitle("Marca", "Listado");

            List<Marca> Marcas = new List<Marca>();

            try
            {
                Marcas = new MarcaBL().ObtenerListado(true);
            }
            catch (Exception ex)
            {
                ViewBag.Error = string.Format("Message: {0} StackTrace: {1}", ex.Message, ex.StackTrace);
                return View("~/Views/Shared/Error.cshtml");
            }

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View(Marcas.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Marca.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Marca", "Nueva");

            string strAtributo = "checked='checked'";

            ViewBag.activoSi = strAtributo;
            ViewBag.activoNo = "";

            return View();
        }

        [HttpPost]
        [Permiso("Control.Marca.Crear")]
        public ActionResult Crear(Marca modelo, bool activo)
        {

            if (ModelState.IsValid)
            {
                modelo.Activo = activo;
                string strMensaje = new MarcaBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Marca-Success"] = strMensaje;
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

        [Permiso("Control.Marca.Editar")]
        public ActionResult Editar(long id)
        {
            Marca MarcaActual = new MarcaBL().ObtenerPorId(id);

            if (MarcaActual == null || MarcaActual.MarcaId == 0)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Marca", "Editar");

            string strAtributo = "checked='checked'";

            ViewBag.activoSi = MarcaActual.Activo == true ? strAtributo : "";
            ViewBag.activoNo = MarcaActual.Activo == false ? strAtributo : "";

            return View(MarcaActual);
        }

        [HttpPost]
        [Permiso("Control.Marca.Editar")]
        public ActionResult Editar(Marca modelo, bool activo)
        {

            if (ModelState.IsValid)
            {
                modelo.Activo = activo;
                string strMensaje = new MarcaBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Marca-Success"] = strMensaje;
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