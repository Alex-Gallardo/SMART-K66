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

    public class LocalidadController : Controller
    {
        [Permiso("Control.Localidad.Listado")]
        public ActionResult Index(int? page, string search)
        {
            CustomHelper.setTitle("Localidad", "Listado");

            List<Localidad> Agencias = new List<Localidad>();

            try
            {
                if (!string.IsNullOrWhiteSpace(search) && search != null)
                {
                    Agencias = new LocalidadBL().Buscar(search).ToList();
                }
                else
                {
                    Agencias = new LocalidadBL().ObtenerListado(true);
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
            return View(Agencias.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Localidad.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Localidad", "Nuevo");

            string strAtributo = "checked='checked'";

            ViewBag.activoSi = strAtributo;
            ViewBag.activoNo = "";

            List<Municipio> prodo = new MunicipioBL().ObtenerListado(true).Where(x => x.Activo).ToList();
            var Agencias = new AgenciaBL().ObtenerListado(true, CustomHelper.getUserId());
          
            ViewBag.Agencias = new SelectList(Agencias, "AgenciaId", "Nombre");
            ViewBag.Municipios = new SelectList(prodo, "MunicipioId", "Nombre"); ;
            return View();
        }

        [HttpPost]
        [Permiso("Control.Localidad.Crear")]
        public ActionResult Crear(Localidad modelo, bool activo)
        {

            if (ModelState.IsValid)
            {
                string strMensaje = new LocalidadBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Localidad-Success"] = strMensaje;
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
            List<Municipio> prodo = new MunicipioBL().ObtenerListado(true).Where(x => x.Activo).ToList();
            var Agencias = new AgenciaBL().ObtenerListado(true, CustomHelper.getUserId());

            ViewBag.Agencias = new SelectList(Agencias, "AgenciaId", "Nombre");
            ViewBag.Municipios = new SelectList(prodo, "MunicipioId", "Nombre"); ;

            return View(modelo);
        }

        [Permiso("Control.Localidad.Editar")]
        public ActionResult Editar(long id)
        {
            Localidad AgenciaActual = new LocalidadBL().ObtenerPorId(id);

            if (AgenciaActual == null || AgenciaActual.MunicipioId == 0)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Localidad", "Editar");

            string strAtributo = "checked='checked'";

            ViewBag.activoSi = AgenciaActual.Activo == true ? strAtributo : "";
            ViewBag.activoNo = AgenciaActual.Activo == false ? strAtributo : "";

            List<Municipio> prodo = new MunicipioBL().ObtenerListado(true).Where(x => x.Activo).ToList();
            var Agencias = new AgenciaBL().ObtenerListado(true, CustomHelper.getUserId());

            ViewBag.Agencias = new SelectList(Agencias, "AgenciaId", "Nombre");
            ViewBag.Municipios = new SelectList(prodo, "MunicipioId", "Nombre"); ;

            return View(AgenciaActual);
        }

        [HttpPost]
        [Permiso("Control.Localidad.Editar")]
        public ActionResult Editar(Localidad modelo, bool activo)
        {

            if (ModelState.IsValid)
            {
                modelo.Activo = activo;
                string strMensaje = new LocalidadBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Localidad-Success"] = strMensaje;
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

            List<Municipio> prodo = new MunicipioBL().ObtenerListado(true).Where(x => x.Activo).ToList();
            var Agencias = new AgenciaBL().ObtenerListado(true, CustomHelper.getUserId());

            ViewBag.Agencias = new SelectList(Agencias, "AgenciaId", "Nombre");
            ViewBag.Municipios = new SelectList(prodo, "MunicipioId", "Nombre"); ;

            return View(modelo);
        }
    }
}