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
    public class FacturaController : Controller
    {       
        #region Metodos Privados

            private void CargaControles()
            {
                var Tipos = new FacturaTipoBL().ObtenerListado();
                var Vendedores = new VendedorBL().ObtenerVendedoresPorAgencia(CustomHelper.getAgenciaId());             
                var Descuentos = new FacturaBL().ObtenerPorcentajeDescuento();
                var Repartos = new List<ComboModel>() { new ComboModel() { ID = 1, Nombre = "Sí" }, new ComboModel() { ID = 2, Nombre = "No" } };
                var Transportes = new TransporteBL().ObtenerListado().AsEnumerable().Select(x => new Transporte() { TransporteId = x.TransporteId, Nombre = string.Format("{0} - {1}", x.Nombre, x.DescripcionEmpaque) }).ToList();

                ViewBag.Tipos = new SelectList(Tipos, "FacturaTipoId", "Nombre");
                ViewBag.Vendedores = new SelectList(Vendedores, "VendedorId", "Nombre");              
                ViewBag.Descuentos = new SelectList(Descuentos, "DescuentoId", "Valor");
                ViewBag.Repartos = new SelectList(Repartos, "ID", "Nombre");
                ViewBag.Transportes = new SelectList(Transportes, "TransporteId", "Nombre");
               
                this.CargaSeries();
                this.CargaFormas();
                this.CargaTransportes();
            }

            private void CargaSeries() 
            {
                var Series = new SerieBL().ObtenerSeriesPorAgencia(CustomHelper.getAgenciaId());

                ViewBag.Series = new SelectList(Series, "SerieId", "Nombre");
            }

            private void CargaFormas()
            {
                var Formas = new FormaPagoBL().ObtenerListado(false, CustomHelper.getEmpresaId());

                ViewBag.Formas = new SelectList(Formas, "FormaPagoId", "Nombre");
            }

            private void CargaTransportes()
            {
                var Transportes = new TransporteBL().ObtenerListado().AsEnumerable().Select(x => new Transporte() { TransporteId = x.TransporteId, Nombre = string.Format("{0} - {1}", x.Nombre, x.DescripcionEmpaque) }).ToList();

                ViewBag.Transportes = new SelectList(Transportes, "TransporteId", "Nombre");
            }
     
            private byte[] GetReportBytes(string reportPath, DataSet reportDataSource, decimal pageWidth = 13.38m, decimal pageHeight = 8.5m, decimal MarginLeft = 1m, decimal MarginRight = 1m)
            {

                byte[] reportBytes = null;

                // Se crea la instancia del reporte y se cargan sus datos.
                LocalReport reporte = new LocalReport() { ReportPath = reportPath };
                reporte.DataSources.Add(new ReportDataSource("MovimientoEncabezado", reportDataSource.Tables[0]));
                reporte.DataSources.Add(new ReportDataSource("MovimientoDetalle", reportDataSource.Tables[1]));
                reporte.DataSources.Add(new ReportDataSource("MovimientoControl", reportDataSource.Tables[2]));

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

        // GET: Factura
        [Permiso("Control.Factura.Ver_Listado")]
        public ActionResult Index(int? page, long? serie, string factura, DateTime? FechaInicial, DateTime? FechaFinal)
        {
            CustomHelper.setTitle("Factura", "Listado");
            List<Factura> Facturas = new List<Factura>();
         
            try
            {
                if (!FechaInicial.HasValue && !FechaFinal.HasValue)
                {
                    FechaInicial = DateTime.Today;
                    FechaFinal = DateTime.Today;
                }

                if (!string.IsNullOrWhiteSpace(factura) && factura != null)
                {
                    Facturas = new FacturaBL().BuscarFactura(serie, factura, CustomHelper.getAgenciaId());  
                }
                else
                {
                    Facturas = new FacturaBL().ObtenerListadoPorFecha(FechaInicial.Value, FechaFinal.Value, CustomHelper.getAgenciaId()).ToList();
                }                
            }
            catch (Exception)
            {}

            this.CargaSeries();
            ViewBag.serie = serie;
            ViewBag.factura = factura;

            int pageSize = 15;
            int pageNumber = (page ?? 1);
            return View(Facturas.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Factura.Ver_Listado_Sin_Despachar")]
        public ActionResult Sin_Despachar(int? page)
        {
            CustomHelper.setTitle("Factura x Despachar", "Listado");
            List<Factura> Facturas = new List<Factura>();

            try
            {
                Facturas = new FacturaBL().ObtenerListadoSinDespachar(CustomHelper.getAgenciaId()).ToList();
            }
            catch (Exception)
            {}

            int pageSize = 15;
            int pageNumber = (page ?? 1);
            return View(Facturas.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Factura.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Factura", "Nueva");

            string strAtributo = "checked='checked'";
        
            ViewBag.PagadaSi = strAtributo;
            ViewBag.PagadaNo = "";

            ViewBag.ClienteIds = 0;

            this.CargaControles();
            return View();
        }

        [Permiso("Control.Factura.Crear")]
        public ActionResult Crear_Correlativo(long id)
        {
            CustomHelper.setTitle("Factura", "Nueva");

            string Ticket = new ServicioClienteBL().Visualizar(id);

            ViewBag.ClienteIds = 0;

            this.CargaControles();
            return View(new Factura() { TicketId = id, Ticket = Ticket });
        }

        [Permiso("Control.Factura.Crear")]
        [HttpPost]
        public ActionResult Crear(Factura modelo, bool pagada, string[] productoIds, string[] nombreProductoIds, long[] presentacionIds, string[] nombrePresentacionIds, decimal[] existenciaIds, decimal[] cantidadIds, decimal[] precioIds, long[] formaIds, decimal[] pagarIds, string[] notaIds, decimal[] descuentoIds, string[] idIds)
        {
            if (productoIds == null || productoIds.Length == 0)
            {
                ModelState.AddModelError("", "Para realizar una venta debe de asignar productos");
            }

            modelo.Pagos = new List<FacturaFormaPago>();

            if (pagada)
            {
                if (formaIds != null && formaIds.Length > 0)
                {
                    for (int i = 0; i < formaIds.Length; i++)
                    {
                        FacturaFormaPago Forma = new FacturaFormaPago();
                        Forma.FormaPagoId = formaIds[i];
                        Forma.Valor = pagarIds[i];
                        Forma.Nota = notaIds[i];

                        modelo.Pagos.Add(Forma);
                    }
                }
            }

            if (pagada)
            {
                if (modelo.Pagos.Count() == 0)
                {
                    ModelState.AddModelError("", "Se le informa que la factura que ingreso no contiene ningún registro de pago, lo cual no es valido");
                }
            }         
                             
            modelo.Empleado = false;
            modelo.Credito = false;
            modelo.DiaCredito = 0;
            modelo.AgenciaId = CustomHelper.getAgenciaId();
            modelo.UsrCreo = CustomHelper.getUserId();
            modelo.FacturaElectronica = false;
            modelo.Reparto = modelo.RepartoId == 1 ? true : false;
            modelo.Pagada = pagada;
            modelo.ServicioCliente = false;
            modelo.EntregadoTransporte = false;
            modelo.SerieId = 20200520001;
            modelo.NoFactura = 0;
            modelo.EmpresaId = CustomHelper.getEmpresaId();

            modelo.Detalles = new List<FacturaDetalle>();
            for (int i = 0; i < productoIds.Length; i++)
            {
                FacturaDetalle Detalle = new FacturaDetalle();
                Detalle.ProductoId = productoIds[i];
                Detalle.UnidadId = presentacionIds[i];
                Detalle.Nombre = nombreProductoIds[i];
                Detalle.Existencia = existenciaIds[i];
                Detalle.Cantidad = cantidadIds[i];

                Detalle.Descuento = descuentoIds[i];
                Detalle.Precio = precioIds[i] - descuentoIds[i];

                if (idIds != null)
                {
                    Detalle.ID = idIds[i];
                }          

                modelo.Detalles.Add(Detalle);           
            }
            
            if (pagada)
            {
                if (modelo.Detalles != null && modelo.Detalles.Count() > 0 && modelo.Pagos != null && modelo.Pagos.Count() > 0)
                {
                    decimal TotalFactura = decimal.Round(modelo.Detalles.Sum(x => x.Cantidad * x.Precio), 4);
                    decimal TotalPago = modelo.Pagos.Sum(x => x.Valor);

                    if (TotalFactura != TotalPago)
                    {
                        ModelState.AddModelError("", string.Format("El monto de la factura es de: {0:C4} y el monto de pago es de: {1:C4}", TotalFactura, TotalPago));
                    }
                }
            }

            if (ModelState.IsValid)
            {
                string strMensaje = new FacturaBL().Guardar(modelo);
                if (strMensaje.Equals("OK"))
                {
                    TempData["Factura-Success"] = strMensaje;
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
            ViewBag.idIds = idIds;

            ViewBag.formaIds = formaIds;
            ViewBag.pagarIds = pagarIds;
            ViewBag.notaIds = notaIds;

            string strAtributo = "checked='checked'";

            ViewBag.PagadaSi = pagada == true ? strAtributo : "";
            ViewBag.PagadaNo = pagada == false ? strAtributo : "";

            ViewBag.ClienteIds = modelo.ClienteId;

            this.CargaControles();
            return View(modelo);
        }

        [Permiso("Control.Factura.Crear")]
        [HttpPost]
        public ActionResult Crear_Correlativo(Factura modelo, string[] productoIds, string[] nombreProductoIds, long[] presentacionIds, string[] nombrePresentacionIds, decimal[] existenciaIds, decimal[] cantidadIds, decimal[] precioIds, long[] formaIds, decimal[] pagarIds, string[] notaIds, decimal[] descuentoIds, string[] idIds)
        {
            if (productoIds == null || productoIds.Length == 0)
            {
                ModelState.AddModelError("", "Para realizar una venta debe de asignar productos");
            }

            if (formaIds == null || formaIds.Length == 0)
            {
                ModelState.AddModelError("", "Para realizar una factura debe de ingresar la forma de pago");
            }
            else
            {
                modelo.Pagos = new List<FacturaFormaPago>();
                for (int i = 0; i < formaIds.Length; i++)
                {
                    FacturaFormaPago Forma = new FacturaFormaPago();
                    Forma.FormaPagoId = formaIds[i];
                    Forma.Valor = pagarIds[i];
                    Forma.Nota = notaIds[i];

                    modelo.Pagos.Add(Forma);
                }
            }

            if (modelo.NoFactura == 0)
            {
                ModelState.AddModelError("", "Para realizar una venta debe de asignar un no. de factura");
            }

            modelo.Empleado = false;
            modelo.Credito = false;
            modelo.DiaCredito = 0;
            modelo.AgenciaId = CustomHelper.getAgenciaId();
            modelo.UsrCreo = CustomHelper.getUserId();
            modelo.FacturaElectronica = false;
            modelo.Reparto = modelo.RepartoId == 1 ? true : false;
            modelo.Pagada = true;
            modelo.ServicioCliente = true;
            modelo.EntregadoTransporte = false;

            modelo.Detalles = new List<FacturaDetalle>();
            for (int i = 0; i < productoIds.Length; i++)
            {
                FacturaDetalle Detalle = new FacturaDetalle();
                Detalle.ProductoId = productoIds[i];
                Detalle.UnidadId = presentacionIds[i];
                Detalle.Existencia = existenciaIds[i];
                Detalle.Cantidad = cantidadIds[i];

                Detalle.Descuento = descuentoIds[i];
                Detalle.Precio = precioIds[i] - descuentoIds[i];
                Detalle.ID = idIds[i];

                modelo.Detalles.Add(Detalle);
            }

            if (modelo.Detalles != null && modelo.Detalles.Count() > 0)
            {
                bool ExistenciaNoValida = modelo.Detalles.Where(x => x.Cantidad > x.Existencia).Count() > 0;
                if (ExistenciaNoValida)
                {
                    ModelState.AddModelError("", "Hay producto(s) que sobre pasan las existencias");
                }
            }

            if (ModelState.IsValid)
            {
                string strMensaje = new FacturaBL().Guardar(modelo);
                if (strMensaje.Equals("OK"))
                {

                    using (var db = new GiveContext())
                    {
                        var agencia = db.Agencias.FirstOrDefault(a => a.AgenciaId == modelo.AgenciaId);
                        var serie = db.Series.FirstOrDefault(x => x.SerieId == modelo.SerieId);

                        if (agencia != null && serie != null)
                        {
                            foreach (var p in modelo.Detalles)
                            {
                                var productoId = p.ProductoId;
                                var producto = db.Productos.Include(pr => pr.Marca).FirstOrDefault(pr => pr.ProductoId == productoId);
                                var existencia = db.ProductoInventarios.FirstOrDefault(pr => pr.ProductoId == productoId && pr.AgenciaId == agencia.AgenciaId);
                                decimal existenciaActual = 0;

                                if (producto == null) continue;

                                if (existencia != null)
                                {
                                    existenciaActual = existencia.Cantidad;
                                }

                                db.RegistrosKardex.Add(new RegistroKardex
                                {
                                    FechaHora = DateTime.Now,
                                    Fecha = DateTime.Today,
                                    ProductoId = p.ProductoId,
                                    ProductoCodigo = producto.Codigo,
                                    ProductoNombre = producto.Nombre,
                                    ProductoDescripcion = producto.Descripcion,
                                    MarcaId = producto.MarcaId,
                                    MarcaNombre = producto.Marca.Nombre,
                                    DocumentoNumero = serie.Nombre + "-" + modelo.NoFactura,
                                    AgenciaId = modelo.AgenciaId,
                                    AgenciaNombre = agencia.Nombre,
                                    TipoRegistro = "Factura",
                                    SalidaCantidadTienda = p.Cantidad,
                                    SalidaCostoTienda = p.Precio,
                                    ExistenciaFinalTienda = existenciaActual
                                });
                            }

                            db.SaveChanges();
                        }
                    }

                    TempData["Factura-Success"] = strMensaje;
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
            ViewBag.idIds = idIds;

            ViewBag.formaIds = formaIds;
            ViewBag.pagarIds = pagarIds;
            ViewBag.notaIds = notaIds;

            ViewBag.ClienteIds = modelo.ClienteId;

            this.CargaControles();
            return View(modelo);
        }

        [Permiso("Control.Factura.Crear")]
        public ActionResult Certificar(long id)
        {
            Factura FacturaActual = new FacturaBL().ObtenerPorId(id, true, true, false);

            if (FacturaActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Factura", "Certificar");

            return View(FacturaActual);
        }

        [Permiso("Control.Factura.Crear")]
        [HttpPost]
        public ActionResult Certificar(Factura modelo)
        {
            string strMensaje = new FacturaBL().Certificar(modelo);
            if (strMensaje.Equals("OK"))
            {
                TempData["Factura-Success"] = strMensaje;
                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("", strMensaje);
            }

            return View(new FacturaBL().ObtenerPorId(modelo.FacturaId, true, true, false));
        }

        [Permiso("Control.Factura.Anular")]
        public ActionResult Anular(long id)
        {
            Factura FacturaActual = new FacturaBL().ObtenerPorId(id, true, true, false);

            if (FacturaActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Factura", "Anular");

            return View(FacturaActual);
        }

        [Permiso("Control.Factura.Anular")]
        [HttpPost]
        public ActionResult Anular(long facturaId, string comentario)
        {
            string strMensaje = new FacturaBL().Anular(facturaId, comentario, CustomHelper.getUserId());
            if (strMensaje.Equals("OK"))
            {
                TempData["Factura_Anular-Success"] = strMensaje;
                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("", strMensaje);
            }

            Factura FacturaActual = new FacturaBL().ObtenerPorId(facturaId, true, true, false);

            if (FacturaActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Factura", "Anular");

            return View(FacturaActual);
        }

        [Permiso("Control.Factura.Nota_Credito")]
        public ActionResult Nota_Credito(long id)
        {
            Factura FacturaActual = new FacturaBL().ObtenerPorId(id, true, true, false);

            if (FacturaActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Factura", "Nota de Credito");

            return View(FacturaActual);
        }

        [Permiso("Control.Factura.Nota_Credito")]
        [HttpPost]
        public ActionResult Nota_Credito(Factura modelo, string[] productoIds, string[] nombreProductoIds, long[] presentacionIds, string[] nombrePresentacionIds, decimal[] existenciaIds, decimal[] cantidadIds, decimal[] precioIds)
        {
            if (productoIds == null || productoIds.Length == 0)
            {
                ModelState.AddModelError("", "Para realizar una nota de credito debe de asignar productos");
            }

            modelo.Detalles = new List<FacturaDetalle>();
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
                    FacturaDetalle Detalle = new FacturaDetalle();
                    Detalle.ProductoId = productoIds[i];
                    Detalle.UnidadId = presentacionIds[i];
                    Detalle.Nombre = nombreProductoIds[i];
                    Detalle.Existencia = existenciaIds[i];
                    Detalle.Cantidad = cantidadIds[i];                 
                    Detalle.Precio = precioIds[i];

                    modelo.Detalles.Add(Detalle);
                }
            }

            if (modelo.Detalles != null && modelo.Detalles.Count() > 0)
            {
                bool ExistenciaNoValida = modelo.Detalles.Where(x => x.Cantidad > x.Existencia).Count() > 0;
                if (ExistenciaNoValida)
                {
                    ModelState.AddModelError("", "Hay producto(s) que sobre pasan las existencias");
                }
            }

            if (ModelState.IsValid)
            {
                modelo.UsrCreo = CustomHelper.getUserId();
                string strMensaje = new FacturaBL().GenerarNotaCredito(modelo);
                if (strMensaje.Equals("OK"))
                {
                    TempData["Factura_Nota_Credito-Success"] = strMensaje;
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
            ViewBag.precioIds = precioIds;

            return View(new FacturaBL().ObtenerPorId(modelo.FacturaId, true, true, false));
        }

        [Permiso("Control.Factura.Detalle")]
        public ActionResult Detalle(long id)
        {
            Factura FacturaActual = new FacturaBL().ObtenerPorId(id, true, true, false);

            if (FacturaActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Factura", "Detalle");

            return View(FacturaActual);
        }

        [Permiso("Control.Factura.Asignar_Transporte")]
        public ActionResult Asignar_Transporte(long id)
        {
            Factura FacturaActual = new FacturaBL().ObtenerPorId(id, true, true, false);

            if (FacturaActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Factura", "Asignar Transporte");

            this.CargaTransportes();
            return View(FacturaActual);
        }

        [Permiso("Control.Factura.Asignar_Transporte")]
        [HttpPost]
        public ActionResult Asignar_Transporte(long facturaId, long transporteId)
        {
            string strMensaje = new FacturaBL().AsignarTransporte(facturaId, transporteId);
            if (strMensaje.Equals("OK"))
            {
                TempData["Factura_Asignar_Transporte-Success"] = strMensaje;
                return RedirectToAction("Sin_Despachar");
            }
            else
            {
                ModelState.AddModelError("", strMensaje);
            }

            Factura FacturaActual = new FacturaBL().ObtenerPorId(facturaId, true, true, false);

            if (FacturaActual == null)
            {
                return HttpNotFound();
            }
           
            this.CargaTransportes();
            return View(FacturaActual);
        }

        [Permiso("Control.Factura.Asignar_Lote")]
        public ActionResult Asignar_Lote(long id)
        {
            Factura FacturaActual = new FacturaBL().ObtenerPorId(id, true, true, false);

            if (FacturaActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Factura", "Asignar Lote");

            return View(FacturaActual);
        }

        [Permiso("Control.Factura.Asignar_Lote")]
        [HttpPost]
        public ActionResult Asignar_Lote(Factura modelo, string[] productoIds, string[] nombreProductoIds, string[] loteIds, decimal[] existenciaIds, decimal[] cantidadIds)
        {
            if (productoIds == null || productoIds.Length == 0)
            {
                ModelState.AddModelError("", "Para realizar una asignacion de lote debe de asignar productos");
            }

            modelo.Lotes = new List<FacturaLote>();
            for (int i = 0; i < productoIds.Length; i++)
            {
                FacturaLote Detalle = new FacturaLote();
                Detalle.ProductoId = productoIds[i];
                Detalle.Lote = loteIds[i];
                Detalle.Cantidad = cantidadIds[i];

                modelo.Lotes.Add(Detalle);
            }

            string strMensaje = new FacturaBL().GuardarLote(modelo);
            if (strMensaje.Equals("OK"))
            {
                TempData["Factura_Lote-Success"] = strMensaje;
                return RedirectToAction("Sin_Despachar");
            }
            else
            {
                ModelState.AddModelError("", strMensaje);
            }

            ViewBag.productoIds = productoIds;
            ViewBag.nombreProductoIds = nombreProductoIds;
            ViewBag.loteIds = loteIds;
            ViewBag.existenciaIds = existenciaIds;
            ViewBag.cantidadIds = cantidadIds;

            return View(new FacturaBL().ObtenerPorId(modelo.FacturaId, true, true, false));
        }

        [Permiso("Control.Reporte.Boleta_Factura")]
        public ActionResult Boleta(long Id)
        {
            Factura FacturaActual = new FacturaBL().ObtenerPorId(Id, true, true, false, true);

            if (FacturaActual != null)
            {
                DataSet Movimiento = new DataSet("Inventario");

                DataTable Encabezado = new DataTable("MovimientoEncabezado");
                DataTable Detalle = new DataTable("MovimientoDetalle");
                DataTable Control = new DataTable("MovimientoControl");

                Encabezado.Columns.Add(new DataColumn("MovimientoId", typeof(long)));
                Encabezado.Columns.Add(new DataColumn("Agencia", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Nombre", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Direccion", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Descripcion", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Fecha", typeof(DateTime)));
                Encabezado.Columns.Add(new DataColumn("Descuento", typeof(decimal)));
                Encabezado.Columns.Add(new DataColumn("Total", typeof(decimal)));
                Encabezado.Columns.Add(new DataColumn("Categoria", typeof(string)));

                Numalet Convetir = new Numalet();
                Encabezado.Rows.Add(FacturaActual.FacturaId, FacturaActual.Agencia.Nombre, FacturaActual.Cliente.Nombre, FacturaActual.Cliente.Direccion, FacturaActual.Cliente.Nit, FacturaActual.Fecha.ToString("dd/MM/yyyy"), FacturaActual.DescuentoTotal, FacturaActual.Total, Convetir.ToCustomCardinal(FacturaActual.Total).ToUpper());

                Detalle.Columns.Add(new DataColumn("MovimientoId", typeof(long)));
                Detalle.Columns.Add(new DataColumn("ProductoId", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Nombre", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Presentacion", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Cantidad", typeof(int)));
                Detalle.Columns.Add(new DataColumn("Precio", typeof(decimal)));

                if (FacturaActual.Detalles != null && FacturaActual.Detalles.Count() > 0)
                {
                    foreach (var DetalleActual in FacturaActual.Detalles)
                    {
                        if (!string.IsNullOrWhiteSpace(DetalleActual.ID))
                        {
                            //Detalle.Rows.Add(FacturaActual.FacturaId, DetalleActual.ProductoId, string.Format("{0} - {1}(IDs: {2})", DetalleActual.Producto.Codigo, string.IsNullOrWhiteSpace(DetalleActual.Nombre) ? DetalleActual.Producto.Nombre : DetalleActual.Nombre, DetalleActual.ID), DetalleActual.Unidad.Nombre, DetalleActual.Cantidad, DetalleActual.Precio);
                            Detalle.Rows.Add(FacturaActual.FacturaId, DetalleActual.ProductoId, string.Format("{0}(IDs: {1})", string.IsNullOrWhiteSpace(DetalleActual.Nombre) ? DetalleActual.Producto.Nombre : DetalleActual.Nombre, DetalleActual.ID), DetalleActual.Unidad.Nombre, DetalleActual.Cantidad, DetalleActual.Precio);
                        }
                        else
                        {
                            Detalle.Rows.Add(FacturaActual.FacturaId, DetalleActual.ProductoId, string.Format("{0}", string.IsNullOrWhiteSpace(DetalleActual.Nombre) ? DetalleActual.Producto.Nombre : DetalleActual.Nombre), DetalleActual.Unidad.Nombre, DetalleActual.Cantidad, DetalleActual.Precio);
                        }
                    }
                }

                Control.Columns.Add(new DataColumn("MovimientoId", typeof(long)));
                Control.Columns.Add(new DataColumn("Factura", typeof(string)));
                Control.Columns.Add(new DataColumn("FormaPago", typeof(string)));

                Control.Rows.Add(FacturaActual.FacturaId, FacturaActual.Documento, FacturaActual.FormaPago);

                Movimiento.Tables.Add(Encabezado);
                Movimiento.Tables.Add(Detalle);
                Movimiento.Tables.Add(Control);

                // Se define la ruta del reporte
                var reportPath = Server.MapPath("~/Reports/ReportMovFactura.rdlc");

                // se obtienen los bytes del reporte en pdf
                var bytes = GetReportBytes(reportPath, Movimiento, 8.5m, 11.0m, 0m, 0m);
           
                return File(bytes, "application/pdf");
            }

            return View();
        }

        [Permiso("Control.Recibo.Asignar_Lote")]
        public ActionResult Boleta_Lote(long id)
        {
            Factura FacturaActual = new FacturaBL().ObtenerPorId(id, true, true, false, true);

            if (FacturaActual != null)
            {
                DataSet Movimiento = new DataSet("Inventario");

                DataTable Encabezado = new DataTable("MovimientoEncabezado");
                DataTable Detalle = new DataTable("MovimientoDetalle");
                DataTable Control = new DataTable("MovimientoControl");

                Encabezado.Columns.Add(new DataColumn("MovimientoId", typeof(long)));
                Encabezado.Columns.Add(new DataColumn("Agencia", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Nombre", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Direccion", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Descripcion", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Fecha", typeof(DateTime)));
                Encabezado.Columns.Add(new DataColumn("Descuento", typeof(decimal)));
                Encabezado.Columns.Add(new DataColumn("Total", typeof(decimal)));
                Encabezado.Columns.Add(new DataColumn("Categoria", typeof(string)));

                Numalet Convetir = new Numalet();
                Encabezado.Rows.Add(FacturaActual.FacturaId, FacturaActual.Agencia.Nombre, FacturaActual.Cliente.Nombre, FacturaActual.Cliente.Direccion, FacturaActual.Cliente.Nit, FacturaActual.Fecha.ToString("dd/MM/yyyy"), FacturaActual.DescuentoTotal, FacturaActual.Total, Convetir.ToCustomCardinal(FacturaActual.Total).ToUpper());

                Detalle.Columns.Add(new DataColumn("MovimientoId", typeof(long)));
                Detalle.Columns.Add(new DataColumn("ProductoId", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Nombre", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Presentacion", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Cantidad", typeof(int)));
                Detalle.Columns.Add(new DataColumn("Precio", typeof(decimal)));

                if (FacturaActual.Lotes != null && FacturaActual.Lotes.Count() > 0)
                {
                    foreach (var DetalleActual in FacturaActual.Lotes)
                    {
                        Detalle.Rows.Add(FacturaActual.FacturaId, DetalleActual.ProductoId, string.Format("{0} - {1}", DetalleActual.Producto.Codigo, DetalleActual.Producto.Nombre), string.Format("{0} - F. Vencimiento: {1}", DetalleActual.Lote, DetalleActual.FechaVencimiento.ToString("dd/MM/yyyy")), DetalleActual.Cantidad, 0);
                    }
                }

                Control.Columns.Add(new DataColumn("MovimientoId", typeof(long)));
                Control.Columns.Add(new DataColumn("Factura", typeof(string)));
                Control.Columns.Add(new DataColumn("FormaPago", typeof(string)));

                Control.Rows.Add(FacturaActual.FacturaId, FacturaActual.Documento, FacturaActual.FormaPago);

                Movimiento.Tables.Add(Encabezado);
                Movimiento.Tables.Add(Detalle);
                Movimiento.Tables.Add(Control);

                // Se define la ruta del reporte
                var reportPath = Server.MapPath("~/Reports/ReportMovComprobantexLote.rdlc");

                // se obtienen los bytes del reporte en pdf
                var bytes = GetReportBytes(reportPath, Movimiento, 8.5m, 11.0m, 0m, 0m);

                return File(bytes, "application/pdf");
            }

            return View();
        }

        [Permiso("Control.Reporte.Boleta_Nota_Credito")]
        public ActionResult Boleta_Nota_Credito(long id)
        {
            Factura FacturaActual = new FacturaBL().ObtenerPorId(id, true, true, false, true);
            FacturaNotaCredito FacturaNotaCreditoActual = new FacturaBL().ObtenerNotaCreditoxId(id);

            if (FacturaActual != null && FacturaNotaCreditoActual != null)
            {
                DataSet Movimiento = new DataSet("Inventario");

                DataTable Encabezado = new DataTable("MovimientoEncabezado");
                DataTable Detalle = new DataTable("MovimientoDetalle");
                DataTable Control = new DataTable("MovimientoControl");

                Encabezado.Columns.Add(new DataColumn("MovimientoId", typeof(long)));
                Encabezado.Columns.Add(new DataColumn("Agencia", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Nombre", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Direccion", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Descripcion", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Fecha", typeof(DateTime)));
                Encabezado.Columns.Add(new DataColumn("Descuento", typeof(decimal)));
                Encabezado.Columns.Add(new DataColumn("Total", typeof(decimal)));
                Encabezado.Columns.Add(new DataColumn("Categoria", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("FechaHoraCertificacionFEL", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("NumeroFEL", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("SerieFEL", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("UUIDFEL", typeof(string)));

                Encabezado.Rows.Add(FacturaActual.FacturaId, FacturaActual.Agencia.Nombre, FacturaActual.Cliente.Nombre, FacturaActual.Cliente.Direccion, FacturaActual.Cliente.Nit, FacturaActual.Fecha.ToString("dd/MM/yyyy"), 0, FacturaActual.Total, "", FacturaNotaCreditoActual.FechaHoraCertificacionFEL, FacturaNotaCreditoActual.NumeroFEL, FacturaNotaCreditoActual.SerieFEL, FacturaNotaCreditoActual.UUIDFEL);

                Detalle.Columns.Add(new DataColumn("MovimientoId", typeof(long)));
                Detalle.Columns.Add(new DataColumn("ProductoId", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Nombre", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Presentacion", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Cantidad", typeof(int)));
                Detalle.Columns.Add(new DataColumn("Precio", typeof(decimal)));

                if (FacturaNotaCreditoActual.Detalles != null && FacturaNotaCreditoActual.Detalles.Count() > 0)
                {
                    foreach (var DetalleActual in FacturaNotaCreditoActual.Detalles)
                    {
                        Detalle.Rows.Add(FacturaActual.FacturaId, DetalleActual.ProductoId, string.IsNullOrWhiteSpace(DetalleActual.Nombre) ? string.Format("{0} - {1}", DetalleActual.Producto.Codigo, DetalleActual.Producto.Nombre) : DetalleActual.Nombre, DetalleActual.Unidad.Nombre, DetalleActual.Cantidad, DetalleActual.Precio);
                    }
                }

                Control.Columns.Add(new DataColumn("MovimientoId", typeof(long)));
                Control.Columns.Add(new DataColumn("Factura", typeof(string)));
                Control.Columns.Add(new DataColumn("FormaPago", typeof(string)));

                Control.Rows.Add(FacturaActual.FacturaId, FacturaActual.Documento, FacturaActual.FormaPago);

                Movimiento.Tables.Add(Encabezado);
                Movimiento.Tables.Add(Detalle);
                Movimiento.Tables.Add(Control);

                // Se define la ruta del reporte
                var reportPath = Server.MapPath("~/Reports/ReportMovNota.rdlc");

                // se obtienen los bytes del reporte en pdf
                var bytes = GetReportBytes(reportPath, Movimiento, 8.5m, 5.5m, 0m, 0m);

                return File(bytes, "application/pdf");
            }

            return View();
        }

        [ActionName("ObtenerFacturaActual")]
        public JsonResult ObtenerFactura(long serieId)
        {
            if (serieId > 0)
            {
                SerieAgenciaFactura FacturaActual = new SerieBL().ObtenerFacturaActual(CustomHelper.getAgenciaId(), serieId);
                if (FacturaActual != null && FacturaActual.Factura > 0)
                {
                    return Json(new { Operacion = true, Data = FacturaActual }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerFacturaxSerie")]
        public JsonResult ObtenerFacturaxSerie(long serieId, long factura)
        {
            if (serieId > 0 && factura > 0)
            {
                FacturaModel FacturaActual = new FacturaBL().ObtenerFacturaxSerie(serieId, factura);
                if (FacturaActual != null && FacturaActual.FacturaId > 0)
                {
                    return Json(new { Operacion = true, Data = FacturaActual }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ActualizarFacturaPagada")]
        public JsonResult ActualizarFacturaPagada(long facturaId)
        {
            if (facturaId > 0)
            {
                string Mensaje = new FacturaBL().Pagar(facturaId);
                if (Mensaje.Equals("OK"))
                {
                    return Json(new { Operacion = true }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerFacturaGarantiaActual")]
        public JsonResult ObtenerFacturaGarantiaActual(long serieId, long factura)
        {
            if (serieId > 0 && factura > 0)
            {
                FacturaGarantia FacturaActual = new FacturaBL().ObtenerProductosFactura(serieId, factura);
                if (FacturaActual != null)
                {
                    return Json(new { Operacion = true, Data = FacturaActual }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ActualizarFacturaDespachado")]
        public JsonResult ActualizarFacturaDespachado(long facturaId)
        {
            if (facturaId > 0)
            {
                string Mensaje = new FacturaBL().Despachar(facturaId, CustomHelper.getUserId());
                if (Mensaje.Equals("OK"))
                {
                    return Json(new { Operacion = true }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ActionName("EnviarCorreo")]
        public JsonResult EnviarCorreo(long id)
        {
            if (id > 0)
            {
                string Mensaje = new FacturaBL().EnviarCorreo(id);
                if (Mensaje.Equals("OK"))
                {
                    return Json(new { Operacion = true }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerSaldoFacturas")]
        public JsonResult ObtenerSaldoFacturas()
        {
            return Json(new { Operacion = true, Data = new FacturaBL().ObtenerSaldoFacturas(CustomHelper.getEmpresaId()) }, JsonRequestBehavior.AllowGet);
        }
    }
}