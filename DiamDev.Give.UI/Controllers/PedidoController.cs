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
    public class PedidoController : Controller
    {    
        #region Metodos Privados

            private void CargaControles()
            { 
                var Descuentos = new FacturaBL().ObtenerPorcentajeDescuento();
                var Vendedores = new VendedorBL().ObtenerVendedoresPorAgencia(CustomHelper.getAgenciaId());    
              
                ViewBag.Descuentos = new SelectList(Descuentos, "DescuentoId", "Valor");
                ViewBag.Vendedores = new SelectList(Vendedores, "VendedorId", "Nombre");   
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

        // GET: Pedido
        [Permiso("Control.Pedido.Ver_Listado")]
        public ActionResult Index(int? page, string search, DateTime? FechaInicial, DateTime? FechaFinal)
        {
            CustomHelper.setTitle("Cotizacion", "Listado");
            List<Pedido> Pedidos = new List<Pedido>();
         
            try
            {
                if (!FechaInicial.HasValue && !FechaFinal.HasValue)
                {
                    FechaInicial = DateTime.Today;
                    FechaFinal = DateTime.Today;
                }
               
                if (!string.IsNullOrWhiteSpace(search) && search != null)
                {
                    Pedidos = new PedidoBL().Buscar(search, CustomHelper.getAgenciaId()).ToList();
                }
                else
                {
                    Pedidos = new PedidoBL().ObtenerListadoPorFecha(FechaInicial.Value, FechaFinal.Value, CustomHelper.getAgenciaId()).ToList();
                }
            }
            catch (Exception)
            {}

            ViewBag.Search = search;

            int pageSize = 15;
            int pageNumber = (page ?? 1);
            return View(Pedidos.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Pedido.Ver_Listado_Sin_Operar")]
        public ActionResult Sin_Operar(int? page)
        {
            CustomHelper.setTitle("Cotizacion Sin Operar", "Listado");
            List<Pedido> Pedidos = new List<Pedido>();

            try
            {
                Pedidos = new PedidoBL().ObtenerListadoSinOperar(CustomHelper.getAgenciaId()).ToList();
            }
            catch (Exception)
            {
            }

            if (Pedidos != null && Pedidos.Count() > 0)
            {
                ViewBag.Total = (Pedidos.Sum(y => y.Detalles.Sum(z => z.Cantidad * z.Precio))).ToString("C4");
            }
            else
            {
                ViewBag.Total = "Q0.0000";
            }
          
            int pageSize = 15;
            int pageNumber = (page ?? 1);
            return View(Pedidos.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Pedido.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Cotizacion", "Nueva");

            ViewBag.ClienteIds = 0;

            string strAtributo = "checked='checked'";

            ViewBag.cotizacionSi = "";
            ViewBag.cotizacionNo = strAtributo;

            this.CargaControles();
            return View();
        }

        [Permiso("Control.Pedido.Crear")]
        [HttpPost]
        public ActionResult Crear(Pedido modelo, bool cotizacion, string[] productoIds, string[] nombreProductoIds, long[] presentacionIds, string[] nombrePresentacionIds, decimal[] existenciaIds, decimal[] cantidadIds, decimal[] precioIds, decimal[] descuentoIds, HttpPostedFileBase fotografiaApp)
        {
            if (productoIds == null || productoIds.Length == 0)
            {
                ModelState.AddModelError("", "Para realizar una cotizacion debe de asignar productos");
            }
          
            modelo.AgenciaId = CustomHelper.getAgenciaId();
            modelo.UsrCreo = CustomHelper.getUserId();
            modelo.Cotizacion = cotizacion;
         
            modelo.Detalles = new List<PedidoDetalle>();
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
                    PedidoDetalle Detalle = new PedidoDetalle();
                    Detalle.ProductoId = productoIds[i];
                    Detalle.UnidadId = presentacionIds[i];
                    Detalle.Nombre = nombreProductoIds[i];
                    Detalle.Existencia = existenciaIds[i];
                    Detalle.Cantidad = cantidadIds[i];

                    Detalle.Descuento = descuentoIds[i];
                    Detalle.Precio = precioIds[i] - descuentoIds[i];

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

                        modelo.FotografiaCotizacion = "cotizacion.png";
                        modelo.Fotografia = new ProductoFotografia() { Nombre = fotografiaApp.FileName, Content = FileData, ContentType = fotografiaApp.ContentType, Length = fotografiaApp.ContentLength };
                    }
                }

                string strMensaje = new PedidoBL().Guardar(modelo);
                if (strMensaje.Equals("OK"))
                {
                    TempData["Pedido-Success"] = strMensaje;
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
            ViewBag.existenciaIds = existenciaIds;
            ViewBag.cantidadIds = cantidadIds;
            ViewBag.descuentoIds = descuentoIds;
            ViewBag.precioIds = precioIds;

            ViewBag.ClienteIds = modelo.ClienteId;

            string strAtributo = "checked='checked'";

            ViewBag.cotizacionSi = cotizacion == true ? strAtributo : "";
            ViewBag.cotizacionNo = cotizacion == false ? strAtributo : "";

            this.CargaControles();
            return View(modelo);
        }

        [Permiso("Control.Pedido.Crear_Tablet")]
        public ActionResult Tablet()
        {
            CustomHelper.setTitle("Pedido Tablet", "Nuevo");         

            this.CargaControles();
            return View();
        }

        [Permiso("Control.Pedido.Crear_Tablet")]
        [HttpPost]
        public ActionResult Tablet(Pedido modelo, string[] productoIds, string[] nombreProductoIds, long[] presentacionIds, string[] nombrePresentacionIds, decimal[] existenciaIds, decimal[] cantidadIds, decimal[] precioIds, decimal[] descuentoIds, HttpPostedFileBase fotografiaApp)
        {
            if (productoIds == null || productoIds.Length == 0)
            {
                ModelState.AddModelError("", "Para realizar una cotizacion debe de asignar productos");
            }

            modelo.AgenciaId = CustomHelper.getAgenciaId();
            modelo.UsrCreo = CustomHelper.getUserId();
            modelo.Cotizacion = false;

            modelo.Detalles = new List<PedidoDetalle>();
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
                    PedidoDetalle Detalle = new PedidoDetalle();
                    Detalle.ProductoId = productoIds[i];
                    Detalle.UnidadId = presentacionIds[i];
                    Detalle.Nombre = nombreProductoIds[i];
                    Detalle.Existencia = existenciaIds[i];
                    Detalle.Cantidad = cantidadIds[i];

                    Detalle.Descuento = descuentoIds[i];
                    Detalle.Precio = precioIds[i] - descuentoIds[i];

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

                        modelo.FotografiaCotizacion = "cotizacion.png";
                        modelo.Fotografia = new ProductoFotografia() { Nombre = fotografiaApp.FileName, Content = FileData, ContentType = fotografiaApp.ContentType, Length = fotografiaApp.ContentLength };
                    }
                }

                string strMensaje = new PedidoBL().Guardar(modelo);
                if (strMensaje.Equals("OK"))
                {
                    TempData["Pedido-Success"] = strMensaje;
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
            ViewBag.existenciaIds = existenciaIds;
            ViewBag.cantidadIds = cantidadIds;
            ViewBag.descuentoIds = descuentoIds;
            ViewBag.precioIds = precioIds;
            
            this.CargaControles();
            return View(modelo);
        }

        [Permiso("Control.Pedido.Editar")]
        public ActionResult Editar(long id)
        {
            Pedido PedidoActual = new PedidoBL().ObtenerPorId(id, true, true);

            if (PedidoActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Pedido", "Editar");

            if (PedidoActual.Detalles != null && PedidoActual.Detalles.Count() > 0)
            {
                ViewBag.productoIds = PedidoActual.Detalles.OrderBy(x => x.DetalleId).Select(x => x.ProductoId).ToList();
                ViewBag.nombreProductoIds = PedidoActual.Detalles.AsEnumerable().OrderBy(x => x.DetalleId).Select(x => x.Nombre).ToList();
                ViewBag.presentacionIds = PedidoActual.Detalles.OrderBy(x => x.DetalleId).Select(x => x.UnidadId).ToList();
                ViewBag.nombrePresentacionIds = PedidoActual.Detalles.OrderBy(x => x.DetalleId).Select(x => x.Unidad.Nombre).ToList();
                ViewBag.existenciaIds = 0;
                ViewBag.cantidadIds = PedidoActual.Detalles.OrderBy(x => x.DetalleId).Select(x => x.Cantidad).ToList();
                ViewBag.descuentoIds = PedidoActual.Detalles.OrderBy(x => x.DetalleId).Select(x => x.Descuento).ToList();
                ViewBag.precioIds = PedidoActual.Detalles.OrderBy(x => x.DetalleId).Select(x => x.Precio).ToList();
            }
            else
            {
                ViewBag.productoIds = "";
                ViewBag.nombreProductoIds = "";
                ViewBag.presentacionIds = "";
                ViewBag.nombrePresentacionIds = "";
                ViewBag.existenciaIds = "";
                ViewBag.cantidadIds = "";
                ViewBag.descuentoIds = "";
                ViewBag.precioIds = "";
            }           

            ViewBag.ClienteIds = PedidoActual.ClienteId;

            string strAtributo = "checked='checked'";

            ViewBag.cotizacionSi = PedidoActual.Cotizacion == true ? strAtributo : "";
            ViewBag.cotizacionNo = PedidoActual.Cotizacion == false ? strAtributo : "";

            this.CargaControles();
            return View(PedidoActual);
        }

        [Permiso("Control.Pedido.Editar")]
        [HttpPost]
        public ActionResult Editar(Pedido modelo, bool cotizacion, string[] productoIds, string[] nombreProductoIds, long[] presentacionIds, string[] nombrePresentacionIds, decimal[] existenciaIds, decimal[] cantidadIds, decimal[] precioIds, decimal[] descuentoIds, HttpPostedFileBase fotografiaApp)
        {
            if (productoIds == null || productoIds.Length == 0)
            {
                ModelState.AddModelError("", "Para realizar una cotizacion debe de asignar productos");
            }

            modelo.AgenciaId = CustomHelper.getAgenciaId();
            modelo.UsrCreo = CustomHelper.getUserId();
            modelo.Cotizacion = cotizacion;

            modelo.Detalles = new List<PedidoDetalle>();
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
                    PedidoDetalle Detalle = new PedidoDetalle();
                    Detalle.ProductoId = productoIds[i];
                    Detalle.UnidadId = presentacionIds[i];
                    Detalle.Nombre = nombreProductoIds[i];
                    Detalle.Existencia = existenciaIds[i];
                    Detalle.Cantidad = cantidadIds[i];

                    Detalle.Descuento = descuentoIds[i];
                    Detalle.Precio = precioIds[i] - descuentoIds[i];

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

                        modelo.FotografiaCotizacion = "cotizacion.png";
                        modelo.Fotografia = new ProductoFotografia() { Nombre = fotografiaApp.FileName, Content = FileData, ContentType = fotografiaApp.ContentType, Length = fotografiaApp.ContentLength };
                    }
                }

                string strMensaje = new PedidoBL().Guardar(modelo);
                if (strMensaje.Equals("OK"))
                {
                    TempData["Pedido-Success"] = strMensaje;
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
            ViewBag.existenciaIds = existenciaIds;
            ViewBag.cantidadIds = cantidadIds;
            ViewBag.descuentoIds = descuentoIds;
            ViewBag.precioIds = precioIds;

            ViewBag.ClienteIds = modelo.ClienteId;

            string strAtributo = "checked='checked'";

            ViewBag.cotizacionSi = cotizacion == true ? strAtributo : "";
            ViewBag.cotizacionNo = cotizacion == false ? strAtributo : "";

            this.CargaControles();
            return View(modelo);
        }

        [Permiso("Control.Pedido.Duplicar")]
        public ActionResult Duplicar(long id)
        {
            Pedido PedidoActual = new PedidoBL().ObtenerPorId(id, true);

            if (PedidoActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Pedido", "Duplicar");

            return View(PedidoActual);
        }

        [Permiso("Control.Pedido.Duplicar")]
        [HttpPost]
        public ActionResult Duplicar(Pedido modelo)
        {
            string strMensaje = new PedidoBL().Duplicar(modelo, CustomHelper.getUserId());
            if (strMensaje.Equals("OK"))
            {
                TempData["Pedido-Success"] = strMensaje;
                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("", strMensaje);
            }

            return View(new PedidoBL().ObtenerPorId(modelo.PedidoId, true));
        }

        [Permiso("Control.Pedido.Detalle")]
        public ActionResult Detalle(long id)
        {
            Pedido PedidoActual = new PedidoBL().ObtenerPorId(id, true);

            if (PedidoActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Pedido", "Detalle");

            return View(PedidoActual);
        }

        [Permiso("Control.Pedido.Operar")]
        public ActionResult Operar(long id)
        {
            Pedido PedidoActual = new PedidoBL().ObtenerPorId(id, true);

            if (PedidoActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Pedido", "Operar");

            return View(PedidoActual);
        }

        [Permiso("Control.Pedido.Operar")]
        [HttpPost]
        public ActionResult Operar(Pedido modelo)
        {
            string strMensaje = new PedidoBL().Operar(modelo.PedidoId, CustomHelper.getUserId());
            if (strMensaje.Contains("OK"))
            {
                string[] IDs = strMensaje.Split(';');
                TempData["Pedido_Operar-Success"] = "OK";
                return RedirectToAction("Boleta", "Recibo", new { Id = IDs[1] });
            }
            else
            {
                ModelState.AddModelError("", strMensaje);
            }

            Pedido PedidoActual = new PedidoBL().ObtenerPorId(modelo.PedidoId, true);

            if (PedidoActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Pedido", "Operar");

            return View(PedidoActual);
        }

        [Permiso("Control.Pedido.Anular")]
        public ActionResult Anular(long id)
        {
            Pedido PedidoActual = new PedidoBL().ObtenerPorId(id, true);

            if (PedidoActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Pedido", "Anular");

            return View(PedidoActual);
        }

        [Permiso("Control.Pedido.Anular")]
        [HttpPost]
        public ActionResult Anular(long pedidoId, string comentario)
        {
            string strMensaje = new PedidoBL().Anular(pedidoId, comentario, CustomHelper.getUserId());
            if (strMensaje.Equals("OK"))
            {
                TempData["Pedido_Anular-Success"] = strMensaje;
                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("", strMensaje);
            }

            Pedido PedidoActual = new PedidoBL().ObtenerPorId(pedidoId, true);

            if (PedidoActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Pedido", "Anular");

            return View(PedidoActual);
        }

        [Permiso("Control.Pedido.Boleta_Pedido")]
        public ActionResult Boleta(long Id)
        {
            Pedido PedidoActual = new PedidoBL().ObtenerPorId(Id, true);
            string PathFotografia = ConfigurationManager.AppSettings["Path_Fotografia_Cotizacion"].ToString();

            if (PedidoActual != null)
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
                
                string FotografiaActual = string.Format(@"{0}\{1}\cotizacion.png", PathFotografia, PedidoActual.PedidoId);

                if (System.IO.File.Exists(FotografiaActual))
                {
                    Fotografia = System.IO.File.ReadAllBytes(FotografiaActual);
                }
                else
                {
                    Fotografia = System.IO.File.ReadAllBytes(string.Format(@"{0}\sin_fotografia.jpg", PathFotografia));
                }

                Encabezado.Rows.Add(PedidoActual.PedidoId, PedidoActual.Agencia.Nombre, PedidoActual.Cliente.Nombre, PedidoActual.Cliente.Direccion, PedidoActual.Descripcion, PedidoActual.Fecha.ToString("dd/MM/yyyy"), 0, 0, "", PedidoActual.UsuarioCreo.Nombre, "", PedidoActual.Cliente.Nit, PedidoActual.Cliente.EmailCliente, PedidoActual.Cliente.NoTelefono, PedidoActual.FormaPago, PedidoActual.TiempoEntrega, Fotografia);

                Detalle.Columns.Add(new DataColumn("MovimientoId", typeof(long)));
                Detalle.Columns.Add(new DataColumn("ProductoId", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Nombre", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Presentacion", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Cantidad", typeof(decimal)));
                Detalle.Columns.Add(new DataColumn("Precio", typeof(decimal)));

                if (PedidoActual.Detalles != null && PedidoActual.Detalles.Count() > 0)
                {
                    foreach (var DetalleActual in PedidoActual.Detalles)
                    {
                        Detalle.Rows.Add(PedidoActual.PedidoId, DetalleActual.ProductoId, string.Format("{0} - {1}", DetalleActual.Producto.Codigo, string.IsNullOrWhiteSpace(DetalleActual.Nombre) ? DetalleActual.Producto.Nombre : DetalleActual.Nombre), DetalleActual.Unidad.Nombre, DetalleActual.Cantidad, DetalleActual.Precio);
                    }
                }
                            
                Movimiento.Tables.Add(Encabezado);
                Movimiento.Tables.Add(Detalle);
             
                // Se define la ruta del reporte
                var reportPath = Server.MapPath("~/Reports/ReportMovPedido.rdlc");

                // se obtienen los bytes del reporte en pdf
                var bytes = GetReportBytes(reportPath, Movimiento, 8.5m, 11.0m, 0.2m, 0m);
           
                return File(bytes, "application/pdf");
            }

            return View();
        }

        [ActionName("ObtenerPedidosPendientes")]
        public JsonResult ObtenerPedidosPendientes()
        {
            IList _result = new List<SelectListItem>();
            _result = new PedidoBL().ObtenerListadoSinOperarxAgencia(CustomHelper.getAgenciaId()).Select(m => new SelectListItem() { Text = m.Nombre, Value = m.PedidoId.ToString() }).ToList();
            return Json(_result, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerPedidoActual")]
        public JsonResult ObtenerPedido(long pedidoId)
        {
            if (pedidoId > 0)
            {
                MensajePedido PedidoActual = new PedidoBL().ObtenerPedido(pedidoId);
                if (PedidoActual != null)
                {
                    return Json(new { Operacion = true, Data = PedidoActual }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ConvertirCotizacionPedido")]
        public JsonResult ConvertirCotizacionPedido(long pedidoId)
        {
            if (pedidoId > 0)
            {
                string Mensaje = new PedidoBL().Convertir(pedidoId);
                if (Mensaje.Equals("OK"))
                {
                    return Json(new { Operacion = true }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [ActionName("RevivirPedido")]
        public JsonResult RevivirPedido(long pedidoId)
        {
            if (pedidoId > 0)
            {
                string Mensaje = new PedidoBL().Revivir(pedidoId);
                if (Mensaje.Equals("OK"))
                {
                    return Json(new { Operacion = true }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetEscalaPreciosxProducto(string id)
        {
            return PartialView("_ProductoNivelPrecio", new ProductoBL().ObtenerEscalaPreciosxProducto(id));
        }
    }
}