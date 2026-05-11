using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class GastoBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public GastoBL()
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
                    Gasto GastoActual = db.Set<Gasto>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (GastoActual != null)
                    {
                        Inicial_Id = GastoActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;
                }
                catch (Exception)
                {
                }

                return Id;
            }

            private string Agregar(Gasto entidad)
            {
                string Mensaje = "OK";

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngGastoId = new Herramienta().Formato_Correlativo(Id);

                        if (lngGastoId > 0)
                        {
                            entidad.GastoId = lngGastoId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;
                            entidad.FechaHoraGasto = DateTime.Now;

                            if (entidad.Fotografias != null && entidad.Fotografias.Count() > 0)
                            {
                                int fotografiaId = 1;
                                foreach (var Imagen in entidad.Fotografias)
                                {
                                    Imagen.FotografiaId = fotografiaId;
                                    Imagen.GastoId = entidad.GastoId;
                                    fotografiaId++;
                                }
                            }
                                                     
                            db.Set<Gasto>().Add(entidad);
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

            public string Guardar(Gasto entidad)
            {
                string Mensaje = "OK";
               
                if (entidad.GastoId > 0)
                {                    
                }
                else
                {
                    Mensaje = Agregar(entidad);
                }
            
                return Mensaje;
            }

            public string Anular(long gastoId, string comentario, long usuarioId)
            {
                string Mensaje = "OK";

                try
                {
                    Gasto GastoActual = db.Set<Gasto>().Where(x => x.GastoId == gastoId).FirstOrDefault();
                    if (GastoActual == null)
                    {
                        return "El gasto que selecciono no se encuentra disponible";
                    }

                    GastoActual.Comentario = comentario;
                    GastoActual.Anulada = true;
                    GastoActual.UsrAnular = usuarioId;
                    GastoActual.FechaAnular = DateTime.Now;

                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            public Gasto ObtenerPorId(long id, bool todos = true, bool imagen = false)
            {
                Gasto GastoActual = new Gasto();
               
                try
                {
                    if (todos)
                    {
                        if (imagen)
                        {
                            GastoActual = db.Set<Gasto>().Include("Agencia").Include("Proveedor").Include("TipoCompra").Include("Categoria").Include("Fotografias").Where(x => x.GastoId == id).FirstOrDefault();
                        }
                        else 
                        {
                            GastoActual = db.Set<Gasto>().Where(x => x.GastoId == id).FirstOrDefault();
                        }
                    }
                    else
                    {
                        GastoActual = db.Set<Gasto>().Where(x => x.GastoId == id).FirstOrDefault();
                    }
                }
                catch (Exception)
                {}
            
                return GastoActual;
            }

            public List<Gasto> ObtenerListadoxFecha(DateTime fechaInicial, DateTime fechaFinal, long usuarioId)
            {
                List<Gasto> Gastos = new List<Gasto>();

                try
                {
                    var AgenciasIds = db.Set<UsuarioAgencia>().Where(x => x.UsuarioId == usuarioId).AsEnumerable().Select(x => x.AgenciaId).ToList();
                    if (AgenciasIds != null && AgenciasIds.Count() > 0)
                    {
                        Gastos = db.Set<Gasto>().Include("Agencia").Include("Proveedor").Include("TipoCompra").Include("Categoria").Include("UsuarioCreo").AsNoTracking().Where(x => x.FechaFactura >= fechaInicial && x.FechaFactura <= fechaFinal && AgenciasIds.Contains(x.AgenciaId.Value)).OrderByDescending(x => x.FechaFactura).ThenByDescending(x => x.GastoId).ToList();
                    }
                }
                catch (Exception)
                {}

                return Gastos;
            }

            public GastoFotografia Fotografia(int fotografiaId, long gastoId)
            {
                GastoFotografia FotografiaActual = new GastoFotografia();

                try
                {
                    FotografiaActual = db.Set<GastoFotografia>().Where(x => x.FotografiaId == fotografiaId && x.GastoId == gastoId).FirstOrDefault();
                }
                catch (Exception)
                {}

                return FotografiaActual;
            }

            public List<ReporteEgresosEfectivo> ReporteEgresosEfectivo(long agenciaId, long categoriaId, DateTime fechaInicial, DateTime fechaFinal)
            {
                List<ReporteEgresosEfectivo> Egresos = new List<ReporteEgresosEfectivo>();

                try
                {
                    if (agenciaId == 0 && categoriaId == 0)
                    {
                        Egresos = db.Database.SqlQuery<ReporteEgresosEfectivo>("dbo.sp_reporte_egresos_efectivo @AgenciaId, @CategoriaId, @FechaInicial, @FechaFinal", new SqlParameter("@AgenciaId", DBNull.Value), new SqlParameter("@CategoriaId", DBNull.Value), new SqlParameter("@FechaInicial", fechaInicial), new SqlParameter("@FechaFinal", fechaFinal)).ToList();
                    }
                    else if (agenciaId != 0 && categoriaId != 0)
                    {
                        Egresos = db.Database.SqlQuery<ReporteEgresosEfectivo>("dbo.sp_reporte_egresos_efectivo @AgenciaId, @CategoriaId, @FechaInicial, @FechaFinal", new SqlParameter("@AgenciaId", agenciaId), new SqlParameter("@CategoriaId", categoriaId), new SqlParameter("@FechaInicial", fechaInicial), new SqlParameter("@FechaFinal", fechaFinal)).ToList();
                    }
                    else if (agenciaId != 0 && categoriaId == 0)
                    {
                        Egresos = db.Database.SqlQuery<ReporteEgresosEfectivo>("dbo.sp_reporte_egresos_efectivo @AgenciaId, @CategoriaId, @FechaInicial, @FechaFinal", new SqlParameter("@AgenciaId", agenciaId), new SqlParameter("@CategoriaId", DBNull.Value), new SqlParameter("@FechaInicial", fechaInicial), new SqlParameter("@FechaFinal", fechaFinal)).ToList();
                    }
                    else if (agenciaId == 0 && categoriaId != 0)
                    {
                        Egresos = db.Database.SqlQuery<ReporteEgresosEfectivo>("dbo.sp_reporte_egresos_efectivo @AgenciaId, @CategoriaId, @FechaInicial, @FechaFinal", new SqlParameter("@AgenciaId", DBNull.Value), new SqlParameter("@CategoriaId", categoriaId), new SqlParameter("@FechaInicial", fechaInicial), new SqlParameter("@FechaFinal", fechaFinal)).ToList();
                    }
                }
                catch (Exception)
                {}

                return Egresos;
            }
        
        #endregion
    }
}
