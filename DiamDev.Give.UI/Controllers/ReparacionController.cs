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
using System.Data;
using Microsoft.Reporting.WebForms;

namespace DiamDev.Give.UI.Controllers
{
    [Authorize]
    [Seguridad]
    [HandleError]
    public class ReparacionController : Controller
    {
        #region Metodos Privados

            private void CargaControles()
            {
                var Servicios = new ServicioBL().ObtenerListado(false);

                ViewBag.Servicios = new SelectList(Servicios, "ServicioId", "Nombre");
               
                this.CargaAgencias();
            }           

            private void CargaFormas()
            {
                var Formas = new FormaPagoBL().ObtenerListado(false);
              
                ViewBag.Formas = new SelectList(Formas, "FormaPagoId", "Nombre");               
            }

            private void CargaAgencias()
            {
                var Agencias = new AgenciaBL().ObtenerListadoPorUsuario(CustomHelper.getUserId());

                ViewBag.Agencias = new SelectList(Agencias, "AgenciaId", "Nombre");
            }           

            private void CargaPoliticas()
            {
                var Politicas = new PoliticaCategoriaBL().ObtenerListado(true);

                ViewBag.Politicas = new SelectList(Politicas, "PoliticaCategoriaId", "Nombre");
            }

            private byte[] GetReportBytes(string reportPath, DataSet reportDataSource, decimal pageWidth = 13.38m, decimal pageHeight = 8.5m, decimal MarginLeft = 1m, decimal MarginRight = 1m)
            {

                byte[] reportBytes = null;

                // Se crea la instancia del reporte y se cargan sus datos.
                LocalReport reporte = new LocalReport() { ReportPath = reportPath };
                reporte.DataSources.Add(new ReportDataSource("ReparacionEncabezado", reportDataSource.Tables[0]));
                reporte.DataSources.Add(new ReportDataSource("ReparacionServicio", reportDataSource.Tables[1]));
                reporte.DataSources.Add(new ReportDataSource("ReparacionRepuesto", reportDataSource.Tables[2]));

                string deviceInfo =
                    "<DeviceInfo>" +
                    "  <OutputFormat>PDF</OutputFormat>" + // Formato del documento PDF
                    "  <PageWidth>" + pageWidth + "in</PageWidth>" + // Ancho de 8.5 pulgadas para paginas oficio
                    "  <PageHeight>" + pageHeight + "in</PageHeight>" + // Alto de 13.38 pulgadas para paginas oficio
                    "  <MarginTop>0.5in</MarginTop>" + // margen superior de 0.5 pulgadas
                    "  <MarginLeft>" + MarginLeft + "</MarginLeft>" + // margen izquierdo de 1 pulgada
                    "  <MarginRight>" + MarginRight + "</MarginRight>" + // margen derecho de 1 pulgada.
                    "  <MarginBottom>0.5in</MarginBottom>" + // margen inferior de 0.5 pulgadas.
                    "</DeviceInfo>";

                string mimeType;
                string encoding;
                string fileNameExtension;
                Warning[] warnings;
                string[] streams;

                // Se renderiza el reporte.
                reportBytes = reporte.Render("PDF",
                    deviceInfo,
                    out mimeType,
                    out encoding,
                    out fileNameExtension,
                    out streams,
                    out warnings);

                return reportBytes;

            }

        #endregion

        #region Metodos Publicos

            public FileResult Preview(int Id, long DocumentoId)
            {
                ReparacionFotografia FotografiaActual = new ReparacionBL().Fotografia(Id, DocumentoId);

                var content = Binario.Drawing.ImageManager.GetThumbnail(FotografiaActual.Content, 100);
                return File(content, FotografiaActual.ContentType);
            }

            public FileResult Imagen(int Id, long DocumentoId)
            {
                ReparacionFotografia FotografiaActual = new ReparacionBL().Fotografia(Id, DocumentoId);

                return File(FotografiaActual.Content, FotografiaActual.ContentType);
            }

        #endregion

        // GET: Reparacion
        [Permiso("Control.Reparacion.Ver_Listado")]
        public ActionResult Index(int? page, string search)
        {
            CustomHelper.setTitle("Reparación", "Listado");

            List<Reparacion> Reparaciones = new List<Reparacion>();
                      
            try
            {
                
                if (!string.IsNullOrWhiteSpace(search) && search != null)
                {
                    Reparaciones = new ReparacionBL().Buscar(search, CustomHelper.getUserId()).ToList();
                }
                else
                {
                    Reparaciones = new ReparacionBL().ObtenerListadoPorFecha(CustomHelper.getUserId(), DateTime.Today, DateTime.Today).ToList();
                }
            }
            catch (Exception)
            {
            }

            ViewBag.Search = search;

            return View(Reparaciones);
        }

        [Permiso("Control.Reparacion.Ver_Listado_Sin_Asignar")]
        public ActionResult SinAsignar()
        {
            CustomHelper.setTitle("Reparación Sin Asignar", "Listado");

            List<Reparacion> Reparaciones = new List<Reparacion>();

            try
            {
                Reparaciones = new ReparacionBL().ObtenerListadoPorUsuarioYDepartamento(CustomHelper.getUserId(), CustomHelper.getDepartamentoId(), 1).ToList();
            }
            catch (Exception)
            {
            }

            return View(Reparaciones);
        }

        [Permiso("Control.Reparacion.Ver_Listado_Asignadas")]
        public ActionResult Asignadas()
        {
            CustomHelper.setTitle("Reparación Asignadas", "Listado");

            List<Reparacion> Reparaciones = new List<Reparacion>();

            try
            {
                Reparaciones = new ReparacionBL().ObtenerListadoPorUsuarioYDepartamento(CustomHelper.getUserId(), CustomHelper.getDepartamentoId(), 2, false).ToList();
            }
            catch (Exception)
            {
            }

            return View(Reparaciones);
        }

        [Permiso("Control.Reparacion.Ver_Listado_Asignadas")]
        public ActionResult Historial_Reparacion_x_Tecnico(DateTime? FechaInicial, DateTime? FechaFinal)
        {
            CustomHelper.setTitle("Historial de Reparación x Tecnico", "Listado");

            List<HistorialReparacion> Reparaciones = new List<HistorialReparacion>();

            try
            {
                if (!FechaInicial.HasValue && !FechaFinal.HasValue)
                {
                    FechaInicial = DateTime.Today;
                    FechaFinal = DateTime.Today;
                }

                Reparaciones = new ReparacionBL().ObtenerHistorialReparacionxTecnicoFecha(CustomHelper.getUserId(), FechaInicial.Value, FechaFinal.Value).ToList();
            }
            catch (Exception)
            {
            }

            return View(Reparaciones);
        }

        [Permiso("Control.Reparacion.Ver_Listado_Entregas")]
        public ActionResult Entregas()
        {
            CustomHelper.setTitle("Reparación Entregas", "Listado");

            List<Reparacion> Reparaciones = new List<Reparacion>();

            try
            {
                Reparaciones = new ReparacionBL().ObtenerListadoPorUsuarioYDepartamento(CustomHelper.getUserId(), CustomHelper.getDepartamentoId(), 6, false).ToList();
            }
            catch (Exception)
            {
            }

            return View(Reparaciones);
        }

        [Permiso("Control.Reparacion.Ver_Listado_Entregas_2-5")]
        public ActionResult Entregas_de_2_a_5()
        {
            CustomHelper.setTitle("Reparación Entregas de 2 a 5 Meses", "Listado");

            List<Reparacion> Reparaciones = new List<Reparacion>();

            try
            {
                Reparaciones = new ReparacionBL().ObtenerListadoPorUsuarioYDepartamento(CustomHelper.getUserId(), CustomHelper.getDepartamentoId(), 3, false, true).ToList();
            }
            catch (Exception)
            {
            }

            return View(Reparaciones);
        }

        [Permiso("Control.Reparacion.Ver_Listado_Entregas_6")]
        public ActionResult Entregas_de_6()
        {
            CustomHelper.setTitle("Reparación Entregas de 6 Meses en Adelante", "Listado");

            List<Reparacion> Reparaciones = new List<Reparacion>();

            try
            {
                Reparaciones = new ReparacionBL().ObtenerListadoPorUsuarioYDepartamento(CustomHelper.getUserId(), CustomHelper.getDepartamentoId(), 3, false, false, true).ToList();
            }
            catch (Exception)
            {
            }

            return View(Reparaciones);
        }

        [Permiso("Control.Reparacion.Ver_Listado")]
        public ActionResult Reparacion_Liquidar(int? page, long? agenciaId)
        {
            CustomHelper.setTitle("Reparación Liquidar", "Listado");

            List<Reparacion> Reparaciones = new List<Reparacion>();

            try
            {
                if (agenciaId == null)
                {
                    agenciaId = 0;
                }

                Reparaciones = new ReparacionBL().ObtenerListadoxEstados(agenciaId.Value, CustomHelper.getUserId()).ToList();
            }
            catch (Exception)
            {
            }

            ViewBag.AgenciaId = agenciaId;
            this.CargaAgencias();

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View(Reparaciones.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Reparacion.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Reparación", "Nuevo");

            this.CargaControles();
            this.CargaPoliticas();
            return View();
        }

        [Permiso("Control.Reparacion.Crear")]
        [HttpPost]
        public ActionResult Crear(Reparacion modelo, long[] servicioIds, string[] notaIds, string[] estadoIds, string[] productoIds, int[] cantidadIds, decimal[] precioIds, int descuento, ArchivoModel[] archivos, long[] politicaIdIds, string[] politicaIds)
        {
            if (servicioIds == null || servicioIds.Length == 0)
            {
                ModelState.AddModelError("", "Se necesita la evaluación del equipo");
            }

            modelo.Descuento = descuento;

            if (politicaIdIds == null || politicaIdIds.Length == 0)
            {
                ModelState.AddModelError("", "Para crear una reparación debe de asignar politicas");
            }

            if (ModelState.IsValid)
            {

                if (servicioIds != null && servicioIds.Count() > 0)
                {
                    modelo.Servicios = new List<ReparacionServicio>();
                    for (int i = 0; i < servicioIds.Length; i++)
                    {
                        ReparacionServicio Servicio = new ReparacionServicio();
                        Servicio.ServicioId = servicioIds[i];
                        Servicio.Nota = notaIds[i];
                        Servicio.Estado = estadoIds[i] == "Estado Bueno" ? true : false;
                        modelo.Servicios.Add(Servicio);
                    }
                }

                if (productoIds != null && productoIds.Count() > 0)
                {
                    modelo.Piezas = new List<ReparacionPieza>();
                    for (int i = 0; i < productoIds.Length; i++)
                    {
                        ReparacionPieza Detalle = new ReparacionPieza();
                        Detalle.ProductoId = productoIds[i];
                        Detalle.Cantidad = cantidadIds[i];
                        Detalle.Precio = precioIds[i];

                        modelo.CostoServicio += cantidadIds[i] * precioIds[i];

                        modelo.Piezas.Add(Detalle);
                    }
                }

                if (archivos != null && archivos.Count() > 0)
                {
                    modelo.Imagenes = new List<ReparacionFotografia>();
                    foreach (ArchivoModel archivo in archivos)
                    {
                        byte[] FileData = new byte[archivo.Archivo.ContentLength + 1];
                        archivo.Archivo.InputStream.Read(FileData, 0, archivo.Archivo.ContentLength);
                        modelo.Imagenes.Add(new ReparacionFotografia() { Nombre = archivo.Archivo.FileName, Content = FileData, ContentType = archivo.Archivo.ContentType, Length = archivo.Archivo.ContentLength });
                    }
                }

                if (politicaIdIds != null && politicaIdIds.Count() > 0)
                {
                    modelo.Politicas = new List<ReparacionPoliticaCategoria>();
                    for (int i = 0; i < politicaIdIds.Length; i++)
                    {
                        ReparacionPoliticaCategoria Detalle = new ReparacionPoliticaCategoria();
                        Detalle.PoliticaCategoriaId = politicaIdIds[i];
                        modelo.Politicas.Add(Detalle);
                    }
                }

                modelo.TipoId = 1;
                modelo.EstadoId = 1;                
                modelo.DepartamentoId = 20151023002;
                modelo.UsrCreo = CustomHelper.getUserId();

                string strMensaje = new ReparacionBL().Guardar(modelo);
                if (strMensaje.Equals("OK"))
                {
                    TempData["Reparacion-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }

            ViewBag.servicioIds = servicioIds;
            ViewBag.notaIds = notaIds;
            ViewBag.estadoIds = estadoIds;

            ViewBag.politicaIdIds = politicaIdIds;
            ViewBag.politicaIds = politicaIds;

            this.CargaControles();
            this.CargaPoliticas();
            return View(modelo);
        }

        [Permiso("Control.Reparacion.Editar")]
        public ActionResult Editar(long id)
        {
            Reparacion ReparacionActual = new ReparacionBL().ObtenerPorId(id, false, true);

            if (ReparacionActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Reparación", "Editar");

            if (ReparacionActual.Politicas != null && ReparacionActual.Politicas.Count() > 0)
            {
                ViewBag.servicioIds = ReparacionActual.Servicios.Select(x => x.ServicioId);
                ViewBag.notaIds = ReparacionActual.Servicios.Select(x => x.Nota);
                ViewBag.estadoIds = ReparacionActual.Servicios.Select(x => x.Estado);

                ViewBag.politicaIdIds = ReparacionActual.Politicas.Select(x => x.PoliticaCategoriaId);
                ViewBag.politicaIds = ReparacionActual.Politicas.Select(x => x.Politica.Nombre);
            }
            else
            {
                ViewBag.servicioIds = 0;
                ViewBag.notaIds = "";
                ViewBag.estadoIds = "";

                ViewBag.politicaIdIds = 0;
                ViewBag.politicaIds = "";
            }

            this.CargaControles();
            this.CargaPoliticas();
            return View(ReparacionActual);
        }

        [Permiso("Control.Reparacion.Editar")]
        [HttpPost]
        public ActionResult Editar(Reparacion modelo, long[] servicioIds, string[] notaIds, string[] estadoIds, long[] politicaIdIds, string[] politicaIds)
        {
            if (servicioIds == null || servicioIds.Length == 0)
            {
                ModelState.AddModelError("", "Se necesita la evaluación del equipo");
            }

            if (politicaIdIds == null || politicaIdIds.Length == 0)
            {
                ModelState.AddModelError("", "Para crear una politica categoria debe de asignar politicas");
            }

            Reparacion ReparacionActual = new ReparacionBL().ObtenerPorId(modelo.ReparacionId, false, true);

            if (ModelState.IsValid)
            {
                if (servicioIds != null && servicioIds.Count() > 0)
                {
                    modelo.Servicios = new List<ReparacionServicio>();
                    for (int i = 0; i < servicioIds.Length; i++)
                    {
                        ReparacionServicio Servicio = new ReparacionServicio();
                        Servicio.ServicioId = servicioIds[i];
                        Servicio.Nota = notaIds[i];
                        Servicio.Estado = estadoIds[i] == "Estado Bueno" ? true : false;
                        modelo.Servicios.Add(Servicio);
                    }
                }

                if (politicaIdIds != null && politicaIdIds.Count() > 0)
                {
                    modelo.Politicas = new List<ReparacionPoliticaCategoria>();
                    for (int i = 0; i < politicaIdIds.Length; i++)
                    {
                        ReparacionPoliticaCategoria Detalle = new ReparacionPoliticaCategoria();
                        Detalle.PoliticaCategoriaId = politicaIdIds[i];
                        modelo.Politicas.Add(Detalle);
                    }
                }

                string strMensaje = new ReparacionBL().ActualizarCosto(modelo);
                if (strMensaje.Equals("OK"))
                {
                    TempData["Reparacion-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }

            ViewBag.servicioIds = servicioIds;
            ViewBag.notaIds = notaIds;
            ViewBag.estadoIds = estadoIds;

            ViewBag.politicaIdIds = politicaIdIds;
            ViewBag.politicaIds = politicaIds;

            this.CargaControles();
            this.CargaPoliticas();
            return View(ReparacionActual);
        }

        [Permiso("Control.Reparacion.Verificar")]
        public ActionResult Verificar(long id)
        {
            Reparacion ReparacionActual = new ReparacionBL().ObtenerPorId(id, true, true);

            if (ReparacionActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Reparación", "Verificar");

            return View(ReparacionActual);
        }

        [Permiso("Control.Reparacion.Verificar")]
        [HttpPost]
        public ActionResult Verificar(Reparacion modelo)
        {
            Reparacion ReparacionActual = new ReparacionBL().ObtenerPorId(modelo.ReparacionId, true, true);

            if (ModelState.IsValid)
            {
                modelo.EstadoId = 2;
                modelo.UsrAsignado = CustomHelper.getUserId();
                modelo.FechaIniciaReparacion = DateTime.Today;
                string strMensaje = new ReparacionBL().Guardar(modelo);
                if (strMensaje.Equals("OK"))
                {
                    TempData["Reparacion-Asignada-Success"] = strMensaje;
                    return RedirectToAction("SinAsignar");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }

            return View(ReparacionActual);
        }

        [Permiso("Control.Reparacion.Editar_6")]
        public ActionResult Editar_de_6(long id)
        {
            Reparacion ReparacionActual = new ReparacionBL().ObtenerPorId(id, false, true);

            if (ReparacionActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Reparación", "Editar");

            this.CargaControles();
            return View(ReparacionActual);
        }

        [Permiso("Control.Reparacion.Editar_6")]
        [HttpPost]
        public ActionResult Editar_de_6(Reparacion modelo)
        {
            Reparacion ReparacionActual = new ReparacionBL().ObtenerPorId(modelo.ReparacionId, false, true);

            if (ModelState.IsValid)
            {
                string strMensaje = new ReparacionBL().ActualizarCosto(modelo);
                if (strMensaje.Equals("OK"))
                {
                    TempData["Reparacion-Success"] = strMensaje;
                    return RedirectToAction("Entregas_de_6");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }

            return View(ReparacionActual);
        }

        [Permiso("Control.Reparacion.Detalle")]
        public ActionResult Detalle(long id)
        {
            Reparacion ReparacionActual = new ReparacionBL().ObtenerPorId(id, true, true);

            if (ReparacionActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Reparación", "Detalle");

            return View(ReparacionActual);
        }

        [Permiso("Control.Reparacion.Anotacion")]
        public ActionResult Anotacion(long id)
        {
            Reparacion ReparacionActual = new ReparacionBL().ObtenerPorId(id, true, true, CustomHelper.getUserId());

            if (ReparacionActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Reparación", "Anotación");
                       
            return View(ReparacionActual);
        }

        [Permiso("Control.Reparacion.Anotacion")]
        [HttpPost]
        public ActionResult Anotacion(Reparacion modelo)
        {
            Reparacion ReparacionActual = new ReparacionBL().ObtenerPorId(modelo.ReparacionId, true, true);

            if (ModelState.IsValid)
            {
                modelo.EstadoId = 3;
                modelo.Operado = true;
                string strMensaje = new ReparacionBL().Guardar(modelo);
                if (strMensaje.Equals("OK"))
                {
                    TempData["Reparacion-Asignada-Success"] = strMensaje;
                    return RedirectToAction("Asignadas");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }
           
            return View(ReparacionActual);
        }

        [Permiso("Control.Reparacion.Entrega")]
        public ActionResult Entrega(long id)
        {
            Reparacion ReparacionActual = new ReparacionBL().ObtenerPorId(id, true, true);

            if (ReparacionActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Reparación", "Entrega");
            
            this.CargaFormas();
            return View(ReparacionActual);
        }

        [Permiso("Control.Reparacion.Entrega")]
        [HttpPost]
        public ActionResult Entrega(Reparacion modelo, long[] formaIds, decimal[] pagarIds, string[] boletaIds, string[] notaIds, int descuento)
        {
            Reparacion ReparacionActual = new ReparacionBL().ObtenerPorId(modelo.ReparacionId, true, true);

            if (formaIds == null || formaIds.Length == 0)
            {
                ModelState.AddModelError("", "Para realizar una entrega debe de cancelar los productos");
            }
            else
            {
                modelo.Pagos = new List<ReparacionFormaPago>();
                for (int i = 0; i < formaIds.Length; i++)
                {
                    ReparacionFormaPago Forma = new ReparacionFormaPago();
                    Forma.FormaPagoId = formaIds[i];
                    Forma.Valor = pagarIds[i];
                    Forma.Nota = notaIds[i];

                    modelo.Pagos.Add(Forma);
                }
            }

            if (ModelState.IsValid)
            {
                modelo.TipoId = 1;
                modelo.EstadoId = 4;
                modelo.UsrEntrega = CustomHelper.getUserId();
                string strMensaje = new ReparacionBL().Guardar(modelo);
                if (strMensaje.Equals("OK"))
                {
                    TempData["Reparacion-Entrega-Success"] = strMensaje;
                    return RedirectToAction("Entregas");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }

            ViewBag.formaIds = formaIds;
            ViewBag.pagarIds = pagarIds;
            ViewBag.boletaIds = boletaIds;
            ViewBag.notaIds = notaIds;
                     
            this.CargaFormas();
            return View(ReparacionActual);
        }

        [Permiso("Control.Reparacion.Entrega_2_a_5")]
        public ActionResult Entrega_de_2_a_5(long id)
        {
            Reparacion ReparacionActual = new ReparacionBL().ObtenerPorId(id, true, true);

            if (ReparacionActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Reparación", "Entrega de 2 a 5 Meses");
                       
            this.CargaFormas();
            return View(ReparacionActual);
        }

        [Permiso("Control.Reparacion.Entrega_2_a_5")]
        [HttpPost]
        public ActionResult Entrega_de_2_a_5(Reparacion modelo, long[] formaIds, decimal[] pagarIds, string[] boletaIds, string[] notaIds, int descuento)
        {
            Reparacion ReparacionActual = new ReparacionBL().ObtenerPorId(modelo.ReparacionId, true, true);

            if (formaIds == null || formaIds.Length == 0)
            {
                ModelState.AddModelError("", "Para realizar una entrega debe de cancelar los productos");
            }
            else
            {
                modelo.Pagos = new List<ReparacionFormaPago>();
                for (int i = 0; i < formaIds.Length; i++)
                {
                    ReparacionFormaPago Forma = new ReparacionFormaPago();
                    Forma.FormaPagoId = formaIds[i];
                    Forma.Valor = pagarIds[i];
                    Forma.Nota = notaIds[i];

                    modelo.Pagos.Add(Forma);
                }
            }

            if (ModelState.IsValid)
            {
                modelo.TipoId = 1;
                modelo.EstadoId = 4;
                modelo.UsrEntrega = CustomHelper.getUserId();
                string strMensaje = new ReparacionBL().Guardar(modelo);
                if (strMensaje.Equals("OK"))
                {
                    TempData["Reparacion-Entrega-Success"] = strMensaje;
                    return RedirectToAction("Entregas_de_2_a_5");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }

            ViewBag.formaIds = formaIds;
            ViewBag.pagarIds = pagarIds;
            ViewBag.boletaIds = boletaIds;
            ViewBag.notaIds = notaIds;
                    
            this.CargaFormas();
            return View(ReparacionActual);
        }

        [Permiso("Control.Reparacion.Entrega_6")]
        public ActionResult Entrega_de_6(long id)
        {
            Reparacion ReparacionActual = new ReparacionBL().ObtenerPorId(id, true, true);

            if (ReparacionActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Reparación", "Entrega de 6 Meses en Adelante");
                      
            this.CargaFormas();
            return View(ReparacionActual);
        }

        [Permiso("Control.Reparacion.Entrega_6")]
        [HttpPost]
        public ActionResult Entrega_de_6(Reparacion modelo, long[] formaIds, decimal[] pagarIds, string[] boletaIds, string[] notaIds, int descuento)
        {
            Reparacion ReparacionActual = new ReparacionBL().ObtenerPorId(modelo.ReparacionId, true, true);

            if (formaIds == null || formaIds.Length == 0)
            {
                ModelState.AddModelError("", "Para realizar una entrega debe de cancelar los productos");
            }
            else
            {
                modelo.Pagos = new List<ReparacionFormaPago>();
                for (int i = 0; i < formaIds.Length; i++)
                {
                    ReparacionFormaPago Forma = new ReparacionFormaPago();
                    Forma.FormaPagoId = formaIds[i];
                    Forma.Valor = pagarIds[i];
                    Forma.Nota = notaIds[i];
                    
                    modelo.Pagos.Add(Forma);
                }
            }

            if (ModelState.IsValid)
            {
                modelo.TipoId = 1;
                modelo.EstadoId = 4;
                modelo.UsrEntrega = CustomHelper.getUserId();
                string strMensaje = new ReparacionBL().Guardar(modelo);
                if (strMensaje.Equals("OK"))
                {
                    TempData["Reparacion-Entrega-Success"] = strMensaje;
                    return RedirectToAction("Entregas_de_6");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }

            ViewBag.formaIds = formaIds;
            ViewBag.pagarIds = pagarIds;           
            ViewBag.boletaIds = boletaIds;
            ViewBag.notaIds = notaIds;
                      
            this.CargaFormas();
            return View(ReparacionActual);
        }

        [Permiso("Control.Reparacion.Entrega")]
        public ActionResult Liquidar(long id)
        {
            Reparacion ReparacionActual = new ReparacionBL().ObtenerPorId(id, true, true);

            if (ReparacionActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Reparación", "Liquidar");

            return View(ReparacionActual);
        }

        [Permiso("Control.Reparacion.Entrega")]
        [HttpPost]
        public ActionResult Liquidar(Reparacion modelo)
        {
            Reparacion ReparacionActual = new ReparacionBL().ObtenerPorId(modelo.ReparacionId, true, true);

            if (ModelState.IsValid)
            {
                modelo.TipoId = 1;
                modelo.EstadoId = 5;
                modelo.UsrEntrega = CustomHelper.getUserId();
                string strMensaje = new ReparacionBL().Guardar(modelo);
                if (strMensaje.Equals("OK"))
                {
                    TempData["Reparacion-Entrega-Success"] = strMensaje;
                    return RedirectToAction("Reparacion_Liquidar");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }

            return View(ReparacionActual);
        }

        [Permiso("Control.Reparacion.Anular")]
        public ActionResult Anular(long id)
        {
            Reparacion ReparacionActual = new ReparacionBL().ObtenerPorId(id, true, true);

            if (ReparacionActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Reparación", "Anular");

            return View(ReparacionActual);
        }

        [Permiso("Control.Reparacion.Anular")]
        [HttpPost]
        public ActionResult Anular(long reparacionId, string comentario)
        {
            string strMensaje = new ReparacionBL().Anular(reparacionId, comentario, CustomHelper.getUserId());
            if (strMensaje.Equals("OK"))
            {
                TempData["Reparacion_Anular-Success"] = strMensaje;
                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("", strMensaje);
            }

            Reparacion ReparacionActual = new ReparacionBL().ObtenerPorId(reparacionId, true, true);

            if (ReparacionActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Reparación", "Anular");

            return View(ReparacionActual);
        }

        [Permiso("Control.Reparacion.Aprobar")]
        public ActionResult Aprobar(long id)
        {
            Reparacion ReparacionActual = new ReparacionBL().ObtenerPorId(id, true, true);

            if (ReparacionActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Reparación", "Aprobar");

            return View(ReparacionActual);
        }

        [Permiso("Control.Reparacion.Aprobar")]
        [HttpPost]
        public ActionResult Aprobar(long reparacionId, string comentario)
        {
            string strMensaje = new ReparacionBL().Aprobar(reparacionId);
            if (strMensaje.Equals("OK"))
            {
                TempData["Reparacion_Aprobar-Success"] = strMensaje;
                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("", strMensaje);
            }

            Reparacion ReparacionActual = new ReparacionBL().ObtenerPorId(reparacionId, true, true);

            if (ReparacionActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Reparación", "Aprobar");

            return View(ReparacionActual);
        }

        [Permiso("Control.Reporte.Boleta")]
        public ActionResult Boleta(long Id)
        {
            Reparacion SolicitudActual = new ReparacionBL().ObtenerPorId(Id, false, true);

            if (SolicitudActual != null)
            {
                DataSet Solicitud = new DataSet("Inventario");

                DataTable Encabezado = new DataTable("ReparacionEncabezado");
                DataTable Detalle = new DataTable("ReparacionServicio");
                DataTable Repuesto = new DataTable("ReparacionRepuesto");

                Encabezado.Columns.Add(new DataColumn("Solicitud", typeof(long)));
                Encabezado.Columns.Add(new DataColumn("Descripcion", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Agencia", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Cliente", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("DPI", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Contacto", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Marca", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Falla", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("IMEI", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Fecha", typeof(DateTime)));
                Encabezado.Columns.Add(new DataColumn("Precio", typeof(decimal)));               
                Encabezado.Columns.Add(new DataColumn("PoliticaIngreso", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("PoliticaGarantia", typeof(string)));
                              
                //Obteniendo las politicas de ingresos.
                string Politica = string.Empty;

                List<PoliticaCategoria> Politicas = new List<PoliticaCategoria>();

                //Se verifica las categorias que tiene asignadas
                if (SolicitudActual.Politicas != null && SolicitudActual.Politicas.Count() > 0)
                {
                    if (SolicitudActual.Politicas.Count() == 1)
                    {
                        bool General = SolicitudActual.Politicas.Where(x => x.PoliticaCategoriaId == 0).Count() > 0;
                        if (General)
                        {
                            Politicas = new PoliticaCategoriaBL().PoliticasxCategoria(20181103001);
                        }
                        else
                        {
                            foreach (var item in SolicitudActual.Politicas.OrderBy(x => x.OrdenId))
                            {
                                if (item.Politica.TipoId == 1)
                                {
                                    Politicas.Add(item.Politica);
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var item in SolicitudActual.Politicas.OrderBy(x => x.OrdenId))
                        {
                            if (item.Politica.TipoId == 1)
                            {
                                Politicas.Add(item.Politica);
                            }
                        }
                    }
                }
                else
                {
                    Politicas = new PoliticaCategoriaBL().PoliticasxCategoria(20181103001);
                }

                if (Politicas != null && Politicas.Count() > 0)
                {
                    foreach (var Categoria in Politicas)
                    {
                        if (Categoria.PoliticaCategoriaId == 0)
                        {
                            continue;
                        }

                        //Titulo de la politica
                        Politica += String.Format("<b>{0}</b>", Categoria.Nombre.ToUpper());
                        Politica += "<br/>";
                        Politica += "<ol>";

                        //Detalle de la politica
                        foreach (var item in Categoria.Politicas)
                        {
                            if (item.Politica != null)
                            {
                                Politica += String.Format("<li><p>{0}</p></li>", item.Politica.Nombre.ToUpper());
                            }
                        }
                        Politica += "</ol>";
                        Politica += "<br/>";
                    }
                }

                //Obteniendo las politicas de garantias.
                string PoliticaGarantia = string.Empty;

                Politicas = new List<PoliticaCategoria>();

                //Se verifica las categorias que tiene asignadas
                if (SolicitudActual.Politicas != null && SolicitudActual.Politicas.Count() > 0)
                {
                    if (SolicitudActual.Politicas.Count() == 1)
                    {
                        bool General = SolicitudActual.Politicas.Where(x => x.PoliticaCategoriaId == 0).Count() > 0;
                        if (General)
                        {
                            Politicas = new PoliticaCategoriaBL().PoliticasxCategoria(20181103002);
                        }
                        else
                        {
                            foreach (var item in SolicitudActual.Politicas.OrderBy(x => x.OrdenId))
                            {
                                if (item.Politica.TipoId == 2)
                                {
                                    Politicas.Add(item.Politica);
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var item in SolicitudActual.Politicas.OrderBy(x => x.OrdenId))
                        {
                            if (item.Politica.TipoId == 2)
                            {
                                Politicas.Add(item.Politica);
                            }
                        }
                    }
                }
                else
                {
                    Politicas = new PoliticaCategoriaBL().PoliticasxCategoria(20181103002);
                }

                if (Politicas != null && Politicas.Count() > 0)
                {
                    foreach (var Categoria in Politicas)
                    {
                        if (Categoria.PoliticaCategoriaId == 0)
                        {
                            continue;
                        }

                        //Titulo de la politica
                        PoliticaGarantia += String.Format("<b>{0}</b>", Categoria.Nombre.ToUpper());
                        PoliticaGarantia += "<br/>";
                        PoliticaGarantia += "<ol>";

                        //Detalle de la politica
                        foreach (var item in Categoria.Politicas)
                        {
                            if (item.Politica != null)
                            {
                                PoliticaGarantia += String.Format("<li><p>{0}</p></li>", item.Politica.Nombre.ToUpper());
                            }
                        }
                        PoliticaGarantia += "</ol>";
                        PoliticaGarantia += "<br/>";
                    }
                }

                Encabezado.Rows.Add(SolicitudActual.ReparacionId, SolicitudActual.Descripcion, SolicitudActual.Agencia.Nombre, String.IsNullOrWhiteSpace(SolicitudActual.Cliente.NoTelefono) ? SolicitudActual.Cliente.Nombre : string.Format("Nit: {0} - {1} - Tel.: {2} - {3}", SolicitudActual.Cliente.Nit, SolicitudActual.Cliente.Nombre, SolicitudActual.Cliente.NoTelefono, SolicitudActual.Cliente.NoTelefono), SolicitudActual.Cliente.DPI, SolicitudActual.Cliente.NoTelefono, SolicitudActual.Marca, SolicitudActual.Falla, SolicitudActual.IMEI, SolicitudActual.Fecha.ToString("dd/MM/yyyy"), SolicitudActual.CostoServicio, Politica, PoliticaGarantia);

                Detalle.Columns.Add(new DataColumn("Solicitud", typeof(long)));
                Detalle.Columns.Add(new DataColumn("ServicioId", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Nombre", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Estado", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Nota", typeof(string)));

                if (SolicitudActual.Servicios != null && SolicitudActual.Servicios.Count() > 0)
                {
                    foreach (var DetalleActual in SolicitudActual.Servicios)
                    {
                        Detalle.Rows.Add(SolicitudActual.ReparacionId, DetalleActual.ServicioId, DetalleActual.Servicio.Nombre, DetalleActual.Estado == true ? "Sí" : "No", DetalleActual.Nota);
                    }
                }

                Repuesto.Columns.Add(new DataColumn("Solicitud", typeof(long)));
                Repuesto.Columns.Add(new DataColumn("Nombre", typeof(string)));

                if (SolicitudActual.Piezas != null && SolicitudActual.Piezas.Count() > 0)
                {
                    foreach (var RepuestoActual in SolicitudActual.Piezas)
                    {
                        Repuesto.Rows.Add(SolicitudActual.ReparacionId, RepuestoActual.Producto.Nombre);
                    }
                }

                Solicitud.Tables.Add(Encabezado);
                Solicitud.Tables.Add(Detalle);
                Solicitud.Tables.Add(Repuesto);

                // Se define la ruta del reporte
                var reportPath = Server.MapPath("~/Reports/ReportSolicitud.rdlc");

                // se obtienen los bytes del reporte en pdf
                var bytes = GetReportBytes(reportPath, Solicitud, 8.5m, 11.0m, 0.2m, 0m);

                return File(bytes, "application/pdf");

            }

            return View();
        }

        [HttpPost]
        [ActionName("EliminarPieza")]
        public JsonResult EliminarPieza(long reparacionId, long agenciaId, string productoId)
        {
            return Json(new { Operacion = new ReparacionBL().EliminarPieza(reparacionId, agenciaId, productoId) }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ActionName("NuevoProducto")]
        public JsonResult NuevoProducto(ReparacionPieza modelo)
        {
            return Json(new { Operacion = new ReparacionBL().NuevoProducto(modelo) }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ActionName("NuevoComentario")]
        public JsonResult NuevoComentario(long ReparacionId, string Comentario)
        {
            return Json(new { Operacion = new ReparacionAnotacionBL().Guardar(new ReparacionAnotacion() { ReparacionId = ReparacionId, Comentario = Comentario, UsrAnotacion = CustomHelper.getUserId() }) }, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerProducto")]
        public JsonResult ObtenerProducto(long agenciaId, string productoId)
        {
            if (agenciaId > 0 && !string.IsNullOrWhiteSpace(productoId))
            {
                Producto ProductoActual = new ProductoBL().ObtenerPorId(agenciaId, productoId, false, true);
                if (ProductoActual != null && !string.IsNullOrWhiteSpace(ProductoActual.ProductoId))
                {
                    return Json(new { Operacion = true, Data = ProductoActual }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerComentariosPendientes")]
        public JsonResult ObtenerComentariosPendientes()
        {            
            return Json(new { Operacion = true, Data = new ReparacionBL().ObtenerConteoComentariosNuevos(CustomHelper.getUserId(), CustomHelper.getAgenciaId()) }, JsonRequestBehavior.AllowGet);
        }
    }
}