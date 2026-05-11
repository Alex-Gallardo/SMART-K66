using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class OrdenCompraBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public OrdenCompraBL()
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
                    OrdenCompra OrdenCompraActual = db.Set<OrdenCompra>().AsNoTracking().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (OrdenCompraActual != null)
                    {
                        Inicial_Id = OrdenCompraActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;
                }
                catch (Exception)
                {}

                return Id;
            }           
        
            private string Agregar(OrdenCompra entidad)
            {
                string Mensaje = "OK";

                string PathFotografia = ConfigurationManager.AppSettings["Path_Fotografia_Orden"].ToString();
                string UrlFotografia = ConfigurationManager.AppSettings["Url_Fotografia_Orden"].ToString();

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngOrdenId = new Herramienta().Formato_Correlativo(Id);

                        if (lngOrdenId > 0)
                        {
                            entidad.OrdenId = lngOrdenId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;                            

                            if (!string.IsNullOrWhiteSpace(entidad.FotografiaOrden))
                            {
                                entidad.FotografiaOrden = string.Format(@"{0}{1}/{2}", UrlFotografia, entidad.OrdenId, entidad.FotografiaOrden);
                            }

                            if (entidad.Detalles != null && entidad.Detalles.Count() > 0)
                            {
                                int i = 1;
                                foreach (var Detalle in entidad.Detalles)
                                {
                                    Detalle.DetalleId = i;
                                    Detalle.OrdenId = entidad.OrdenId;
                                    i++;
                                }
                            }

                            db.Set<OrdenCompra>().Add(entidad);
                            db.SaveChanges();                            

                            if (Mensaje.Equals("OK"))
                            {
                                //Se crea carpeta de la fotografia
                                string Path_Fotografia_Orden = string.Format(@"{0}\{1}", PathFotografia, entidad.OrdenId);

                                if (!(Directory.Exists(Path_Fotografia_Orden)))
                                {
                                    Directory.CreateDirectory(Path_Fotografia_Orden);
                                }

                                if (entidad.Fotografia != null)
                                {
                                    ConvetirbyteAImage(entidad.Fotografia.Content).Save(string.Format(@"{0}\{1}", Path_Fotografia_Orden, "orden_compra.png"));
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            public string Actualizar(OrdenCompra entidad) 
            {
                string Mensaje = "OK";

                string PathFotografia = ConfigurationManager.AppSettings["Path_Fotografia_Orden"].ToString();
                string UrlFotografia = ConfigurationManager.AppSettings["Url_Fotografia_Orden"].ToString();

                try
                {
                    OrdenCompra OrdenCompraActual = db.Set<OrdenCompra>().Where(x => x.OrdenId == entidad.OrdenId).FirstOrDefault();
                    if (OrdenCompraActual != null)
                    {
                        OrdenCompraActual.ProveedorId = entidad.ProveedorId;
                        OrdenCompraActual.MonedaId = entidad.MonedaId;
                        OrdenCompraActual.Observaciones = entidad.Observaciones;
                        OrdenCompraActual.Comentario = entidad.Comentario;                                            

                        if (!string.IsNullOrWhiteSpace(entidad.FotografiaOrden))
                        {
                            OrdenCompraActual.FotografiaOrden = string.Format(@"{0}{1}/{2}", UrlFotografia, OrdenCompraActual.OrdenId, entidad.FotografiaOrden);
                        }

                        var Detalles = db.Set<OrdenCompraDetalle>().Where(x => x.OrdenId == entidad.OrdenId).ToList();
                        db.Set<OrdenCompraDetalle>().RemoveRange(Detalles);

                        if (entidad.Detalles != null && entidad.Detalles.Count() > 0)
                        {
                            int i = 1;
                            foreach (var Detalle in entidad.Detalles)
                            {
                                Detalle.DetalleId = i;
                                Detalle.OrdenId = entidad.OrdenId;
                                db.Set<OrdenCompraDetalle>().Add(Detalle);
                                i++;
                            }
                        }

                        db.SaveChanges();                        

                        if (Mensaje.Equals("OK"))
                        {
                            //Se crea carpeta de la fotografia
                            string Path_Fotografia_Orden = string.Format(@"{0}\{1}", PathFotografia, entidad.OrdenId);

                            if (!(Directory.Exists(Path_Fotografia_Orden)))
                            {
                                Directory.CreateDirectory(Path_Fotografia_Orden);
                            }

                            if (entidad.Fotografia != null)
                            {
                                ConvetirbyteAImage(entidad.Fotografia.Content).Save(string.Format(@"{0}\{1}", Path_Fotografia_Orden, "orden_compra.png"));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            private Image ConvetirbyteAImage(byte[] byteArrayIn)
            {
                return Image.FromStream(new MemoryStream(byteArrayIn));
            }

        #endregion

        #region Metodos Publicos

            public string Guardar(OrdenCompra entidad)
            {
                string Mensaje = "OK";
               
                if (entidad.OrdenId > 0)
                {
                    Mensaje = Actualizar(entidad);
                }
                else
                {
                    Mensaje = Agregar(entidad);
                }            

                return Mensaje;
            }           

            public OrdenCompra ObtenerPorId(long id, bool todo = false)
            {
                OrdenCompra OrdenCompraActual = new OrdenCompra();

                try
                {
                    if (todo)
                    {
                        OrdenCompraActual = db.Set<OrdenCompra>().Include("Agencia").Include("Proveedor").Include("Moneda").Include("Detalles").Include("Detalles.Producto").Include("Detalles.Unidad").Include("UsuarioCreo").AsNoTracking().Where(x => x.OrdenId == id).FirstOrDefault();
                    }
                    else
                    {
                        OrdenCompraActual = db.Set<OrdenCompra>().Where(x => x.OrdenId == id).FirstOrDefault();
                    }                  
                }
                catch (Exception)
                {}

                return OrdenCompraActual;
            }

            public List<OrdenCompra> ObtenerListadoPorFecha(DateTime fechaInicial, DateTime fechaFinal, long usuarioId)
            {
                List<OrdenCompra> Ordenes = new List<OrdenCompra>();

                try
                {
                    var AgenciasIds = db.Set<UsuarioAgencia>().Where(x => x.UsuarioId == usuarioId).AsEnumerable().Select(x => x.AgenciaId).ToList();
                    if (AgenciasIds != null && AgenciasIds.Count() > 0)
                    {
                        Ordenes = db.Set<OrdenCompra>().Include("Agencia").Include("Proveedor").Include("Moneda").Include("UsuarioCreo").Include("Detalles").AsNoTracking().Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && AgenciasIds.Contains(x.AgenciaId)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.OrdenId).ToList();
                    }
                }
                catch (Exception)
                {}

                return Ordenes;
            }

            public List<OrdenCompra> Buscar(string search, long usuarioId)
            {
                List<OrdenCompra> Ordenes = new List<OrdenCompra>();
                long OrdenId = 0;

                try
                {
                    long.TryParse(search, out OrdenId);

                    var AgenciasIds = db.Set<UsuarioAgencia>().Where(x => x.UsuarioId == usuarioId).AsEnumerable().Select(x => x.AgenciaId).ToList();
                    if (AgenciasIds != null && AgenciasIds.Count() > 0)
                    {
                        if (OrdenId > 0)
                        {
                            Ordenes = db.Set<OrdenCompra>().Include("Agencia").Include("Proveedor").Include("Moneda").Include("UsuarioCreo").Include("Detalles").AsNoTracking().Where(x => x.OrdenId == OrdenId && AgenciasIds.Contains(x.AgenciaId)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.OrdenId).ToList();
                        }
                        else
                        {
                            Ordenes = db.Set<OrdenCompra>().Include("Agencia").Include("Proveedor").Include("Moneda").Include("UsuarioCreo").Include("Detalles").AsNoTracking().Where(x => (x.Agencia.Nombre.ToLower().Contains(search.ToLower()) || x.Proveedor.Nombre.ToLower().Contains(search.ToLower())) && AgenciasIds.Contains(x.AgenciaId)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.OrdenId).ToList();
                        }
                    }
                }
                catch (Exception)
                { }

                return Ordenes;
            }
        
        #endregion
    }
}
