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
            var Agencias = new AgenciaBL().ObtenerListadoPorUsuario(CustomHelper.getUserId());
            var Meses = new MesBL().ObtenerListado();

            ViewBag.Agencias = new SelectList(Agencias, "AgenciaId", "Nombre");
            ViewBag.Meses = new SelectList(Meses, "MesId", "Nombre");
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
                    Vendedors = new VendedorBL().Buscar(search, CustomHelper.getEmpresaId()).ToList();
                }
                else
                {
                    Vendedors = new VendedorBL().ObtenerListado(true, CustomHelper.getEmpresaId()).ToList();
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

        [Permiso("Control.Vendedor.Metas")]
        public ActionResult Metas()
        {
            CustomHelper.setTitle("Ventas", "Metas");
            MetaModel Metas = new MetaModel();

            try
            {
                Metas = new VendedorBL().ObtenerVentaYMetaxVendedor(DateTime.Today, CustomHelper.getUserId());
            }
            catch (Exception)
            { }

            return View(Metas);
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
        public ActionResult Crear(Vendedor modelo, long[] agenciaIds, decimal[] inicioIds, decimal[] finIds, decimal[] porcentajeIds, int[] mesIds, int[] anioIds, decimal[] metaMensualIds, decimal[] metaMensualRealIds, string[] fechaIds, decimal[] metaxdiaIds, bool activo)
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

                if (inicioIds != null && inicioIds.Count() > 0)
                {
                    modelo.Escalas = new List<VendedorEscala>();
                    for (int i = 0; i < inicioIds.Length; i++)
                    {
                        VendedorEscala Detalle = new VendedorEscala();
                        Detalle.Inicio = inicioIds[i];
                        Detalle.Fin = finIds[i];
                        Detalle.Porcentaje = porcentajeIds[i];

                        modelo.Escalas.Add(Detalle);
                    }                    
                }

                if (mesIds != null && mesIds.Count() > 0)
                {
                    modelo.Metas = new List<VendedorMeta>();
                    for (int i = 0; i < mesIds.Length; i++)
                    {
                        VendedorMeta Detalle = new VendedorMeta();
                        Detalle.MesId = mesIds[i];
                        Detalle.Anio = anioIds[i];
                        Detalle.MontoMensualMeta = metaMensualIds[i];
                        Detalle.MontoMensualReal = metaMensualRealIds[i];

                        modelo.Metas.Add(Detalle);
                    }
                }

                if (fechaIds != null && fechaIds.Count() > 0)
                {
                    modelo.MetasxDia = new List<VendedorMetaxDia>();
                    for (int i = 0; i < fechaIds.Length; i++)
                    {
                        VendedorMetaxDia Detalle = new VendedorMetaxDia();
                        Detalle.Fecha = DateTime.Parse(fechaIds[i]);                    
                        Detalle.MontoxDia = metaxdiaIds[i];
                     
                        modelo.MetasxDia.Add(Detalle);
                    }
                }

                modelo.Activo = activo;

                modelo.ResponsableId = CustomHelper.getUserId();
                modelo.EmpresaId = CustomHelper.getEmpresaId();

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
            ViewBag.inicioIds = inicioIds;
            ViewBag.finIds = finIds;
            ViewBag.porcentajeIds = porcentajeIds;

            ViewBag.mesIds = mesIds;
            ViewBag.anioIds = anioIds;
            ViewBag.metaMensualIds = metaMensualIds;
            ViewBag.metaMensualRealIds = metaMensualRealIds;

            ViewBag.fechaIds = fechaIds;
            ViewBag.metaxdiaIds = metaxdiaIds;

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
                ViewBag.agenciaIds = 0;
            }

            if (VendedorActual.Escalas != null && VendedorActual.Escalas.Count() > 0)
            {
                ViewBag.inicioIds = VendedorActual.Escalas.OrderBy(x => x.EscalaId).Select(x => x.Inicio).ToList();
                ViewBag.finIds = VendedorActual.Escalas.OrderBy(x => x.EscalaId).Select(x => x.Fin).ToList();
                ViewBag.porcentajeIds = VendedorActual.Escalas.OrderBy(x => x.EscalaId).Select(x => x.Porcentaje).ToList();
            }
            else
            {
                ViewBag.inicioIds = 0;
                ViewBag.finIds = 0;
                ViewBag.porcentajeIds = 0;
            }

            if (VendedorActual.Metas != null && VendedorActual.Metas.Count() > 0)
            {
                ViewBag.mesIds = VendedorActual.Metas.Select(x => x.MesId);
                ViewBag.anioIds = VendedorActual.Metas.Select(x => x.Anio);
                ViewBag.metaMensualIds = VendedorActual.Metas.Select(x => x.MontoMensualMeta);
                ViewBag.metaMensualRealIds = VendedorActual.Metas.Select(x => x.MontoMensualReal);
            }
            else
            {
                ViewBag.mesIds = 0;
                ViewBag.anioIds = 0;
                ViewBag.metaMensualIds = 0;
                ViewBag.metaMensualRealIds = 0;
            }

            if (VendedorActual.MetasxDia != null && VendedorActual.MetasxDia.Count() > 0)
            {
                ViewBag.fechaIds = VendedorActual.MetasxDia.Select(x => x.Fecha.ToString("yyyy-MM-dd"));
                ViewBag.metaxdiaIds = VendedorActual.MetasxDia.Select(x => x.MontoxDia);
            }
            else
            {
                ViewBag.fechaIds = 0;
                ViewBag.metaxdiaIds = 0;             
            }

            this.CargaControles();
            return View(VendedorActual);
        }

        [Permiso("Control.Vendedor.Editar")]
        [HttpPost]
        public ActionResult Editar(Vendedor modelo, long[] agenciaIds, decimal[] inicioIds, decimal[] finIds, decimal[] porcentajeIds, int[] mesIds, int[] anioIds, decimal[] metaMensualIds, decimal[] metaMensualRealIds, string[] fechaIds, decimal[] metaxdiaIds, bool activo)
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

                if (inicioIds != null && inicioIds.Count() > 0)
                {
                    modelo.Escalas = new List<VendedorEscala>();
                    for (int i = 0; i < inicioIds.Length; i++)
                    {
                        VendedorEscala Detalle = new VendedorEscala();
                        Detalle.Inicio = inicioIds[i];
                        Detalle.Fin = finIds[i];
                        Detalle.Porcentaje = porcentajeIds[i];

                        modelo.Escalas.Add(Detalle);
                    }
                }

                if (mesIds != null && mesIds.Count() > 0)
                {
                    modelo.Metas = new List<VendedorMeta>();
                    for (int i = 0; i < mesIds.Length; i++)
                    {
                        VendedorMeta Detalle = new VendedorMeta();
                        Detalle.MesId = mesIds[i];
                        Detalle.Anio = anioIds[i];
                        Detalle.MontoMensualMeta = metaMensualIds[i];
                        Detalle.MontoMensualReal = metaMensualRealIds[i];

                        modelo.Metas.Add(Detalle);
                    }
                }

                if (fechaIds != null && fechaIds.Count() > 0)
                {
                    modelo.MetasxDia = new List<VendedorMetaxDia>();
                    for (int i = 0; i < fechaIds.Length; i++)
                    {
                        VendedorMetaxDia Detalle = new VendedorMetaxDia();
                        Detalle.Fecha = DateTime.Parse(fechaIds[i]);
                        Detalle.MontoxDia = metaxdiaIds[i];

                        modelo.MetasxDia.Add(Detalle);
                    }
                }

                modelo.Activo = activo;

                modelo.ResponsableId = CustomHelper.getUserId();
                modelo.EmpresaId = CustomHelper.getEmpresaId();

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
            ViewBag.inicioIds = inicioIds;
            ViewBag.finIds = finIds;
            ViewBag.porcentajeIds = porcentajeIds;

            ViewBag.mesIds = mesIds;
            ViewBag.anioIds = anioIds;
            ViewBag.metaMensualIds = metaMensualIds;
            ViewBag.metaMensualRealIds = metaMensualRealIds;

            ViewBag.fechaIds = fechaIds;
            ViewBag.metaxdiaIds = metaxdiaIds;

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