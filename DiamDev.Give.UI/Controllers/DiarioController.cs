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
    public class DiarioController : Controller
    {
        #region Metodos Privados

            private void CargaControles()
            {
                var Agencias = new AgenciaBL().ObtenerListado(false, 0);
                var Cuentas = new CuentaContableBL().ObtenerCuentas();

                ViewBag.Agencias = new SelectList(Agencias, "AgenciaId", "Nombre");
                ViewBag.Cuentas = new SelectList(Cuentas, "CuentaId", "Nombre");
            }

        #endregion

        // GET: Diario
        [Permiso("Control.Diario.Ver_Listado")]
        public ActionResult Index(DateTime? FechaInicial, DateTime? FechaFinal)
        {
            CustomHelper.setTitle("Diario", "Listado");

            List<Diario> Diarios = new List<Diario>();

            if (!FechaInicial.HasValue && !FechaFinal.HasValue)
            {
                FechaInicial = DateTime.Today;
                FechaFinal = DateTime.Today;
            }

            try
            {
                Diarios = new DiarioBL().ObtenerListado(FechaInicial.Value, FechaFinal.Value).ToList();
            }
            catch (Exception)
            {
            }

            return View(Diarios);
        }

        [Permiso("Control.Diario.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Diario", "Nuevo");

            this.CargaControles();
            return View();
        }

        [Permiso("Control.Diario.Crear")]
        [HttpPost]
        public ActionResult Crear(Diario modelo, long[] agenciasIds, long[] cuentaIds, decimal[] debeIds, decimal[] haberIds)
        {
            if (cuentaIds == null || cuentaIds.Length == 0)
            {
                ModelState.AddModelError("", "Debe seleccionar una cuenta");
            }

            modelo.UsrCreo = CustomHelper.getUserId();

            if (ModelState.IsValid)
            {
                modelo.Detalles = new List<DiarioDetalle>();
                for (int i = 0; i < cuentaIds.Length; i++)
                {
                    DiarioDetalle Detalle = new DiarioDetalle();
                    Detalle.CuentaId = cuentaIds[i];
                    Detalle.Debe = debeIds[i];
                    Detalle.Haber = haberIds[i];

                    modelo.Detalles.Add(Detalle);
                }

                modelo.Agencias = new List<DiarioAgencia>();
                if (agenciasIds != null && agenciasIds.Count() > 0)
                {
                    for (int i = 0; i < agenciasIds.Length; i++)
                    {
                        modelo.Agencias.Add(new DiarioAgencia() { AgenciaId = agenciasIds[i] });
                    }
                }

                string strMensaje = new DiarioBL().Guardar(modelo);
                if (strMensaje.Equals("OK"))
                {
                    TempData["Diario-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }

            ViewBag.cuentaIds = cuentaIds;
            ViewBag.debeIds = debeIds;
            ViewBag.haberIds = haberIds;

            this.CargaControles();
            return View(modelo);
        }

        [Permiso("Control.Diario.Detalle")]
        public ActionResult Detalle(long id)
        {
            Diario DiarioActual = new DiarioBL().ObtenerPorId(id, true);

            if (DiarioActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Diario", "Detalle");

            return View(DiarioActual);
        }
    }
}