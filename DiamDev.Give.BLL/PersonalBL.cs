using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class PersonalBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public PersonalBL()
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

                Personal PersonalActual = db.Set<Personal>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                int Inicial_Id = 1;

                if (PersonalActual != null)
                {
                    Inicial_Id = PersonalActual.Correlativo + 1;
                }

                Id = Inicial_Id;

            }
            catch (Exception)
            {
            }

            return Id;
        }

        private string Agregar(Personal entidad)
        {
            string Mensaje = "OK";

            try
            {
                int Id = Correlativo();

                if (Id > 0)
                {
                    long lngPersonalId = new Herramienta().Formato_Correlativo(Id);

                    if (lngPersonalId > 0)
                    {
                        entidad.PersonalId = lngPersonalId;
                        entidad.Correlativo = Id;
                        entidad.Fecha = DateTime.Today;

                        db.Set<Personal>().Add(entidad);
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

        private string Actualizar(Personal entidad)
        {
            string Mensaje = "OK";

            try
            {

                Personal PersonalActual = ObtenerPorId(entidad.PersonalId, false);

                if (PersonalActual.PersonalId > 0)
                {                    
                    PersonalActual.Nombre = entidad.Nombre;
                    PersonalActual.Direccion = entidad.Direccion;
                    PersonalActual.DPI = entidad.DPI;
                    PersonalActual.NoTelefono = entidad.NoTelefono;
                    PersonalActual.NoTelefonoAlterno = entidad.NoTelefonoAlterno;
                    PersonalActual.NoCelularPrincipal = entidad.NoCelularPrincipal;
                    PersonalActual.NoCelularAlterno = entidad.NoCelularAlterno;
                    PersonalActual.Email = entidad.Email;
                    PersonalActual.FechaNacimiento = entidad.FechaNacimiento;
                    PersonalActual.LicenciaVehiculo = entidad.LicenciaVehiculo;
                    PersonalActual.LicenciaMoto = entidad.LicenciaMoto;
                    PersonalActual.NoAfiliacionIGSS = entidad.NoAfiliacionIGSS;
                    PersonalActual.FechaIngreso = entidad.FechaIngreso;
                    PersonalActual.FechaEgreso = entidad.FechaEgreso;
                    PersonalActual.BancoId = entidad.BancoId;
                    PersonalActual.Planilla = entidad.Planilla;
                    PersonalActual.Contrato = entidad.Contrato;                    
                    PersonalActual.Sueldo = entidad.Sueldo;
                    PersonalActual.Bonificacion = entidad.Bonificacion;
                    PersonalActual.IGSS = entidad.IGSS;
                    PersonalActual.MotivoEgreso = entidad.MotivoEgreso;
                    PersonalActual.Activo = entidad.Activo;

                    db.SaveChanges();                    
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

        public string Guardar(Personal entidad)
        {
            string Mensaje = "OK";
           
            if (!string.IsNullOrWhiteSpace(entidad.Email))
            {
                if (!new Herramienta().ValidarEmail(entidad.Email))
                {
                    return "El correo electrónico ingresado no es valido";
                }
            }          

            if (entidad.PersonalId > 0)
            {
                Mensaje = Actualizar(entidad);
            }
            else
            {
                Mensaje = Agregar(entidad);
            }

            return Mensaje;
        }

        public Personal ObtenerPorId(long id, bool todo)
        {
            Personal PersonalActual = new Personal();

            try
            {
                if (todo)
                {
                    PersonalActual = db.Set<Personal>().Include("Puesto").Include("Banco").Include("Anotaciones").Include("Anotaciones.Tipo").Where(x => x.PersonalId == id).FirstOrDefault();
                }
                else
                {
                    PersonalActual = db.Set<Personal>().Where(x => x.PersonalId == id).FirstOrDefault();
                }
            }
            catch (Exception)
            {
            }

            return PersonalActual;
        }

        public List<Personal> ObtenerListado(bool activo)
        {
            List<Personal> Personals = new List<Personal>();

            try
            {
                if (activo)
                {
                    Personals = db.Set<Personal>().Where(x => x.Activo == true).AsEnumerable().Select(x => new Personal() { PersonalId = x.PersonalId, Nombre = x.Nombre, Fecha = x.Fecha, TemplateBytes = x.TemplateBytes }).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.PersonalId).ToList();
                }
                else
                {
                    Personals = db.Set<Personal>().OrderByDescending(x => x.Fecha).ThenByDescending(x => x.PersonalId).ToList();
                }
            }
            catch (Exception)
            {
            }

            return Personals;
        }

        public List<Personal> Buscar(string search)
        {
            List<Personal> Personals = new List<Personal>();

            try
            {
                Personals = db.Set<Personal>().Where(x => x.Nombre.Contains(search) || x.Direccion.Contains(search) || x.NoTelefono.Contains(search) || x.Email.Contains(search)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.PersonalId).ToList();
            }
            catch (Exception)
            {
            }

            return Personals;
        }

        #endregion
    }
}
