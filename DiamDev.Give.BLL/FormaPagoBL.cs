using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class FormaPagoBL
    {

        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public FormaPagoBL()
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
                    FormaPago FormaPagoActual = db.Set<FormaPago>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (FormaPagoActual != null)
                    {
                        Inicial_Id = FormaPagoActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;
                }
                catch (Exception)
                {
                }

                return Id;
            }

            private bool Agregar(FormaPago entidad)
            {
                bool FormaPagoAgregar = false;

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngFormaPagoId = new Herramienta().Formato_Correlativo(Id);

                        if (lngFormaPagoId > 0)
                        {
                            entidad.FormaPagoId = lngFormaPagoId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            db.Set<FormaPago>().Add(entidad);
                            db.SaveChanges();
                            FormaPagoAgregar = true;
                        }
                    }
                }
                catch (Exception)
                {
                }

                return FormaPagoAgregar;
            }

            private bool Actualizar(FormaPago entidad)
            {
                bool FormaPagoActualizar = false;

                try
                {
                    FormaPago FormaPagoActual = ObtenerPorId(entidad.FormaPagoId);

                    if (FormaPagoActual.FormaPagoId > 0)
                    {
                        FormaPagoActual.Nombre = entidad.Nombre;
                        FormaPagoActual.Activo = entidad.Activo;

                        db.SaveChanges();
                        FormaPagoActualizar = true;
                    }
                }
                catch (Exception)
                {
                }

                return FormaPagoActualizar;
            }


        #endregion

        #region Metodos Publicos

            public string Guardar(FormaPago entidad)
            {
                string Mensaje = "OK";
                bool OperacionExitosa = false;

                if (entidad.FormaPagoId > 0)
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

            public FormaPago ObtenerPorId(long id)
            {
                FormaPago FormaPagoActual = new FormaPago();

                try
                {
                    FormaPagoActual = db.Set<FormaPago>().Where(x => x.FormaPagoId == id).FirstOrDefault();
                }
                catch (Exception)
                {
                }

                return FormaPagoActual;
            }

            public List<FormaPago> ObtenerListado(bool todos)
            {
                List<FormaPago> FormaPagos = new List<FormaPago>();

                try
                {
                    if (todos)
                    {
                        FormaPagos = db.Set<FormaPago>().OrderByDescending(x => x.Fecha).ThenByDescending(x => x.FormaPagoId).ToList();
                    }
                    else
                    {
                        FormaPagos = db.Set<FormaPago>().Where(x => x.Activo == true).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.FormaPagoId).ToList();
                    }
                }
                catch (Exception)
                {
                }

                return FormaPagos;
            }

        #endregion

    }
}
