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
using System.Collections;

namespace DiamDev.Give.UI.Controllers
{
    [Authorize]
    [Seguridad]
    [HandleError]
    public class PoliticaController : Controller
    {
        #region Metodos Privados

            private void CargaControles()
            {
                var Tipos = new PoliticaTipoBL().ObtenerListado();

                ViewBag.Tipos = new SelectList(Tipos, "PoliticaTipoId", "Nombre");
            }

        #endregion

        // GET: Politica
        [Permiso("Control.Politica.Ver_Listado")]
        public ActionResult Index(int? page, string search)
        {
            CustomHelper.setTitle("Política", "Listado");

            List<Politica> Politicas = new List<Politica>();

            try
            {
                if (!string.IsNullOrWhiteSpace(search) && search != null)
                {
                    Politicas = new PoliticaBL().Buscar(search).ToList();
                }
                else
                {
                    Politicas = new PoliticaBL().ObtenerListado().ToList();
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
            return View(Politicas.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Politica.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Política", "Nueva");

            string strAtributo = "checked='checked'";

            ViewBag.ActivoSi = strAtributo;
            ViewBag.ActivoNo = "";

            this.CargaControles();
            return View();
        }

        [Permiso("Control.Politica.Crear")]
        [HttpPost]
        public ActionResult Crear(Politica modelo, bool activo)
        {
            if (ModelState.IsValid)
            {
                modelo.Activo = activo;
                string strMensaje = new PoliticaBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Politica-Success"] = strMensaje;
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

            this.CargaControles();
            return View(modelo);
        }

        [Permiso("Control.Politica.Editar")]
        public ActionResult Editar(long id)
        {
            Politica PoliticaActual = new PoliticaBL().ObtenerPorId(id);

            if (PoliticaActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Política", "Editar");

            string strAtributo = "checked='checked'";

            ViewBag.ActivoSi = PoliticaActual.Activo == true ? strAtributo : "";
            ViewBag.ActivoNo = PoliticaActual.Activo == false ? strAtributo : "";

            this.CargaControles();
            return View(PoliticaActual);
        }

        [Permiso("Control.Politica.Editar")]
        [HttpPost]
        public ActionResult Editar(Politica modelo, bool activo)
        {
            if (ModelState.IsValid)
            {
                modelo.Activo = activo;
                string strMensaje = new PoliticaBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Politica-Success"] = strMensaje;
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

            this.CargaControles();
            return View(modelo);
        }

        [ActionName("ObtenerPoliticas")]
        public JsonResult PoliticasListado(int tipoId)
        {
            IList _result = new List<SelectListItem>();
            _result = new PoliticaBL().ObtenerPoliticasxTipoId(tipoId).Select(m => new SelectListItem() { Text = m.Nombre, Value = m.PoliticaId.ToString() }).ToList();
            return Json(_result, JsonRequestBehavior.AllowGet);
        }
    }
}