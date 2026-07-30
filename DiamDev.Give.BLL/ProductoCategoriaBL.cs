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
    public class ProductoCategoriaBL
    {

        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public ProductoCategoriaBL()
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

                    ProductoCategoria ProductoCategoriaActual = db.Set<ProductoCategoria>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (ProductoCategoriaActual != null)
                    {
                        Inicial_Id = ProductoCategoriaActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;

                }
                catch (Exception)
                {
                }

                return Id;
            }

            private string Agregar(ProductoCategoria entidad)
            {
                string Mensaje = "OK";

                string PathFotografia = ConfigurationManager.AppSettings["Path_Categoria_FotografiaApp"].ToString();

                string UrlFotografia = ConfigurationManager.AppSettings["Url_Categoria_FotografiaApp"].ToString();

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngProductoCategoriaId = new Herramienta().Formato_Correlativo(Id);

                        if (lngProductoCategoriaId > 0)
                        {
                            entidad.ProductoCategoriaId = lngProductoCategoriaId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;
                            entidad.FotografiaApp = string.Format(@"{0}{1}/{2}.png", UrlFotografia, entidad.ProductoCategoriaId, entidad.ProductoCategoriaId);

                            db.Set<ProductoCategoria>().Add(entidad);
                            db.SaveChanges();

                            if (Mensaje.Equals("OK"))
                            {
                                //Se crea carpeta por categoria
                                string Path_Categoria = string.Format(@"{0}\{1}", PathFotografia, entidad.ProductoCategoriaId);

                                if (!(Directory.Exists(Path_Categoria)))
                                {
                                    Directory.CreateDirectory(Path_Categoria);
                                }

                                if (entidad.Fotografia != null)
                                {
                                    ConvetirbyteAImage(entidad.Fotografia.Content).Save(string.Format(@"{0}\{1}.png", Path_Categoria, entidad.ProductoCategoriaId));
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

            private string Actualizar(ProductoCategoria entidad)
            {
                string Mensaje = "OK";

                string PathFotografia = ConfigurationManager.AppSettings["Path_Categoria_FotografiaApp"].ToString();

                string UrlFotografia = ConfigurationManager.AppSettings["Url_Categoria_FotografiaApp"].ToString();

                try
                {

                    ProductoCategoria ProductoCategoriaActual = ObtenerPorId(entidad.ProductoCategoriaId);

                    if (ProductoCategoriaActual.ProductoCategoriaId > 0)
                    {
                        ProductoCategoriaActual.Nombre = entidad.Nombre;
                        ProductoCategoriaActual.Activo = entidad.Activo;

                        if (!string.IsNullOrWhiteSpace(entidad.FotografiaApp))
                        {
                            ProductoCategoriaActual.FotografiaApp = string.Format(@"{0}{1}/{2}.png", UrlFotografia, ProductoCategoriaActual.ProductoCategoriaId, ProductoCategoriaActual.ProductoCategoriaId);
                        }

                        if (Mensaje.Equals("OK"))
                        {
                            //Se crea carpeta por categoria
                            string Path_Categoria = string.Format(@"{0}\{1}", PathFotografia, entidad.ProductoCategoriaId);

                            if (!(Directory.Exists(Path_Categoria)))
                            {
                                Directory.CreateDirectory(Path_Categoria);
                            }

                            if (entidad.Fotografia != null)
                            {
                                ConvetirbyteAImage(entidad.Fotografia.Content).Save(string.Format(@"{0}\{1}.png", Path_Categoria, entidad.ProductoCategoriaId));
                            }
                        }

                        db.SaveChanges();                        
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

            public string Guardar(ProductoCategoria entidad)
            {
                string Mensaje = "OK";
              
                if (entidad.ProductoCategoriaId > 0)
                {
                    Mensaje = Actualizar(entidad);
                }
                else
                {
                    Mensaje = Agregar(entidad);
                }
              
                return Mensaje;
            }

            public ProductoCategoria ObtenerPorId(long id)
            {
                ProductoCategoria ProductoCategoriaActual = new ProductoCategoria();

                try
                {
                    ProductoCategoriaActual = db.Set<ProductoCategoria>().Where(x => x.ProductoCategoriaId == id).FirstOrDefault();
                }
                catch (Exception)
                {
                }

                return ProductoCategoriaActual;
            }

            public List<ProductoCategoria> ObtenerListado(bool todo = true)
            {
                List<ProductoCategoria> ProductoCategorias = new List<ProductoCategoria>();

                try
                {
                    if (todo)
                    {
                        ProductoCategorias = db.Set<ProductoCategoria>().OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ProductoCategoriaId).ToList();
                    }
                    else
                    {
                        ProductoCategorias = db.Set<ProductoCategoria>().Where(x => x.Activo).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ProductoCategoriaId).ToList();
                    }
                }
                catch (Exception)
                {
                }

                return ProductoCategorias;
            }
        public List<ProductoCategoria> ObtenerListadoBasadoExistencias(int localidadid)
        {
            List<ProductoCategoria> ProductoCategorias = new List<ProductoCategoria>();
            List<ProductoCategoria> ProductoCategoriasVienen = new List<ProductoCategoria>();
            long agenciapedidos = 0;
            if (localidadid == -1)
            {
                Configuracion con = new ConfiguracionBL().ObtenerPorIdentificador("AgenciaCentral");
                agenciapedidos = Convert.ToInt64(con.Valor);
            }
            else {
                Configuracion con = new ConfiguracionBL().ObtenerPorIdentificador("AgenciaPedidos");
                if (con.Valor == "-1")
                {
                    long localidad = Convert.ToInt64(new ClienteBL().ObtenerDireccionPorId(localidadid).LocalidadId);
                    agenciapedidos = Convert.ToInt64(new LocalidadBL().ObtenerPorId(localidad).AgenciaId);
                }
                else {
                    agenciapedidos = Convert.ToInt64(con.Valor);
                }
                
            }
         
            try
            {

                ProductoCategoriasVienen = db.Set<ProductoCategoria>().Where(x => x.Activo == true).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ProductoCategoriaId).ToList();
                foreach (ProductoCategoria item in ProductoCategoriasVienen) {
                    List<Producto> listadoproductos = db.Productos.Where(x => x.CategoriaId == item.ProductoCategoriaId).ToList();
                    bool agregarcategoria = false;
                    foreach (Producto prod in listadoproductos) {
                        ProductoInventario ie = db.ProductoInventarios.Where(x => x.ProductoId == prod.ProductoId && x.AgenciaId == agenciapedidos).FirstOrDefault();
                        if (ie != null) {
                            if (ie.Cantidad > 0) {
                                agregarcategoria = true;
                                break;
                            }
                        }

                    }

                    if (agregarcategoria) {
                        ProductoCategorias.Add(item);
                    }

                }


            }
            catch (Exception)
            {
            }

            return ProductoCategorias;
        }
        public List<ProductoCategoria> Buscar(string search)
            {
                List<ProductoCategoria> ProductoCategorias = new List<ProductoCategoria>();

                try
                {
                    ProductoCategorias = db.Set<ProductoCategoria>().Where(x => x.Nombre.Contains(search)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ProductoCategoriaId).ToList();
                }
                catch (Exception)
                {
                }

                return ProductoCategorias;
            }

        #endregion

    }
}
