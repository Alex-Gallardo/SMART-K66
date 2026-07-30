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
using System.Collections;

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
                var Tipos = new ProveedorTipoBL().ObtenerListado();
                var Bancos = new BancoBL().ObtenerListado();
                var Productos = new ProductoBL().ObtenerListado(true, false, true);

                ViewBag.Tipos = new SelectList(Tipos, "TipoId", "Nombre");
                ViewBag.Bancos = new SelectList(Bancos, "BancoId", "Nombre");
                ViewBag.Productos = new SelectList(Productos, "ProductoId", "Nombre");               
            }

            private void CargaProveedores() 
            {
                var Proveedores = new ProveedorBL().ObtenerListado(false);

                ViewBag.Proveedores = new SelectList(Proveedores, "ProveedorId", "Nombre");
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

        [Permiso("Control.Proveedor.Credito.Ver_Listado")]
        public ActionResult Credito(long? proveedorId)
        {
            CustomHelper.setTitle("Creditos Pendientes", "Listado");
            List<MovimientoxProveedorModel> Creditos = new List<MovimientoxProveedorModel>();

            try
            {
                if (proveedorId != null)
                {
                    Creditos = new MovimientoBL().ObtenerMovimientoAlCreditoNoCancelados(proveedorId.Value).ToList();
                }
            }
            catch (Exception)
            {
            }

            if (Creditos != null && Creditos.Count() > 0)
            {
                ViewBag.Total = (Creditos.Sum(y => y.Monto)).ToString("C4");
            }
            else
            {
                ViewBag.Total = "Q0.0000";
            }

         
            this.CargaProveedores();

            return View(Creditos);
        }

        [Permiso("Control.Proveedor.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Proveedor", "Nuevo");

            string strAtributo = "checked='checked'";

            ViewBag.activoSi = strAtributo;
            ViewBag.activoNo = "";

            this.CargaControles();
            return View();
        }

        [Permiso("Control.Proveedor.Crear")]
        [HttpPost]
        public ActionResult Crear(Proveedor modelo, long[] bancoIds, string[] cuentaIds, string[] productoIds, bool activo)
        {
            if (ModelState.IsValid)
            {
                if (bancoIds != null && bancoIds.Count() > 0)
                {
                    modelo.Cuentas = new List<ProveedorCuentaBancaria>();
                    for (int i = 0; i < bancoIds.Length; i++)
                    {
                        ProveedorCuentaBancaria Detalle = new ProveedorCuentaBancaria();
                        Detalle.BancoId = bancoIds[i];
                        Detalle.Cuenta = cuentaIds[i];

                        modelo.Cuentas.Add(Detalle);
                    }                   
                }

                if (productoIds != null && productoIds.Count() > 0)
                {
                    modelo.Productos = new List<ProveedorProducto>();
                    for (int i = 0; i < productoIds.Length; i++)
                    {
                        ProveedorProducto Detalle = new ProveedorProducto();
                        Detalle.ProductoId = productoIds[i];

                        modelo.Productos.Add(Detalle);
                    }
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

            ViewBag.bancoIds = bancoIds;
            ViewBag.cuentaIds = cuentaIds;
            ViewBag.productoIds = productoIds;

            this.CargaControles();
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

            if (ProveedorActual.Cuentas != null && ProveedorActual.Cuentas.Count() > 0)
            {
                ViewBag.bancoIds = ProveedorActual.Cuentas.Select(x => x.BancoId).ToList();
                ViewBag.cuentaIds = ProveedorActual.Cuentas.Select(x => x.Cuenta).ToList();
            }
            else
            {
                ViewBag.bancoIds = 0;
                ViewBag.cuentaIds = 0;
            }

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
        public ActionResult Editar(Proveedor modelo, long[] bancoIds, string[] cuentaIds, string[] productoIds, bool activo)
        {  
            if (ModelState.IsValid)
            {
                if (bancoIds != null && bancoIds.Count() > 0)
                {
                    modelo.Cuentas = new List<ProveedorCuentaBancaria>();
                    for (int i = 0; i < bancoIds.Length; i++)
                    {
                        ProveedorCuentaBancaria Detalle = new ProveedorCuentaBancaria();
                        Detalle.BancoId = bancoIds[i];
                        Detalle.Cuenta = cuentaIds[i];

                        modelo.Cuentas.Add(Detalle);
                    }
                }

                if (productoIds != null && productoIds.Count() > 0)
                {
                    modelo.Productos = new List<ProveedorProducto>();
                    for (int i = 0; i < productoIds.Length; i++)
                    {
                        ProveedorProducto Detalle = new ProveedorProducto();
                        Detalle.ProductoId = productoIds[i];

                        modelo.Productos.Add(Detalle);
                    }
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

            ViewBag.bancoIds = bancoIds;
            ViewBag.cuentaIds = cuentaIds;
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

        [ActionName("ObtenerCreditosxProveedorId")]
        public JsonResult ObtenerCreditosxProveedorId(long id)
        {
            IList _result = new List<SelectListItem>();
            _result = new MovimientoBL().ObtenerCreditosNoCancelados(id).Select(m => new SelectListItem() { Text = m.Documento, Value = m.MovimientoId.ToString() }).ToList();
            return Json(_result, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerTotalCreditos")]
        public JsonResult ObtenerTotalCreditos(long proveedorId, long id)
        {
            if (proveedorId > 0 && id > 0)
            {
                return Json(new { Operacion = true, Data = new MovimientoBL().ObtenerTotalCreditoPendiente(proveedorId, id) }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ActionName("GenerarPagoMasivoxProveedor")]
        public JsonResult GenerarPagoMasivoxProveedor(long proveedorId, long[] creditoIDs, decimal[] saldoIDs, string observaciones)
        {
            if (creditoIDs != null && saldoIDs != null)
            {
                string Mensaje = new ProveedorMovimientoBL().GenerarMasivo(proveedorId, creditoIDs, saldoIDs, observaciones, CustomHelper.getUserId());
                if (Mensaje.Equals("OK"))
                {
                    return Json(new { Operacion = true }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }
    }
}