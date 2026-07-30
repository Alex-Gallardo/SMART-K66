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
    public class MesaController : Controller
    {
        #region Metodos Privados

            private void CargaControles()
            {
                var Tipos = new TipoUbicacionBL().ObtenerListado(false);

                ViewBag.Tipos = new SelectList(Tipos, "TipoId", "Nombre");
            }
        
        #endregion

        // GET: Mesa
        [Permiso("Control.Mesa.Ver_Listado")]
        public ActionResult Index(int? page, string search)
        {
            CustomHelper.setTitle("Mesa", "Listado");

            List<Mesa> Mesas = new List<Mesa>();

            try
            {
                if (!string.IsNullOrWhiteSpace(search) && search != null)
                {
                    Mesas = new MesaBL().Buscar(search, CustomHelper.getAgenciaId()).ToList();
                }             
                else
                {
                    Mesas = new MesaBL().ObtenerListado(true, CustomHelper.getAgenciaId()).ToList();
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
            return View(Mesas.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Mesa.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Mesa", "Nueva");

            string strAtributo = "checked='checked'";

            ViewBag.activoSi = strAtributo;
            ViewBag.activoNo = "";

            this.CargaControles();
            return View();
        }

        [HttpPost]
        [Permiso("Control.Mesa.Crear")]
        public ActionResult Crear(Mesa modelo, bool activo)
        {
            if (ModelState.IsValid)
            {
                modelo.AgenciaId = CustomHelper.getAgenciaId();
                modelo.Activo = activo;

                string strMensaje = new MesaBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Mesa-Success"] = strMensaje;
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

        [Permiso("Control.Mesa.Editar")]
        public ActionResult Editar(long id)
        {
            Mesa MesaActual = new MesaBL().ObtenerPorId(id);

            if (MesaActual == null || MesaActual.MesaId == 0)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Mesa", "Editar");

            string strAtributo = "checked='checked'";

            ViewBag.activoSi = MesaActual.Activo == true ? strAtributo : "";
            ViewBag.activoNo = MesaActual.Activo == false ? strAtributo : "";

            this.CargaControles();
            return View(MesaActual);
        }

        [HttpPost]
        [Permiso("Control.Mesa.Editar")]
        public ActionResult Editar(Mesa modelo, bool activo)
        {
            if (ModelState.IsValid)
            {
                modelo.Activo = activo;
                string strMensaje = new MesaBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Mesa-Success"] = strMensaje;
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
    }
}