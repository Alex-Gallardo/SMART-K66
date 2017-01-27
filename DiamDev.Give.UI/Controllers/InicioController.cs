using DiamDev.Give.BLL;
using DiamDev.Give.Entities;
using DiamDev.Give.UI.App_Start;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PagedList;

namespace DiamDev.Give.UI.Controllers
{
    [Authorize]
    [HandleError]
    public class InicioController : Controller
    {
        // GET: Inicio
        public ActionResult Dashboard()
        {
            CustomHelper.setTitle("Dashboard", "Inicio");
            return View();
        }

        public ActionResult Agencias(int? page, string search)
        {
            CustomHelper.setTitle("Agencias", "Listado");

            List<Agencia> Agencias = new List<Agencia>();

            try
            {
                if (!string.IsNullOrWhiteSpace(search) && search != null)
                {
                    Agencias = new AgenciaBL().Buscar(search, CustomHelper.getUserId()).ToList();
                }
                else
                {
                    Agencias = new AgenciaBL().ObtenerListado(true, CustomHelper.getUserId()).ToList();
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = string.Format("Message: {0} StackTrace: {1}", ex.Message, ex.StackTrace);
                return View("~/Views/Shared/Error.cshtml");
            }

            ViewBag.Search = search;

            int pageSize = 5;
            int pageNumber = (page ?? 1);
            return View(Agencias.ToPagedList(pageNumber, pageSize));
        }

        [HttpPost]
        public ActionResult Agencias(long? agenciaId)
        {
            if (agenciaId.HasValue)
            {
                Agencia AgenciaActual = new AgenciaBL().ObtenerPorId(agenciaId.Value);

                if (AgenciaActual != null)
                {
                    CustomHelper.setAgencia(AgenciaActual);

                    return RedirectToAction("Dashboard", "Inicio");
                }
            }
            else
            {
                ModelState.AddModelError("", "Debe seleccionar una agencia");
                return RedirectToAction("Agencias", "Inicio");
            }

            return View();
        }
    }
}