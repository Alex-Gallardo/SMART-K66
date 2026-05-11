using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class LaboratorioBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public LaboratorioBL()
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
                    Laboratorio LaboratorioActual = db.Set<Laboratorio>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (LaboratorioActual != null)
                    {
                        Inicial_Id = LaboratorioActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;
                }
                catch (Exception)
                {
                }

                return Id;
            }

            private string Agregar(Laboratorio entidad)
            {
                string Mensaje = "OK";

                try
                {
                    //Producto Base
                    Producto ProductoBaseActual = db.Set<Producto>().AsNoTracking().Where(x => x.ProductoId == entidad.ProductoBaseId).FirstOrDefault();
                    if (ProductoBaseActual != null)
                    {
                        ProductoInventario InventarioActual = db.Set<ProductoInventario>().Where(x => x.ProductoId == ProductoBaseActual.ProductoId && x.AgenciaId == entidad.AgenciaId).FirstOrDefault();
                        if (InventarioActual != null)
                        {
                            if (entidad.CantidadBase > InventarioActual.Cantidad)
                            {
                                return "Se le informa que la existencia es menor a la cantidad ingresada por favor verificar.";
                            }
                            else
                            {
                                InventarioActual.Cantidad -= entidad.CantidadBase;
                            }                        
                        }
                    }

                    //Producto Destino
                    Producto ProductoDestinoActual = db.Set<Producto>().AsNoTracking().Where(x => x.ProductoId == entidad.ProductoDestinoId).FirstOrDefault();
                    if (ProductoDestinoActual != null)
                    {
                        //Se verifica que exista la unidad de conversión 
                        UnidadConversion ConversionActual = db.Set<UnidadConversion>().AsNoTracking().Where(x => x.UnidadBaseId == ProductoBaseActual.UnidadId && x.UnidadDestinoId == ProductoDestinoActual.UnidadId).FirstOrDefault();
                        if (ConversionActual == null)
                        {
                            return "Se le informa que no se encuentra registrado en el sistema la unidad de conversión que tienen asignada los productos, verificar unidad de medida de productos.";
                        }

                        //Conversion
                        decimal CantidadDestino = 0;
                        if (ConversionActual.OperacionId == 1)
                        {
                            CantidadDestino = entidad.CantidadBase * ConversionActual.CantidadDestino;
                        }
                        else if (ConversionActual.OperacionId == 2)
                        {
                            CantidadDestino = ConversionActual.CantidadBase / entidad.CantidadBase;
                        }

                        //Existencia producto destino
                        ProductoInventario InventarioDestinoActual = db.Set<ProductoInventario>().Where(x => x.ProductoId == ProductoDestinoActual.ProductoId && x.AgenciaId == entidad.AgenciaId).FirstOrDefault();
                        if (InventarioDestinoActual != null)
                        {
                            InventarioDestinoActual.Cantidad += CantidadDestino;
                        }
                        else
                        {
                            db.Set<ProductoInventario>().Add(new ProductoInventario() { ProductoId = ProductoDestinoActual.ProductoId, AgenciaId = entidad.AgenciaId, Cantidad = CantidadDestino, Transito = 0 });
                        }

                        entidad.CantidadDestino = CantidadDestino;
                    }

                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngLaboratorioId = new Herramienta().Formato_Correlativo(Id);

                        if (lngLaboratorioId > 0)
                        {
                            entidad.LaboratorioId = lngLaboratorioId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;                            

                            db.Set<Laboratorio>().Add(entidad);
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

            public string Guardar(Laboratorio entidad)
            {
                string Mensaje = "OK";
               
                if (entidad.LaboratorioId > 0)
                {
                }
                else
                {
                    Mensaje = Agregar(entidad);
                }
          
                return Mensaje;
            }

            public Laboratorio ObtenerPorId(long id, bool todo)
            {
                Laboratorio LaboratorioActual = new Laboratorio();

                try
                {
                    if (todo)
                    {
                        LaboratorioActual = db.Set<Laboratorio>().Include("Agencia").Include("UsuarioCreo").Include("ProductoBase").Include("ProductoBase.Unidad").Include("ProductoDestino").Include("ProductoDestino.Unidad").AsNoTracking().Where(x => x.LaboratorioId == id).FirstOrDefault();
                    }
                    else
                    {
                        LaboratorioActual = db.Set<Laboratorio>().Where(x => x.LaboratorioId == id).FirstOrDefault();
                    }
                }
                catch (Exception)
                {
                }

                return LaboratorioActual;
            }
           
            public List<Laboratorio> ObtenerListadoPorFecha(DateTime fechaInicial, DateTime fechaFinal, long usuarioId)
            {
                List<Laboratorio> Laboratorios = new List<Laboratorio>();

                try
                {
                    var AgenciasIds = db.Set<UsuarioAgencia>().Where(x => x.UsuarioId == usuarioId).AsEnumerable().Select(x => x.AgenciaId).ToList();
                    if (AgenciasIds != null && AgenciasIds.Count() > 0)
                    {
                        Laboratorios = db.Set<Laboratorio>().Include("Agencia").Include("UsuarioCreo").Include("ProductoBase").Include("ProductoBase.Unidad").Include("ProductoDestino").Include("ProductoDestino.Unidad").AsNoTracking().Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && AgenciasIds.Contains(x.AgenciaId)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.LaboratorioId).ToList();
                    }
                }
                catch (Exception)
                {
                }

                return Laboratorios;
            }

        #endregion
    }
}
