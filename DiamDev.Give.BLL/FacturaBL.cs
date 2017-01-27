using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
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
                {
                }

                return Id;
            }

            private bool Agregar(Factura entidad)
            {
                bool FacturaAgregar = false;

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
                            
                            //Cambiar de estado a la factura
                            SerieAgenciaFactura FacturaActual = db.Set<SerieAgenciaFactura>().Where(x => x.AgenciaId == entidad.AgenciaId && x.SerieId == entidad.SerieId && x.Factura == entidad.NoFactura).FirstOrDefault();
                            if (FacturaActual != null)
                            {
                                FacturaActual.Operada = true;                                
                            }
      
                            if (entidad.Detalles != null && entidad.Detalles.Count() > 0)
                            {
                                int DetalleId = 1;
                                foreach (var Producto in entidad.Detalles)
                                {
                                    Producto.DetalleId = DetalleId;
                                    Producto.FacturaId = entidad.FacturaId;

                                    //Se obtiene el producto para convercion
                                    Producto ProductoPadreActual = new Producto();
                                    Producto ProductoHijoActual = new Producto();
                                    bool UnidadPadre = false;
                                    decimal Cantidad = Producto.Cantidad;                                   
                                    decimal CantidadOriginal = 0;

                                    ProductoPadreActual = db.Set<Producto>().Where(x => x.ProductoId == Producto.ProductoId).FirstOrDefault();

                                    if (ProductoPadreActual != null)
                                    {
                                        if (ProductoPadreActual.UnidadId == Producto.UnidadId)
                                        {
                                            UnidadPadre = true;
                                            CantidadOriginal = ProductoPadreActual.Cantidad;
                                        }
                                    }

                                    if (!UnidadPadre)
                                    {
                                        ProductoHijoActual = db.Set<Producto>().Where(x => x.ProductoPadreId == Producto.ProductoId && x.UnidadId == Producto.UnidadId).FirstOrDefault();

                                        if (ProductoHijoActual != null)
                                        {
                                            Cantidad *= ProductoHijoActual.Cantidad;
                                            CantidadOriginal = ProductoHijoActual.Cantidad;
                                        }
                                    }

                                    ProductoInventario InventarioActual = db.Set<ProductoInventario>().Where(x => x.ProductoId == Producto.ProductoId && x.AgenciaId == entidad.AgenciaId).FirstOrDefault();
                                    if (InventarioActual != null)
                                    {
                                        InventarioActual.Cantidad -= Cantidad;                                        
                                    }

                                    //Agrega el precio costo al producto
                                    ProductoPrecioCosto CostoActual = db.Set<ProductoPrecioCosto>().Where(x => x.ProductoId == Producto.ProductoId).FirstOrDefault();
                                    if (CostoActual != null)
                                    {
                                        Producto.PrecioCosto = decimal.Round(CostoActual.PrecioCosto * CantidadOriginal,2);    
                                    }
                                    
                                    DetalleId += 1;
                                }
                            }

                            if (entidad.Pagos != null && entidad.Pagos.Count() > 0)
                            {
                                foreach (var Pago in entidad.Pagos)
                                {
                                    Pago.FacturaId = entidad.FacturaId;                                    
                                }                                
                            }

                            db.Set<Factura>().Add(entidad);
                            db.SaveChanges();
                            FacturaAgregar = true;
                        }
                    }

                }
                catch (Exception)
                {
                }

                return FacturaAgregar;
            }

        #endregion

        #region Metodos Publicos

            public string Guardar(Factura entidad)
            {
                string Mensaje = "OK";
                bool OperacionExitosa = false;

                if (entidad.FacturaId > 0)
                {
                }
                else
                {
                    OperacionExitosa = Agregar(entidad);
                }

                if (!OperacionExitosa)
                {
                    Mensaje = "La información ingresada no es valida";
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
                            FacturaActual = db.Set<Factura>().Include("Serie").Include("Agencia").Include("Cliente").Include("Detalles").Include("Detalles.Producto").Include("Detalles.Unidad").Include("Pagos").Include("Pagos.FormaPago").Where(x => x.FacturaId == id).FirstOrDefault();
                            if (totalizar)
                            {
                                if (FacturaActual != null)
                                {
                                    FacturaActual.DescuentoTotal = FacturaActual.Descuento == 0 ? 0 : (Convert.ToDecimal(FacturaActual.Descuento) / Convert.ToDecimal(100) * FacturaActual.Detalles.Sum(x => x.Cantidad * x.Precio));
                                    FacturaActual.Total = FacturaActual.Detalles.Sum(x => x.Cantidad * x.Precio) - FacturaActual.DescuentoTotal;
                                }
                            }
                        }
                        else
                        {
                            FacturaActual = db.Set<Factura>().Include("Serie").Include("Agencia").Include("Cliente").Include("Detalles").Include("Detalles.Producto").Include("Detalles.Unidad").Include("Pagos").Include("Pagos.FormaPago").Where(x => x.FacturaId == id && x.FacturaElectronica == electronica).FirstOrDefault();                        
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

            public List<Factura> ObtenerListadoPorFecha(DateTime fechaInicial, DateTime fechaFinal, long usuarioId)
            {
                List<Factura> Facturas = new List<Factura>();

                try
                {
                    var AgenciasIds = db.Set<UsuarioAgencia>().Where(x => x.UsuarioId == usuarioId).AsEnumerable().Select(x => x.AgenciaId).ToList();
                    if (AgenciasIds != null && AgenciasIds.Count() > 0)
                    {
                        Facturas = db.Set<Factura>().Include("Serie").Include("Agencia").Include("Cliente").Include("Detalles").Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && AgenciasIds.Contains(x.AgenciaId)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.FacturaId).ToList();
                    }
                }
                catch (Exception)
                {
                }

                return Facturas;
            }
           
            public string Anular(long facturaId, string comentario, long usuarioId)
            {
                string Mensaje = "OK";

                try
                {

                    Factura FacturaActual = db.Set<Factura>().Include("Cliente").Include("Detalles").Include("Detalles.Producto").Include("Detalles.Unidad").Where(x => x.FacturaId == facturaId).FirstOrDefault();
                    if (FacturaActual == null)
                    {
                        return "La factura no puede ser anular, verificar factura";
                    }                   
                                        
                    FacturaActual.Comentario = comentario;
                    FacturaActual.Anulada = true;
                    FacturaActual.UsrAnular = usuarioId;

                    foreach (var Producto in FacturaActual.Detalles)
                    {
                        //Se obtiene el producto para convercion
                        Producto ProductoPadreActual = new Producto();
                        Producto ProductoHijoActual = new Producto();
                        bool UnidadPadre = false;
                        decimal Cantidad = Producto.Cantidad;

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
                            InventarioActual.Cantidad += Cantidad;                            
                        }
                    }

                    db.SaveChanges();                   
                }
                catch (Exception)
                {
                    Mensaje = "Ocurrio un error, al anular la factura";
                }

                return Mensaje;
            }

            public List<FacturaModel> ObtenerFactura(DateTime fechaInicial, DateTime fechaFinal, long agenciaId, long usuarioId)
            {
                List<FacturaModel> Facturas = new List<FacturaModel>();
                List<FacturaModel> Egresos = new List<FacturaModel>();                
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
                    Facturas = db.Set<Factura>().Include("Serie").Include("Agencia").Include("Detalles").Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && AgenciaIds.Contains(x.AgenciaId)).AsEnumerable().Select(x => new FacturaModel() { FacturaId = x.FacturaId, Documento = string.Format("{0} - {1}",x.Serie.Nombre, x.NoFactura), Fecha = x.Fecha, Agencia = x.Agencia.Nombre, ClienteId = x.ClienteId, Descuento = 0, Total = x.Detalles.Sum(y => y.Cantidad * y.Precio), Anulada = x.Anulada }).AsEnumerable().Select(F => F).Join(db.Set<Cliente>(), S => S.ClienteId, C => C.ClienteId, (S, C) => new FacturaModel() { FacturaId = S.FacturaId, Documento = S.Documento, Fecha = S.Fecha, Agencia = S.Agencia, Nombre = S.Anulada ? "Factura Anulada" : string.Format("{0} - Factura", C.Nombre), Descuento = S.Anulada ? 0 : (Convert.ToDecimal(S.Descuento) / Convert.ToDecimal(100)) * S.Total, Total = S.Anulada ? 0 : S.Total, TotalLiquido = S.Anulada ? 0 : S.Descuento == 0 ? S.Total : (S.Total - (Convert.ToDecimal(S.Descuento) / Convert.ToDecimal(100)) * S.Total) }).ToList();

                    //Egresos
                    Egresos = db.Set<Movimiento>().Include("Agencia").Include("Detalles").Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && x.MovimientoTipoId == 2 && AgenciaIds.Contains(x.AgenciaId)).AsEnumerable().Select(x => new FacturaModel() { FacturaId = x.MovimientoId, Documento = "EG", Fecha = x.Fecha, Agencia = x.Agencia.Nombre, ClienteId = x.ClienteId.Value, Descuento = x.Descuento, Total = x.Detalles.Sum(y => y.Cantidad * y.Precio), Anulada = false }).AsEnumerable().Select(F => F).Join(db.Set<Cliente>(), S => S.ClienteId, C => C.ClienteId, (S, C) => new FacturaModel() { FacturaId = S.FacturaId, Documento = S.Documento, Fecha = S.Fecha, Agencia = S.Agencia, Nombre = S.Anulada ? "Factura Anulada" : string.Format("{0} - Egreso", C.Nombre), Descuento = S.Anulada ? 0 : (Convert.ToDecimal(S.Descuento) / Convert.ToDecimal(100)) * S.Total, Total = S.Anulada ? 0 : S.Total, TotalLiquido = S.Anulada ? 0 : S.Descuento == 0 ? S.Total : (S.Total - (Convert.ToDecimal(S.Descuento) / Convert.ToDecimal(100)) * S.Total) }).ToList();

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

                    if (Egresos != null && Egresos.Count() > 0)
                    {
                        foreach (var Egreso in Egresos)
                        {
                            List<string> Formas = db.Set<MovimientoFormaPago>().Include("FormaPago").Where(x => x.MovimientoId == Egreso.FacturaId).AsEnumerable().Select(x => string.Format("{0} - {1}", x.FormaPago.Nombre, x.Valor.ToString("C"))).ToList();
                            if (Formas != null && Formas.Count() > 0)
                            {
                                foreach (var item in Formas)
                                {
                                    Egreso.Forma += string.Format("{0}\n", item);
                                }
                            }
                            Facturas.Add(Egreso);
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
                    List<FormaModel> FacturasIds = db.Set<Factura>().Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && x.Anulada == false && AgenciaIds.Contains(x.AgenciaId)).Join(db.Set<FacturaFormaPago>(), F => F.FacturaId, FF => FF.FacturaId, (F, FF) => new { F, FF }).GroupBy(r => r.FF.FormaPagoId).Select(x => new FormaModel { FacturaId = x.Key, Total = x.Sum(g => g.FF.Valor) }).ToList();
                    if (FacturasIds != null && FacturasIds.Count() > 0)
                    {
                        Formas = FacturasIds.Join(db.Set<FormaPago>(), F => F.FacturaId, F => F.FormaPagoId, (R, F) => new FormaPago() { FormaPagoId = F.FormaPagoId, Nombre = F.Nombre, Valor = R.Total }).ToList();
                    }

                    //Egresos
                    FacturasIds = db.Set<Movimiento>().Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && x.MovimientoTipoId == 2 && AgenciaIds.Contains(x.AgenciaId)).Join(db.Set<MovimientoFormaPago>(), F => F.MovimientoId, FF => FF.MovimientoId, (F, FF) => new { F, FF }).GroupBy(r => r.FF.FormaPagoId).Select(x => new FormaModel { FacturaId = x.Key, Total = x.Sum(g => g.FF.Valor) }).ToList();
                    if (FacturasIds != null && FacturasIds.Count() > 0)
                    {
                        Formas.AddRange(FacturasIds.Join(db.Set<FormaPago>(), F => F.FacturaId, F => F.FormaPagoId, (R, F) => new FormaPago() { FormaPagoId = F.FormaPagoId, Nombre = F.Nombre, Valor = R.Total }));
                    }

                    if (Formas != null && Formas.Count() > 0)
                    {
                        Formas = Formas.GroupBy(x => new { x.FormaPagoId, x.Nombre }).Select(g => new FormaPago() { FormaPagoId = g.Key.FormaPagoId, Nombre = g.Key.Nombre, Valor = g.Sum(y => y.Valor) }).ToList();
                    }
                }
                catch (Exception)
                {
                }

                return Formas;
            }

        #endregion
    }
}
