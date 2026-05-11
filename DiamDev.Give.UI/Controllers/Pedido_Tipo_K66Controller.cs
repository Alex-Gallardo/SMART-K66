using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using DiamDev.Give.BLL;
using DiamDev.Give.Entities;
using DiamDev.Give.UI.App_Start;
using PagedList;

namespace DiamDev.Give.UI.Controllers
{
    [Authorize]
    [Seguridad]
    [HandleError]
    public class Pedido_Tipo_K66Controller : Controller
    {
        #region Metodos Privados

            private void CargaEmpresas()
            {
                var Empresas = new EmpresaBL().ObtenerListadoxUsuario(CustomHelper.getUserId());

                ViewBag.Empresas = new SelectList(Empresas, "EmpresaId", "Nombre");
            }

        #endregion

        // GET: Pedido_Tipo_K66
        [Permiso("Control.Pedido_Tipo_K66.Ver_Listado")]
        public ActionResult Index(int? page, long? empresa, string search)
        {
            CustomHelper.setTitle("Tipo de Pedido K66", "Listado");
            List<PedidoTipoK66> Tipos = new List<PedidoTipoK66>();
         
            try
            {
                if (!string.IsNullOrWhiteSpace(search) && search != null)
                {
                    Tipos = new PedidoTipok66BL().Buscar(search).ToList();
                }
                else if (empresa != null)
                {
                    Tipos = new PedidoTipok66BL().ObtenerListado().ToList();//new PedidoTipok66BL().ObtenerListadoxEmpresa(empresa.Value).ToList();
                }
                else
                {
                    Tipos = new PedidoTipok66BL().ObtenerListado().ToList();
                }
            }
            catch (Exception)
            {}
            
            ViewBag.Search = search;

            this.CargaEmpresas();

            int pageSize = 15;
            int pageNumber = (page ?? 1);
            return View(Tipos.ToPagedList(pageNumber, pageSize));
        }     

        [Permiso("Control.Pedido_Tipo_K66.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Tipo de Pedido K66", "Nuevo");             

            this.CargaEmpresas();
            return View();
        }

        [Permiso("Control.Pedido_Tipo_K66.Crear")]
        [HttpPost]
        public ActionResult Crear(PedidoTipoK66 modelo)
        {  
            modelo.ResponsableId = CustomHelper.getUserId();       
            
            if (ModelState.IsValid)
            {
                string strMensaje = new PedidoTipok66BL().Guardar(modelo);
                if (strMensaje.Equals("OK"))
                {
                    TempData["Tipo_Pedido-Success"] = strMensaje;
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

        [Permiso("Control.Pedido_Tipo_K66.Editar")]
        public ActionResult Editar(Guid id)
        {
            PedidoTipoK66 PedidoTipoK66Actual = new PedidoTipok66BL().ObtenerxId(id);

            if (PedidoTipoK66Actual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Tipo de Pedido K66", "Editar");

            this.CargaEmpresas();
            return View(PedidoTipoK66Actual);
        }

        [HttpPost]
        [Permiso("Control.Pedido_Tipo_K66.Editar")]
        public ActionResult Editar(PedidoTipoK66 modelo)
        {
            if (ModelState.IsValid)
            {                
                string strMensaje = new PedidoTipok66BL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Tipo_Pedido-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }

            return View(new PedidoTipok66BL().ObtenerxId(modelo.TipoId));
        }

        [ActionName("GetPedidoTipoxId")]
        public JsonResult GetPedidoTipoxId(Guid id)
        {
            PedidoTipoK66 PedidoTipoK66Actual = new PedidoTipok66BL().ObtenerxId(id);
            if (PedidoTipoK66Actual != null)
            {
                return Json(new { Operacion = true, Data = PedidoTipoK66Actual }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }
    }
}