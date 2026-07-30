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
    public class ConfiguracionController : Controller
    {
        #region Metodos Privados

            private void CargaControles()
            {
                var Padres = new ConfiguracionBL().ObtenerListado(true, false, true);

                ViewBag.Padres = new SelectList(Padres, "ConfiguracionId", "Nombre");
            }

        #endregion

        // GET: Configuracion
        [Permiso("Control.Configuracion.Ver_Listado")]
        public ActionResult Index(int? page, string search)
        {
            CustomHelper.setTitle("Configuracion", "Listado");

            List<Configuracion> Configuracions = new List<Configuracion>();

            try
            {
                if (!string.IsNullOrWhiteSpace(search) && search != null)
                {
                    Configuracions = new ConfiguracionBL().Buscar(search).ToList();
                }
                else
                {
                    Configuracions = new ConfiguracionBL().ObtenerListado().ToList();
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
            return View(Configuracions.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Configuracion.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Configuracion", "Nueva");

            this.CargaControles();
            return View();
        }

        [Permiso("Control.Configuracion.Crear")]
        [HttpPost]
        public ActionResult Crear(Configuracion modelo)
        {
            if (ModelState.IsValid)
            {
                string strMensaje = new ConfiguracionBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Configuracion-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }

            }

            this.CargaControles();
            return View(modelo);
        }

        [Permiso("Control.Configuracion.Editar")]
        public ActionResult Editar(long id)
        {
            Configuracion ConfiguracionActual = new ConfiguracionBL().ObtenerPorId(id);

            if (ConfiguracionActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Configuracion", "Editar");

            this.CargaControles();
            return View(ConfiguracionActual);
        }

        [Permiso("Control.Configuracion.Editar")]
        [HttpPost]
        public ActionResult Editar(Configuracion modelo)
        {
            if (ModelState.IsValid)
            {
                string strMensaje = new ConfiguracionBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Configuracion-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }

            }

            this.CargaControles();
            return View(modelo);
        }

        [ActionName("ConfiguracionPorcentajeTarjeta")]
        public JsonResult ConfiguracionPorcentajeTarjeta()
        {
            return Json(new { Operacion = true, Data = new ConfiguracionBL().ObtenerConfiguracionPorcentajeTarjeta() }, JsonRequestBehavior.AllowGet);
        }
    }
}