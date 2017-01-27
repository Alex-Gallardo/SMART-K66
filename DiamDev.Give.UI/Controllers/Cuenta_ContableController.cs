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
    public class Cuenta_ContableController : Controller
    {
        #region Metodos Privados

            private void CargaControles()
            {
                var Tipos = new CuentaContableTipoBL().ObtenerListado();
                var Cuentas = new CuentaContableBL().ObtenerListado(true, true);

                ViewBag.Tipos = new SelectList(Tipos, "TipoId", "Nombre");
                ViewBag.Cuentas = new SelectList(Cuentas, "CuentaId", "Nombre");
            }

        #endregion

        // GET: Cuenta_Contable
        [Permiso("Control.Cuenta_Contable.Ver_Listado")]
        public ActionResult Index(int? page, string search)
        {
            CustomHelper.setTitle("Cuenta Contable", "Listado");

            List<CuentaContable> Cuentas = new List<CuentaContable>();

            try
            {
                if (!string.IsNullOrWhiteSpace(search) && search != null)
                {
                    Cuentas = new CuentaContableBL().Buscar(search).ToList();
                }
                else
                {
                    Cuentas = new CuentaContableBL().ObtenerListado().ToList();
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
            return View(Cuentas.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Cuenta_Contable.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Cuenta Contable", "Nueva");

            string strAtributo = "checked='checked'";

            ViewBag.activoSi = strAtributo;
            ViewBag.activoNo = "";

            ViewBag.debeSi = strAtributo;
            ViewBag.debeNo = "";

            this.CargaControles();
            return View();
        }

        [Permiso("Control.Cuenta_Contable.Crear")]
        [HttpPost]
        public ActionResult Crear(CuentaContable modelo, bool activo)
        {

            if (ModelState.IsValid)
            {
                modelo.Activo = activo;
                string strMensaje = new CuentaContableBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Cuenta_Contable-Success"] = strMensaje;
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

            this.CargaControles();
            return View(modelo);
        }

        [Permiso("Control.Cuenta_Contable.Editar")]
        public ActionResult Editar(long id)
        {
            CuentaContable CuentaActual = new CuentaContableBL().ObtenerPorId(id, false);

            if (CuentaActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Cuenta Contable", "Editar");

            string strAtributo = "checked='checked'";

            ViewBag.activoSi = CuentaActual.Activo == true ? strAtributo : "";
            ViewBag.activoNo = CuentaActual.Activo == false ? strAtributo : "";

            this.CargaControles();
            return View(CuentaActual);
        }

        [Permiso("Control.Cuenta_Contable.Editar")]
        [HttpPost]
        public ActionResult Editar(CuentaContable modelo, bool activo)
        {
            if (ModelState.IsValid)
            {
                modelo.Activo = activo;
                string strMensaje = new CuentaContableBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Cuenta_Contable-Success"] = strMensaje;
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

            this.CargaControles();
            return View(modelo);
        }

        [Permiso("Control.Cuenta_Contable.Detalle")]
        public ActionResult Detalle(long id)
        {
            CuentaContable CuentaActual = new CuentaContableBL().ObtenerPorId(id, true);

            if (CuentaActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Cuenta Contable", "Detalle");

            return View(CuentaActual);
        }
    }
}