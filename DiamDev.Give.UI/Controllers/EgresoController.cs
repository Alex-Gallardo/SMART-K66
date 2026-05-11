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
using System.Data;
using Microsoft.Reporting.WebForms;
using DiamDev.Give.DAL;
using System.Data.Entity;

namespace DiamDev.Give.UI.Controllers
{
    [Authorize]
    [Seguridad]
    [HandleError]
    public class EgresoController : Controller
    {
        #region Metodos Privados

            private void CargaControles()
            {
                var Agencias = new AgenciaBL().ObtenerListado(false, 0);
           
                ViewBag.Agencias = new SelectList(Agencias, "AgenciaId", "Nombre");          
            }

        #endregion

        // GET: Egreso
        [Permiso("Control.Egreso.Ver_Listado")]
        public ActionResult Index(DateTime? FechaInicial, DateTime? FechaFinal)
        {
            CustomHelper.setTitle("Egreso", "Listado");

            List<Egreso> Egresos = new List<Egreso>();

            if (!FechaInicial.HasValue && !FechaFinal.HasValue)
            {
                FechaInicial = DateTime.Today;
                FechaFinal = DateTime.Today;
            }

            try
            {
                Egresos = new EgresoBL().ObtenerListado(FechaInicial.Value, FechaFinal.Value).ToList();
            }
            catch (Exception ex)
            {
                ViewBag.Error = string.Format("Message: {0} StackTrace: {1}", ex.Message, ex.StackTrace);
                return View("~/Views/Shared/Error.cshtml");
            }

            return View(Egresos);
        }

        [Permiso("Control.Egreso.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Egreso", "Nuevo");

            this.CargaControles();
            return View();
        }

        [Permiso("Control.Egreso.Crear")]
        [HttpPost]
        public ActionResult Crear(Egreso modelo, string[] productoIds, long[] presentacionIds, decimal[] cantidadIds, string[] idIds, decimal[] precioIds)
        {
            if (productoIds == null || productoIds.Length == 0)
            {
                ModelState.AddModelError("", "Para realizar un egreso debe de asignar productos");
            }
            else
            {
                modelo.Detalles = new List<EgresoDetalle>();
                for (int i = 0; i < productoIds.Length; i++)
                {
                    EgresoDetalle Detalle = new EgresoDetalle();
                    Detalle.ProductoId = productoIds[i];
                    Detalle.UnidadId = presentacionIds[i];
                    Detalle.Cantidad = cantidadIds[i];
                    Detalle.ID = idIds[i];
                    Detalle.PrecioCosto = precioIds[i];

                    modelo.Detalles.Add(Detalle);
                }
            }

            if (ModelState.IsValid)
            {
                modelo.UsrInicial = CustomHelper.getUserId();
                string strMensaje = new EgresoBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Egreso-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }

            }

            ViewBag.productoIds = productoIds;
            ViewBag.presentacionIds = presentacionIds;
            ViewBag.cantidadIds = cantidadIds;
            ViewBag.idIds = idIds;
            ViewBag.precioIds = precioIds;

            this.CargaControles();
            return View(modelo);
        }

        [Permiso("Control.Egreso.Detalle")]
        public ActionResult Detalle(long id)
        {
            Egreso EgresoActual = new EgresoBL().ObtenerPorId(id, true);

            if (EgresoActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Egreso", "Detalle");

            return View(EgresoActual);
        }
    }
}