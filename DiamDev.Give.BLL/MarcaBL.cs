using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class MarcaBL
    {

        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public MarcaBL()
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
                    Marca MarcaActual = db.Set<Marca>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (MarcaActual != null)
                    {
                        Inicial_Id = MarcaActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;
                }
                catch (Exception)
                {
                }

                return Id;
            }

            private bool Agregar(Marca entidad)
            {
                bool MarcaAgregar = false;

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngMarcaId = new Herramienta().Formato_Correlativo(Id);

                        if (lngMarcaId > 0)
                        {
                            entidad.MarcaId = lngMarcaId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            db.Set<Marca>().Add(entidad);
                            db.SaveChanges();
                            MarcaAgregar = true;
                        }
                    }
                }
                catch (Exception)
                {
                }

                return MarcaAgregar;
            }

            private bool Actualizar(Marca entidad)
            {
                bool MarcaActualizar = false;

                try
                {
                    Marca MarcaActual = ObtenerPorId(entidad.MarcaId);

                    if (MarcaActual.MarcaId > 0)
                    {
                        MarcaActual.Nombre = entidad.Nombre;
                        MarcaActual.Activo = entidad.Activo;

                        db.SaveChanges();
                        MarcaActualizar = true;
                    }
                }
                catch (Exception)
                {
                }

                return MarcaActualizar;
            }


        #endregion

        #region Metodos Publicos

            public string Guardar(Marca entidad)
            {
                string Mensaje = "OK";
                bool OperacionExitosa = false;

                if (entidad.MarcaId > 0)
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

            public Marca ObtenerPorId(long id)
            {
                Marca MarcaActual = new Marca();

                try
                {
                    MarcaActual = db.Set<Marca>().Where(x => x.MarcaId == id && x.Activo == true).FirstOrDefault();
                }
                catch (Exception)
                {
                }

                return MarcaActual;
            }

            public List<Marca> ObtenerListado(bool todos)
            {
                List<Marca> Marcas = new List<Marca>();

                try
                {
                    if (todos)
                    {
                        Marcas = db.Set<Marca>().OrderByDescending(x => x.Fecha).ThenByDescending(x => x.MarcaId).ToList();
                    }
                    else
                    {
                        Marcas = db.Set<Marca>().Where(x => x.Activo == true).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.MarcaId).ToList();
                    }
                }
                catch (Exception)
                {
                }

                return Marcas;
            }

        #endregion

    }
}
