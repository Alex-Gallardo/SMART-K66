using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class PaqueteEmpresaBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public PaqueteEmpresaBL()
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
                    PaqueteEmpresa PaqueteEmpresaActual = db.Set<PaqueteEmpresa>().AsNoTracking().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (PaqueteEmpresaActual != null)
                    {
                        Inicial_Id = PaqueteEmpresaActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;
                }
                catch (Exception)
                {}

                return Id;
            }

            private string Agregar(PaqueteEmpresa entidad)
            {
                string Mensaje = "OK";

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngPaqueteEmpresaId = new Herramienta().Formato_Correlativo(Id);

                        if (lngPaqueteEmpresaId > 0)
                        {
                            entidad.PaqueteEmpresaId = lngPaqueteEmpresaId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            //SE OBTIENE EL PAQUETE
                            Paquete PaqueteActual = db.Set<Paquete>().AsNoTracking().Where(x => x.PaqueteId == entidad.PaqueteId).FirstOrDefault();
                            if (PaqueteActual == null)
                            {
                                return "El paquete no se encuentra registrado en el sistema";
                            }

                            entidad.SaldoFactura = PaqueteActual.CantidadDTE;
                            entidad.FechaVencimiento = DateTime.Today.AddMonths(PaqueteActual.Vigencia);
                            entidad.Costo = PaqueteActual.Costo;
                            entidad.Precio = PaqueteActual.Precio;

                            db.Set<PaqueteEmpresa>().Add(entidad);
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

        #endregion

        #region Metodos Publicos

            public string Guardar(PaqueteEmpresa entidad)
            {
                string Mensaje = "OK";
                
                if (entidad.PaqueteEmpresaId == 0)              
                {
                    Mensaje = Agregar(entidad);
                }

                return Mensaje;
            }

            public List<PaqueteEmpresa> ObtenerPaquetesxEmpresa(long id) 
            {
                List<PaqueteEmpresa> Paquetes = new List<PaqueteEmpresa>();

                try
                {
                    Paquetes = db.Set<PaqueteEmpresa>().Include("Paquete").Include("FormaPago").AsNoTracking().Where(x => x.EmpresaId == id && x.SaldoFactura > 0).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.PaqueteEmpresaId).ToList();
                }
                catch (Exception)
                {}

                return Paquetes;
            }

        #endregion
    }
}
