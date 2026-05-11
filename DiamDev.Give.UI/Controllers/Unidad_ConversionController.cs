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
    public class Unidad_ConversionController : Controller
    {
        #region Metodos Privados

            private void CargaControles()
            {
                var Operaciones = new UnidadOperacionBL().ObtenerListado();
                var Unidades = new UnidadBL().ObtenerListado(false);

                ViewBag.Operaciones = new SelectList(Operaciones, "OperacionId", "Nombre");
                ViewBag.Unidades = new SelectList(Unidades, "UnidadId", "Nombre");
                ViewBag.UnidadesDestino = new SelectList(Unidades, "UnidadId", "Nombre");
            }

        #endregion

        // GET: Unidad_Conversion
        [Permiso("Control.Unidad_Conversion.Ver_Listado")]
        public ActionResult Index(int? page, string search)
        {
            CustomHelper.setTitle("Unidad de Conversión", "Listado");

            List<UnidadConversion> UnidadConversions = new List<UnidadConversion>();

            try
            {
                if (!string.IsNullOrWhiteSpace(search) && search != null)
                {
                    UnidadConversions = new UnidadConversionBL().Buscar(search).ToList();
                }
                else
                {
                    UnidadConversions = new UnidadConversionBL().ObtenerListado().ToList();
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
            return View(UnidadConversions.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Unidad_Conversion.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Unidad de Conversión", "Nueva");

            this.CargaControles();
            return View();
        }

        [Permiso("Control.Unidad_Conversion.Crear")]
        [HttpPost]
        public ActionResult Crear(UnidadConversion modelo)
        {
            if (ModelState.IsValid)
            {
                string strMensaje = new UnidadConversionBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Unidad_Conversion-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }

            }

            this.CargaControles();
            return View(modelo);
        }

        [Permiso("Control.Unidad_Conversion.Editar")]
        public ActionResult Editar(long id)
        {
            UnidadConversion UnidadConversionActual = new UnidadConversionBL().ObtenerPorId(id);

            if (UnidadConversionActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Unidad de Conversión", "Editar");

            this.CargaControles();
            return View(UnidadConversionActual);
        }

        [Permiso("Control.Unidad_Conversion.Editar")]
        [HttpPost]
        public ActionResult Editar(UnidadConversion modelo)
        {
            if (ModelState.IsValid)
            {
                string strMensaje = new UnidadConversionBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Unidad_Conversion-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }

            this.CargaControles();
            return View(modelo);
        }
    }
}