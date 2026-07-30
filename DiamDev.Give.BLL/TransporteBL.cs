using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class TransporteBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public TransporteBL()
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
                    Transporte TransporteActual = db.Set<Transporte>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (TransporteActual != null)
                    {
                        Inicial_Id = TransporteActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;
                }
                catch (Exception)
                {
                }

                return Id;
            }

            private string Agregar(Transporte entidad)
            {
                string Mensaje = string.Empty;

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngTransporteId = new Herramienta().Formato_Correlativo(Id);

                        if (lngTransporteId > 0)
                        {
                            entidad.TransporteId = lngTransporteId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            db.Set<Transporte>().Add(entidad);
                            db.SaveChanges();

                            Mensaje = "OK";
                        }
                    }

                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            private string Actualizar(Transporte entidad)
            {
                string Mensaje = string.Empty;

                try
                {

                    Transporte TransporteActual = ObtenerPorId(entidad.TransporteId);

                    if (TransporteActual.TransporteId > 0)
                    {
                        TransporteActual.Nombre = entidad.Nombre;
                        TransporteActual.Descripcion = entidad.Descripcion;
                        TransporteActual.DescripcionEmpaque = entidad.DescripcionEmpaque;
                        TransporteActual.SitioWeb = entidad.SitioWeb;
                        TransporteActual.Contacto = entidad.Contacto;
                        TransporteActual.NoTelefono = entidad.NoTelefono;
                        TransporteActual.Nit = entidad.Nit;
                        TransporteActual.NombrePago = entidad.NombrePago;

                        db.SaveChanges();
                        Mensaje = "OK";
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

        public string Guardar(Transporte entidad)
        {
            string Mensaje = "OK";
                     
            if (entidad.TransporteId > 0)
            {
                Mensaje = Actualizar(entidad);              
            }
            else
            {
                Mensaje = Agregar(entidad);
            }
            
            return Mensaje;
        }

        public Transporte ObtenerPorId(long id)
        {
            Transporte TransporteActual = new Transporte();

            try
            {
                TransporteActual = db.Set<Transporte>().Where(x => x.TransporteId == id).FirstOrDefault();
            }
            catch (Exception)
            {
            }

            return TransporteActual;
        }

        public List<Transporte> ObtenerListado()
        {
            List<Transporte> Transportes = new List<Transporte>();

            try
            {
                Transportes = db.Set<Transporte>().OrderByDescending(x => x.Fecha).ThenByDescending(x => x.TransporteId).ToList();
            }
            catch (Exception)
            {
            }

            return Transportes;
        }

        public List<Transporte> Buscar(string search)
        {
            List<Transporte> Transportes = new List<Transporte>();

            try
            {
                Transportes = db.Set<Transporte>().Where(x => x.Nit.Contains(search) || x.Nombre.Contains(search) || x.Descripcion.Contains(search) || x.DescripcionEmpaque.Contains(search) || x.NoTelefono.Contains(search)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.TransporteId).ToList();
            }
            catch (Exception)
            {
            }

            return Transportes;
        }

        #endregion
    }
}
