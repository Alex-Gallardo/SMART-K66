using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class MunicipioBL
    {

        #region Variables Globales

        private GiveContext db;

        #endregion

        #region Constructores

        public MunicipioBL()
        {
            this.db = new GiveContext();
        }

        #endregion


        #region Metodos Privados
         private string Agregar(Municipio entidad)
        {
            string Mensaje = "OK";

            try
            {


                entidad.Activo = true;
                
                        db.Set<Municipio>().Add(entidad);
                        db.SaveChanges();
                
                
            }
            catch (Exception ex)
            {
                Mensaje = string.Format("Descripción del Error {0}", ex.Message);
            }

            return Mensaje;
        }
        
        private string Actualizar(Municipio entidad)
        {
            string Mensaje = "OK";

            try
            {
                
                Municipio AgenciaActual = ObtenerPorId(entidad.MunicipioId);

                if (AgenciaActual.MunicipioId > 0)
                {
                    AgenciaActual.Nombre = entidad.Nombre;
                    AgenciaActual.Descripcion = entidad.Descripcion;
                    
                    AgenciaActual.Activo = entidad.Activo;

                    db.SaveChanges();
                }
                else
                {
                    Mensaje = "La Municipio seleccionada no se encuentra con ID valido";
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

        public string Guardar(Municipio entidad)
        {
            string Mensaje = "OK";

            if (entidad.MunicipioId > 0)
            {
                Mensaje = Actualizar(entidad);
            }
            else
            {
                Mensaje = Agregar(entidad);
            }

            return Mensaje;
        }

        public Municipio ObtenerPorId(long id)
        {
            Municipio AgenciaActual = new Municipio();

            try
            {
                AgenciaActual = db.Set<Municipio>().Where(x => x.MunicipioId == id).FirstOrDefault();
            }
            catch (Exception)
            {
            }

            return AgenciaActual;
        }

        public List<Municipio> ObtenerListado(bool todos, long usuarioId = 0)
        {
            List<Municipio> Agencias = new List<Municipio>();

            try
            {
                if (todos)
                {
                   
                        Agencias = db.Set<Municipio>().Take(200).ToList();
                 
                }
                else
                {
                    Agencias = db.Set<Municipio>().AsNoTracking().Where(x => x.Activo == true).ToList();
                }
            }
            catch (Exception)
            {
            }

            return Agencias;
        }
        #endregion
    }
}
