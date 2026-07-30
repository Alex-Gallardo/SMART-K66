using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class MonedaBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public MonedaBL()
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
                    Moneda MonedaActual = db.Set<Moneda>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (MonedaActual != null)
                    {
                        Inicial_Id = MonedaActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;
                }
                catch (Exception)
                {}

                return Id;
            }

            private string Agregar(Moneda entidad)
            {
                string Mensaje = "OK";

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngMonedaId = new Herramienta().Formato_Correlativo(Id);

                        if (lngMonedaId > 0)
                        {
                            entidad.MonedaId = lngMonedaId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            db.Set<Moneda>().Add(entidad);
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

            private string Actualizar(Moneda entidad)
            {
                string Mensaje = "OK";

                try
                {
                    Moneda MonedaActual = ObtenerPorId(entidad.MonedaId);

                    if (MonedaActual.MonedaId > 0)
                    {
                        MonedaActual.Codigo = entidad.Codigo;
                        MonedaActual.Nombre = entidad.Nombre;
                        MonedaActual.Descripcion = entidad.Descripcion;
                        MonedaActual.Simbolo = entidad.Simbolo;
                        MonedaActual.TipoDeCambioCompra = entidad.TipoDeCambioCompra;
                        MonedaActual.TipoDeCambioVenta = entidad.TipoDeCambioVenta;

                        db.SaveChanges();
                    }
                    else
                    {
                        Mensaje = "La moneda seleccionada no se encuentra con ID valido";
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

            public string Guardar(Moneda entidad)
            {
                string Mensaje = "OK";
               
                if (entidad.MonedaId > 0)
                {
                    Mensaje = Actualizar(entidad);
                }
                else
                {
                    Mensaje = Agregar(entidad);
                }            

                return Mensaje;
            }

            public Moneda ObtenerPorId(long id)
            {
                Moneda MonedaActual = new Moneda();

                try
                {
                    MonedaActual = db.Set<Moneda>().Where(x => x.MonedaId == id).FirstOrDefault();
                }
                catch (Exception)
                {}

                return MonedaActual;
            }

            public List<Moneda> ObtenerListado()
            {
                List<Moneda> Monedas = new List<Moneda>();

                try
                {
                    Monedas = db.Set<Moneda>().AsNoTracking().OrderByDescending(x => x.Fecha).ThenByDescending(x => x.MonedaId).ToList();
                }
                catch (Exception)
                {}

                return Monedas;
            }

            public List<Moneda> Buscar(string search)
            {
                List<Moneda> Monedas = new List<Moneda>();

                try
                {
                    Monedas = db.Set<Moneda>().AsNoTracking().Where(x => x.Nombre.Contains(search)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.MonedaId).Take(200).ToList();
                }
                catch (Exception)
                {}

                return Monedas;
            }

        #endregion
    }
}
