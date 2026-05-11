using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class PoliticaBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public PoliticaBL()
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

                    Politica PoliticaActual = db.Set<Politica>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (PoliticaActual != null)
                    {
                        Inicial_Id = PoliticaActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;

                }
                catch (Exception)
                {
                }

                return Id;
            }

            private bool Agregar(Politica entidad)
            {
                bool PoliticaAgregar = false;

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngPoliticaId = new Herramienta().Formato_Correlativo(Id);

                        if (lngPoliticaId > 0)
                        {
                            entidad.PoliticaId = lngPoliticaId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            db.Set<Politica>().Add(entidad);
                            db.SaveChanges();
                            PoliticaAgregar = true;
                        }
                    }

                }
                catch (Exception)
                {
                }

                return PoliticaAgregar;
            }

            private bool Actualizar(Politica entidad)
            {
                bool PoliticaActualizar = false;

                try
                {
                    Politica PoliticaActual = ObtenerPorId(entidad.PoliticaId);

                    if (PoliticaActual.PoliticaId > 0)
                    {
                        PoliticaActual.TipoId = entidad.TipoId;
                        PoliticaActual.Nombre = entidad.Nombre;
                        PoliticaActual.Activo = entidad.Activo;

                        db.SaveChanges();
                        PoliticaActualizar = true;
                    }

                }
                catch (Exception)
                {
                }

                return PoliticaActualizar;
            }

        #endregion

        #region Metodos Publicos

            public string Guardar(Politica entidad)
            {
                string Mensaje = "OK";
                bool OperacionExitosa = false;

                if (entidad.PoliticaId > 0)
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

            public Politica ObtenerPorId(long id, bool todo = false)
            {
                Politica PoliticaActual = new Politica();

                try
                {
                    if (todo)
                    {
                        PoliticaActual = db.Set<Politica>().Include("Tipo").Where(x => x.PoliticaId == id).FirstOrDefault();
                    }
                    else
                    {
                        PoliticaActual = db.Set<Politica>().Where(x => x.PoliticaId == id).FirstOrDefault();
                    }
                }
                catch (Exception)
                {
                }

                return PoliticaActual;
            }

            public List<Politica> ObtenerListado()
            {
                List<Politica> Politicas = new List<Politica>();

                try
                {
                    Politicas = db.Set<Politica>().Include("Tipo").AsNoTracking().Where(x => x.Activo).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.PoliticaId).ToList();
                }
                catch (Exception)
                {
                }

                return Politicas;
            }

            public List<Politica> Buscar(string search)
            {
                List<Politica> Politicas = new List<Politica>();

                try
                {
                    Politicas = db.Set<Politica>().Include("Tipo").AsNoTracking().Where(x => x.Nombre.Contains(search)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.PoliticaId).ToList();
                }
                catch (Exception)
                {
                }

                return Politicas;
            }

            public List<Politica> ObtenerPoliticasxTipoId(int tipoId)
            {
                List<Politica> Politicas = new List<Politica>();

                try
                {
                    Politicas = db.Set<Politica>().AsNoTracking().Where(x => x.TipoId == tipoId && x.Activo).ToList();
                }
                catch (Exception)
                {
                }

                return Politicas;
            }

        #endregion
    }
}
