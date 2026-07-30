using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class PersonalHorarioBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public PersonalHorarioBL()
            {
                this.db = new GiveContext();
            }

        #endregion

        #region Metodos Privados
         
            private string Agregar(PersonalHorario entidad)
            {
                string Mensaje = "OK";
                var fecha = entidad.Fecha.ToShortDateString();
                entidad.Fecha = DateTime.Parse(fecha);

                try
                {
                    db.Set<PersonalHorario>().Add(entidad);
                    db.SaveChanges();                   
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            private string Actualizar(PersonalHorario entidad)
            {
                string Mensaje = "OK";

                try
                {
                    PersonalHorario HorarioActual = db.Set<PersonalHorario>().Where(x => x.PersonalId == entidad.PersonalId && x.Fecha == DateTime.Today).FirstOrDefault();

                    if (HorarioActual.PersonalId > 0)
                    {
                        HorarioActual.Salida = entidad.Salida;
                     
                        db.SaveChanges();                       
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            private bool Existe(long personalId, DateTime fechaActual) 
            {
                var fecha = db.Set<PersonalHorario>().Where(x => x.PersonalId == personalId).ToList();
                var result = db.Set<PersonalHorario>().Where(x => x.PersonalId == personalId && x.Fecha == fechaActual).Count() > 0;

                return result; 
            }

        #endregion

        #region Metodos Publicos

            public string Guardar(PersonalHorario entidad)
            {
                string Mensaje = "OK";
               
                if (Existe(entidad.PersonalId, DateTime.Today))
                {
                    Mensaje = Actualizar(entidad);
                }
                else
                {
                    Mensaje = Agregar(entidad);
                }
               
                return Mensaje;
            }

            public List<PersonalHorario> Buscar(string search)
            {
                List<PersonalHorario> Personals = new List<PersonalHorario>();

                try
                {
                    Personals = db.Set<PersonalHorario>().Include("Personal").Where(x => x.Personal.Nombre.Contains(search)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.PersonalId).ToList();
                }
                catch (Exception)
                {
                }

                return Personals;
            }

        public List<PersonalHorario> ObtenerListado(bool activo)
        {
            List<PersonalHorario> Personals = new List<PersonalHorario>();

            try
            {
                if (activo)
                {
                    Personals = db.Set<PersonalHorario>().Include("Personal").Where(x => x.Personal.Activo == true).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.PersonalId).ToList();
                }
                else
                {
                    Personals = db.Set<PersonalHorario>().Include("Personal").OrderByDescending(x => x.Fecha).ThenByDescending(x => x.PersonalId).ToList();
                }
            }
            catch (Exception)
            {
            }

            return Personals;
        }

        public List<HorarioModel> ObtenerHorarioxPersonalId(DateTime fechaInicial, DateTime fechaFinal, long personalId) 
        {
            List<HorarioModel> Horarios = new List<HorarioModel>();

            try
            {
                Horarios = db.Set<PersonalHorario>().Include("Personal").AsNoTracking().Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && x.PersonalId == personalId).AsEnumerable().Select(x => new HorarioModel() { PersonaId = x.PersonalId, Nombre = x.Personal.Nombre, Fecha = x.Fecha, Entrada = x.Entrada, Salida = x.Salida, Laborado = x.Salida == null ? TimeSpan.Parse("0") : new TimeSpan(x.Salida.Value.Ticks - x.Entrada.Ticks) }).OrderByDescending(x => x.Fecha).ToList();
            }
            catch (Exception)
            {
            }

            return Horarios;
        }

        #endregion
    }
}
