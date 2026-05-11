using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class UnidadBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public UnidadBL()
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

                    Unidad UnidadActual = db.Set<Unidad>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (UnidadActual != null)
                    {
                        Inicial_Id = UnidadActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;

                }
                catch (Exception)
                {
                }

                return Id;
            }

            private string Agregar(Unidad entidad)
            {
                string Mensaje = "OK";

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {

                        long lngUnidadId = new Herramienta().Formato_Correlativo(Id);

                        if (lngUnidadId > 0)
                        {
                            entidad.UnidadId = lngUnidadId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            db.Set<Unidad>().Add(entidad);
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

            private string Actualizar(Unidad entidad)
            {
                string Mensaje = "OK";

                try
                {

                    Unidad UnidadActual = ObtenerPorId(entidad.UnidadId);

                    if (UnidadActual.UnidadId > 0)
                    {
                        UnidadActual.Codigo = entidad.Codigo;
                        UnidadActual.Nombre = entidad.Nombre;
                        UnidadActual.Cantidad = entidad.Cantidad;
                        UnidadActual.Activo = entidad.Activo;

                        db.SaveChanges();                       
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

            public string Guardar(Unidad entidad)
            {
                string Mensaje = "OK";
               
                if (entidad.UnidadId > 0)
                {
                    Mensaje = Actualizar(entidad);
                }
                else
                {
                    Mensaje = Agregar(entidad);
                }
          
                return Mensaje;
            }

            public Unidad ObtenerPorId(long id)
            {
                Unidad UnidadActual = new Unidad();

                try
                {
                    UnidadActual = db.Set<Unidad>().Where(x => x.UnidadId == id).FirstOrDefault();
                }
                catch (Exception)
                {
                }

                return UnidadActual;
            }

            public List<Unidad> ObtenerListado(bool todos)
            {
                List<Unidad> Unidades = new List<Unidad>();

                try
                {
                    if (todos)
                    {
                        Unidades = db.Set<Unidad>().OrderByDescending(x => x.Fecha).ThenByDescending(x => x.UnidadId).ToList();
                    }
                    else
                    {
                        Unidades = db.Set<Unidad>().Where(x => x.Activo == true).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.UnidadId).ToList();
                    }
                }
                catch (Exception)
                {}

                return Unidades;
            }

            public List<UnidadK66> ObtenerUnidadxConversion(string conversion, long usuarioId, long empresaId)
            {
                List<UnidadK66> Unidades = new List<UnidadK66>();

                try
                {
                    UsuarioEmpresa UsuarioEmpresaActual = db.Set<UsuarioEmpresa>().Include("Empresa").AsNoTracking().Where(x => x.UsuarioId == usuarioId && x.EmpresaId == empresaId).FirstOrDefault();
                    if (UsuarioEmpresaActual != null)
                    {
                        if (empresaId == 20210705001)
                        {
                            using (var dbK66 = new VMBOLIKContext())
                            {
                                Unidades = dbK66.Database.SqlQuery<UnidadK66>("dbo.sp_obtener_unidad_medida_x_conversion @Conversion", new SqlParameter("@Conversion", conversion)).ToList();
                            }
                        }
                        else if (empresaId == 20210705002)
                        {
                            using (var dbK66 = new VMEMPAQUESContext())
                            {
                                Unidades = dbK66.Database.SqlQuery<UnidadK66>("dbo.sp_obtener_unidad_medida_x_conversion @Conversion", new SqlParameter("@Conversion", conversion)).ToList();
                            }
                        }
                        else if (empresaId == 20210705003)
                        {
                            using (var dbK66 = new VMFAESContext())
                            {
                                Unidades = dbK66.Database.SqlQuery<UnidadK66>("dbo.sp_obtener_unidad_medida_x_conversion @Conversion", new SqlParameter("@Conversion", conversion)).ToList();
                            }
                        }
                        else if (empresaId == 20210705004)
                        {
                            using (var dbK66 = new VMGRACOContext())
                            {
                                Unidades = dbK66.Database.SqlQuery<UnidadK66>("dbo.sp_obtener_unidad_medida_x_conversion @Conversion", new SqlParameter("@Conversion", conversion)).ToList();
                            }
                        }

                        Unidades.Add(new UnidadK66() { UnidadId = 0, Unidad = conversion });
                    }
                }
                catch (Exception)
                { }

                return Unidades;
            }

            public UnidadK66 ObtenerUnidadxID(int id, long usuarioId, long empresaId)
            {
                UnidadK66 UnidadActual = new UnidadK66();

                try
                {
                    UsuarioEmpresa UsuarioEmpresaActual = db.Set<UsuarioEmpresa>().Include("Empresa").AsNoTracking().Where(x => x.UsuarioId == usuarioId && x.EmpresaId == empresaId).FirstOrDefault();
                    if (UsuarioEmpresaActual != null)
                    {
                        if (empresaId == 20210705001)
                        {
                            using (var dbK66 = new VMBOLIKContext())
                            {
                                UnidadActual = dbK66.Database.SqlQuery<UnidadK66>("dbo.sp_obtener_unidad_medida_x_id @ROWID", new SqlParameter("@ROWID", id)).FirstOrDefault();
                            }
                        }
                        else if (empresaId == 20210705002)
                        {
                            using (var dbK66 = new VMEMPAQUESContext())
                            {
                                UnidadActual = dbK66.Database.SqlQuery<UnidadK66>("dbo.sp_obtener_unidad_medida_x_id @ROWID", new SqlParameter("@ROWID", id)).FirstOrDefault();
                            }
                        }
                        else if (empresaId == 20210705003)
                        {
                            using (var dbK66 = new VMFAESContext())
                            {
                                UnidadActual = dbK66.Database.SqlQuery<UnidadK66>("dbo.sp_obtener_unidad_medida_x_id @ROWID", new SqlParameter("@ROWID", id)).FirstOrDefault();
                            }
                        }
                        else if (empresaId == 20210705004)
                        {
                            using (var dbK66 = new VMGRACOContext())
                            {
                                UnidadActual = dbK66.Database.SqlQuery<UnidadK66>("dbo.sp_obtener_unidad_medida_x_id @ROWID", new SqlParameter("@ROWID", id)).FirstOrDefault();
                            }
                        }
                    }
                }
                catch (Exception)
                { }

                return UnidadActual;
            }

        #endregion
    }
}
