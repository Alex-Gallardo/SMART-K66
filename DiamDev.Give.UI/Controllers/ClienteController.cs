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
    public class ClienteController : Controller
    {
        // GET: Cliente
        [Permiso("Control.Cliente.Ver_Listado")]
        public ActionResult Index(int? page, string search)
        {
            CustomHelper.setTitle("Cliente", "Listado");

            List<Cliente> Clientes = new List<Cliente>();

            try
            {
                if (!string.IsNullOrWhiteSpace(search) && search != null)
                {
                    Clientes = new ClienteBL().Buscar(search).ToList();
                }
                else
                {
                    Clientes = new ClienteBL().ObtenerListado(true, false).ToList();
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
            return View(Clientes.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Cliente.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Cliente", "Nuevo");

            string strAtributo = "checked='checked'";

            ViewBag.vipSi = "";
            ViewBag.vipNo = strAtributo;

            ViewBag.activoSi = strAtributo;
            ViewBag.activoNo = "";

            return View();
        }

        [Permiso("Control.Cliente.Crear")]
        [HttpPost]
        public ActionResult Crear(Cliente modelo, bool vip, bool activo)
        {
            if (ModelState.IsValid)
            {
                modelo.Vip = vip;
                modelo.Activo = activo;
                string strMensaje = new ClienteBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Cliente-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }

            }

            string strAtributo = "checked='checked'";

            ViewBag.vipSi = vip == true ? strAtributo : "";
            ViewBag.vipNo = vip == false ? strAtributo : "";

            ViewBag.activoSi = activo == true ? strAtributo : "";
            ViewBag.activoNo = activo == false ? strAtributo : "";

            return View(modelo);
        }

        [Permiso("Control.Cliente.Crear")]
        [HttpPost]
        [ActionName("NuevoCliente")]
        public ActionResult Crear(Cliente modelo)
        {
            if (ModelState.IsValid)
            {               
                long ClienteId = new ClienteBL().GuardarML(modelo);

                if (ClienteId > 0)
                {
                    return Json(new { Operacion = true, Cliente = ClienteId }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [Permiso("Control.Cliente.Editar")]
        public ActionResult Editar(long id)
        {
            Cliente ClienteActual = new ClienteBL().ObtenerPorId(id, false);

            if (ClienteActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Cliente", "Editar");

            string strAtributo = "checked='checked'";

            ViewBag.vipSi = ClienteActual.Vip == true ? strAtributo : "";
            ViewBag.vipNo = ClienteActual.Vip == false ? strAtributo : "";

            ViewBag.activoSi = ClienteActual.Activo == true ? strAtributo : "";
            ViewBag.activoNo = ClienteActual.Activo == false ? strAtributo : "";

            return View(ClienteActual);
        }

        [Permiso("Control.Cliente.Editar")]
        [HttpPost]
        public ActionResult Editar(Cliente modelo, bool vip, bool activo)
        {
            if (ModelState.IsValid)
            {
                modelo.Vip = vip;
                modelo.Activo = activo;
                string strMensaje = new ClienteBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Cliente-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }

            }

            string strAtributo = "checked='checked'";

            ViewBag.vipSi = vip == true ? strAtributo : "";
            ViewBag.vipNo = vip == false ? strAtributo : "";

            ViewBag.activoSi = activo == true ? strAtributo : "";
            ViewBag.activoNo = activo == false ? strAtributo : "";

            return View(modelo);
        }

        [Permiso("Control.Cliente.Detalle")]
        public ActionResult Detalle(long id)
        {
            Cliente ClienteActual = new ClienteBL().ObtenerPorId(id, true);

            if (ClienteActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Cliente", "Detalle");

            return View(ClienteActual);
        }

        [ActionName("ObtenerDescuento")]
        public JsonResult ObtenerDescuento(long clienteId)
        {
            if (clienteId > 0)
            {
                return Json(new { Operacion = true, Data = new ClienteBL().ObtenerDescuentoPorId(clienteId) }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [Permiso("Control.Cliente.Crear")]
        [ActionName("ObtenerPorNit")]
        public JsonResult ObtenerPorNit(string nit)
        {
            if (string.IsNullOrWhiteSpace(nit))
            {
                return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
            }

            var cliente = new ClienteBL().ObtenerPorNit(nit);

            if (cliente == null)
            {
                return Json(new { Operacion = true, Data = (object)null }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { Operacion = true, Data = new { cliente.ClienteId, cliente.Nit, cliente.Nombre, cliente.Direccion, cliente.DPI, cliente.NoTelefono, cliente.EmailCliente, cliente.Vip, cliente.Activo } }, JsonRequestBehavior.AllowGet);
        }
    }
}