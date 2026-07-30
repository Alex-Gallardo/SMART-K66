using System;
using System.Collections;
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
    public class EmpresaController : Controller
    {
        #region Metodos Privados

            public void CargaBodegasWarehousexEmpresaId(long id)
            {
                var Warehouses = new ProductoK66BL().ObtenerBodegaActivaWarehouse(id);

                ViewBag.Warehouses = new SelectList(Warehouses, "ID", "ID");
            }

        #endregion

        // GET: Empresa
        [Permiso("Control.Empresa.Ver_Listado")]
        public ActionResult Index(int? page, string search)
        {
            CustomHelper.setTitle("Empresa", "Listado");

            List<Empresa> Empresas = new List<Empresa>();

            try
            {
                if (!string.IsNullOrWhiteSpace(search) && search != null)
                {
                    Empresas = new EmpresaBL().Buscar(search).ToList();
                }
                else
                {
                    Empresas = new EmpresaBL().ObtenerListado().ToList();
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
            return View(Empresas.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Empresa.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Empresa", "Nueva");          

            return View();
        }

        [HttpPost]
        [Permiso("Control.Empresa.Crear")]
        public ActionResult Crear(Empresa modelo)
        {
            if (ModelState.IsValid)
            {               
                string strMensaje = new EmpresaBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Empresa-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }

            return View(modelo);
        }

        [Permiso("Control.Empresa.Editar")]
        public ActionResult Editar(long id)
        {
            Empresa EmpresaActual = new EmpresaBL().ObtenerPorId(id, false);

            if (EmpresaActual == null || EmpresaActual.EmpresaId == 0)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Empresa", "Editar");         

            return View(EmpresaActual);
        }

        [HttpPost]
        [Permiso("Control.Empresa.Editar")]
        public ActionResult Editar(Empresa modelo)
        {
            if (ModelState.IsValid)
            {              
                string strMensaje = new EmpresaBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Empresa-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }         

            return View(modelo);
        }

        [Permiso("Control.Empresa.Detalle")]
        public ActionResult Detalle(long id)
        {
            Empresa EmpresaActual = new EmpresaBL().ObtenerPorId(id, true);

            if (EmpresaActual == null || EmpresaActual.EmpresaId == 0)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Empresa", "Detalle");

            return View(EmpresaActual);
        }

        [Permiso("Control.Empresa.Bodegas_Activas")]
        public ActionResult Bodegas_Activas(long id)
        {
            Empresa EmpresaActual = new EmpresaBL().ObtenerPorId(id, true);

            if (EmpresaActual == null || EmpresaActual.EmpresaId == 0)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Empresa", "Bodegas Activas");

            this.CargaBodegasWarehousexEmpresaId(EmpresaActual.EmpresaId);
            return View(EmpresaActual);
        }

        [Permiso("Control.Empresa.Productos_Especiales")]
        public ActionResult Productos_Especiales(long id)
        {
            Empresa EmpresaActual = new EmpresaBL().ObtenerPorId(id, true);

            if (EmpresaActual == null || EmpresaActual.EmpresaId == 0)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Empresa", "Productos Especiales");

            return View(EmpresaActual);
        }

        [HttpPost]
        [ActionName("GuardarBodegaActiva")]
        [Permiso("Control.Empresa.Bodegas_Activas")]
        public JsonResult GuardarBodegaActiva(EmpresaBodegaActiva modelo)
        {           
            string Mensaje = new EmpresaBL().GuardarBodegaActiva(modelo);
            if (Mensaje.Equals("OK"))
            {
                return Json(new { Operacion = true }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ActionName("EliminarBodegaActiva")]
        [Permiso("Control.Empresa.Bodegas_Activas")]
        public JsonResult EliminarBodegaActiva(Guid id)
        {
            string Mensaje = new EmpresaBL().EliminarBodegaActiva(id);
            if (Mensaje.Equals("OK"))
            {
                return Json(new { Operacion = true }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ActionName("GuardarProductoEspecial")]
        [Permiso("Control.Empresa.Productos_Especiales")]
        public JsonResult GuardarProductoEspecial(EmpresaProductoEspecial modelo)
        {
            modelo.ResponsableId = CustomHelper.getUserId();
            string Mensaje = new EmpresaBL().GuardarProductoEspecial(modelo);
            if (Mensaje.Equals("OK"))
            {
                return Json(new { Operacion = true }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ActionName("EliminarProductoEspecial")]
        [Permiso("Control.Empresa.Productos_Especiales")]
        public JsonResult EliminarProductoEspecial(Guid id)
        {            
            string Mensaje = new EmpresaBL().EliminarProductoEspecial(id);
            if (Mensaje.Equals("OK"))
            {
                return Json(new { Operacion = true }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }      

        [ActionName("ObtenerBodegaActivaxWarehouse")]
        public JsonResult ObtenerBodegaActivaxWarehouse(long id, string warehouseId)
        {
            IList _result = new List<SelectListItem>();
            _result = new ProductoK66BL().ObtenerBodegaActivaxWarehouse(id, warehouseId).Select(m => new SelectListItem() { Text = m.ID, Value = m.ID }).ToList();
            return Json(_result, JsonRequestBehavior.AllowGet);
        }
    }
}