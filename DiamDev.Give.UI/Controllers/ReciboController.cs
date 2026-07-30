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
    public class ReciboController : Controller
    {       
        #region Metodos Privados

            private void CargaControles()
            {
                var Tipos = new ReciboTipoBL().ObtenerListado();
                var Vendedores = new VendedorBL().ObtenerVendedoresPorAgencia(CustomHelper.getAgenciaId());
            var Agencias = new AgenciaBL().ObtenerListado(true, CustomHelper.getUserId());
            var Descuentos = new FacturaBL().ObtenerPorcentajeDescuento();
                var Repartos = new List<ComboModel>() { new ComboModel() { ID = 1, Nombre = "Sí" }, new ComboModel() { ID = 2, Nombre = "No" } };                

                ViewBag.Tipos = new SelectList(Tipos, "ReciboTipoId", "Nombre");
                ViewBag.Vendedores = new SelectList(Vendedores, "VendedorId", "Nombre");              
                ViewBag.Descuentos = new SelectList(Descuentos, "DescuentoId", "Valor");
                ViewBag.Repartos = new SelectList(Repartos, "ID", "Nombre");
                ViewBag.Agencias = new SelectList(Agencias, "AgenciaId", "Nombre");






            this.CargaFormas();
                this.CargaTransportes();
            }

            private void CargaFormas()
            {
                var Formas = new FormaPagoBL().ObtenerListado(false, 0);

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

        // GET: Recibo
        [Permiso("Control.Recibo.Ver_Listado")]
        public ActionResult Index(int? page, string recibo, DateTime? FechaInicial, DateTime? FechaFinal)
        {
            CustomHelper.setTitle("Recibo", "Listado");
            List<Recibo> Recibos = new List<Recibo>();
         
            try
            {
                if (!FechaInicial.HasValue && !FechaFinal.HasValue)
                {
                    FechaInicial = DateTime.Today;
                    FechaFinal = DateTime.Today;
                }

                if (!string.IsNullOrWhiteSpace(recibo) && recibo != null)
                {
                    Recibos = new ReciboBL().BuscarRecibo(recibo, CustomHelper.getUserId(), false);  
                }
                else
                {
                    Recibos = new ReciboBL().ObtenerListadoPorFecha(FechaInicial.Value, FechaFinal.Value, CustomHelper.getUserId(), false).ToList();
                }                
            }
            catch (Exception)
            {}

            ViewBag.fechaInicial = FechaInicial.Value.ToString("yyyy-MM-dd");
            ViewBag.fechaFinal = FechaFinal.Value.ToString("yyyy-MM-dd");
            ViewBag.recibo = recibo;

            int pageSize = 15;
            int pageNumber = (page ?? 1);
            return View(Recibos.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Recibo.Ver_Listado")]
        public ActionResult Venta_x_Recibo(int? page, string search)
        {
            CustomHelper.setTitle("Venta x Recibo", "Listado");
            List<Recibo> Recibos = new List<Recibo>();

            try
            {
                if (!string.IsNullOrWhiteSpace(search) && search != null)
                {
                    Recibos = new ReciboBL().Buscar(search, CustomHelper.getUserId()).ToList();
                }
                else
                {
                    Recibos = new ReciboBL().ObtenerListadoPorFecha(DateTime.Today, DateTime.Today, CustomHelper.getUserId(), false).ToList();
                }
            }
            catch (Exception)
            {}

            ViewBag.Search = search;

            int pageSize = 15;
            int pageNumber = (page ?? 1);
            return View(Recibos.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Recibo.Ver_Listado_Sin_Despachar")]
        public ActionResult Sin_Despachar(int? page)
        {
            CustomHelper.setTitle("Recibo x Despachar", "Listado");
            List<Recibo> Recibos = new List<Recibo>();

            try
            {
                Recibos = new ReciboBL().ObtenerListadoSinDespachar(CustomHelper.getAgenciaId()).ToList();
            }
            catch (Exception)
            {
            }
            
            int pageSize = 15;
            int pageNumber = (page ?? 1);
            return View(Recibos.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Recibo.Cocina")]
        public ActionResult Cocina(int? page)
        {
            CustomHelper.setTitle("Cocina", "Listado");
            List<Recibo> Recibos = new List<Recibo>();
            
            try
            {
                Recibos = new ReciboBL().ObtenerListadoSinDespacharCocina(CustomHelper.getAgenciaId()).ToList();
               
            }
            catch (Exception)
            {
            }

            int pageSize = 15;
            int pageNumber = (page ?? 1);
            return View(Recibos.ToPagedList(pageNumber, pageSize));
        }


        [Permiso("Control.Cocina.Pantalla2")]
        public ActionResult CocinaPantalla2(int? page)
        {
            CustomHelper.setTitle("Cocina", "Listado");
            List<Recibo> Recibos = new List<Recibo>();

            try
            {
                Recibos = new ReciboBL().ObtenerListadoSinDespacharCocina(CustomHelper.getAgenciaId()).ToList();
                foreach (Recibo item in Recibos) {
                    foreach (ReciboDetalle itemdetalle in item.Detalles) {
                        Producto temporalproducto = new ProductoBL().ObtenerPorId(itemdetalle.ProductoId);
                        itemdetalle.Descuento = 1000;
                        Configuracion temp = new ConfiguracionBL().ObtenerPorIdentificador("Pantalla2");
                        string[] separado = temp.Valor.Split(';');
                        CustomHelper.setTitle("Cocina", separado[0]);
                        if (separado.Length >= 2) { 
                        for (int i = 1; i < separado.Length; i++) {

                                if (temporalproducto.CategoriaId == Convert.ToInt64(separado[i])) {
                                    itemdetalle.Descuento = 0;
                                }
                        }
                        }

                    }
                }
            }
            catch (Exception)
            {}

            int pageSize = 15;
            int pageNumber = (page ?? 1);
            return View(Recibos.ToPagedList(pageNumber, pageSize));
        }
        [Permiso("Contro.Cocina.Pantalla3")]
        public ActionResult CocinaPantalla3(int? page)
        {
            CustomHelper.setTitle("Cocina", "Listado");
            List<Recibo> Recibos = new List<Recibo>();

            try
            {
                Recibos = new ReciboBL().ObtenerListadoSinDespacharCocina(CustomHelper.getAgenciaId()).ToList();
                foreach (Recibo item in Recibos)
                {
                    foreach (ReciboDetalle itemdetalle in item.Detalles)
                    {
                        Producto temporalproducto = new ProductoBL().ObtenerPorId(itemdetalle.ProductoId);
                        itemdetalle.Descuento = 1000;
                        Configuracion temp = new ConfiguracionBL().ObtenerPorIdentificador("Pantalla3");
                        string[] separado = temp.Valor.Split(';');
                        CustomHelper.setTitle("Cocina", separado[0]);
                        if (separado.Length >= 2)
                        {
                            for (int i = 1; i < separado.Length; i++)
                            {

                                if (temporalproducto.CategoriaId == Convert.ToInt64(separado[i]))
                                {
                                    itemdetalle.Descuento = 0; ;
                                }
                            }
                        }

                    }
                }
            }
            catch (Exception)
            {}

            int pageSize = 15;
            int pageNumber = (page ?? 1);
            return View(Recibos.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Cocina.Pantalla4")]
        public ActionResult CocinaPantalla4(int? page)
        {
            CustomHelper.setTitle("Cocina", "Listado");
            List<Recibo> Recibos = new List<Recibo>();

            try
            {
                Recibos = new ReciboBL().ObtenerListadoSinDespacharCocina(CustomHelper.getAgenciaId()).ToList();
                foreach (Recibo item in Recibos)
                {
                    foreach (ReciboDetalle itemdetalle in item.Detalles)
                    {
                        Producto temporalproducto = new ProductoBL().ObtenerPorId(itemdetalle.ProductoId);
                        itemdetalle.Descuento = 1000;
                        Configuracion temp = new ConfiguracionBL().ObtenerPorIdentificador("Pantalla4");
                        string[] separado = temp.Valor.Split(';');
                        CustomHelper.setTitle("Cocina", separado[0]);
                        if (separado.Length >= 2)
                        {
                            for (int i = 1; i < separado.Length; i++)
                            {

                                if (temporalproducto.CategoriaId == Convert.ToInt64(separado[i]))
                                {
                                    itemdetalle.Descuento = 0;
                                }
                            }
                        }

                    }
                }
            }
            catch (Exception)
            {}

            int pageSize = 15;
            int pageNumber = (page ?? 1);
            return View(Recibos.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Envase_x_Recibir.Ver_Listado")]
        public ActionResult Envase_x_Recibir(int? page, string search)
        {
            CustomHelper.setTitle("Envases x Recibir", "Listado");
            List<ReciboEnvase> Recibos = new List<ReciboEnvase>();

            try
            {
                if (!string.IsNullOrWhiteSpace(search) && search != null)
                {
                    Recibos = new ReciboBL().BuscarEnvasexRecibir(search, CustomHelper.getAgenciaId()).ToList();
                }
                else
                {
                    Recibos = new ReciboBL().ObtenerListadoEnvasexRecibir(CustomHelper.getAgenciaId()).ToList();
                }
            }
            catch (Exception)
            {}

            ViewBag.Search = search;

            int pageSize = 15;
            int pageNumber = (page ?? 1);
            return View(Recibos.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Recibo.Ver_Listado_Cuenta")]
        public ActionResult Cuenta(int? page)
        {
            CustomHelper.setTitle("Cuenta en Restaurante", "Listado");
            List<Recibo> Recibos = new List<Recibo>();

            try
            {
                Recibos = new ReciboBL().ObtenerPendientesCancelarDelivery(CustomHelper.getAgenciaId()).ToList();
            }
            catch (Exception)
            {}

            int pageSize = 15;
            int pageNumber = (page ?? 1);
            return View(Recibos.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Recibo.Ver_Listado_Supervisor")]
        public ActionResult Supervisor(int? page, string recibo, DateTime? FechaInicial, DateTime? FechaFinal)
        {
            CustomHelper.setTitle("Recibo - Supervisor", "Listado");
            List<Recibo> Recibos = new List<Recibo>();

            try
            {
                if (!FechaInicial.HasValue && !FechaFinal.HasValue)
                {
                    FechaInicial = DateTime.Today;
                    FechaFinal = DateTime.Today;
                }

                if (!string.IsNullOrWhiteSpace(recibo) && recibo != null)
                {
                    Recibos = new ReciboBL().BuscarRecibo(recibo, CustomHelper.getUserId(), true);
                }
                else
                {
                    Recibos = new ReciboBL().ObtenerListadoPorFecha(FechaInicial.Value, FechaFinal.Value, CustomHelper.getUserId(), true).ToList();
                }
            }
            catch (Exception)
            {}

            ViewBag.fechaInicial = FechaInicial.Value.ToString("yyyy-MM-dd");
            ViewBag.fechaFinal = FechaFinal.Value.ToString("yyyy-MM-dd");
            ViewBag.recibo = recibo;

            int pageSize = 15;
            int pageNumber = (page ?? 1);
            return View(Recibos.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Recibo.Seguimiento_Pago_Ver_Listado")]
        public ActionResult Seguimiento_Pago(int? page, DateTime? FechaInicial, DateTime? FechaFinal)
        {
            CustomHelper.setTitle("Recibo", "Seguimiento de Pagos");
            List<ReciboFechaPagoEstimadaModel> Recibos = new List<ReciboFechaPagoEstimadaModel>();

            try
            {
                if (!FechaInicial.HasValue && !FechaFinal.HasValue)
                {
                    FechaInicial = DateTime.Today;
                    FechaFinal = DateTime.Today;
                }

                Recibos = new ReciboBL().ObtenerReciboNoPagadoxFechaEstimada(FechaInicial.Value, FechaFinal.Value, CustomHelper.getAgenciaId()).ToList();
            }
            catch (Exception)
            { }

            ViewBag.fechaInicial = FechaInicial.Value.ToString("yyyy-MM-dd");
            ViewBag.fechaFinal = FechaFinal.Value.ToString("yyyy-MM-dd");            

            int pageSize = 15;
            int pageNumber = (page ?? 1);
            return View(Recibos.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Recibo.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Recibo", "Nuevo");

            string strAtributo = "checked='checked'";
        
            ViewBag.PagadaSi = "";
            ViewBag.PagadaNo = strAtributo;
            
            ViewBag.ClienteIds = 0;

            this.CargaControles();
            return View();
        }

        [Permiso("Control.Recibo.Crear")]
        public ActionResult Crear_Tienda()
        {
            CustomHelper.setTitle("Recibo - Tienda", "Nuevo");          

            ViewBag.ClienteIds = 0;
            
            return View();
        }

        [Permiso("Control.Recibo.Crear")]
        [HttpPost]
        public ActionResult Crear(Recibo modelo, bool pagada, string QuienEnvia,string Dedicatoria,string DireccionEntregaHoy, string[] productoIds, string[] nombreProductoIds, long[] presentacionIds, string[] nombrePresentacionIds, decimal[] existenciaIds, decimal[] cantidadIds, decimal[] precioIds, long[] formaIds, decimal[] pagarIds, string[] notaIds, decimal[] descuentoIds, string[] idIds)
        {
            if (productoIds == null || productoIds.Length == 0)
            {
                ModelState.AddModelError("", "Para realizar una venta debe de asignar productos");
            }

            modelo.Pagos = new List<ReciboFormaPago>();

            if (pagada)
            {
                if (formaIds != null && formaIds.Length > 0)
                {
                    for (int i = 0; i < formaIds.Length; i++)
                    {
                        ReciboFormaPago Forma = new ReciboFormaPago();
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
                    ModelState.AddModelError("", "Se le informa que el recibo que ingreso no contiene ningún registro de pago, lo cual no es valido");
                }            
            }
                                                   
            modelo.Empleado = false;
            modelo.Credito = false;
            modelo.DiaCredito = 0;
           // modelo.AgenciaId = CustomHelper.getAgenciaId();
            modelo.UsrCreo = CustomHelper.getUserId();          
            modelo.Reparto = modelo.RepartoId == 1 ? true : false;
            modelo.Pagada = pagada;           
            modelo.EntregadoTransporte = false;
            if (DireccionEntregaHoy != "" && DireccionEntregaHoy != null)
            {
                modelo.DireccionClienteId = Convert.ToInt32(DireccionEntregaHoy);
            }
            else {
                modelo.DireccionClienteId = 0;
            }
            
            if (modelo.FechaHoraEntregaProgramada > DateTime.Now)
            {
                modelo.Programada = false;
                modelo.ComentarioPedido = "ENTREGA ESPECIAL PROGRAMADA ------ De:" + QuienEnvia + "------ Dedicatoria:" + Dedicatoria + "------ Hora Entrega: "+modelo.FechaHoraEntregaProgramada+" ------" + modelo.ComentarioPedido;

            }
            else
            {
                modelo.Programada = true;
            }

            modelo.Detalles = new List<ReciboDetalle>();
            for (int i = 0; i < productoIds.Length; i++)
            {
                ReciboDetalle Detalle = new ReciboDetalle();
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

            if (modelo.Detalles != null && modelo.Detalles.Count() > 0)
            {
                bool ExistenciaNoValida = modelo.Detalles.Where(x => x.Cantidad > x.Existencia).Count() > 0;
                if (ExistenciaNoValida)
                {
                  //  ModelState.AddModelError("", "Hay producto(s) que sobre pasan las existencias");
                }
            }

            if (pagada)
            {
                if (modelo.Detalles != null && modelo.Detalles.Count() > 0 && modelo.Pagos != null && modelo.Pagos.Count() > 0)
                {
                    decimal TotalRecibo = decimal.Round(modelo.Detalles.Sum(x => x.Cantidad * x.Precio), 4);
                    decimal TotalPago = modelo.Pagos.Sum(x => x.Valor);

                    if (TotalRecibo != TotalPago)
                    {
                        ModelState.AddModelError("", string.Format("El monto del recibo es de: {0:C4} y el monto de pago es de: {1:C4}",TotalRecibo, TotalPago));
                    }
                }
            }

            if (ModelState.IsValid)
            {
                string strMensaje = new ReciboBL().Guardar(modelo);
                if (strMensaje.Equals("OK"))
                {

                    using (var db = new GiveContext())
                    {
                        var agencia = db.Agencias.FirstOrDefault(a => a.AgenciaId == modelo.AgenciaId);
                     
                        if (agencia != null)
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
                                    DocumentoNumero = "RECIBO" + "-" + modelo.ReciboId,
                                    AgenciaId = modelo.AgenciaId,
                                    AgenciaNombre = agencia.Nombre,
                                    TipoRegistro = "RECIBO",
                                    SalidaCantidadTienda = p.Cantidad,
                                    SalidaCostoTienda = p.Precio,
                                    ExistenciaFinalTienda = existenciaActual
                                });
                            }

                            db.SaveChanges();
                        }
                    }

                    TempData["Recibo-Success"] = strMensaje;
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

        [Permiso("Control.Recibo.Crear")]
        [HttpPost]
        public ActionResult Crear_Tienda(Recibo modelo, string[] productoIds, string[] nombreProductoIds, long[] presentacionIds, string[] nombrePresentacionIds, decimal[] existenciaIds, decimal[] cantidadIds, decimal[] precioIds, decimal[] descuentoIds, string[] idIds)
        {
            if (productoIds == null || productoIds.Length == 0)
            {
                ModelState.AddModelError("", "Para realizar una venta debe de asignar productos");
            }

            modelo.VendedorId = 20180925001;
            modelo.UsrCreo = CustomHelper.getUserId();

            modelo.Empleado = false;
            modelo.Credito = false;
            modelo.DiaCredito = 0;          
           
            modelo.Reparto = true;
            modelo.Programada = true;
            modelo.Despachado = true;
            modelo.Pagada = true;
            modelo.EntregadoTransporte = false;

            modelo.UsrCocina = 20200506001;
            modelo.UsrDespacho = 20200506001;

            modelo.Detalles = new List<ReciboDetalle>();

            for (int i = 0; i < productoIds.Length; i++)
            {
                ReciboDetalle Detalle = new ReciboDetalle();
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

            if (modelo.Detalles != null && modelo.Detalles.Count() > 0)
            {
                decimal TotalRecibo = decimal.Round(modelo.Detalles.Sum(x => x.Cantidad * x.Precio), 4);
                decimal TotalPago = modelo.Efectivo + modelo.Tarjeta;              

                if (TotalRecibo != TotalPago)
                {
                    ModelState.AddModelError("", string.Format("El monto del recibo es de: {0:C4} y el monto de pago es de: {1:C4}", TotalRecibo, TotalPago));
                }
            }

            if (ModelState.IsValid)
            {
                modelo.Pagos = new List<ReciboFormaPago>();              

                if (modelo.Efectivo > 0 && modelo.Tarjeta == 0)
                {
                    ReciboFormaPago Forma = new ReciboFormaPago();
                    Forma.FormaPagoId = 20171028001;
                    Forma.Valor = modelo.Efectivo;
                    Forma.Nota = "";

                    modelo.Pagos.Add(Forma);
                }
                else if (modelo.Tarjeta > 0 && modelo.Efectivo == 0)
                {
                    ReciboFormaPago Forma = new ReciboFormaPago();
                    Forma.FormaPagoId = 20181021001;
                    Forma.Valor = modelo.Tarjeta;
                    Forma.Nota = "";

                    modelo.Pagos.Add(Forma);
                }
                else if (modelo.Efectivo > 0 && modelo.Tarjeta > 0)
                {
                    //Se agrega el efectivo 
                    ReciboFormaPago Forma = new ReciboFormaPago();
                    Forma.FormaPagoId = 20171028001;
                    Forma.Valor = modelo.Efectivo;
                    Forma.Nota = "";

                    modelo.Pagos.Add(Forma);

                    //Se agrega la tarjeta
                    Forma = new ReciboFormaPago();
                    Forma.FormaPagoId = 20181021001;
                    Forma.Valor = modelo.Tarjeta;
                    Forma.Nota = "";

                    modelo.Pagos.Add(Forma);
                }


                string strMensaje = new ReciboBL().Guardar(modelo, true);
                if (strMensaje.Equals("OK"))
                {

                    using (var db = new GiveContext())
                    {
                        var agencia = db.Agencias.FirstOrDefault(a => a.AgenciaId == modelo.AgenciaId);

                        if (agencia != null)
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
                                    DocumentoNumero = "RECIBO" + "-" + modelo.ReciboId,
                                    AgenciaId = modelo.AgenciaId,
                                    AgenciaNombre = agencia.Nombre,
                                    TipoRegistro = "RECIBO",
                                    SalidaCantidadTienda = p.Cantidad,
                                    SalidaCostoTienda = p.Precio,
                                    ExistenciaFinalTienda = existenciaActual
                                });
                            }

                            db.SaveChanges();
                        }
                    }

                    TempData["Recibo-Success"] = strMensaje;
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

            ViewBag.ClienteIds = modelo.ClienteId;

            this.CargaControles();
            return View(modelo);
        }

        [Permiso("Control.Recibo.Crear")]
        public ActionResult Editar_Cliente(long id)
        {
            Recibo ReciboActual = new ReciboBL().ObtenerPorId(id, true, true);

            if (ReciboActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Recibo", "Editar Cliente");

            ViewBag.ClienteIds = ReciboActual.ClienteId;

            return View(ReciboActual);
        }

        [Permiso("Control.Recibo.Crear")]
        [HttpPost]
        public ActionResult Editar_Cliente(Recibo modelo)
        {
            string strMensaje = new ReciboBL().GuardarCliente(modelo);

            if (strMensaje.Equals("OK"))
            {
                TempData["Recibo-Success"] = strMensaje;
                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("", strMensaje);
            }

            Recibo ReciboActual = new ReciboBL().ObtenerPorId(modelo.ReciboId, true, true);

            ViewBag.ClienteIds = ReciboActual.ClienteId;

            return View(ReciboActual);
        }

        [Permiso("Control.Recibo.Crear")]
        public ActionResult Generar_Factura(long id)
        {
            Recibo ReciboActual = new ReciboBL().ObtenerPorId(id, true, true);

            if (ReciboActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Recibo", "Generar Factura");

            return View(ReciboActual);
        }

        [Permiso("Control.Recibo.Crear")]
        [HttpPost]
        public ActionResult Generar_Factura(Recibo modelo)
        {
            string strMensaje = new ReciboBL().GenerarFactura(modelo.ReciboId, CustomHelper.getUserId(), false);
            if (strMensaje.Equals("OK"))
            {
                TempData["Recibo-Success"] = strMensaje;
                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("", strMensaje);
            }           

            return View(new ReciboBL().ObtenerPorId(modelo.ReciboId, true, true));
        }

        [Permiso("Control.Recibo.Crear_Factura_Cambiaria")]
        public ActionResult Generar_Factura_CAM(long id)
        {
            Recibo ReciboActual = new ReciboBL().ObtenerPorId(id, true, true);

            if (ReciboActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Recibo", "Generar Factura Cambiaria");

            return View(ReciboActual);
        }

        [Permiso("Control.Recibo.Crear_Factura_Cambiaria")]
        [HttpPost]
        public ActionResult Generar_Factura_CAM(Recibo modelo)
        {
            string strMensaje = new ReciboBL().GenerarFactura(modelo.ReciboId, CustomHelper.getUserId(), true);
            if (strMensaje.Equals("OK"))
            {
                TempData["Recibo-Success"] = strMensaje;
                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("", strMensaje);
            }

            return View(new ReciboBL().ObtenerPorId(modelo.ReciboId, true, true));
        }

        [Permiso("Control.Recibo.Anular")]
        public ActionResult Anular(long id)
        {
            Recibo ReciboActual = new ReciboBL().ObtenerPorId(id, true, true);

            if (ReciboActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Recibo", "Anular");

            return View(ReciboActual);
        }

        [Permiso("Control.Recibo.Anular")]
        [HttpPost]
        public ActionResult Anular(long reciboId, string comentario)
        {
            string strMensaje = new ReciboBL().Anular(reciboId, comentario, CustomHelper.getUserId());
            if (strMensaje.Equals("OK"))
            {
                TempData["Recibo_Anular-Success"] = strMensaje;
                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("", strMensaje);
            }

            Recibo ReciboActual = new ReciboBL().ObtenerPorId(reciboId, true, true);

            if (ReciboActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Recibo", "Anular");

            return View(ReciboActual);
        }

        [Permiso("Control.Recibo.Detalle")]
        public ActionResult Detalle(long id)
        {
            Recibo ReciboActual = new ReciboBL().ObtenerPorId(id, true, true);

            if (ReciboActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Recibo", "Detalle");

            ViewBag.EliminarPago = CustomHelper.Permiso("Control.Recibo_Eliminar.Forma_Pago");

            return View(ReciboActual);
        }

        [Permiso("Control.Recibo.Detalle_Envase")]
        public ActionResult Detalle_Envase(long id)
        {
            ReciboEnvase ReciboEnvaseActual = new ReciboBL().ObtenerEnvasePorId(id);

            if (ReciboEnvaseActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Envases x Recibir", "Detalle");

            return View(ReciboEnvaseActual);
        }

        [Permiso("Control.Recibo.Asignar_Transporte")]
        public ActionResult Asignar_Transporte(long id)
        {
            Recibo ReciboActual = new ReciboBL().ObtenerPorId(id, true, true);

            if (ReciboActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Recibo", "Asignar Transporte");

            this.CargaTransportes();
            return View(ReciboActual);
        }

        [Permiso("Control.Recibo.FinalizarCocina")]
        public ActionResult FinalizarCocina(long id)
        {
            Recibo ReciboActual = new ReciboBL().ObtenerPorId(id, true, true);

            if (ReciboActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Recibo", "Asignar Transporte");

        
            return View(ReciboActual);
        }
        [Permiso("Control.Recibo.FinalizarCocina")]
        [HttpPost]
        public ActionResult FinalizarCocina(long reciboId, long sobrecarga)
        {
            string strMensaje = new ReciboBL().FinalizarCocina(reciboId, CustomHelper.getUserId());
            if (strMensaje.Equals("OK"))
            {
                TempData["Recibo_Asignar_Transporte-Success"] = strMensaje;
                return RedirectToAction("Cocina");
            }
            else
            {
                ModelState.AddModelError("", strMensaje);
            }

            Recibo ReciboActual = new ReciboBL().ObtenerPorId(reciboId, true, true);

            if (ReciboActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Recibo", "Asignar Transporte");

            this.CargaTransportes();
            return View(ReciboActual);
        }
        [Permiso("Control.Recibo.Asignar_Transporte")]
        [HttpPost]
        public ActionResult Asignar_Transporte(long reciboId, long transporteId)
        {
            string strMensaje = new ReciboBL().AsignarTransporte(reciboId, transporteId);
            if (strMensaje.Equals("OK"))
            {
                TempData["Recibo_Asignar_Transporte-Success"] = strMensaje;
                return RedirectToAction("Sin_Despachar");
            }
            else
            {
                ModelState.AddModelError("", strMensaje);
            }

            Recibo ReciboActual = new ReciboBL().ObtenerPorId(reciboId, true, true);

            if (ReciboActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Recibo", "Asignar Transporte");

            this.CargaTransportes();
            return View(ReciboActual);
        }

        [Permiso("Control.Recibo.Asignar_Lote")]
        public ActionResult Asignar_Lote(long id)
        {
            Recibo ReciboActual = new ReciboBL().ObtenerPorId(id, true, true);

            if (ReciboActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Recibo", "Asignar Lote");

            return View(ReciboActual);
        }

        [Permiso("Control.Recibo.Asignar_Lote")]
        [HttpPost]
        public ActionResult Asignar_Lote(Recibo modelo, string[] productoIds, string[] nombreProductoIds, string[] loteIds, decimal[] existenciaIds, decimal[] cantidadIds)
        {
            if (productoIds == null || productoIds.Length == 0)
            {
                ModelState.AddModelError("", "Para realizar una asignacion de lote debe de asignar productos");
            }
           
            modelo.Lotes = new List<ReciboLote>();
            for (int i = 0; i < productoIds.Length; i++)
            {
                ReciboLote Detalle = new ReciboLote();
                Detalle.ProductoId = productoIds[i];
                Detalle.Lote = loteIds[i];   
                Detalle.Cantidad = cantidadIds[i];

                modelo.Lotes.Add(Detalle);
            }
          
            string strMensaje = new ReciboBL().GuardarLote(modelo);
            if (strMensaje.Equals("OK"))
            {
                TempData["Recibo_Lote-Success"] = strMensaje;
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

            return View(new ReciboBL().ObtenerPorId(modelo.ReciboId, true, true));
        }

        public ActionResult GetEscalaPreciosxProducto(string id)
        {
            return PartialView("_ProductoNivelPrecio", new ProductoBL().ObtenerEscalaPreciosxProducto(id));
        }

        [Permiso("Control.Reporte.Boleta_Recibo")]
        public ActionResult Boleta(long Id)
        {
            Recibo ReciboActual = new ReciboBL().ObtenerPorId(Id, true, true, true);

            if (ReciboActual != null)
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
                Encabezado.Columns.Add(new DataColumn("Vendedor", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Comentario", typeof(string)));
                
                DireccionCliente dir = new DireccionCliente();
                dir.Direccion = "sin asignar";
                if (ReciboActual.DireccionClienteId != 0)
                {
                    dir = new ClienteBL().ObtenerDireccionPorId(Convert.ToInt32(ReciboActual.DireccionClienteId));
                }
                

                Encabezado.Rows.Add(ReciboActual.ReciboId, ReciboActual.Cliente.NoTelefono, ReciboActual.Cliente.Nombre, dir.Direccion, ReciboActual.ComentarioPedido,Convert.ToDateTime( ReciboActual.FechaHoraRecibo).ToString("dd/MM/yyyy hh:mm:ss"), ReciboActual.DescuentoTotal, ReciboActual.Total, ReciboActual.Vendedor.Nombre, ReciboActual.ComentarioPedido);

                Detalle.Columns.Add(new DataColumn("MovimientoId", typeof(long)));
                Detalle.Columns.Add(new DataColumn("ProductoId", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Nombre", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Presentacion", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Cantidad", typeof(decimal)));
                Detalle.Columns.Add(new DataColumn("Precio", typeof(decimal)));

                if (ReciboActual.Detalles != null && ReciboActual.Detalles.Count() > 0)
                {
                    foreach (var DetalleActual in ReciboActual.Detalles)
                    {
                        if (!string.IsNullOrWhiteSpace(DetalleActual.ID))
                        {
                            Detalle.Rows.Add(ReciboActual.ReciboId, DetalleActual.ProductoId, string.Format("{0} - {1}(IDs: {2})", DetalleActual.Producto.Codigo, string.IsNullOrWhiteSpace(DetalleActual.Nombre) ? DetalleActual.Producto.Nombre : DetalleActual.Nombre, DetalleActual.ID), DetalleActual.Unidad.Nombre, DetalleActual.Cantidad, DetalleActual.Precio);
                        }
                        else
                        {
                            Detalle.Rows.Add(ReciboActual.ReciboId, DetalleActual.ProductoId, string.Format("{0}  -  {1}"," ", string.IsNullOrWhiteSpace(DetalleActual.Nombre) ? DetalleActual.Producto.Nombre : DetalleActual.Nombre), DetalleActual.Unidad.Nombre, DetalleActual.Cantidad, DetalleActual.Precio);
                        }
                    }
                }

                Control.Columns.Add(new DataColumn("MovimientoId", typeof(long)));
                Control.Columns.Add(new DataColumn("Factura", typeof(string)));
                Control.Columns.Add(new DataColumn("FormaPago", typeof(string)));
                Control.Columns.Add(new DataColumn("Nota", typeof(string)));

                if (ReciboActual.Pagos != null && ReciboActual.Pagos.Count() > 0)
                {
                    foreach (var Pago in ReciboActual.Pagos)
                    {
                        string strPago = string.Format("{0} - {1:C4},", Pago.FormaPago.Nombre, Pago.Valor);
                        Control.Rows.Add(ReciboActual.ReciboId, ReciboActual.Documento, strPago, Pago.Nota);
                    }
                }                

                Movimiento.Tables.Add(Encabezado);
                Movimiento.Tables.Add(Detalle);
                Movimiento.Tables.Add(Control);

                // Se define la ruta del reporte
                var reportPath = Server.MapPath("~/Reports/ReportMovRecibo.rdlc");

                // se obtienen los bytes del reporte en pdf
                var bytes = GetReportBytes(reportPath, Movimiento, 8.5m, 11.0m, 0m, 0m);
           
                return File(bytes, "application/pdf");
            }

            return View();
        }

        [Permiso("Control.Recibo.Asignar_Lote")]
        public ActionResult Boleta_Lote(long Id)
        {
            Recibo ReciboActual = new ReciboBL().ObtenerPorId(Id, true, true, true);

            if (ReciboActual != null)
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
                Encabezado.Columns.Add(new DataColumn("Vendedor", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Comentario", typeof(string)));

                Encabezado.Rows.Add(ReciboActual.ReciboId, ReciboActual.Agencia.Nombre, ReciboActual.Cliente.Nombre, ReciboActual.Cliente.Direccion, ReciboActual.ComentarioPedido, ReciboActual.Fecha.ToString("dd/MM/yyyy"), ReciboActual.DescuentoTotal, ReciboActual.Total, ReciboActual.Vendedor.Nombre, ReciboActual.ComentarioPedido);

                Detalle.Columns.Add(new DataColumn("MovimientoId", typeof(long)));
                Detalle.Columns.Add(new DataColumn("ProductoId", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Nombre", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Presentacion", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Cantidad", typeof(decimal)));
                Detalle.Columns.Add(new DataColumn("Precio", typeof(decimal)));

                if (ReciboActual.Lotes != null && ReciboActual.Lotes.Count() > 0)
                {
                    foreach (var DetalleActual in ReciboActual.Lotes)
                    {                      
                        Detalle.Rows.Add(ReciboActual.ReciboId, DetalleActual.ProductoId, string.Format("{0} - {1}", DetalleActual.Producto.Codigo, DetalleActual.Producto.Nombre), string.Format("{0} - F. Vencimiento: {1}", DetalleActual.Lote, DetalleActual.FechaVencimiento.ToString("dd/MM/yyyy")), DetalleActual.Cantidad, 0);
                    }
                }

                Control.Columns.Add(new DataColumn("MovimientoId", typeof(long)));
                Control.Columns.Add(new DataColumn("Factura", typeof(string)));
                Control.Columns.Add(new DataColumn("FormaPago", typeof(string)));
                Control.Columns.Add(new DataColumn("Nota", typeof(string)));

                if (ReciboActual.Pagos != null && ReciboActual.Pagos.Count() > 0)
                {
                    foreach (var Pago in ReciboActual.Pagos)
                    {
                        string strPago = string.Format("{0} - {1:C4},", Pago.FormaPago.Nombre, Pago.Valor);
                        Control.Rows.Add(ReciboActual.ReciboId, ReciboActual.Documento, strPago, Pago.Nota);
                    }
                }

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

        [ActionName("ActualizarReciboDespachado")]
        public JsonResult ActualizarReciboDespachado(long reciboId)
        {
            if (reciboId > 0)
            {
                string Mensaje = new ReciboBL().Despachar(reciboId, CustomHelper.getUserId());
                if (Mensaje.Equals("OK"))
                {
                    return Json(new { Operacion = true }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ActualizarReciboPagado")]
        public JsonResult ActualizarReciboPagado(long reciboId)
        {
            if (reciboId > 0)
            {
                string Mensaje = new ReciboBL().Pagar(reciboId);
                if (Mensaje.Equals("OK"))
                {
                    return Json(new { Operacion = true }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerReciboGarantiaActual")]
        public JsonResult ObtenerReciboGarantiaActual(long recibo)
        {
            if (recibo > 0)
            {
                FacturaGarantia ReciboActual = new ReciboBL().ObtenerProductosRecibo(recibo);
                if (ReciboActual != null)
                {
                    return Json(new { Operacion = true, Data = ReciboActual }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ActualizarReciboEnvase")]
        public JsonResult ActualizarReciboEnvase(long reciboId)
        {
            if (reciboId > 0)
            {
                string Mensaje = new ReciboBL().Envases(reciboId, CustomHelper.getUserId());
                if (Mensaje.Equals("OK"))
                {
                    return Json(new { Operacion = true }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [ActionName("LiquidarDelivery")]
        public JsonResult LiquidarDelivery(long reciboId)
        {
            if (reciboId > 0)
            {
                string Mensaje = new ReciboBL().LiquidarDelivery(reciboId);
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
                string Mensaje = new ReciboBL().EnviarCorreo(id);
                if (Mensaje.Equals("OK"))
                {
                    return Json(new { Operacion = true }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ActionName("GuardarFechaPagoEstimada")]
        public JsonResult GuardarFechaPagoEstimada(Recibo modelo)
        {
            string Mensaje = new ReciboBL().GuardarFechaPagoEstimada(modelo);
            if (Mensaje.Equals("OK"))
            {
                return Json(new { Operacion = true }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ActionName("Eliminar_Pago")]
        public JsonResult Eliminar_Pago(long reciboId, int id)
        {
            if (reciboId > 0 && id > 0)
            {
                string Mensaje = new ReciboBL().EliminarPago(reciboId, id);
                if (Mensaje.Equals("OK"))
                {
                    return Json(new { Operacion = true }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }
    }
}