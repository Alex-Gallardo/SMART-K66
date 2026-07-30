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

            private void CargaControles()
            {
                var Puestos = new PuestoBL().ObtenerListado();
                var Bancos = new BancoBL().ObtenerListado();

                ViewBag.Puestos = new SelectList(Puestos, "PuestoId", "Nombre");
                ViewBag.Bancos = new SelectList(Bancos, "BancoId", "Nombre");
            }

            private void CargaTipos()
            {
                var Tipos = new AnotacionTipoBL().ObtenerListado();

                ViewBag.Tipos = new SelectList(Tipos, "TipoId", "Nombre");
                ViewBag.Catalogos = Tipos;
            }

            private void Anotaciones()
            {
                var Tipos = new AnotacionTipoBL().ObtenerListado();

                ViewBag.Catalogos = Tipos;
            }

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

            ViewBag.DescuentoSi = "";
            ViewBag.DescuentoNo = strAtributo;

            ViewBag.ActivoSi = strAtributo;
            ViewBag.ActivoNo = "";

            this.CargaControles();
            return View();
        }

        [Permiso("Control.Personal.Crear")]
        [HttpPost]
        public ActionResult Crear(Personal modelo, bool descuento, bool activo)
        {
            if (ModelState.IsValid)
            {
                modelo.IGSS = descuento;
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

            ViewBag.DescuentoSi = descuento == true ? strAtributo : "";
            ViewBag.DescuentoNo = descuento == false ? strAtributo : "";

            ViewBag.ActivoSi = activo == true ? strAtributo : "";
            ViewBag.ActivoNo = activo == false ? strAtributo : "";

            this.CargaControles();
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

            ViewBag.DescuentoSi = PersonalActual.IGSS == true ? strAtributo : "";
            ViewBag.DescuentoNo = PersonalActual.IGSS == false ? strAtributo : "";

            ViewBag.ActivoSi = PersonalActual.Activo == true ? strAtributo : "";
            ViewBag.ActivoNo = PersonalActual.Activo == false ? strAtributo : "";

            this.CargaControles();
            return View(PersonalActual);
        }

        [Permiso("Control.Personal.Editar")]
        [HttpPost]
        public ActionResult Editar(Personal modelo, bool descuento, bool activo)
        {
            if (ModelState.IsValid)
            {
                modelo.IGSS = descuento;
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

            ViewBag.DescuentoSi = descuento == true ? strAtributo : "";
            ViewBag.DescuentoNo = descuento == false ? strAtributo : "";

            ViewBag.ActivoSi = activo == true ? strAtributo : "";
            ViewBag.ActivoNo = activo == false ? strAtributo : "";

            this.CargaControles();
            return View(modelo);
        }

        [Permiso("Control.Personal.Anotacion")]
        public ActionResult Anotacion(long id)
        {
            Personal PersonalActual = new PersonalBL().ObtenerPorId(id, true);

            if (PersonalActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Personal", "Anotacion");

            this.CargaTipos();
            return View(PersonalActual);
        }

        [Permiso("Control.Personal.Anotacion")]
        [HttpPost]
        [ActionName("NuevaAnotacion")]
        public ActionResult Anotacion(Anotacion modelo)
        {
            if (ModelState.IsValid)
            {
                string strMensaje = new AnotacionBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    return Json(true, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(false, JsonRequestBehavior.AllowGet);
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

            this.Anotaciones();
            return View(PersonalActual);
        }

        [Permiso("Control.Personal.Detalle")]
        public ActionResult Horario(int? page, string search)
        {
            CustomHelper.setTitle("Personal", "Horario");

            List<PersonalHorario> Personals = new List<PersonalHorario>();

            try
            {
                if (!string.IsNullOrWhiteSpace(search) && search != null)
                {
                    Personals = new PersonalHorarioBL().Buscar(search).ToList();
                }
                else
                {
                    Personals = new PersonalHorarioBL().ObtenerListado(false).ToList();
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
    }
}