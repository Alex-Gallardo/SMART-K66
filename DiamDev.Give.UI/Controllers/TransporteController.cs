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
    public class TransporteController : Controller
    {
        // GET: Transporte
        [Permiso("Control.Transporte.Ver_Listado")]
        public ActionResult Index(int? page, string search)
        {
            CustomHelper.setTitle("Transporte", "Listado");

            List<Transporte> Transportes = new List<Transporte>();

            try
            {
                if (!string.IsNullOrWhiteSpace(search) && search != null)
                {
                    Transportes = new TransporteBL().Buscar(search).ToList();
                }
                else
                {
                    Transportes = new TransporteBL().ObtenerListado().ToList();
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
            return View(Transportes.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Transporte.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Transporte", "Nuevo");

            return View();
        }

        [Permiso("Control.Transporte.Crear")]
        [HttpPost]
        public ActionResult Crear(Transporte modelo)
        {
            if (ModelState.IsValid)
            {
                string strMensaje = new TransporteBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Transporte-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }

            return View(modelo);
        }

        [Permiso("Control.Transporte.Editar")]
        public ActionResult Editar(long id)
        {
            Transporte TransporteActual = new TransporteBL().ObtenerPorId(id);

            if (TransporteActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Transporte", "Editar");
       
            return View(TransporteActual);
        }

        [Permiso("Control.Transporte.Editar")]
        [HttpPost]
        public ActionResult Editar(Transporte modelo)
        {
            if (ModelState.IsValid)
            {
                string strMensaje = new TransporteBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Transporte-Success"] = strMensaje;
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