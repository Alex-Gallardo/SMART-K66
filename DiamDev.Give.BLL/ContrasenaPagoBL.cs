using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class ContrasenaPagoBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public ContrasenaPagoBL()
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
                    ContrasenaPago ContrasenaActual = db.Set<ContrasenaPago>().AsNoTracking().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (ContrasenaActual != null)
                    {
                        Inicial_Id = ContrasenaActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;
                }
                catch (Exception)
                {
                }

                return Id;
            }

            private string Agregar(ContrasenaPago entidad)
            {
                string Mensaje = "OK";

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngContrasenaId = new Herramienta().Formato_Correlativo(Id);

                        if (lngContrasenaId > 0)
                        {
                            entidad.ContrasenaId = lngContrasenaId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            db.Set<ContrasenaPago>().Add(entidad);
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

            public string Guardar(ContrasenaPago entidad)
            {
                string Mensaje = "OK";
               
                if (entidad.ContrasenaId > 0)
                {                    
                }
                else
                {
                    Mensaje = Agregar(entidad);
                }
              
                return Mensaje;
            }
            
            public string Operar(long contrasenaId) 
            {
                string Mensaje = string.Empty;

                try
                {
                    ContrasenaPago ContrasenaActual = db.Set<ContrasenaPago>().Where(x => x.ContrasenaId == contrasenaId).FirstOrDefault();
                    if (ContrasenaActual != null)
                    {
                        ContrasenaActual.Operado = true;
                        db.SaveChanges();

                        Mensaje = "OK";                
                    }
                    else
                    {
                        return "La contraseña ingresada no se encuentra registrada en el sistema";
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }    

                return Mensaje;
            }

            public ContrasenaPago ObtenerPorId(long id)
            {
                ContrasenaPago ContrasenaActual = new ContrasenaPago();

                try
                {
                    ContrasenaActual = db.Set<ContrasenaPago>().Include("Proveedor").Include("Pago").Include("UsuarioCreo").AsNoTracking().Where(x => x.ContrasenaId == id).FirstOrDefault();
                }
                catch (Exception)
                {
                }

                return ContrasenaActual;
            }

            public List<ContrasenaPago> ObtenerListado()
            {
                List<ContrasenaPago> Contrasenas = new List<ContrasenaPago>();

                try
                {
                    Contrasenas = db.Set<ContrasenaPago>().Include("Proveedor").Include("Pago").Include("UsuarioCreo").AsNoTracking().Where(x => !x.Operado).OrderBy(x => x.FechaPago).ThenBy(x => x.ContrasenaId).ToList();
                }
                catch (Exception)
                {
                }

                return Contrasenas;
            }

            public List<ContrasenaPago> Buscar(string search)
            {
                List<ContrasenaPago> Contrasenas = new List<ContrasenaPago>();
                long ContrasenaId = 0;

                try
                {
                    long.TryParse(search, out ContrasenaId);

                    if (ContrasenaId > 0)
                    {
                        Contrasenas = db.Set<ContrasenaPago>().Include("Proveedor").Include("Pago").Include("UsuarioCreo").AsNoTracking().Where(x => x.ContrasenaId == ContrasenaId && !x.Operado).OrderBy(x => x.FechaPago).ThenBy(x => x.ContrasenaId).ToList();
                    }
                    else
                    {
                        Contrasenas = db.Set<ContrasenaPago>().Include("Proveedor").Include("Pago").Include("UsuarioCreo").AsNoTracking().Where(x => (x.Proveedor.Nombre.ToLower().Contains(search.ToLower()) || x.Documento.ToLower().Contains(search.ToLower())) && !x.Operado).OrderBy(x => x.FechaPago).ThenBy(x => x.ContrasenaId).ToList();
                    }
                }
                catch (Exception)
                {
                }

                return Contrasenas;
            }
            
        #endregion
    }
}
