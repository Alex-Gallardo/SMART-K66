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
    public class SerieController : Controller
    {
        #region Metodos Privados

            private void CargaControles()
            {
                var Agencias = new AgenciaBL().ObtenerListado(false, 0);              

                ViewBag.Agencias = new SelectList(Agencias, "AgenciaId", "Nombre");              
            }

            private void CargaSeries()
            {
                var Series = new SerieBL().ObtenerSeriesPorAgencia(CustomHelper.getAgenciaId(), true);

                ViewBag.Series = new SelectList(Series, "SerieId", "Nombre");
            }

        #endregion

        // GET: Serie
        [Permiso("Control.Serie.Ver_Listado")]
        public ActionResult Index(int? page, string search)
        {
            CustomHelper.setTitle("Serie", "Listado");

            List<Serie> Series = new List<Serie>();

            try
            {
                if (!string.IsNullOrWhiteSpace(search) && search != null)
                {
                    Series = new SerieBL().Buscar(search).ToList();
                }
                else
                {
                    Series = new SerieBL().ObtenerListado(true).ToList();
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
            return View(Series.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Serie.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Serie", "Nueva");

            string strAtributo = "checked='checked'";

            ViewBag.activoSi = strAtributo;
            ViewBag.activoNo = "";

            this.CargaControles();
            return View();
        }

        [Permiso("Control.Serie.Crear")]
        [HttpPost]
        public ActionResult Crear(Serie modelo, long[] agenciaIds, bool activo)
        {
            if (agenciaIds == null || agenciaIds.Length == 0)
            {
                ModelState.AddModelError("", "La serie no contiene agencias asignadas");
            }

            if (ModelState.IsValid)
            {
                modelo.Agencias = new List<SerieAgencia>();
                for (int i = 0; i < agenciaIds.Length; i++)
                {
                    SerieAgencia Detalle = new SerieAgencia();                   
                    Detalle.AgenciaId = agenciaIds[i];

                    modelo.Agencias.Add(Detalle);
                }

                modelo.Activo = activo;
                string strMensaje = new SerieBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Serie-Success"] = strMensaje;
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

        [Permiso("Control.Serie.Editar")]
        public ActionResult Editar(long id)
        {
            Serie SerieActual = new SerieBL().ObtenerPorId(id, true);

            if (SerieActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Serie", "Editar");

            string strAtributo = "checked='checked'";

            ViewBag.activoSi = SerieActual.Activo == true ? strAtributo : "";
            ViewBag.activoNo = SerieActual.Activo == false ? strAtributo : "";

            if (SerieActual.Agencias != null && SerieActual.Agencias.Count() > 0)
            {
                ViewBag.agenciaIds = SerieActual.Agencias.Select(x => x.AgenciaId).ToList();
            }
            else
            {
                ViewBag.productoIds = 0;
            }

            this.CargaControles();
            return View(SerieActual);
        }

        [Permiso("Control.Serie.Editar")]
        [HttpPost]
        public ActionResult Editar(Serie modelo, long[] agenciaIds, bool activo)
        {
            if (agenciaIds == null || agenciaIds.Length == 0)
            {
                ModelState.AddModelError("", "La serie no contiene agencias asignadas");
            }

            if (ModelState.IsValid)
            {
                modelo.Agencias = new List<SerieAgencia>();
                for (int i = 0; i < agenciaIds.Length; i++)
                {
                    SerieAgencia Detalle = new SerieAgencia();
                    Detalle.SerieId = modelo.SerieId;
                    Detalle.AgenciaId = agenciaIds[i];

                    modelo.Agencias.Add(Detalle);
                }

                modelo.Activo = activo;
                string strMensaje = new SerieBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Serie-Success"] = strMensaje;
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

        [Permiso("Control.Serie.Detalle")]
        public ActionResult Detalle(long id)
        {
            Serie SerieActual = new SerieBL().ObtenerPorId(id, true);

            if (SerieActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Serie", "Detalle");

            return View(SerieActual);
        }

        [Permiso("Control.Correlativo_Serie.Crear")]
        public ActionResult Correlativo()
        {
            CustomHelper.setTitle("Correlativo de Serie", "Nuevo");
                     
            this.CargaSeries();
            return View();
        }

        [Permiso("Control.Correlativo_Serie.Crear")]
        [HttpPost]
        public ActionResult Correlativo(CorrelativoModel modelo)
        {           
            if (ModelState.IsValid)
            {

                string strMensaje = new SerieBL().GenerarCorrelativo(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Serie_Correlativo-Success"] = strMensaje;
                    return RedirectToAction("Correlativo");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }

            }
                
            this.CargaSeries();
            return View(modelo);
        }

        [ActionName("ObtenerAgenciasxSerie")]
        public JsonResult AgenciaListado(long id)
        {
            IList _result = new List<SelectListItem>();
            _result = new SerieBL().ObtenerAgenciasxSerieId(id).Select(m => new SelectListItem() { Text = m.Nombre, Value = m.AgenciaId.ToString() }).ToList();
            return Json(_result, JsonRequestBehavior.AllowGet);
        }
    }
}