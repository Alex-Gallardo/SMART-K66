using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.BLL
{
    public class UnidadBL
    {

        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public UnidadBL()
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

                    Unidad UnidadActual = db.Set<Unidad>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (UnidadActual != null)
                    {
                        Inicial_Id = UnidadActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;

                }
                catch (Exception)
                {
                }

                return Id;
            }

            private bool Agregar(Unidad entidad)
            {
                bool UnidadAgregar = false;

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {

                        long lngUnidadId = new Herramienta().Formato_Correlativo(Id);

                        if (lngUnidadId > 0)
                        {
                            entidad.UnidadId = lngUnidadId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            db.Set<Unidad>().Add(entidad);
                            db.SaveChanges();
                            UnidadAgregar = true;
                        }
                    }

                }
                catch (Exception)
                {
                }

                return UnidadAgregar;
            }

            private bool Actualizar(Unidad entidad)
            {
                bool UnidadActualizar = false;

                try
                {

                    Unidad UnidadActual = ObtenerPorId(entidad.UnidadId);

                    if (UnidadActual.UnidadId > 0)
                    {
                        UnidadActual.Codigo = entidad.Codigo;
                        UnidadActual.Nombre = entidad.Nombre;
                        UnidadActual.Activo = entidad.Activo;

                        db.SaveChanges();
                        UnidadActualizar = true;
                    }

                }
                catch (Exception)
                {
                }

                return UnidadActualizar;
            }


        #endregion

        #region Metodos Publicos

            public string Guardar(Unidad entidad)
            {
                string Mensaje = "OK";
                bool OperacionExitosa = false;

                if (entidad.UnidadId > 0)
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

            public Unidad ObtenerPorId(long id)
            {
                Unidad UnidadActual = new Unidad();

                try
                {
                    UnidadActual = db.Set<Unidad>().Where(x => x.UnidadId == id).FirstOrDefault();
                }
                catch (Exception)
                {
                }

                return UnidadActual;
            }

            public List<Unidad> ObtenerListado(bool todos)
            {
                List<Unidad> Unidades = new List<Unidad>();

                try
                {
                    if (todos)
                    {
                        Unidades = db.Set<Unidad>().OrderByDescending(x => x.Fecha).ThenByDescending(x => x.UnidadId).ToList();
                    }
                    else
                    {
                        Unidades = db.Set<Unidad>().Where(x => x.Activo == true).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.UnidadId).ToList();
                    }
                }
                catch (Exception)
                {
                }

                return Unidades;
            }

        #endregion

    }
}
