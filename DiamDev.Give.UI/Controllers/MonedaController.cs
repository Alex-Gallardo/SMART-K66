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
    public class MonedaController : Controller
    {
        // GET: Moneda
        [Permiso("Control.Moneda.Ver_Listado")]
        public ActionResult Index(int? page)
        {
            CustomHelper.setTitle("Moneda", "Listado");

            List<Moneda> Monedas = new List<Moneda>();

            try
            {
                Monedas = new MonedaBL().ObtenerListado();
            }
            catch (Exception ex)
            {
                ViewBag.Error = string.Format("Message: {0} StackTrace: {1}", ex.Message, ex.StackTrace);
                return View("~/Views/Shared/Error.cshtml");
            }

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View(Monedas.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Moneda.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Moneda", "Nueva");      

            return View();
        }

        [HttpPost]
        [Permiso("Control.Moneda.Crear")]
        public ActionResult Crear(Moneda modelo)
        {
            if (ModelState.IsValid)
            {              
                string strMensaje = new MonedaBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Moneda-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }         

            return View(modelo);
        }

        [Permiso("Control.Moneda.Editar")]
        public ActionResult Editar(long id)
        {
            Moneda MonedaActual = new MonedaBL().ObtenerPorId(id);

            if (MonedaActual == null || MonedaActual.MonedaId == 0)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Moneda", "Editar");          

            return View(MonedaActual);
        }

        [HttpPost]
        [Permiso("Control.Moneda.Editar")]
        public ActionResult Editar(Moneda modelo)
        {
            if (ModelState.IsValid)
            {                
                string strMensaje = new MonedaBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Moneda-Success"] = strMensaje;
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