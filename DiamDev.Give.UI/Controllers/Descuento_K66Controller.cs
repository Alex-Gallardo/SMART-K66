using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using DiamDev.Give.BLL;
using DiamDev.Give.Entities;
using DiamDev.Give.UI.App_Start;
using PagedList;
using System.Data;

namespace DiamDev.Give.UI.Controllers
{
    [Authorize]
    [Seguridad]
    [HandleError]
    public class Descuento_K66Controller : Controller
    {
        #region Metodos Privados

            private void CargaEmpresas()
            {
                var Empresas = new EmpresaBL().ObtenerListadoxUsuario(CustomHelper.getUserId());

                ViewBag.Empresas = new SelectList(Empresas, "EmpresaId", "Nombre");
            }

        #endregion

        // GET: Descuento_K66
        [Permiso("Control.Descuento_K66.Ver_Listado")]
        public ActionResult Index(int? page, long? empresa, string search, DateTime? FechaInicial, DateTime? FechaFinal)
        {
            CustomHelper.setTitle("Descuento K66", "Listado");
            List<DescuentoK66> Descuentos = new List<DescuentoK66>();
         
            try
            {
                if (!FechaInicial.HasValue && !FechaFinal.HasValue)
                {
                    FechaInicial = DateTime.Today;
                    FechaFinal = DateTime.Today;
                }
               
                if (!string.IsNullOrWhiteSpace(search) && search != null)
                {
                    Descuentos = new Descuentok66BL().Buscar(search).ToList();
                }
                else if (empresa != null)
                {
                    Descuentos = new Descuentok66BL().ObtenerListadoxEmpresa(empresa.Value).ToList();
                }
                else
                {
                    Descuentos = new Descuentok66BL().ObtenerListadoxFecha(FechaInicial.Value, FechaFinal.Value).ToList();
                }
            }
            catch (Exception)
            {}

            ViewBag.Eliminar = CustomHelper.Permiso("Control.Descuento_K66.Eliminar");
            ViewBag.Search = search;

            this.CargaEmpresas();

            int pageSize = 15;
            int pageNumber = (page ?? 1);
            return View(Descuentos.ToPagedList(pageNumber, pageSize));
        }     

        [Permiso("Control.Descuento_K66.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Descuento K66", "Nuevo");             

            this.CargaEmpresas();
            return View();
        }

        [Permiso("Control.Descuento_K66.Crear")]
        [HttpPost]
        public ActionResult Crear(DescuentoK66 modelo)
        {  
            modelo.ResponsableId = CustomHelper.getUserId();       
            
            if (ModelState.IsValid)
            {
                string strMensaje = new Descuentok66BL().Guardar(modelo);
                if (strMensaje.Equals("OK"))
                {
                    TempData["Descuento-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }

            this.CargaEmpresas();
            return View(modelo);
        }

        [Permiso("Control.Descuento_K66.Editar")]
        public ActionResult Editar(Guid id)
        {
            DescuentoK66 DescuentoK66Actual = new Descuentok66BL().ObtenerxId(id);

            if (DescuentoK66Actual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Descuento K66", "Editar");            

            return View(DescuentoK66Actual);
        }

        [HttpPost]
        [Permiso("Control.Descuento_K66.Editar")]
        public ActionResult Editar(DescuentoK66 modelo)
        {
            if (ModelState.IsValid)
            {                
                string strMensaje = new Descuentok66BL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Descuento-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }

            return View(new Descuentok66BL().ObtenerxId(modelo.DescuentoId));
        }

        [HttpPost]
        [ActionName("EliminarDescuento")]
        [Permiso("Control.Descuento_K66.Eliminar")]
        public JsonResult EliminarDescuento(Guid id)
        {
            string Mensaje = new Descuentok66BL().Eliminar(id);
            if (Mensaje.Equals("OK"))
            {
                return Json(new { Operacion = true }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }
    }
}