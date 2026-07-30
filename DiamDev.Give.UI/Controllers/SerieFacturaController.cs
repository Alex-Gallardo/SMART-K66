using DiamDev.Give.BLL;
using DiamDev.Give.Entities;
using DiamDev.Give.UI.App_Start;
using DiamDev.Give.UI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DiamDev.Give.UI.Controllers
{
    [Authorize]
    [Seguridad]
    [HandleError]
    public class SerieFacturaController : Controller
    {
        #region Metodos Privados

        private void CargaControles()
        {
            var Agencias = new AgenciaBL().ObtenerListado(false, 0);

            var Series = new SerieBL().ObtenerListado(true);

            ViewBag.Agencias = new SelectList(Agencias, "AgenciaId", "Nombre");
            ViewBag.Serie = new SelectList(Series, "SerieId", "Nombre");
        }

        #endregion

        // GET: SerieFactura
        [Permiso("Control.Serie.Ver_Listado")]
        public ActionResult Index()
        {
            CustomHelper.setTitle("Serie", "Listado");

            List<Serie> Series = new List<Serie>();

           
            return View();
        }

        //public ActionResult Crear(SerieAgenciaFacturaModel modelo)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        modelo.Operada = true;
                
        //        var model = new SerieAgenciaFactura { Serie = modelo.Serie, Agencia = modelo.Agencia, Factura = modelo.Factura, Operada = modelo.Operada };

        //        string mensaje = new SerieAgenciaFacturaBL().Guardar(model);
        //    }
        //}
    }
}