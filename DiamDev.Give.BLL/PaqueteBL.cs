using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class PaqueteBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public PaqueteBL()
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
                    Paquete PaqueteActual = db.Set<Paquete>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (PaqueteActual != null)
                    {
                        Inicial_Id = PaqueteActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;
                }
                catch (Exception)
                {}

                return Id;
            }

            private string Agregar(Paquete entidad)
            {
                string Mensaje = "OK";

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngPaqueteId = new Herramienta().Formato_Correlativo(Id);

                        if (lngPaqueteId > 0)
                        {
                            entidad.PaqueteId = lngPaqueteId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            db.Set<Paquete>().Add(entidad);
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

            private string Actualizar(Paquete entidad)
            {
                string Mensaje = "OK";

                try
                {
                    Paquete PaqueteActual = ObtenerPorId(entidad.PaqueteId);

                    if (PaqueteActual.PaqueteId > 0)
                    {
                        PaqueteActual.Nombre = entidad.Nombre;
                        PaqueteActual.Descripcion = entidad.Descripcion;
                        PaqueteActual.CantidadDTE = entidad.CantidadDTE;
                        PaqueteActual.Costo = entidad.Costo;
                        PaqueteActual.Precio = entidad.Precio;
                        PaqueteActual.Vigencia = entidad.Vigencia;                                      

                        db.SaveChanges();
                    }
                    else
                    {
                        Mensaje = "El paquete seleccionado no se encuentra con ID valido";
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

            public string Guardar(Paquete entidad)
            {
                string Mensaje = "OK";
               
                if (entidad.PaqueteId > 0)
                {
                    Mensaje = Actualizar(entidad);
                }
                else
                {
                    Mensaje = Agregar(entidad);
                }            

                return Mensaje;
            }

            public Paquete ObtenerPorId(long id)
            {
                Paquete PaqueteActual = new Paquete();

                try
                {
                    PaqueteActual = db.Set<Paquete>().Where(x => x.PaqueteId == id).FirstOrDefault();
                }
                catch (Exception)
                {}

                return PaqueteActual;
            }

            public List<Paquete> ObtenerListado()
            {
                List<Paquete> Paquetes = new List<Paquete>();

                try
                {
                    Paquetes = db.Set<Paquete>().AsNoTracking().OrderByDescending(x => x.Fecha).ThenByDescending(x => x.PaqueteId).Take(200).ToList();
                }
                catch (Exception)
                {}

                return Paquetes;
            }

            public List<Paquete> ObtenerListadoFormato()
            {
                List<Paquete> Paquetes = new List<Paquete>();

                try
                {
                    Paquetes = db.Set<Paquete>().AsNoTracking().OrderByDescending(x => x.Fecha).ThenByDescending(x => x.PaqueteId).ToList();
                    if (Paquetes != null && Paquetes.Count() > 0)
                    {
                        Paquetes.ForEach(x => 
                        {
                            x.Nombre = string.Format("Paquete: {0} - Cantidad de Facturas: {1} - Precio: {2:C}", x.Nombre, x.CantidadDTE, x.Precio);
                        });
                    }
                }
                catch (Exception)
                { }

                return Paquetes;
            }

            public List<Paquete> Buscar(string search)
            {
                List<Paquete> Paquetes = new List<Paquete>();

                try
                {
                    Paquetes = db.Set<Paquete>().AsNoTracking().Where(x => x.Nombre.Contains(search)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.PaqueteId).Take(200).ToList();
                }
                catch (Exception)
                {}

                return Paquetes;
            }

        #endregion
    }
}
