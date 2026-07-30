using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class CorteCajaBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public CorteCajaBL()
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
                    CorteCaja CorteCajaActual = db.Set<CorteCaja>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (CorteCajaActual != null)
                    {
                        Inicial_Id = CorteCajaActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;

                }
                catch (Exception)
                {
                }

                return Id;
            }

            private string Agregar(CorteCaja entidad)
            {
                string Mensaje = "OK";

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngCorteCajaId = new Herramienta().Formato_Correlativo(Id);

                        if (lngCorteCajaId > 0)
                        {
                            entidad.CorteId = lngCorteCajaId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;
                            entidad.FechaHora = DateTime.Now;

                            db.Set<CorteCaja>().Add(entidad);
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

            public string Guardar(CorteCaja entidad)
            {
                string Mensaje = "OK";
               
                if (entidad.CorteId > 0)
                {
                }
                else
                {
                    Mensaje = Agregar(entidad);
                }
          
                return Mensaje;
            }

            public string Recibir(long corteId)
            {
                string Mensaje = "OK";

                try
                {
                    CorteCaja CorteCajaActual = db.Set<CorteCaja>().Where(x => x.CorteId == corteId).FirstOrDefault();
                    if (CorteCajaActual != null)
                    {
                        CorteCajaActual.Recibido = true;
                        CorteCajaActual.FechaHoraRecibido = DateTime.Now;

                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            public CorteCaja ObtenerPorId(long id) 
            {
                CorteCaja CorteCajaActual = new CorteCaja();

                try
                {
                    CorteCajaActual = db.Set<CorteCaja>().Include("Agencia").Include("Opero").AsNoTracking().Where(x => x.CorteId == id).FirstOrDefault();
                    if (CorteCajaActual != null)
                    {
                       CorteCajaActual.Cajero = db.Set<Usuario>().AsNoTracking().Where(y => y.UsuarioId == CorteCajaActual.CajeroId).FirstOrDefault(); 
                       CorteCajaActual.Responsable = db.Set<Usuario>().AsNoTracking().Where(y => y.UsuarioId == CorteCajaActual.ResponsableId).FirstOrDefault();                      
                    }
                }
                catch (Exception)
                {
                }

                return CorteCajaActual;
            }

            public List<CorteCaja> ObtenerListadoPorFecha(long agenciaId, long cajeroId, DateTime fecha)
            {
                List<CorteCaja> CorteCajas = new List<CorteCaja>();

                try
                {
                    CorteCajas = db.Set<CorteCaja>().Include("Agencia").Include("Opero").AsNoTracking().Where(x => x.AgenciaId == agenciaId && x.CajeroId == cajeroId && x.Fecha == fecha).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.CorteId).ToList();
                    if (CorteCajas != null && CorteCajas.Count() > 0)
                    {
                        CorteCajas.ForEach(x => 
                        {
                            x.Responsable = db.Set<Usuario>().AsNoTracking().Where(y => y.UsuarioId == x.ResponsableId).FirstOrDefault();
                        });                        
                    }
                }
                catch (Exception)
                {
                }

                return CorteCajas;
            }

            public CorteCajaModel ObtenerDisponibilidadCorteCaja(long agenciaId, long cajeroId, DateTime fecha) 
            {
                CorteCajaModel CorteCajaActual = new CorteCajaModel();
                List<ReciboFormaPago> Cobros = new List<ReciboFormaPago>();

                try
                {
                    List<long> ReciboIDs = db.Set<Recibo>().AsNoTracking().Where(x => !x.Anulada && x.AgenciaId == agenciaId && x.UsrCreo == cajeroId && x.Fecha == fecha && x.Pagada && !x.Despachado).Select(x => x.ReciboId).ToList();
                    if (ReciboIDs != null && ReciboIDs.Count() > 0)
                    {
                        Cobros = db.Set<ReciboFormaPago>().AsNoTracking().Where(x => x.UsrOperacionId == cajeroId && x.Fecha == fecha && x.FormaPagoId == 20171028001 && ReciboIDs.Contains(x.ReciboId)).ToList();
                        if (Cobros != null && Cobros.Count() > 0)
	                    {
                            CorteCajaActual.TotalRecibos = Cobros.Sum(x => x.Valor);		 
	                    }
                    }

                    Cobros = new List<ReciboFormaPago>();
                    Cobros = db.Set<ReciboFormaPago>().AsNoTracking().Where(x => x.UsrOperacionId == cajeroId && x.Fecha == fecha && x.FormaPagoId == 20171028001).ToList();
                    if (Cobros != null && Cobros.Count() > 0)
                    {
                        List<long> TReciboIDs = Cobros.Select(x => x.ReciboId).ToList();
                        if (TReciboIDs != null && TReciboIDs.Count() > 0)
                        {
                            List<long> RecibosNoAnuladosIDs = db.Set<Recibo>().AsNoTracking().Where(x => TReciboIDs.Contains(x.ReciboId) && !x.Anulada).Select(x => x.ReciboId).ToList();
                            if (RecibosNoAnuladosIDs != null && RecibosNoAnuladosIDs.Count() > 0)
                            {
                                Cobros = Cobros.Where(x => RecibosNoAnuladosIDs.Contains(x.ReciboId)).ToList();
                            }
                        }
                    }

                    if (Cobros != null && Cobros.Count() > 0)
                    {
                        CorteCajaActual.TotalAbonos = Cobros.Sum(x => x.Valor);

                        if (CorteCajaActual.TotalRecibos >= CorteCajaActual.TotalAbonos)
                        {
                            CorteCajaActual.TotalAbonos = CorteCajaActual.TotalRecibos - CorteCajaActual.TotalAbonos; 
                        }
                        else if (CorteCajaActual.TotalAbonos >= CorteCajaActual.TotalRecibos)
                        {
                            CorteCajaActual.TotalAbonos = CorteCajaActual.TotalAbonos - CorteCajaActual.TotalRecibos;
                        }
                        
                    }

                    List<Gasto> Gastos = db.Set<Gasto>().AsNoTracking().Where(x => x.AgenciaId == agenciaId && x.UsrCreo == cajeroId && x.FechaFactura == fecha).ToList();
                    if (Gastos != null && Gastos.Count() > 0)
                    {
                        CorteCajaActual.TotalGastos = Gastos.Sum(x => x.Monto);
                    }

                    List<CorteCaja> Cortes = db.Set<CorteCaja>().AsNoTracking().Where(x => x.AgenciaId == agenciaId && x.CajeroId == cajeroId && x.Fecha == fecha).ToList();
                    if (Cortes != null && Cortes.Count() > 0)
	                {
                        CorteCajaActual.TotalRetiros = Cortes.Sum(x => x.Monto);
	                }                                    

                    CorteCajaActual.Disponible = ((CorteCajaActual.TotalRecibos + CorteCajaActual.TotalAbonos) - (CorteCajaActual.TotalRetiros + CorteCajaActual.TotalGastos));
                }
                catch (Exception)
                {
                }

                return CorteCajaActual;
            }

            public CorteCajaHistorial ObtenerHistorialxAgenciaCajero(long agenciaId, long cajeroId, DateTime fecha)
            {
                CorteCajaHistorial Historial = new CorteCajaHistorial();

                try
                {
                    //Recibos
                    Historial.Recibos = db.Set<Recibo>().Include("Cliente").Include("Detalles").AsNoTracking().Where(x => !x.Anulada && x.AgenciaId == agenciaId && x.UsrCreo == cajeroId && x.Fecha == fecha && x.Pagada && !x.Despachado).ToList();

                    //Facturas
                    Historial.Facturas = db.Set<Factura>().Include("Serie").Include("Cliente").Include("Detalles").AsNoTracking().Where(x => !x.Anulada && x.AgenciaId == agenciaId && x.UsrCreo == cajeroId && x.Fecha == fecha && x.Pagada && !x.Despachado).ToList();

                    //Abonos Recibos
                    List<long> ReciboActualesIDs = Historial.Recibos.Select(x => x.ReciboId).ToList();
                    List<long> ReciboIDs = db.Set<ReciboFormaPago>().AsNoTracking().Where(x => x.UsrOperacionId == cajeroId && x.Fecha == fecha && x.FormaPagoId == 20171028001 && !ReciboActualesIDs.Contains(x.ReciboId)).Select(x => x.ReciboId).ToList();
                    if (ReciboIDs != null && ReciboIDs.Count() > 0)
                    {
                        Historial.Abonos = db.Set<Recibo>().Include("Cliente").Include("Detalles").Include("Pagos").AsNoTracking().Where(x => !x.Anulada && ReciboIDs.Contains(x.ReciboId)).ToList();
                        if (Historial.Abonos != null && Historial.Abonos.Count() > 0)
                        {
                            Historial.Abonos.ForEach(x => 
                            {
                                x.Pagos = x.Pagos.Where(y => y.UsrOperacionId == cajeroId && y.Fecha == fecha && y.FormaPagoId == 20171028001).ToList();
                            });
                        }
                    }

                    //Abonos Facturas
                    List<long> FacturaActualesIDs = Historial.Facturas.Select(x => x.FacturaId).ToList();
                    List<long> FacturaIDs = db.Set<FacturaFormaPago>().AsNoTracking().Where(x => x.UsrOperacionId == cajeroId && x.Fecha == fecha && x.FormaPagoId == 20171028001 && !FacturaActualesIDs.Contains(x.FacturaId)).Select(x => x.FacturaId).ToList();
                    if (FacturaIDs != null && FacturaIDs.Count() > 0)
                    {
                        Historial.FacturaAbonos = db.Set<Factura>().Include("Serie").Include("Cliente").Include("Detalles").Include("Pagos").AsNoTracking().Where(x => !x.Anulada && FacturaIDs.Contains(x.FacturaId)).ToList();
                        if (Historial.FacturaAbonos != null && Historial.FacturaAbonos.Count() > 0)
                        {
                            Historial.FacturaAbonos.ForEach(x =>
                            {
                                x.Pagos = x.Pagos.Where(y => y.UsrOperacionId == cajeroId && y.Fecha == fecha && y.FormaPagoId == 20171028001).ToList();
                            });
                        }
                    }

                    //Reserva
                    bool ReservaActivada = false;
                    Historial.Reservas = new List<Reserva>();

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
                        Historial.Reservas = db.Set<Reserva>().Include("Cliente").Include("Detalles").AsNoTracking().Where(x => !x.Anulada && x.AgenciaId == agenciaId && x.UsrCreo == cajeroId && x.Fecha == fecha).ToList();

                        //Abonos Reserva
                        List<long> ReservaActualesIDs = Historial.Reservas.Select(x => x.ReservaId).ToList();
                        List<long> ReservaIDs = db.Set<ReservaPago>().AsNoTracking().Where(x => x.UsrOperacionId == cajeroId && x.Fecha == fecha && x.FormaPagoId == 20171028001 && !ReservaActualesIDs.Contains(x.ReservaId)).Select(x => x.ReservaId).ToList();
                        if (ReservaIDs != null && ReservaIDs.Count() > 0)
                        {
                            Historial.ReservaAbonos = db.Set<Reserva>().Include("Cliente").Include("Detalles").Include("Pagos").AsNoTracking().Where(x => !x.Anulada && ReservaIDs.Contains(x.ReservaId)).ToList();
                            if (Historial.ReservaAbonos != null && Historial.ReservaAbonos.Count() > 0)
                            {
                                Historial.ReservaAbonos.ForEach(x =>
                                {
                                    x.Pagos = x.Pagos.Where(y => y.UsrOperacionId == cajeroId && y.Fecha == fecha && y.FormaPagoId == 20171028001).ToList();
                                });
                            }
                        }
                    }

                    //Abonos
                    Historial.Gastos = db.Set<Gasto>().Include("Categoria").AsNoTracking().Where(x => x.AgenciaId == agenciaId && x.UsrCreo == cajeroId && x.FechaFactura == fecha).ToList();

                    //Cortes
                    Historial.Cortes = db.Set<CorteCaja>().Include("Opero").AsNoTracking().Where(x => x.AgenciaId == agenciaId && x.CajeroId == cajeroId && x.Fecha == fecha).ToList();
                    if (Historial.Cortes != null && Historial.Cortes.Count() > 0)
                    {
                        Historial.Cortes.ForEach(x => 
                        {
                            x.Responsable = db.Set<Usuario>().AsNoTracking().Where(y => y.UsuarioId == x.ResponsableId).FirstOrDefault();
                        });                       
                    }
                }
                catch (Exception)
                {}

                return Historial;
            }

        #endregion
    }
}
