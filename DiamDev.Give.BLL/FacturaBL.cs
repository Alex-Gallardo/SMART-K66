using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.BLL
{
    public class FacturaBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public FacturaBL()
            {
                this.db = new GiveContext();
            }

        #endregion

        #region Metodos Privados

            private int Correlativo()
            {
                int Id = 0;

                try
                {
                    Factura FacturaActual = db.Set<Factura>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (FacturaActual != null)
                    {
                        Inicial_Id = FacturaActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;
                }
                catch (Exception)
                {}

                return Id;
            }

            private string Agregar(Factura entidad)
            {
                string Mensaje = "OK";

                try
                {
                    if (!entidad.Pagada)
                    {
                        Cliente ClienteActual = db.Set<Cliente>().AsNoTracking().Where(x => x.ClienteId == entidad.ClienteId).FirstOrDefault();
                        if (ClienteActual == null)
                        {
                            return "Se le informa que no contiene cliente asignado en la factura";
                        }

                        decimal TotalFactura = 0;
                        decimal LimiteCreditoCliente = ClienteActual.LimiteCredito == null ? 0 : ClienteActual.LimiteCredito.Value;

                        if (entidad.Detalles != null && entidad.Detalles.Count() > 0)
                        {
                            TotalFactura = entidad.Detalles.Sum(x => x.Cantidad * x.Precio);
                        }

                        List<Factura> FacturasNoPagados = db.Set<Factura>().Include("Detalles").AsNoTracking().Where(x => x.ClienteId == ClienteActual.ClienteId && !x.Pagada && !x.Anulada).ToList();
                        if (FacturasNoPagados != null && FacturasNoPagados.Count() > 0)
                        {
                            TotalFactura += FacturasNoPagados.Sum(x => x.Detalles.Sum(y => y.Cantidad * y.Precio));
                        }

                        if (TotalFactura > LimiteCreditoCliente)
                        {
                            return "Se le informa que no se puede registrar la factura no cuenta con el credito suficiente";
                        }
                    }

                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngFacturaId = new Herramienta().Formato_Correlativo(Id);

                        if (lngFacturaId > 0)
                        {
                            entidad.FacturaId = lngFacturaId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;
                            entidad.FechaHoraFactura = DateTime.Now;

                            if (entidad.DiaCredito > 0)
                            {
                                entidad.Credito = true;
                            }
                            else if (entidad.DiaCredito == 0)
                            {
                                entidad.Credito = false;
                            }

                            if (entidad.NotaCreditoId != null)
                            {
                                NotaCredito NotaActual = db.Set<NotaCredito>().Where(x => x.CreditoId == entidad.NotaCreditoId.Value).FirstOrDefault();
                                if (NotaActual != null)
                                {
                                    NotaActual.Operado = true;
                                }
                            }

                            if (entidad.Detalles != null && entidad.Detalles.Count() > 0)
                            {
                                int DetalleId = 1;                               
                                foreach (var Producto in entidad.Detalles)
                                {
                                    Producto.DetalleId = DetalleId;
                                    Producto.FacturaId = entidad.FacturaId;
                                
                                    DetalleId += 1;
                                }
                            }

                            if (entidad.Pagos != null && entidad.Pagos.Count() > 0)
                            {
                                int i = 1;
                                foreach (var Pago in entidad.Pagos)
                                {
                                    Pago.DetalleId = i;
                                    Pago.FacturaId = entidad.FacturaId;
                                    Pago.Fecha = DateTime.Today;
                                    Pago.UsrOperacionId = entidad.UsrCreo;

                                    i++;
                                }
                            }

                            if (entidad.PedidoId != null && entidad.PedidoId > 0)
                            {
                                Pedido PedidoActual = db.Set<Pedido>().Where(x => x.PedidoId == entidad.PedidoId.Value).FirstOrDefault();
                                if (PedidoActual != null)
                                {
                                    PedidoActual.Operada = true;
                                    PedidoActual.FechaHoraOpero = DateTime.Now;
                                    PedidoActual.UsrOpero = entidad.UsrCreo;
                                }
                            }

                            if (entidad.PedidoId == 0)
                            {
                                entidad.PedidoId = null;
                            }

                            if (entidad.ReservaId != null)
                            {
                                Reserva ReservaActual = db.Set<Reserva>().Where(x => x.ReservaId == entidad.ReservaId.Value).FirstOrDefault();
                                if (ReservaActual != null)
                                {
                                    ReservaActual.Operado = true;
                                }
                            }

                            //Se verifica que productos tiene configurado lote
                            if (entidad.Detalles != null && entidad.Detalles.Count() > 0)
                            {
                                List<string> ProductoIDs = entidad.Detalles.Select(x => x.ProductoId).ToList();
                                if (ProductoIDs != null && ProductoIDs.Count() > 0)
                                {
                                    entidad.ProductoLote = db.Set<Producto>().AsNoTracking().Where(x => ProductoIDs.Contains(x.ProductoId) && x.TieneLote).Count() > 0;                                  
                                }
                            }                           

                            db.Set<Factura>().Add(entidad);
                            db.SaveChanges();

                            //Se valida que si se realizo la factura de manera local
                            if (Mensaje.Equals("OK"))
                            {
                                bool FacturaElectronica = false;

                                //Se obtiene la configuracion para validar que este habilitada la opcion de factura electronica
                                Configuracion ConfiguracionFacturaElectronica = db.Set<Configuracion>().AsNoTracking().Where(x => x.ConfiguracionId == 20191010001).FirstOrDefault();
                                if (ConfiguracionFacturaElectronica != null)
                                {
                                    int Configuracion = 0;
                                    int.TryParse(ConfiguracionFacturaElectronica.Valor, out Configuracion);
                                    if (Configuracion == 1)
                                    {
                                        FacturaElectronica = true;
                                    }
                                }

                                //Se verifica que tenga habilitada la opcion de factura electronica en la configuracion
                                if (FacturaElectronica)
                                {
                                    try
                                    {
                                        //Se cambia el estado de la factura a electronica
                                        Factura FacturaElectronicaActual = db.Set<Factura>().Where(x => x.FacturaId == entidad.FacturaId).FirstOrDefault();
                                        if (FacturaElectronicaActual != null)
                                        {
                                            FacturaElectronicaActual.FacturaElectronica = true;
                                            db.SaveChanges();
                                        }

                                        Mensaje = GenerarDIGIFACT(new Factura() { FacturaId = FacturaElectronicaActual.FacturaId });
                                        if (!Mensaje.Equals("OK"))
                                        {
                                            Mensaje = "OK";
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {                    
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }                        
              
                return Mensaje;
            }

        #region Metodos DIGIFACT

            private async Task<string> PostToken(Uri u, HttpContent c)
            {
                var response = string.Empty;
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

                    HttpResponseMessage result = await client.PostAsync(u, c);
                    if (result.IsSuccessStatusCode)
                    {
                        response = await result.Content.ReadAsStringAsync();
                    }
                }

                return response;
            }

            private async Task<string> PostEnviar(Uri u, HttpContent c, string token)
            {
                var response = string.Empty;
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Add("Authorization", token);

                    HttpResponseMessage result = await client.PostAsync(u, c);
                    if (result.IsSuccessStatusCode)
                    {
                        response = await result.Content.ReadAsStringAsync();
                    }
                }

                return response;
            }

            private async Task<string> GetEnviar(Uri u, string token)
            {
                var response = string.Empty;
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Add("Authorization", token);

                    HttpResponseMessage result = await client.GetAsync(u);
                    if (result.IsSuccessStatusCode)
                    {
                        response = await result.Content.ReadAsStringAsync();
                    }
                }

                return response;
            }

            private string GenerarFacturaComercialDIGIFACT(Factura entidad, string codigoEstablecimiento, string direccionEstablecimiento, Empresa empresa)
            {
                string XML = string.Empty;
                string FechaHoraEmision = entidad.FechaHoraFactura.Value.ToString("yyyy-MM-ddThh:mm:ss");

                string afiliacion = empresa.AfiliacionIvaDIGIFACT;
                string tipodoc = "FACT";
                if (afiliacion == "PEQ")
                {
                    tipodoc = "FPEQ";
                }

                try
                {
                    StringBuilder sbXML = new StringBuilder();
                    sbXML.Append("<dte:GTDocumento xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:dte=\"http://www.sat.gob.gt/dte/fel/0.2.0\" Version=\"0.1\">");
                    sbXML.AppendLine();
                    sbXML.Append(" <dte:SAT ClaseDocumento=\"dte\">");
                    sbXML.AppendLine();
                    sbXML.Append("   <dte:DTE ID=\"DatosCertificados\">");
                    sbXML.AppendLine();
                    sbXML.Append("     <dte:DatosEmision ID=\"DatosEmision\">");
                    sbXML.AppendLine();
                    sbXML.AppendFormat("       <dte:DatosGenerales CodigoMoneda=\"GTQ\" FechaHoraEmision=\"{0}\" Tipo=\"{1}\"></dte:DatosGenerales>", FechaHoraEmision, tipodoc);
                    sbXML.AppendLine();

                //EMISOR
               
               
                    sbXML.AppendFormat("       <dte:Emisor AfiliacionIVA=\""+afiliacion+"\" CodigoEstablecimiento=\"{0}\" NITEmisor=\"{1}\" NombreComercial=\"{2}\" NombreEmisor=\"{3}\">", codigoEstablecimiento, empresa.NitEmisorDIGIFACT, empresa.NombreComercial, empresa.NombreEmisorDIGIFACT);
                    sbXML.AppendLine();
                    //CUERPO EMISOR
                    sbXML.Append("          <dte:DireccionEmisor>");
                    sbXML.AppendLine();
                    sbXML.AppendFormat("            <dte:Direccion>{0}</dte:Direccion>", direccionEstablecimiento);
                    sbXML.AppendLine();
                    sbXML.AppendFormat("            <dte:CodigoPostal>{0}</dte:CodigoPostal>", empresa.CodigoPostalEmisorDIGIFACT);
                    sbXML.AppendLine();
                    sbXML.AppendFormat("            <dte:Municipio>{0}</dte:Municipio>", empresa.MunicipioEmisorDIGIFACT);
                    sbXML.AppendLine();
                    sbXML.AppendFormat("            <dte:Departamento>{0}</dte:Departamento>", empresa.DepartamentoEmisorDIGIFACT);
                    sbXML.AppendLine();
                    sbXML.AppendFormat("            <dte:Pais>{0}</dte:Pais>", empresa.PaisEmisorDIGIFACT);
                    sbXML.AppendLine();
                    sbXML.Append("          </dte:DireccionEmisor>");
                    sbXML.AppendLine();
                    //FIN CUERPO EMISOR                       
                    sbXML.Append("       </dte:Emisor>");
                    sbXML.AppendLine();
                    //FIN EMISOR

                    string nitreceptor=entidad.Cliente.Nit.Replace("-", "");
                    if (nitreceptor.Length > 10) 
                    {
                        nitreceptor = "CF";
                    }

                    //RECEPTOR
                    sbXML.AppendFormat("       <dte:Receptor CorreoReceptor=\"\" IDReceptor=\"{0}\" NombreReceptor=\"{1}\">",nitreceptor , entidad.Cliente.Nombre.Replace("&","Y"));
                    sbXML.AppendLine();
                    //CUERPO RECEPTOR
                    sbXML.Append("          <dte:DireccionReceptor>");
                    sbXML.AppendLine();
                    sbXML.AppendFormat("            <dte:Direccion>{0}</dte:Direccion>", entidad.Cliente.Direccion);
                    sbXML.AppendLine();
                    sbXML.AppendFormat("            <dte:CodigoPostal>{0}</dte:CodigoPostal>", "01001");
                    sbXML.AppendLine();
                    sbXML.AppendFormat("            <dte:Municipio>{0}</dte:Municipio>", "GUATEMALA");
                    sbXML.AppendLine();
                    sbXML.AppendFormat("            <dte:Departamento>{0}</dte:Departamento>", "GUATEMALA");
                    sbXML.AppendLine();
                    sbXML.AppendFormat("            <dte:Pais>{0}</dte:Pais>", empresa.PaisEmisorDIGIFACT);
                    sbXML.AppendLine();
                    sbXML.Append("          </dte:DireccionReceptor>");
                    sbXML.AppendLine();
                    //FIN CUERPO RECEPTOR                       
                    sbXML.Append("       </dte:Receptor>");
                    sbXML.AppendLine();
                    //FIN RECEPTOR

                    //FRASES
                    sbXML.Append("       <dte:Frases>");
                    sbXML.AppendLine();
                    //CUERPO FRASES

                    string tipofrase = empresa.TipoFraseDIGIFACT;

                    sbXML.Append("         <dte:Frase CodigoEscenario=\""+ empresa.CodigoEscenarioDIGIFACT +"\" TipoFrase=\"" + tipofrase  + "\"></dte:Frase>");
                    //FIN CUERPO FRASES
                    sbXML.AppendLine();
                    sbXML.Append("       </dte:Frases>");
                    sbXML.AppendLine();
                    //FIN FRASES

                    //ITEMS
                    sbXML.Append("       <dte:Items>");
                    sbXML.AppendLine();
                    //CUERPO ITEMS

                    decimal dTotal = decimal.Round(entidad.Detalles.Sum(x => x.Cantidad * x.Precio), 2);
                    decimal dTotalImpuesto = 0;

                    int Linea = 1;
                    foreach (FacturaDetalle Detalle in entidad.Detalles)
                    {
                        decimal lTotalxArticulo = decimal.Round(Detalle.Cantidad * Detalle.Precio, 2);
                        decimal lTotalxArticuloSinIva = decimal.Round((lTotalxArticulo / decimal.Parse("1.12")), 2);
                        decimal lIva = decimal.Round(((Detalle.Cantidad * Detalle.Precio) / decimal.Parse("1.12")), 2);
                        decimal lTotalxArticuloIva = decimal.Round(lTotalxArticulo - lIva, 2);
                        dTotalImpuesto += lTotalxArticuloIva;

                        sbXML.AppendFormat("         <dte:Item BienOServicio=\"{0}\" NumeroLinea=\"{1}\">", Detalle.Producto.CategoriaId == 20200525001 ? "B" : "S", Linea);
                        sbXML.AppendLine();
                        sbXML.AppendFormat("           <dte:Cantidad>{0}</dte:Cantidad>", decimal.Round(Detalle.Cantidad, 2));
                        sbXML.AppendLine();
                        sbXML.AppendFormat("           <dte:UnidadMedida>{0}</dte:UnidadMedida>", Detalle.Unidad.Codigo);
                        sbXML.AppendLine();
                        sbXML.AppendFormat("           <dte:Descripcion>{0}</dte:Descripcion>", Detalle.Nombre.Replace("&", "Y"));
                        sbXML.AppendLine();
                        sbXML.AppendFormat("           <dte:PrecioUnitario>{0}</dte:PrecioUnitario>", decimal.Round(Detalle.Precio, 2));
                        sbXML.AppendLine();
                        sbXML.AppendFormat("           <dte:Precio>{0}</dte:Precio>", lTotalxArticulo);
                        sbXML.AppendLine();
                        sbXML.AppendFormat("           <dte:Descuento>{0}</dte:Descuento>", "0.00");

                        if (afiliacion != "PEQ") 
                        { 
                            sbXML.AppendLine();
                            sbXML.Append("           <dte:Impuestos>");
                            sbXML.AppendLine();
                            sbXML.Append("             <dte:Impuesto>");
                            sbXML.AppendLine();
                            sbXML.AppendFormat("               <dte:NombreCorto>{0}</dte:NombreCorto>", "IVA");
                            sbXML.AppendLine();
                            sbXML.AppendFormat("               <dte:CodigoUnidadGravable>{0}</dte:CodigoUnidadGravable>", "1");
                            sbXML.AppendLine();
                            sbXML.AppendFormat("               <dte:MontoGravable>{0}</dte:MontoGravable>", lTotalxArticuloSinIva);
                            sbXML.AppendLine();
                            sbXML.AppendFormat("               <dte:MontoImpuesto>{0}</dte:MontoImpuesto>", lTotalxArticuloIva);
                            sbXML.AppendLine();
                            sbXML.Append("             </dte:Impuesto>");
                            sbXML.AppendLine();
                            sbXML.Append("           </dte:Impuestos>");
                        }
                        sbXML.AppendLine();
                        sbXML.AppendFormat("           <dte:Total>{0}</dte:Total>", lTotalxArticulo);
                        sbXML.AppendLine();
                        sbXML.Append("         </dte:Item>");

                        Linea++;
                    }

                    //FIN CUERPO ITEMS
                    sbXML.AppendLine();
                    sbXML.Append("       </dte:Items>");
                    sbXML.AppendLine();
                    //FIN ITEMS

                    //TOTALES
                    sbXML.Append("       <dte:Totales>");
                    sbXML.AppendLine();

                    //CUERPO TOTALES
                    if (afiliacion != "PEQ")
                    {
                        sbXML.Append("         <dte:TotalImpuestos>");
                        sbXML.AppendLine();
                        sbXML.AppendFormat("           <dte:TotalImpuesto NombreCorto=\"IVA\" TotalMontoImpuesto=\"{0}\"></dte:TotalImpuesto>", dTotalImpuesto);
                        sbXML.AppendLine();
                        sbXML.Append("         </dte:TotalImpuestos>");
                    }
                    sbXML.AppendLine();
                    sbXML.AppendFormat("         <dte:GranTotal>{0}</dte:GranTotal>", dTotal);

                    //FIN CUERPO TOTALES
                    sbXML.AppendLine();
                    sbXML.Append("       </dte:Totales>");
                    sbXML.AppendLine();
                    //FIN TOTALES                  

                    sbXML.AppendLine();
                    sbXML.Append("     </dte:DatosEmision>");
                    sbXML.AppendLine();
                    sbXML.Append("   </dte:DTE>");


                if (tipofrase == "3") {
                    //SE AGREGA LA ADENDA

                    sbXML.AppendLine();
                    sbXML.Append("<dte:Adenda>");
                    sbXML.AppendLine();
                    sbXML.Append("<dtecomm:Informacion_COMERCIAL xmlns:dtecomm=\"https://www.digifact.com.gt/dtecomm\" xsi:schemaLocation = \"https://www.digifact.com.gt/dtecomm\"> ");
                    sbXML.AppendLine();
                    sbXML.Append("<dtecomm:InformacionAdicional Version=\"7.1234654163\">");
                    sbXML.AppendLine();
                    sbXML.Append("<dtecomm:REFERENCIA_INTERNA>"+entidad.FacturaId+"</dtecomm:REFERENCIA_INTERNA>");
                    sbXML.AppendLine();
                    sbXML.Append("<dtecomm:FECHA_REFERENCIA>"+FechaHoraEmision+"</dtecomm:FECHA_REFERENCIA>");
                    sbXML.AppendLine();
                    sbXML.Append("<dtecomm:VALIDAR_REFERENCIA_INTERNA>NO_VALIDAR</dtecomm:VALIDAR_REFERENCIA_INTERNA>");
                    sbXML.AppendLine();
                    sbXML.Append("</dtecomm:InformacionAdicional>");
                    sbXML.AppendLine();
                    sbXML.Append("</dtecomm:Informacion_COMERCIAL>");
                    sbXML.AppendLine();
                    sbXML.Append("</dte:Adenda>");
                }               

                    sbXML.AppendLine();
                    sbXML.Append(" </dte:SAT>");
                    sbXML.AppendLine();
                    sbXML.Append("</dte:GTDocumento>");

                    XML = sbXML.ToString();
                }
                catch (Exception ex)
                {
                    return string.Format("No se genero el XML, descripción del error {0}", ex.Message);
                }

                return XML;
            }

            private string GenerarFacturaCambiariaDIGIFACT(Factura entidad, string codigoEstablecimiento, string direccionEstablecimiento, List<Configuracion> configuraciones)
            {
                string XML = string.Empty;
                string FechaHoraEmision = entidad.FechaHoraFactura.Value.ToString("yyyy-MM-ddThh:mm:ss");

                string afiliacion = configuraciones.Where(x => x.ConfiguracionId == 20200722002).Select(x => x.Valor).FirstOrDefault().Trim();

                try
                {
                    StringBuilder sbXML = new StringBuilder();
                    sbXML.Append("<dte:GTDocumento xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:dte=\"http://www.sat.gob.gt/dte/fel/0.2.0\" Version=\"0.1\">");
                    sbXML.AppendLine();
                    sbXML.Append(" <dte:SAT ClaseDocumento=\"dte\">");
                    sbXML.AppendLine();
                    sbXML.Append("   <dte:DTE ID=\"DatosCertificados\">");
                    sbXML.AppendLine();
                    sbXML.Append("     <dte:DatosEmision ID=\"DatosEmision\">");
                    sbXML.AppendLine();
                    sbXML.AppendFormat("       <dte:DatosGenerales CodigoMoneda=\"GTQ\" FechaHoraEmision=\"{0}\" Tipo=\"{1}\"></dte:DatosGenerales>", FechaHoraEmision, "FCAM");
                    sbXML.AppendLine();

                    //EMISOR
                    sbXML.AppendFormat("       <dte:Emisor AfiliacionIVA=\"" + afiliacion + "\" CodigoEstablecimiento=\"{0}\" NITEmisor=\"{1}\" NombreComercial=\"{2}\" NombreEmisor=\"{3}\">", codigoEstablecimiento, configuraciones.Where(x => x.ConfiguracionId == 20191010016).Select(x => x.Valor).FirstOrDefault(), configuraciones.Where(x => x.ConfiguracionId == 20191010017).Select(x => x.Valor).FirstOrDefault(), configuraciones.Where(x => x.ConfiguracionId == 20191010018).Select(x => x.Valor).FirstOrDefault());
                    sbXML.AppendLine();
                    //CUERPO EMISOR
                    sbXML.Append("          <dte:DireccionEmisor>");
                    sbXML.AppendLine();
                    sbXML.AppendFormat("            <dte:Direccion>{0}</dte:Direccion>", direccionEstablecimiento);
                    sbXML.AppendLine();
                    sbXML.AppendFormat("            <dte:CodigoPostal>{0}</dte:CodigoPostal>", configuraciones.Where(x => x.ConfiguracionId == 20191010020).Select(x => x.Valor).FirstOrDefault());
                    sbXML.AppendLine();
                    sbXML.AppendFormat("            <dte:Municipio>{0}</dte:Municipio>", configuraciones.Where(x => x.ConfiguracionId == 20191010021).Select(x => x.Valor).FirstOrDefault());
                    sbXML.AppendLine();
                    sbXML.AppendFormat("            <dte:Departamento>{0}</dte:Departamento>", configuraciones.Where(x => x.ConfiguracionId == 20191010022).Select(x => x.Valor).FirstOrDefault());
                    sbXML.AppendLine();
                    sbXML.AppendFormat("            <dte:Pais>{0}</dte:Pais>", configuraciones.Where(x => x.ConfiguracionId == 20191010011).Select(x => x.Valor).FirstOrDefault());
                    sbXML.AppendLine();
                    sbXML.Append("          </dte:DireccionEmisor>");
                    sbXML.AppendLine();
                    //FIN CUERPO EMISOR                       
                    sbXML.Append("       </dte:Emisor>");
                    sbXML.AppendLine();
                    //FIN EMISOR

                    string nitreceptor = entidad.Cliente.Nit.Replace("-", "");
                    if (nitreceptor.Length > 10)
                    {
                        nitreceptor = "CF";
                    }

                    //RECEPTOR
                    sbXML.AppendFormat("       <dte:Receptor CorreoReceptor=\"\" IDReceptor=\"{0}\" NombreReceptor=\"{1}\">", nitreceptor, entidad.Cliente.Nombre.Replace("&", "Y"));
                    sbXML.AppendLine();
                    //CUERPO RECEPTOR
                    sbXML.Append("          <dte:DireccionReceptor>");
                    sbXML.AppendLine();
                    sbXML.AppendFormat("            <dte:Direccion>{0}</dte:Direccion>", entidad.Cliente.Direccion);
                    sbXML.AppendLine();
                    sbXML.AppendFormat("            <dte:CodigoPostal>{0}</dte:CodigoPostal>", "01001");
                    sbXML.AppendLine();
                    sbXML.AppendFormat("            <dte:Municipio>{0}</dte:Municipio>", "GUATEMALA");
                    sbXML.AppendLine();
                    sbXML.AppendFormat("            <dte:Departamento>{0}</dte:Departamento>", "GUATEMALA");
                    sbXML.AppendLine();
                    sbXML.AppendFormat("            <dte:Pais>{0}</dte:Pais>", configuraciones.Where(x => x.ConfiguracionId == 20191010011).Select(x => x.Valor).FirstOrDefault());
                    sbXML.AppendLine();
                    sbXML.Append("          </dte:DireccionReceptor>");
                    sbXML.AppendLine();
                    //FIN CUERPO RECEPTOR                       
                    sbXML.Append("       </dte:Receptor>");
                    sbXML.AppendLine();
                    //FIN RECEPTOR

                    //FRASES
                    sbXML.Append("       <dte:Frases>");
                    sbXML.AppendLine();
                    //CUERPO FRASES
                    string tipofrase = configuraciones.Where(x => x.ConfiguracionId == 20200722001).Select(x => x.Valor).FirstOrDefault().Trim();

                    sbXML.Append("         <dte:Frase CodigoEscenario=\"" + configuraciones.Where(x => x.ConfiguracionId == 20200715001).Select(x => x.Valor).FirstOrDefault() + "\" TipoFrase=\"" + tipofrase + "\"></dte:Frase>");
                    //FIN CUERPO FRASES
                    sbXML.AppendLine();
                    sbXML.Append("       </dte:Frases>");
                    sbXML.AppendLine();
                    //FIN FRASES

                    //ITEMS
                    sbXML.Append("       <dte:Items>");
                    sbXML.AppendLine();
                    //CUERPO ITEMS

                    decimal dTotal = decimal.Round(entidad.Detalles.Sum(x => x.Cantidad * x.Precio), 2);
                    decimal dTotalImpuesto = 0;

                    int Linea = 1;
                    foreach (FacturaDetalle Detalle in entidad.Detalles)
                    {
                        decimal lTotalxArticulo = decimal.Round(Detalle.Cantidad * Detalle.Precio, 2);
                        decimal lTotalxArticuloSinIva = decimal.Round((lTotalxArticulo / decimal.Parse("1.12")), 2);
                        decimal lIva = decimal.Round(((Detalle.Cantidad * Detalle.Precio) / decimal.Parse("1.12")), 2);
                        decimal lTotalxArticuloIva = decimal.Round(lTotalxArticulo - lIva, 2);
                        dTotalImpuesto += lTotalxArticuloIva;

                        sbXML.AppendFormat("         <dte:Item BienOServicio=\"B\" NumeroLinea=\"{0}\">", Linea);
                        sbXML.AppendLine();
                        sbXML.AppendFormat("           <dte:Cantidad>{0}</dte:Cantidad>", decimal.Round(Detalle.Cantidad, 2));
                        sbXML.AppendLine();
                        sbXML.AppendFormat("           <dte:UnidadMedida>{0}</dte:UnidadMedida>", Detalle.Unidad.Codigo);
                        sbXML.AppendLine();
                        sbXML.AppendFormat("           <dte:Descripcion>{0}</dte:Descripcion>", Detalle.Producto.Nombre.Replace("&", "Y"));
                        sbXML.AppendLine();
                        sbXML.AppendFormat("           <dte:PrecioUnitario>{0}</dte:PrecioUnitario>", decimal.Round(Detalle.Precio, 2));
                        sbXML.AppendLine();
                        sbXML.AppendFormat("           <dte:Precio>{0}</dte:Precio>", lTotalxArticulo);
                        sbXML.AppendLine();
                        sbXML.AppendFormat("           <dte:Descuento>{0}</dte:Descuento>", "0.00");
                        sbXML.AppendLine();
                        sbXML.Append("           <dte:Impuestos>");
                        sbXML.AppendLine();
                        sbXML.Append("             <dte:Impuesto>");
                        sbXML.AppendLine();
                        sbXML.AppendFormat("               <dte:NombreCorto>{0}</dte:NombreCorto>", "IVA");
                        sbXML.AppendLine();
                        sbXML.AppendFormat("               <dte:CodigoUnidadGravable>{0}</dte:CodigoUnidadGravable>", "1");
                        sbXML.AppendLine();
                        sbXML.AppendFormat("               <dte:MontoGravable>{0}</dte:MontoGravable>", lTotalxArticuloSinIva);
                        sbXML.AppendLine();
                        sbXML.AppendFormat("               <dte:MontoImpuesto>{0}</dte:MontoImpuesto>", lTotalxArticuloIva);
                        sbXML.AppendLine();
                        sbXML.Append("             </dte:Impuesto>");
                        sbXML.AppendLine();
                        sbXML.Append("           </dte:Impuestos>");
                        sbXML.AppendLine();
                        sbXML.AppendFormat("           <dte:Total>{0}</dte:Total>", lTotalxArticulo);
                        sbXML.AppendLine();
                        sbXML.Append("         </dte:Item>");

                        Linea++;
                    }

                    //FIN CUERPO ITEMS
                    sbXML.AppendLine();
                    sbXML.Append("       </dte:Items>");
                    sbXML.AppendLine();
                    //FIN ITEMS

                    //TOTALES
                    sbXML.Append("       <dte:Totales>");
                    sbXML.AppendLine();
                    //CUERPO TOTALES

                    sbXML.Append("         <dte:TotalImpuestos>");
                    sbXML.AppendLine();
                    sbXML.AppendFormat("           <dte:TotalImpuesto NombreCorto=\"IVA\" TotalMontoImpuesto=\"{0}\"></dte:TotalImpuesto>", dTotalImpuesto);
                    sbXML.AppendLine();
                    sbXML.Append("         </dte:TotalImpuestos>");
                    sbXML.AppendLine();
                    sbXML.AppendFormat("         <dte:GranTotal>{0}</dte:GranTotal>", dTotal);

                    //FIN CUERPO TOTALES
                    sbXML.AppendLine();
                    sbXML.Append("       </dte:Totales>");
                    sbXML.AppendLine();
                    //FIN TOTALES   

                    //COMPLEMENTOS - FACTURA CAMBIARIA
                    sbXML.Append("       <dte:Complementos>");
                    sbXML.AppendLine();
                    //CUERPO COMPLEMENTOS
                    sbXML.Append("         <dte:Complemento xmlns:cfc=\"http://www.sat.gob.gt/dte/fel/CompCambiaria/0.1.0\" URIComplemento=\"dtecamb\" NombreComplemento=\"FCAMB\" IDComplemento=\"ID\" xsi:schemaLocation=\"http://www.sat.gob.gt/dte/fel/CompCambiaria/0.1.0 GT_Complemento_Cambiaria-0.1.0.xsd\">");
                    sbXML.AppendLine();

                    sbXML.Append("           <cfc:AbonosFacturaCambiaria Version=\"1\">");
                    sbXML.AppendLine();

                    sbXML.Append("             <cfc:Abono>");
                    sbXML.AppendLine();

                    sbXML.Append("               <cfc:NumeroAbono>1</cfc:NumeroAbono>");
                    sbXML.AppendLine();
                    sbXML.AppendFormat("               <cfc:FechaVencimiento>{0}</cfc:FechaVencimiento>", entidad.Fecha.AddDays(90).ToString("yyyy-MM-dd"));
                    sbXML.AppendLine();
                    sbXML.AppendFormat("               <cfc:MontoAbono>{0}</cfc:MontoAbono>", dTotal);

                    sbXML.AppendLine();
                    sbXML.Append("             </cfc:Abono>");

                    sbXML.AppendLine();
                    sbXML.Append("           </cfc:AbonosFacturaCambiaria>");

                    sbXML.AppendLine();
                    sbXML.Append("         </dte:Complemento>");
                    sbXML.AppendLine();

                    //FIN CUERPO COMPLEMENTOS
                    sbXML.Append("       </dte:Complementos>");
                    //FIN COMPLEMENTOS - FACTURA CAMBIARIA

                    sbXML.AppendLine();
                    sbXML.Append("     </dte:DatosEmision>");
                    sbXML.AppendLine();
                    sbXML.Append("   </dte:DTE>");
                    sbXML.AppendLine();
                    sbXML.Append(" </dte:SAT>");
                    sbXML.AppendLine();
                    sbXML.Append("</dte:GTDocumento>");

                    XML = sbXML.ToString();
                }
                catch (Exception ex)
                {
                    return string.Format("No se genero el XML, descripción del error {0}", ex.Message);
                }

                return XML;
            }

            private string GenerarNotaCreditoDIGIFACT(Factura entidad, FacturaNotaCredito nota, string codigoEstablecimiento, List<Configuracion> configuraciones)
            {
                string XML = string.Empty;
                string FechaHoraEmision = nota.FechaHoraNotaCredito.ToString("yyyy-MM-ddThh:mm:ss");

                string afiliacion = configuraciones.Where(x => x.ConfiguracionId == 20200722002).Select(x => x.Valor).FirstOrDefault().Trim();

                try
                {
                    StringBuilder sbXML = new StringBuilder();
                    sbXML.Append("<dte:GTDocumento xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:dte=\"http://www.sat.gob.gt/dte/fel/0.2.0\" Version=\"0.1\">");
                    sbXML.AppendLine();
                    sbXML.Append(" <dte:SAT ClaseDocumento=\"dte\">");
                    sbXML.AppendLine();
                    sbXML.Append("   <dte:DTE ID=\"DatosCertificados\">");
                    sbXML.AppendLine();
                    sbXML.Append("     <dte:DatosEmision ID=\"DatosEmision\">");
                    sbXML.AppendLine();
                    sbXML.AppendFormat("       <dte:DatosGenerales CodigoMoneda=\"GTQ\" FechaHoraEmision=\"{0}\" Tipo=\"{1}\"></dte:DatosGenerales>", FechaHoraEmision, "NCRE");
                    sbXML.AppendLine();

                    //EMISOR
                    sbXML.AppendFormat("       <dte:Emisor AfiliacionIVA=\"" + afiliacion + "\" CodigoEstablecimiento=\"{0}\" NITEmisor=\"{1}\" NombreComercial=\"{2}\" NombreEmisor=\"{3}\">", codigoEstablecimiento, configuraciones.Where(x => x.ConfiguracionId == 20191010016).Select(x => x.Valor).FirstOrDefault(), configuraciones.Where(x => x.ConfiguracionId == 20191010017).Select(x => x.Valor).FirstOrDefault(), configuraciones.Where(x => x.ConfiguracionId == 20191010018).Select(x => x.Valor).FirstOrDefault());
                    sbXML.AppendLine();
                    //CUERPO EMISOR
                    sbXML.Append("          <dte:DireccionEmisor>");
                    sbXML.AppendLine();
                    sbXML.AppendFormat("            <dte:Direccion>{0}</dte:Direccion>", configuraciones.Where(x => x.ConfiguracionId == 20191010019).Select(x => x.Valor).FirstOrDefault());
                    sbXML.AppendLine();
                    sbXML.AppendFormat("            <dte:CodigoPostal>{0}</dte:CodigoPostal>", configuraciones.Where(x => x.ConfiguracionId == 20191010020).Select(x => x.Valor).FirstOrDefault());
                    sbXML.AppendLine();
                    sbXML.AppendFormat("            <dte:Municipio>{0}</dte:Municipio>", configuraciones.Where(x => x.ConfiguracionId == 20191010021).Select(x => x.Valor).FirstOrDefault());
                    sbXML.AppendLine();
                    sbXML.AppendFormat("            <dte:Departamento>{0}</dte:Departamento>", configuraciones.Where(x => x.ConfiguracionId == 20191010022).Select(x => x.Valor).FirstOrDefault());
                    sbXML.AppendLine();
                    sbXML.AppendFormat("            <dte:Pais>{0}</dte:Pais>", configuraciones.Where(x => x.ConfiguracionId == 20191010011).Select(x => x.Valor).FirstOrDefault());
                    sbXML.AppendLine();
                    sbXML.Append("          </dte:DireccionEmisor>");
                    sbXML.AppendLine();
                    //FIN CUERPO EMISOR                       
                    sbXML.Append("       </dte:Emisor>");
                    sbXML.AppendLine();
                    //FIN EMISOR

                    string nitreceptor = entidad.Cliente.Nit.Replace("-", "");
                    if (nitreceptor.Length > 10)
                    {
                        nitreceptor = "CF";
                    }

                    //RECEPTOR
                    sbXML.AppendFormat("       <dte:Receptor CorreoReceptor=\"\" IDReceptor=\"{0}\" NombreReceptor=\"{1}\">", nitreceptor, entidad.Cliente.Nombre.Replace("&", "Y"));
                    sbXML.AppendLine();
                    //CUERPO RECEPTOR
                    sbXML.Append("          <dte:DireccionReceptor>");
                    sbXML.AppendLine();
                    sbXML.AppendFormat("            <dte:Direccion>{0}</dte:Direccion>", entidad.Cliente.Direccion);
                    sbXML.AppendLine();
                    sbXML.AppendFormat("            <dte:CodigoPostal>{0}</dte:CodigoPostal>", "01001");
                    sbXML.AppendLine();
                    sbXML.AppendFormat("            <dte:Municipio>{0}</dte:Municipio>", "GUATEMALA");
                    sbXML.AppendLine();
                    sbXML.AppendFormat("            <dte:Departamento>{0}</dte:Departamento>", "GUATEMALA");
                    sbXML.AppendLine();
                    sbXML.AppendFormat("            <dte:Pais>{0}</dte:Pais>", configuraciones.Where(x => x.ConfiguracionId == 20191010011).Select(x => x.Valor).FirstOrDefault());
                    sbXML.AppendLine();
                    sbXML.Append("          </dte:DireccionReceptor>");
                    sbXML.AppendLine();
                    //FIN CUERPO RECEPTOR                       
                    sbXML.Append("       </dte:Receptor>");
                    sbXML.AppendLine();
                    //FIN RECEPTOR                  

                    //ITEMS
                    sbXML.Append("       <dte:Items>");
                    sbXML.AppendLine();
                    //CUERPO ITEMS

                    decimal dTotal = decimal.Round(nota.Detalles.Sum(x => x.Cantidad * x.Precio), 2);
                    decimal dTotalImpuesto = 0;

                    int Linea = 1;
                    foreach (FacturaNotaCreditoDetalle Detalle in nota.Detalles)
                    {
                        decimal lTotalxArticulo = decimal.Round(Detalle.Cantidad * Detalle.Precio, 2);
                        decimal lTotalxArticuloSinIva = decimal.Round((lTotalxArticulo / decimal.Parse("1.12")), 2);
                        decimal lIva = decimal.Round(((Detalle.Cantidad * Detalle.Precio) / decimal.Parse("1.12")), 2);
                        decimal lTotalxArticuloIva = decimal.Round(lTotalxArticulo - lIva, 2);
                        dTotalImpuesto += lTotalxArticuloIva;

                        sbXML.AppendFormat("         <dte:Item BienOServicio=\"B\" NumeroLinea=\"{0}\">", Linea);
                        sbXML.AppendLine();
                        sbXML.AppendFormat("           <dte:Cantidad>{0}</dte:Cantidad>", decimal.Round(Detalle.Cantidad, 2));
                        sbXML.AppendLine();
                        sbXML.AppendFormat("           <dte:UnidadMedida>{0}</dte:UnidadMedida>", Detalle.Unidad.Codigo);
                        sbXML.AppendLine();
                        sbXML.AppendFormat("           <dte:Descripcion>{0}</dte:Descripcion>", Detalle.Producto.Nombre.Replace("&", "Y"));
                        sbXML.AppendLine();
                        sbXML.AppendFormat("           <dte:PrecioUnitario>{0}</dte:PrecioUnitario>", decimal.Round(Detalle.Precio, 2));
                        sbXML.AppendLine();
                        sbXML.AppendFormat("           <dte:Precio>{0}</dte:Precio>", lTotalxArticulo);
                        sbXML.AppendLine();
                        sbXML.AppendFormat("           <dte:Descuento>{0}</dte:Descuento>", "0.00");
                        sbXML.AppendLine();
                        sbXML.Append("           <dte:Impuestos>");
                        sbXML.AppendLine();
                        sbXML.Append("             <dte:Impuesto>");
                        sbXML.AppendLine();
                        sbXML.AppendFormat("               <dte:NombreCorto>{0}</dte:NombreCorto>", "IVA");
                        sbXML.AppendLine();
                        sbXML.AppendFormat("               <dte:CodigoUnidadGravable>{0}</dte:CodigoUnidadGravable>", "1");
                        sbXML.AppendLine();
                        sbXML.AppendFormat("               <dte:MontoGravable>{0}</dte:MontoGravable>", lTotalxArticuloSinIva);
                        sbXML.AppendLine();
                        sbXML.AppendFormat("               <dte:MontoImpuesto>{0}</dte:MontoImpuesto>", lTotalxArticuloIva);
                        sbXML.AppendLine();
                        sbXML.Append("             </dte:Impuesto>");
                        sbXML.AppendLine();
                        sbXML.Append("           </dte:Impuestos>");
                        sbXML.AppendLine();
                        sbXML.AppendFormat("           <dte:Total>{0}</dte:Total>", lTotalxArticulo);
                        sbXML.AppendLine();
                        sbXML.Append("         </dte:Item>");

                        Linea++;
                    }

                    //FIN CUERPO ITEMS
                    sbXML.AppendLine();
                    sbXML.Append("       </dte:Items>");
                    sbXML.AppendLine();
                    //FIN ITEMS

                    //TOTALES
                    sbXML.Append("       <dte:Totales>");
                    sbXML.AppendLine();
                    //CUERPO TOTALES

                    sbXML.Append("         <dte:TotalImpuestos>");
                    sbXML.AppendLine();
                    sbXML.AppendFormat("           <dte:TotalImpuesto NombreCorto=\"IVA\" TotalMontoImpuesto=\"{0}\"></dte:TotalImpuesto>", dTotalImpuesto);
                    sbXML.AppendLine();
                    sbXML.Append("         </dte:TotalImpuestos>");
                    sbXML.AppendLine();
                    sbXML.AppendFormat("         <dte:GranTotal>{0}</dte:GranTotal>", dTotal);

                    //FIN CUERPO TOTALES
                    sbXML.AppendLine();
                    sbXML.Append("       </dte:Totales>");
                    sbXML.AppendLine();
                    //FIN TOTALES   

                    //COMPLEMENTOS - NOTA DE CREDITO
                    sbXML.Append("       <dte:Complementos>");
                    sbXML.AppendLine();
                    //CUERPO COMPLEMENTOS
                    sbXML.Append("         <dte:Complemento URIComplemento=\"dteref\" NombreComplemento =\"NCRE\" xmlns:dteref=\"http://www.sat.gob.gt/face2/ComplementoReferenciaNota/0.1.0\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"http://www.sat.gob.gt/face2/ComplementoReferenciaNota/0.1.0 GT_Complemento_Referencia_Nota-0.1.0.xsd\">");
                    sbXML.AppendLine();

                    sbXML.AppendFormat("           <dteref:ReferenciasNota Version=\"1\" NumeroAutorizacionDocumentoOrigen=\"{0}\" FechaEmisionDocumentoOrigen=\"{1}\" MotivoAjuste=\"{2}\"  NumeroDocumentoOrigen=\"{3}\" SerieDocumentoOrigen=\"{4}\" />", entidad.UUIDFEL, entidad.Fecha.ToString("yyyy-MM-dd"), nota.Motivo, entidad.NumeroFEL, entidad.SerieFEL);
                   
                    sbXML.AppendLine();
                    sbXML.Append("         </dte:Complemento>");
                    sbXML.AppendLine();

                    //FIN CUERPO COMPLEMENTOS
                    sbXML.Append("       </dte:Complementos>");
                    //FIN COMPLEMENTOS - NOTA DE CREDITO

                    sbXML.AppendLine();
                    sbXML.Append("     </dte:DatosEmision>");
                    sbXML.AppendLine();
                    sbXML.Append("   </dte:DTE>");
                    sbXML.AppendLine();
                    sbXML.Append(" </dte:SAT>");
                    sbXML.AppendLine();
                    sbXML.Append("</dte:GTDocumento>");

                    XML = sbXML.ToString();
                }
                catch (Exception ex)
                {
                    return string.Format("No se genero el XML, descripción del error {0}", ex.Message);
                }

                return XML;
            }

            private string GenerarAnulacionFacturaDIGIFACT(Factura entidad, string comentarioAnulacion, Empresa empresa)
            {
                string XML = string.Empty;
                string FechaHoraEmision = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");

                try
                {
                    StringBuilder sbXML = new StringBuilder();
                    sbXML.Append("<dte:GTAnulacionDocumento xmlns:dte=\"http://www.sat.gob.gt/dte/fel/0.1.0\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"http://www.sat.gob.gt/dte/fel/0.1.0 https://www.digifact.com.gt/FEL/GT_Documento-0.1.0.xsd http://www.w3.org/2000/09/xmldsig# https://www.digifact.com.gt/FEL/XmlDSignatureVFEL.xsd\" Version =\"0.1\">");
                    sbXML.AppendLine();
                    sbXML.Append(" <dte:SAT>");
                    sbXML.AppendLine();
                    sbXML.Append("   <dte:AnulacionDTE ID=\"DatosCertificados\">");
                    sbXML.AppendLine();
                    sbXML.AppendFormat("       <dte:DatosGenerales ID=\"DatosAnulacion\" NumeroDocumentoAAnular=\"{0}\" NITEmisor=\"{1}\" IDReceptor=\"{2}\" FechaEmisionDocumentoAnular=\"{3}\" FechaHoraAnulacion=\"{4}\" MotivoAnulacion=\"{5}\"></dte:DatosGenerales>", entidad.UUIDFEL, empresa.NitEmisorDIGIFACT, entidad.Cliente.Nit.Replace("-", ""), entidad.FechaHoraCertificacionFEL, FechaHoraEmision, comentarioAnulacion);
                    sbXML.AppendLine();
                    sbXML.Append("   </dte:AnulacionDTE>");
                    sbXML.AppendLine();
                    sbXML.Append(" </dte:SAT>");
                    sbXML.AppendLine();
                    sbXML.Append("</dte:GTAnulacionDocumento>");

                    XML = sbXML.ToString();
                }
                catch (Exception ex)
                {
                    return string.Format("No se genero el XML, descripción del error {0}", ex.Message);
                }

                return XML;
            }

        #endregion

        #endregion

        #region Metodos Publicos

            public string Guardar(Factura entidad)
            {
                string Mensaje = "OK";
               
                if (entidad.FacturaId == 0)
                {
                    Mensaje = Agregar(entidad);
                }              
          
                return Mensaje;
            }

            public string GuardarLocal(Factura entidad)
            {
                string Mensaje = "OK";

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngFacturaId = new Herramienta().Formato_Correlativo(Id);

                        if (lngFacturaId > 0)
                        {
                            entidad.FacturaId = lngFacturaId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;
                            entidad.FechaHoraFactura = DateTime.Now;                          

                            if (entidad.Detalles != null && entidad.Detalles.Count() > 0)
                            {
                                int DetalleId = 1;
                                foreach (var Producto in entidad.Detalles)
                                {
                                    Producto.DetalleId = DetalleId;
                                    Producto.FacturaId = entidad.FacturaId;
                                
                                    DetalleId += 1;
                                }
                            }

                            if (entidad.Pagos != null && entidad.Pagos.Count() > 0)
                            {
                                int i = 1;
                                foreach (var Pago in entidad.Pagos)
                                {
                                    Pago.DetalleId = i;
                                    Pago.FacturaId = entidad.FacturaId;
                                
                                    i++;
                                }
                            } 

                            db.Set<Factura>().Add(entidad);
                            db.SaveChanges();

                            //Se valida que si se realizo la factura de manera local
                            if (Mensaje.Equals("OK"))
                            {
                                try
                                {
                                    //Se verifica el certificador que se encuentra habilitado  
                                    Configuracion ConfiguracionCertificador = db.Set<Configuracion>().AsNoTracking().Where(x => x.ConfiguracionId == 20191010015).FirstOrDefault();
                                    if (ConfiguracionCertificador != null)
                                    {
                                        if (ConfiguracionCertificador.Valor.Equals("2"))
                                        {
                                            Mensaje = GenerarDIGIFACT(new Factura() { FacturaId = entidad.FacturaId });
                                        }
                                        else
                                        {
                                            return "El certificador que se encuentra configurado no es valido";
                                        }
                                    }

                                    if (!Mensaje.Equals("OK"))
                                    {
                                        Mensaje = "OK";
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            public string Certificar(Factura entidad)
            {
                Configuracion ConfiguracionActual = db.Set<Configuracion>().AsNoTracking().Where(x => x.ConfiguracionId == 20191010015).FirstOrDefault();
                if (ConfiguracionActual != null)
                {
                    if (ConfiguracionActual.Valor.Equals("2"))
                    {
                        return GenerarDIGIFACT(entidad);
                    }
                    else
                    {
                        return "El certificador que se encuentra configurado no es valido";
                    }
                }

                return "No se encuentra ningun certificador configurado";
            }

        #region DIGIFACT

            public string GenerarDIGIFACT(Factura entidad)
            {
                string Mensaje = "OK";

                try
                {
                    //Se obtiene la factura completa
                    Factura FacturaGeneralActual = db.Set<Factura>().Include("Tipo").Include("Empresa").Include("Agencia").Include("Cliente").Include("Detalles").Include("Detalles.Producto").Include("Detalles.Unidad").Where(x => x.FacturaId == entidad.FacturaId).FirstOrDefault();
                    if (FacturaGeneralActual == null)
                    {
                        return "No existe la factura seleccionada";
                    }

                    if (FacturaGeneralActual.Empresa == null)
                    {
                        return "No existe empresa en la factura seleccionada";
                    }

                    //Se valida que contenga paquetes de facturas disponibles
                    PaqueteEmpresa PaqueteEmpresaDisponible = db.Set<PaqueteEmpresa>().Where(x => x.EmpresaId == FacturaGeneralActual.EmpresaId && x.FechaVencimiento >= DateTime.Today && x.SaldoFactura > 0).OrderBy(x => x.Fecha).ThenBy(x => x.PaqueteEmpresaId).FirstOrDefault();
                    if (PaqueteEmpresaDisponible == null)
                    {
                        return "No contiene paquete disponible de facturas";
                    }

                    if (PaqueteEmpresaDisponible.SaldoFactura == 0)
                    {
                        return "No contiene saldo disponible de facturas";
                    }

                    if (FacturaGeneralActual.Agencia == null)
                    {
                        return "No existe agencia en la factura seleccionada";
                    }                   

                    if (FacturaGeneralActual.Cliente == null)
                    {
                        return "No existe cliente en la factura seleccionada";
                    }

                    if (FacturaGeneralActual.Detalles == null)
                    {
                        return "No contiene detalle la factura seleccionada";
                    }                   

                    string CodigoEstablecimiento = FacturaGeneralActual.Agencia.CodigoEstablecimiento.Value.ToString();
                    string DireccionEstablecimiento = FacturaGeneralActual.Agencia.Direccion;

                    //Se inicia proceso de generar el XML
                    string FacturaXML = string.Empty;

                    if (!FacturaGeneralActual.Credito)
                    {
                        FacturaXML = GenerarFacturaComercialDIGIFACT(FacturaGeneralActual, CodigoEstablecimiento, DireccionEstablecimiento, FacturaGeneralActual.Empresa);
                    }
                    //else if (FacturaGeneralActual.Credito)
                    //{
                    //    FacturaXML = GenerarFacturaCambiariaDIGIFACT(FacturaGeneralActual, CodigoEstablecimiento, DireccionEstablecimiento, Configuraciones);
                    //}

                    //SE VALIDA QUE EXISTA XML
                    if (string.IsNullOrWhiteSpace(FacturaXML))
                    {
                        return "No se genero el XML";
                    }

                    //GENERAR TOKEN DIGIFACT
                    try
                    {
                        string NitEmisor = FacturaGeneralActual.Empresa.NitEmisorDIGIFACT;
                        NitEmisor = NitEmisor.PadLeft(12, '0');

                        string Usuario = string.Format(@"{0}.{1}.{2}", FacturaGeneralActual.Empresa.PaisEmisorDIGIFACT, NitEmisor, FacturaGeneralActual.Empresa.UsuarioDIGIFACT);
                        var Parametros = new Dictionary<string, string>
                                    {
                                        { "username", Usuario },
                                        { "password", FacturaGeneralActual.Empresa.PasswordDIGIFACT }
                                    };

                        //Url de generacion de token digifact
                        string UrlToken = db.Set<Configuracion>().AsNoTracking().Where(x => x.ConfiguracionId == 20191010033).Select(x => x.Valor).FirstOrDefault();

                        //ENVIAR TOKEN DIGIFACT
                        var t = Task.Run(() => PostToken(new Uri(UrlToken), new StringContent(JsonConvert.SerializeObject(Parametros), Encoding.UTF8, "application/json")));
                        t.Wait();

                        string responseString = t.Result;
                        if (!string.IsNullOrWhiteSpace(responseString))
                        {
                            DigifactToken Token = JsonConvert.DeserializeObject<DigifactToken>(responseString);
                            if (Token != null)
                            {
                                try
                                {
                                    //Url que certifica documentos en digifact
                                    string UrlCertificaDocumento = db.Set<Configuracion>().AsNoTracking().Where(x => x.ConfiguracionId == 20191010034).Select(x => x.Valor).FirstOrDefault();

                                    //ENVIAR FACTURA
                                    var tFactura = Task.Run(() => PostEnviar(new Uri(string.Format("{0}?NIT={1}&TIPO=CERTIFICATE_DTE_XML_TOSIGN&FORMAT=XML", UrlCertificaDocumento, NitEmisor)), new StringContent(FacturaXML, Encoding.UTF8, "application/json"), Token.Token));
                                    tFactura.Wait();

                                    string responseFacturaString = tFactura.Result;
                                    if (!string.IsNullOrWhiteSpace(responseFacturaString))
                                    {
                                        DigifactMensaje FacturaAutorizada = JsonConvert.DeserializeObject<DigifactMensaje>(responseFacturaString);
                                        if (FacturaAutorizada != null)
                                        {
                                            FacturaGeneralActual.CantidadErroresFEL = 0;
                                            FacturaGeneralActual.DescripcionFEL = FacturaAutorizada.Mensaje;
                                            FacturaGeneralActual.JsonFEL = responseFacturaString;

                                            if (FacturaAutorizada.Codigo.Equals("1"))
                                            {
                                                FacturaGeneralActual.Infile = true;
                                                FacturaGeneralActual.FechaHoraCertificacionFEL = FacturaAutorizada.Fecha_DTE;
                                                FacturaGeneralActual.NumeroFEL = FacturaAutorizada.NUMERO;
                                                FacturaGeneralActual.SerieFEL = FacturaAutorizada.Serie;
                                                FacturaGeneralActual.UUIDFEL = FacturaAutorizada.Autorizacion;
                                                FacturaGeneralActual.XMLCertificadoFEL = FacturaAutorizada.ResponseDATA1;

                                                PaqueteEmpresaDisponible.SaldoFactura -= 1;
                                            }
                                            else
                                            {
                                                Mensaje = "No se genero la factura, intentar de nuevo";
                                            }

                                            db.SaveChanges();

                                            if (FacturaAutorizada.Codigo.Equals("1"))
                                            {
                                                //ENVIO AUTOMATICO DE FACTURA FEL
                                                EnviarCorreo(FacturaGeneralActual.FacturaId);
                                            }                                            
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    return string.Format("No se genero la factura, descripción del error {0}", ex.Message);
                                }
                            }
                            else
                            {
                                return "No se genero el token en DIGIFACT";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        return string.Format("No se genero el token en DIGIFACT, descripción del error {0}", ex.Message);
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            public string GenerarNotaCreditoDIGIFACT(Factura entidad)
            {
                string Mensaje = "OK";

                try
                {
                    //Se obtiene la configuracion de DIGIFACT
                    List<long> ConfiguracionIDs = new List<long>() { 20200722002, 20200722001, 20200715001, 20191010002, 20191010011, 20191010016, 20191010017, 20191010018, 20191010019, 20191010020, 20191010021, 20191010022, 20191010023, 20191010024, 20191010033, 20191010034 };
                    List<Configuracion> Configuraciones = db.Set<Configuracion>().AsNoTracking().Where(x => ConfiguracionIDs.Contains(x.ConfiguracionId)).ToList();
                    if (Configuraciones != null && Configuraciones.Count() > 0)
                    {
                        //Se verifica que ninguna configuracion de DIGIFACT venga vacia.
                        foreach (Configuracion ConfiguracionActual in Configuraciones)
                        {
                            if (string.IsNullOrWhiteSpace(ConfiguracionActual.Valor))
                            {
                                return "La configuracion de DIGIFACT no es valida.";
                            }
                        }

                        //Se obtiene la factura completa
                        Factura FacturaGeneralActual = db.Set<Factura>().Include("Tipo").Include("Agencia").Include("Cliente").Where(x => x.FacturaId == entidad.FacturaId).FirstOrDefault();

                        if (FacturaGeneralActual == null)
                        {
                            return "No existe la factura seleccionada";
                        }

                        if (FacturaGeneralActual.Agencia == null)
                        {
                            return "No existe agencia en la factura seleccionada";
                        }

                        if (FacturaGeneralActual.Cliente == null)
                        {
                            return "No existe cliente en la factura seleccionada";
                        }

                        //Se obtiene la factura completa
                        FacturaNotaCredito FacturaNotaCreditoActual = db.Set<FacturaNotaCredito>().Include("Detalles").Include("Detalles.Producto").Include("Detalles.Unidad").Where(x => x.FacturaId == entidad.FacturaId).FirstOrDefault();

                        if (FacturaNotaCreditoActual == null)
                        {
                            return "No existe la factura seleccionada";
                        }

                        if (FacturaNotaCreditoActual.Detalles == null)
                        {
                            return "No contiene detalle la nota de credito seleccionada";
                        }
                        
                        string CodigoEstablecimiento = string.Empty;

                        if (string.IsNullOrWhiteSpace(CodigoEstablecimiento))
                        {
                            CodigoEstablecimiento = Configuraciones.Where(x => x.ConfiguracionId == 20191010002).Select(x => x.Valor).FirstOrDefault();
                        }

                        //Se inicia proceso de generar el XML
                        string NotaCreditoXML = string.Empty;

                        NotaCreditoXML = GenerarNotaCreditoDIGIFACT(FacturaGeneralActual, FacturaNotaCreditoActual, CodigoEstablecimiento, Configuraciones);

                        //SE VALIDA QUE EXISTA XML
                        if (string.IsNullOrWhiteSpace(NotaCreditoXML))
                        {
                            return "No se genero el XML";
                        }

                        //GENERAR TOKEN DIGIFACT
                        try
                        {
                            string NitEmisor = Configuraciones.Where(x => x.ConfiguracionId == 20191010016).Select(x => x.Valor).FirstOrDefault();
                            NitEmisor = NitEmisor.PadLeft(12, '0');

                            string Usuario = string.Format(@"{0}.{1}.{2}", Configuraciones.Where(x => x.ConfiguracionId == 20191010011).Select(x => x.Valor).FirstOrDefault(), NitEmisor, Configuraciones.Where(x => x.ConfiguracionId == 20191010023).Select(x => x.Valor).FirstOrDefault());
                            var Parametros = new Dictionary<string, string>
                                    {
                                        { "username", Usuario },
                                        { "password", Configuraciones.Where(x => x.ConfiguracionId == 20191010024).Select(x => x.Valor).FirstOrDefault() }
                                    };

                            //Url de generacion de token digifact
                            string UrlToken = Configuraciones.Where(x => x.ConfiguracionId == 20191010033).Select(x => x.Valor).FirstOrDefault();

                            //ENVIAR TOKEN DIGIFACT
                            var t = Task.Run(() => PostToken(new Uri(UrlToken), new StringContent(JsonConvert.SerializeObject(Parametros), Encoding.UTF8, "application/json")));
                            t.Wait();

                            string responseString = t.Result;
                            if (!string.IsNullOrWhiteSpace(responseString))
                            {
                                DigifactToken Token = Newtonsoft.Json.JsonConvert.DeserializeObject<DigifactToken>(responseString);
                                if (Token != null)
                                {
                                    try
                                    {
                                        //Url que certifica documentos en digifact
                                        string UrlCertificaDocumento = Configuraciones.Where(x => x.ConfiguracionId == 20191010034).Select(x => x.Valor).FirstOrDefault();

                                        //ENVIAR FACTURA
                                        var tFactura = Task.Run(() => PostEnviar(new Uri(string.Format("{0}?NIT={1}&TIPO=CERTIFICATE_DTE_XML_TOSIGN&FORMAT=XML", UrlCertificaDocumento, NitEmisor)), new StringContent(NotaCreditoXML, Encoding.UTF8, "application/json"), Token.Token));
                                        tFactura.Wait();

                                        string responseFacturaString = tFactura.Result;
                                        if (!string.IsNullOrWhiteSpace(responseFacturaString))
                                        {
                                            DigifactMensaje FacturaAutorizada = Newtonsoft.Json.JsonConvert.DeserializeObject<DigifactMensaje>(responseFacturaString);
                                            if (FacturaAutorizada != null)
                                            {
                                                FacturaNotaCreditoActual.CantidadErroresFEL = 0;
                                                FacturaNotaCreditoActual.DescripcionFEL = FacturaAutorizada.Mensaje;
                                                FacturaNotaCreditoActual.JsonFEL = responseFacturaString;

                                                if (FacturaAutorizada.Codigo.Equals("1"))
                                                {
                                                    FacturaNotaCreditoActual.Infile = true;
                                                    FacturaNotaCreditoActual.FechaHoraCertificacionFEL = FacturaAutorizada.Fecha_DTE;
                                                    FacturaNotaCreditoActual.NumeroFEL = FacturaAutorizada.NUMERO;
                                                    FacturaNotaCreditoActual.SerieFEL = FacturaAutorizada.Serie;
                                                    FacturaNotaCreditoActual.UUIDFEL = FacturaAutorizada.Autorizacion;
                                                    FacturaNotaCreditoActual.XMLCertificadoFEL = FacturaAutorizada.ResponseDATA1;
                                                }
                                                else
                                                {
                                                    Mensaje = "No se genero la factura, intentar de nuevo";
                                                }

                                                db.SaveChanges();
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        return string.Format("No se genero la factura, descripción del error {0}", ex.Message);
                                    }
                                }
                                else
                                {
                                    return "No se genero el token en DIGIFACT";
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            return string.Format("No se genero el token en DIGIFACT, descripción del error {0}", ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            public string GenerarAnulacionDIGIFACT(Factura entidad, string comentarioAnulacion)
            {
                string Mensaje = "OK";

                try
                {
                    //Se obtiene la factura completa
                    Factura FacturaGeneralActual = db.Set<Factura>().Include("Tipo").Include("Empresa").Include("Agencia").Include("Cliente").Where(x => x.FacturaId == entidad.FacturaId).FirstOrDefault();
                    if (FacturaGeneralActual == null)
                    {
                        return "No existe la factura seleccionada";
                    }

                    if (FacturaGeneralActual.Empresa == null)
                    {
                        return "No existe empresa en la factura seleccionada";
                    }

                    if (FacturaGeneralActual.Agencia == null)
                    {
                        return "No existe agencia en la factura seleccionada";
                    }

                    if (FacturaGeneralActual.Cliente == null)
                    {
                        return "No existe cliente en la factura seleccionada";
                    }

                    //Se inicia proceso de generar el XML
                    string AnularXML = string.Empty;

                    AnularXML = GenerarAnulacionFacturaDIGIFACT(FacturaGeneralActual, comentarioAnulacion, FacturaGeneralActual.Empresa);

                    //SE VALIDA QUE EXISTA XML
                    if (string.IsNullOrWhiteSpace(AnularXML))
                    {
                        return "No se genero el XML";
                    }

                    //GENERAR TOKEN DIGIFACT
                    try
                    {
                        string NitEmisor = FacturaGeneralActual.Empresa.NitEmisorDIGIFACT;
                        NitEmisor = NitEmisor.PadLeft(12, '0');

                        string Usuario = string.Format(@"{0}.{1}.{2}", FacturaGeneralActual.Empresa.PaisEmisorDIGIFACT, NitEmisor, FacturaGeneralActual.Empresa.UsuarioDIGIFACT);
                        var Parametros = new Dictionary<string, string>
                                        {
                                            { "username", Usuario },
                                            { "password", FacturaGeneralActual.Empresa.PasswordDIGIFACT }
                                        };

                        //Url de generacion de token digifact
                        string UrlToken = db.Set<Configuracion>().AsNoTracking().Where(x => x.ConfiguracionId == 20191010033).Select(x => x.Valor).FirstOrDefault();

                        //ENVIAR TOKEN DIGIFACT
                        var t = Task.Run(() => PostToken(new Uri(UrlToken), new StringContent(JsonConvert.SerializeObject(Parametros), Encoding.UTF8, "application/json")));
                        t.Wait();

                        string responseString = t.Result;
                        if (!string.IsNullOrWhiteSpace(responseString))
                        {
                            DigifactToken Token = Newtonsoft.Json.JsonConvert.DeserializeObject<DigifactToken>(responseString);
                            if (Token != null)
                            {
                                try
                                {
                                    //Url que certifica documentos en digifact
                                    string UrlCertificaDocumento = db.Set<Configuracion>().AsNoTracking().Where(x => x.ConfiguracionId == 20191010034).Select(x => x.Valor).FirstOrDefault();

                                    //ENVIAR ANULACION
                                    var tFactura = Task.Run(() => PostEnviar(new Uri(string.Format("{0}?NIT={1}&TIPO=ANULAR_FEL_TOSIGN&FORMAT=XML", UrlCertificaDocumento, NitEmisor)), new StringContent(AnularXML, Encoding.UTF8, "application/json"), Token.Token));
                                    tFactura.Wait();

                                    string responseFacturaString = tFactura.Result;
                                    if (!string.IsNullOrWhiteSpace(responseFacturaString))
                                    {
                                        DigifactMensaje AnulacionAutorizada = JsonConvert.DeserializeObject<DigifactMensaje>(responseFacturaString);
                                        if (AnulacionAutorizada != null)
                                        {
                                            FacturaGeneralActual.DescripcionAnularFEL = AnulacionAutorizada.Mensaje;
                                            FacturaGeneralActual.JsonAnularFEL = responseFacturaString;

                                            if (AnulacionAutorizada.Codigo.Equals("1"))
                                            {
                                                FacturaGeneralActual.FechaHoraCertificacionAnularFEL = AnulacionAutorizada.Fecha_DTE;
                                                FacturaGeneralActual.XMLCertificadoAnularFEL = AnulacionAutorizada.ResponseDATA1;
                                            }
                                            else
                                            {
                                                Mensaje = "No se genero la anulacion de la factura, intentar de nuevo";
                                            }

                                            db.SaveChanges();
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    return string.Format("No se genero la factura, descripción del error {0}", ex.Message);
                                }
                            }
                            else
                            {
                                return "No se genero el token en DIGIFACT";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        return string.Format("No se genero el token en DIGIFACT, descripción del error {0}", ex.Message);
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            public RESPONSE ObtenerCliente(string nit)
            {
                RESPONSE ClienteActual = new RESPONSE();

                try
                {
                    List<long> ConfiguracionIds = new List<long>() { 20191010011, 20191010016, 20191010023, 20191010024 };
                    List<Configuracion> Configuraciones = db.Set<Configuracion>().AsNoTracking().Where(x => ConfiguracionIds.Contains(x.ConfiguracionId)).ToList();

                    if (Configuraciones != null && Configuraciones.Count() > 0)
                    {
                        nit = nit.Replace("-", "");

                        string NitEmisor = Configuraciones.Where(x => x.ConfiguracionId == 20191010016).Select(x => x.Valor).FirstOrDefault();
                        NitEmisor = NitEmisor.PadLeft(12, '0');

                        string Usuario = string.Format(@"{0}.{1}.{2}", Configuraciones.Where(x => x.ConfiguracionId == 20191010011).Select(x => x.Valor).FirstOrDefault(), NitEmisor, Configuraciones.Where(x => x.ConfiguracionId == 20191010023).Select(x => x.Valor).FirstOrDefault());
                        var Parametros = new Dictionary<string, string>
                                            {
                                                { "username", Usuario },
                                                { "password", Configuraciones.Where(x => x.ConfiguracionId == 20191010024).Select(x => x.Valor).FirstOrDefault() }
                                            };

                        //Url de generacion de token digifact
                        //Url Desarrollo
                        //string UrlToken = "https://felgttestaws.digifact.com.gt/felapiv2/api/login/get_token";

                        //Url Produccion
                        string UrlToken = "https://felgtaws.digifact.com.gt/gt.com.fel.api.v2/api/login/get_token";


                        //ENVIAR TOKEN DIGIFACT
                        var t = Task.Run(() => PostToken(new Uri(UrlToken), new StringContent(JsonConvert.SerializeObject(Parametros), Encoding.UTF8, "application/json")));
                        t.Wait();

                        string responseString = t.Result;
                        if (!string.IsNullOrWhiteSpace(responseString))
                        {
                            DigifactToken Token = JsonConvert.DeserializeObject<DigifactToken>(responseString);
                            if (Token != null)
                            {
                                //Url obtener datos de cliente en digifact
                                //Url Desarrollo
                                //string UrlClienteDigifact = string.Format(@"https://felgttestaws.digifact.com.gt/felapiv2/api/sharedInfo?NIT={0}&DATA1=SHARED_GETINFONITcom&DATA2=NIT|{1}&USERNAME={2}", NitEmisor, nit, Configuraciones.Where(x => x.ConfiguracionId == 20191010023).Select(x => x.Valor).FirstOrDefault());

                                //Url Produccion
                                string UrlClienteDigifact = string.Format(@"https://felgtaws.digifact.com.gt/gt.com.fel.api.v2/api/sharedInfo?NIT={0}&DATA1=SHARED_GETINFONITcom&DATA2=NIT|{1}&USERNAME={2}", NitEmisor, nit, Configuraciones.Where(x => x.ConfiguracionId == 20191010023).Select(x => x.Valor).FirstOrDefault());

                                //ENVIAR CLIENTE
                                var tCliente = Task.Run(() => GetEnviar(new Uri(UrlClienteDigifact), Token.Token));
                                tCliente.Wait();

                                string responseClienteString = tCliente.Result;
                                if (!string.IsNullOrWhiteSpace(responseClienteString))
                                {
                                    ClienteDigifact Cliente = Newtonsoft.Json.JsonConvert.DeserializeObject<ClienteDigifact>(responseClienteString);
                                    if (Cliente != null)
                                    {
                                        if (Cliente.RESPONSE != null && Cliente.RESPONSE.Count() > 0)
                                        {
                                            ClienteActual = Cliente.RESPONSE[0];
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                { }

                return ClienteActual;
            }

        #endregion

        public string GuardarLote(Factura entidad)
            {
                string Mensaje = "OK";

                try
                {
                    Factura FacturaActual = db.Set<Factura>().Where(x => x.FacturaId == entidad.FacturaId).FirstOrDefault();
                    if (FacturaActual != null)
                    {
                        FacturaActual.ProductoLote = false;

                        if (entidad.Lotes != null && entidad.Lotes.Count() > 0)
                        {
                            int DetalleId = 1;
                            foreach (var LoteActual in entidad.Lotes)
                            {
                                LoteActual.DetalleId = DetalleId;
                                LoteActual.FacturaId = entidad.FacturaId;

                                //Se obtiene el lote actual
                                ProductoLote LoteProductoActual = db.Set<ProductoLote>().Where(x => x.ProductoId == LoteActual.ProductoId && x.AgenciaId == FacturaActual.AgenciaId && x.Lote == LoteActual.Lote).FirstOrDefault();
                                if (LoteProductoActual != null)
                                {
                                    if (LoteActual.Cantidad > LoteProductoActual.Cantidad)
                                    {
                                        return string.Format("Se le informa que la cantidad solicitada es mayor a la existencia que contiene el #lote: {0}", LoteActual.Lote);
                                    }
                                    else
                                    {
                                        LoteActual.FechaVencimiento = LoteProductoActual.FechaVencimiento;
                                        LoteProductoActual.Cantidad -= LoteActual.Cantidad;
                                    }
                                }
                                else
                                {
                                    return "Se le informa que el #lote ingresado no se encuentra registrado en el sistema";
                                }

                                db.Set<FacturaLote>().Add(LoteActual);
                                DetalleId++;
                            }

                            db.SaveChanges();
                        }
                        else
                        {
                            return "Se le informa que la factura ingresada no contiene lotes asignados";
                        }
                    }
                    else
                    {
                        return "Se le informa que la factura seleccionada no se encuentra registrada en el sistema";
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            public string GenerarPago(Factura entidad, long usuarioId, List<FacturaFormaPago> pagos)
            {
                string Mensaje = "OK";

                try
                {
                    int Id = 1;

                    if (pagos != null && pagos.Count() > 0)
                    {
                        FacturaFormaPago PagoUltimo = db.Set<FacturaFormaPago>().AsNoTracking().Where(x => x.FacturaId == entidad.FacturaId).OrderByDescending(x => x.DetalleId).FirstOrDefault();
                        if (PagoUltimo != null)
                        {
                            Id = PagoUltimo.DetalleId + 1;
                        }

                        foreach (var pago in pagos)
                        {
                            pago.DetalleId = Id;
                            pago.Fecha = DateTime.Today;
                            Id++;

                            db.Set<FacturaFormaPago>().Add(pago);
                        }

                        decimal TotalFactura = db.Set<FacturaDetalle>().AsNoTracking().Where(x => x.FacturaId == entidad.FacturaId).Sum(x => x.Cantidad * x.Precio);
                        decimal TotalPago = 0;

                        var TotalPagos = db.Set<FacturaFormaPago>().Where(x => x.FacturaId == entidad.FacturaId).ToList();
                        if (TotalPagos != null && TotalPagos.Count() > 0)
                        {
                            TotalPago = TotalPagos.Sum(x => x.Valor);
                        }

                        TotalPago += pagos.Sum(x => x.Valor);

                        if (TotalPago == TotalFactura)
                        {
                            Factura FacturaActual = db.Set<Factura>().Where(x => x.FacturaId == entidad.FacturaId).FirstOrDefault();

                            if (FacturaActual != null)
                            {
                                FacturaActual.Pagada = true;
                            }
                        }
                        else if (TotalPago > TotalFactura)
                        {
                            return "Se le informa que el monto que ingreso es mayor a la deuda";
                        }

                        db.SaveChanges();
                    }

                }
                catch (Exception)
                {
                    Mensaje = "La información ingresada no es valida";
                }

                return Mensaje;
            }

            public string GenerarPago(long[] facturaIDs, decimal[] saldoIDs, long usuarioId)
            {
                string Mensaje = "OK";

                try
                {
                    if (facturaIDs != null && facturaIDs.Count() > 0 && saldoIDs != null && saldoIDs.Count() > 0)
                    {
                        for (int i = 0; i < facturaIDs.Length; i++)
                        {
                            long FacturaActualId = facturaIDs[i];
                            decimal SaldoActual = saldoIDs[i];

                            if (FacturaActualId > 0)
                            {
                                int Id = 1;

                                FacturaFormaPago PagoUltimo = db.Set<FacturaFormaPago>().AsNoTracking().Where(x => x.FacturaId == FacturaActualId).OrderByDescending(x => x.DetalleId).FirstOrDefault();
                                if (PagoUltimo != null)
                                {
                                    Id = PagoUltimo.DetalleId + 1;
                                }

                                FacturaFormaPago PagoActual = new FacturaFormaPago();
                                PagoActual.DetalleId = Id;
                                PagoActual.FacturaId = FacturaActualId;
                                PagoActual.FormaPagoId = 20171028001;
                                PagoActual.Valor = SaldoActual;
                                PagoActual.Fecha = DateTime.Today;
                                PagoActual.UsrOperacionId = usuarioId;

                                db.Set<FacturaFormaPago>().Add(PagoActual);

                                decimal TotalFactura = db.Set<FacturaDetalle>().AsNoTracking().Where(x => x.FacturaId == FacturaActualId).Sum(x => x.Cantidad * x.Precio);
                                decimal TotalPago = 0;

                                var TotalPagos = db.Set<FacturaFormaPago>().AsNoTracking().Where(x => x.FacturaId == FacturaActualId).ToList();
                                if (TotalPagos != null && TotalPagos.Count() > 0)
                                {
                                    TotalPago = TotalPagos.Sum(x => x.Valor);
                                }

                                TotalPago += SaldoActual;

                                if (TotalPago == TotalFactura)
                                {
                                    Factura FacturaActual = db.Set<Factura>().Where(x => x.FacturaId == FacturaActualId).FirstOrDefault();

                                    if (FacturaActual != null)
                                    {
                                        FacturaActual.Pagada = true;
                                    }
                                }
                                else if (TotalPago > TotalFactura)
                                {
                                    return "Se le informa que el monto que ingreso es mayor a la deuda";
                                }

                                db.SaveChanges();
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    Mensaje = "La información ingresada no es valida";
                }

                return Mensaje;
            }

            public string AsignarTransporte(long facturaId, long transporteId)
            {
                string Mensaje = "OK";

                try
                {
                    Factura FacturaActual = db.Set<Factura>().Where(x => x.FacturaId == facturaId).FirstOrDefault();
                    if (FacturaActual == null)
                    {
                        return "La factura que selecciono no se encuentra disponible";
                    }

                    FacturaActual.TransporteId = transporteId;

                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            public string Despachar(long id, long usuarioId)
            {
                string Mensaje = "OK";

                try
                {
                    Factura FacturaActual = db.Set<Factura>().Where(x => x.FacturaId == id).FirstOrDefault();
                    if (FacturaActual != null)
                    {
                        FacturaActual.Despachado = true;
                        FacturaActual.UsrDespacho = usuarioId;
                        FacturaActual.FechaHoraDespacho = DateTime.Now;

                        db.SaveChanges();
                    }
                    else
                    {
                        Mensaje = "El recibo no se encuentra disponible";
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            public string Pagar(long id)
            {
                string Mensaje = "OK";

                try
                {
                    Factura FacturaActual = db.Set<Factura>().Where(x => x.FacturaId == id).FirstOrDefault();
                    if (FacturaActual != null)
                    {
                        FacturaActual.Pagada = true;
                        db.SaveChanges();
                    }
                    else
                    {
                        Mensaje = "La factura no se encuentra disponible";
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            public Factura ObtenerPorId(long id, bool todo, bool factura, bool electronica, bool totalizar = false)
            {
                Factura FacturaActual = new Factura();

                try
                {
                    if (todo)
                    {
                        if (factura)
                        {
                            FacturaActual = db.Set<Factura>().Include("Tipo").Include("Serie").Include("Agencia").Include("Cliente").Include("Detalles").Include("Detalles.Producto").Include("Detalles.Unidad").Include("Lotes").Include("Lotes.Producto").Include("Pagos").Include("Pagos.FormaPago").Include("Pagos.UsuarioOperacion").Where(x => x.FacturaId == id).FirstOrDefault();
                            if (totalizar)
                            {
                                if (FacturaActual != null)
                                {
                                    FacturaActual.DescuentoTotal = FacturaActual.Descuento == 0 ? 0 : (Convert.ToDecimal(FacturaActual.Descuento) / Convert.ToDecimal(100) * FacturaActual.Detalles.Sum(x => x.Cantidad * x.Precio));
                                    FacturaActual.Total = FacturaActual.Detalles.Sum(x => x.Cantidad * x.Precio) - FacturaActual.DescuentoTotal;
                                }
                            }

                            //Se valida que exista informacion sobre la factura y se agrega el numero de documento(Factura) y las formas de pago como fue pagada la factura
                            if (FacturaActual != null)
                            {
                                FacturaActual.Documento = string.Format("{0} - {1}", FacturaActual.Serie.Nombre, FacturaActual.NoFactura);

                                if (FacturaActual.Pagos != null && FacturaActual.Pagos.Count() > 0) 
                                {
                                    foreach (var Pago in FacturaActual.Pagos)
                                    {
                                        FacturaActual.FormaPago += string.Format("{0} - {1:C},", Pago.FormaPago.Nombre, Pago.Valor);                                       
                                    }

                                    if (!string.IsNullOrWhiteSpace(FacturaActual.FormaPago))
                                    {
                                        FacturaActual.FormaPago = FacturaActual.FormaPago.Substring(0, FacturaActual.FormaPago.Length - 1);
                                        FacturaActual.FormaPago = FacturaActual.FormaPago.ToUpper();
                                    }
                                }
                            }
                        }
                        else
                        {
                            FacturaActual = db.Set<Factura>().Include("Tipo").Include("Serie").Include("Agencia").Include("Cliente").Include("Detalles").Include("Detalles.Producto").Include("Detalles.Unidad").Include("Pagos").Include("Pagos.FormaPago").Where(x => x.FacturaId == id && x.FacturaElectronica == electronica).FirstOrDefault();                        
                        }                       
                    }
                    else 
                    {
                        FacturaActual = db.Set<Factura>().Where(x => x.FacturaId == id).FirstOrDefault();                    
                    }
                   
                }
                catch (Exception)
                {
                }

                return FacturaActual;
            }

            public List<Factura> BuscarFactura(long? serie, string factura, long agenciaId)
            {
                List<Factura> Facturas = new List<Factura>();

                try
                {
                    long NoFacturaActual = 0;
                    bool EsNumero = long.TryParse(factura, out NoFacturaActual);
                    if (EsNumero)
                    {
                        if (serie == null)
                        {
                            Facturas = db.Set<Factura>().Include("Tipo").Include("Serie").Include("Agencia").Include("Cliente").Include("Detalles").Where(x => x.NoFactura == NoFacturaActual && x.AgenciaId == agenciaId).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.FacturaId).ToList();
                        }
                        else
                        {
                            Facturas = db.Set<Factura>().Include("Tipo").Include("Serie").Include("Agencia").Include("Cliente").Include("Detalles").Where(x => x.SerieId == serie.Value && x.NoFactura == NoFacturaActual && x.AgenciaId == agenciaId).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.FacturaId).ToList();
                        }
                    }

                    if (Facturas != null && Facturas.Count() > 0)
                    {
                        Facturas.ForEach(x => 
                        {
                            x.NotaCredito = false;
                            x.NotaCredito = db.Set<FacturaNotaCredito>().AsNoTracking().Where(y => y.FacturaId == x.FacturaId).Count() > 0;
                        });
                    }
                }
                catch (Exception)
                {}

                return Facturas;
            }

            public List<Factura> BuscarNoPagadas(string search, long agenciaId)
            {
                List<Factura> Facturas = new List<Factura>();
                long FacturaId = 0;

                try
                {
                    long.TryParse(search, out FacturaId);

                    if (FacturaId > 0)
                    {
                        Facturas = db.Set<Factura>().Include("Tipo").Include("Agencia").Include("Vendedor").Include("Cliente").Include("Detalles").Include("Pagos").AsNoTracking().Where(x => x.FacturaId == FacturaId && x.AgenciaId == agenciaId && !x.Anulada && !x.Pagada && x.Despachado).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.FacturaId).ToList();
                    }
                    else
                    {
                        Facturas = db.Set<Factura>().Include("Tipo").Include("Agencia").Include("Vendedor").Include("Cliente").Include("Detalles").Include("Pagos").AsNoTracking().Where(x => (x.Agencia.Nombre.ToLower().Contains(search.ToLower()) || x.Cliente.Nombre.ToLower().Contains(search.ToLower())) && x.AgenciaId == agenciaId && !x.Anulada && !x.Pagada && x.Despachado).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.FacturaId).ToList();
                    }

                    if (Facturas != null && Facturas.Count() > 0)
                    {
                        Facturas.ForEach(x =>
                        {
                            if (x.Pagos != null && x.Pagos.Count() > 0)
                            {
                                x.Abono = x.Pagos.Sum(y => y.Valor);
                            }
                        });
                    }
                }
                catch (Exception)
                {
                }

                return Facturas;
            }

            public List<Factura> BuscarNoPagadasxCliente(long clienteId, long agenciaId)
            {
                List<Factura> Facturas = new List<Factura>();

                try
                {
                    Facturas = db.Set<Factura>().Include("Tipo").Include("Agencia").Include("Vendedor").Include("Cliente").Include("Detalles").Include("Pagos").AsNoTracking().Where(x => x.ClienteId == clienteId && x.AgenciaId == agenciaId && !x.Anulada && !x.Pagada && x.Despachado).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.FacturaId).ToList();

                    if (Facturas != null && Facturas.Count() > 0)
                    {
                        Facturas.ForEach(x =>
                        {
                            x.HabilitarCheck = true;

                            if (x.Pagos != null && x.Pagos.Count() > 0)
                            {
                                x.Abono = x.Pagos.Sum(y => y.Valor);
                            }
                        });
                    }
                }
                catch (Exception)
                {
                }

                return Facturas;
            }

            public List<Factura> ObtenerListadoPorFecha(DateTime fechaInicial, DateTime fechaFinal, long agenciaId)
            {
                List<Factura> Facturas = new List<Factura>();

                try
                {
                    Facturas = db.Set<Factura>().Include("Tipo").Include("Serie").Include("Agencia").Include("Cliente").Include("Detalles").Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && x.AgenciaId == agenciaId).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.FacturaId).ToList();

                    if (Facturas != null && Facturas.Count() > 0)
                    {
                        Facturas.ForEach(x =>
                        {
                            x.NotaCredito = false;
                            x.NotaCredito = db.Set<FacturaNotaCredito>().AsNoTracking().Where(y => y.FacturaId == x.FacturaId).Count() > 0;
                        });
                    }
                }
                catch (Exception)
                {}

                return Facturas;
            }

            public List<Factura> ObtenerListadoNoPagadas(long usuarioId)
            {
                List<Factura> Facturas = new List<Factura>();

                try
                {
                    var AgenciasIds = db.Set<UsuarioAgencia>().Where(x => x.UsuarioId == usuarioId).AsEnumerable().Select(x => x.AgenciaId).ToList();
                    if (AgenciasIds != null && AgenciasIds.Count() > 0)
                    {
                        Facturas = db.Set<Factura>().Include("Tipo").Include("Serie").Include("Agencia").Include("Cliente").Include("Detalles").Where(x => !x.Pagada && AgenciasIds.Contains(x.AgenciaId)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.FacturaId).ToList();
                    }
                }
                catch (Exception)
                {
                }

                return Facturas;
            }

            public List<ClienteNoPagadoModel> ObtenerClienteNoPagadas(long agenciaId)
            {
                List<ClienteNoPagadoModel> Clientes = new List<ClienteNoPagadoModel>();
                List<Factura> Facturas = new List<Factura>();

                try
                {
                    Facturas = db.Set<Factura>().Include("Detalles").Include("Pagos").AsNoTracking().Where(x => !x.Anulada && !x.Pagada && x.Despachado && x.AgenciaId == agenciaId).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.FacturaId).ToList();
                    if (Facturas != null && Facturas.Count() > 0)
                    {
                        Facturas.ForEach(x =>
                        {
                            if (x.Pagos != null && x.Pagos.Count() > 0)
                            {
                                x.Abono = x.Pagos.Sum(y => y.Valor);
                            }
                        });


                        Clientes = Facturas.GroupBy(x => new { x.ClienteId }).Select(x => new ClienteNoPagadoModel() { ClienteId = x.Key.ClienteId, Monto = x.Sum(y => y.Detalles.Sum(z => z.Cantidad * z.Precio)) - x.Sum(y => y.Abono) }).ToList();
                        if (Clientes != null && Clientes.Count() > 0)
                        {
                            Clientes = Clientes.AsEnumerable().Join(db.Set<Cliente>().AsNoTracking(), C => C.ClienteId, CC => CC.ClienteId, (C, CC) => new ClienteNoPagadoModel() { ClienteId = C.ClienteId, Nombre = string.Format("{0} - {1:C4}", CC.Nombre, C.Monto), Monto = C.Monto }).OrderByDescending(x => x.Monto).ToList();
                        }
                    }
                }
                catch (Exception)
                {
                }

                return Clientes;
            }

            public List<Factura> ObtenerListadoSinDespachar(long agenciaId)
            {
                List<Factura> Facturas = new List<Factura>();

                try
                {
                    Facturas = db.Set<Factura>().Include("Tipo").Include("Serie").Include("Agencia").Include("Vendedor").Include("Cliente").Include("Detalles").Include("Lotes").AsNoTracking().Where(x => !x.Anulada && !x.Despachado && x.AgenciaId == agenciaId).OrderBy(x => x.Fecha).ThenBy(x => x.FacturaId).ToList();
                }
                catch (Exception)
                {
                }

                return Facturas;
            }

            public string Anular(long facturaId, string comentario, long usuarioId)
            {
                string Mensaje = "OK";
                bool FacturaElectronica = false;

                try
                {
                    //Se obtiene la configuracion para validar que este habilitada la opcion de factura electronica
                    Configuracion ConfiguracionFacturaElectronica = db.Set<Configuracion>().AsNoTracking().Where(x => x.ConfiguracionId == 20191010001).FirstOrDefault();
                    if (ConfiguracionFacturaElectronica != null)
                    {
                        int Configuracion = 0;
                        int.TryParse(ConfiguracionFacturaElectronica.Valor, out Configuracion);
                        if (Configuracion == 1)
                        {
                            FacturaElectronica = true;
                        }
                    }

                    //Se verifica que tenga habilitada la opcion de factura electronica en la configuracion
                    if (FacturaElectronica)
                    {
                        Configuracion ConfiguracionActual = db.Set<Configuracion>().AsNoTracking().Where(x => x.ConfiguracionId == 20191010015).FirstOrDefault();
                        if (ConfiguracionActual != null)
                        {
                            if (ConfiguracionActual.Valor.Equals("2"))
                            {
                                Mensaje = GenerarAnulacionDIGIFACT(new Factura() { FacturaId = facturaId }, comentario);
                            }
                            else
                            {
                                return "El certificador que se encuentra configurado no es valido";
                            }
                        }
                    }

                    if (Mensaje.Equals("OK"))
                    {
                        Factura FacturaActual = db.Set<Factura>().Include("Tipo").Include("Cliente").Include("Detalles").Include("Detalles.Producto").Include("Detalles.Unidad").Include("Lotes").Where(x => x.FacturaId == facturaId).FirstOrDefault();
                        if (FacturaActual == null)
                        {
                            return "La factura que selecciono no se encuentra disponible";
                        }

                        FacturaActual.Comentario = comentario;
                        FacturaActual.Anulada = true;
                        FacturaActual.UsrAnular = usuarioId;
                        FacturaActual.FechaAnular = DateTime.Now;

                        //Se verifica que no tenga algun recibo relacionado
                        bool ReciboAnulado = false;

                        if (FacturaActual.ReciboId != null)
                        {
                            Recibo ReciboActual = db.Set<Recibo>().AsNoTracking().Where(x => x.ReciboId == FacturaActual.ReciboId.Value).FirstOrDefault();
                            if (ReciboActual != null)
                            {
                                ReciboAnulado = ReciboActual.Anulada;
                            }
                        }

                        if (!ReciboAnulado)
                        {
                            foreach (var Producto in FacturaActual.Detalles)
                            {
                                //Se obtiene el producto para convercion
                                Producto ProductoPadreActual = new Producto();
                                Producto ProductoHijoActual = new Producto();
                                bool UnidadPadre = false;
                                decimal Cantidad = Producto.Cantidad;

                                decimal KardexPrecio = Producto.Precio;
                                decimal KardexExistenciaActual = 0;
                                decimal KardexExistenciaFinal = 0;

                                ProductoPadreActual = db.Set<Producto>().Where(x => x.ProductoId == Producto.ProductoId).FirstOrDefault();

                                if (ProductoPadreActual != null)
                                {
                                    if (ProductoPadreActual.UnidadId == Producto.UnidadId)
                                    {
                                        UnidadPadre = true;
                                    }
                                }

                                if (!UnidadPadre)
                                {
                                    ProductoHijoActual = db.Set<Producto>().Where(x => x.ProductoPadreId == Producto.ProductoId && x.UnidadId == Producto.UnidadId).FirstOrDefault();

                                    if (ProductoHijoActual != null)
                                    {
                                        Cantidad *= ProductoHijoActual.Cantidad;
                                    }
                                }

                                ProductoInventario InventarioActual = db.Set<ProductoInventario>().Where(x => x.ProductoId == Producto.ProductoId && x.AgenciaId == FacturaActual.AgenciaId).FirstOrDefault();
                                if (InventarioActual != null)
                                {
                                    KardexExistenciaActual = InventarioActual.Cantidad;
                                    KardexExistenciaFinal = InventarioActual.Cantidad + Cantidad;

                                    InventarioActual.Cantidad += Cantidad;
                                }

                                if (!string.IsNullOrWhiteSpace(Producto.ID))
                                {
                                    ProductoInventarioID InventarioIDActual = db.Set<ProductoInventarioID>().Where(x => x.ProductoId == Producto.ProductoId && x.ID.Equals(Producto.ID)).FirstOrDefault();
                                    if (InventarioIDActual != null)
                                    {
                                        InventarioIDActual.Operado = false;
                                    }
                                }

                                //Se agrega la informacion al Kardex
                                db.Set<KardexMovimiento>().Add(new KardexMovimiento() { AgenciaId = FacturaActual.AgenciaId, TipoId = 11, Fecha = DateTime.Today, FechaHora = DateTime.Now, ProductoId = Producto.ProductoId, UnidadId = Producto.UnidadId, DocumentoId = FacturaActual.FacturaId, Cantidad = Producto.Cantidad, Precio = KardexPrecio, ExistenciaActual = KardexExistenciaActual, ExistenciaFinal = KardexExistenciaFinal, ResponsableId = FacturaActual.UsrAnular.Value });
                            }

                            if (FacturaActual.Lotes != null && FacturaActual.Lotes.Count() > 0)
                            {
                                foreach (FacturaLote LoteActual in FacturaActual.Lotes)
                                {
                                    ProductoLote ProductoLoteActual = db.Set<ProductoLote>().Where(x => x.ProductoId == LoteActual.ProductoId && x.AgenciaId == FacturaActual.AgenciaId && x.Lote == LoteActual.Lote).FirstOrDefault();
                                    if (ProductoLoteActual != null)
                                    {
                                        ProductoLoteActual.Cantidad += LoteActual.Cantidad;
                                    }
                                }
                            }

                            Recibo ReciboFinalActual = db.Set<Recibo>().Where(x => x.ReciboId == FacturaActual.ReciboId.Value).FirstOrDefault();
                            if (ReciboFinalActual != null)
                            {
                                ReciboFinalActual.Anulada = true;
                            }
                        } 

                        db.SaveChanges();
                    }                                   
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            public List<FacturaModel> ObtenerFactura(DateTime fechaInicial, DateTime fechaFinal, long agenciaId, long usuarioId)
            {
              List<FacturaModel> Facturas = new List<FacturaModel>();
                List<FacturaModel> Recibos = new List<FacturaModel>();
               // List<FacturaModel> Reparaciones = new List<FacturaModel>();     
                List<FacturaModel> Egresos = new List<FacturaModel>();
               // List<FacturaModel> Reservas = new List<FacturaModel>();     
                List<long> AgenciaIds = new List<long>();

                try
                {
                    if (agenciaId == 0)
                    {
                        AgenciaIds = db.Set<UsuarioAgencia>().Where(x => x.UsuarioId == usuarioId).Select(x => x.AgenciaId).ToList();
                    }
                    else
                    {
                        AgenciaIds.Add(agenciaId);
                    }

                    //Facturas 
                    //List<FacturaFormaPago> FacturasCobros = db.Set<FacturaFormaPago>().Include("FormaPago").AsNoTracking().Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal).ToList();
                    //List<long> FacturaIDs = new List<long>();
                    //if (FacturasCobros != null && FacturasCobros.Count() > 0)
                    //{
                    //    FacturaIDs = FacturasCobros.Select(x => x.FacturaId).ToList();
                    //    if (FacturaIDs != null && FacturaIDs.Count() > 0)
                    //    {
                    //        FacturaIDs = db.Set<Factura>().AsNoTracking().Where(x => FacturaIDs.Contains(x.FacturaId) && !x.Anulada && AgenciaIds.Contains(x.AgenciaId)).Select(x => x.FacturaId).ToList();                           
                    //    }
                    //}

                //  Facturas = db.Set<Factura>().Include("Serie").Include("Agencia").Include("UsuarioCreo").Include("Detalles").Include("Pagos").Where(x => FacturaIDs.Contains(x.FacturaId)).AsEnumerable().Select(x => new FacturaModel() { FacturaId = x.FacturaId, Documento = string.Format("{0} - {1}", x.Serie.Nombre, x.NoFactura), Tipo = x.Credito ? "al Credito" : "al Contado", Dias = x.DiaCredito, Fecha = x.Fecha, Agencia = x.Agencia.Nombre, Usuario = x.UsuarioCreo.Nombre, ClienteId = x.ClienteId, Descuento = 0, Total = x.Pagos.Where(y => y.Fecha >= fechaInicial && y.Fecha <= fechaFinal).Sum(y => y.Valor), Anulada = x.Anulada }).AsEnumerable().Select(F => F).Join(db.Set<Cliente>(), S => S.ClienteId, C => C.ClienteId, (S, C) => new FacturaModel() { FacturaId = S.FacturaId, Documento = S.Documento, Fecha = S.Fecha, Agencia = S.Agencia, Usuario = S.Usuario, Nombre = S.Anulada ? "Factura Anulada" : (S.Dias > 0 ? string.Format("{0} - Factura {1} - {2} dia(s)", C.Nombre, S.Tipo, S.Dias) : string.Format("{0} - Factura {1}", C.Nombre, S.Tipo)), Descuento = S.Anulada ? 0 : (Convert.ToDecimal(S.Descuento) / Convert.ToDecimal(100)) * S.Total, Total = S.Anulada ? 0 : S.Total, TotalLiquido = S.Anulada ? 0 : S.Descuento == 0 ? S.Total : (S.Total - (Convert.ToDecimal(S.Descuento) / Convert.ToDecimal(100)) * S.Total) }).ToList();
                
                    //Recibos
                    List<ReciboFormaPago> RecibosCobros = db.Set<ReciboFormaPago>().Include("FormaPago").AsNoTracking().Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal).ToList();
                    List<long> ReciboIDs = new List<long>();
                    if (RecibosCobros != null && RecibosCobros.Count() > 0)
                    {
                        ReciboIDs = RecibosCobros.Select(x => x.ReciboId).ToList();
                        if (ReciboIDs != null && ReciboIDs.Count() > 0)
                        {
                            ReciboIDs = db.Set<Recibo>().AsNoTracking().Where(x => ReciboIDs.Contains(x.ReciboId) && !x.Anulada && AgenciaIds.Contains(x.AgenciaId)).Select(x => x.ReciboId).ToList();
                        }
                    }

                    Recibos = db.Set<Recibo>().Include("Agencia").Include("UsuarioCreo").Include("Detalles").Include("Pagos").Where(x => ReciboIDs.Contains(x.ReciboId)).AsEnumerable().Select(x => new FacturaModel() { FacturaId = x.ReciboId, Documento = string.Format("REC - {0}", x.ReciboId), Fecha = x.Fecha, Agencia = x.Agencia.Nombre, Usuario = x.UsuarioCreo.Nombre, ClienteId = x.ClienteId, Descuento = x.Descuento, Total = x.Pagos.Where(y => y.Fecha >= fechaInicial && y.Fecha <= fechaFinal).Sum(y => y.Valor), Anulada = x.Anulada }).AsEnumerable().Select(F => F).Join(db.Set<Cliente>(), S => S.ClienteId, C => C.ClienteId, (S, C) => new FacturaModel() { FacturaId = S.FacturaId, Documento = S.Documento, Fecha = S.Fecha, Agencia = S.Agencia, Usuario = S.Usuario, Nombre = S.Anulada ? "Recibo Anulado" : string.Format("{0} - Recibo", C.Nombre), Descuento = S.Anulada ? 0 : (Convert.ToDecimal(S.Descuento) / Convert.ToDecimal(100)) * S.Total, Total = S.Anulada ? 0 : S.Total, TotalLiquido = S.Anulada ? 0 : S.Descuento == 0 ? S.Total : (S.Total - (Convert.ToDecimal(S.Descuento) / Convert.ToDecimal(100)) * S.Total) }).ToList();

                    //Reparaciones
                  //  Reparaciones = db.Set<Reparacion>().Include("UsuarioCreo").Include("Agencia").Where(x => x.FechaCancelacion >= fechaInicial && x.FechaCancelacion <= fechaFinal && AgenciaIds.Contains(x.AgenciaId) && x.Operado).AsEnumerable().Select(x => new FacturaModel() { FacturaId = x.ReparacionId, Documento = string.Format("REP - {0}", x.ReparacionId), Fecha = x.Fecha, Agencia = x.Agencia.Nombre, Usuario = x.UsuarioCreo.Nombre, ClienteId = x.ClienteId, Descuento = x.Descuento, Total = x.CostoServicio, Anulada = false }).AsEnumerable().Select(F => F).Join(db.Set<Cliente>(), S => S.ClienteId, C => C.ClienteId, (S, C) => new FacturaModel() { FacturaId = S.FacturaId, Documento = S.Documento, Fecha = S.Fecha, Agencia = S.Agencia, Usuario = S.Usuario, Nombre = S.Anulada ? "Reparacion Anulado" : string.Format("{0} - Reparacion", C.Nombre), Descuento = S.Anulada ? 0 : (Convert.ToDecimal(S.Descuento) / Convert.ToDecimal(100)) * S.Total, Total = S.Anulada ? 0 : S.Total, TotalLiquido = S.Anulada ? 0 : S.Descuento == 0 ? S.Total : (S.Total - (Convert.ToDecimal(S.Descuento) / Convert.ToDecimal(100)) * S.Total) }).ToList();

                    //Egresos
                   // Egresos = db.Set<Movimiento>().Include("Agencia").Include("UsuarioCreo").Include("Detalles").Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && x.MovimientoTipoId == 2 && AgenciaIds.Contains(x.AgenciaId)).AsEnumerable().Select(x => new FacturaModel() { FacturaId = x.MovimientoId, Documento = "EG", Fecha = x.Fecha, Agencia = x.Agencia.Nombre, Usuario = x.UsuarioCreo.Nombre, ClienteId = x.ClienteId.Value, Descuento = x.Descuento, Total = x.Detalles.Sum(y => y.Cantidad * y.Precio), Anulada = x.Anulada }).AsEnumerable().Select(F => F).Join(db.Set<Cliente>(), S => S.ClienteId, C => C.ClienteId, (S, C) => new FacturaModel() { FacturaId = S.FacturaId, Documento = S.Documento, Fecha = S.Fecha, Agencia = S.Agencia, Usuario = S.Usuario, Nombre = S.Anulada ? "Egreso Anulado" : string.Format("{0} - Egreso", C.Nombre), Descuento = S.Anulada ? 0 : (Convert.ToDecimal(S.Descuento) / Convert.ToDecimal(100)) * S.Total, Total = S.Anulada ? 0 : S.Total, TotalLiquido = S.Anulada ? 0 : S.Descuento == 0 ? S.Total : (S.Total - (Convert.ToDecimal(S.Descuento) / Convert.ToDecimal(100)) * S.Total) }).ToList();

                    //Reservas
                    //List<long> ReservaIDs = new List<long>();
                    //ReservaIDs = db.Set<ReservaPago>().AsNoTracking().Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal).Select(x => x.ReservaId).Distinct().ToList();
                    //if (ReservaIDs != null && ReservaIDs.Count() > 0)
                    //{
                    //    Reservas = db.Set<Reserva>().Include("Agencia").Include("UsuarioCreo").Include("Detalles").Include("Pagos").Where(x => ReservaIDs.Contains(x.ReservaId) && AgenciaIds.Contains(x.AgenciaId)).AsEnumerable().Select(x => new FacturaModel() { FacturaId = x.ReservaId, Documento = string.Format("RES - {0}", x.ReservaId), Fecha = x.Fecha, Agencia = x.Agencia.Nombre, Usuario = x.UsuarioCreo.Nombre, ClienteId = x.ClienteId, Descuento = 0, Total = x.Pagos.Where(y => y.Fecha >= fechaInicial && y.Fecha <= fechaFinal).Sum(y => y.Valor), Anulada = x.Anulada }).AsEnumerable().Select(F => F).Join(db.Set<Cliente>(), S => S.ClienteId, C => C.ClienteId, (S, C) => new FacturaModel() { FacturaId = S.FacturaId, Documento = S.Documento, Fecha = S.Fecha, Agencia = S.Agencia, Usuario = S.Usuario, Nombre = S.Anulada ? "Reserva Anulada" : string.Format("{0} - Reserva", C.Nombre), Descuento = S.Anulada ? 0 : (Convert.ToDecimal(S.Descuento) / Convert.ToDecimal(100)) * S.Total, Total = S.Anulada ? 0 : S.Total, TotalLiquido = S.Anulada ? 0 : S.Descuento == 0 ? S.Total : (S.Total - (Convert.ToDecimal(S.Descuento) / Convert.ToDecimal(100)) * S.Total) }).ToList();                   
                    //}                    

                    //if (Facturas != null && Facturas.Count() > 0)
                    //{
                    //    foreach (var Factura in Facturas)
                    //    {
                    //        if (!Factura.Nombre.Equals("Factura Anulada"))
                    //        {
                    //            List<string> Formas = db.Set<FacturaFormaPago>().Include("FormaPago").Where(x => x.FacturaId == Factura.FacturaId && x.Fecha >= fechaInicial && x.Fecha <= fechaFinal).AsEnumerable().Select(x => string.Format("{0} - {1}", x.FormaPago.Nombre, x.Valor.ToString("C"))).ToList();
                    //            if (Formas != null && Formas.Count() > 0)
                    //            {
                    //                foreach (var item in Formas)
                    //                {
                    //                    Factura.Forma += string.Format("{0}\n", item);
                    //                }
                    //            }
                    //        }
                    //        else
                    //        {
                    //            Factura.Forma = "F.A.";
                    //        }                                                 
                    //    }      
                    //}

                    if (Recibos != null && Recibos.Count() > 0)
                    {
                        foreach (var Recibo in Recibos)
                        {
                            List<string> Formas = db.Set<ReciboFormaPago>().Include("FormaPago").Where(x => x.ReciboId == Recibo.FacturaId && x.Fecha >= fechaInicial && x.Fecha <= fechaFinal).AsEnumerable().Select(x => string.Format("{0} - {1}", x.FormaPago.Nombre, x.Valor.ToString("C"))).ToList();
                            if (Formas != null && Formas.Count() > 0)
                            {
                                foreach (var item in Formas)
                                {
                                    Recibo.Forma += string.Format("{0}\n", item);
                                }
                            }
                          Facturas.Add(Recibo);
                        }
                    }

                    //if (Reparaciones != null && Reparaciones.Count() > 0)
                    //{
                    //    foreach (var Reparacion in Reparaciones)
                    //    {
                    //        List<string> Formas = db.Set<ReparacionFormaPago>().Include("FormaPago").Where(x => x.ReparacionId == Reparacion.FacturaId).AsEnumerable().Select(x => string.Format("{0} - {1}", x.FormaPago.Nombre, x.Valor.ToString("C"))).ToList();
                    //        if (Formas != null && Formas.Count() > 0)
                    //        {
                    //            foreach (var item in Formas)
                    //            {
                    //                Reparacion.Forma += string.Format("{0}\n", item);
                    //            }
                    //        }
                    //        Facturas.Add(Reparacion);
                    //    }
                    //}

                    //if (Egresos != null && Egresos.Count() > 0)
                    //{
                    //    foreach (var Egreso in Egresos)
                    //    {
                    //        List<string> Formas = db.Set<MovimientoFormaPago>().Include("FormaPago").Where(x => x.MovimientoId == Egreso.FacturaId).AsEnumerable().Select(x => string.Format("{0} - {1}", x.FormaPago.Nombre, x.Valor.ToString("C"))).ToList();
                    //        if (Formas != null && Formas.Count() > 0)
                    //        {
                    //            foreach (var item in Formas)
                    //            {
                    //                Egreso.Forma += string.Format("{0}\n", item);
                    //            }
                    //        }
                    //        Facturas.Add(Egreso);
                    //    }     
                    //}

                    //if (Reservas != null && Reservas.Count() > 0)
                    //{
                    //    foreach (var Reserva in Reservas)
                    //    {
                    //        List<string> Formas = db.Set<ReservaPago>().Include("FormaPago").Where(x => x.ReservaId == Reserva.FacturaId && x.Fecha >= fechaInicial && x.Fecha <= fechaFinal).AsEnumerable().Select(x => string.Format("{0} - {1}", x.FormaPago.Nombre, x.Valor.ToString("C"))).ToList();
                    //        if (Formas != null && Formas.Count() > 0)
                    //        {
                    //            foreach (var item in Formas)
                    //            {
                    //                Reserva.Forma += string.Format("{0}\n", item);
                    //            }
                    //        }
                    //        Facturas.Add(Reserva);
                    //    }
                    //}
                }
                catch (Exception)
                {
                }

                return Facturas;
            }

            public List<FacturaModel> ObtenerFacturaxUsuario(DateTime fechaInicial, DateTime fechaFinal, long agenciaId, long usuarioId)
            {
                List<FacturaModel> Facturas = new List<FacturaModel>();
                List<FacturaModel> Recibos = new List<FacturaModel>();
                List<FacturaModel> Reparaciones = new List<FacturaModel>();
                List<FacturaModel> Reservas = new List<FacturaModel>();
                List<long> UsuarioIds = new List<long>();

                try
                {
                    if (usuarioId == 0)
                    {
                        UsuarioIds = db.Set<UsuarioAgencia>().Where(x => x.AgenciaId == agenciaId).Select(x => x.UsuarioId).ToList();
                    }
                    else
                    {
                        UsuarioIds.Add(usuarioId);
                    }

                    //Facturas 
                    Facturas = db.Set<Factura>().Include("Serie").Include("Agencia").Include("UsuarioCreo").Include("Detalles").Where(x => x.FechaHoraFactura >= fechaInicial && x.FechaHoraFactura <= fechaFinal && x.AgenciaId == agenciaId && UsuarioIds.Contains(x.UsrCreo)).AsEnumerable().Select(x => new FacturaModel() { FacturaId = x.FacturaId, Documento = string.Format("{0} - {1}", x.Serie.Nombre, x.NoFactura), Tipo = x.Credito ? "al Credito" : "al Contado", Dias = x.DiaCredito, Fecha = x.Fecha, Agencia = x.Agencia.Nombre, Usuario = x.UsuarioCreo.Nombre, ClienteId = x.ClienteId, Descuento = 0, Total = x.Detalles.Sum(y => y.Cantidad * y.Precio), Anulada = x.Anulada }).AsEnumerable().Select(F => F).Join(db.Set<Cliente>(), S => S.ClienteId, C => C.ClienteId, (S, C) => new FacturaModel() { FacturaId = S.FacturaId, Documento = S.Documento, Fecha = S.Fecha, Agencia = S.Agencia, Usuario = S.Usuario, Nombre = S.Anulada ? "Factura Anulada" : (S.Dias > 0 ? string.Format("{0} - Factura {1} - {2} dia(s)", C.Nombre, S.Tipo, S.Dias) : string.Format("{0} - Factura {1}", C.Nombre, S.Tipo)), Descuento = S.Anulada ? 0 : (Convert.ToDecimal(S.Descuento) / Convert.ToDecimal(100)) * S.Total, Total = S.Anulada ? 0 : S.Total, TotalLiquido = S.Anulada ? 0 : S.Descuento == 0 ? S.Total : (S.Total - (Convert.ToDecimal(S.Descuento) / Convert.ToDecimal(100)) * S.Total) }).ToList();

                    //Recibos 
                    Recibos = db.Set<Recibo>().Include("Agencia").Include("UsuarioCreo").Include("Detalles").Where(x => x.FechaHoraRecibo >= fechaInicial && x.FechaHoraRecibo <= fechaFinal && x.AgenciaId == agenciaId && UsuarioIds.Contains(x.UsrCreo)).AsEnumerable().Select(x => new FacturaModel() { FacturaId = x.ReciboId, Documento = string.Format("REC - {0}", x.ReciboId), Tipo = x.Credito ? "al Credito" : "al Contado", Dias = x.DiaCredito, Fecha = x.Fecha, Agencia = x.Agencia.Nombre, Usuario = x.UsuarioCreo.Nombre, ClienteId = x.ClienteId, Descuento = 0, Total = x.Detalles.Sum(y => y.Cantidad * y.Precio), Anulada = x.Anulada }).AsEnumerable().Select(F => F).Join(db.Set<Cliente>(), S => S.ClienteId, C => C.ClienteId, (S, C) => new FacturaModel() { FacturaId = S.FacturaId, Documento = S.Documento, Fecha = S.Fecha, Agencia = S.Agencia, Usuario = S.Usuario, Nombre = S.Anulada ? "Recibo Anulado" : (S.Dias > 0 ? string.Format("{0} - Recibo {1} - {2} dia(s)", C.Nombre, S.Tipo, S.Dias) : string.Format("{0} - Recibo {1}", C.Nombre, S.Tipo)), Descuento = S.Anulada ? 0 : (Convert.ToDecimal(S.Descuento) / Convert.ToDecimal(100)) * S.Total, Total = S.Anulada ? 0 : S.Total, TotalLiquido = S.Anulada ? 0 : S.Descuento == 0 ? S.Total : (S.Total - (Convert.ToDecimal(S.Descuento) / Convert.ToDecimal(100)) * S.Total) }).ToList();

                    //Reservas 
                    Reservas = db.Set<Reserva>().Include("Agencia").Include("UsuarioCreo").Include("Detalles").Include("Pagos").Where(x => x.FechaHoraReserva >= fechaInicial && x.FechaHoraReserva <= fechaFinal && x.AgenciaId == agenciaId && UsuarioIds.Contains(x.UsrCreo)).AsEnumerable().Select(x => new FacturaModel() { FacturaId = x.ReservaId, Documento = string.Format("RES - {0}", x.ReservaId), Tipo = false ? "al Credito" : "al Contado", Dias = 0, Fecha = x.Fecha, Agencia = x.Agencia.Nombre, Usuario = x.UsuarioCreo.Nombre, ClienteId = x.ClienteId, Descuento = 0, Total = x.Pagos.Sum(y => y.Valor), Anulada = x.Anulada }).AsEnumerable().Select(F => F).Join(db.Set<Cliente>(), S => S.ClienteId, C => C.ClienteId, (S, C) => new FacturaModel() { FacturaId = S.FacturaId, Documento = S.Documento, Fecha = S.Fecha, Agencia = S.Agencia, Usuario = S.Usuario, Nombre = S.Anulada ? "Reserva Anulada" : (S.Dias > 0 ? string.Format("{0} - Reserva {1} - {2} dia(s)", C.Nombre, S.Tipo, S.Dias) : string.Format("{0} - Reserva {1}", C.Nombre, S.Tipo)), Descuento = S.Anulada ? 0 : (Convert.ToDecimal(S.Descuento) / Convert.ToDecimal(100)) * S.Total, Total = S.Anulada ? 0 : S.Total, TotalLiquido = S.Anulada ? 0 : S.Descuento == 0 ? S.Total : (S.Total - (Convert.ToDecimal(S.Descuento) / Convert.ToDecimal(100)) * S.Total) }).ToList();

                    if (Facturas != null && Facturas.Count() > 0)
                    {
                        foreach (var Factura in Facturas)
                        {
                            if (!Factura.Nombre.Equals("Factura Anulada"))
                            {
                                List<string> Formas = db.Set<FacturaFormaPago>().Include("FormaPago").Where(x => x.FacturaId == Factura.FacturaId).AsEnumerable().Select(x => string.Format("{0} - {1}", x.FormaPago.Nombre, x.Valor.ToString("C"))).ToList();
                                if (Formas != null && Formas.Count() > 0)
                                {
                                    foreach (var item in Formas)
                                    {
                                        Factura.Forma += string.Format("{0}\n", item);
                                    }
                                }
                            }
                            else
                            {
                                Factura.Forma = "F.A.";
                            }
                        }
                    }

                    if (Recibos != null && Recibos.Count() > 0)
                    {
                        foreach (var Recibo in Recibos)
                        {
                            if (!Recibo.Nombre.Equals("Recibo Anulado"))
                            {
                                List<string> Formas = db.Set<ReciboFormaPago>().Include("FormaPago").Where(x => x.ReciboId == Recibo.FacturaId).AsEnumerable().Select(x => string.Format("{0} - {1}", x.FormaPago.Nombre, x.Valor.ToString("C"))).ToList();
                                if (Formas != null && Formas.Count() > 0)
                                {
                                    foreach (var item in Formas)
                                    {
                                        Recibo.Forma += string.Format("{0}\n", item);
                                    }
                                }
                            }
                            else
                            {
                                Recibo.Forma = "R.A.";
                            }

                            Facturas.Add(Recibo);
                        }
                    }

                    if (Reservas != null && Reservas.Count() > 0)
                    {
                        foreach (var Reserva in Reservas)
                        {
                            if (!Reserva.Nombre.Equals("Reserva Anulada"))
                            {
                                List<string> Formas = db.Set<ReservaPago>().Include("FormaPago").Where(x => x.ReservaId == Reserva.FacturaId).AsEnumerable().Select(x => string.Format("{0} - {1}", x.FormaPago.Nombre, x.Valor.ToString("C"))).ToList();
                                if (Formas != null && Formas.Count() > 0)
                                {
                                    foreach (var item in Formas)
                                    {
                                        Reserva.Forma += string.Format("{0}\n", item);
                                    }
                                }
                            }
                            else
                            {
                                Reserva.Forma = "R.A.";
                            }

                            Facturas.Add(Reserva);
                        }
                    }          
                }
                catch (Exception)
                {
                }

                return Facturas;
            }

            public List<FormaPago> ObtenerFacturaPorFormaPago(DateTime fechaInicial, DateTime fechaFinal, long agenciaId, long usuarioId)
            {
                List<FormaPago> Formas = new List<FormaPago>();
                List<long> AgenciaIds = new List<long>();

                try
                {
                    if (agenciaId == 0)
                    {
                        AgenciaIds = db.Set<UsuarioAgencia>().Where(x => x.UsuarioId == usuarioId).Select(x => x.AgenciaId).ToList();
                    }
                    else
                    {
                        AgenciaIds.Add(agenciaId);
                    }

                    //Facturas
                    //List<FacturaFormaPago> FacturasCobros = db.Set<FacturaFormaPago>().Include("FormaPago").AsNoTracking().Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal).ToList();
                    List<long> FacturaIDs = new List<long>();
                    //if (FacturasCobros != null && FacturasCobros.Count() > 0)
                    //{
                    //    FacturaIDs = FacturasCobros.Select(x => x.FacturaId).ToList();
                    //    if (FacturaIDs != null && FacturaIDs.Count() > 0)
                    //    {
                    //        FacturaIDs = db.Set<Factura>().AsNoTracking().Where(x => FacturaIDs.Contains(x.FacturaId) && !x.Anulada && AgenciaIds.Contains(x.AgenciaId)).Select(x => x.FacturaId).ToList();
                    //    }
                    //}

                    List<FormaModel> FacturasIds = db.Set<Factura>().Where(x => FacturaIDs.Contains(x.FacturaId)).Join(db.Set<FacturaFormaPago>().Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal), F => F.FacturaId, FF => FF.FacturaId, (F, FF) => new { F, FF }).GroupBy(r => r.FF.FormaPagoId).Select(x => new FormaModel { FacturaId = x.Key, Total = x.Sum(g => g.FF.Valor) }).ToList();
                    //if (FacturasIds != null && FacturasIds.Count() > 0)
                    //{
                    //    Formas = FacturasIds.Join(db.Set<FormaPago>(), F => F.FacturaId, F => F.FormaPagoId, (R, F) => new FormaPago() { FormaPagoId = F.FormaPagoId, Nombre = F.Nombre, Valor = R.Total }).ToList();
                    //}

                    //Recibos
                    List<ReciboFormaPago> RecibosCobros = db.Set<ReciboFormaPago>().Include("FormaPago").AsNoTracking().Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal).ToList();
                    List<long> ReciboIDs = new List<long>();
                    if (RecibosCobros != null && RecibosCobros.Count() > 0)
                    {
                        ReciboIDs = RecibosCobros.Select(x => x.ReciboId).ToList();
                        if (ReciboIDs != null && ReciboIDs.Count() > 0)
                        {
                            ReciboIDs = db.Set<Recibo>().AsNoTracking().Where(x => ReciboIDs.Contains(x.ReciboId) && !x.Anulada && AgenciaIds.Contains(x.AgenciaId)).Select(x => x.ReciboId).ToList();
                        }
                    }

                    FacturasIds = db.Set<Recibo>().Where(x => ReciboIDs.Contains(x.ReciboId)).Join(db.Set<ReciboFormaPago>().Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal), F => F.ReciboId, FF => FF.ReciboId, (F, FF) => new { F, FF }).GroupBy(r => r.FF.FormaPagoId).Select(x => new FormaModel { FacturaId = x.Key, Total = x.Sum(g => g.FF.Valor) }).ToList();
                    if (FacturasIds != null && FacturasIds.Count() > 0)
                    {
                        Formas.AddRange(FacturasIds.Join(db.Set<FormaPago>(), F => F.FacturaId, F => F.FormaPagoId, (R, F) => new FormaPago() { FormaPagoId = F.FormaPagoId, Nombre = F.Nombre, Valor = R.Total }));
                    }

                    //Reparaciones
                    //FacturasIds = db.Set<Reparacion>().Where(x => x.FechaCancelacion >= fechaInicial && x.FechaCancelacion <= fechaFinal && x.Operado && AgenciaIds.Contains(x.AgenciaId)).Join(db.Set<ReparacionFormaPago>(), F => F.ReparacionId, FF => FF.ReparacionId, (F, FF) => new { F, FF }).GroupBy(r => r.FF.FormaPagoId).Select(x => new FormaModel { FacturaId = x.Key, Total = x.Sum(g => g.FF.Valor) }).ToList();
                    //if (FacturasIds != null && FacturasIds.Count() > 0)
                    //{
                    //    Formas.AddRange(FacturasIds.Join(db.Set<FormaPago>(), F => F.FacturaId, F => F.FormaPagoId, (R, F) => new FormaPago() { FormaPagoId = F.FormaPagoId, Nombre = F.Nombre, Valor = R.Total }));
                    //}

                    ////Egresos
                    //FacturasIds = db.Set<Movimiento>().Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && x.Anulada == false && x.MovimientoTipoId == 2 && AgenciaIds.Contains(x.AgenciaId)).Join(db.Set<MovimientoFormaPago>(), F => F.MovimientoId, FF => FF.MovimientoId, (F, FF) => new { F, FF }).GroupBy(r => r.FF.FormaPagoId).Select(x => new FormaModel { FacturaId = x.Key, Total = x.Sum(g => g.FF.Valor) }).ToList();
                    //if (FacturasIds != null && FacturasIds.Count() > 0)
                    //{
                    //    Formas.AddRange(FacturasIds.Join(db.Set<FormaPago>(), F => F.FacturaId, F => F.FormaPagoId, (R, F) => new FormaPago() { FormaPagoId = F.FormaPagoId, Nombre = F.Nombre, Valor = R.Total }));
                    //}

                    //Reservas
                    //List<long> ReservaIDs = new List<long>();
                    //ReservaIDs = db.Set<ReservaPago>().AsNoTracking().Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal).Select(x => x.ReservaId).Distinct().ToList();
                    //if (ReservaIDs != null && ReservaIDs.Count() > 0)
                    //{
                    //    FacturasIds = db.Set<Reserva>().Where(x => ReservaIDs.Contains(x.ReservaId) && x.Anulada == false && AgenciaIds.Contains(x.AgenciaId)).Join(db.Set<ReservaPago>().Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal), F => F.ReservaId, FF => FF.ReservaId, (F, FF) => new { F, FF }).GroupBy(r => r.FF.FormaPagoId).Select(x => new FormaModel { FacturaId = x.Key, Total = x.Sum(g => g.FF.Valor) }).ToList();
                    //    if (FacturasIds != null && FacturasIds.Count() > 0)
                    //    {
                    //        Formas.AddRange(FacturasIds.Join(db.Set<FormaPago>(), F => F.FacturaId, F => F.FormaPagoId, (R, F) => new FormaPago() { FormaPagoId = F.FormaPagoId, Nombre = F.Nombre, Valor = R.Total }));
                    //    }
                    //}  

                    if (Formas != null && Formas.Count() > 0)
                    {
                        Formas = Formas.GroupBy(x => new { x.FormaPagoId, x.Nombre }).Select(g => new FormaPago() { FormaPagoId = g.Key.FormaPagoId, Nombre = g.Key.Nombre, Valor = g.Sum(y => y.Valor) }).ToList();
                    }

                    if (Formas != null && Formas.Count() > 0)
                    {
                        List<Gasto> Gastos = db.Set<Gasto>().AsNoTracking().Where(x => x.FechaFactura >= fechaInicial && x.FechaFactura <= fechaFinal && AgenciaIds.Contains(x.AgenciaId.Value)).ToList();
                        decimal TotalGastos = 0;
                        if (Gastos != null && Gastos.Count() > 0)
                        {
                            TotalGastos = Gastos.Sum(x => x.Monto);
                        }

                        if (TotalGastos > 0)
                        {
                            foreach (var item in Formas)
                            {
                                if (item.FormaPagoId == 20171028001)
                                {
                                    item.Valor -= TotalGastos;
                                }
                            }

                            Formas.Add(new FormaPago() { FormaPagoId = 20171028002, Nombre = "Gastos", Valor = TotalGastos, Activo = true });
                        }
                    }
                }
                catch (Exception)
                {
                }

                return Formas;
            }

            public List<FormaPago> ObtenerFacturaPorFormaPagoxUsuario(DateTime fechaInicial, DateTime fechaFinal, long agenciaId, long usuarioId)
            {
                List<FormaPago> Formas = new List<FormaPago>();
                List<long> UsuarioIds = new List<long>();

                try
                {
                    if (usuarioId == 0)
                    {
                        UsuarioIds = db.Set<UsuarioAgencia>().Where(x => x.AgenciaId == agenciaId).Select(x => x.UsuarioId).ToList();
                    }
                    else
                    {
                        UsuarioIds.Add(usuarioId);
                    }

                    //Facturas
                    List<FormaModel> FacturasIds = db.Set<Factura>().Where(x => x.FechaHoraFactura >= fechaInicial && x.FechaHoraFactura <= fechaFinal && x.Anulada == false && x.AgenciaId == agenciaId && UsuarioIds.Contains(x.UsrCreo)).Join(db.Set<FacturaFormaPago>(), F => F.FacturaId, FF => FF.FacturaId, (F, FF) => new { F, FF }).GroupBy(r => r.FF.FormaPagoId).Select(x => new FormaModel { FacturaId = x.Key, Total = x.Sum(g => g.FF.Valor) }).ToList();
                    if (FacturasIds != null && FacturasIds.Count() > 0)
                    {
                        Formas = FacturasIds.Join(db.Set<FormaPago>(), F => F.FacturaId, F => F.FormaPagoId, (R, F) => new FormaPago() { FormaPagoId = F.FormaPagoId, Nombre = F.Nombre, Valor = R.Total }).ToList();
                    }

                    //Recibos
                    FacturasIds = db.Set<Recibo>().Where(x => x.FechaHoraRecibo >= fechaInicial && x.FechaHoraRecibo <= fechaFinal && x.Anulada == false && x.AgenciaId == agenciaId && UsuarioIds.Contains(x.UsrCreo)).Join(db.Set<ReciboFormaPago>(), F => F.ReciboId, FF => FF.ReciboId, (F, FF) => new { F, FF }).GroupBy(r => r.FF.FormaPagoId).Select(x => new FormaModel { FacturaId = x.Key, Total = x.Sum(g => g.FF.Valor) }).ToList();
                    if (FacturasIds != null && FacturasIds.Count() > 0)
                    {
                        Formas.AddRange(FacturasIds.Join(db.Set<FormaPago>(), F => F.FacturaId, F => F.FormaPagoId, (R, F) => new FormaPago() { FormaPagoId = F.FormaPagoId, Nombre = F.Nombre, Valor = R.Total }));
                    }

                    //Reservas
                    FacturasIds = db.Set<Reserva>().Where(x => x.FechaHoraReserva >= fechaInicial && x.FechaHoraReserva <= fechaFinal && x.Anulada == false && x.AgenciaId == agenciaId && UsuarioIds.Contains(x.UsrCreo)).Join(db.Set<ReservaPago>(), F => F.ReservaId, FF => FF.ReservaId, (F, FF) => new { F, FF }).GroupBy(r => r.FF.FormaPagoId).Select(x => new FormaModel { FacturaId = x.Key, Total = x.Sum(g => g.FF.Valor) }).ToList();
                    if (FacturasIds != null && FacturasIds.Count() > 0)
                    {
                        Formas.AddRange(FacturasIds.Join(db.Set<FormaPago>(), F => F.FacturaId, F => F.FormaPagoId, (R, F) => new FormaPago() { FormaPagoId = F.FormaPagoId, Nombre = F.Nombre, Valor = R.Total }));
                    }  

                    if (Formas != null && Formas.Count() > 0)
                    {
                        Formas = Formas.GroupBy(x => new { x.FormaPagoId, x.Nombre }).Select(g => new FormaPago() { FormaPagoId = g.Key.FormaPagoId, Nombre = g.Key.Nombre, Valor = g.Sum(y => y.Valor) }).ToList();
                    }

                    if (Formas != null && Formas.Count() > 0)
                    {
                        List<Gasto> Gastos = db.Set<Gasto>().AsNoTracking().Where(x => x.FechaFactura >= fechaInicial && x.FechaFactura <= fechaFinal && x.AgenciaId == agenciaId && UsuarioIds.Contains(x.UsrCreo)).ToList();
                        decimal TotalGastos = 0;
                        if (Gastos != null && Gastos.Count() > 0)
                        {
                            TotalGastos = Gastos.Sum(x => x.Monto);
                        }

                        if (TotalGastos > 0)
                        {
                            foreach (var item in Formas)
                            {
                                if (item.FormaPagoId == 20171028001)
                                {
                                    item.Valor -= TotalGastos;
                                }
                            }

                            Formas.Add(new FormaPago() { FormaPagoId = 20171028002, Nombre = "Gastos", Valor = TotalGastos, Activo = true });
                        }
                    }
                }
                catch (Exception)
                {
                }

                return Formas;
            }

            public List<VentaModel> ObtenerVentasxTienda(DateTime fechaInicial, DateTime fechaFinal, long marcaId, long agenciaId, long usuarioId)
            {
                List<VentaModel> Ventas = new List<VentaModel>();
                List<VentaModel> Recibos = new List<VentaModel>();
                List<VentaModel> Facturas = new List<VentaModel>();

                List<long> AgenciaIds = new List<long>();

                try
                {
                    if (agenciaId == 0)
                    {
                        AgenciaIds = db.Set<UsuarioAgencia>().Where(x => x.UsuarioId == usuarioId).Select(x => x.AgenciaId).ToList();
                    }
                    else
                    {
                        AgenciaIds.Add(agenciaId);
                    }

                    if (marcaId == 0)
                    {
                        Facturas = db.Set<Factura>().Include("Vendedor").Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && AgenciaIds.Contains(x.AgenciaId)).Select(F => new VentaModel() { SerieId = F.SerieId, NoFactura = F.NoFactura, AgenciaId = F.AgenciaId, FacturaId = F.FacturaId, Dias = F.DiaCredito, Fecha = F.Fecha, Vendedor = F.Vendedor.Nombre, Estado = F.Anulada }).AsEnumerable().Join(db.Set<FacturaDetalle>(), F => F.FacturaId, FD => FD.FacturaId, (F, FD) => new VentaModel() { Id = FD.ProductoId, SerieId = F.SerieId, NoFactura = F.NoFactura, AgenciaId = F.AgenciaId, Cantidad = F.Estado ? 0 : FD.Cantidad, Total = F.Estado ? 0 : FD.Cantidad * FD.Precio, CostoIva = F.Estado ? 0 : FD.PrecioCosto, PrecioIva = F.Estado ? 0 : FD.Precio, Descuento = F.Estado ? 0 : FD.Descuento.Value, FacturaId = F.FacturaId, Dias = F.Dias, Fecha = F.Fecha, Vendedor = F.Vendedor, Estado = F.Estado }).AsEnumerable().Join(db.Set<Producto>(), V => V.Id, P => P.ProductoId, (V, P) => new VentaModel() { Id = V.Id, Codigo = P.Codigo, MarcaId = P.MarcaId, Descripcion = P.Descripcion, SerieId = V.SerieId, NoFactura = V.NoFactura, Cantidad = V.Cantidad, Total = V.Total, CostoIva = V.CostoIva, PrecioIva = V.PrecioIva, AgenciaId = V.AgenciaId, Descuento = V.Descuento, FacturaId = V.FacturaId, Dias = V.Dias, Fecha = V.Fecha, Vendedor = V.Vendedor, Estado = V.Estado }).AsEnumerable().Join(db.Set<Agencia>(), V => V.AgenciaId, A => A.AgenciaId, (V, A) => new VentaModel() { Id = V.Id, Codigo = V.Codigo, MarcaId = V.MarcaId, Descripcion = V.Descripcion, SerieId = V.SerieId, NoFactura = V.NoFactura, AgenciaId = V.AgenciaId, Agencia = A.Nombre, Cantidad = V.Cantidad, Total = V.Total, CostoIva = V.CostoIva, PrecioIva = V.PrecioIva, Descuento = V.Descuento, FacturaId = V.FacturaId, Dias = V.Dias, Fecha = V.Fecha, Vendedor = V.Vendedor, Estado = V.Estado }).AsEnumerable().Join(db.Set<Serie>(), V => V.SerieId, S => S.SerieId, (V, S) => new VentaModel() { Id = V.Id, Codigo = V.Codigo, MarcaId = V.MarcaId, Descripcion = V.Descripcion, SerieId = V.SerieId, Serie = S.Nombre, NoFactura = V.NoFactura, AgenciaId = V.AgenciaId, Agencia = V.Agencia, Cantidad = V.Cantidad, Total = V.Total, CostoIva = V.CostoIva, PrecioIva = V.PrecioIva, Descuento = V.Descuento, FacturaId = V.FacturaId, Dias = V.Dias, Fecha = V.Fecha, Vendedor = V.Vendedor, Estado = V.Estado }).AsEnumerable().Join(db.Set<Marca>(), V => V.MarcaId, M => M.MarcaId, (V, M) => new VentaModel() { Id = V.Id, Codigo = V.Codigo, MarcaId = V.MarcaId, Marca = M.Nombre, Descripcion = V.Descripcion, SerieId = V.SerieId, Serie = V.Serie, NoFactura = V.NoFactura, AgenciaId = V.AgenciaId, Agencia = V.Agencia, Cantidad = V.Cantidad, Total = V.Total, CostoIva = V.CostoIva, PrecioIva = V.PrecioIva, Descuento = V.Descuento, FacturaId = V.FacturaId, Dias = V.Dias, Fecha = V.Fecha, Vendedor = V.Vendedor, Estado = V.Estado }).OrderBy(x => x.Fecha).ThenBy(x => x.NoFactura).ToList();
                        Recibos = db.Set<Recibo>().Include("Vendedor").Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && AgenciaIds.Contains(x.AgenciaId)).Select(F => new VentaModel() { SerieId = 0, Serie = "REC", NoFactura = F.ReciboId, AgenciaId = F.AgenciaId, FacturaId = F.ReciboId, Dias = F.DiaCredito, Fecha = F.Fecha, Vendedor = F.Vendedor.Nombre, Estado = F.Anulada }).AsEnumerable().Join(db.Set<ReciboDetalle>(), F => F.FacturaId, FD => FD.ReciboId, (F, FD) => new VentaModel() { Id = FD.ProductoId, SerieId = F.SerieId, Serie = F.Serie, NoFactura = F.NoFactura, AgenciaId = F.AgenciaId, Cantidad = F.Estado ? 0 : FD.Cantidad, Total = F.Estado ? 0 : FD.Cantidad * FD.Precio, CostoIva = F.Estado ? 0 : FD.PrecioCosto, PrecioIva = F.Estado ? 0 : FD.Precio, Descuento = F.Estado ? 0 : FD.Descuento.Value, FacturaId = F.FacturaId, Dias = F.Dias, Fecha = F.Fecha, Vendedor = F.Vendedor, Estado = F.Estado }).AsEnumerable().Join(db.Set<Producto>(), V => V.Id, P => P.ProductoId, (V, P) => new VentaModel() { Id = V.Id, Codigo = P.Codigo, MarcaId = P.MarcaId, Descripcion = P.Descripcion, SerieId = V.SerieId, Serie = V.Serie, NoFactura = V.NoFactura, Cantidad = V.Cantidad, Total = V.Total, CostoIva = V.CostoIva, PrecioIva = V.PrecioIva, AgenciaId = V.AgenciaId, Descuento = V.Descuento, FacturaId = V.FacturaId, Dias = V.Dias, Fecha = V.Fecha, Vendedor = V.Vendedor, Estado = V.Estado }).AsEnumerable().Join(db.Set<Agencia>(), V => V.AgenciaId, A => A.AgenciaId, (V, A) => new VentaModel() { Id = V.Id, Codigo = V.Codigo, MarcaId = V.MarcaId, Descripcion = V.Descripcion, SerieId = V.SerieId, Serie = V.Serie, NoFactura = V.NoFactura, AgenciaId = V.AgenciaId, Agencia = A.Nombre, Cantidad = V.Cantidad, Total = V.Total, CostoIva = V.CostoIva, PrecioIva = V.PrecioIva, Descuento = V.Descuento, FacturaId = V.FacturaId, Dias = V.Dias, Fecha = V.Fecha, Vendedor = V.Vendedor, Estado = V.Estado }).AsEnumerable().Join(db.Set<Marca>(), V => V.MarcaId, M => M.MarcaId, (V, M) => new VentaModel() { Id = V.Id, Codigo = V.Codigo, MarcaId = V.MarcaId, Marca = M.Nombre, Descripcion = V.Descripcion, SerieId = V.SerieId, Serie = V.Serie, NoFactura = V.NoFactura, AgenciaId = V.AgenciaId, Agencia = V.Agencia, Cantidad = V.Cantidad, Total = V.Total, CostoIva = V.CostoIva, PrecioIva = V.PrecioIva, Descuento = V.Descuento, FacturaId = V.FacturaId, Dias = V.Dias, Fecha = V.Fecha, Vendedor = V.Vendedor, Estado = V.Estado }).OrderBy(x => x.Fecha).ThenBy(x => x.FacturaId).ToList();                        
                    }
                    else
                    {
                        Facturas = db.Set<Factura>().Include("Vendedor").Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && AgenciaIds.Contains(x.AgenciaId)).Select(F => new VentaModel() { SerieId = F.SerieId, NoFactura = F.NoFactura, AgenciaId = F.AgenciaId, FacturaId = F.FacturaId, Dias = F.DiaCredito, Fecha = F.Fecha, Vendedor = F.Vendedor.Nombre, Estado = F.Anulada }).AsEnumerable().Join(db.Set<FacturaDetalle>(), F => F.FacturaId, FD => FD.FacturaId, (F, FD) => new VentaModel() { Id = FD.ProductoId, SerieId = F.SerieId, NoFactura = F.NoFactura, AgenciaId = F.AgenciaId, Cantidad = F.Estado ? 0 : FD.Cantidad, Total = F.Estado ? 0 : FD.Cantidad * FD.Precio, CostoIva = F.Estado ? 0 : FD.PrecioCosto, PrecioIva = F.Estado ? 0 : FD.Precio, Descuento = F.Estado ? 0 : FD.Descuento.Value, FacturaId = F.FacturaId, Dias = F.Dias, Fecha = F.Fecha, Vendedor = F.Vendedor, Estado = F.Estado }).AsEnumerable().Join(db.Set<Producto>().Where(x => x.MarcaId == marcaId), V => V.Id, P => P.ProductoId, (V, P) => new VentaModel() { Id = V.Id, Codigo = P.Codigo, MarcaId = P.MarcaId, Descripcion = P.Descripcion, SerieId = V.SerieId, NoFactura = V.NoFactura, Cantidad = V.Cantidad, Total = V.Total, CostoIva = V.CostoIva, PrecioIva = V.PrecioIva, AgenciaId = V.AgenciaId, Descuento = V.Descuento, FacturaId = V.FacturaId, Dias = V.Dias, Fecha = V.Fecha, Vendedor = V.Vendedor, Estado = V.Estado }).AsEnumerable().Join(db.Set<Agencia>(), V => V.AgenciaId, A => A.AgenciaId, (V, A) => new VentaModel() { Id = V.Id, Codigo = V.Codigo, MarcaId = V.MarcaId, Descripcion = V.Descripcion, SerieId = V.SerieId, NoFactura = V.NoFactura, AgenciaId = V.AgenciaId, Agencia = A.Nombre, Cantidad = V.Cantidad, Total = V.Total, CostoIva = V.CostoIva, PrecioIva = V.PrecioIva, Descuento = V.Descuento, FacturaId = V.FacturaId, Dias = V.Dias, Fecha = V.Fecha, Vendedor = V.Vendedor, Estado = V.Estado }).AsEnumerable().Join(db.Set<Serie>(), V => V.SerieId, S => S.SerieId, (V, S) => new VentaModel() { Id = V.Id, Codigo = V.Codigo, MarcaId = V.MarcaId, Descripcion = V.Descripcion, SerieId = V.SerieId, Serie = S.Nombre, NoFactura = V.NoFactura, AgenciaId = V.AgenciaId, Agencia = V.Agencia, Cantidad = V.Cantidad, Total = V.Total, CostoIva = V.CostoIva, PrecioIva = V.PrecioIva, Descuento = V.Descuento, FacturaId = V.FacturaId, Dias = V.Dias, Fecha = V.Fecha, Vendedor = V.Vendedor, Estado = V.Estado }).AsEnumerable().Join(db.Set<Marca>(), V => V.MarcaId, M => M.MarcaId, (V, M) => new VentaModel() { Id = V.Id, Codigo = V.Codigo, MarcaId = V.MarcaId, Marca = M.Nombre, Descripcion = V.Descripcion, SerieId = V.SerieId, Serie = V.Serie, NoFactura = V.NoFactura, AgenciaId = V.AgenciaId, Agencia = V.Agencia, Cantidad = V.Cantidad, Total = V.Total, CostoIva = V.CostoIva, PrecioIva = V.PrecioIva, Descuento = V.Descuento, FacturaId = V.FacturaId, Dias = V.Dias, Fecha = V.Fecha, Vendedor = V.Vendedor, Estado = V.Estado }).OrderBy(x => x.Fecha).ThenBy(x => x.NoFactura).ToList();
                        Recibos = db.Set<Recibo>().Include("Vendedor").Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && AgenciaIds.Contains(x.AgenciaId)).Select(F => new VentaModel() { SerieId = 0, Serie = "REC", NoFactura = F.ReciboId, AgenciaId = F.AgenciaId, FacturaId = F.ReciboId, Dias = F.DiaCredito, Fecha = F.Fecha, Vendedor = F.Vendedor.Nombre, Estado = F.Anulada }).AsEnumerable().Join(db.Set<ReciboDetalle>(), F => F.FacturaId, FD => FD.ReciboId, (F, FD) => new VentaModel() { Id = FD.ProductoId, SerieId = F.SerieId, Serie = F.Serie, NoFactura = F.NoFactura, AgenciaId = F.AgenciaId, Cantidad = F.Estado ? 0 : FD.Cantidad, Total = F.Estado ? 0 : FD.Cantidad * FD.Precio, CostoIva = F.Estado ? 0 : FD.PrecioCosto, PrecioIva = F.Estado ? 0 : FD.Precio, Descuento = F.Estado ? 0 : FD.Descuento.Value, FacturaId = F.FacturaId, Dias = F.Dias, Fecha = F.Fecha, Vendedor = F.Vendedor, Estado = F.Estado }).AsEnumerable().Join(db.Set<Producto>(), V => V.Id, P => P.ProductoId, (V, P) => new VentaModel() { Id = V.Id, Codigo = P.Codigo, MarcaId = P.MarcaId, Descripcion = P.Descripcion, SerieId = V.SerieId, Serie = V.Serie, NoFactura = V.NoFactura, Cantidad = V.Cantidad, Total = V.Total, CostoIva = V.CostoIva, PrecioIva = V.PrecioIva, AgenciaId = V.AgenciaId, Descuento = V.Descuento, FacturaId = V.FacturaId, Dias = V.Dias, Fecha = V.Fecha, Vendedor = V.Vendedor, Estado = V.Estado }).AsEnumerable().Join(db.Set<Agencia>(), V => V.AgenciaId, A => A.AgenciaId, (V, A) => new VentaModel() { Id = V.Id, Codigo = V.Codigo, MarcaId = V.MarcaId, Descripcion = V.Descripcion, SerieId = V.SerieId, Serie = V.Serie, NoFactura = V.NoFactura, AgenciaId = V.AgenciaId, Agencia = A.Nombre, Cantidad = V.Cantidad, Total = V.Total, CostoIva = V.CostoIva, PrecioIva = V.PrecioIva, Descuento = V.Descuento, FacturaId = V.FacturaId, Dias = V.Dias, Fecha = V.Fecha, Vendedor = V.Vendedor, Estado = V.Estado }).AsEnumerable().Join(db.Set<Marca>(), V => V.MarcaId, M => M.MarcaId, (V, M) => new VentaModel() { Id = V.Id, Codigo = V.Codigo, MarcaId = V.MarcaId, Marca = M.Nombre, Descripcion = V.Descripcion, SerieId = V.SerieId, Serie = V.Serie, NoFactura = V.NoFactura, AgenciaId = V.AgenciaId, Agencia = V.Agencia, Cantidad = V.Cantidad, Total = V.Total, CostoIva = V.CostoIva, PrecioIva = V.PrecioIva, Descuento = V.Descuento, FacturaId = V.FacturaId, Dias = V.Dias, Fecha = V.Fecha, Vendedor = V.Vendedor, Estado = V.Estado }).OrderBy(x => x.Fecha).ThenBy(x => x.FacturaId).ToList();  
                    }

                    if (Facturas != null && Facturas.Count() > 0)
                    {
                        foreach (var Factura in Facturas)
                        {
                            if (!Factura.Estado)
                            {
                                List<string> Formas = db.Set<FacturaFormaPago>().Include("FormaPago").Where(x => x.FacturaId == Factura.FacturaId).AsEnumerable().Select(x => string.Format("{0} - {1}", x.FormaPago.Nombre, x.Valor.ToString("C"))).ToList();
                                if (Formas != null && Formas.Count() > 0)
                                {
                                    foreach (var item in Formas)
                                    {
                                        Factura.Concepto += string.Format("{0}\n", item);
                                    }

                                    if (Factura.Dias > 0)
                                    {
                                        Factura.Concepto = string.Format("{0} - {1} dia(s)", Factura.Concepto, Factura.Dias);
                                    }
                                }         
                            }
                            else
                            {
                                Factura.Concepto = "Factura Anulada";
                            }                                       
                        }                         
                    }

                    if (Recibos != null && Recibos.Count() > 0)
                    {
                        foreach (var Recibo in Recibos)
                        {
                            if (!Recibo.Estado)
                            {
                                List<string> Formas = db.Set<ReciboFormaPago>().Include("FormaPago").Where(x => x.ReciboId == Recibo.FacturaId).AsEnumerable().Select(x => string.Format("{0} - {1}", x.FormaPago.Nombre, x.Valor.ToString("C"))).ToList();
                                if (Formas != null && Formas.Count() > 0)
                                {
                                    foreach (var item in Formas)
                                    {
                                        Recibo.Concepto += string.Format("{0}\n", item);
                                    }

                                    if (Recibo.Dias > 0)
                                    {
                                        Recibo.Concepto = string.Format("{0} - {1} dia(s)", Recibo.Concepto, Recibo.Dias);
                                    }
                                }
                            }
                            else
                            {
                                Recibo.Concepto = "Recibo Anulado";
                            }
                        }
                    }

                    Ventas.AddRange(Facturas);
                    Ventas.AddRange(Recibos);
                }
                catch (Exception)
                {
                }

                return Ventas;
            }

            public FacturaModel ObtenerFacturaxSerie(long serieId, long factura)
            {
                FacturaModel FacturaActual = new FacturaModel();
           
                try
                {
                    FacturaActual = db.Set<Factura>().Include("Serie").Include("Agencia").Include("Detalles").Where(x => x.SerieId == serieId && x.NoFactura == factura).AsEnumerable().Select(x => new FacturaModel() { FacturaId = x.FacturaId, Documento = string.Format("{0} - {1}", x.Serie.Nombre, x.NoFactura), Dias = x.DiaCredito, Fecha = x.Fecha, Agencia = x.Agencia.Nombre, ClienteId = x.ClienteId, Descuento = 0, Total = x.Detalles.Sum(y => y.Cantidad * y.Precio), Anulada = x.Anulada }).AsEnumerable().Select(F => F).Join(db.Set<Cliente>(), S => S.ClienteId, C => C.ClienteId, (S, C) => new FacturaModel() { FacturaId = S.FacturaId, Documento = S.Documento, Fecha = S.Fecha, Agencia = S.Agencia, Nombre = S.Anulada ? "Factura Anulada" : string.Format("{0} - Factura", C.Nombre), Descuento = S.Anulada ? 0 : (Convert.ToDecimal(S.Descuento) / Convert.ToDecimal(100)) * S.Total, Total = S.Anulada ? 0 : S.Total, TotalLiquido = S.Anulada ? 0 : S.Descuento == 0 ? S.Total : (S.Total - (Convert.ToDecimal(S.Descuento) / Convert.ToDecimal(100)) * S.Total) }).FirstOrDefault();
                }
                catch (Exception)
                {
                }

                return FacturaActual;
            }

            public List<DescuentoModel> ObtenerPorcentajeDescuento() 
            {
                List<DescuentoModel> Descuentos = new List<DescuentoModel>();

                try
                {
                    Configuracion Valores = db.Set<Configuracion>().Where(x => x.Identificador.Equals("DescuentoArticulo")).FirstOrDefault();
                    if (Valores != null)
                    {
                        string[] Porcentajes = Valores.Valor.Split(',');
                        if (Porcentajes != null && Porcentajes.Count() > 0)
                        {
                            foreach (var item in Porcentajes)
                            {
                                Descuentos.Add(new DescuentoModel() { DescuentoId = Convert.ToDecimal(item) , Valor = string.Format("{0}%", item) });                              
                            }                        
                        }
                    }
                    else
                    {
                        Descuentos.Add(new DescuentoModel() { DescuentoId = 0, Valor = "0%" });
                    }
                }
                catch (Exception)
                {
                }

                return Descuentos;
            }

            public List<CreditoModel> ObtenerDiasCredito()
            {
                List<CreditoModel> Dias = new List<CreditoModel>();

                try
                {
                    Configuracion Valores = db.Set<Configuracion>().Where(x => x.Identificador.Equals("DiasCredito")).FirstOrDefault();
                    if (Valores != null)
                    {
                        string[] Porcentajes = Valores.Valor.Split(',');
                        if (Porcentajes != null && Porcentajes.Count() > 0)
                        {
                            foreach (var item in Porcentajes)
                            {
                                Dias.Add(new CreditoModel() { CreditoId = Convert.ToInt32(item), Nombre = string.Format("{0} dia(s)", item) });
                            }
                        }
                    }
                    else
                    {
                        Dias.Add(new CreditoModel() { CreditoId = 0, Nombre = "0 dia(s)" });
                    }
                }
                catch (Exception)
                {
                }

                return Dias;
            }

            public List<VentaResumen> ObtenerVentasResumenxTienda(DateTime fechaInicial, DateTime fechaFinal, long agenciaId)
            {
                List<VentaResumen> Ventas = new List<VentaResumen>();
                List<long> AgenciaIds = new List<long>();

                try
                {
                    AgenciaIds.Add(agenciaId);
                    Ventas = db.Set<Factura>().AsNoTracking().Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && !x.Anulada && AgenciaIds.Contains(x.AgenciaId)).Join(db.Set<FacturaDetalle>(), F => F.FacturaId, FD => FD.FacturaId, (F, FD) => new VentaResumen() { Fecha = F.Fecha, Monto = FD.Cantidad * FD.Precio }).OrderBy(x => x.Fecha).ToList();
                    List<VentaResumen> FormasdePago = db.Set<Factura>().AsNoTracking().Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && !x.Anulada && AgenciaIds.Contains(x.AgenciaId)).Join(db.Set<FacturaFormaPago>().AsNoTracking(), F => F.FacturaId, FF => FF.FacturaId, (F, FF) => new VentaResumen() { Fecha = F.Fecha, FormaId = FF.FormaPagoId, Monto = FF.Valor }).ToList();
                
                    if (Ventas != null && Ventas.Count() > 0)
                    {
                        Ventas = Ventas.GroupBy(x => x.Fecha).Select(x => new VentaResumen() { Fecha = x.Key, Monto = x.Sum(y => y.Monto) }).ToList();
                        foreach (var Venta in Ventas)
                        {
                            List<VentaResumen> TC = FormasdePago.Where(x => x.FormaId == 20181021001 && x.Fecha == Venta.Fecha).ToList();
                            if (TC != null && TC.Count() > 0)
                            {
                                Venta.TC = TC.Sum(x => x.Monto);
                            }

                            List<VentaResumen> Efectivo = FormasdePago.Where(x => x.FormaId == 20171028001 && x.Fecha == Venta.Fecha).ToList();
                            if (Efectivo != null && Efectivo.Count() > 0)
                            {
                                Venta.Efectivo = Efectivo.Sum(x => x.Monto);                          
                            }

                            List<VentaResumen> EfectivoDolares = FormasdePago.Where(x => x.FormaId == 20170710001 && x.Fecha == Venta.Fecha).ToList();
                            if (EfectivoDolares != null && EfectivoDolares.Count() > 0)
                            {
                                Venta.EfectivoDolar = EfectivoDolares.Sum(x => x.Monto);
                            }

                            List<long> DescatarFormas = new List<long>() { 20181021001, 20171028001, 20170710001 };
                            List<VentaResumen> Otros = FormasdePago.Where(x => !DescatarFormas.Contains(x.FormaId) && x.Fecha == Venta.Fecha).ToList();
                            if (Otros != null && Otros.Count() > 0)
                            {
                                Venta.Otros = Otros.Sum(x => x.Monto);
                            }
                        }                   
                    }
                }
                catch (Exception)
                {
                }
                return Ventas;
            }

            public List<VentaResumen> ObtenerCierreResumen(DateTime fechaInicial, DateTime fechaFinal, long agenciaId)
            {
                List<VentaResumen> Ventas = new List<VentaResumen>();
                List<long> AgenciaIds = new List<long>();

                try
                {
                    AgenciaIds.Add(agenciaId);

                    Ventas = db.Set<Factura>().Include("Serie").AsNoTracking().Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && !x.Anulada && AgenciaIds.Contains(x.AgenciaId)).AsEnumerable().Select(x => new VentaResumen() { FacturaId = x.FacturaId, Factura = string.Format("{0} - {1}", x.Serie.Nombre, x.NoFactura) }).Join(db.Set<FacturaDetalle>(), F => F.FacturaId, FD => FD.FacturaId, (F, FD) => new VentaResumen() { FacturaId = F.FacturaId, Factura = F.Factura, Monto = FD.Cantidad * FD.Precio }).OrderBy(x => x.Factura).ToList();
                    List<VentaResumen> FormasdePago = db.Set<Factura>().AsNoTracking().Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && !x.Anulada && AgenciaIds.Contains(x.AgenciaId)).Join(db.Set<FacturaFormaPago>().AsNoTracking(), F => F.FacturaId, FF => FF.FacturaId, (F, FF) => new VentaResumen() { FacturaId = F.FacturaId, FormaId = FF.FormaPagoId, Monto = FF.Valor }).ToList();

                    if (Ventas != null && Ventas.Count() > 0)
                    {
                        Ventas = Ventas.GroupBy(x => new { x.FacturaId, x.Factura }).Select(x => new VentaResumen() { FacturaId = x.Key.FacturaId, Factura = x.Key.Factura, Monto = x.Sum(y => y.Monto) }).ToList();

                        foreach (var Venta in Ventas)
                        {
                            List<VentaResumen> TC = FormasdePago.Where(x => x.FormaId == 20181021001 && x.FacturaId == Venta.FacturaId).ToList();
                            if (TC != null && TC.Count() > 0)
                            {
                                Venta.TC = TC.Sum(x => x.Monto);
                            }

                            List<VentaResumen> Efectivo = FormasdePago.Where(x => x.FormaId == 20171028001 && x.FacturaId == Venta.FacturaId).ToList();
                            if (Efectivo != null && Efectivo.Count() > 0)
                            {
                                Venta.Efectivo = Efectivo.Sum(x => x.Monto);
                            }

                            List<VentaResumen> EfectivoDolares = FormasdePago.Where(x => x.FormaId == 20170710001 && x.FacturaId == Venta.FacturaId).ToList();
                            if (EfectivoDolares != null && EfectivoDolares.Count() > 0)
                            {
                                Venta.EfectivoDolar = EfectivoDolares.Sum(x => x.Monto);
                            }

                            List<long> DescatarFormas = new List<long>() { 20181021001, 20171028001, 20170710001 };
                            List<VentaResumen> Otros = FormasdePago.Where(x => !DescatarFormas.Contains(x.FormaId) && x.FacturaId == Venta.FacturaId).ToList();
                            if (Otros != null && Otros.Count() > 0)
                            {
                                Venta.Otros = Otros.Sum(x => x.Monto);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                }
                return Ventas;
            }

            public List<LibroVentaModel> ObtenerLibroVenta(DateTime fechaInicial, DateTime fechaFinal, long agenciaId, long usuarioId)
            {
                List<LibroVentaModel> Ventas = new List<LibroVentaModel>();
                List<long> AgenciaIds = new List<long>();

                try
                {
                    if (agenciaId == 0)
                    {
                        AgenciaIds = db.Set<UsuarioAgencia>().Where(x => x.UsuarioId == usuarioId).Select(x => x.AgenciaId).ToList();
                    }
                    else
                    {
                        AgenciaIds.Add(agenciaId);
                    }

                    Ventas = db.Set<Factura>().Include("Agencia").Include("Serie").Include("Cliente").Include("Pagos").Include("Detalles").AsNoTracking().Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && AgenciaIds.Contains(x.AgenciaId)).AsEnumerable().Select(x => new LibroVentaModel() { Fecha = x.Fecha, Agencia = x.Agencia.Nombre, TipoDocumento = "FAC", Serie = x.SerieFEL, NoFactura = x.NumeroFEL, TipoTransaccion = x.Anulada ? "A" : "E", Nit = x.Anulada ? "" : x.Cliente.Nit, Nombre = x.Anulada ? "" : (x.Cliente.Nit == "C/F" ? "C/F" : x.Cliente.Nombre), Total = x.Anulada ? 0 : x.Detalles.Sum(y => y.Cantidad*y.Precio), TotalSinIva = x.Anulada ? 0 : x.Detalles.Sum(y => y.Cantidad * y.Precio) / decimal.Parse("1.12") }).OrderBy(x => x.Fecha).ThenBy(x => x.NoFactura).ToList();
                    Ventas.AddRange(db.Set<NotaCredito>().Include("Agencia").Include("Cliente").Include("Pagos").AsNoTracking().Where(x => x.Operado && x.Fecha >= fechaInicial && x.Fecha <= fechaFinal).AsEnumerable().Select(x => new LibroVentaModel() { Fecha = x.Fecha, Agencia = x.Agencia.Nombre, TipoDocumento = "NC", Serie = x.Serie, NoFactura = x.NoNotaCredito, TipoTransaccion = x.Anulada ? "A" : "E", Nit = x.Anulada ? "" : x.Cliente.Nit, Nombre = x.Anulada ? "" : (x.Cliente.Nit == "C/F" ? "C/F" : x.Cliente.Nombre), Total = x.Anulada ? 0 : x.Pagos.Sum(y => y.Valor), TotalSinIva = x.Anulada ? 0 : x.Pagos.Sum(y => y.Valor) / decimal.Parse("1.12") }));
                }
                catch (Exception)
                {}

                return Ventas;
            }

            public List<ReporteVentaComisionVendedorModel> ReporteVentaComisionVendedor(long agenciaId, long vendedorId, DateTime fechaInicial, DateTime fechaFinal)
            {
                List<ReporteVentaComisionVendedorModel> Ventas = new List<ReporteVentaComisionVendedorModel>();

                try
                {
                    if (agenciaId == 0 && vendedorId != 0)
                    {
                        Ventas = db.Database.SqlQuery<ReporteVentaComisionVendedorModel>("dbo.sp_reporte_venta_comision_vendedor @AgenciaId, @VendedorId, @FechaInicial, @FechaFinal", new SqlParameter("@AgenciaId", DBNull.Value), new SqlParameter("@VendedorId", vendedorId), new SqlParameter("@FechaInicial", fechaInicial), new SqlParameter("@FechaFinal", fechaFinal)).ToList();
                    }
                    else if (agenciaId != 0 && vendedorId == 0)
                    {
                        Ventas = db.Database.SqlQuery<ReporteVentaComisionVendedorModel>("dbo.sp_reporte_venta_comision_vendedor @AgenciaId, @VendedorId, @FechaInicial, @FechaFinal", new SqlParameter("@AgenciaId", agenciaId), new SqlParameter("@VendedorId", DBNull.Value), new SqlParameter("@FechaInicial", fechaInicial), new SqlParameter("@FechaFinal", fechaFinal)).ToList();
                    }                    
                }
                catch (Exception)
                {
                }

                return Ventas;
            }

            public List<ReporteVentaTransporteModel> ReporteVentaTransporte(long transporteId, DateTime fechaInicial, DateTime fechaFinal)
            {
                List<ReporteVentaTransporteModel> Ventas = new List<ReporteVentaTransporteModel>();

                try
                {
                    if (transporteId == 0)
                    {
                        Ventas = db.Database.SqlQuery<ReporteVentaTransporteModel>("dbo.sp_reporte_venta_transporte @TransporteId, @FechaInicial, @FechaFinal", new SqlParameter("@TransporteId", DBNull.Value), new SqlParameter("@FechaInicial", fechaInicial), new SqlParameter("@FechaFinal", fechaFinal)).ToList();
                    }
                    else if (transporteId != 0)
                    {
                        Ventas = db.Database.SqlQuery<ReporteVentaTransporteModel>("dbo.sp_reporte_venta_transporte @TransporteId, @FechaInicial, @FechaFinal", new SqlParameter("@TransporteId", transporteId), new SqlParameter("@FechaInicial", fechaInicial), new SqlParameter("@FechaFinal", fechaFinal)).ToList();
                    }
                }
                catch (Exception)
                {
                }

                return Ventas;
            }

            public FacturaGarantia ObtenerProductosFactura(long serieId, long factura)
            {
                FacturaGarantia FacturaActual = new FacturaGarantia();

                try
                {
                    Factura Factura = db.Set<Factura>().Include("Cliente").Include("Detalles").Include("Detalles.Producto").AsNoTracking().Where(x => x.SerieId == serieId && x.NoFactura == factura).FirstOrDefault();
                    if (Factura != null)
                    {
                        if (Factura.Anulada)
                        {
                            FacturaActual.MensajeId = -2;
                        }
                        else
                        {
                            FacturaActual.MensajeId = 1;
                            FacturaActual.FacturaId = Factura.FacturaId;
                            FacturaActual.Cliente = Factura.Cliente.Nombre;

                            if (Factura.Detalles != null && Factura.Detalles.Count() > 0)
                            {
                                FacturaActual.Productos = new List<Producto>();
                                foreach (var item in Factura.Detalles)
                                {
                                    FacturaActual.Productos.Add(item.Producto);
                                }
                            }
                        }
                    }
                    else
                    {
                        FacturaActual.MensajeId = -1;
                    }
                }
                catch (Exception)
                {
                }

                return FacturaActual;
            }

            public List<ReporteCierreTransporteModel> ReporteCierreTransporte(long transporteId, DateTime fechaInicial, DateTime fechaFinal)
            {
                List<ReporteCierreTransporteModel> Ventas = new List<ReporteCierreTransporteModel>();

                try
                {
                    if (transporteId == 0)
                    {
                        Ventas = db.Database.SqlQuery<ReporteCierreTransporteModel>("dbo.sp_reporte_cierre_x_transporte @TransporteId, @FechaInicial, @FechaFinal", new SqlParameter("@TransporteId", DBNull.Value), new SqlParameter("@FechaInicial", fechaInicial), new SqlParameter("@FechaFinal", fechaFinal)).ToList();
                    }
                    else if (transporteId != 0)
                    {
                        Ventas = db.Database.SqlQuery<ReporteCierreTransporteModel>("dbo.sp_reporte_cierre_x_transporte @TransporteId, @FechaInicial, @FechaFinal", new SqlParameter("@TransporteId", transporteId), new SqlParameter("@FechaInicial", fechaInicial), new SqlParameter("@FechaFinal", fechaFinal)).ToList();
                    }
                }
                catch (Exception)
                {
                }

                return Ventas;
            }

            public List<ReporteVentaxTipoCliente> ReporteVentaxTipoCliente(long agenciaId, long tipoId, DateTime fechaInicial, DateTime fechaFinal)
            {
                List<ReporteVentaxTipoCliente> Ventas = new List<ReporteVentaxTipoCliente>();

                try
                {
                    if (agenciaId == 0 && tipoId != 0)
                    {
                        Ventas = db.Database.SqlQuery<ReporteVentaxTipoCliente>("dbo.sp_reporte_venta_x_tipo_cliente @AgenciaId, @TipoId, @FechaInicial, @FechaFinal", new SqlParameter("@AgenciaId", DBNull.Value), new SqlParameter("@TipoId", tipoId), new SqlParameter("@FechaInicial", fechaInicial), new SqlParameter("@FechaFinal", fechaFinal)).ToList();
                    }
                    else if (agenciaId != 0 && tipoId == 0)
                    {
                        Ventas = db.Database.SqlQuery<ReporteVentaxTipoCliente>("dbo.sp_reporte_venta_x_tipo_cliente @AgenciaId, @TipoId, @FechaInicial, @FechaFinal", new SqlParameter("@AgenciaId", agenciaId), new SqlParameter("@TipoId", DBNull.Value), new SqlParameter("@FechaInicial", fechaInicial), new SqlParameter("@FechaFinal", fechaFinal)).ToList();
                    }
                    else if (agenciaId != 0 && tipoId != 0)
                    {
                        Ventas = db.Database.SqlQuery<ReporteVentaxTipoCliente>("dbo.sp_reporte_venta_x_tipo_cliente @AgenciaId, @TipoId, @FechaInicial, @FechaFinal", new SqlParameter("@AgenciaId", agenciaId), new SqlParameter("@TipoId", tipoId), new SqlParameter("@FechaInicial", fechaInicial), new SqlParameter("@FechaFinal", fechaFinal)).ToList();
                    }
                    else if (agenciaId == 0 && tipoId == 0)
                    {
                        Ventas = db.Database.SqlQuery<ReporteVentaxTipoCliente>("dbo.sp_reporte_venta_x_tipo_cliente @AgenciaId, @TipoId, @FechaInicial, @FechaFinal", new SqlParameter("@AgenciaId", DBNull.Value), new SqlParameter("@TipoId", DBNull.Value), new SqlParameter("@FechaInicial", fechaInicial), new SqlParameter("@FechaFinal", fechaFinal)).ToList();
                    }
                }
                catch (Exception)
                {
                }

                return Ventas;
            }

            public List<ReporteGraficoVentaxTipoCliente> ReporteGraficaVentaxTipoCliente(long agenciaId, DateTime fechaInicial, DateTime fechaFinal)
            {
                List<ReporteVentaxTipoCliente> Ventas = new List<ReporteVentaxTipoCliente>();
                List<ReporteGraficoVentaxTipoCliente> VentasIDs = new List<ReporteGraficoVentaxTipoCliente>();

                try
                {
                    if (agenciaId == 0)
                    {
                        Ventas = db.Database.SqlQuery<ReporteVentaxTipoCliente>("dbo.sp_reporte_venta_x_tipo_cliente @AgenciaId, @TipoId, @FechaInicial, @FechaFinal", new SqlParameter("@AgenciaId", DBNull.Value), new SqlParameter("@TipoId", DBNull.Value), new SqlParameter("@FechaInicial", fechaInicial), new SqlParameter("@FechaFinal", fechaFinal)).ToList();
                    }
                    else if (agenciaId != 0)
                    {
                        Ventas = db.Database.SqlQuery<ReporteVentaxTipoCliente>("dbo.sp_reporte_venta_x_tipo_cliente @AgenciaId, @TipoId, @FechaInicial, @FechaFinal", new SqlParameter("@AgenciaId", agenciaId), new SqlParameter("@TipoId", DBNull.Value), new SqlParameter("@FechaInicial", fechaInicial), new SqlParameter("@FechaFinal", fechaFinal)).ToList();
                    }

                    if (Ventas != null && Ventas.Count() > 0)
                    {
                        VentasIDs = Ventas.GroupBy(x => x.Tipo).Select(x => new ReporteGraficoVentaxTipoCliente { Tipo = x.Key, Cantidad = x.Count() }).ToList();
                        VentasIDs = VentasIDs.OrderByDescending(x => x.Cantidad).ToList();
                    }
                }
                catch (Exception)
                {
                }

                return VentasIDs;
            }

            public List<ReporteVentaComisionxVendedorConfigurable> ReporteVentaComisionxVendedorConfigurable(long vendedorId, DateTime fechaInicial, DateTime fechaFinal)
            {
                List<ReporteVentaComisionxVendedorConfigurable> Ventas = new List<ReporteVentaComisionxVendedorConfigurable>();
             
                try
                {
                    if (vendedorId == 0)
                    {
                        Ventas = db.Database.SqlQuery<ReporteVentaComisionxVendedorConfigurable>("dbo.sp_reporte_venta_comision_vendedor_configurable @VendedorId, @FechaInicial, @FechaFinal", new SqlParameter("@VendedorId", DBNull.Value), new SqlParameter("@FechaInicial", fechaInicial), new SqlParameter("@FechaFinal", fechaFinal)).ToList();
                    }
                    else if (vendedorId != 0)
                    {
                        Ventas = db.Database.SqlQuery<ReporteVentaComisionxVendedorConfigurable>("dbo.sp_reporte_venta_comision_vendedor_configurable @VendedorId, @FechaInicial, @FechaFinal", new SqlParameter("@VendedorId", vendedorId), new SqlParameter("@FechaInicial", fechaInicial), new SqlParameter("@FechaFinal", fechaFinal)).ToList();
                    }
                }
                catch (Exception)
                {
                }

                return Ventas;
            }

            public List<ReporteVentaxFormaPago> ReporteVentaxFormaPago(long agenciaId, long formaId, DateTime fechaInicial, DateTime fechaFinal)
            {
                List<ReporteVentaxFormaPago> Ventas = new List<ReporteVentaxFormaPago>();

                try
                {
                    if (formaId == 0)
                    {
                        Ventas = db.Database.SqlQuery<ReporteVentaxFormaPago>("dbo.sp_reporte_venta_x_forma_pago @AgenciaId, @FormaId, @FechaInicial, @FechaFinal", new SqlParameter("@AgenciaId", agenciaId), new SqlParameter("@FormaId", DBNull.Value), new SqlParameter("@FechaInicial", fechaInicial), new SqlParameter("@FechaFinal", fechaFinal)).ToList();
                    }
                    else if (formaId != 0)
                    {
                        Ventas = db.Database.SqlQuery<ReporteVentaxFormaPago>("dbo.sp_reporte_venta_x_forma_pago @AgenciaId, @FormaId, @FechaInicial, @FechaFinal", new SqlParameter("@AgenciaId", agenciaId), new SqlParameter("@FormaId", formaId), new SqlParameter("@FechaInicial", fechaInicial), new SqlParameter("@FechaFinal", fechaFinal)).ToList();
                    }
                }
                catch (Exception)
                {
                }

                return Ventas;
            }

            public string GenerarNotaCredito(Factura entidad)
            {                
                string Mensaje = "OK";

                try
                {
                    if (entidad.Detalles != null && entidad.Detalles.Count() > 0)
                    {
                        FacturaNotaCredito NotaCredito = new FacturaNotaCredito();
                        NotaCredito.FacturaId = entidad.FacturaId;
                        NotaCredito.Motivo = entidad.Comentario;
                        NotaCredito.Infile = false;
                        NotaCredito.CantidadErroresFEL = 0;
                        NotaCredito.UsrCreo = entidad.UsrCreo;
                        NotaCredito.Fecha = DateTime.Today;
                        NotaCredito.FechaHoraNotaCredito = DateTime.Now;

                        NotaCredito.Detalles = new List<FacturaNotaCreditoDetalle>();

                        int DetalleId = 1;
                        foreach (FacturaDetalle DetalleActual in entidad.Detalles)
                        {
                            NotaCredito.Detalles.Add(new FacturaNotaCreditoDetalle() { DetalleId = DetalleId, FacturaId = entidad.FacturaId, ProductoId = DetalleActual.ProductoId, UnidadId = DetalleActual.UnidadId, Nombre = DetalleActual.Nombre, Cantidad = DetalleActual.Cantidad, Precio = DetalleActual.Precio });
                            DetalleId++;
                        }

                        db.Set<FacturaNotaCredito>().Add(NotaCredito);
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                if (Mensaje.Equals("OK"))
                {
                    try
                    {
                        //Se verifica el certificador que se encuentra habilitado  
                        Configuracion ConfiguracionCertificador = db.Set<Configuracion>().AsNoTracking().Where(x => x.ConfiguracionId == 20191010015).FirstOrDefault();
                        if (ConfiguracionCertificador != null)
                        {
                            if (ConfiguracionCertificador.Valor.Equals("2"))
                            {
                                Mensaje = GenerarNotaCreditoDIGIFACT(new Factura() { FacturaId = entidad.FacturaId });
                            }
                            else
                            {
                                return "El certificador que se encuentra configurado no es valido";
                            }
                        }

                        if (!Mensaje.Equals("OK"))
                        {
                            Mensaje = "OK";
                        }
                    }
                    catch (Exception ex)
                    {
                        Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                    }
                }

                return Mensaje;
            }

            public FacturaNotaCredito ObtenerNotaCreditoxId(long id)
            {
                FacturaNotaCredito NotaCreditoActual = new FacturaNotaCredito();

                try
                {
                    NotaCreditoActual = db.Set<FacturaNotaCredito>().Include("Detalles").Include("Detalles.Producto").Include("Detalles.Unidad").AsNoTracking().Where(x => x.FacturaId == id).FirstOrDefault();
                }
                catch (Exception)
                { }

                return NotaCreditoActual;
            }

            public string EnviarCorreo(long id)
            {
                string Mensaje = "OK";
                string CorreoNotificacion = "";
                string MensajeNotificacion = "";

                string CuerpoMensaje = "";
                string EnlanceMensaje = "";

                try
                {
                    //Se obtiene la configuracion del correo
                    Configuracion ConfiguracionActual = db.Set<Configuracion>().AsNoTracking().Where(x => x.ConfiguracionId == 20170611001).FirstOrDefault();
                    if (ConfiguracionActual != null)
                    {
                        CorreoNotificacion = ConfiguracionActual.Valor;
                    }

                    //Se obtiene la configuracion del correo
                    Configuracion ConfiguracionCuerpoActual = db.Set<Configuracion>().AsNoTracking().Where(x => x.ConfiguracionId == 20210709001).FirstOrDefault();
                    if (ConfiguracionCuerpoActual != null)
                    {
                        CuerpoMensaje = ConfiguracionCuerpoActual.Valor;
                    }

                    //Se obtiene la configuracion del correo
                    Configuracion ConfiguracionEnlanceActual = db.Set<Configuracion>().AsNoTracking().Where(x => x.ConfiguracionId == 20210709002).FirstOrDefault();
                    if (ConfiguracionEnlanceActual != null)
                    {
                        EnlanceMensaje = ConfiguracionEnlanceActual.Valor;
                    }

                    //Se obtiene la factura
                    Factura FacturaActual = db.Set<Factura>().Include("Cliente").AsNoTracking().Where(x => x.FacturaId == id).FirstOrDefault();
                    if (FacturaActual != null)
                    {
                        if (FacturaActual.Cliente != null)
                        {
                            if (!string.IsNullOrWhiteSpace(FacturaActual.Cliente.EmailCliente))
                            {
                                if (!FacturaActual.Cliente.EmailCliente.Equals("sincorreo@sincorreo.com"))
                                {
                                    CorreoNotificacion = FacturaActual.Cliente.EmailCliente;
                                }
                            }
                        }

                        MensajeNotificacion = string.Format("FACTURA ELECTRONICA: SERIE: {0} - NUMERO: {1}", FacturaActual.SerieFEL, FacturaActual.NumeroFEL);

                        Herramienta.EnviarCorreoAsync(string.Format(CuerpoMensaje, MensajeNotificacion, string.Format(EnlanceMensaje, id)), CorreoNotificacion);
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            public int ObtenerSaldoFacturas(long empresaId) 
            {
                int SaldoActual = 0;

                try
                {                    
                    List<PaqueteEmpresa> PaquetesDisponibles = db.Set<PaqueteEmpresa>().AsNoTracking().Where(x => x.EmpresaId == empresaId && x.FechaVencimiento >= DateTime.Today && x.SaldoFactura > 0).ToList();
                    if (PaquetesDisponibles != null && PaquetesDisponibles.Count() > 0)
                    {
                        PaquetesDisponibles.ForEach(x =>
                        {
                            SaldoActual += x.SaldoFactura;
                        });
                    }                
                }
                catch (Exception)
                {}

                return SaldoActual;
            }

        #endregion
    }
}
