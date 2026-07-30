using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class PoliticaCategoriaBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public PoliticaCategoriaBL()
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

                    PoliticaCategoria PoliticaCategoriaActual = db.Set<PoliticaCategoria>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (PoliticaCategoriaActual != null)
                    {
                        Inicial_Id = PoliticaCategoriaActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;

                }
                catch (Exception)
                {
                }

                return Id;
            }

            private bool Agregar(PoliticaCategoria entidad)
            {
                bool PoliticaCategoriaAgregar = false;

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngPoliticaCategoriaId = new Herramienta().Formato_Correlativo(Id);

                        if (lngPoliticaCategoriaId > 0)
                        {
                            entidad.PoliticaCategoriaId = lngPoliticaCategoriaId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            if (entidad.Politicas != null && entidad.Politicas.Count() > 0)
                            {
                                foreach (var item in entidad.Politicas)
                                {
                                    item.PoliticaCategoriaId = entidad.PoliticaCategoriaId;
                                }
                            }

                            db.Set<PoliticaCategoria>().Add(entidad);
                            db.SaveChanges();
                            PoliticaCategoriaAgregar = true;
                        }
                    }

                }
                catch (Exception)
                {
                }

                return PoliticaCategoriaAgregar;
            }

            private bool Actualizar(PoliticaCategoria entidad)
            {
                bool PoliticaCategoriaActualizar = false;

                try
                {
                    PoliticaCategoria PoliticaCategoriaActual = ObtenerPorId(entidad.PoliticaCategoriaId);

                    if (PoliticaCategoriaActual.PoliticaCategoriaId > 0)
                    {
                        PoliticaCategoriaActual.Nombre = entidad.Nombre;
                        PoliticaCategoriaActual.Activo = entidad.Activo;

                        if (entidad.Politicas != null && entidad.Politicas.Count() > 0)
                        {
                            List<PoliticaCategoriaPolitica> Politicas = db.Set<PoliticaCategoriaPolitica>().Where(x => x.PoliticaCategoriaId == entidad.PoliticaCategoriaId).ToList();
                            db.Set<PoliticaCategoriaPolitica>().RemoveRange(Politicas);

                            foreach (var item in entidad.Politicas)
                            {
                                item.PoliticaCategoriaId = entidad.PoliticaCategoriaId;
                                db.Set<PoliticaCategoriaPolitica>().Add(item);
                            }
                        }

                        db.SaveChanges();
                        PoliticaCategoriaActualizar = true;
                    }

                }
                catch (Exception)
                {
                }

                return PoliticaCategoriaActualizar;
            }

        #endregion

        #region Metodos Publicos

            public string Guardar(PoliticaCategoria entidad)
            {
                string Mensaje = "OK";
                bool OperacionExitosa = false;

                if (entidad.PoliticaCategoriaId > 0)
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

            public PoliticaCategoria ObtenerPorId(long id, bool todo = false)
            {
                PoliticaCategoria PoliticaCategoriaActual = new PoliticaCategoria();

                try
                {
                    if (todo)
                    {
                        PoliticaCategoriaActual = db.Set<PoliticaCategoria>().Include("Politicas").Include("Politicas.Politica").AsNoTracking().Where(x => x.PoliticaCategoriaId == id).FirstOrDefault();
                    }
                    else
                    {
                        PoliticaCategoriaActual = db.Set<PoliticaCategoria>().Where(x => x.PoliticaCategoriaId == id).FirstOrDefault();
                    }
                }
                catch (Exception)
                {
                }

                return PoliticaCategoriaActual;
            }

            public List<PoliticaCategoria> ObtenerListado(bool todo)
            {
                List<PoliticaCategoria> PoliticaCategorias = new List<PoliticaCategoria>();
                List<int> TiposIds = new List<int>() { 1, 2 };

                try
                {
                    if (todo)
                    {
                        PoliticaCategorias = db.Set<PoliticaCategoria>().AsNoTracking().Where(x => x.Activo && TiposIds.Contains(x.TipoId)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.PoliticaCategoriaId).ToList();
                    }
                    else
                    {
                        PoliticaCategorias = db.Set<PoliticaCategoria>().AsNoTracking().Where(x => x.Activo && x.PoliticaCategoriaId > 0).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.PoliticaCategoriaId).ToList();
                    }
                }
                catch (Exception)
                {
                }

                return PoliticaCategorias;
            }

            public List<PoliticaCategoria> Buscar(string search)
            {
                List<PoliticaCategoria> PoliticaCategorias = new List<PoliticaCategoria>();

                try
                {
                    PoliticaCategorias = db.Set<PoliticaCategoria>().AsNoTracking().Where(x => x.Nombre.Contains(search)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.PoliticaCategoriaId).ToList();
                }
                catch (Exception)
                {
                }

                return PoliticaCategorias;
            }

            public List<PoliticaCategoria> PoliticasxCategoria(long categoriaId)
            {
                List<PoliticaCategoria> Politicas = new List<PoliticaCategoria>();

                try
                {
                    Politicas = db.Set<PoliticaCategoria>().Include("Politicas").Include("Politicas.Politica").AsNoTracking().Where(x => x.PoliticaCategoriaId == categoriaId && x.Activo).ToList();
                }
                catch (Exception)
                {
                }

                return Politicas;
            }

        #endregion
    }
}
