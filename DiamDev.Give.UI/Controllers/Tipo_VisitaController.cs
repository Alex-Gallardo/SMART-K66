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
    public class Tipo_VisitaController : Controller
    {
        // GET: Tipo_Visita
        [Permiso("Control.Tipo_Visita.Ver_Listado")]
        public ActionResult Index(int? page)
        {
            CustomHelper.setTitle("Tipo de Visita", "Listado");

            List<VisitaTipo> Tipos = new List<VisitaTipo>();

            try
            {
                Tipos = new VisitaTipoBL().ObtenerListado(true);
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

        [Permiso("Control.Tipo_Visita.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Tipo de Visita", "Nuevo");

            string strAtributo = "checked='checked'";           

            ViewBag.activoSi = strAtributo;
            ViewBag.activoNo = "";
            
            return View();
        }

        [HttpPost]
        [Permiso("Control.Tipo_Visita.Crear")]
        public ActionResult Crear(VisitaTipo modelo, bool activo)
        {
            if (ModelState.IsValid)
            {                
                modelo.Activo = activo;
                string strMensaje = new VisitaTipoBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Visita_Tipo-Success"] = strMensaje;
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

        [Permiso("Control.Tipo_Visita.Editar")]
        public ActionResult Editar(long id)
        {
            VisitaTipo VisitaTipoActual = new VisitaTipoBL().ObtenerPorId(id);

            if (VisitaTipoActual == null || VisitaTipoActual.TipoId == 0)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Tipo de Visita", "Editar");

            string strAtributo = "checked='checked'";         

            ViewBag.activoSi = VisitaTipoActual.Activo == true ? strAtributo : "";
            ViewBag.activoNo = VisitaTipoActual.Activo == false ? strAtributo : "";
            
            return View(VisitaTipoActual);
        }

        [HttpPost]
        [Permiso("Control.Tipo_Visita.Editar")]
        public ActionResult Editar(VisitaTipo modelo, bool activo)
        {
            if (ModelState.IsValid)
            {                
                modelo.Activo = activo;
                string strMensaje = new VisitaTipoBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Visita_Tipo-Success"] = strMensaje;
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