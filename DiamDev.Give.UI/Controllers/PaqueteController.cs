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
    public class PaqueteController : Controller
    {
        #region Metodos Privados

            private void CargaEmpresas()
            {
                var Empresas = new EmpresaBL().ObtenerListado();

                ViewBag.Empresas = new SelectList(Empresas, "EmpresaId", "Nombre");
            }

            private void CargaPaquetes()
            {
                var Paquetes = new PaqueteBL().ObtenerListadoFormato();

                ViewBag.Paquetes = new SelectList(Paquetes, "PaqueteId", "Nombre");
            }

            private void CargaFormas()
            {
                var Formas = new FormaPagoBL().ObtenerListado(false, 20210705001);

                ViewBag.Formas = new SelectList(Formas, "FormaPagoId", "Nombre");
            }

        #endregion

        // GET: Paquete
        [Permiso("Control.Paquete.Ver_Listado")]
        public ActionResult Index(int? page, string search)
        {
            CustomHelper.setTitle("Paquete", "Listado");

            List<Paquete> Paquetes = new List<Paquete>();

            try
            {
                if (!string.IsNullOrWhiteSpace(search) && search != null)
                {
                    Paquetes = new PaqueteBL().Buscar(search).ToList();
                }
                else
                {
                    Paquetes = new PaqueteBL().ObtenerListado().ToList();
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
            return View(Paquetes.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Paquete.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Paquete", "Nuevo");          

            return View();
        }

        [HttpPost]
        [Permiso("Control.Paquete.Crear")]
        public ActionResult Crear(Paquete modelo)
        {
            if (ModelState.IsValid)
            {               
                string strMensaje = new PaqueteBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Paquete-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }

            return View(modelo);
        }

        [Permiso("Control.Paquete.Editar")]
        public ActionResult Editar(long id)
        {
            Paquete PaqueteActual = new PaqueteBL().ObtenerPorId(id);

            if (PaqueteActual == null || PaqueteActual.PaqueteId == 0)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Paquete", "Editar");         

            return View(PaqueteActual);
        }

        [HttpPost]
        [Permiso("Control.Paquete.Editar")]
        public ActionResult Editar(Paquete modelo)
        {
            if (ModelState.IsValid)
            {              
                string strMensaje = new PaqueteBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Paquete-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }         

            return View(modelo);
        }

        [Permiso("Control.Paquete.Venta")]
        public ActionResult Venta()
        {
            CustomHelper.setTitle("Paquete", "Venta");

            this.CargaEmpresas();
            this.CargaPaquetes();
            this.CargaFormas();
            return View();
        }

        [HttpPost]
        [Permiso("Control.Paquete.Venta")]
        public ActionResult Venta(PaqueteEmpresa modelo)
        {
            if (ModelState.IsValid)
            {
                modelo.ResponsableId = CustomHelper.getUserId();
                string strMensaje = new PaqueteEmpresaBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Paquete_Empresa-Success"] = strMensaje;
                    return RedirectToAction("Venta");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }

            this.CargaEmpresas();
            this.CargaPaquetes();
            this.CargaFormas();
            return View(modelo);
        }

        [Permiso("Control.Paquete.Propios")]
        public ActionResult Propios()
        {
            CustomHelper.setTitle("Paquete", "Propios");
           
            return View(new PaqueteEmpresaBL().ObtenerPaquetesxEmpresa(CustomHelper.getEmpresaId()));
        }
    }
}