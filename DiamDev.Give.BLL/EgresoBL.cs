using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class EgresoBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public EgresoBL()
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
                    Egreso EgresoActual = db.Set<Egreso>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (EgresoActual != null)
                    {
                        Inicial_Id = EgresoActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;
                }
                catch (Exception)
                {
                }

                return Id;
            }

            private string Agregar(Egreso entidad)
            {
                string Mensaje = "OK";

                try
                {
                    List<EgresoDetalle> Productos = new List<EgresoDetalle>();
                    if (entidad.Detalles != null && entidad.Detalles.Count() > 0)
                    {
                        foreach (var item in entidad.Detalles)
                        {
                            bool Existe = Productos.Where(x => x.ProductoId == item.ProductoId).Count() > 0;
                            if (Existe)
                            {
                                foreach (var Producto in Productos)
                                {
                                    if (Producto.ProductoId == item.ProductoId)
                                    {
                                        Producto.Cantidad += item.Cantidad;
                                        if (!string.IsNullOrWhiteSpace(item.ID))
                                        {
                                            Producto.ID += string.Format("{0},", item.ID);                                            
                                        }
                                    }                                    
                                }
                            }
                            else
                            {
                                Productos.Add(new EgresoDetalle() { ProductoId = item.ProductoId, UnidadId = item.UnidadId, Cantidad = item.Cantidad, ID = string.IsNullOrWhiteSpace(item.ID) ? "" : string.Format("{0},", item.ID) });
                            }
                        }
                    }
                    else
                    {
                        return "No contiene productos";
                    }

                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngEgresoId = new Herramienta().Formato_Correlativo(Id);

                        if (lngEgresoId > 0)
                        {
                            entidad.EgresoId = lngEgresoId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            if (entidad.Detalles != null && entidad.Detalles.Count() > 0)
                            {   
                                int i = 1;
                                foreach (var Detalle in entidad.Detalles)
                                {
                                    Detalle.DetalleId = i;
                                    Detalle.EgresoId = entidad.EgresoId;
                                    i++;                                   
                                }

                                foreach (var Detalle in Productos)
                                {
                                    //Se obtiene el producto para convercion
                                    Producto ProductoPadreActual = new Producto();
                                    Producto ProductoHijoActual = new Producto();
                                    bool UnidadPadre = false;
                                    decimal Cantidad = Detalle.Cantidad;

                                    decimal KardexPrecio = Detalle.PrecioCosto;
                                    decimal KardexExistenciaActual = 0;
                                    decimal KardexExistenciaFinal = 0;

                                    ProductoPadreActual = db.Set<Producto>().Where(x => x.ProductoId == Detalle.ProductoId).FirstOrDefault();

                                    if (ProductoPadreActual != null)
                                    {
                                        if (ProductoPadreActual.UnidadId == Detalle.UnidadId)
                                        {
                                            UnidadPadre = true;
                                        }
                                    }

                                    if (!UnidadPadre)
                                    {
                                        ProductoHijoActual = db.Set<Producto>().Where(x => x.ProductoPadreId == Detalle.ProductoId && x.UnidadId == Detalle.UnidadId).FirstOrDefault();

                                        if (ProductoHijoActual != null)
                                        {
                                            Cantidad *= ProductoHijoActual.Cantidad;
                                        }
                                    }

                                    ProductoInventario InventarioOrigenActual = db.Set<ProductoInventario>().Where(x => x.ProductoId == Detalle.ProductoId && x.AgenciaId == entidad.AgenciaId).FirstOrDefault();
                                    if (InventarioOrigenActual != null)
                                    {
                                        KardexExistenciaActual = InventarioOrigenActual.Cantidad;
                                        KardexExistenciaFinal = InventarioOrigenActual.Cantidad - Cantidad;

                                        InventarioOrigenActual.Cantidad -= Cantidad;
                                    }                                    

                                    if (!string.IsNullOrWhiteSpace(Detalle.ID))
                                    {
                                        string[] IDs = Detalle.ID.Split(',');
                                        for (int x = 0; x < IDs.Length; x++)
                                        {
                                            if (!string.IsNullOrWhiteSpace(IDs[x]))
                                            {
                                                //Se elimina de la agencia origen
                                                string IDsActual = IDs[x];
                                                ProductoInventarioID ProductoActualID = db.Set<ProductoInventarioID>().Where(y => y.ProductoId == Detalle.ProductoId && y.AgenciaId == entidad.AgenciaId && y.ID.Equals(IDsActual)).FirstOrDefault();
                                                if (ProductoActualID != null)
                                                {
                                                    db.Set<ProductoInventarioID>().Remove(ProductoActualID);
                                                }                                               
                                            }                                                                                        
                                        }
                                    }

                                    //Se agrega la informacion al Kardex
                                    db.Set<KardexMovimiento>().Add(new KardexMovimiento() { AgenciaId = entidad.AgenciaId, TipoId = 5, Fecha = DateTime.Today, FechaHora = DateTime.Now, ProductoId = Detalle.ProductoId, UnidadId = Detalle.UnidadId, DocumentoId = entidad.EgresoId, Cantidad = Detalle.Cantidad, Precio = KardexPrecio, ExistenciaActual = KardexExistenciaActual, ExistenciaFinal = KardexExistenciaFinal, ResponsableId = entidad.UsrInicial });
                                }
                            }

                            db.Set<Egreso>().Add(entidad);
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

            public string Guardar(Egreso entidad)
            {
                string Mensaje = "OK";
             
                if (entidad.EgresoId > 0)
                {
                   
                }
                else
                {
                    Mensaje = Agregar(entidad);
                }
          
                return Mensaje;
            }

            public Egreso ObtenerPorId(long id, bool todo = false)
            {
                Egreso EgresoActual = new Egreso();

                try
                {
                    if (todo)
                    {
                        EgresoActual = db.Set<Egreso>().Include("Agencia").Include("UsuarioInicial").Include("Detalles").Include("Detalles.Producto").Include("Detalles.Unidad").Where(x => x.EgresoId == id).FirstOrDefault();
                    }
                    else
                    {
                        EgresoActual = db.Set<Egreso>().Where(x => x.EgresoId == id).FirstOrDefault();
                    }
                }
                catch (Exception)
                {
                }

                return EgresoActual;
            }

            public List<Egreso> ObtenerListado(DateTime fechaInicial, DateTime fechaFinal)
            {
                List<Egreso> Egresos = new List<Egreso>();

                try
                {
                    Egresos = db.Set<Egreso>().Include("Agencia").Include("UsuarioInicial").Include("Detalles").AsNoTracking().Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.EgresoId).ToList();
                }
                catch (Exception)
                {
                }

                return Egresos;
            }

        #endregion
    }
}
