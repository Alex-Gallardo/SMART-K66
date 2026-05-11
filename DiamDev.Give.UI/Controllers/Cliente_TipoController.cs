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
    public class Cliente_TipoController : Controller
    {
        // GET: Cliente_Tipo
        [Permiso("Control.Cliente_Tipo.Ver_Listado")]
        public ActionResult Index(int? page)
        {
            CustomHelper.setTitle("Tipo de Cliente", "Listado");

            List<ClienteTipo> Tipos = new List<ClienteTipo>();

            try
            {
                Tipos = new ClienteTipoBL().ObtenerListado();
            }
            catch (Exception ex)
            {
                ViewBag.Error = string.Format("Message: {0} StackTrace: {1}", ex.Message, ex.StackTrace);
                return View("~/Views/Shared/Error.cshtml");
            }

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View(Tipos.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Cliente_Tipo.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Tipo de Cliente", "Nuevo");
                      
            return View();
        }

        [HttpPost]
        [Permiso("Control.Cliente_Tipo.Crear")]
        public ActionResult Crear(ClienteTipo modelo)
        {

            if (ModelState.IsValid)
            {               
                string strMensaje = new ClienteTipoBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Tipo-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }          

            return View(modelo);
        }

        [Permiso("Control.Cliente_Tipo.Editar")]
        public ActionResult Editar(long id)
        {
            ClienteTipo ClienteTipoActual = new ClienteTipoBL().ObtenerPorId(id);

            if (ClienteTipoActual == null || ClienteTipoActual.TipoId == 0)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Tipo de Cliente", "Editar");
           
            return View(ClienteTipoActual);
        }

        [HttpPost]
        [Permiso("Control.Cliente_Tipo.Editar")]
        public ActionResult Editar(ClienteTipo modelo)
        {
            if (ModelState.IsValid)
            {                
                string strMensaje = new ClienteTipoBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Tipo-Success"] = strMensaje;
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