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
    public class Categoria_GastoController : Controller
    {
        // GET: Categoria_Gasto
        [Permiso("Control.Categoria_Gasto.Ver_Listado")]
        public ActionResult Index(int? page, string search)
        {
            CustomHelper.setTitle("Categoria de Gasto", "Listado");

            List<CategoriaGasto> CategoriaGastos = new List<CategoriaGasto>();

            try
            {
                if (!string.IsNullOrWhiteSpace(search) && search != null)
                {
                    CategoriaGastos = new CategoriaGastoBL().Buscar(search);
                }
                else
                {
                    CategoriaGastos = new CategoriaGastoBL().ObtenerListado(true);
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
            return View(CategoriaGastos.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Categoria_Gasto.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Categoria de Gasto", "Nueva");

            string strAtributo = "checked='checked'";

            ViewBag.activoSi = strAtributo;
            ViewBag.activoNo = "";

            return View();
        }

        [HttpPost]
        [Permiso("Control.Categoria_Gasto.Crear")]
        public ActionResult Crear(CategoriaGasto modelo, bool activo)
        {

            if (ModelState.IsValid)
            {
                modelo.Activo = activo;
                string strMensaje = new CategoriaGastoBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Categoria_Gasto-Success"] = strMensaje;
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

        [Permiso("Control.Categoria_Gasto.Editar")]
        public ActionResult Editar(long id)
        {
            CategoriaGasto CategoriaGastoActual = new CategoriaGastoBL().ObtenerPorId(id);

            if (CategoriaGastoActual == null || CategoriaGastoActual.CategoriaId == 0)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Categoria de Gasto", "Editar");

            string strAtributo = "checked='checked'";

            ViewBag.activoSi = CategoriaGastoActual.Activo == true ? strAtributo : "";
            ViewBag.activoNo = CategoriaGastoActual.Activo == false ? strAtributo : "";

            return View(CategoriaGastoActual);
        }

        [HttpPost]
        [Permiso("Control.Categoria_Gasto.Editar")]
        public ActionResult Editar(CategoriaGasto modelo, bool activo)
        {

            if (ModelState.IsValid)
            {
                modelo.Activo = activo;
                string strMensaje = new CategoriaGastoBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Categoria_Gasto-Success"] = strMensaje;
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