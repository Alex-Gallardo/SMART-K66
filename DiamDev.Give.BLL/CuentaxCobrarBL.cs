using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class CuentaxCobrarBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public CuentaxCobrarBL()
            {
                this.db = new GiveContext();
            }

        #endregion

        #region Metodos Privados
        #endregion

        #region Metodos Publicos

            public CuentaxCobrarModel BuscarNoPagadas(string search, long agenciaId)
            {
                CuentaxCobrarModel Cuentas = new CuentaxCobrarModel();

                Cuentas.Recibos = new List<Recibo>();
                Cuentas.Facturas = new List<Factura>();

                long id = 0;

                try
                {
                    long.TryParse(search, out id);

                //Recibos
                //if (id > 0)
                //{
                //    Cuentas.Recibos = db.Set<Recibo>().Include("Tipo").Include("Agencia").Include("Transporte").Include("Vendedor").Include("Cliente").Include("Detalles").Include("Pagos").AsNoTracking().Where(x => x.ReciboId == id && x.AgenciaId == agenciaId && !x.Anulada && !x.Pagada && x.Despachado).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ReciboId).ToList();
                //}
                //else
                //{
                //    Cuentas.Recibos = db.Set<Recibo>().Include("Tipo").Include("Agencia").Include("Transporte").Include("Vendedor").Include("Cliente").Include("Detalles").Include("Pagos").AsNoTracking().Where(x => (x.Agencia.Nombre.ToLower().Contains(search.ToLower()) || x.Cliente.Nombre.ToLower().Contains(search.ToLower())) && x.AgenciaId == agenciaId && !x.Anulada && !x.Pagada && x.Despachado).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ReciboId).ToList();
                //}

                //Facturas
                if (id > 0)
                {
                    Cuentas.Facturas = db.Set<Factura>().Include("Tipo").Include("Serie").Include("Agencia").Include("Vendedor").Include("Cliente").Include("Detalles").Include("Pagos").AsNoTracking().Where(x => x.FacturaId == id && x.AgenciaId == agenciaId && !x.Anulada && !x.Pagada && x.Despachado).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.FacturaId).ToList();
                }
                else
                {
                    Cuentas.Facturas = db.Set<Factura>().Include("Tipo").Include("Serie").Include("Agencia").Include("Vendedor").Include("Cliente").Include("Detalles").Include("Pagos").AsNoTracking().Where(x => (x.Agencia.Nombre.ToLower().Contains(search.ToLower()) || x.Cliente.Nombre.ToLower().Contains(search.ToLower())) && x.AgenciaId == agenciaId && !x.Anulada && !x.Pagada && x.Despachado).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.FacturaId).ToList();
                }

                //Recibos
                //if (Cuentas.Recibos != null && Cuentas.Recibos.Count() > 0)
                //    {
                //        Cuentas.Recibos.ForEach(x =>
                //        {
                //            if (x.Pagos != null && x.Pagos.Count() > 0)
                //            {
                //                x.Abono = x.Pagos.Sum(y => y.Valor);
                //            }

                //            //Se verifica que contiene factura enlazada
                //            if (x.Factura)
                //            {
                //                Factura FacturaActual = db.Set<Factura>().AsNoTracking().Where(y => y.ReciboId == x.ReciboId).FirstOrDefault();
                //                if (FacturaActual != null)
                //                {
                //                    x.NoFactura = string.Format("{0} - {1}", FacturaActual.SerieFEL, FacturaActual.NumeroFEL);
                //                }
                //            }
                //        });
                //    }                   

                    //Facturas
                    if (Cuentas.Facturas != null && Cuentas.Facturas.Count() > 0)
                    {
                        Cuentas.Facturas.ForEach(x =>
                        {
                            if (x.Pagos != null && x.Pagos.Count() > 0)
                            {
                                x.Abono = x.Pagos.Sum(y => y.Valor);
                            }
                        });
                    }
                }
                catch (Exception)
                {}

                return Cuentas;
            }

            public CuentaxCobrarModel BuscarNoPagadasxCliente(long clienteId, long agenciaId)
            {
                CuentaxCobrarModel Cuentas = new CuentaxCobrarModel();

                Cuentas.Recibos = new List<Recibo>();
                Cuentas.Facturas = new List<Factura>();

                try
                {
                //Recibos
                //Cuentas.Recibos = db.Set<Recibo>().Include("Tipo").Include("Agencia").Include("Vendedor").Include("Cliente").Include("Detalles").Include("Pagos").AsNoTracking().Where(x => x.ClienteId == clienteId && x.AgenciaId == agenciaId && !x.Anulada && !x.Pagada && x.Despachado).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ReciboId).ToList();

                //Facturas
                Cuentas.Facturas = db.Set<Factura>().Include("Tipo").Include("Serie").Include("Agencia").Include("Vendedor").Include("Cliente").Include("Detalles").Include("Pagos").AsNoTracking().Where(x => x.ClienteId == clienteId && x.AgenciaId == agenciaId && !x.Anulada && !x.Pagada && x.Despachado).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.FacturaId).ToList();

                //Recibos
                //if (Cuentas.Recibos != null && Cuentas.Recibos.Count() > 0)
                //    {
                //        Cuentas.Recibos.ForEach(x =>
                //        {
                //            x.HabilitarCheck = true;

                //            if (x.Pagos != null && x.Pagos.Count() > 0)
                //            {
                //                x.Abono = x.Pagos.Sum(y => y.Valor);
                //            }

                //            //Se verifica que contiene factura enlazada
                //            if (x.Factura)
                //            {
                //                Factura FacturaActual = db.Set<Factura>().AsNoTracking().Where(y => y.ReciboId == x.ReciboId).FirstOrDefault();
                //                if (FacturaActual != null)
                //                {
                //                    x.NoFactura = string.Format("{0} - {1}", FacturaActual.SerieFEL, FacturaActual.NumeroFEL);
                //                }
                //            }
                //        });
                //    }

                    //Facturas
                    if (Cuentas.Facturas != null && Cuentas.Facturas.Count() > 0)
                    {
                        Cuentas.Facturas.ForEach(x =>
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
                {}

                return Cuentas;
            }

            public CuentaxCobrarModel ObtenerListadoNoPagadas(long agenciaId)
            {
                CuentaxCobrarModel Cuentas = new CuentaxCobrarModel();

                Cuentas.Recibos = new List<Recibo>();
                Cuentas.Facturas = new List<Factura>();

                try
                {
                    //Recibos
                    //Cuentas.Recibos = db.Set<Recibo>().Include("Tipo").Include("Agencia").Include("Vendedor").Include("Cliente").Include("Detalles").Include("Pagos").AsNoTracking().Where(x => !x.Anulada && !x.Pagada && x.Despachado && x.AgenciaId == agenciaId).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ReciboId).ToList();

                    //Facturas
                    Cuentas.Facturas = db.Set<Factura>().Include("Tipo").Include("Serie").Include("Agencia").Include("Vendedor").Include("Cliente").Include("Detalles").Include("Pagos").AsNoTracking().Where(x => !x.Anulada && !x.Pagada && x.Despachado && x.AgenciaId == agenciaId).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.FacturaId).ToList();

                    //Recibos                    
                    //if (Cuentas.Recibos != null && Cuentas.Recibos.Count() > 0)
                    //{
                    //    Cuentas.Recibos.ForEach(x =>
                    //    {
                    //        if (x.Pagos != null && x.Pagos.Count() > 0)
                    //        {
                    //            x.Abono = x.Pagos.Sum(y => y.Valor);
                    //        }

                    //        //Se verifica que contiene factura enlazada
                    //        if (x.Factura)
                    //        {
                    //            Factura FacturaActual = db.Set<Factura>().AsNoTracking().Where(y => y.ReciboId == x.ReciboId).FirstOrDefault();
                    //            if (FacturaActual != null)
                    //            {
                    //                x.NoFactura = string.Format("{0} - {1}", FacturaActual.SerieFEL, FacturaActual.NumeroFEL);
                    //            }
                    //        }
                    //    });
                    //}

                    //Facturas                    
                    if (Cuentas.Facturas != null && Cuentas.Facturas.Count() > 0)
                    {
                        Cuentas.Facturas.ForEach(x =>
                        {
                            if (x.Pagos != null && x.Pagos.Count() > 0)
                            {
                                x.Abono = x.Pagos.Sum(y => y.Valor);
                            }
                        });
                    }
                }
                catch (Exception)
                {}

                return Cuentas;
            }

            public List<ClienteNoPagadoModel> ObtenerClienteNoPagadas(long agenciaId)
            {
                List<ClienteNoPagadoModel> Clientes = new List<ClienteNoPagadoModel>();

                List<Recibo> Recibos = new List<Recibo>();
                
                try
                {
                    Recibos = db.Set<Recibo>().Include("Detalles").Include("Pagos").AsNoTracking().Where(x => !x.Anulada && !x.Pagada && x.Despachado && x.AgenciaId == agenciaId).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ReciboId).ToList();
                    if (Recibos != null && Recibos.Count() > 0)
                    {
                        Recibos.ForEach(x =>
                        {
                            if (x.Pagos != null && x.Pagos.Count() > 0)
                            {
                                x.Abono = x.Pagos.Sum(y => y.Valor);
                            }
                        });                                                
                    }                  

                    if (Recibos != null && Recibos.Count() > 0)
                    {
                        Clientes = Recibos.GroupBy(x => new { x.ClienteId }).Select(x => new ClienteNoPagadoModel() { ClienteId = x.Key.ClienteId, Monto = x.Sum(y => y.Detalles.Sum(z => z.Cantidad * z.Precio)) - x.Sum(y => y.Abono) }).ToList();                         
                    }                    

                    if (Clientes != null && Clientes.Count() > 0)
                    {
                        Clientes = Clientes.AsEnumerable().Join(db.Set<Cliente>().AsNoTracking(), C => C.ClienteId, CC => CC.ClienteId, (C, CC) => new ClienteNoPagadoModel() { ClienteId = C.ClienteId, Nombre = string.Format("{0} - {1:C4}", CC.Nombre, C.Monto), Monto = C.Monto }).OrderByDescending(x => x.Monto).ToList();
                    }
                }
                catch (Exception)
                {}

                return Clientes;
            }

            public string GenerarPagoRecibo(Recibo entidad, long usuarioId, List<ReciboFormaPago> pagos)
            {
                string Mensaje = "OK";

                try
                {
                    int Id = 1;

                    if (pagos != null && pagos.Count() > 0)
                    {
                        ReciboFormaPago PagoUltimo = db.Set<ReciboFormaPago>().AsNoTracking().Where(x => x.ReciboId == entidad.ReciboId).OrderByDescending(x => x.DetalleId).FirstOrDefault();
                        if (PagoUltimo != null)
                        {
                            Id = PagoUltimo.DetalleId + 1;
                        }

                        foreach (var pago in pagos)
                        {
                            pago.DetalleId = Id;
                            pago.Fecha = DateTime.Today;
                            Id++;

                            db.Set<ReciboFormaPago>().Add(pago);
                        }

                        decimal TotalRecibo = db.Set<ReciboDetalle>().AsNoTracking().Where(x => x.ReciboId == entidad.ReciboId).Sum(x => x.Cantidad * x.Precio);
                        decimal TotalPago = 0;

                        var TotalPagos = db.Set<ReciboFormaPago>().Where(x => x.ReciboId == entidad.ReciboId).ToList();
                        if (TotalPagos != null && TotalPagos.Count() > 0)
                        {
                            TotalPago = TotalPagos.Sum(x => x.Valor);
                        }

                        TotalPago += pagos.Sum(x => x.Valor);

                        if (TotalPago == TotalRecibo)
                        {
                            Recibo ReciboActual = db.Set<Recibo>().Where(x => x.ReciboId == entidad.ReciboId).FirstOrDefault();

                            if (ReciboActual != null)
                            {
                                ReciboActual.Pagada = true;
                            }
                        }
                        else if (TotalPago > TotalRecibo)
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

            public string GenerarPagoFactura(Factura entidad, long usuarioId, List<FacturaFormaPago> pagos)
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

            public string GenerarPagoRecibo(long[] reciboIDs, decimal[] saldoIDs, long usuarioId)
            {
                string Mensaje = "OK";

                try
                {
                    if (reciboIDs != null && reciboIDs.Count() > 0 && saldoIDs != null && saldoIDs.Count() > 0)
                    {
                        for (int i = 0; i < reciboIDs.Length; i++)
                        {
                            long ReciboActualId = reciboIDs[i];
                            decimal SaldoActual = saldoIDs[i];

                            if (ReciboActualId > 0)
                            {
                                int Id = 1;

                                ReciboFormaPago PagoUltimo = db.Set<ReciboFormaPago>().AsNoTracking().Where(x => x.ReciboId == ReciboActualId).OrderByDescending(x => x.DetalleId).FirstOrDefault();
                                if (PagoUltimo != null)
                                {
                                    Id = PagoUltimo.DetalleId + 1;
                                }

                                ReciboFormaPago PagoActual = new ReciboFormaPago();
                                PagoActual.DetalleId = Id;
                                PagoActual.ReciboId = ReciboActualId;
                                PagoActual.FormaPagoId = 20171028001;
                                PagoActual.Valor = SaldoActual;
                                PagoActual.Fecha = DateTime.Today;
                                PagoActual.UsrOperacionId = usuarioId;

                                db.Set<ReciboFormaPago>().Add(PagoActual);

                                decimal TotalRecibo = db.Set<ReciboDetalle>().AsNoTracking().Where(x => x.ReciboId == ReciboActualId).Sum(x => x.Cantidad * x.Precio);
                                decimal TotalPago = 0;

                                var TotalPagos = db.Set<ReciboFormaPago>().AsNoTracking().Where(x => x.ReciboId == ReciboActualId).ToList();
                                if (TotalPagos != null && TotalPagos.Count() > 0)
                                {
                                    TotalPago = TotalPagos.Sum(x => x.Valor);
                                }

                                TotalPago += SaldoActual;

                                if (TotalPago == TotalRecibo)
                                {
                                    Recibo ReciboActual = db.Set<Recibo>().Where(x => x.ReciboId == ReciboActualId).FirstOrDefault();

                                    if (ReciboActual != null)
                                    {
                                        ReciboActual.Pagada = true;
                                    }
                                }
                                else if (TotalPago > TotalRecibo)
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

            public string GenerarPagoFactura(long[] facturaIDs, decimal[] saldoIDs, long usuarioId)
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

            public ReciboFormaPago ObtenerAbonoxRecibo(long reciboId, int detalleId) 
            {
                ReciboFormaPago Abono = new ReciboFormaPago();

                try
                {
                    Abono = db.Set<ReciboFormaPago>().Include("Recibo").Include("Recibo.Cliente").Include("FormaPago").Include("UsuarioOperacion").AsNoTracking().Where(x => x.ReciboId == reciboId && x.DetalleId == detalleId).FirstOrDefault();
                }
                catch (Exception)
                {
                }

                return Abono;
            }

            public FacturaFormaPago ObtenerAbonoxFactura(long facturaId, int detalleId)
            {
                FacturaFormaPago Abono = new FacturaFormaPago();

                try
                {
                    Abono = db.Set<FacturaFormaPago>().Include("Factura").Include("Factura.Serie").Include("Factura.Cliente").Include("FormaPago").Include("UsuarioOperacion").AsNoTracking().Where(x => x.FacturaId == facturaId && x.DetalleId == detalleId).FirstOrDefault();
                }
                catch (Exception)
                {
                }

                return Abono;
            }

        #endregion
    }
}
