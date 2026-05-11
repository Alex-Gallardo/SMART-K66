using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class CuentaContableBL
    {

        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public CuentaContableBL()
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

                    CuentaContable CuentaContableActual = db.Set<CuentaContable>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (CuentaContableActual != null)
                    {
                        Inicial_Id = CuentaContableActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;

                }
                catch (Exception)
                {
                }

                return Id;
            }

            private string Agregar(CuentaContable entidad)
            {
                string Mensaje = "OK";

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngCuentaContableId = new Herramienta().Formato_Correlativo(Id);

                        if (lngCuentaContableId > 0)
                        {
                            entidad.CuentaId = lngCuentaContableId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            db.Set<CuentaContable>().Add(entidad);

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

            private string Actualizar(CuentaContable entidad)
            {
                string Mensaje = "OK";

                try
                {

                    CuentaContable CuentaActual = ObtenerPorId(entidad.CuentaId, false);

                    if (CuentaActual.CuentaId > 0)
                    {
                        CuentaActual.CuentaPadreId = entidad.CuentaPadreId;
                        CuentaActual.TipoId = entidad.TipoId;
                        CuentaActual.Nombre = entidad.Nombre;
                        CuentaActual.Descripcion = entidad.Descripcion;
                        CuentaActual.Activo = entidad.Activo;

                        db.SaveChanges();                       
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

            public string Guardar(CuentaContable entidad)
            {
                string Mensaje = "OK";
               
                if (entidad.CuentaId > 0)
                {
                    Mensaje = Actualizar(entidad);
                }
                else
                {
                    Mensaje = Agregar(entidad);
                }
               
                return Mensaje;
            }

            public CuentaContable ObtenerPorId(long id, bool todo)
            {
                CuentaContable CuentaActual = new CuentaContable();

                try
                {
                    if (todo)
                    {
                        CuentaActual = db.Set<CuentaContable>().Include("Tipo").Where(x => x.CuentaId == id).FirstOrDefault();
                    }
                    else
                    {
                        CuentaActual = db.Set<CuentaContable>().Where(x => x.CuentaId == id).FirstOrDefault();
                    }
                }
                catch (Exception)
                {
                }

                return CuentaActual;
            }

            public List<CuentaContable> ObtenerListado(bool formato = false, bool todos = true)
            {
                List<CuentaContable> Cuentas = new List<CuentaContable>();

                try
                {
                    if (formato)
                    {
                        if (todos)
                        {
                            Cuentas = db.Set<CuentaContable>().AsEnumerable().Select(x => new CuentaContable() { CuentaId = x.CuentaId, Nombre = x.Nombre }).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.CuentaId).ToList();
                        }
                        else
                        {
                            Cuentas = db.Set<CuentaContable>().Where(x => x.Activo == true).AsEnumerable().Select(x => new CuentaContable() { CuentaId = x.CuentaId, Nombre = x.Nombre }).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.CuentaId).ToList();
                        }
                    }
                    else
                    {
                        Cuentas = db.Set<CuentaContable>().Include("Tipo").OrderByDescending(x => x.Fecha).ThenByDescending(x => x.CuentaId).ToList();
                    }
                }
                catch (Exception)
                {
                }

                return Cuentas;
            }

            public List<CuentaContable> ObtenerCuentas()
            {
                List<CuentaContable> Cuentas = new List<CuentaContable>();

                try
                {
                    Cuentas = db.Set<CuentaContable>().AsEnumerable().Select(x => new CuentaContable() { CuentaId = x.CuentaId, Nombre = string.Format("{0}-{1}", x.Cuenta, x.Nombre) }).OrderBy(x => x.Cuenta).ToList();
                }
                catch (Exception)
                {
                }

                return Cuentas;
            }

            public List<CuentaContable> Buscar(string search)
            {
                List<CuentaContable> Cuentas = new List<CuentaContable>();

                try
                {
                    Cuentas = db.Set<CuentaContable>().Include("Tipo").Where(x => x.Cuenta.Contains(search) || x.Nombre.Contains(search)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.CuentaId).ToList();
                }
                catch (Exception)
                {
                }

                return Cuentas;
            }

        #endregion

    }
}
