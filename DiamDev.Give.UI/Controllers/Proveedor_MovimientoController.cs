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
using DiamDev.Give.DAL;
using System.Data.Entity;

namespace DiamDev.Give.UI.Controllers
{
    [Authorize]
    [Seguridad]
    [HandleError]
    public class Proveedor_MovimientoController : Controller
    {
        #region Metodos Privados

            private void CargaControles()
            {
                var Proveedores = new ProveedorBL().ObtenerListado(false);
                var Tipos = new ProveedorMovimientoTipoBL().ObtenerListado();
               
                ViewBag.Proveedores = new SelectList(Proveedores, "ProveedorId", "Nombre");     
                ViewBag.Tipos = new SelectList(Tipos, "TipoId", "Nombre");            
            }

            private byte[] GetReportBytes(string reportPath, DataSet reportDataSource, decimal pageWidth = 13.38m, decimal pageHeight = 8.5m, decimal MarginLeft = 1m, decimal MarginRight = 1m)
            {

                byte[] reportBytes = null;

                // Se crea la instancia del reporte y se cargan sus datos.
                LocalReport reporte = new LocalReport() { ReportPath = reportPath };
                reporte.DataSources.Add(new ReportDataSource("Laboratorio", reportDataSource.Tables[0]));

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

            public FileResult Preview(int id, long movimientoId)
            {
                ProveedorMovimientoFotografia FotografiaActual = new ProveedorMovimientoBL().Fotografia(id, movimientoId);

                var content = Binario.Drawing.ImageManager.GetThumbnail(FotografiaActual.Content, 100);
                return File(content, FotografiaActual.ContentType);
            }

            public FileResult Imagen(int id, long movimientoId)
            {
                ProveedorMovimientoFotografia FotografiaActual = new ProveedorMovimientoBL().Fotografia(id, movimientoId);

                return File(FotografiaActual.Content, FotografiaActual.ContentType);
            }

        #endregion

        // GET: Proveedor_Movimiento
        [Permiso("Control.Proveedor_Movimiento.Ver_Listado")]
        public ActionResult Index(DateTime? FechaInicial, DateTime? FechaFinal)
        {
            CustomHelper.setTitle("Movimiento", "Listado");
            List<ProveedorMovimiento> Movimientos = new List<ProveedorMovimiento>();
         
            try
            {
                if (!FechaInicial.HasValue && !FechaFinal.HasValue)
                {
                    FechaInicial = DateTime.Today;
                    FechaFinal = DateTime.Today;
                }

                Movimientos = new ProveedorMovimientoBL().ObtenerListadoxFecha(FechaInicial.Value, FechaFinal.Value).ToList();            
            }
            catch (Exception)
            {
            }
           
            return View(Movimientos);
        }

        [Permiso("Control.Proveedor_Movimiento.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Movimiento", "Nuevo");

            this.CargaControles();
            return View();
        }

        [Permiso("Control.Proveedor_Movimiento.Crear")]
        [HttpPost]
        public ActionResult Crear(ProveedorMovimiento modelo, ArchivoModel[] archivos)
        {
            if (archivos != null && archivos.Count() > 0)
            {
                modelo.Fotografias = new List<ProveedorMovimientoFotografia>();
                foreach (ArchivoModel archivo in archivos)
                {
                    byte[] FileData = new byte[archivo.Archivo.ContentLength + 1];
                    archivo.Archivo.InputStream.Read(FileData, 0, archivo.Archivo.ContentLength);
                    modelo.Fotografias.Add(new ProveedorMovimientoFotografia() { Nombre = archivo.Archivo.FileName, Content = FileData, ContentType = archivo.Archivo.ContentType, Length = archivo.Archivo.ContentLength });
                }
            }
                        
            modelo.UsrCreo = CustomHelper.getUserId();       

            if (ModelState.IsValid)
            {
                string strMensaje = new ProveedorMovimientoBL().Guardar(modelo);
                if (strMensaje.Equals("OK"))
                {
                    TempData["Proveedor_Movimiento-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }

            this.CargaControles();
            return View(modelo);
        }

        [Permiso("Control.Proveedor_Movimiento.Detalle")]
        public ActionResult Detalle(long id)
        {
            ProveedorMovimiento MovimientoActual = new ProveedorMovimientoBL().ObtenerPorId(id, true, true);

            if (MovimientoActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Movimiento", "Detalle");

            return View(MovimientoActual);
        }

        [Permiso("Control.Reporte.Boleta_Movimiento")]
        public ActionResult Boleta(long id)
        {
            ProveedorMovimiento MovimientoActual = new ProveedorMovimientoBL().ObtenerPorId(id, true, true);

            if (MovimientoActual != null)
            {
                DataSet Movimiento = new DataSet("Laboratorio");

                DataTable Encabezado = new DataTable("Laboratorio");

                Encabezado.Columns.Add(new DataColumn("LaboratorioId", typeof(long)));
                Encabezado.Columns.Add(new DataColumn("Agencia", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("ProductoBase", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("ProductoDestino", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("CantidadBase", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("CantidadDestino", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Responsable", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Fecha", typeof(string)));
                                
                if (MovimientoActual.TipoId == 1)
                {
                    Encabezado.Rows.Add(MovimientoActual.MovimientoId, "", MovimientoActual.Documento, MovimientoActual.Proveedor.Nombre, "", "", MovimientoActual.Monto.ToString("C"), MovimientoActual.FechaMovimiento.ToString("dd/MM/yyyy"));
                }
                else if (MovimientoActual.TipoId == 2)
                {
                    string Nombre = MovimientoActual.Proveedor.Nombre;

                    if (!string.IsNullOrWhiteSpace(MovimientoActual.Proveedor.NombreCheque))
                    {
                        Nombre = MovimientoActual.Proveedor.NombreCheque;                      
                    }

                    Numalet Convetir = new Numalet();                    
                    Encabezado.Rows.Add(MovimientoActual.MovimientoId, "", Convetir.ToCustomCardinal(MovimientoActual.Monto).ToUpper(), Nombre.ToUpper(), "", "", MovimientoActual.Monto.ToString("N2"), MovimientoActual.FechaMovimiento.ToLongDateString().ToUpper());
                }

                Movimiento.Tables.Add(Encabezado);
                // Se define la ruta del reporte
                string reportPath = string.Empty;

                if (MovimientoActual.TipoId == 1)
                {
                    reportPath = Server.MapPath("~/Reports/ReportMovMovimiento.rdlc");
                }
                else if (MovimientoActual.TipoId == 2)
                {
                    reportPath = Server.MapPath("~/Reports/ReportMovCheque.rdlc");                    
                }

                // se obtienen los bytes del reporte en pdf
                var bytes = GetReportBytes(reportPath, Movimiento, 8.5m, 11.0m, 0.2m, 0m);

                return File(bytes, "application/pdf");
            }

            return View();
        } 
    }
}