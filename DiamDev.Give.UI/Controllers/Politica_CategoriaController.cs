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
    public class Politica_CategoriaController : Controller
    {
        #region Metodos Privados

            private void CargaControles()
            {
                var Tipos = new PoliticaTipoBL().ObtenerListado();

                ViewBag.Tipos = new SelectList(Tipos, "PoliticaTipoId", "Nombre");
            }

        #endregion

        // GET: Politica_Categoria
        [Permiso("Control.Politica_Categoria.Ver_Listado")]
        public ActionResult Index(int? page, string search)
        {
            CustomHelper.setTitle("Política Categoría", "Listado");

            List<PoliticaCategoria> PoliticaCategorias = new List<PoliticaCategoria>();

            try
            {
                if (!string.IsNullOrWhiteSpace(search) && search != null)
                {
                    PoliticaCategorias = new PoliticaCategoriaBL().Buscar(search).ToList();
                }
                else
                {
                    PoliticaCategorias = new PoliticaCategoriaBL().ObtenerListado(false).ToList();
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
            return View(PoliticaCategorias.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Politica_Categoria.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Política Categoría", "Nueva");

            string strAtributo = "checked='checked'";

            ViewBag.ActivoSi = strAtributo;
            ViewBag.ActivoNo = "";

            this.CargaControles();
            return View();
        }

        [Permiso("Control.Politica_Categoria.Crear")]
        [HttpPost]
        public ActionResult Crear(PoliticaCategoria modelo, bool activo, long[] politicaIdIds, string[] politicaIds)
        {
            if (politicaIdIds == null || politicaIdIds.Length == 0)
            {
                ModelState.AddModelError("", "Para crear una politica categoria debe de asignar politicas");
            }

            if (politicaIdIds != null && politicaIdIds.Count() > 0)
            {
                modelo.Politicas = new List<PoliticaCategoriaPolitica>();
                for (int i = 0; i < politicaIdIds.Length; i++)
                {
                    PoliticaCategoriaPolitica Detalle = new PoliticaCategoriaPolitica();
                    Detalle.PoliticaId = politicaIdIds[i];
                    modelo.Politicas.Add(Detalle);
                }
            }

            if (ModelState.IsValid)
            {
                modelo.Activo = activo;
                string strMensaje = new PoliticaCategoriaBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Politica_Categoria-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }

            }

            string strAtributo = "checked='checked'";

            ViewBag.ActivoSi = activo == true ? strAtributo : "";
            ViewBag.ActivoNo = activo == false ? strAtributo : "";

            ViewBag.politicaIdIds = politicaIdIds;
            ViewBag.politicaIds = politicaIds;

            this.CargaControles();
            return View(modelo);
        }

        [Permiso("Control.Politica_Categoria.Editar")]
        public ActionResult Editar(long id)
        {
            PoliticaCategoria PoliticaCategoriaActual = new PoliticaCategoriaBL().ObtenerPorId(id, true);

            if (PoliticaCategoriaActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Política Categoría", "Editar");

            string strAtributo = "checked='checked'";

            ViewBag.ActivoSi = PoliticaCategoriaActual.Activo == true ? strAtributo : "";
            ViewBag.ActivoNo = PoliticaCategoriaActual.Activo == false ? strAtributo : "";

            if (PoliticaCategoriaActual.Politicas != null && PoliticaCategoriaActual.Politicas.Count() > 0)
            {
                ViewBag.politicaIdIds = PoliticaCategoriaActual.Politicas.Select(x => x.PoliticaId);
                ViewBag.politicaIds = PoliticaCategoriaActual.Politicas.Select(x => x.Politica.Nombre);
            }
            else
            {
                ViewBag.politicaIdIds = 0;
                ViewBag.politicaIds = "";
            }

            this.CargaControles();
            return View(PoliticaCategoriaActual);
        }

        [Permiso("Control.Politica_Categoria.Editar")]
        [HttpPost]
        public ActionResult Editar(PoliticaCategoria modelo, bool activo, long[] politicaIdIds, string[] politicaIds)
        {
            if (politicaIdIds == null || politicaIdIds.Length == 0)
            {
                ModelState.AddModelError("", "Para crear una politica categoria debe de asignar politicas");
            }

            if (politicaIdIds != null && politicaIdIds.Count() > 0)
            {
                modelo.Politicas = new List<PoliticaCategoriaPolitica>();
                for (int i = 0; i < politicaIdIds.Length; i++)
                {
                    PoliticaCategoriaPolitica Detalle = new PoliticaCategoriaPolitica();
                    Detalle.PoliticaId = politicaIdIds[i];
                    modelo.Politicas.Add(Detalle);
                }
            }

            if (ModelState.IsValid)
            {
                modelo.Activo = activo;
                string strMensaje = new PoliticaCategoriaBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Politica_Categoria-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }

            string strAtributo = "checked='checked'";

            ViewBag.ActivoSi = activo == true ? strAtributo : "";
            ViewBag.ActivoNo = activo == false ? strAtributo : "";

            ViewBag.politicaIdIds = politicaIdIds;
            ViewBag.politicaIds = politicaIds;

            this.CargaControles();
            return View(modelo);
        }
    }
}