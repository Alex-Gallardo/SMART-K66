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
    public class VendedorController : Controller
    {
        #region Metodos Privados

            private void CargaControles()
            {
                var Agencias = new AgenciaBL().ObtenerListado(false, 0);              

                ViewBag.Agencias = new SelectList(Agencias, "AgenciaId", "Nombre");              
            }

        #endregion

        // GET: Vendedor
        [Permiso("Control.Vendedor.Ver_Listado")]
        public ActionResult Index(int? page, string search)
        {
            CustomHelper.setTitle("Vendedor", "Listado");

            List<Vendedor> Vendedors = new List<Vendedor>();

            try
            {
                if (!string.IsNullOrWhiteSpace(search) && search != null)
                {
                    Vendedors = new VendedorBL().Buscar(search).ToList();
                }
                else
                {
                    Vendedors = new VendedorBL().ObtenerListado(true).ToList();
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
            return View(Vendedors.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Vendedor.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Vendedor", "Nuevo");

            string strAtributo = "checked='checked'";

            ViewBag.activoSi = strAtributo;
            ViewBag.activoNo = "";

            this.CargaControles();
            return View();
        }

        [Permiso("Control.Vendedor.Crear")]
        [HttpPost]
        public ActionResult Crear(Vendedor modelo, long[] agenciaIds, bool activo)
        {
            if (agenciaIds == null || agenciaIds.Length == 0)
            {
                ModelState.AddModelError("", "El vendedor no contiene agencias asignadas");
            }

            if (ModelState.IsValid)
            {
                modelo.Agencias = new List<VendedorAgencia>();
                for (int i = 0; i < agenciaIds.Length; i++)
                {
                    VendedorAgencia Detalle = new VendedorAgencia();                   
                    Detalle.AgenciaId = agenciaIds[i];

                    modelo.Agencias.Add(Detalle);
                }

                modelo.Activo = activo;
                string strMensaje = new VendedorBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Vendedor-Success"] = strMensaje;
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

            ViewBag.agenciaIds = agenciaIds;

            this.CargaControles();
            return View(modelo);
        }

        [Permiso("Control.Vendedor.Editar")]
        public ActionResult Editar(long id)
        {
            Vendedor VendedorActual = new VendedorBL().ObtenerPorId(id, true);

            if (VendedorActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Vendedor", "Editar");

            string strAtributo = "checked='checked'";

            ViewBag.activoSi = VendedorActual.Activo == true ? strAtributo : "";
            ViewBag.activoNo = VendedorActual.Activo == false ? strAtributo : "";

            if (VendedorActual.Agencias != null && VendedorActual.Agencias.Count() > 0)
            {
                ViewBag.agenciaIds = VendedorActual.Agencias.Select(x => x.AgenciaId).ToList();
            }
            else
            {
                ViewBag.productoIds = 0;
            }

            this.CargaControles();
            return View(VendedorActual);
        }

        [Permiso("Control.Vendedor.Editar")]
        [HttpPost]
        public ActionResult Editar(Vendedor modelo, long[] agenciaIds, bool activo)
        {
            if (agenciaIds == null || agenciaIds.Length == 0)
            {
                ModelState.AddModelError("", "El vendedor no contiene agencias asignadas");
            }

            if (ModelState.IsValid)
            {
                modelo.Agencias = new List<VendedorAgencia>();
                for (int i = 0; i < agenciaIds.Length; i++)
                {
                    VendedorAgencia Detalle = new VendedorAgencia();
                    Detalle.VendedorId = modelo.VendedorId;
                    Detalle.AgenciaId = agenciaIds[i];

                    modelo.Agencias.Add(Detalle);
                }

                modelo.Activo = activo;
                string strMensaje = new VendedorBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Vendedor-Success"] = strMensaje;
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

            ViewBag.agenciaIds = agenciaIds;

            this.CargaControles();
            return View(modelo);
        }

        [Permiso("Control.Vendedor.Detalle")]
        public ActionResult Detalle(long id)
        {
            Vendedor VendedorActual = new VendedorBL().ObtenerPorId(id, true);

            if (VendedorActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Vendedor", "Detalle");

            return View(VendedorActual);
        }
    }
}