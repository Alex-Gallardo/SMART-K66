using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class BancoBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public BancoBL()
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
                    Banco BancoActual = db.Set<Banco>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (BancoActual != null)
                    {
                        Inicial_Id = BancoActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;
                }
                catch (Exception)
                {
                }

                return Id;
            }

            private string Agregar(Banco entidad)
            {
                string Mensaje = "OK";

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngBancoId = new Herramienta().Formato_Correlativo(Id);

                        if (lngBancoId > 0)
                        {
                            entidad.BancoId = lngBancoId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            db.Set<Banco>().Add(entidad);
                            db.SaveChanges();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            private string Actualizar(Banco entidad)
            {
                string Mensaje = "OK";

                try
                {
                    Banco BancoActual = ObtenerPorId(entidad.BancoId);

                    if (BancoActual.BancoId > 0)
                    {
                        BancoActual.Nombre = entidad.Nombre;

                        db.SaveChanges();
                    }
                    else
                    {
                        Mensaje = "El banco seleccionada no se encuentra con ID valido";
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }


        #endregion

        #region Metodos Publicos

            public string Guardar(Banco entidad)
            {
                string Mensaje = "OK";

                if (entidad.BancoId > 0)
                {
                    Mensaje = Actualizar(entidad);
                }
                else
                {
                    Mensaje = Agregar(entidad);
                }

                return Mensaje;
            }

            public Banco ObtenerPorId(long id)
            {
                Banco BancoActual = new Banco();

                try
                {
                    BancoActual = db.Set<Banco>().Where(x => x.BancoId == id).FirstOrDefault();
                }
                catch (Exception)
                {
                }

                return BancoActual;
            }

            public List<Banco> ObtenerListado()
            {
                List<Banco> Bancos = new List<Banco>();

                try
                {
                    Bancos = db.Set<Banco>().OrderByDescending(x => x.Fecha).ThenByDescending(x => x.BancoId).ToList();
                }
                catch (Exception)
                {
                }

                return Bancos;
            }

            public List<Banco> Buscar(string search)
            {
                List<Banco> Bancos = new List<Banco>();

                try
                {
                    Bancos = db.Set<Banco>().Where(x => x.Nombre.ToLower().Contains(search.ToLower())).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.BancoId).Take(200).ToList();
                }
                catch (Exception)
                {
                }

                return Bancos;
            }

        #endregion
    }
}
