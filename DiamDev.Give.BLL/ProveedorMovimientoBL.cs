using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class ProveedorMovimientoBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public ProveedorMovimientoBL()
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
                    ProveedorMovimiento ProveedorMovimientoActual = db.Set<ProveedorMovimiento>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (ProveedorMovimientoActual != null)
                    {
                        Inicial_Id = ProveedorMovimientoActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;
                }
                catch (Exception)
                {
                }

                return Id;
            }

            private string Agregar(ProveedorMovimiento entidad)
            {
                string Mensaje = "OK";

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngMovimientoId = new Herramienta().Formato_Correlativo(Id);

                        if (lngMovimientoId > 0)
                        {
                            entidad.MovimientoId = lngMovimientoId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            if (entidad.Fotografias != null && entidad.Fotografias.Count() > 0)
                            {
                                int fotografiaId = 1;
                                foreach (var Imagen in entidad.Fotografias)
                                {
                                    Imagen.FotografiaId = fotografiaId;
                                    Imagen.MovimientoId = entidad.MovimientoId;
                                    fotografiaId++;
                                }
                            }

                            //Se actualiza el saldo al proveedor
                            Proveedor ProveedorActual = db.Set<Proveedor>().Where(x => x.ProveedorId == entidad.ProveedorId).FirstOrDefault();
                            if (ProveedorActual != null)
                            {
                                if (entidad.TipoId == 1)
                                {
                                    ProveedorActual.Credito += entidad.Monto;
                                }
                                else if (entidad.TipoId == 2)
                                {
                                    ProveedorActual.Abono += entidad.Monto;
                                }
                            }

                            if (entidad.CreditoId > 0 && entidad.TipoId == 2)
                            {
                                Movimiento MovimientoActual = db.Set<Movimiento>().Where(x => x.MovimientoId == entidad.CreditoId).FirstOrDefault();
                                if (MovimientoActual != null)
                                {
                                    MovimientoActual.Cancelado = true;                                    
                                }
                            }

                            db.Set<ProveedorMovimiento>().Add(entidad);
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

            public string Guardar(ProveedorMovimiento entidad)
            {
                string Mensaje = "OK";

                if (entidad.MovimientoId > 0)
                {
                }
                else
                {
                    Mensaje = Agregar(entidad);
                }

                return Mensaje;
            }

            public string GenerarMasivo(long proveedorId, long[] creditoIDs, decimal[] saldoIDs, string observaciones, long usuarioId)
            {
                string Mensaje = "OK";

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        for (int i = 0; i < creditoIDs.Length; i++)
                        {
                            long lngMovimientoId = new Herramienta().Formato_Correlativo(Id);

                            if (lngMovimientoId > 0)
                            {
                                ProveedorMovimiento MovimientoProveedorActual = new ProveedorMovimiento();

                                MovimientoProveedorActual.MovimientoId = lngMovimientoId;
                                MovimientoProveedorActual.TipoId = 2;
                                MovimientoProveedorActual.ProveedorId = proveedorId;
                                MovimientoProveedorActual.Documento = "PAGO MASIVO";
                                MovimientoProveedorActual.DiasCredito = 0;
                                MovimientoProveedorActual.FechaMovimiento = DateTime.Today;
                                MovimientoProveedorActual.FechaVencimiento = DateTime.Today;
                                MovimientoProveedorActual.Observaciones = observaciones;
                                MovimientoProveedorActual.UsrCreo = usuarioId;
                                MovimientoProveedorActual.Monto = saldoIDs[i];
                                MovimientoProveedorActual.Correlativo = Id;
                                MovimientoProveedorActual.Fecha = DateTime.Today;

                                //Se actualiza el saldo al proveedor
                                Proveedor ProveedorActual = db.Set<Proveedor>().Where(x => x.ProveedorId == proveedorId).FirstOrDefault();
                                if (ProveedorActual != null)
                                {
                                    ProveedorActual.Abono += MovimientoProveedorActual.Monto;
                                }

                                long MovimientoId = creditoIDs[i];
                                Movimiento MovimientoActual = db.Set<Movimiento>().Where(x => x.MovimientoId == MovimientoId).FirstOrDefault();
                                if (MovimientoActual != null)
                                {
                                    MovimientoActual.Cancelado = true;
                                }

                                db.Set<ProveedorMovimiento>().Add(MovimientoProveedorActual);                               
                                Id++;
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

            public ProveedorMovimiento ObtenerPorId(long id, bool todos = true, bool imagen = false)
            {
                ProveedorMovimiento ProveedorMovimientoActual = new ProveedorMovimiento();

                try
                {
                    if (todos)
                    {
                        if (imagen)
                        {
                            ProveedorMovimientoActual = db.Set<ProveedorMovimiento>().Include("Tipo").Include("Proveedor").Include("Fotografias").Where(x => x.MovimientoId == id).FirstOrDefault();
                        }
                        else
                        {
                            ProveedorMovimientoActual = db.Set<ProveedorMovimiento>().Where(x => x.MovimientoId == id).FirstOrDefault();
                        }
                    }
                    else
                    {
                        ProveedorMovimientoActual = db.Set<ProveedorMovimiento>().Where(x => x.MovimientoId == id).FirstOrDefault();
                    }
                }
                catch (Exception)
                {

                }

                return ProveedorMovimientoActual;
            }

            public List<ProveedorMovimiento> ObtenerListadoxFecha(DateTime fechaInicial, DateTime fechaFinal)
            {
                List<ProveedorMovimiento> ProveedorMovimientos = new List<ProveedorMovimiento>();

                try
                {
                    ProveedorMovimientos = db.Set<ProveedorMovimiento>().Include("Tipo").Include("Proveedor").Include("UsuarioCreo").AsNoTracking().Where(x => x.FechaMovimiento >= fechaInicial && x.FechaMovimiento <= fechaFinal).OrderByDescending(x => x.FechaMovimiento).ThenByDescending(x => x.MovimientoId).ToList();
                }
                catch (Exception)
                {
                }

                return ProveedorMovimientos;
            }

            public ProveedorMovimientoFotografia Fotografia(int fotografiaId, long movimientoId)
            {
                ProveedorMovimientoFotografia FotografiaActual = new ProveedorMovimientoFotografia();

                try
                {
                    FotografiaActual = db.Set<ProveedorMovimientoFotografia>().Where(x => x.FotografiaId == fotografiaId && x.MovimientoId == movimientoId).FirstOrDefault();
                }
                catch (Exception)
                {
                }

                return FotografiaActual;
            }

        #endregion
    }
}
