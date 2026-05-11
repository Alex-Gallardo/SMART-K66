using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class OfertaDeliveryBL
    {

        #region Variables Globales

        private GiveContext db;

        #endregion

        #region Constructores

        public OfertaDeliveryBL()
        {
            this.db = new GiveContext();
        }

        #endregion

        #region Metodos Privados

        
        private string Agregar(OfertaDelivery entidad)
        {
            string Mensaje = "OK";

            try
            {
             
                
                    
                    
                       
                       
                        entidad.Fecha = DateTime.Today;
                         
                
                        db.Set<OfertaDelivery>().Add(entidad);
                        db.SaveChanges();
                    
                
            }
            catch (Exception ex)
            {
                Mensaje = string.Format("Descripción del Error {0}", ex.Message);
            }

            return Mensaje;
        }

        private string Actualizar(OfertaDelivery entidad)
        {
            string Mensaje = "OK";

            try
            {
                OfertaDelivery OfertaActual = ObtenerPorId(entidad.OfertaId);

                if (OfertaActual.OfertaId > 0)
                {
                    OfertaActual.Nombre = entidad.Nombre;
                    OfertaActual.Descripcion = entidad.Descripcion;
                    OfertaActual.FechaInicioOferta = entidad.FechaInicioOferta;
                    OfertaActual.FechaFinOferta = entidad.FechaFinOferta;
                    OfertaActual.ProductoBaseId = entidad.ProductoBaseId;



                    db.SaveChanges();
                }
                else
                {
                    Mensaje = "La Oferta seleccionada no se encuentra con ID valido";
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

            public string Guardar(OfertaDelivery entidad)
            {
                string Mensaje = "OK";
               
                if (entidad.OfertaId > 0)
                {
                    Mensaje = Actualizar(entidad);
                }
                else
                {
                    Mensaje = Agregar(entidad);
                }            

                return Mensaje;
            }

            public OfertaDelivery ObtenerPorId(int id)
            {
            OfertaDelivery OfertaActual = new OfertaDelivery();

                try
                {
                OfertaActual = db.Set<OfertaDelivery>().Include("ProductoBase").Where(x => x.OfertaId == id).FirstOrDefault();
                }
                catch (Exception)
                {
                }

                return OfertaActual;
            }

            public List<OfertaDelivery> ObtenerListado()
            {
                List<OfertaDelivery> Agencias = new List<OfertaDelivery>();

                try
                {
                Agencias = db.Set<OfertaDelivery>().Include("ProductoBase").AsNoTracking().OrderByDescending(x => x.Fecha).ThenByDescending(x => x.OfertaId).ToList();
            }
                catch (Exception)
                {
                }

                return Agencias;
            }

        public List<OfertaDelivery> ObtenerListadoActivas()
        {
            List<OfertaDelivery> Ofertas = new List<OfertaDelivery>();

            try
            {
                Ofertas = db.Set<OfertaDelivery>().AsNoTracking().Include("ProductoBase").Where(x=> x.FechaFinOferta > DateTime.Today && x.FechaInicioOferta < DateTime.Today).OrderByDescending(x => x.FechaFinOferta).ThenByDescending(x => x.OfertaId).ToList();
            }
            catch (Exception)
            {
            }

            return Ofertas;
        }


   
       
       
        #endregion
    }
}
