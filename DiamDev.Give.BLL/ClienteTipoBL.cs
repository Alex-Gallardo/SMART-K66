using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class ClienteTipoBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public ClienteTipoBL()
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
                    ClienteTipo ClienteTipoActual = db.Set<ClienteTipo>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (ClienteTipoActual != null)
                    {
                        Inicial_Id = ClienteTipoActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;
                }
                catch (Exception)
                {
                }

                return Id;
            }

            private string Agregar(ClienteTipo entidad)
            {
                string Mensaje = "OK";

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngClienteTipoId = new Herramienta().Formato_Correlativo(Id);

                        if (lngClienteTipoId > 0)
                        {
                            entidad.TipoId = lngClienteTipoId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            db.Set<ClienteTipo>().Add(entidad);
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

            private string Actualizar(ClienteTipo entidad)
            {
                string Mensaje = "OK";

                try
                {
                    ClienteTipo ClienteTipoActual = ObtenerPorId(entidad.TipoId);

                    if (ClienteTipoActual.TipoId > 0)
                    {
                        ClienteTipoActual.Nombre = entidad.Nombre;
                        ClienteTipoActual.Descripcion = entidad.Descripcion;
                        ClienteTipoActual.Motivo = entidad.Motivo;
                        ClienteTipoActual.PorcentajeDescuento = entidad.PorcentajeDescuento;

                        db.SaveChanges();
                    }
                    else
                    {
                        Mensaje = "El tipo de cliente seleccionado no se encuentra con ID valido";
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

            public string Guardar(ClienteTipo entidad)
            {
                string Mensaje = "OK";
               
                if (entidad.TipoId > 0)
                {
                    Mensaje = Actualizar(entidad);
                }
                else
                {
                    Mensaje = Agregar(entidad);
                }            

                return Mensaje;
            }

            public ClienteTipo ObtenerPorId(long id)
            {
                ClienteTipo ClienteTipoActual = new ClienteTipo();

                try
                {
                    ClienteTipoActual = db.Set<ClienteTipo>().Where(x => x.TipoId == id).FirstOrDefault();
                }
                catch (Exception)
                {
                }

                return ClienteTipoActual;
            }

            public List<ClienteTipo> ObtenerListado()
            {
                List<ClienteTipo> ClienteTipos = new List<ClienteTipo>();

                try
                {
                    ClienteTipos = db.Set<ClienteTipo>().OrderByDescending(x => x.Fecha).ThenByDescending(x => x.TipoId).ToList();
                }
                catch (Exception)
                {
                }

                return ClienteTipos;
            }

            public List<ClienteTipo> Buscar(string search)
            {
                List<ClienteTipo> ClienteTipos = new List<ClienteTipo>();

                try
                {
                    ClienteTipos = db.Set<ClienteTipo>().Where(x => (x.Nombre.Contains(search))).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.TipoId).Take(200).ToList();
                }
                catch (Exception)
                {
                }

                return ClienteTipos;
            }
          
        #endregion
    }
}
