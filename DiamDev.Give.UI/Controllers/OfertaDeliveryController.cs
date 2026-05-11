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
    public class OfertaDeliveryController : Controller
    {
        // GET: OFERTAS
        [Permiso("Control.Oferta.Listado")]
        public ActionResult Index(int? page)
        {
            CustomHelper.setTitle("Ofertas Delivery", "Listado");

            List<OfertaDelivery> Agencias = new List<OfertaDelivery>();

            try
            {
                Agencias = new OfertaDeliveryBL().ObtenerListado();
            }
            catch (Exception ex)
            {
                ViewBag.Error = string.Format("Message: {0} StackTrace: {1}", ex.Message, ex.StackTrace);
                return View("~/Views/Shared/Error.cshtml");
            }

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View(Agencias.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Oferta.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Oferta", "Nueva");

            List<Producto> prodo = new ProductoBL().ObtenerListado().Where(x => x.Activo).ToList();

            ViewBag.Productos = new SelectList(prodo, "ProductoId", "Nombre"); ;
            return View();
        }
        [HttpPost]
        [Permiso("Control.Oferta.Crear")]
        public ActionResult Crear(OfertaDelivery modelo)
        {

            if (ModelState.IsValid)
            {
                modelo.UsrCreo = CustomHelper.getUserId();
                string strMensaje = new OfertaDeliveryBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Oferta-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }

            }

           

            return View(modelo);
        }

        [Permiso("Control.Oferta.Editar")]
        public ActionResult Editar(int id)
        {
            OfertaDelivery OfertaActual = new OfertaDeliveryBL().ObtenerPorId(id);

            if (OfertaActual == null || OfertaActual.OfertaId == 0)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Oferta", "Editar");

          
            List<Producto> prodo = new ProductoBL().ObtenerListado().Where(x => x.Activo).ToList();
           
            ViewBag.Productos = new SelectList(prodo, "ProductoId", "Nombre"); ;

            return View(OfertaActual);
        }

        [HttpPost]
        [Permiso("Control.Oferta.Editar")]
        public ActionResult Editar(OfertaDelivery modelo)
        {

            if (ModelState.IsValid)
            {
            
                string strMensaje = new OfertaDeliveryBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Oferta-Success"] = strMensaje;
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