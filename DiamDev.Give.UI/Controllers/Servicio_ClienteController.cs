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

namespace DiamDev.Give.UI.Controllers
{
    [Authorize]
    [Seguridad]
    [HandleError]
    public class Servicio_ClienteController : Controller
    {
        // GET: Servicio_Cliente
        [Permiso("Control.Servicio_Cliente.Ver_Listado")]
        public ActionResult Index()
        {
            CustomHelper.setTitle("Servicio al Cliente", "Listado");

            List<ServicioCliente> Numeros = new List<ServicioCliente>();
            
            try
            {
                Numeros = new ServicioClienteBL().ObtenerListadoxFechayAgencia(DateTime.Today, CustomHelper.getAgenciaId());
            }
            catch (Exception ex)
            {
                ViewBag.Error = string.Format("Message: {0} StackTrace: {1}", ex.Message, ex.StackTrace);
                return View("~/Views/Shared/Error.cshtml");
            }

            return View(Numeros);
        }

        public ActionResult Atender(long id)
        {
            string strMensaje = new ServicioClienteBL().Atender(id, CustomHelper.getUserId());
            if (strMensaje.Equals("OK"))
            {
                return RedirectToAction("Crear_Correlativo", "Factura", new { @id = id });
            }
            else
            {
                ModelState.AddModelError("", strMensaje);
            }

            return RedirectToAction("Index");
        }
                
        [ActionName("ActualizarEstadoTicket")]
        public JsonResult ActualizarEstadoTicket(long ticketId)
        {
            if (ticketId > 0)
            {
                string Mensaje = new ServicioClienteBL().Anular(ticketId);
                if (Mensaje.Equals("OK"))
                {
                    return Json(new { Operacion = true }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }
    }
}