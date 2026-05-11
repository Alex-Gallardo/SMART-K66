using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class PuestoBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public PuestoBL()
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

                    Puesto PuestoActual = db.Set<Puesto>().AsNoTracking().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (PuestoActual != null)
                    {
                        Inicial_Id = PuestoActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;

                }
                catch (Exception)
                {
                }

                return Id;
            }

            private bool Agregar(Puesto entidad)
            {
                bool PuestoAgregar = false;

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngPuestoId = new Herramienta().Formato_Correlativo(Id);

                        if (lngPuestoId > 0)
                        {
                            entidad.PuestoId = lngPuestoId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            db.Set<Puesto>().Add(entidad);
                            db.SaveChanges();
                            PuestoAgregar = true;
                        }
                    }

                }
                catch (Exception)
                {
                }

                return PuestoAgregar;
            }

            private bool Actualizar(Puesto entidad)
            {
                bool PuestoActualizar = false;

                try
                {

                    Puesto PuestoActual = ObtenerPorId(entidad.PuestoId);

                    if (PuestoActual.PuestoId > 0)
                    {
                        PuestoActual.Nombre = entidad.Nombre;

                        db.SaveChanges();
                        PuestoActualizar = true;
                    }

                }
                catch (Exception)
                {
                }

                return PuestoActualizar;
            }

        #endregion

        #region Metodos Publicos

            public string Guardar(Puesto entidad)
            {
                string Mensaje = "OK";
                bool OperacionExitosa = false;

                if (entidad.PuestoId > 0)
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

            public Puesto ObtenerPorId(long id)
            {
                Puesto PuestoActual = new Puesto();

                try
                {
                    PuestoActual = db.Set<Puesto>().Where(x => x.PuestoId == id).FirstOrDefault();
                }
                catch (Exception)
                {
                }

                return PuestoActual;
            }

            public List<Puesto> ObtenerListado()
            {
                List<Puesto> Puestos = new List<Puesto>();

                try
                {
                    Puestos = db.Set<Puesto>().AsNoTracking().OrderByDescending(x => x.Fecha).ThenByDescending(x => x.PuestoId).ToList();
                }
                catch (Exception)
                {
                }

                return Puestos;
            }

            public List<Puesto> Buscar(string search)
            {
                List<Puesto> Puestos = new List<Puesto>();

                try
                {
                    Puestos = db.Set<Puesto>().AsNoTracking().Where(x => x.Nombre.Contains(search)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.PuestoId).ToList();
                }
                catch (Exception)
                {
                }

                return Puestos;
            }

        #endregion
    }
}
