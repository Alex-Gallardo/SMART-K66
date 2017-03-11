using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class DiarioBL
    {

        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public DiarioBL()
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
                    Diario DiarioActual = db.Set<Diario>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (DiarioActual != null)
                    {
                        Inicial_Id = DiarioActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;
                }
                catch (Exception)
                {
                }

                return Id;
            }

            private int CorrelativoPartida(DateTime fecha)
            {
                int Id = 0;

                try
                {
                    Diario DiarioActual = db.Set<Diario>().Where(x => x.Fecha.Year == fecha.Year && x.Fecha.Month == fecha.Month).OrderByDescending(x => x.PartidaId).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (DiarioActual != null)
                    {
                        Inicial_Id = DiarioActual.PartidaId + 1;
                    }

                    Id = Inicial_Id;
                }
                catch (Exception)
                {
                }

                return Id;
            }

            private bool Agregar(Diario entidad)
            {
                bool DiarioAgregar = false;

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngDiarioId = new Herramienta().Formato_Correlativo(Id);

                        if (lngDiarioId > 0)
                        {
                            entidad.DiarioId = lngDiarioId;
                            entidad.PartidaId = CorrelativoPartida(entidad.FechaDocumento);
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            if (entidad.Detalles != null && entidad.Detalles.Count() > 0)
                            {
                                int DetalleId = 1;
                                foreach (var Detalle in entidad.Detalles)
                                {
                                    Detalle.DetalleId = DetalleId;
                                    Detalle.DiarioId = entidad.DiarioId;
                                    DetalleId++;
                                }
                            }

                            if (entidad.Agencias != null && entidad.Agencias.Count() > 0)
                            {
                                foreach (var Agencia in entidad.Agencias)
                                {
                                    Agencia.DiarioId = entidad.DiarioId;
                                }
                            }
                            else
                            {
                                entidad.General = true;
                            }

                            db.Set<Diario>().Add(entidad);
                            db.SaveChanges();
                            DiarioAgregar = true;
                        }
                    }
                }
                catch (Exception)
                {
                }

                return DiarioAgregar;
            }

        #endregion

        #region Metodos Publicos

            public string Guardar(Diario entidad)
            {
                string Mensaje = "OK";
                bool OperacionExitosa = false;

                if (entidad.Detalles != null && entidad.Detalles.Count() > 0)
                {
                    decimal debe = entidad.Detalles.Sum(x => x.Debe);
                    decimal Haber = entidad.Detalles.Sum(x => x.Haber);

                    if (debe != Haber)
                    {
                        return "La suma del debe y el haber no son iguales";
                    }
                }

                if (entidad.DiarioId > 0)
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

            public Diario ObtenerPorId(long id, bool todo)
            {
                Diario DiarioActual = new Diario();

                try
                {
                    if (todo)
                    {
                        DiarioActual = db.Set<Diario>().Include("Agencias").Include("Agencias.Agencia").Include("Detalles").Include("Detalles.Cuenta").Where(x => x.DiarioId == id).FirstOrDefault();
                    }
                    else
                    {
                        DiarioActual = db.Set<Diario>().Where(x => x.DiarioId == id).FirstOrDefault();
                    }
                }
                catch (Exception)
                {
                }

                return DiarioActual;
            }

            public List<Diario> ObtenerListado(DateTime fechaInicial, DateTime fechaFinal)
            {
                List<Diario> Diarios = new List<Diario>();

                try
                {
                    Diarios = db.Set<Diario>().Include("Agencias").Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.DiarioId).ToList();
                }
                catch (Exception)
                {
                }

                return Diarios;
            }

            public List<DiarioModel> ObtenerDiarioPorFecha(DateTime fechaInicial, DateTime fechaFinal, long agenciaId, long usuarioId, bool mayor, bool balance_saldos)
            {
                List<DiarioModel> Cuentas = new List<DiarioModel>();
                List<long> AgenciaIds = new List<long>();

                if (agenciaId == 0)
                {
                    AgenciaIds = db.Set<UsuarioAgencia>().Where(x => x.UsuarioId == usuarioId).AsEnumerable().Select(x => x.AgenciaId).ToList();
                }
                else
                {
                    AgenciaIds.Add(agenciaId);
                }

                try
                {
                    //Obtiene las cuentas principales 
                    Cuentas = db.Set<Diario>().Where(x => x.FechaDocumento >= fechaInicial && x.FechaDocumento <= fechaFinal && x.General == true).Join(db.Set<DiarioDetalle>().Include("Cuenta"), D => D.DiarioId, DD => DD.DiarioId, (D, DD) => new DiarioModel() { DiarioId = D.DiarioId, PartidaId = D.PartidaId, Agencia = "1 - General", Descripcion = D.Descripcion, Fecha = D.FechaDocumento, CuentaId = DD.CuentaId, Debe = DD.Debe, Haber = DD.Haber }).Join(db.Set<CuentaContable>(), D => D.CuentaId, C => C.CuentaId, (D, C) => new { D, C }).Select(x => x).AsEnumerable().Select(x => new DiarioModel() { DiarioId = x.D.DiarioId, PartidaId = x.D.PartidaId, Agencia = x.D.Agencia, Descripcion = x.D.Descripcion, Fecha = x.D.Fecha, Cuenta = string.Format("{0}-{1}", x.C.Cuenta, x.C.Nombre), Debe = x.D.Debe, Haber = x.D.Haber }).ToList();
                    //Obtiene las cuentas de todos los centros de costo que tenga acceso el usuario
                    Cuentas.AddRange(db.Set<DiarioAgencia>().Include("Agencia").Where(x => AgenciaIds.Contains(x.AgenciaId)).Join(db.Set<Diario>().Where(x => x.FechaDocumento >= fechaInicial && x.FechaDocumento <= fechaFinal && x.General == false), C => C.DiarioId, D => D.DiarioId, (C, D) => new DiarioModel { DiarioId = C.DiarioId, PartidaId = D.PartidaId, Agencia = C.Agencia.Nombre, Descripcion = D.Descripcion, Fecha = D.FechaDocumento }).AsEnumerable().Select(x => x).Join(db.Set<DiarioDetalle>().Include("Cuenta"), D => D.DiarioId, DD => DD.DiarioId, (D, DD) => new DiarioModel() { DiarioId = D.DiarioId, PartidaId = D.PartidaId, Agencia = D.Agencia, Descripcion = D.Descripcion, Fecha = D.Fecha, CuentaId = DD.CuentaId, Debe = DD.Debe, Haber = DD.Haber }).Join(db.Set<CuentaContable>(), D => D.CuentaId, C => C.CuentaId, (D, C) => new { D, C }).Select(x => x).AsEnumerable().Select(x => new DiarioModel() { DiarioId = x.D.DiarioId, PartidaId = x.D.PartidaId, Agencia = x.D.Agencia, Descripcion = x.D.Descripcion, Fecha = x.D.Fecha, Cuenta = string.Format("{0}-{1}", x.C.Cuenta, x.C.Nombre), Debe = x.D.Debe, Haber = x.D.Haber }).ToList());

                    if (Cuentas != null && Cuentas.Count() > 0)
                    {
                        Cuentas = Cuentas.OrderBy(x => x.PartidaId).ToList();
                    }

                    if (mayor)
                    {
                        if (Cuentas != null && Cuentas.Count() > 0)
                        {
                            Cuentas = Cuentas.AsEnumerable().Select(x => new DiarioModel() { DiarioId = x.DiarioId, PartidaId = x.PartidaId, Agencia = x.Agencia, Descripcion = string.Format("La partida No. {0} con fecha: {1} ", x.PartidaId, x.Fecha.ToString("dd/MM/yyyy")), Fecha = x.Fecha, Cuenta = x.Cuenta, Debe = x.Debe, Haber = x.Haber }).ToList();
                            Cuentas = Cuentas.GroupBy(x => new { x.Agencia, x.Cuenta, x.Descripcion, x.DiarioId }).Select(y => new DiarioModel() { DiarioId = y.Key.DiarioId, Agencia = y.Key.Agencia, Cuenta = y.Key.Cuenta, Descripcion = y.Key.Descripcion, Debe = y.Sum(z => z.Debe), Haber = y.Sum(z => z.Haber) }).OrderBy(x => x.DiarioId).ToList();
                        }
                    }
                    else if (!mayor && balance_saldos)
                    {
                        if (Cuentas != null && Cuentas.Count() > 0)
                        {
                            Cuentas = Cuentas.AsEnumerable().Select(x => new DiarioModel() { DiarioId = x.DiarioId, PartidaId = x.PartidaId, Agencia = x.Agencia, Fecha = x.Fecha, Cuenta = x.Cuenta, Debe = x.Debe, Haber = x.Haber }).ToList();
                            Cuentas = Cuentas.GroupBy(x => new { x.Agencia, x.Cuenta }).Select(y => new DiarioModel() { Agencia = y.Key.Agencia, Cuenta = y.Key.Cuenta, Debe = y.Sum(z => z.Debe), Haber = y.Sum(z => z.Haber) }).ToList();
                        }
                    }
                }
                catch (Exception)
                {
                }

                return Cuentas;
            }

        #endregion

    }
}
