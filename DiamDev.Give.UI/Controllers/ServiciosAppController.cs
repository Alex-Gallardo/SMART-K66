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
    public class ServiciosAppController : Controller
    {
        // GET: ServiciosApp
        public ActionResult Index()
        {
            return View();
        }

        //public JsonResult CrearCliente(string nombre, int celular, string direccion, string altitud, string latitud) {

        //}

        public JsonResult ObtenerClientePorNumero(string numero)

        {
            
            Cliente cli = new ClienteBL().ObtenerClientePorNumero(numero,false);
            if (cli == null) {
                cli = new Cliente();
                cli.ClienteId = -1;
            }

            return Json(cli, JsonRequestBehavior.AllowGet);
        }


        public JsonResult ObtenerDireccionesPorNumero(string numero)

        {

            Cliente cli = new ClienteBL().ObtenerClientePorNumero(numero, false);
            List<DireccionCliente> direcciones = new ClienteBL().ObtenerDireccionesClientePorId(cli.ClienteId);


            return Json(direcciones, JsonRequestBehavior.AllowGet);
        }

        public JsonResult EliminarDireccionPorId(int direccionid)
        {
            RespuestaApp nuev = new RespuestaApp();
            try
            {
                string mensaje = new ClienteBL().EliminarDireccionPorId(direccionid);
                nuev.Id = 1;
                nuev.Respuesta0 = mensaje;
            }
            catch (Exception ) 
            {
                nuev.Id = -1;
                nuev.Respuesta0 = "Existe Error al eliminar Direccion";
            }

            return Json(nuev, JsonRequestBehavior.AllowGet);
        }

        public JsonResult ObtenerCategoriasCat()
        {
            List<ProductoCategoria> listado = new List<ProductoCategoria>();
         
                // It's on or after 11 AM!
                listado = new ProductoCategoriaBL().ObtenerListadoBasadoExistencias(-1);
           



            return Json(listado, JsonRequestBehavior.AllowGet);
        }

        public JsonResult ObtenerCategorias(int direccionid) {
            List<ProductoCategoria> listado = new List<ProductoCategoria>();
            Configuracion ini = new ConfiguracionBL().ObtenerPorIdentificador("HoraInicio");
            Configuracion fin = new ConfiguracionBL().ObtenerPorIdentificador("HoraFin");
            Configuracion MensajeHorario = new ConfiguracionBL().ObtenerPorIdentificador("MensajeHorario");

            if (System.DateTime.Now.Hour >= Convert.ToInt32(ini.Valor) && System.DateTime.Now.Hour < Convert.ToInt32(fin.Valor))
            {


                // It's on or after 11 AM!
                listado = new ProductoCategoriaBL().ObtenerListadoBasadoExistencias(direccionid);
            }
            else {
                ProductoCategoria cat = new ProductoCategoria();
                cat.Nombre = "Horario No Apto";
                cat.ProductoCategoriaId = 0;
                cat.FotografiaApp = " ";

                listado.Add(cat);

            }

            

            return Json(listado, JsonRequestBehavior.AllowGet);
        }
        public FileResult Preview(int id, string documentoId)
        {
            ProductoFotografia FotografiaActual = new ProductoBL().Fotografia(id, documentoId);

            var content = Binario.Drawing.ImageManager.GetThumbnail(FotografiaActual.Content, 100);
            return File(content, FotografiaActual.ContentType);
        }

        public FileResult Imagen(int id, string documentoId)
        {
            ProductoFotografia FotografiaActual = new ProductoBL().Fotografia(id, documentoId);

            return File(FotografiaActual.Content, FotografiaActual.ContentType);
        }

        public JsonResult PuedoHacerPedido()
        {
            RespuestaApp ob = new RespuestaApp();
            Configuracion ini = new ConfiguracionBL().ObtenerPorIdentificador("HoraInicio");
            Configuracion fin = new ConfiguracionBL().ObtenerPorIdentificador("HoraFin");
            Configuracion MensajeHorario = new ConfiguracionBL().ObtenerPorIdentificador("MensajeHorario");
            if (System.DateTime.Now.Hour >= Convert.ToInt32(ini.Valor) && System.DateTime.Now.Hour < Convert.ToInt32(fin.Valor))
            {
                // It's on or after 11 AM!
                ob.Id = 1;
                ob.Respuesta0="";
            }
            else {
                ob.Id = 0;
                ob.Respuesta0 = MensajeHorario.Valor;
            }



            return Json(ob, JsonRequestBehavior.AllowGet);
        }
        public JsonResult MontoMinimo()
        {
            RespuestaApp ob = new RespuestaApp();
            Configuracion monto = new ConfiguracionBL().ObtenerPorIdentificador("MinimoPedido");
            
            
          
                ob.Id = 1;
                ob.Respuesta0 = monto.Valor;
          


            return Json(ob, JsonRequestBehavior.AllowGet);
        }

        public JsonResult ObtenerProductosPorCategoriaId(long categoriaid,int localidadid)
        {
            Configuracion con = new ConfiguracionBL().ObtenerPorIdentificador("AgenciaPedidos");
            long agenciapedidos = 0;

            if (con.Valor == "-1")
            {
                if (localidadid == -1)
                {
                    Configuracion con2 = new ConfiguracionBL().ObtenerPorIdentificador("AgenciaCentral");
                    agenciapedidos = Convert.ToInt64(con2.Valor);
                }
                else { 
                long localidad = Convert.ToInt64(new ClienteBL().ObtenerDireccionPorId(localidadid).LocalidadId);
                agenciapedidos = Convert.ToInt64(new LocalidadBL().ObtenerPorId(localidad).AgenciaId);
                }
            }
            else
            {
                Configuracion con2 = new ConfiguracionBL().ObtenerPorIdentificador("AgenciaCentral");
                agenciapedidos = Convert.ToInt64(con2.Valor);
            }

            List<Producto> listado = new ProductoBL().ObtenerProductoPorCategoriaIdConPrecioExistenciaAgencia(categoriaid,agenciapedidos);

            return Json(listado, JsonRequestBehavior.AllowGet);
        }
        public JsonResult ObtenerProductosPorCategoriaIdCorto(long categoriaid, int localidadid)
        {
            Configuracion con = new ConfiguracionBL().ObtenerPorIdentificador("AgenciaPedidos");
            long agenciapedidos = 0;

            if (con.Valor == "-1")
            {
                if (localidadid == -1)
                {
                    Configuracion con2 = new ConfiguracionBL().ObtenerPorIdentificador("AgenciaCentral");
                    agenciapedidos = Convert.ToInt64(con2.Valor);
                }
                else
                {
                    long localidad = Convert.ToInt64(new ClienteBL().ObtenerDireccionPorId(localidadid).LocalidadId);
                    agenciapedidos = Convert.ToInt64(new LocalidadBL().ObtenerPorId(localidad).AgenciaId);
                }
            }
            else
            {
                Configuracion con2 = new ConfiguracionBL().ObtenerPorIdentificador("AgenciaCentral");
                agenciapedidos = Convert.ToInt64(con2.Valor);
            }

            List<Producto> listado = new ProductoBL().ObtenerProductoPorCategoriaIdConPrecioExistenciaAgencia(categoriaid, agenciapedidos);
            List<ProductoCorto> listadocorto = new List<ProductoCorto>();
            foreach(Producto item in listado)
            {
                ProductoCorto corto = new ProductoCorto();
                corto.Nombre = item.Nombre;
                corto.ProductoId = item.ProductoId;
                corto.FotografiaApp = item.FotografiaApp;


                listadocorto.Add(corto);
            }

            return Json(listadocorto, JsonRequestBehavior.AllowGet);
        }
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


                Encabezado.Rows.Add(ReciboActual.ReciboId, ReciboActual.Cliente.NoTelefono, ReciboActual.Cliente.Nombre, dir.Direccion, ReciboActual.ComentarioPedido, ReciboActual.Fecha.ToString("dd/MM/yyyy"), ReciboActual.DescuentoTotal, ReciboActual.Total, ReciboActual.Vendedor.Nombre, ReciboActual.ComentarioPedido);

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
                            Detalle.Rows.Add(ReciboActual.ReciboId, DetalleActual.ProductoId, string.Format("{0}  -  {1}", " ", string.IsNullOrWhiteSpace(DetalleActual.Nombre) ? DetalleActual.Producto.Nombre : DetalleActual.Nombre), DetalleActual.Unidad.Nombre, DetalleActual.Cantidad, DetalleActual.Precio);
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

        public JsonResult CrearPedidoRecibo(int DireccionEntregaHoy,string comentario,string celular, string productoIdsa, string Especialesa, string cantidadIdsa)
        {
            Recibo modelo = new Recibo();
            Cliente cli = new ClienteBL().ObtenerClientePorNumero(celular, false);
            Configuracion con = new ConfiguracionBL().ObtenerPorIdentificador("AgenciaPedidos");
            long agenciapedidos = 0;
            if (con.Valor == "-1")
            {
                long localidad = Convert.ToInt64(new ClienteBL().ObtenerDireccionPorId(DireccionEntregaHoy).LocalidadId);
                
                agenciapedidos =Convert.ToInt64( new LocalidadBL().ObtenerPorId(localidad).AgenciaId);
            }
            else {
                 agenciapedidos = Convert.ToInt64(con.Valor);
            }
            

            string[] productoIds = productoIdsa.Split(',');
            string[] Especiales = Especialesa.Split(',');
            string[] cantidadIds = cantidadIdsa.Split(',');
            //Cliente cli = new ClienteBL().ObtenerClientePorNumero(celular,false);
            modelo.Empleado = false;
            modelo.Credito = false;
            modelo.ComentarioPedido = comentario;
            modelo.DiaCredito = 0;
            modelo.AgenciaId = agenciapedidos;
            modelo.UsrCreo = 20200506001;
            modelo.Reparto = false;
            modelo.Pagada = false;
            modelo.EntregadoTransporte = false;
            modelo.VendedorId = 20200506001;
            modelo.ClienteId = cli.ClienteId;
            modelo.DireccionClienteId = Convert.ToInt32(DireccionEntregaHoy);
            modelo.TipoId = 1; 
            modelo.Programada = true;
            
            modelo.Detalles = new List<ReciboDetalle>();
            for (int i = 0; i < productoIds.Length; i++)
            {
                ReciboDetalle Detalle = new ReciboDetalle();
                Producto pro = new ProductoBL().ObtenerPorId(productoIds[i]);
                Detalle.ProductoId = productoIds[i];
                Detalle.UnidadId = 20190921001;
                Detalle.Nombre = pro.Nombre + Especiales[i]; 
                Detalle.Existencia = Convert.ToInt32(cantidadIds[i]);
                Detalle.Cantidad = Convert.ToInt32(cantidadIds[i]);

                Detalle.Descuento = 0;
                Detalle.Precio = new ProductoBL().ObtenerPrecioActualPorProductoId(pro.ProductoId, 20190921001).Valor;

                int toca = i + 1;
                    Detalle.ID = "";
               

                modelo.Detalles.Add(Detalle);
            }



            RespuestaApp ret = new RespuestaApp();
            string strMensaje = new ReciboBL().Guardar(modelo);
                if (strMensaje.Equals("OK"))
                {


                ret.Id = 1;
                Recibo dd = new ReciboBL().ObtenerUltimoCliente(cli.ClienteId);
                ret.Respuesta0 = dd.ReciboId.ToString(); 
                }
                else
                {
                ret.Id = 0;
                ret.Respuesta0 = "Problema al crear el pedido. Lo sentimos";
            }
   
            
           
            return Json(ret, JsonRequestBehavior.AllowGet);


        }
        public JsonResult CrearPedidoReciboBot( string comentario, string celular, string productoIdsa, string Especialesa, string cantidadIdsa)
        {
            Recibo modelo = new Recibo();
            Cliente cli = new ClienteBL().ObtenerClientePorNumero(celular, false);
            Configuracion con = new ConfiguracionBL().ObtenerPorIdentificador("AgenciaPedidos");
            long agenciapedidos = 0;
            if (con.Valor == "-1")
            {
                //long localidad = Convert.ToInt64(new ClienteBL().ObtenerDireccionPorId(DireccionEntregaHoy).LocalidadId);

                agenciapedidos = Convert.ToInt64(20180916001);
            }
            else
            {
                agenciapedidos = Convert.ToInt64(con.Valor);
            }


            string[] productoIds = productoIdsa.Split(',');
            string[] Especiales = Especialesa.Split(',');
            string[] cantidadIds = cantidadIdsa.Split(',');
            //Cliente cli = new ClienteBL().ObtenerClientePorNumero(celular,false);
            modelo.Empleado = false;
            modelo.Credito = false;
            modelo.ComentarioPedido = comentario;
            modelo.DiaCredito = 0;
            modelo.AgenciaId = agenciapedidos;
            modelo.UsrCreo = 20200506001;
            modelo.Reparto = false;
            modelo.Pagada = false;
            modelo.EntregadoTransporte = false;
            modelo.VendedorId = 20200506001;
            modelo.ClienteId = cli.ClienteId;
            modelo.DireccionClienteId = Convert.ToInt32(new ClienteBL().ObtenerDireccionesClientePorId(cli.ClienteId).FirstOrDefault().DireccionId);
            modelo.TipoId = 1;
            modelo.Programada = true;

            modelo.Detalles = new List<ReciboDetalle>();
            //for (int i = 0; i < productoIds.Length; i++)
            //{
                ReciboDetalle Detalle = new ReciboDetalle();
                Producto pro = new ProductoBL().ObtenerPorId(productoIdsa);
                Detalle.ProductoId = productoIdsa;
                Detalle.UnidadId = 20190921001;
                Detalle.Nombre = pro.Nombre + Especialesa;
                Detalle.Existencia = Convert.ToInt32(cantidadIdsa);
                Detalle.Cantidad = Convert.ToInt32(cantidadIdsa);

                Detalle.Descuento = 0;
                Detalle.Precio = new ProductoBL().ObtenerPrecioActualPorProductoId(pro.ProductoId, 20190921001).Valor;

              
                Detalle.ID = "";


            ReciboDetalle Detalle2 = new ReciboDetalle();
            
            Detalle2.ProductoId = "20200521054";
            Detalle2.UnidadId = 20190921001;
            Detalle2.Nombre = pro.Nombre;
            Detalle2.Existencia = Convert.ToInt32(1);
            Detalle2.Cantidad = Convert.ToInt32(1);

            Detalle2.Descuento = 0;
            Detalle2.Precio = 10;


            Detalle2.ID = "";
            modelo.Detalles.Add(Detalle);
            modelo.Detalles.Add(Detalle2);
            //}



            RespuestaApp ret = new RespuestaApp();
            string strMensaje = new ReciboBL().Guardar(modelo);
            if (strMensaje.Equals("OK"))
            {


                ret.Id = 1;
                Recibo dd = new ReciboBL().ObtenerUltimoCliente(cli.ClienteId);
                ret.Respuesta0 = dd.ReciboId.ToString();
            }
            else
            {
                ret.Id = 0;
                ret.Respuesta0 = "Problema al crear el pedido. Lo sentimos";
            }



            return Json(ret, JsonRequestBehavior.AllowGet);


        }

        public ActionResult PagarRecibo(string token,long reciboid) {
            RespuestaApp ret = new RespuestaApp();
            ReciboBL conec = new ReciboBL();

            conec.PagarReciboPagadito(reciboid,token);

            Recibo ReciboActual = new ReciboBL().ObtenerPorId(reciboid, true, true, true);

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


                Encabezado.Rows.Add(ReciboActual.ReciboId, ReciboActual.Cliente.NoTelefono, ReciboActual.Cliente.Nombre, dir.Direccion, ReciboActual.ComentarioPedido, ReciboActual.Fecha.ToString("dd/MM/yyyy"), ReciboActual.DescuentoTotal, ReciboActual.Total, ReciboActual.Vendedor.Nombre, ReciboActual.ComentarioPedido);

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
                            Detalle.Rows.Add(ReciboActual.ReciboId, DetalleActual.ProductoId, string.Format("{0}  -  {1}", " ", string.IsNullOrWhiteSpace(DetalleActual.Nombre) ? DetalleActual.Producto.Nombre : DetalleActual.Nombre), DetalleActual.Unidad.Nombre, DetalleActual.Cantidad, DetalleActual.Precio);
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

        public JsonResult ObtenerProductoPorId(string productoid) {
            Producto dev = new ProductoBL().ObtenerPorId(productoid,true);
            ProductoPlano deva = new ProductoPlano();

            deva.Codigo = dev.Codigo;
            deva.Producto = dev.Nombre;
            deva.Descripcion = dev.Descripcion;
            deva.Precio = dev.PrecioActual.ToString();
            deva.Fotografia = dev.FotografiaApp;
            deva.Marca = dev.Marca.Nombre;
            

            
            

            return Json(deva, JsonRequestBehavior.AllowGet);
        }

        public JsonResult ObtenerRecibo(long reciboid) {
            Recibo dd = new ReciboBL().ObtenerPorId(reciboid,false, false, true);
            return Json(dd, JsonRequestBehavior.AllowGet);

        }

        public JsonResult ObtenerTransporte(long transporteid)
        {
            Transporte dd = new TransporteBL().ObtenerPorId(transporteid);
            return Json(dd, JsonRequestBehavior.AllowGet);

        }

        public JsonResult ObtenerMunicipios()

        {
            List<Municipio> municipios = new MunicipioBL().ObtenerListado(false).OrderByDescending(x=>x.Nombre).ToList();

            return Json(municipios, JsonRequestBehavior.AllowGet);
        }
        public JsonResult ObtenerLocalidadPorMunicipio(int municipioid)

        {
            List<Localidad> localidades = new LocalidadBL().ObtenerListadoPorMunicipioId(municipioid).OrderByDescending(x => x.Nombre).ToList();

            return Json(localidades, JsonRequestBehavior.AllowGet);
        }


        [HttpGet]
        public JsonResult CrearDireccion(string celular,string localidad,string direccion) 
        {
            Cliente cli = new ClienteBL().ObtenerClientePorNumero(celular, false);
            DireccionCliente nueva = new DireccionCliente();
            RespuestaApp rep = new RespuestaApp();
            try
            {
                nueva.ClienteId = cli.ClienteId;
                nueva.Direccion = direccion;
                nueva.LocalidadId = Convert.ToInt64(localidad);
                nueva.DireccionId = 0;
                string respuesta = new ClienteBL().GuardarDireccion(nueva);
                rep.Id = 1;

                rep.Respuesta0 = "Se Creó la dirección exitosamente. ";
                return Json(rep, JsonRequestBehavior.AllowGet);
            }
            catch (Exception) 
            {
                rep.Id = 0;
                rep.Respuesta0 = "Imposible Crear la Dirección.";
                return Json(rep, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpGet]
        public JsonResult CrearClienteApp(string celular,string nombre, string localidad,string passwordv,string direccion) {

            Cliente nuevo = new Cliente();
            nuevo.Nombre = nombre;
            nuevo.NoTelefono = celular;
            Localidad loc = new LocalidadBL().ObtenerPorId(Convert.ToInt64(localidad));
            Municipio n = new MunicipioBL().ObtenerPorId(loc.MunicipioId);

            nuevo.Direccion = n.Nombre + " " + loc.Nombre + " " + direccion;
            nuevo.Pass = passwordv;
            nuevo.TipoId = 20200505001;
            nuevo.Vip = false;
            nuevo.LimiteCredito = 10000;
            nuevo.EmailCliente = "app@creadoapp.com";
            nuevo.Nit = "CF";

            long respuesta = new ClienteBL().AgregarApp(nuevo, loc.LocalidadId);
            RespuestaApp rep = new RespuestaApp();
            if (respuesta == -1)
            {
                rep.Id = 0;
                rep.Respuesta0 = "Imposible realizar el registro. Mil disculpas.";
                return Json(rep, JsonRequestBehavior.AllowGet);
            }
            else {
                rep.Id = 1;

                rep.Respuesta0 = "Registro Exitoso. ";
                return Json(rep, JsonRequestBehavior.AllowGet);

            }

        }


        [HttpGet]
        public JsonResult ActualizarClienteApp(string celular, string nombre, string passwordv)
        {
            ClienteBL BB = new ClienteBL();
            Cliente cli =  BB.ObtenerClientePorNumero(celular, false);

            cli.Nombre = nombre;
            cli.Pass = passwordv;
            string respuesta = BB.Guardar(cli);
            RespuestaApp rep = new RespuestaApp();
            if (respuesta !="OK")
            {
                rep.Id = 0;
                rep.Respuesta0 = "Imposible Actualizar Perfil.";
                return Json(rep, JsonRequestBehavior.AllowGet);
            }
            else
            {
                rep.Id = 1;

                rep.Respuesta0 = "Se Actualizó Exitosamente el Perfil ";
                return Json(rep, JsonRequestBehavior.AllowGet);

            }

        }

        public JsonResult ObtenerOfertasApp()

        {
            List<OfertaDelivery> ofertas = new OfertaDeliveryBL().ObtenerListadoActivas();
            foreach (OfertaDelivery of in ofertas) {
                ProductoPrecio pre = new ProductoBL().ObtenerPrecioActualPorProductoId(of.ProductoBase.ProductoId, of.ProductoBase.UnidadId);
                of.ProductoBase.PrecioActual = pre.Valor;
                
            }

            return Json(ofertas, JsonRequestBehavior.AllowGet);
        }

        public JsonResult ObtenerCobertura(string direccionc)

        {
            List<string> ofertas = new ClienteBL().ObtenerCobertura(direccionc);

            List<RespuestaApp> listado = new List<RespuestaApp>();
            int i = 0;
            foreach (string item in ofertas) {

                RespuestaApp items = new RespuestaApp();
                items.Id = i;
                items.Respuesta0 = item;
                listado.Add(items);
                i++;
            }
            return Json(listado, JsonRequestBehavior.AllowGet);
        }
    }
}