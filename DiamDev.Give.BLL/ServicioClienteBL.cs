using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class ServicioClienteBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public ServicioClienteBL()
            {
                this.db = new GiveContext();
            }

        #endregion

        #region Metodos Privados           
        #endregion

        #region Metodos Publicos

            public string Atender(long id, long usuarioId) 
            {
                string Mensaje = "OK";

                try
                {
                    ServicioCliente NumeroActual = db.Set<ServicioCliente>().Where(x => x.ID == id && x.Estado == 0).FirstOrDefault();
                    if (NumeroActual != null)
                    {
                        Usuario UsuarioActual = db.Set<Usuario>().AsNoTracking().Where(x => x.UsuarioId == usuarioId).FirstOrDefault();
                        if (UsuarioActual != null)
                        {
                            NumeroActual.Atentido = UsuarioActual.Nombre;
                        }
                        else
                        {
                            NumeroActual.Atentido = "No se encuentra registrado";
                        }
                        
                        NumeroActual.Estado = 1;
                        NumeroActual.HoraAtendido = DateTime.Now;
                        db.SaveChanges();                       
                    }
                    else
                    {
                        Mensaje = "El # del correlativo no se encuentra disponible";
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }    

                return Mensaje;
            }

            public string Anular(long id)
            {
                string Mensaje = "OK";

                try
                {
                    ServicioCliente NumeroActual = db.Set<ServicioCliente>().Where(x => x.ID == id).FirstOrDefault();
                    if (NumeroActual != null)
                    {                       
                        NumeroActual.Estado = 10;                     
                        db.SaveChanges();
                    }
                    else
                    {
                        Mensaje = "El # del correlativo no se encuentra disponible";
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            public string Visualizar(long id)
            {
                string Mensaje = string.Empty;

                try
                {
                    ServicioCliente NumeroActual = db.Set<ServicioCliente>().Where(x => x.ID == id).FirstOrDefault();
                    if (NumeroActual != null)
                    {
                        Mensaje = string.Format("ATENDIENDO CLIENTE # {0}", NumeroActual.Correlativo);
                    }
                    else
                    {
                        Mensaje = "El # del correlativo no se encuentra disponible";
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            public List<ServicioCliente> ObtenerListadoxFechayAgencia(DateTime fecha, long agenciaId) 
            {
                List<ServicioCliente> Numeros = new List<ServicioCliente>();

                try
                {
                    Numeros = db.Set<ServicioCliente>().Include("TipoServicio").AsNoTracking().Where(x => x.Fecha == fecha && x.AgenciaId == agenciaId && x.Estado == 0).OrderBy(x => x.ID).ToList();
                }
                catch (Exception)
                {
                }

                return Numeros;
            }
                     
        #endregion
    }
}
