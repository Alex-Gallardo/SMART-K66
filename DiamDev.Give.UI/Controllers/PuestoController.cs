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
    public class PuestoController : Controller
    {
        // GET: Puesto
        [Permiso("Control.Puesto.Ver_Listado")]
        public ActionResult Index(int? page, string search)
        {
            CustomHelper.setTitle("Puesto", "Listado");

            List<Puesto> Puestos = new List<Puesto>();

            try
            {
                if (!string.IsNullOrWhiteSpace(search) && search != null)
                {
                    Puestos = new PuestoBL().Buscar(search).ToList();
                }
                else
                {
                    Puestos = new PuestoBL().ObtenerListado().ToList();
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
            return View(Puestos.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Puesto.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Puesto", "Nuevo");

            return View();
        }

        [Permiso("Control.Puesto.Crear")]
        [HttpPost]
        public ActionResult Crear(Puesto modelo)
        {
            if (ModelState.IsValid)
            {
                string strMensaje = new PuestoBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Puesto-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }

            }

            return View(modelo);
        }

        [Permiso("Control.Puesto.Editar")]
        public ActionResult Editar(long id)
        {
            Puesto PuestoActual = new PuestoBL().ObtenerPorId(id);

            if (PuestoActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Puesto", "Editar");

            return View(PuestoActual);
        }

        [Permiso("Control.Puesto.Editar")]
        [HttpPost]
        public ActionResult Editar(Puesto modelo)
        {
            if (ModelState.IsValid)
            {
                string strMensaje = new PuestoBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Puesto-Success"] = strMensaje;
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