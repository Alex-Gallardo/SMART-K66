using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class DepartamentoBL
    {

        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public DepartamentoBL()
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

                    Departamento DepartamentoActual = db.Set<Departamento>().AsNoTracking().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (DepartamentoActual != null)
                    {
                        Inicial_Id = DepartamentoActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;

                }
                catch (Exception)
                {
                }

                return Id;
            }

            private bool Agregar(Departamento entidad)
            {
                bool DepartamentoAgregar = false;

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {

                        long lngDepartamentoId = new Herramienta().Formato_Correlativo(Id);

                        if (lngDepartamentoId > 0)
                        {
                            entidad.DepartamentoId = lngDepartamentoId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            db.Set<Departamento>().Add(entidad);
                            db.SaveChanges();
                            DepartamentoAgregar = true;
                        }
                    }

                }
                catch (Exception)
                {
                }

                return DepartamentoAgregar;
            }

            private bool Actualizar(Departamento entidad)
            {
                bool DepartamentoActualizar = false;

                try
                {

                    Departamento UnidadActual = ObtenerPorId(entidad.DepartamentoId);

                    if (UnidadActual.DepartamentoId > 0)
                    {

                        UnidadActual.Nombre = entidad.Nombre;
                        UnidadActual.Activo = entidad.Activo;

                        db.SaveChanges();
                        DepartamentoActualizar = true;
                    }

                }
                catch (Exception)
                {
                }

                return DepartamentoActualizar;
            }


        #endregion

        #region Metodos Publicos

            public string Guardar(Departamento entidad)
            {
                string Mensaje = "OK";
                bool OperacionExitosa = false;

                if (entidad.DepartamentoId > 0)
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

            public Departamento ObtenerPorId(long id)
            {
                Departamento DepartamentoActual = new Departamento();

                try
                {
                    DepartamentoActual = db.Set<Departamento>().Where(x => x.DepartamentoId == id).FirstOrDefault();
                }
                catch (Exception)
                {
                }

                return DepartamentoActual;
            }

            public List<Departamento> ObtenerListado(bool todos)
            {
                List<Departamento> Departamentos = new List<Departamento>();

                try
                {
                    if (todos)
                    {
                        Departamentos = db.Set<Departamento>().AsNoTracking().OrderByDescending(x => x.Fecha).ThenByDescending(x => x.DepartamentoId).ToList();
                    }
                    else
                    {
                        Departamentos = db.Set<Departamento>().AsNoTracking().Where(x => x.Activo == true).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.DepartamentoId).ToList();
                    }
                }
                catch (Exception)
                {
                }

                return Departamentos;
            }

            public List<Departamento> ObtenerListadoConsulta()
            {
                List<Departamento> Departamentos = new List<Departamento>();
                List<long> DepartamentoIds = new List<long>() { 20151023003, 20151023004, 20151023005, 20151023007 };

                try
                {
                    Departamentos = db.Set<Departamento>().AsNoTracking().Where(x => x.Activo == true && DepartamentoIds.Contains(x.DepartamentoId)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.DepartamentoId).ToList();
                }
                catch (Exception)
                {
                }

                return Departamentos;
            }
        #endregion

    }
}
