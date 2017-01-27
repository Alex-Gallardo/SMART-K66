using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.BLL
{
    public class SerieBL
    {

        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public SerieBL()
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
                    Serie SerieActual = db.Set<Serie>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (SerieActual != null)
                    {
                        Inicial_Id = SerieActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;
                }
                catch (Exception)
                {
                }

                return Id;
            }

            private bool Agregar(Serie entidad)
            {
                bool SerieAgregar = false;

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngSerieId = new Herramienta().Formato_Correlativo(Id);

                        if (lngSerieId > 0)
                        {
                            entidad.SerieId = lngSerieId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            if (entidad.Agencias != null && entidad.Agencias.Count() > 0)
                            {
                                foreach (SerieAgencia Serie in entidad.Agencias)
                                {
                                    Serie.SerieId = entidad.SerieId;                                    
                                }                                
                            }

                            db.Set<Serie>().Add(entidad);
                            db.SaveChanges();
                            SerieAgregar = true;
                        }
                    }

                }
                catch (Exception)
                {
                }

                return SerieAgregar;
            }

            private bool Actualizar(Serie entidad)
            {
                bool SerieActualizar = false;

                try
                {

                    Serie SerieActual = ObtenerPorId(entidad.SerieId);

                    if (SerieActual.SerieId > 0)
                    {                        
                        SerieActual.Nombre = entidad.Nombre;                       
                        SerieActual.Activo = entidad.Activo;

                        if (entidad.Agencias != null && entidad.Agencias.Count() > 0)
                        {
                            var Agencias = db.Set<SerieAgencia>().Where(x => x.SerieId == entidad.SerieId);
                            db.Set<SerieAgencia>().RemoveRange(Agencias);

                            foreach (var Agencia in entidad.Agencias)
                            {
                                Agencia.SerieId = entidad.SerieId;
                                db.Set<SerieAgencia>().Add(Agencia);
                            }
                        }

                        db.SaveChanges();
                        SerieActualizar = true;
                    }

                }
                catch (Exception)
                {
                }

                return SerieActualizar;
            }

        #endregion

        #region Metodos Publicos

            public string Guardar(Serie entidad)
            {
                string Mensaje = "OK";
                bool OperacionExitosa = false;

                if (entidad.SerieId > 0)
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

            public Serie ObtenerPorId(long id, bool todo = false)
            {
                Serie SerieActual = new Serie();

                try
                {
                    if (todo)
                    {
                        SerieActual = db.Set<Serie>().Include("Agencias").Include("Agencias.Agencia").Where(x => x.SerieId == id).FirstOrDefault();
                    }
                    else
                    {
                        SerieActual = db.Set<Serie>().Where(x => x.SerieId == id).FirstOrDefault();
                    }
                }
                catch (Exception)
                {
                }

                return SerieActual;
            }

            public List<Serie> ObtenerListado(bool todos)
            {
                List<Serie> Series = new List<Serie>();

                try
                {
                    if (todos)
                    {
                        Series = db.Set<Serie>().Include("Agencias").OrderByDescending(x => x.Fecha).ThenByDescending(x => x.SerieId).ToList();
                    }
                    else
                    {
                        Series = db.Set<Serie>().Where(x => x.Activo == true).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.SerieId).ToList();
                    }
                }
                catch (Exception)
                {
                }

                return Series;
            }

            public List<Serie> Buscar(string search)
            {
                List<Serie> Series = new List<Serie>();

                try
                {
                    Series = db.Set<Serie>().Include("Agencias").Where(x => x.Nombre.Contains(search)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.SerieId).ToList();
                }
                catch (Exception)
                {
                }

                return Series;
            }

            public List<Serie> ObtenerSeriesPorAgencia(long agenciaId) 
            {
                List<Serie> Series = new List<Serie>();

                try
                {
                    Series = db.Set<SerieAgencia>().Where(x => x.AgenciaId == agenciaId).Join(db.Set<Serie>().Where(x => x.Activo == true), SA => SA.SerieId, S => S.SerieId, (SA, S) => new { S }).Select(x => x.S).ToList();
                }
                catch (Exception)
                {
                }

                return Series;
            }

            public SerieAgenciaFactura ObtenerFacturaActual(long agenciaId, long serieId) 
            {
                SerieAgenciaFactura FacturaActual = new SerieAgenciaFactura();

                try
                {
                    FacturaActual = db.Set<SerieAgenciaFactura>().Where(x => x.AgenciaId == agenciaId && x.SerieId == serieId && x.Operada == false).FirstOrDefault();
                }
                catch (Exception)
                {
                }

                return FacturaActual;
            }

        #endregion

    }
}

