using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            private bool Agregar(PersonalHorario entidad)
            {
                bool HorarioAgregar = false;

                try
                {
                    entidad.Entrada = DateTime.Now;
                    db.Set<PersonalHorario>().Add(entidad);
                    db.SaveChanges();
                    HorarioAgregar = true;
                }
                catch (Exception)
                {
                }

                return HorarioAgregar;
            }

            private bool Actualizar(PersonalHorario entidad)
            {
                bool HorarioActualizar = false;

                try
                {
                    PersonalHorario HorarioActual = db.Set<PersonalHorario>().Where(x => x.PersonalId == entidad.PersonalId && x.Fecha == DateTime.Today).FirstOrDefault();

                    if (HorarioActual.PersonalId > 0)
                    {
                        if (HorarioActual.Salida == null)
                        {
                            TimeSpan duracion = DateTime.Now - HorarioActual.Entrada;

                            if (duracion.Minutes >= 5)
                            {
                                HorarioActual.Salida = DateTime.Now;
                                db.SaveChanges();
                            }
                        }

                        HorarioActualizar = true;
                    }
                }
                catch (Exception)
                {
                }

                return HorarioActualizar;
            }

            private bool Existe(long personalId, DateTime fechaActual)
            {
                return db.Set<PersonalHorario>().Where(x => x.PersonalId == personalId && x.Fecha == fechaActual).Count() > 0;
            }

        #endregion

        #region Metodos Publicos

            public string Guardar(PersonalHorario entidad)
            {
                string Mensaje = "OK";
                bool OperacionExitosa = false;

                if (Existe(entidad.PersonalId, DateTime.Today))
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

      #endregion
    }
}
