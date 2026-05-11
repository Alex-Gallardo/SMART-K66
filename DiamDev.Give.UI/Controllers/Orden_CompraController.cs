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
using System.Collections;
using System.Configuration;

namespace DiamDev.Give.UI.Controllers
{
    [Authorize]
    [Seguridad]
    [HandleError]
    public class Orden_CompraController : Controller
    {    
        #region Metodos Privados

            private void CargaControles()
            { 
                var Proveedores = new ProveedorBL().ObtenerListado(false);
                var Monedas = new MonedaBL().ObtenerListado();    
              
                ViewBag.Proveedores = new SelectList(Proveedores, "ProveedorId", "Nombre");
                ViewBag.Monedas = new SelectList(Monedas, "MonedaId", "Nombre");   
            }
        
            private byte[] GetReportBytes(string reportPath, DataSet reportDataSource, decimal pageWidth = 13.38m, decimal pageHeight = 8.5m, decimal MarginLeft = 1m, decimal MarginRight = 1m)
            {

                byte[] reportBytes = null;

                // Se crea la instancia del reporte y se cargan sus datos.
                LocalReport reporte = new LocalReport() { ReportPath = reportPath };
                reporte.DataSources.Add(new ReportDataSource("MovimientoEncabezado", reportDataSource.Tables[0]));
                reporte.DataSources.Add(new ReportDataSource("MovimientoDetalle", reportDataSource.Tables[1]));
            
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

        // GET: Orden_Compra
        [Permiso("Control.Orden_Compra.Ver_Listado")]
        public ActionResult Index(int? page, string search, DateTime? FechaInicial, DateTime? FechaFinal)
        {
            CustomHelper.setTitle("Orden de Compra", "Listado");
            List<OrdenCompra> Ordenes = new List<OrdenCompra>();
         
            try
            {
                if (!FechaInicial.HasValue && !FechaFinal.HasValue)
                {
                    FechaInicial = DateTime.Today;
                    FechaFinal = DateTime.Today;
                }
               
                if (!string.IsNullOrWhiteSpace(search) && search != null)
                {
                    Ordenes = new OrdenCompraBL().Buscar(search, CustomHelper.getUserId()).ToList();
                }
                else
                {
                    Ordenes = new OrdenCompraBL().ObtenerListadoPorFecha(FechaInicial.Value, FechaFinal.Value, CustomHelper.getUserId()).ToList();
                }
            }
            catch (Exception)
            {}

            ViewBag.Search = search;

            int pageSize = 15;
            int pageNumber = (page ?? 1);
            return View(Ordenes.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Orden_Compra.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Orden de Compra", "Nueva");         

            this.CargaControles();
            return View();
        }

        [Permiso("Control.Orden_Compra.Crear")]
        [HttpPost]
        public ActionResult Crear(OrdenCompra modelo, string[] productoIds, string[] nombreProductoIds, long[] presentacionIds, string[] nombrePresentacionIds, decimal[] cantidadIds, decimal[] precioIds, HttpPostedFileBase fotografiaApp)
        {
            if (productoIds == null || productoIds.Length == 0)
            {
                ModelState.AddModelError("", "Para realizar una orden de compra debe de asignar productos");
            }
          
            modelo.AgenciaId = CustomHelper.getAgenciaId();
            modelo.UsrCreo = CustomHelper.getUserId();
            modelo.Operado = false;
         
            modelo.Detalles = new List<OrdenCompraDetalle>();
            for (int i = 0; i < productoIds.Length; i++)
            {
                if (modelo.Detalles.Where(x => x.ProductoId == productoIds[i]).Count() > 0)
                {
                    foreach (var item in modelo.Detalles)
                    {
                        if (item.ProductoId == productoIds[i])
                        {
                            item.Cantidad += cantidadIds[i];
                            break;
                        }
                    }
                }
                else
                {
                    OrdenCompraDetalle Detalle = new OrdenCompraDetalle();
                    Detalle.ProductoId = productoIds[i];
                    Detalle.UnidadId = presentacionIds[i];
                    Detalle.Nombre = nombreProductoIds[i];                    
                    Detalle.Cantidad = cantidadIds[i];                    
                    Detalle.Precio = precioIds[i];

                    modelo.Detalles.Add(Detalle);
                }               
            }
        
            if (ModelState.IsValid)
            {
                if (fotografiaApp != null)
                {
                    modelo.Fotografia = new ProductoFotografia();
                    if (fotografiaApp != null)
                    {
                        byte[] FileData = new byte[fotografiaApp.ContentLength + 1];
                        fotografiaApp.InputStream.Read(FileData, 0, fotografiaApp.ContentLength);

                        modelo.FotografiaOrden = "orden_compra.png";
                        modelo.Fotografia = new ProductoFotografia() { Nombre = fotografiaApp.FileName, Content = FileData, ContentType = fotografiaApp.ContentType, Length = fotografiaApp.ContentLength };
                    }
                }

                string strMensaje = new OrdenCompraBL().Guardar(modelo);
                if (strMensaje.Equals("OK"))
                {
                    TempData["Orden_Compra-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }
                      
            ViewBag.productoIds = productoIds;
            ViewBag.nombreProductoIds = nombreProductoIds;
            ViewBag.presentacionIds = presentacionIds;
            ViewBag.nombrePresentacionIds = nombrePresentacionIds;            
            ViewBag.cantidadIds = cantidadIds;            
            ViewBag.precioIds = precioIds;                        

            this.CargaControles();
            return View(modelo);
        }

        [Permiso("Control.Orden_Compra.Editar")]
        public ActionResult Editar(long id)
        {
            OrdenCompra OrdenCompraActual = new OrdenCompraBL().ObtenerPorId(id, true);

            if (OrdenCompraActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Orden de Compra", "Editar");

            if (OrdenCompraActual.Detalles != null && OrdenCompraActual.Detalles.Count() > 0)
            {
                ViewBag.productoIds = OrdenCompraActual.Detalles.OrderBy(x => x.DetalleId).Select(x => x.ProductoId).ToList();
                ViewBag.nombreProductoIds = OrdenCompraActual.Detalles.AsEnumerable().OrderBy(x => x.DetalleId).Select(x => x.Nombre).ToList();
                ViewBag.presentacionIds = OrdenCompraActual.Detalles.OrderBy(x => x.DetalleId).Select(x => x.UnidadId).ToList();
                ViewBag.nombrePresentacionIds = OrdenCompraActual.Detalles.OrderBy(x => x.DetalleId).Select(x => x.Unidad.Nombre).ToList();                
                ViewBag.cantidadIds = OrdenCompraActual.Detalles.OrderBy(x => x.DetalleId).Select(x => x.Cantidad).ToList();                
                ViewBag.precioIds = OrdenCompraActual.Detalles.OrderBy(x => x.DetalleId).Select(x => x.Precio).ToList();
            }
            else
            {
                ViewBag.productoIds = "";
                ViewBag.nombreProductoIds = "";
                ViewBag.presentacionIds = "";
                ViewBag.nombrePresentacionIds = "";                
                ViewBag.cantidadIds = "";                
                ViewBag.precioIds = "";
            }                      

            this.CargaControles();
            return View(OrdenCompraActual);
        }

        [Permiso("Control.Orden_Compra.Editar")]
        [HttpPost]
        public ActionResult Editar(OrdenCompra modelo, string[] productoIds, string[] nombreProductoIds, long[] presentacionIds, string[] nombrePresentacionIds, decimal[] cantidadIds, decimal[] precioIds, HttpPostedFileBase fotografiaApp)
        {
            if (productoIds == null || productoIds.Length == 0)
            {
                ModelState.AddModelError("", "Para realizar una orden de compra debe de asignar productos");
            }          

            modelo.Detalles = new List<OrdenCompraDetalle>();
            for (int i = 0; i < productoIds.Length; i++)
            {
                if (modelo.Detalles.Where(x => x.ProductoId == productoIds[i]).Count() > 0)
                {
                    foreach (var item in modelo.Detalles)
                    {
                        if (item.ProductoId == productoIds[i])
                        {
                            item.Cantidad += cantidadIds[i];
                            break;
                        }
                    }
                }
                else
                {
                    OrdenCompraDetalle Detalle = new OrdenCompraDetalle();
                    Detalle.ProductoId = productoIds[i];
                    Detalle.UnidadId = presentacionIds[i];
                    Detalle.Nombre = nombreProductoIds[i];                    
                    Detalle.Cantidad = cantidadIds[i];                    
                    Detalle.Precio = precioIds[i];

                    modelo.Detalles.Add(Detalle);
                }
            }

            if (ModelState.IsValid)
            {
                if (fotografiaApp != null)
                {
                    modelo.Fotografia = new ProductoFotografia();
                    if (fotografiaApp != null)
                    {
                        byte[] FileData = new byte[fotografiaApp.ContentLength + 1];
                        fotografiaApp.InputStream.Read(FileData, 0, fotografiaApp.ContentLength);

                        modelo.FotografiaOrden = "orden_compra.png";
                        modelo.Fotografia = new ProductoFotografia() { Nombre = fotografiaApp.FileName, Content = FileData, ContentType = fotografiaApp.ContentType, Length = fotografiaApp.ContentLength };
                    }
                }

                string strMensaje = new OrdenCompraBL().Guardar(modelo);
                if (strMensaje.Equals("OK"))
                {
                    TempData["Orden_Compra-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }

            ViewBag.productoIds = productoIds;
            ViewBag.nombreProductoIds = nombreProductoIds;
            ViewBag.presentacionIds = presentacionIds;
            ViewBag.nombrePresentacionIds = nombrePresentacionIds;            
            ViewBag.cantidadIds = cantidadIds;            
            ViewBag.precioIds = precioIds;           

            this.CargaControles();
            return View(modelo);
        }

        [Permiso("Control.Orden_Compra.Boleta_Orden")]
        public ActionResult Boleta(long id)
        {
            OrdenCompra OrdenCompraActual = new OrdenCompraBL().ObtenerPorId(id, true);
            string PathFotografia = ConfigurationManager.AppSettings["Path_Fotografia_Orden"].ToString();

            if (OrdenCompraActual != null)
            {
                DataSet Movimiento = new DataSet("Inventario");

                DataTable Encabezado = new DataTable("MovimientoEncabezado");
                DataTable Detalle = new DataTable("MovimientoDetalle");
              
                Encabezado.Columns.Add(new DataColumn("MovimientoId", typeof(long)));
                Encabezado.Columns.Add(new DataColumn("Agencia", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Nombre", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Direccion", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Descripcion", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Fecha", typeof(DateTime)));
                Encabezado.Columns.Add(new DataColumn("Descuento", typeof(decimal)));
                Encabezado.Columns.Add(new DataColumn("Total", typeof(decimal)));
                Encabezado.Columns.Add(new DataColumn("Categoria", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Vendedor", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Comentario", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Nit", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Correo", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Telefono", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("FormaPago", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("TiempoEntrega", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Fotografia", typeof(byte[])));

                byte[] Fotografia = null;
                
                string FotografiaActual = string.Format(@"{0}\{1}\orden_compra.png", PathFotografia, OrdenCompraActual.OrdenId);

                if (System.IO.File.Exists(FotografiaActual))
                {
                    Fotografia = System.IO.File.ReadAllBytes(FotografiaActual);
                }
                else
                {
                    Fotografia = System.IO.File.ReadAllBytes(string.Format(@"{0}\sin_fotografia.jpg", PathFotografia));
                }

                Encabezado.Rows.Add(OrdenCompraActual.OrdenId, OrdenCompraActual.Agencia.Nombre, OrdenCompraActual.Proveedor.Nombre, OrdenCompraActual.Proveedor.Direccion, OrdenCompraActual.Observaciones, OrdenCompraActual.Fecha.ToString("dd/MM/yyyy"), 0, 0, "", OrdenCompraActual.UsuarioCreo.Nombre, "", OrdenCompraActual.Proveedor.Nit, OrdenCompraActual.Proveedor.EmailProveedor, OrdenCompraActual.Proveedor.NoTelefonoOficina, OrdenCompraActual.Moneda.Nombre, OrdenCompraActual.Moneda.Simbolo, Fotografia);

                Detalle.Columns.Add(new DataColumn("MovimientoId", typeof(long)));
                Detalle.Columns.Add(new DataColumn("ProductoId", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Nombre", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Presentacion", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Cantidad", typeof(decimal)));
                Detalle.Columns.Add(new DataColumn("Precio", typeof(decimal)));

                if (OrdenCompraActual.Detalles != null && OrdenCompraActual.Detalles.Count() > 0)
                {
                    foreach (var DetalleActual in OrdenCompraActual.Detalles)
                    {
                        Detalle.Rows.Add(OrdenCompraActual.OrdenId, DetalleActual.ProductoId, string.Format("{0} - {1}", DetalleActual.Producto.Codigo, string.IsNullOrWhiteSpace(DetalleActual.Nombre) ? DetalleActual.Producto.Nombre : DetalleActual.Nombre), DetalleActual.Unidad.Nombre, DetalleActual.Cantidad, DetalleActual.Precio);
                    }
                }
                            
                Movimiento.Tables.Add(Encabezado);
                Movimiento.Tables.Add(Detalle);
             
                // Se define la ruta del reporte
                var reportPath = Server.MapPath("~/Reports/ReportMovOrden.rdlc");

                // se obtienen los bytes del reporte en pdf
                var bytes = GetReportBytes(reportPath, Movimiento, 8.5m, 11.0m, 0.2m, 0m);
           
                return File(bytes, "application/pdf");
            }

            return View();
        }
    }
}