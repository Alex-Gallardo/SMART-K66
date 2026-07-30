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
    public class RegionController : Controller
    {
        // GET: Region
        [Permiso("Control.Region.Ver_Listado")]
        public ActionResult Index(int? page)
        {
            CustomHelper.setTitle("Region", "Listado");

            List<Region> Regiones = new List<Region>();

            try
            {
                Regiones = new RegionBL().ObtenerListado();
            }
            catch (Exception ex)
            {
                ViewBag.Error = string.Format("Message: {0} StackTrace: {1}", ex.Message, ex.StackTrace);
                return View("~/Views/Shared/Error.cshtml");
            }

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View(Regiones.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Region.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Region", "Nueva");       
            return View();
        }

        [HttpPost]
        [Permiso("Control.Region.Crear")]
        public ActionResult Crear(Region modelo)
        {
            if (ModelState.IsValid)
            {
                string strMensaje = new RegionBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Region-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }
          
            return View(modelo);
        }

        [Permiso("Control.Region.Editar")]
        public ActionResult Editar(long id)
        {
            Region RegionActual = new RegionBL().ObtenerPorId(id);

            if (RegionActual == null || RegionActual.RegionId == 0)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Region", "Editar");

            return View(RegionActual);
        }

        [HttpPost]
        [Permiso("Control.Region.Editar")]
        public ActionResult Editar(Region modelo)
        {
            if (ModelState.IsValid)
            {
                string strMensaje = new RegionBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Region-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }
          
            return View(modelo);
        }
    }
}