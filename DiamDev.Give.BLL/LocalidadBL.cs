using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;


namespace DiamDev.Give.BLL
{
    public class LocalidadBL
    {
        #region Variables Globales

        private GiveContext db;

        #endregion

        #region Constructores

        public LocalidadBL()
        {
            this.db = new GiveContext();
        }

        #endregion


        #region Metodos Privados
        private string Agregar(Localidad entidad)
        {
            string Mensaje = "OK";

            try
            {


                entidad.Activo = true;

                db.Set<Localidad>().Add(entidad);
                db.SaveChanges();


            }
            catch (Exception ex)
            {
                Mensaje = string.Format("Descripción del Error {0}", ex.Message);
            }

            return Mensaje;
        }

        private string Actualizar(Localidad entidad)
        {
            string Mensaje = "OK";

            try
            {

                Localidad AgenciaActual = ObtenerPorId(entidad.LocalidadId);

                if (AgenciaActual.LocalidadId > 0)
                {
                    AgenciaActual.Nombre = entidad.Nombre;
                    AgenciaActual.Descripcion = entidad.Descripcion;
                    AgenciaActual.MunicipioId = entidad.MunicipioId;
                    AgenciaActual.CostoEnvio = entidad.CostoEnvio;
                    AgenciaActual.Activo = entidad.Activo;
                    AgenciaActual.AgenciaId = entidad.AgenciaId;

                    db.SaveChanges();
                }
                else
                {
                    Mensaje = "La Localidad seleccionada no se encuentra con ID valido";
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
        public List<Localidad> Buscar(string search)
        {
            List<Localidad> Vendedors = new List<Localidad>();

            try
            {
                Vendedors = db.Set<Localidad>().Include("Municipio").Include("Agencia").AsNoTracking().Where(x => x.Nombre.Contains(search)|| x.Municipio.Nombre.Contains(search)).OrderByDescending(x => x.Nombre).ToList();
            }
            catch (Exception)
            {
            }

            return Vendedors;
        }
        public string Guardar(Localidad entidad)
        {
            string Mensaje = "OK";

            if (entidad.LocalidadId > 0)
            {
                Mensaje = Actualizar(entidad);
            }
            else
            {
                Mensaje = Agregar(entidad);
            }

            return Mensaje;
        }

        public Localidad ObtenerPorId(long id)
        {
            Localidad AgenciaActual = new Localidad();

            try
            {
                AgenciaActual = db.Set<Localidad>().Include("Municipio").Where(x => x.LocalidadId == id).FirstOrDefault();
            }
            catch (Exception)
            {
            }

            return AgenciaActual;
        }

        public List<Localidad> ObtenerListado(bool todos, long usuarioId = 0)
        {
            List<Localidad> Agencias = new List<Localidad>();

            try
            {
                if (todos)
                {

                    Agencias = db.Set<Localidad>().Include("Municipio").Include("Agencia").Take(200).OrderBy(x => x.Nombre).ToList();

                }
                else
                {
                    Agencias = db.Set<Localidad>().Include("Municipio").Include("Agencia").AsNoTracking().OrderBy(x=>x.Nombre).ToList();
                }
            }
            catch (Exception)
            {
            }

            return Agencias;
        }
        public List<Localidad> ObtenerListadoPorMunicipioId(long municipioid)
        {
            List<Localidad> Agencias = new List<Localidad>();

            try
            {
                
                    Agencias = db.Set<Localidad>().AsNoTracking().Where(x => x.Activo == true&&x.MunicipioId==municipioid).ToList();
              
            }
            catch (Exception)
            {
            }

            return Agencias;
        }
        #endregion
    }
}
