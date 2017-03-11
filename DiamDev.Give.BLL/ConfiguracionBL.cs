using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class ConfiguracionBL
    {

        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public ConfiguracionBL()
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
                    Configuracion ConfiguracionActual = db.Set<Configuracion>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (ConfiguracionActual != null)
                    {
                        Inicial_Id = ConfiguracionActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;

                }
                catch (Exception)
                {
                }

                return Id;
            }

            private bool Agregar(Configuracion entidad)
            {
                bool ConfiguracionAgregar = false;

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngConfiguracionId = new Herramienta().Formato_Correlativo(Id);

                        if (lngConfiguracionId > 0)
                        {
                            entidad.ConfiguracionId = lngConfiguracionId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            if (entidad.ConfiguracionPadreId == 0)
                            {
                                entidad.ConfiguracionPadreId = null;
                            }

                            db.Set<Configuracion>().Add(entidad);

                            db.SaveChanges();
                            ConfiguracionAgregar = true;
                        }
                    }

                }
                catch (Exception)
                {
                }

                return ConfiguracionAgregar;
            }

            private bool Actualizar(Configuracion entidad)
            {
                bool ConfiguracionActualizar = false;

                try
                {

                    Configuracion ConfiguracionActual = ObtenerPorId(entidad.ConfiguracionId);

                    if (ConfiguracionActual.ConfiguracionId > 0)
                    {
                        ConfiguracionActual.Nombre = entidad.Nombre;
                        ConfiguracionActual.Valor = entidad.Valor;

                        db.SaveChanges();
                        ConfiguracionActualizar = true;
                    }

                }
                catch (Exception)
                {
                }

                return ConfiguracionActualizar;
            }

        #endregion

        #region Metodos Publicos

            public string Guardar(Configuracion entidad)
            {
                string Mensaje = "OK";
                bool OperacionExitosa = false;

                if (entidad.ConfiguracionId > 0)
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

            public Configuracion ObtenerPorId(long id)
            {
                Configuracion ConfiguracionActual = new Configuracion();

                try
                {
                    ConfiguracionActual = db.Set<Configuracion>().Where(x => x.ConfiguracionId == id).FirstOrDefault();
                }
                catch (Exception)
                {
                }

                return ConfiguracionActual;
            }

            public Configuracion ObtenerPorIdentificador(string identificadorId, GiveContext db)
            {
                Configuracion ConfiguracionActual = new Configuracion();

                try
                {
                    ConfiguracionActual = db.Set<Configuracion>().Where(x => x.Identificador.Equals(identificadorId)).FirstOrDefault();
                }
                catch (Exception)
                {
                }

                return ConfiguracionActual;
            }

            public List<Configuracion> ObtenerListado(bool formato = false, bool todos = true, bool todosPadre = false)
            {
                List<Configuracion> Configuracions = new List<Configuracion>();

                try
                {
                    if (formato)
                    {
                        if (todos)
                        {
                            Configuracions = db.Set<Configuracion>().AsEnumerable().Select(x => new Configuracion() { ConfiguracionId = x.ConfiguracionId, Nombre = x.Nombre }).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ConfiguracionId).ToList();
                        }
                        else
                        {
                            if (todosPadre)
                            {
                                Configuracions = db.Set<Configuracion>().Where(x => x.ConfiguracionPadreId == null).AsEnumerable().Select(x => new Configuracion() { ConfiguracionId = x.ConfiguracionId, Nombre = x.Nombre }).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ConfiguracionId).ToList();
                            }
                            else
                            {
                                Configuracions = db.Set<Configuracion>().AsEnumerable().Select(x => new Configuracion() { ConfiguracionId = x.ConfiguracionId, Nombre = x.Nombre }).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ConfiguracionId).ToList();
                            }
                        }
                    }
                    else
                    {
                        Configuracions = db.Set<Configuracion>().OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ConfiguracionId).ToList();
                    }
                }
                catch (Exception)
                {
                }

                return Configuracions;
            }

            public List<Configuracion> Buscar(string search)
            {
                List<Configuracion> Configuracions = new List<Configuracion>();

                try
                {
                    Configuracions = db.Set<Configuracion>().Where(x => x.Nombre.Contains(search)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ConfiguracionId).ToList();
                }
                catch (Exception)
                {
                }

                return Configuracions;
            }

            public List<Configuracion> ObtenerPorPadreId(long padreId, GiveContext db)
            {
                List<Configuracion> Configuraciones = new List<Configuracion>();

                try
                {
                    Configuraciones = db.Set<Configuracion>().Where(x => x.ConfiguracionPadreId == padreId).ToList();
                }
                catch (Exception)
                {
                }

                return Configuraciones;
            }

        #endregion

    }
}
