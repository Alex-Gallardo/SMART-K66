using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class EmpresaBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public EmpresaBL()
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
                    Empresa EmpresaActual = db.Set<Empresa>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (EmpresaActual != null)
                    {
                        Inicial_Id = EmpresaActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;
                }
                catch (Exception)
                {}

                return Id;
            }

            private string Agregar(Empresa entidad)
            {
                string Mensaje = "OK";

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngEmpresaId = new Herramienta().Formato_Correlativo(Id);

                        if (lngEmpresaId > 0)
                        {
                            entidad.EmpresaId = lngEmpresaId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            db.Set<Empresa>().Add(entidad);
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

            private string Actualizar(Empresa entidad)
            {
                string Mensaje = "OK";

                try
                {
                    Empresa EmpresaActual = ObtenerPorId(entidad.EmpresaId, false);

                    if (EmpresaActual.EmpresaId > 0)
                    {
                        EmpresaActual.Nombre = entidad.Nombre;
                        EmpresaActual.NombreComercial = entidad.NombreComercial;
                        EmpresaActual.NombreContacto = entidad.NombreContacto;
                        EmpresaActual.TelefonoContacto = entidad.TelefonoContacto;
                        EmpresaActual.TelefonoContacto2 = entidad.TelefonoContacto2;
                        EmpresaActual.CorreoContacto = entidad.CorreoContacto;
                        EmpresaActual.AnyDeskId = entidad.AnyDeskId;
                        EmpresaActual.NitEmisorDIGIFACT = entidad.NitEmisorDIGIFACT;
                        EmpresaActual.NombreComercialDIGIFACT = entidad.NombreComercialDIGIFACT;
                        EmpresaActual.NombreEmisorDIGIFACT = entidad.NombreEmisorDIGIFACT;
                        EmpresaActual.DireccionEmisorDIGIFACT = entidad.DireccionEmisorDIGIFACT;
                        EmpresaActual.CodigoPostalEmisorDIGIFACT = entidad.CodigoPostalEmisorDIGIFACT;
                        EmpresaActual.DepartamentoEmisorDIGIFACT = entidad.DepartamentoEmisorDIGIFACT;
                        EmpresaActual.MunicipioEmisorDIGIFACT = entidad.MunicipioEmisorDIGIFACT;
                        EmpresaActual.PaisEmisorDIGIFACT = entidad.PaisEmisorDIGIFACT;
                        EmpresaActual.CodigoEscenarioDIGIFACT = entidad.CodigoEscenarioDIGIFACT;
                        EmpresaActual.TipoFraseDIGIFACT = entidad.TipoFraseDIGIFACT;
                        EmpresaActual.AfiliacionIvaDIGIFACT = entidad.AfiliacionIvaDIGIFACT;
                        EmpresaActual.UsuarioDIGIFACT = entidad.UsuarioDIGIFACT;
                        EmpresaActual.PasswordDIGIFACT = entidad.PasswordDIGIFACT;

                        EmpresaActual.Reporte1 = entidad.Reporte1;
                        EmpresaActual.Reporte2 = entidad.Reporte2;
                        EmpresaActual.ReporteCotizacion = entidad.ReporteCotizacion;

                        db.SaveChanges();
                    }
                    else
                    {
                        Mensaje = "La empresa seleccionada no se encuentra con ID valido";
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

            public string Guardar(Empresa entidad)
            {
                string Mensaje = "OK";
               
                if (entidad.EmpresaId > 0)
                {
                    Mensaje = Actualizar(entidad);
                }
                else
                {
                    Mensaje = Agregar(entidad);
                }            

                return Mensaje;
            }

            public string GuardarBodegaActiva(EmpresaBodegaActiva entidad)
            {
                string Mensaje = "OK";

                try
                {
                    bool Existe = db.Set<EmpresaBodegaActiva>().AsNoTracking().Where(x => x.EmpresaId == entidad.EmpresaId && x.WarehouseId == entidad.WarehouseId && x.LocationId == entidad.LocationId).Count() > 0;
                    if (!Existe)
                    {
                        db.Set<EmpresaBodegaActiva>().Add(entidad);
                        db.SaveChanges();
                    }                   
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            public string EliminarBodegaActiva(Guid id)
            {
                string Mensaje = "OK";

                try
                {
                    EmpresaBodegaActiva EmpresaBodegaActivaActual = db.Set<EmpresaBodegaActiva>().Where(x => x.BodegaId == id).FirstOrDefault();
                    if (EmpresaBodegaActivaActual != null)
                    {
                        db.Set<EmpresaBodegaActiva>().Remove(EmpresaBodegaActivaActual);
                        db.SaveChanges();
                    }
                    else
                    {
                        return "nOK";
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            public string GuardarProductoEspecial(EmpresaProductoEspecial entidad)
            {
                string Mensaje = "OK";

                try
                {
                    entidad.Fecha = DateTime.Today;
                    db.Set<EmpresaProductoEspecial>().Add(entidad);
                    db.SaveChanges();
                }   
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            public string EliminarProductoEspecial(Guid id)
            {
                string Mensaje = "OK";

                try
                {
                    EmpresaProductoEspecial EmpresaProductoEspecialActual = db.Set<EmpresaProductoEspecial>().Where(x => x.EspecialId == id).FirstOrDefault();
                    if (EmpresaProductoEspecialActual != null)
                    {
                        db.Set<EmpresaProductoEspecial>().Remove(EmpresaProductoEspecialActual);
                        db.SaveChanges();
                    }
                    else
                    {
                        return "nOK";
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            public Empresa ObtenerPorId(long id, bool todo)
            {
                Empresa EmpresaActual = new Empresa();

                try
                {
                    if (todo)
                    {
                        EmpresaActual = db.Set<Empresa>().Include("Bodegas").Include("ProductosEspeciales").Include("ProductosEspeciales.Responsable").AsNoTracking().Where(x => x.EmpresaId == id).FirstOrDefault();
                        if (EmpresaActual != null)
                        {
                            EmpresaActual.Paquetes = new List<PaqueteEmpresa>();
                            EmpresaActual.Paquetes = db.Set<PaqueteEmpresa>().Include("Paquete").Include("FormaPago").AsNoTracking().Where(x => x.EmpresaId == id).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.PaqueteEmpresaId).ToList();
                        }
                    }
                    else
                    {
                        EmpresaActual = db.Set<Empresa>().Where(x => x.EmpresaId == id).FirstOrDefault();
                    }
                }
                catch (Exception)
                {}

                return EmpresaActual;
            }

            public List<Empresa> ObtenerListado()
            {
                List<Empresa> Empresas = new List<Empresa>();

                try
                {
                    Empresas = db.Set<Empresa>().AsNoTracking().OrderByDescending(x => x.Fecha).ThenByDescending(x => x.EmpresaId).Take(200).ToList();
                }
                catch (Exception)
                {}

                return Empresas;
            }

            public List<Empresa> ObtenerListadoxUsuario(long usuarioId)
            {
                List<Empresa> Empresas = new List<Empresa>();

                try
                {
                    List<long> EmpresaIDs = db.Set<UsuarioEmpresa>().AsNoTracking().Where(x => x.UsuarioId == usuarioId).Select(x => x.EmpresaId).ToList();
                    if (EmpresaIDs != null && EmpresaIDs.Count() > 0)
                    {
                        Empresas = db.Set<Empresa>().AsNoTracking().Where(x => EmpresaIDs.Contains(x.EmpresaId)).ToList();
                    }                    
                }
                catch (Exception)
                { }

                return Empresas;
            }

            public List<Empresa> Buscar(string search)
            {
                List<Empresa> Empresas = new List<Empresa>();

                try
                {
                    Empresas = db.Set<Empresa>().AsNoTracking().Where(x => x.Nombre.Contains(search)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.EmpresaId).Take(200).ToList();
                }
                catch (Exception)
                {}

                return Empresas;
            }

        #endregion
    }
}
