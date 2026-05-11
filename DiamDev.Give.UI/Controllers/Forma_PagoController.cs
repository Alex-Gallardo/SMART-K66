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
    public class Forma_PagoController : Controller
    {
        // GET: /Forma_Pago/
        [Permiso("Control.Forma_Pago.Ver_Listado")]
        public ActionResult Index(int? page)
        {
            CustomHelper.setTitle("Forma de Pago", "Listado");

            List<FormaPago> FormaPagos = new List<FormaPago>();

            try
            {
                FormaPagos = new FormaPagoBL().ObtenerListado(true, CustomHelper.getEmpresaId());
            }
            catch (Exception ex)
            {
                ViewBag.Error = string.Format("Message: {0} StackTrace: {1}", ex.Message, ex.StackTrace);
                return View("~/Views/Shared/Error.cshtml");
            }

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View(FormaPagos.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Forma_Pago.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Forma de Pago", "Nueva");

            string strAtributo = "checked='checked'";

            ViewBag.activoSi = strAtributo;
            ViewBag.activoNo = "";

            return View();
        }

        [HttpPost]
        [Permiso("Control.Forma_Pago.Crear")]
        public ActionResult Crear(FormaPago modelo, bool activo)
        {
            if (ModelState.IsValid)
            {
                modelo.EmpresaId = CustomHelper.getEmpresaId();
                modelo.Activo = activo;
                string strMensaje = new FormaPagoBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Forma-Pago-Success"] = strMensaje;
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

        [Permiso("Control.Forma_Pago.Editar")]
        public ActionResult Editar(long id)
        {
            FormaPago FormaPagoActual = new FormaPagoBL().ObtenerPorId(id);

            if (FormaPagoActual == null || FormaPagoActual.FormaPagoId == 0)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Forma de Pago", "Editar");

            string strAtributo = "checked='checked'";

            ViewBag.activoSi = FormaPagoActual.Activo == true ? strAtributo : "";
            ViewBag.activoNo = FormaPagoActual.Activo == false ? strAtributo : "";

            return View(FormaPagoActual);
        }

        [HttpPost]
        [Permiso("Control.Forma_Pago.Editar")]
        public ActionResult Editar(FormaPago modelo, bool activo)
        {
            if (ModelState.IsValid)
            {
                modelo.EmpresaId = CustomHelper.getEmpresaId();
                modelo.Activo = activo;
                string strMensaje = new FormaPagoBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Forma-Pago-Success"] = strMensaje;
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