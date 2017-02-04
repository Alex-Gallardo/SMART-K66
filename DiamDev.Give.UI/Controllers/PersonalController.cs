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
    public class PersonalController : Controller
    {
        #region Metodos Privados
             
        #endregion
        
        // GET: Personal
        [Permiso("Control.Personal.Ver_Listado")]
        public ActionResult Index(int? page, string search)
        {
            CustomHelper.setTitle("Personal", "Listado");

            List<Personal> Personals = new List<Personal>();

            try
            {
                if (!string.IsNullOrWhiteSpace(search) && search != null)
                {
                    Personals = new PersonalBL().Buscar(search).ToList();
                }
                else
                {
                    Personals = new PersonalBL().ObtenerListado(false).ToList();
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
            return View(Personals.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Personal.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Personal", "Nuevo");

            string strAtributo = "checked='checked'";

            ViewBag.ActivoSi = strAtributo;
            ViewBag.ActivoNo = "";
                       
            return View();
        }

        [Permiso("Control.Personal.Crear")]
        [HttpPost]
        public ActionResult Crear(Personal modelo, bool activo)
        {
            if (ModelState.IsValid)
            {
                modelo.Activo = activo;
                string strMensaje = new PersonalBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Personal-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }

            }

            string strAtributo = "checked='checked'";

            ViewBag.ActivoSi = activo == true ? strAtributo : "";
            ViewBag.ActivoNo = activo == false ? strAtributo : "";
                        
            return View(modelo);
        }

        [Permiso("Control.Personal.Editar")]
        public ActionResult Editar(long id)
        {
            Personal PersonalActual = new PersonalBL().ObtenerPorId(id, false);

            if (PersonalActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Personal", "Editar");

            string strAtributo = "checked='checked'";

            ViewBag.ActivoSi = PersonalActual.Activo == true ? strAtributo : "";
            ViewBag.ActivoNo = PersonalActual.Activo == false ? strAtributo : "";
                        
            return View(PersonalActual);
        }

        [Permiso("Control.Personal.Editar")]
        [HttpPost]
        public ActionResult Editar(Personal modelo, bool activo)
        {
            if (ModelState.IsValid)
            {
                modelo.Activo = activo;
                string strMensaje = new PersonalBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Personal-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }

            }

            string strAtributo = "checked='checked'";

            ViewBag.ActivoSi = activo == true ? strAtributo : "";
            ViewBag.ActivoNo = activo == false ? strAtributo : "";
                        
            return View(modelo);
        }

        [Permiso("Control.Personal.Detalle")]
        public ActionResult Detalle(long id)
        {
            Personal PersonalActual = new PersonalBL().ObtenerPorId(id, true);

            if (PersonalActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Personal", "Detalle");

            return View(PersonalActual);
        }
    }
}