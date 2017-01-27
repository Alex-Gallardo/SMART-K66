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
    public class ProveedorController : Controller
    {
        #region Metodos Privados

        private void CargaControles()
        {
            var Productos = new ProductoBL().ObtenerListado(true, false, true);

            ViewBag.Productos = new SelectList(Productos, "ProductoId", "Nombre");
        }

        #endregion

        // GET: Proveedor
        [Permiso("Control.Proveedor.Ver_Listado")]
        public ActionResult Index(int? page, string search)
        {
            CustomHelper.setTitle("Proveedor", "Listado");

            List<Proveedor> Proveedores = new List<Proveedor>();

            try
            {
                if (!string.IsNullOrWhiteSpace(search) && search != null)
                {
                    Proveedores = new ProveedorBL().Buscar(search).ToList();
                }
                else
                {
                    Proveedores = new ProveedorBL().ObtenerListado(true).ToList();
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
            return View(Proveedores.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Proveedor.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Proveedor", "Nuevo");

            string strAtributo = "checked='checked'";

            ViewBag.activoSi = strAtributo;
            ViewBag.activoNo = "";

            return View();
        }

        [Permiso("Control.Proveedor.Crear")]
        [HttpPost]
        public ActionResult Crear(Proveedor modelo, bool activo)
        {
            if (ModelState.IsValid)
            {
                modelo.Activo = activo;
                string strMensaje = new ProveedorBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Proveedor-Success"] = strMensaje;
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

        [Permiso("Control.Proveedor.Crear")]
        [HttpPost]
        [ActionName("NuevoProveedor")]
        public ActionResult Crear(Proveedor modelo)
        {
            if (ModelState.IsValid)
            {
                string strMensaje = new ProveedorBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    return Json(true, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(false, JsonRequestBehavior.AllowGet);
        }

        [Permiso("Control.Proveedor.Editar")]
        public ActionResult Editar(long id)
        {
            Proveedor ProveedorActual = new ProveedorBL().ObtenerPorId(id, false);

            if (ProveedorActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Proveedor", "Editar");

            string strAtributo = "checked='checked'";

            ViewBag.activoSi = ProveedorActual.Activo == true ? strAtributo : "";
            ViewBag.activoNo = ProveedorActual.Activo == false ? strAtributo : "";

            if (ProveedorActual.Productos != null && ProveedorActual.Productos.Count() > 0)
            {
                ViewBag.productoIds = ProveedorActual.Productos.Select(x => x.ProductoId).ToList();
            }
            else
            {
                ViewBag.productoIds = 0;
            }

            this.CargaControles();
            return View(ProveedorActual);
        }

        [Permiso("Control.Proveedor.Editar")]
        [HttpPost]
        public ActionResult Editar(Proveedor modelo, string[] productoIds, bool activo)
        {
            if (productoIds == null || productoIds.Length == 0)
            {
                ModelState.AddModelError("", "El proveedor no contiene productos asignados");
            }

            if (ModelState.IsValid)
            {
                modelo.Productos = new List<ProveedorProducto>();
                for (int i = 0; i < productoIds.Length; i++)
                {
                    ProveedorProducto Detalle = new ProveedorProducto();
                    Detalle.ProveedorId = modelo.ProveedorId;
                    Detalle.ProductoId = productoIds[i];

                    modelo.Productos.Add(Detalle);
                }

                modelo.Activo = activo;
                string strMensaje = new ProveedorBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Proveedor-Success"] = strMensaje;
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

            ViewBag.productoIds = productoIds;

            this.CargaControles();
            return View(modelo);
        }

        [Permiso("Control.Proveedor.Detalle")]
        public ActionResult Detalle(long id)
        {
            Proveedor ProveedorActual = new ProveedorBL().ObtenerPorId(id, true);

            if (ProveedorActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Proveedor", "Detalle");

            return View(ProveedorActual);
        }
    }
}