using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class CierreBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public CierreBL()
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
                    Cierre CierreActual = db.Set<Cierre>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (CierreActual != null)
                    {
                        Inicial_Id = CierreActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;
                }
                catch (Exception)
                {}

                return Id;
            }

            private string Agregar(Cierre entidad)
            {
                string Mensaje = "OK";

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngCierreId = new Herramienta().Formato_Correlativo(Id);

                        if (lngCierreId > 0)
                        {
                            entidad.CierreId = lngCierreId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;
                            entidad.FechaHora = DateTime.Now;

                            if (entidad.Detalles != null && entidad.Detalles.Count() > 0)
                            {
                                int DetalleId = 1;
                                foreach (CierreDetalle Detalle in entidad.Detalles)
                                {
                                    Detalle.CierreId = entidad.CierreId;
                                    Detalle.DetalleId = DetalleId;
                                    DetalleId++;
                                }                                
                            }

                            db.Set<Cierre>().Add(entidad);
                            db.SaveChanges();                 
                        }
                    }
                }
                catch (Exception ex)
                {                    
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }                        
              
                return Mensaje;
            }

        #endregion

        #region Metodos Publicos

            public string Guardar(Cierre entidad)
            {
                string Mensaje = "OK";
               
                if (entidad.CierreId == 0)
                {
                    Mensaje = Agregar(entidad);
                }
          
                return Mensaje;
            }

            public string Guardar(long[] formaPagoIDs, decimal[] cantidadCajeroIDs, decimal[] cantidadSistemaIDs, decimal gastoIDs, decimal retiroIDs, long agenciaId, long cajeroId)
            {
                Cierre CierreActual = new Cierre();
                CierreActual.AgenciaId = agenciaId;
                CierreActual.CajeroId = cajeroId;

                CierreActual.Detalles = new List<CierreDetalle>();
                for (int i = 0; i < formaPagoIDs.Length; i++)
                {
                    CierreDetalle Detalle = new CierreDetalle();
                    Detalle.FormaPagoId = formaPagoIDs[i];
                    Detalle.MontoCajero = cantidadCajeroIDs[i];

                    if (Detalle.FormaPagoId == 20171028001)
                    {
                        Detalle.MontoSistema = cantidadSistemaIDs[i] - (gastoIDs + retiroIDs);
                    }
                    else
                    {
                        Detalle.MontoSistema = cantidadSistemaIDs[i];
                    }
                    

                    CierreActual.Detalles.Add(Detalle);
                }

                return Agregar(CierreActual);
            }

            public string Recibir(long cierreId) 
            {
                string Mensaje = "OK";

                try
                {
                    Cierre CierreActual = db.Set<Cierre>().Where(x => x.CierreId == cierreId).FirstOrDefault();
                    if (CierreActual != null)
                    {
                        CierreActual.Recibido = true;
                        CierreActual.FechaHoraRecibido = DateTime.Now;

                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }     

                return Mensaje;
            }

            public string Eliminar(long cierreId)
            {
                string Mensaje = "OK";

                try
                {
                    Cierre CierreActual = db.Set<Cierre>().Where(x => x.CierreId == cierreId).FirstOrDefault();
                    if (CierreActual != null)
                    {
                        db.Set<Cierre>().Remove(CierreActual);
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            public Cierre ObtenerPorId(long id) 
            {
                Cierre CierreCajaActual = new Cierre();

                try
                {
                    CierreCajaActual = db.Set<Cierre>().Include("Agencia").Include("Cajero").Include("Detalles").Include("Detalles.FormaPago").AsNoTracking().Where(x => x.CierreId == id).FirstOrDefault();
                    if (CierreCajaActual != null)
                    {
                        if (CierreCajaActual.Detalles != null && CierreCajaActual.Detalles.Count() > 0)
                        {
                            foreach (var Detalle in CierreCajaActual.Detalles)
                            {
                                if (Detalle.MontoSistema >  Detalle.MontoCajero)
                                {
                                    Detalle.Faltante = Detalle.MontoSistema - Detalle.MontoCajero;                               
                                }
                                else if (Detalle.MontoCajero > Detalle.MontoSistema)
                                {
                                    Detalle.Faltante = Detalle.MontoCajero - Detalle.MontoSistema;
                                } 
                            }                            
                        }                        
                    }
                }
                catch (Exception)
                {
                }

                return CierreCajaActual;
            }

            public List<Cierre> ObtenerListadoPorFecha(long agenciaId, long cajeroId, DateTime fecha)
            {
                List<Cierre> Cierres = new List<Cierre>();

                try
                {
                    Cierres = db.Set<Cierre>().Include("Agencia").Include("Cajero").Include("Detalles").AsNoTracking().Where(x => x.AgenciaId == agenciaId && x.CajeroId == cajeroId && x.Fecha == fecha).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.CierreId).ToList();                   
                }
                catch (Exception)
                {
                }

                return Cierres;
            }

            public CierreCajaModelxCajero ObtenerDisponibilidadCierre(long agenciaId, long cajeroId, DateTime fecha) 
            {
                CierreCajaModelxCajero CierreActual = new CierreCajaModelxCajero();
                               
                List<ReciboFormaPago> RecibosCobros = new List<ReciboFormaPago>();
                List<FacturaFormaPago> FacturasCobros = new List<FacturaFormaPago>();
                List<ReservaPago> ReservaPagos = new List<ReservaPago>();

                try
                {
                    //Cajero 
                    CierreActual.Cajero = db.Set<Usuario>().AsNoTracking().Where(x => x.UsuarioId == cajeroId).FirstOrDefault();

                    //Se verifica que no exista ningun cierre en al fecha seleccionada
                    CierreActual.Operado = db.Set<Cierre>().AsNoTracking().Where(x => x.AgenciaId == agenciaId && x.CajeroId == cajeroId && x.Fecha == fecha).Count() > 0;

                    if (!CierreActual.Operado)
                    {
                        //Recibos
                        RecibosCobros = db.Set<ReciboFormaPago>().Include("FormaPago").AsNoTracking().Where(x => x.UsrOperacionId == cajeroId && x.Fecha == fecha).ToList();
                        if (RecibosCobros != null && RecibosCobros.Count() > 0)
                        {
                            List<long> ReciboIDs = RecibosCobros.Select(x => x.ReciboId).ToList();
                            if (ReciboIDs != null && ReciboIDs.Count() > 0)
                            {
                                List<long> RecibosNoAnuladosIDs = db.Set<Recibo>().AsNoTracking().Where(x => ReciboIDs.Contains(x.ReciboId) && !x.Anulada).Select(x => x.ReciboId).ToList();
                                if (RecibosNoAnuladosIDs != null && RecibosNoAnuladosIDs.Count() > 0)
                                {
                                    RecibosCobros = RecibosCobros.Where(x => RecibosNoAnuladosIDs.Contains(x.ReciboId)).ToList();
                                }
                            }
                        }

                        //Reserva
                        bool ReservaActivada = false;

                        Configuracion ConfiguracionReserva = db.Set<Configuracion>().AsNoTracking().Where(x => x.ConfiguracionId == 20200722004).FirstOrDefault();
                        if (ConfiguracionReserva != null)
                        {
                            int intReservaActivada = int.Parse(ConfiguracionReserva.Valor);
                            if (intReservaActivada == 1)
                            {
                                ReservaActivada = true;
                            }
                        }

                        if (ReservaActivada)
                        {
                            ReservaPagos = db.Set<ReservaPago>().Include("FormaPago").AsNoTracking().Where(x => x.UsrOperacionId == cajeroId && x.Fecha == fecha).ToList();
                            if (ReservaPagos != null && ReservaPagos.Count() > 0)
                            {
                                List<long> ReservaIDs = ReservaPagos.Select(x => x.ReservaId).ToList();
                                if (ReservaIDs != null && ReservaIDs.Count() > 0)
                                {
                                    List<long> ReservaNoAnuladosIDs = db.Set<Reserva>().AsNoTracking().Where(x => ReservaIDs.Contains(x.ReservaId) && !x.Anulada).Select(x => x.ReservaId).ToList();
                                    if (ReservaNoAnuladosIDs != null && ReservaNoAnuladosIDs.Count() > 0)
                                    {
                                        ReservaPagos = ReservaPagos.Where(x => ReservaNoAnuladosIDs.Contains(x.ReservaId)).ToList();
                                    }
                                }
                            }
                        }

                        //Facturas
                        //FacturasCobros = db.Set<FacturaFormaPago>().Include("FormaPago").AsNoTracking().Where(x => x.UsrOperacionId == cajeroId && x.Fecha == fecha).ToList();
                        //if (FacturasCobros != null && FacturasCobros.Count() > 0)
                        //{
                        //    List<long> FacturaIDs = FacturasCobros.Select(x => x.FacturaId).ToList();
                        //    if (FacturaIDs != null && FacturaIDs.Count() > 0)
                        //    {
                        //        List<long> FacturasNoAnuladosIDs = db.Set<Factura>().AsNoTracking().Where(x => FacturaIDs.Contains(x.FacturaId) && !x.Anulada).Select(x => x.FacturaId).ToList();
                        //        if (FacturasNoAnuladosIDs != null && FacturasNoAnuladosIDs.Count() > 0)
                        //        {
                        //            FacturasCobros = FacturasCobros.Where(x => FacturasNoAnuladosIDs.Contains(x.FacturaId)).ToList();
                        //        }
                        //    }
                        //}

                        CierreActual.Formas = new List<FormaPago>();

                        if (RecibosCobros != null && RecibosCobros.Count() > 0)
                        {
                            foreach (var Detalle in RecibosCobros)
                            {
                                CierreActual.Formas.Add(new FormaPago() { FormaPagoId = Detalle.FormaPagoId, Nombre = Detalle.FormaPago.Nombre, Valor = Detalle.Valor, MontoCajero = 0 });                                
                            }
                        }

                        if (ReservaPagos != null && ReservaPagos.Count() > 0)
                        {
                            foreach (var Detalle in ReservaPagos)
                            {
                                CierreActual.Formas.Add(new FormaPago() { FormaPagoId = Detalle.FormaPagoId, Nombre = Detalle.FormaPago.Nombre, Valor = Detalle.Valor, MontoCajero = 0 });
                            }
                        }

                        //if (FacturasCobros != null && FacturasCobros.Count() > 0)
                        //{
                        //    foreach (var Detalle in FacturasCobros)
                        //    {
                        //        CierreActual.Formas.Add(new FormaPago() { FormaPagoId = Detalle.FormaPagoId, Nombre = Detalle.FormaPago.Nombre, Valor = Detalle.Valor, MontoCajero = 0 });
                        //    }
                        //}

                        if ( CierreActual.Formas != null &&  CierreActual.Formas.Count() > 0)
                        {
                            CierreActual.Formas = CierreActual.Formas.GroupBy(x => new { x.FormaPagoId, x.Nombre }).Select(x => new FormaPago() { FormaPagoId = x.Key.FormaPagoId, Nombre = x.Key.Nombre, Valor = x.Sum(y => y.Valor), MontoCajero = 0 }).ToList();                            
                        }

                        List<Gasto> Gastos = db.Set<Gasto>().AsNoTracking().Where(x => x.AgenciaId == agenciaId && x.UsrCreo == cajeroId && x.FechaFactura == fecha).ToList();
                        if (Gastos != null && Gastos.Count() > 0)
                        {
                            CierreActual.TotalGastos = Gastos.Sum(x => x.Monto);
                        }

                        List<CorteCaja> Cortes = db.Set<CorteCaja>().AsNoTracking().Where(x => x.AgenciaId == agenciaId && x.CajeroId == cajeroId && x.Fecha == fecha).ToList();
                        if (Cortes != null && Cortes.Count() > 0)
                        {
                            CierreActual.TotalRetiros = Cortes.Sum(x => x.Monto);
                        }                                                 
                    }

                    CierreActual.Cierres = new List<Cierre>();
                    CierreActual.Cierres = db.Set<Cierre>().Include("Cajero").Include("Detalles").AsNoTracking().Where(x => x.AgenciaId == agenciaId && x.CajeroId == cajeroId && x.Fecha == fecha).ToList();
                    if (CierreActual.Cierres != null && CierreActual.Cierres.Count() > 0)
                    {
                        decimal TotalSistema = CierreActual.Cierres.Sum(x => x.Detalles.Sum(y => y.MontoSistema));
                        decimal TotalCajero = CierreActual.Cierres.Sum(x => x.Detalles.Sum(y => y.MontoCajero));

                        if (TotalSistema > TotalCajero)
                        {
                            CierreActual.Faltante = TotalSistema - TotalCajero;
                        }
                        else if (TotalCajero > TotalSistema)
                        {
                            CierreActual.Sobrante = TotalCajero - TotalSistema;
                        }
                    }
                }
                catch (Exception)
                {}

                return CierreActual;
            }

            public CierreCajaModelxCajero ObtenerCierres(long agenciaId, DateTime fechaInicial, DateTime fechaFinal)
            {
                CierreCajaModelxCajero CierreActual = new CierreCajaModelxCajero();
                
                try
                {
                    CierreActual.Cierres = new List<Cierre>();
                    if (agenciaId == 0)
                    {
                        CierreActual.Cierres = db.Set<Cierre>().Include("Agencia").Include("Cajero").Include("Detalles").AsNoTracking().Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal).ToList();
                    }
                    else
                    {
                        CierreActual.Cierres = db.Set<Cierre>().Include("Agencia").Include("Cajero").Include("Detalles").AsNoTracking().Where(x => x.AgenciaId == agenciaId && x.Fecha >= fechaInicial && x.Fecha <= fechaFinal).ToList();
                    }

                    if (CierreActual.Cierres != null && CierreActual.Cierres.Count() > 0)
                    {
                        CierreActual.Cierres.ForEach(x => 
                        {
                            decimal TotalSistema = x.Detalles.Sum(y => y.MontoSistema);
                            decimal TotalCajero = x.Detalles.Sum(y => y.MontoCajero);

                            if (TotalSistema > TotalCajero)
                            {
                                x.Faltante = TotalSistema - TotalCajero;
                            }
                            else if (TotalCajero > TotalSistema)
                            {
                                x.Sobrante = TotalCajero - TotalSistema;
                            }
                        });
                    }
                }
                catch (Exception)
                {
                }

                return CierreActual;
            }

            public CierreCajaModelxCajero ObtenerCierresPendientes(long usuarioId)
            {
                CierreCajaModelxCajero CierreActual = new CierreCajaModelxCajero();
                List<long> AgenciaIds = new List<long>();

                try
                {
                    AgenciaIds = db.Set<UsuarioAgencia>().AsNoTracking().Where(x => x.UsuarioId == usuarioId).Select(x => x.AgenciaId).ToList();
                    if (AgenciaIds != null && AgenciaIds.Count() > 0)
                    {
                        //Cierres
                        CierreActual.Cierres = new List<Cierre>();
                        CierreActual.Cierres = db.Set<Cierre>().Include("Cajero").Include("Detalles").AsNoTracking().Where(x => AgenciaIds.Contains(x.AgenciaId) && !x.Recibido).OrderBy(x => x.Fecha).ToList();

                        if (CierreActual.Cierres != null && CierreActual.Cierres.Count() > 0)
                        {
                            CierreActual.Cierres.ForEach(x =>
                            {
                                decimal TotalSistema = x.Detalles.Sum(y => y.MontoSistema);
                                decimal TotalCajero = x.Detalles.Sum(y => y.MontoCajero);

                                if (TotalSistema > TotalCajero)
                                {
                                    x.Faltante = TotalSistema - TotalCajero;
                                }
                                else if (TotalCajero > TotalSistema)
                                {
                                    x.Sobrante = TotalCajero - TotalSistema;
                                }
                            });
                        }

                        //Cortes
                        CierreActual.Cortes = new List<CorteCaja>();
                        CierreActual.Cortes = db.Set<CorteCaja>().Include("Agencia").Include("Opero").AsNoTracking().Where(x => AgenciaIds.Contains(x.AgenciaId) && !x.Recibido).OrderBy(x => x.Fecha).ToList();
                        if (CierreActual.Cortes != null && CierreActual.Cortes.Count() > 0)
                        {
                            CierreActual.Cortes.ForEach(x =>
                            {
                                x.Responsable = db.Set<Usuario>().AsNoTracking().Where(y => y.UsuarioId == x.ResponsableId).FirstOrDefault();
                            });
                        }
                    }                    
                }
                catch (Exception)
                {
                }

                return CierreActual;
            }           

        #endregion
    }
}
