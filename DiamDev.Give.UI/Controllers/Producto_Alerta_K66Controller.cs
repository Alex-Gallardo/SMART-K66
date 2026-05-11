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
    public class Producto_Alerta_K66Controller : Controller
    {
        // GET: Producto_Alerta_K66
        [Permiso("Control.Producto_Alerta_K66.Ver_Listado")]
        public ActionResult Index(int? page)
        {
            CustomHelper.setTitle("Alerta de Producto", "Listado");

            List<ProductoAlertaK66> Alertas = new List<ProductoAlertaK66>();

            try
            {
                Alertas = new ProductoAlertaK66BL().ObtenerListado();
            }
            catch (Exception ex)
            {
                ViewBag.Error = string.Format("Message: {0} StackTrace: {1}", ex.Message, ex.StackTrace);
                return View("~/Views/Shared/Error.cshtml");
            }

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View(Alertas.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Producto_Alerta_K66.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Alerta de Producto", "Nueva");         
            
            return View();
        }

        [HttpPost]
        [Permiso("Control.Producto_Alerta_K66.Crear")]
        public ActionResult Crear(ProductoAlertaK66 modelo)
        {
            if (ModelState.IsValid)
            {  
                string strMensaje = new ProductoAlertaK66BL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Producto_Alerta-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }

            return View(modelo);
        }

        [Permiso("Control.Producto_Alerta_K66.Editar")]
        public ActionResult Editar(long id)
        {
            ProductoAlertaK66 ProductoAlertaK66Actual = new ProductoAlertaK66BL().ObtenerPorId(id);

            if (ProductoAlertaK66Actual == null || ProductoAlertaK66Actual.AlertaId == 0)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Alerta de Producto", "Editar");           
            
            return View(ProductoAlertaK66Actual);
        }

        [HttpPost]
        [Permiso("Control.Producto_Alerta_K66.Editar")]
        public ActionResult Editar(ProductoAlertaK66 modelo)
        {
            if (ModelState.IsValid)
            {  
                string strMensaje = new ProductoAlertaK66BL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Producto_Alerta-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }          
           
            return View(modelo);
        }
    }
}