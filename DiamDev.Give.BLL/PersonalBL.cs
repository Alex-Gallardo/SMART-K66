using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        private bool Agregar(Personal entidad)
        {
            bool PersonalAgregar = false;

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
                        PersonalAgregar = true;
                    }
                }

            }
            catch (Exception)
            {
            }

            return PersonalAgregar;
        }

        private bool Actualizar(Personal entidad)
        {
            bool PersonalActualizar = false;

            try
            {

                Personal PersonalActual = ObtenerPorId(entidad.PersonalId, false);

                if (PersonalActual.PersonalId > 0)
                {                    
                    PersonalActual.Nombre = entidad.Nombre;
                    PersonalActual.Direccion = entidad.Direccion;                    
                    PersonalActual.NoTelefono = entidad.NoTelefono;
                    PersonalActual.NoTelefonoAlterno = entidad.NoTelefonoAlterno;
                    PersonalActual.NoCelularPrincipal = entidad.NoCelularPrincipal;
                    PersonalActual.NoCelularAlterno = entidad.NoCelularAlterno;
                    PersonalActual.Email = entidad.Email;
                    PersonalActual.Activo = entidad.Activo;

                    db.SaveChanges();
                    PersonalActualizar = true;
                }

            }
            catch (Exception)
            {
            }

            return PersonalActualizar;
        }

        #endregion

        #region Metodos Publicos

        public string Guardar(Personal entidad)
        {
            string Mensaje = "OK";
            bool OperacionExitosa = false;

            if (!string.IsNullOrWhiteSpace(entidad.Email))
            {
                if (!new Herramienta().ValidarEmail(entidad.Email))
                {
                    return "El correo electrónico ingresado no es valido";
                }
            }          

            if (entidad.PersonalId > 0)
            {
                OperacionExitosa = Actualizar(entidad);
            }
            else
            {
                OperacionExitosa = Agregar(entidad);
            }

            if (!OperacionExitosa)
            {
                Mensaje = "La información ingresada no es valida";
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
                    PersonalActual = db.Set<Personal>().Where(x => x.PersonalId == id).FirstOrDefault();
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

        public List<Personal> ObtenerListado(bool activo, bool huella = true)
        {
            List<Personal> Personals = new List<Personal>();

            try
            {
                if (activo)
                {
                    if (huella)
                    {
                        Personals = db.Set<Personal>().Where(x => x.Activo == true).AsEnumerable().Select(x => new Personal() { PersonalId = x.PersonalId,Nombre = x.Nombre, Fecha = x.Fecha, TemplateBytes = x.TemplateBytes }).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.PersonalId).ToList();
                    }
                    else
                    {
                        Personals = db.Set<Personal>().Where(x => x.Activo == true).AsEnumerable().Select(x => new Personal() { PersonalId = x.PersonalId, Nombre = x.Nombre, Fecha = x.Fecha }).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.PersonalId).ToList();
                    }
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

        public List<HorarioModel> ObtenerHorarioPersonalPorFecha(DateTime fechaInicial, DateTime fechaFinal, long personalId = 0)
        {
            List<HorarioModel> Horarios = new List<HorarioModel>();

            try
            {
                if (personalId == 0)
                {
                    Horarios = db.Set<PersonalHorario>().Include("Personal").Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal).AsEnumerable().Select(x => new HorarioModel() { PersonaId = x.PersonalId, Nombre = x.Personal.Nombre, Fecha = x.Fecha, Entrada = x.Entrada, Salida = x.Salida }).ToList();
                }
                else
                {
                    Horarios = db.Set<PersonalHorario>().Include("Personal").Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && x.PersonalId == personalId).AsEnumerable().Select(x => new HorarioModel() { PersonaId = x.PersonalId, Nombre = x.Personal.Nombre, Fecha = x.Fecha, Entrada = x.Entrada, Salida = x.Salida }).ToList();
                }
            }
            catch (Exception)
            {
            }

            return Horarios;
        }

        #endregion

    }
}
