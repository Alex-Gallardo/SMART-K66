using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class AnotacionTipoBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public AnotacionTipoBL()
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

                    AnotacionTipo AnotacionTipoActual = db.Set<AnotacionTipo>().AsNoTracking().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (AnotacionTipoActual != null)
                    {
                        Inicial_Id = AnotacionTipoActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;

                }
                catch (Exception)
                {
                }

                return Id;
            }

            private bool Agregar(AnotacionTipo entidad)
            {
                bool AnotacionTipoAgregar = false;

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngAnotacionTipoId = new Herramienta().Formato_Correlativo(Id);

                        if (lngAnotacionTipoId > 0)
                        {
                            entidad.TipoId = lngAnotacionTipoId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            db.Set<AnotacionTipo>().Add(entidad);
                            db.SaveChanges();
                            AnotacionTipoAgregar = true;
                        }
                    }

                }
                catch (Exception)
                {
                }

                return AnotacionTipoAgregar;
            }

            private bool Actualizar(AnotacionTipo entidad)
            {
                bool AnotacionTipoActualizar = false;

                try
                {

                    AnotacionTipo AnotacionTipoActual = ObtenerPorId(entidad.TipoId);

                    if (AnotacionTipoActual.TipoId > 0)
                    {
                        AnotacionTipoActual.Nombre = entidad.Nombre;
                        AnotacionTipoActual.Descuento = entidad.Descuento;

                        db.SaveChanges();
                        AnotacionTipoActualizar = true;
                    }

                }
                catch (Exception)
                {
                }

                return AnotacionTipoActualizar;
            }

        #endregion

        #region Metodos Publicos

            public string Guardar(AnotacionTipo entidad)
            {
                string Mensaje = "OK";
                bool OperacionExitosa = false;

                if (entidad.TipoId > 0)
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

            public AnotacionTipo ObtenerPorId(long id)
            {
                AnotacionTipo AnotacionTipoActual = new AnotacionTipo();

                try
                {
                    AnotacionTipoActual = db.Set<AnotacionTipo>().Where(x => x.TipoId == id).FirstOrDefault();
                }
                catch (Exception)
                {
                }

                return AnotacionTipoActual;
            }

            public List<AnotacionTipo> ObtenerListado()
            {
                List<AnotacionTipo> AnotacionTipos = new List<AnotacionTipo>();

                try
                {
                    AnotacionTipos = db.Set<AnotacionTipo>().AsNoTracking().OrderByDescending(x => x.Fecha).ThenByDescending(x => x.TipoId).ToList();
                }
                catch (Exception)
                {
                }

                return AnotacionTipos;
            }

            public List<AnotacionTipo> Buscar(string search)
            {
                List<AnotacionTipo> AnotacionTipos = new List<AnotacionTipo>();

                try
                {
                    AnotacionTipos = db.Set<AnotacionTipo>().AsNoTracking().Where(x => x.Nombre.Contains(search)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.TipoId).ToList();
                }
                catch (Exception)
                {
                }

                return AnotacionTipos;
            }

        #endregion
    }
}
