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
    public class BancoController : Controller
    {
        // GET: Banco
        [Permiso("Control.Banco.Ver_Listado")]
        public ActionResult Index(int? page)
        {
            CustomHelper.setTitle("Banco", "Listado");

            List<Banco> Bancos = new List<Banco>();

            try
            {
                Bancos = new BancoBL().ObtenerListado();
            }
            catch (Exception ex)
            {
                ViewBag.Error = string.Format("Message: {0} StackTrace: {1}", ex.Message, ex.StackTrace);
                return View("~/Views/Shared/Error.cshtml");
            }

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View(Bancos.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Banco.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Banco", "Nuevo");
        
            return View();
        }

        [HttpPost]
        [Permiso("Control.Banco.Crear")]
        public ActionResult Crear(Banco modelo)
        {
            if (ModelState.IsValid)
            {
                string strMensaje = new BancoBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Banco-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }
            
            return View(modelo);
        }

        [Permiso("Control.Banco.Editar")]
        public ActionResult Editar(long id)
        {
            Banco BancoActual = new BancoBL().ObtenerPorId(id);

            if (BancoActual == null || BancoActual.BancoId == 0)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Banco", "Editar");
                    
            return View(BancoActual);
        }

        [HttpPost]
        [Permiso("Control.Banco.Editar")]
        public ActionResult Editar(Banco modelo)
        {
            if (ModelState.IsValid)
            {
                string strMensaje = new BancoBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Banco-Success"] = strMensaje;
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