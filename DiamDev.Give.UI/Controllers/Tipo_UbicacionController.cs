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
    public class Tipo_UbicacionController : Controller
    {
        // GET: Tipo_Ubicacion
        [Permiso("Control.Tipo_Ubicacion.Ver_Listado")]
        public ActionResult Index(int? page, string search)
        {
            CustomHelper.setTitle("Tipo de Ubicacion", "Listado");

            List<TipoUbicacion> Tipos = new List<TipoUbicacion>();

            try
            {
                if (!string.IsNullOrWhiteSpace(search) && search != null)
                {
                    Tipos = new TipoUbicacionBL().Buscar(search).ToList();
                }             
                else
                {
                    Tipos = new TipoUbicacionBL().ObtenerListado(true).ToList();
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
            return View(Tipos.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Tipo_Ubicacion.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Tipo de Ubicacion", "Nuevo");

            string strAtributo = "checked='checked'";

            ViewBag.activoSi = strAtributo;
            ViewBag.activoNo = "";

            return View();
        }

        [HttpPost]
        [Permiso("Control.Tipo_Ubicacion.Crear")]
        public ActionResult Crear(TipoUbicacion modelo, bool activo)
        {
            if (ModelState.IsValid)
            {
                modelo.Activo = activo;
                string strMensaje = new TipoUbicacionBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Tipo_Ubicacion-Success"] = strMensaje;
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

        [Permiso("Control.Tipo_Ubicacion.Editar")]
        public ActionResult Editar(long id)
        {
            TipoUbicacion TipoUbicacionActual = new TipoUbicacionBL().ObtenerPorId(id);

            if (TipoUbicacionActual == null || TipoUbicacionActual.TipoId == 0)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Tipo de Ubicacion", "Editar");

            string strAtributo = "checked='checked'";

            ViewBag.activoSi = TipoUbicacionActual.Activo == true ? strAtributo : "";
            ViewBag.activoNo = TipoUbicacionActual.Activo == false ? strAtributo : "";

            return View(TipoUbicacionActual);
        }

        [HttpPost]
        [Permiso("Control.Tipo_Ubicacion.Editar")]
        public ActionResult Editar(TipoUbicacion modelo, bool activo)
        {
            if (ModelState.IsValid)
            {
                modelo.Activo = activo;
                string strMensaje = new TipoUbicacionBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Tipo_Ubicacion-Success"] = strMensaje;
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