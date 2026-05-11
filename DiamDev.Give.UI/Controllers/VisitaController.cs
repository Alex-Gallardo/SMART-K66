using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using DiamDev.Give.BLL;
using DiamDev.Give.Entities;
using DiamDev.Give.UI.App_Start;
using OfficeOpenXml;

namespace DiamDev.Give.UI.Controllers
{
    [Authorize]
    [Seguridad]
    [HandleError]
    public class VisitaController : Controller
    {
        #region Metodos Privados   

        private List<Empresa> Empresas()
        {
            return new EmpresaBL().ObtenerListadoxUsuario(CustomHelper.getUserId());
        }

        private void CargaTipos()
        {
            var Tipos = new VisitaTipoBL().ObtenerListado(false);

            ViewBag.Tipos = new SelectList(Tipos, "TipoId", "Nombre");
        }

        private void CargaResponsables()
        {
            var Responsables = new UsuarioBL().ObtenerActivos();

            ViewBag.Responsables = new SelectList(Responsables, "UsuarioId", "Nombre");
        }

        #endregion

        // GET: Visita
        [Permiso("Control.Visita.Ver_Listado")]
        public ActionResult Index(DateTime? FechaInicial, DateTime? FechaFinal)
        {
            CustomHelper.setTitle("Visita", "Listado");
            List<Visita> Visitas = new List<Visita>();

            try
            {
                if (!FechaInicial.HasValue && !FechaFinal.HasValue)
                {
                    FechaInicial = DateTime.Today;
                    FechaFinal = DateTime.Today;
                }

                Visitas = new VisitaBL().ObtenerListado(FechaInicial.Value, FechaFinal.Value, CustomHelper.getUserId()).ToList();
            }
            catch (Exception)
            { }

            ViewBag.fechaInicial = FechaInicial.Value.ToString("yyyy-MM-dd");
            ViewBag.fechaFinal = FechaFinal.Value.ToString("yyyy-MM-dd");

            return View(Visitas);
        }

        [Permiso("Control.Visita_Monitoreo.Ver_Listado")]
        public ActionResult Monitoreo(DateTime? FechaInicial, DateTime? FechaFinal, long? Vendedor)
        {
            CustomHelper.setTitle("Visita", "Monitoreo");
            List<Visita> Visitas = new List<Visita>();

            try
            {
                if (!FechaInicial.HasValue && !FechaFinal.HasValue)
                {
                    FechaInicial = DateTime.Today;
                    FechaFinal = DateTime.Today;
                }

                if (Vendedor != null)
                {
                    ViewBag.Markers = new VisitaBL().ObtenerLocalizacionVisita(FechaInicial.Value, FechaFinal.Value, Vendedor.Value);
                    Visitas = new VisitaBL().ObtenerListado(FechaInicial.Value, FechaFinal.Value, Vendedor.Value).ToList();
                }
            }
            catch (Exception)
            { }

            ViewBag.fechaInicial = FechaInicial.Value.ToString("yyyy-MM-dd");
            ViewBag.fechaFinal = FechaFinal.Value.ToString("yyyy-MM-dd");

            this.CargaResponsables();
            return View(Visitas);
        }

        [Permiso("Control.Visita_Monitoreo.Ver_Listado")]
        public ActionResult Excel_General(DateTime? FechaInicial, DateTime? FechaFinal, long? Vendedor)
        {
            List<Visita> Visitas = new List<Visita>();
            Visitas = new VisitaBL().ObtenerListado(FechaInicial.Value, FechaFinal.Value, Vendedor.Value).ToList();

            if (Visitas == null)
            {
                return HttpNotFound();
            }

            if (Visitas.Count() == 0)
            {
                return HttpNotFound();
            }

            using (var pck = new ExcelPackage())
            {
                var ws = pck.Workbook.Worksheets.Add("VISITAS");
                ws.Cells["A1"].Value = "EMPRESA";
                ws.Cells["B1"].Value = "TIPO DE VISITA";
                ws.Cells["C1"].Value = "CLIENTE ID - K66";
                ws.Cells["D1"].Value = "CLIENTE";
                ws.Cells["E1"].Value = "DIRECCION";
                ws.Cells["F1"].Value = "FECHA";

                var fila = 1;
                foreach (var Detalle in Visitas)
                {
                    fila++;
                    ws.Cells[fila, 1].Value = (Detalle.Empresa == null ? "No Disponible" : Detalle.Empresa.Nombre).ToUpper();
                    ws.Cells[fila, 2].Value = (Detalle.TipoVisita == null ? "No Disponible" : Detalle.TipoVisita.Nombre).ToUpper();
                    ws.Cells[fila, 3].Value = Detalle.IDK66;
                    ws.Cells[fila, 4].Value = (Detalle.Nombre).ToUpper();
                    ws.Cells[fila, 5].Value = (Detalle.Direccion).ToUpper();
                    ws.Cells[fila, 6].Value = Detalle.Fecha.ToString("dd/MM/yyyy");
                }

                using (var range = ws.Cells[1, 1, fila, 6])
                {
                    range.AutoFitColumns();
                }

                return File(pck.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", string.Format("visita_fecha_inicial_{0}_fecha_final_{1}.xlsx", FechaInicial.Value.ToString("yyyyMMdd"), FechaFinal.Value.ToString("yyyyMMdd")));
            }
        }

        [Permiso("Control.Visita.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Visita", "Nueva");

            this.CargaTipos();
            return View(new Visita() { Empresas = Empresas() });
        }

        [Permiso("Control.Visita.Crear")]
        [HttpPost]
        public ActionResult Crear(Visita modelo, long[] empresasIds)
        {
            if (empresasIds == null || empresasIds.Length == 0)
            {
                ModelState.AddModelError("", "Para realizar una visita debe de seleccionar a una empresa");
            }
            else
            {
                for (int i = 0; i < empresasIds.Length; i++)
                {
                    if (empresasIds[i] == 20210705001)
                    {
                        modelo.EmpresaId = 20210705001;
                        modelo.Bolik = true;
                    }
                    else if (empresasIds[i] == 20210705002)
                    {
                        modelo.EmpresaId = 20210705002;
                        modelo.Empaques = true;
                    }
                    else if (empresasIds[i] == 20210705003)
                    {
                        modelo.EmpresaId = 20210705003;
                        modelo.Faes = true;
                    }
                    else if (empresasIds[i] == 20210705004)
                    {
                        modelo.EmpresaId = 20210705004;
                        modelo.Graco = true;
                    }
                }
            }

            if (ModelState.IsValid)
            {
                modelo.ResponsableId = CustomHelper.getUserId();
                string strMensaje = new VisitaBL().Guardar(modelo);
                if (strMensaje.Equals("OK"))
                {
                    TempData["Visita-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }
            
            this.CargaTipos();

            modelo.Empresas = new List<Empresa>();
            modelo.Empresas = Empresas();
            return View(modelo);
        }
    }
}