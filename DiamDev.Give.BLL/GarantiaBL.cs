using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class GarantiaBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public GarantiaBL()
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
                    Garantia GarantiaActual = db.Set<Garantia>().AsNoTracking().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (GarantiaActual != null)
                    {
                        Inicial_Id = GarantiaActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;
                }
                catch (Exception)
                {
                }

                return Id;
            }

            private string Agregar(Garantia entidad)
            {
                string Mensaje = "OK";

                try
                {
                    if (entidad.Detalles == null)
                    {
                        return "Se le informa que la garantia no contiene productos";
                    }

                    if (entidad.Detalles.Count() == 0)
                    {
                        return "Se le informa que la garantia no contiene productos";                            
                    }

                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngGarantiaId = new Herramienta().Formato_Correlativo(Id);

                        if (lngGarantiaId > 0)
                        {
                            entidad.GarantiaId = lngGarantiaId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            if (entidad.Detalles != null && entidad.Detalles.Count() > 0)
                            {
                                 int DetalleId = 1;
                                 foreach (var Producto in entidad.Detalles)
                                 {
                                     Producto.DetalleId = DetalleId;
                                     Producto.GarantiaId = entidad.GarantiaId;

                                     if (entidad.DocumentoId == 1)
                                     {
                                         FacturaDetalle DetalleFacturaActual = db.Set<FacturaDetalle>().AsNoTracking().Where(x => x.FacturaId == entidad.FacturaId.Value && x.ProductoId == Producto.ProductoId).FirstOrDefault();
                                         if (DetalleFacturaActual != null)
                                         {
                                             Producto.UnidadId = DetalleFacturaActual.UnidadId;                                             
                                         }
                                     }
                                     else if (entidad.DocumentoId == 2)
                                     {
                                         ReciboDetalle DetalleReciboActual = db.Set<ReciboDetalle>().AsNoTracking().Where(x => x.ReciboId == entidad.ReciboId.Value && x.ProductoId == Producto.ProductoId).FirstOrDefault();
                                         if (DetalleReciboActual != null)
                                         {
                                             Producto.UnidadId = DetalleReciboActual.UnidadId;
                                         }                                        
                                     }

                                     DetalleId += 1;
                                 }                                                                
                            }
                           
                            db.Set<Garantia>().Add(entidad);
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

            public string Guardar(Garantia entidad)
            {
                string Mensaje = "OK";

                if (entidad.GarantiaId > 0)
                {
                }
                else
                {
                    Mensaje = Agregar(entidad);
                }

                return Mensaje;
            }

            public string Entrega(long garantiaId, long responsableId)
            {
                string Mensaje = "OK";

                try
                {
                    Garantia GarantiaActual = db.Set<Garantia>().Where(x => x.GarantiaId == garantiaId).FirstOrDefault();
                    if (GarantiaActual != null)
                    {
                        GarantiaActual.UsrEntrega = responsableId;
                        GarantiaActual.FechaEntrega = DateTime.Today;
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            public Garantia ObtenerPorId(long id, bool todo = false)
            {
                Garantia GarantiaActual = new Garantia();

                try
                {
                    if (todo)
                    {
                        GarantiaActual = db.Set<Garantia>().Include("Documento").Include("Factura").Include("Factura.Serie").Include("Factura.Cliente").Include("Recibo").Include("Recibo.Cliente").Include("Detalles").Include("Detalles.Producto").Include("Detalles.Unidad").Include("UsuarioCreo").Include("UsuarioEntrega").AsNoTracking().Where(x => x.GarantiaId == id).FirstOrDefault();
                    }
                    else
                    {
                        GarantiaActual = db.Set<Garantia>().Where(x => x.GarantiaId == id).FirstOrDefault();
                    }
                }
                catch (Exception)
                {
                }

                return GarantiaActual;
            }

            public List<Garantia> ObtenerListado()
            {
                List<Garantia> Garantias = new List<Garantia>();

                try
                {
                    Garantias = db.Set<Garantia>().Include("Documento").Include("Factura").Include("Factura.Serie").Include("Factura.Cliente").Include("Recibo").Include("Recibo.Cliente").AsNoTracking().Where(x => x.FechaEntrega == null).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.GarantiaId).ToList();                    
                }
                catch (Exception)
                {
                }

                return Garantias;
            }

            public List<Garantia> Buscar(string search)
            {
                List<Garantia> Garantias = new List<Garantia>();

                try
                {
                    Garantias = db.Set<Garantia>().Include("Documento").Include("Factura").Include("Factura.Serie").Include("Factura.Cliente").Include("Recibo").Include("Recibo.Cliente").AsNoTracking().Where(x => x.Factura.Cliente.Nombre.ToLower().Contains(search.ToLower()) || x.Recibo.Cliente.Nombre.ToLower().Contains(search.ToLower())).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.GarantiaId).ToList();                    
                }
                catch (Exception)
                {
                }

                return Garantias;
            }

        #endregion
    }
}
