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
    public class DepartamentoController : Controller
    {
        // GET: Departamento
        [Permiso("Control.Departamento.Ver_Listado")]
        public ActionResult Index(int? page)
        {
            CustomHelper.setTitle("Departamento", "Listado");

            List<Departamento> Departamentos = new List<Departamento>();

            try
            {
                Departamentos = new DepartamentoBL().ObtenerListado(true);
            }
            catch (Exception ex)
            {
                ViewBag.Error = string.Format("Message: {0} StackTrace: {1}", ex.Message, ex.StackTrace);
                return View("~/Views/Shared/Error.cshtml");
            }

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View(Departamentos.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Departamento.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Departamento", "Nuevo");

            string strAtributo = "checked='checked'";

            ViewBag.DepartamentoSi = strAtributo;
            ViewBag.DepartamentoNo = "";

            return View();
        }

        [HttpPost]
        [Permiso("Control.Departamento.Crear")]
        public ActionResult Crear(Departamento modelo, bool departamento)
        {

            if (ModelState.IsValid)
            {

                modelo.Activo = departamento;
                string strMensaje = new DepartamentoBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Departamento-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }

            }

            string strAtributo = "checked='checked'";

            ViewBag.DepartamentoSi = departamento == true ? strAtributo : "";
            ViewBag.DepartamentoNo = departamento == false ? strAtributo : "";

            return View(modelo);
        }

        [Permiso("Control.Departamento.Editar")]
        public ActionResult Editar(long id)
        {
            Departamento DepartamentoActual = new DepartamentoBL().ObtenerPorId(id);

            if (DepartamentoActual == null || DepartamentoActual.DepartamentoId == 0)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Departamento", "Editar");

            string strAtributo = "checked='checked'";

            ViewBag.DepartamentoSi = DepartamentoActual.Activo == true ? strAtributo : "";
            ViewBag.DepartamentoNo = DepartamentoActual.Activo == false ? strAtributo : "";

            return View(DepartamentoActual);
        }

        [HttpPost]
        [Permiso("Control.Departamento.Editar")]
        public ActionResult Editar(Departamento modelo, bool departamento)
        {

            if (ModelState.IsValid)
            {
                modelo.Activo = departamento;
                string strMensaje = new DepartamentoBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Departamento-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }

            }

            string strAtributo = "checked='checked'";

            ViewBag.DepartamentoSi = departamento == true ? strAtributo : "";
            ViewBag.DepartamentoNo = departamento == false ? strAtributo : "";

            return View(modelo);
        }
    }
}