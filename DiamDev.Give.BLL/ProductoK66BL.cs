using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Configuration;

namespace DiamDev.Give.BLL
{
    public class ProductoK66BL
    {
        #region Variables Globales   

            private GiveContext db;
        string URL_SAP;

        #endregion

        #region Constructores 

        public ProductoK66BL()
        {
            this.db = new GiveContext();
            this.URL_SAP = ConfigurationManager.AppSettings["URL_SAP"].ToString();
        }

        #endregion

        #region Metodos Privados
        #endregion

        #region Metodos Publicos

        public List<ProductoK66> BuscarProductoxNombreK66(string buscar, string clienteId, long usuarioId, long empresaId)
            {
                List<ProductoK66> Productos = new List<ProductoK66>();

                try
                {
                    UsuarioEmpresa UsuarioEmpresaActual = db.Set<UsuarioEmpresa>().Include("Empresa").AsNoTracking().Where(x => x.UsuarioId == usuarioId && x.EmpresaId == empresaId).FirstOrDefault();
                    if (UsuarioEmpresaActual != null)
                    {
                        if (empresaId == 20210705001)
                        {
                        Productos = ObtenerItemLibre(buscar, "BOLIK", clienteId);
                        //using (var dbK66 = new VMBOLIKContext())
                        //{
                        //    Productos = dbK66.Database.SqlQuery<ProductoK66>("dbo.sp_consulta_producto_texto_libre @Buscar, @Cliente, @Codigo", new SqlParameter("@Buscar", buscar), new SqlParameter("@Cliente", clienteId), new SqlParameter("@Codigo", UsuarioEmpresaActual.Codigo)).ToList();
                        //}
                    }
                        else if (empresaId == 20210705002)
                        {
                            Productos = ObtenerItemLibre(buscar, "EMPAQUES", clienteId);
                            //using (var dbK66 = new VMEMPAQUESContext())
                            //{
                            //    Productos = dbK66.Database.SqlQuery<ProductoK66>("dbo.sp_consulta_producto_texto_libre @Buscar, @Cliente, @Codigo", new SqlParameter("@Buscar", buscar), new SqlParameter("@Cliente", clienteId), new SqlParameter("@Codigo", UsuarioEmpresaActual.Codigo)).ToList();
                            //}
                        }
                        else if (empresaId == 20210705003)
                        {
                            Productos = ObtenerItemLibre(buscar, "ESCOCESA", clienteId);
                            //using (var dbK66 = new VMFAESContext())
                            //{
                            //    Productos = dbK66.Database.SqlQuery<ProductoK66>("dbo.sp_consulta_producto_texto_libre @Buscar, @Cliente, @Codigo", new SqlParameter("@Buscar", buscar), new SqlParameter("@Cliente", clienteId), new SqlParameter("@Codigo", UsuarioEmpresaActual.Codigo)).ToList();
                            //}
                        }
                        else if (empresaId == 20210705004)
                        {
                            Productos = ObtenerItemLibre(buscar, "GRACO", clienteId);
                            //using (var dbK66 = new VMGRACOContext())
                            //{
                            //    Productos = dbK66.Database.SqlQuery<ProductoK66>("dbo.sp_consulta_producto_texto_libre @Buscar, @Cliente, @Codigo", new SqlParameter("@Buscar", buscar), new SqlParameter("@Cliente", clienteId), new SqlParameter("@Codigo", UsuarioEmpresaActual.Codigo)).ToList();
                            //}
                        }
                    }                    
                }
                catch (Exception)
                {}

                return Productos;
            }

        public List<ProductoK66> ObtenerItemLibre(string buscar, string NombreEmpresa, string Cliente)
        {
            // Base URL de la API
            string apiUrl = $"{URL_SAP}ItemLibre";

            // Crear la consulta con parámetros
            string requestUrl = $"{apiUrl}?ItemCode={buscar}&Empresa={NombreEmpresa}&CardName={Cliente}";

            // Inicializar HttpClient (asegúrate de gestionar adecuadamente la vida útil de HttpClient)
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // Hacer la solicitud GET de manera síncrona
                    HttpResponseMessage response = client.GetAsync(requestUrl).GetAwaiter().GetResult();

                    // Verificar si la respuesta fue exitosa
                    if (response.IsSuccessStatusCode)
                    {
                        // Leer el contenido de la respuesta de manera síncrona
                        string jsonResponse = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                        // Deserializar el JSON a la lista de modelos
                        List<ProductoK66> productos = new List<ProductoK66>();
                        try
                        {
                            productos = JsonConvert.DeserializeObject<List<ProductoK66>>(jsonResponse);
                        }
                        catch(Exception ex)
                        {
                            var producto = JsonConvert.DeserializeObject<ProductoK66>(jsonResponse);
                            productos.Add(producto);
                        }
                        // Retornar los clientes deserializados
                        return productos;
                    }
                    else
                    {
                        // Manejo de error si la respuesta no fue exitosa
                        throw new HttpRequestException($"Error al llamar a la API: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    // Manejar la excepción
                    Console.WriteLine($"Excepción: {ex.Message}");
                    return new List<ProductoK66>();
                }
            }
        }

        public List<ProductoK66> ObtenerItemExistencia(string buscar, string NombreEmpresa, string Cliente, string roles)
        {
            // Base URL de la API
            string apiUrl = $"{URL_SAP}ItemExistencia";

            // Crear la consulta con parámetros
            string requestUrl = $"{apiUrl}?ItemCode={buscar}&Empresa={NombreEmpresa}&CardCode={Cliente}&Rol={roles}";

            // Inicializar HttpClient (asegúrate de gestionar adecuadamente la vida útil de HttpClient)
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // Hacer la solicitud GET de manera síncrona
                    HttpResponseMessage response = client.GetAsync(requestUrl).GetAwaiter().GetResult();

                    // Verificar si la respuesta fue exitosa
                    if (response.IsSuccessStatusCode)
                    {
                        // Leer el contenido de la respuesta de manera síncrona
                        string jsonResponse = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                        // Deserializar el JSON a la lista de modelos
                        List<ProductoK66> productos = new List<ProductoK66>();
                        try
                        {
                            productos = JsonConvert.DeserializeObject<List<ProductoK66>>(jsonResponse);
                        }
                        catch (Exception ex)
                        {
                            var producto = JsonConvert.DeserializeObject<ProductoK66>(jsonResponse);
                            productos.Add(producto);
                        }
                        // Retornar los clientes deserializados
                        return productos;
                    }
                    else
                    {
                        // Manejo de error si la respuesta no fue exitosa
                        throw new HttpRequestException($"Error al llamar a la API: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    // Manejar la excepción
                    Console.WriteLine($"Excepción: {ex.Message}");
                    return new List<ProductoK66>();
                }
            }
        }

        public ProductoK66 ObtenerItemId(string buscar, string NombreEmpresa,string CardCode, string Unidad, int cantidad)
        {
            // Base URL de la API
            string apiUrl = $"{URL_SAP}ItemPrice";

            // Crear la consulta con parámetros
            string requestUrl = $"{apiUrl}?ItemCode={buscar}&Empresa={NombreEmpresa}&CardCode={CardCode}&Unidad={Unidad}&Cantidad={cantidad}";

            // Inicializar HttpClient (asegúrate de gestionar adecuadamente la vida útil de HttpClient)
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // Hacer la solicitud GET de manera síncrona
                    HttpResponseMessage response = client.GetAsync(requestUrl).GetAwaiter().GetResult();

                    // Verificar si la respuesta fue exitosa
                    if (response.IsSuccessStatusCode)
                    {
                        // Leer el contenido de la respuesta de manera síncrona
                        string jsonResponse = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                        // Deserializar el JSON a la lista de modelos
                        List<ProductoK66> producto = JsonConvert.DeserializeObject<List<ProductoK66>>(jsonResponse);
                        return producto.FirstOrDefault();
                    }
                    else
                    {
                        // Manejo de error si la respuesta no fue exitosa
                        throw new HttpRequestException($"Error al llamar a la API: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    // Manejar la excepción
                    Console.WriteLine($"Excepción: {ex.Message}");
                    return new ProductoK66();
                }
            }
        }

        public ProductoK66 ObtenerItemName(string buscar, string NombreEmpresa, string Unidad)
        {
            // Base URL de la API
            string apiUrl = $"{URL_SAP}ItemName";

            // Crear la consulta con parámetros
            string requestUrl = $"{apiUrl}?ItemCode={buscar}&Empresa={NombreEmpresa}&Unidad={Unidad}";

            // Inicializar HttpClient (asegúrate de gestionar adecuadamente la vida útil de HttpClient)
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // Hacer la solicitud GET de manera síncrona
                    HttpResponseMessage response = client.GetAsync(requestUrl).GetAwaiter().GetResult();

                    // Verificar si la respuesta fue exitosa
                    if (response.IsSuccessStatusCode)
                    {
                        // Leer el contenido de la respuesta de manera síncrona
                        string jsonResponse = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                        // Deserializar el JSON a la lista de modelos
                        List<ProductoK66> producto = JsonConvert.DeserializeObject<List<ProductoK66>>(jsonResponse);

                        // Retornar los clientes deserializados
                        return producto.FirstOrDefault();
                    }
                    else
                    {
                        // Manejo de error si la respuesta no fue exitosa
                        throw new HttpRequestException($"Error al llamar a la API: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    // Manejar la excepción
                    Console.WriteLine($"Excepción: {ex.Message}");
                    return new ProductoK66();
                }
            }
        }

        public List<ProductoK66> BuscarProductoxTextoLibreK66(string buscar, string clienteId, long usuarioId, long empresaId)
            {
                List<ProductoK66> Productos = new List<ProductoK66>();

                try
                {
                    if (string.IsNullOrWhiteSpace(buscar))
                    {
                        return new List<ProductoK66>();
                    }

                    string cadena = buscar;
                    string pattern = @"\((.*?)\)";

                    Match match = Regex.Match(cadena, pattern);

                    int Inicio = buscar.LastIndexOf(")") + 4;
                    string result = match.Groups[1].Value;
                    buscar = buscar.Substring(Inicio);   

                    string Descripcion = buscar.Substring(0, buscar.LastIndexOf('/'));
                    string Unidad = buscar.Substring(buscar.LastIndexOf('/') + 1);

                    if (!string.IsNullOrWhiteSpace(Descripcion))
                    {
                        Descripcion = Descripcion.Trim();
                    }

                    if (!string.IsNullOrWhiteSpace(Unidad))
                    {
                        Unidad = Unidad.Trim();
                    }
                    Descripcion = result;
                    UsuarioEmpresa UsuarioEmpresaActual = db.Set<UsuarioEmpresa>().Include("Empresa").AsNoTracking().Where(x => x.UsuarioId == usuarioId && x.EmpresaId == empresaId).FirstOrDefault();
                    if (UsuarioEmpresaActual != null)
                    {
                        if (empresaId == 20210705001)
                        {
                            using (var dbK66 = new VMBOLIKContext())
                            {
                                Productos.Add(ObtenerItemName(Descripcion, "BOLIK", Unidad));
                                //Productos = dbK66.Database.SqlQuery<ProductoK66>("dbo.sp_consulta_producto_x_nombre @Buscar, @Unidad, @Cliente, @Codigo", new SqlParameter("@Buscar", Descripcion), new SqlParameter("@Unidad", Unidad), new SqlParameter("@Cliente", clienteId), new SqlParameter("@Codigo", UsuarioEmpresaActual.Codigo)).ToList();
                            }
                        }
                        else if (empresaId == 20210705002)
                        {
                            using (var dbK66 = new VMEMPAQUESContext())
                            {
                                Productos.Add(ObtenerItemName(Descripcion, "EMPAQUES", Unidad));
                                //Productos = dbK66.Database.SqlQuery<ProductoK66>("dbo.sp_consulta_producto_x_nombre @Buscar, @Unidad, @Cliente, @Codigo", new SqlParameter("@Buscar", Descripcion), new SqlParameter("@Unidad", Unidad), new SqlParameter("@Cliente", clienteId), new SqlParameter("@Codigo", UsuarioEmpresaActual.Codigo)).ToList();
                            }
                        }
                        else if (empresaId == 20210705003)
                        {
                            using (var dbK66 = new VMFAESContext())
                            {
                                Productos.Add(ObtenerItemName(Descripcion, "ESCOCESA", Unidad));
                                //Productos = dbK66.Database.SqlQuery<ProductoK66>("dbo.sp_consulta_producto_x_nombre @Buscar, @Unidad, @Cliente, @Codigo", new SqlParameter("@Buscar", Descripcion), new SqlParameter("@Unidad", Unidad), new SqlParameter("@Cliente", clienteId), new SqlParameter("@Codigo", UsuarioEmpresaActual.Codigo)).ToList();
                            }
                        }
                        else if (empresaId == 20210705004)
                        {
                            using (var dbK66 = new VMGRACOContext())
                            {
                                Productos.Add(ObtenerItemName(Descripcion, "GRACO", Unidad));
                                //Productos = dbK66.Database.SqlQuery<ProductoK66>("dbo.sp_consulta_producto_x_nombre @Buscar, @Unidad, @Cliente, @Codigo", new SqlParameter("@Buscar", Descripcion), new SqlParameter("@Unidad", Unidad), new SqlParameter("@Cliente", clienteId), new SqlParameter("@Codigo", UsuarioEmpresaActual.Codigo)).ToList();
                            }
                        }
                    }
                }
                catch (Exception)
                { }

                return Productos;
            }

        public ProductoK66 ObtenerxIDK66(string id, string unidad, string clienteId, string direccionId, long usuarioId, long empresaId, int cantidad)
        {
            ProductoK66 ProductoActual = new ProductoK66();

            try
            {
                UsuarioEmpresa UsuarioEmpresaActual = db.Set<UsuarioEmpresa>().Include("Empresa").AsNoTracking().Where(x => x.UsuarioId == usuarioId && x.EmpresaId == empresaId).FirstOrDefault();
                if (UsuarioEmpresaActual != null)
                {
                    if (empresaId == 20210705001)
                    {
                        using (var dbK66 = new VMBOLIKContext())
                        {
                            //if (direccionId == "0")
                            //{
                            //    ProductoActual = dbK66.Database.SqlQuery<ProductoK66>("dbo.sp_obtener_producto_x_id @ID, @Unidad, @ClienteID, @DireccionID", new SqlParameter("@ID", id), new SqlParameter("@Unidad", unidad), new SqlParameter("@ClienteID", clienteId), new SqlParameter("@DireccionID", DBNull.Value)).FirstOrDefault();
                            //}
                            //else
                            //{
                            //    ProductoActual = dbK66.Database.SqlQuery<ProductoK66>("dbo.sp_obtener_producto_x_id @ID, @Unidad, @ClienteID, @DireccionID", new SqlParameter("@ID", id), new SqlParameter("@Unidad", unidad), new SqlParameter("@ClienteID", clienteId), new SqlParameter("@DireccionID", direccionId)).FirstOrDefault();
                            //}
                            ProductoActual = ObtenerItemId(id, "BOLIK", clienteId, unidad, cantidad);
                        }
                    }
                    else if (empresaId == 20210705002)
                    {
                        //using (var dbK66 = new VMEMPAQUESContext())
                        //{
                        //if (direccionId == "0")
                        //{
                        //    ProductoActual = dbK66.Database.SqlQuery<ProductoK66>("dbo.sp_obtener_producto_x_id @ID, @Unidad, @ClienteID, @DireccionID", new SqlParameter("@ID", id), new SqlParameter("@Unidad", unidad), new SqlParameter("@ClienteID", clienteId), new SqlParameter("@DireccionID", DBNull.Value)).FirstOrDefault();
                        //}
                        //else
                        //{
                        //    ProductoActual = dbK66.Database.SqlQuery<ProductoK66>("dbo.sp_obtener_producto_x_id @ID, @Unidad, @ClienteID, @DireccionID", new SqlParameter("@ID", id), new SqlParameter("@Unidad", unidad), new SqlParameter("@ClienteID", clienteId), new SqlParameter("@DireccionID", direccionId)).FirstOrDefault();
                        //}
                        ProductoActual = ObtenerItemId(id, "BOLIK", clienteId, unidad, cantidad);
                    //}
                    }
                    else if (empresaId == 20210705003)
                    {
                        ProductoActual = ObtenerItemId(id, "ESCOCESA", clienteId, unidad, cantidad);
                        //using (var dbK66 = new VMFAESContext())
                        //{
                        //    if (direccionId == "0")
                        //    {
                        //        ProductoActual = dbK66.Database.SqlQuery<ProductoK66>("dbo.sp_obtener_producto_x_id @ID, @Unidad, @ClienteID, @DireccionID", new SqlParameter("@ID", id), new SqlParameter("@Unidad", unidad), new SqlParameter("@ClienteID", clienteId), new SqlParameter("@DireccionID", DBNull.Value)).FirstOrDefault();
                        //    }
                        //    else
                        //    {
                        //        ProductoActual = dbK66.Database.SqlQuery<ProductoK66>("dbo.sp_obtener_producto_x_id @ID, @Unidad, @ClienteID, @DireccionID", new SqlParameter("@ID", id), new SqlParameter("@Unidad", unidad), new SqlParameter("@ClienteID", clienteId), new SqlParameter("@DireccionID", direccionId)).FirstOrDefault();
                        //    }
                        //}
                    }
                    else if (empresaId == 20210705004)
                    {
                        ProductoActual = ObtenerItemId(id, "GRACO", clienteId, unidad, cantidad);
                        //using (var dbK66 = new VMGRACOContext())
                        //{
                        //    if (direccionId == "0")
                        //    {
                        //        ProductoActual = dbK66.Database.SqlQuery<ProductoK66>("dbo.sp_obtener_producto_x_id @ID, @Unidad, @ClienteID, @DireccionID", new SqlParameter("@ID", id), new SqlParameter("@Unidad", unidad), new SqlParameter("@ClienteID", clienteId), new SqlParameter("@DireccionID", DBNull.Value)).FirstOrDefault();
                        //    }
                        //    else
                        //    {
                        //        ProductoActual = dbK66.Database.SqlQuery<ProductoK66>("dbo.sp_obtener_producto_x_id @ID, @Unidad, @ClienteID, @DireccionID", new SqlParameter("@ID", id), new SqlParameter("@Unidad", unidad), new SqlParameter("@ClienteID", clienteId), new SqlParameter("@DireccionID", direccionId)).FirstOrDefault();
                        //    }
                        //}
                    }

                    if (ProductoActual != null)
                    {
                        //SE OBTIENE LOS DESCUENTOS
                        //nueva integracion con sap, ya vendra integrado el valor de descuento desde el api
                        //DescuentoK66 DescuentoActual = db.Set<DescuentoK66>().AsNoTracking().Where(x => x.EmpresaId == empresaId && x.IDK66 == clienteId && x.ProductoId == id).FirstOrDefault();
                        //if (DescuentoActual != null)
                        //{
                        //    ProductoActual.PrecioOriginal = ProductoActual.Precio;
                        //    ProductoActual.Precio = decimal.Round(ProductoActual.Precio * (1 - DescuentoActual.Descuento), 4);
                        //}
                        ProductoActual.PrecioOriginal = ProductoActual.Precio;
                        ProductoActual.Precio = ProductoActual.Precio;//decimal.Round(ProductoActual.Precio * (1 - (decimal)0.15), 4);

                        ProductoActual.InventarioDisponible = ProductoActual.InventarioTotal - ProductoActual.InventarioComprometido;
                        if (ProductoActual.InventarioDisponible < 0)
                        {
                            ProductoActual.InventarioDisponible = 0;
                        }
                    }
                }
            }
            catch (Exception)
            { }

            return ProductoActual;
        }

        public List<ProductoK66> ObtenerExistenciaxIDK66(string id, string clienteId, long usuarioId, long empresaId)
        {
            List<ProductoK66> Productos = new List<ProductoK66>();

            try
            {
                    
                UsuarioEmpresa UsuarioEmpresaActual = db.Set<UsuarioEmpresa>().Include("Empresa").AsNoTracking().Where(x => x.UsuarioId == usuarioId && x.EmpresaId == empresaId).FirstOrDefault();
                if (UsuarioEmpresaActual != null)
                {
                    var Roles = db.Set<UsuarioRol>().Where(x => x.UsuarioId == usuarioId).ToList();
                    string concatenatedRoles = string.Join(",", Roles.Select(r => r.RolId));
                    if (empresaId == 20210705001)
                    {
                        //using (var dbK66 = new VMBOLIKContext())
                        //{
                        //    Productos = dbK66.Database.SqlQuery<ProductoK66>("dbo.sp_obtener_existencia_producto_x_id @ID, @Cliente", new SqlParameter("@ID", id), new SqlParameter("@Cliente", clienteId)).ToList();
                        //}
                        Productos = ObtenerItemExistencia(id, "BOLIK", clienteId, concatenatedRoles);
                    }
                    else if (empresaId == 20210705002)
                    {
                        //using (var dbK66 = new VMEMPAQUESContext())
                        //{
                        //    Productos = dbK66.Database.SqlQuery<ProductoK66>("dbo.sp_obtener_existencia_producto_x_id @ID, @Cliente", new SqlParameter("@ID", id), new SqlParameter("@Cliente", clienteId)).ToList();
                        //}
                        Productos = ObtenerItemExistencia(id, "EMPAQUES", clienteId, concatenatedRoles);
                    }
                    else if (empresaId == 20210705003)
                    {
                        //using (var dbK66 = new VMFAESContext())
                        //{
                        //    Productos = dbK66.Database.SqlQuery<ProductoK66>("dbo.sp_obtener_existencia_producto_x_id @ID, @Cliente", new SqlParameter("@ID", id), new SqlParameter("@Cliente", clienteId)).ToList();
                        //}
                        Productos = ObtenerItemExistencia(id, "ESCOCESA", clienteId, concatenatedRoles);
                    }
                    else if (empresaId == 20210705004)
                    {
                        //using (var dbK66 = new VMGRACOContext())
                        //{
                        //    Productos = dbK66.Database.SqlQuery<ProductoK66>("dbo.sp_obtener_existencia_producto_x_id @ID, @Cliente", new SqlParameter("@ID", id), new SqlParameter("@Cliente", clienteId)).ToList();
                        //}
                        Productos = ObtenerItemExistencia(id, "GRACO", clienteId, concatenatedRoles);
                    }

                    if (Productos != null && Productos.Count() > 0)
                    {
                        Productos.ForEach(p => 
                        {
                            p.InventarioDisponible = p.Existencia - p.InventarioComprometido;
                            if (p.InventarioDisponible < 0)
                            {
                                p.InventarioDisponible = 0;
                            }
                        });                            
                    }
                }
            }
            catch (Exception)
            { }

            return Productos;
        }

        public List<BodegaActivaModel> ObtenerBodegaActivaWarehouse(long empresaId)
        {
            List<BodegaActivaModel> Bodegas = new List<BodegaActivaModel>();

            try
            {
                if (empresaId == 20210705001)
                {
                    using (var dbK66 = new VMBOLIKContext())
                    {
                        Bodegas = dbK66.Database.SqlQuery<BodegaActivaModel>("dbo.sp_obtener_location_warehouse_id").ToList();
                    }
                }
                else if (empresaId == 20210705002)
                {
                    using (var dbK66 = new VMEMPAQUESContext())
                    {
                        Bodegas = dbK66.Database.SqlQuery<BodegaActivaModel>("dbo.sp_obtener_location_warehouse_id").ToList();
                    }
                }
                else if (empresaId == 20210705003)
                {
                    using (var dbK66 = new VMFAESContext())
                    {
                        Bodegas = dbK66.Database.SqlQuery<BodegaActivaModel>("dbo.sp_obtener_location_warehouse_id").ToList();
                    }
                }
                else if (empresaId == 20210705004)
                {
                    using (var dbK66 = new VMGRACOContext())
                    {
                        Bodegas = dbK66.Database.SqlQuery<BodegaActivaModel>("dbo.sp_obtener_location_warehouse_id").ToList();
                    }
                }
            }
            catch (Exception)
            { }

            return Bodegas;
        }

        public List<BodegaActivaModel> ObtenerBodegaActivaxWarehouse(long empresaId, string warehouseId)
        {
            List<BodegaActivaModel> Bodegas = new List<BodegaActivaModel>();

            try
            {
                if (empresaId == 20210705001)
                {
                    using (var dbK66 = new VMBOLIKContext())
                    {
                        Bodegas = dbK66.Database.SqlQuery<BodegaActivaModel>("dbo.sp_obtener_location_x_warehouse_id @WarehouseId", new SqlParameter("@WarehouseId", warehouseId)).ToList();
                    }
                }
                else if (empresaId == 20210705002)
                {
                    using (var dbK66 = new VMEMPAQUESContext())
                    {
                        Bodegas = dbK66.Database.SqlQuery<BodegaActivaModel>("dbo.sp_obtener_location_x_warehouse_id @WarehouseId", new SqlParameter("@WarehouseId", warehouseId)).ToList();
                    }
                }
                else if (empresaId == 20210705003)
                {
                    using (var dbK66 = new VMFAESContext())
                    {
                        Bodegas = dbK66.Database.SqlQuery<BodegaActivaModel>("dbo.sp_obtener_location_x_warehouse_id @WarehouseId", new SqlParameter("@WarehouseId", warehouseId)).ToList();
                    }
                }
                else if (empresaId == 20210705004)
                {
                    using (var dbK66 = new VMGRACOContext())
                    {
                        Bodegas = dbK66.Database.SqlQuery<BodegaActivaModel>("dbo.sp_obtener_location_x_warehouse_id @WarehouseId", new SqlParameter("@WarehouseId", warehouseId)).ToList();
                    }
                }
            }
            catch (Exception)
            { }

            return Bodegas;
        }

        public PrecioxCantidadModel ObtenerPrecioxCantidad(long empresaId, string id, string clienteId, int cantidad)
            {
                PrecioxCantidadModel PrecioActual = new PrecioxCantidadModel();

                try
                {
                    if (empresaId == 20210705001)
                    {
                        using (var dbK66 = new VMBOLIKContext())
                        {
                        PrecioActual = ObtenerPrecioApi(id, "BOLIK", clienteId, cantidad);
                            //PrecioActual = dbK66.Database.SqlQuery<PrecioxCantidadModel>("dbo.sp_obtener_precio_por_cantidad @ID, @ClienteID, @Cantidad", new SqlParameter("@ID", id), new SqlParameter("@ClienteID", clienteId), new SqlParameter("@Cantidad", cantidad)).FirstOrDefault();
                        }
                    }
                    else if (empresaId == 20210705002)
                    {
                        using (var dbK66 = new VMEMPAQUESContext())
                        {
                        PrecioActual = ObtenerPrecioApi(id, "EMPAQUES", clienteId, cantidad);
                        //PrecioActual = dbK66.Database.SqlQuery<PrecioxCantidadModel>("dbo.sp_obtener_precio_por_cantidad @ID, @ClienteID, @Cantidad", new SqlParameter("@ID", id), new SqlParameter("@ClienteID", clienteId), new SqlParameter("@Cantidad", cantidad)).FirstOrDefault();
                    }
                    }
                    else if (empresaId == 20210705003)
                    {
                        using (var dbK66 = new VMFAESContext())
                        {
                        PrecioActual = ObtenerPrecioApi(id, "ESCOCESA", clienteId, cantidad);
                        //PrecioActual = dbK66.Database.SqlQuery<PrecioxCantidadModel>("dbo.sp_obtener_precio_por_cantidad @ID, @ClienteID, @Cantidad", new SqlParameter("@ID", id), new SqlParameter("@ClienteID", clienteId), new SqlParameter("@Cantidad", cantidad)).FirstOrDefault();
                    }
                    }
                    else if (empresaId == 20210705004)
                    {
                        using (var dbK66 = new VMGRACOContext())
                        {
                        PrecioActual = ObtenerPrecioApi(id, "GRACO", clienteId, cantidad);
                        //PrecioActual = dbK66.Database.SqlQuery<PrecioxCantidadModel>("dbo.sp_obtener_precio_por_cantidad @ID, @ClienteID, @Cantidad", new SqlParameter("@ID", id), new SqlParameter("@ClienteID", clienteId), new SqlParameter("@Cantidad", cantidad)).FirstOrDefault();
                    }
                    }
                }
                catch (Exception)
                { }

                return PrecioActual;
            }

        public PrecioxCantidadModel ObtenerPrecioApi(string buscar, string NombreEmpresa, string CardCode, int Cantidad)
        {
            // Base URL de la API
            string apiUrl = $"{URL_SAP}ItemCantidad"; 

            // Crear la consulta con parámetros
            string requestUrl = $"{apiUrl}?ItemCode={buscar}&Empresa={NombreEmpresa}&CardCode={CardCode}&Cantidad={Cantidad}";

            // Inicializar HttpClient (asegúrate de gestionar adecuadamente la vida útil de HttpClient)
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // Hacer la solicitud GET de manera síncrona
                    HttpResponseMessage response = client.GetAsync(requestUrl).GetAwaiter().GetResult();

                    // Verificar si la respuesta fue exitosa
                    if (response.IsSuccessStatusCode)
                    {
                        // Leer el contenido de la respuesta de manera síncrona
                        string jsonResponse = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                        // Deserializar el JSON a la lista de modelos
                        List<PrecioxCantidadModel> producto = JsonConvert.DeserializeObject<List<PrecioxCantidadModel>>(jsonResponse);
                        return producto.FirstOrDefault();
                    }
                    else
                    {
                        // Manejo de error si la respuesta no fue exitosa
                        throw new HttpRequestException($"Error al llamar a la API: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    // Manejar la excepción
                    Console.WriteLine($"Excepción: {ex.Message}");
                    return new PrecioxCantidadModel();
                }
            }
        }


        public List<Warehouse> BuscarBodegasxProducto(string buscar, string CardCode, long empresaId)
        {
            List<Warehouse> Productos = new List<Warehouse>();

            try
            {
                //Productos.Add(new Warehouse { WarehouseId = "W1", Nombre = "producto terminado" });
                

                if (empresaId == 20210705001)
                {
                    Productos = ObtenerBodegasApi(buscar, CardCode, "BOLIK");
                }
                else if (empresaId == 20210705002)
                {
                    Productos = ObtenerBodegasApi(buscar, CardCode, "EMPAQUES");
                }
                else if (empresaId == 20210705003)
                {
                    Productos = ObtenerBodegasApi(buscar, CardCode, "ESCOCESA");
                }
                else if (empresaId == 20210705004)
                {
                    Productos = ObtenerBodegasApi(buscar, CardCode, "GRACO");
                }
            }
            catch (Exception)
            { }

            return Productos;
        }

        public ResponseContadorBodega BuscarContadorBodegaxProducto(string ItemCode, string WarehouseCode, long empresaId)
        {
            ResponseContadorBodega Productos = new ResponseContadorBodega();

            try
            {


                if (empresaId == 20210705001)
                {
                    Productos = ObtenerContadorBodegaApi(ItemCode, WarehouseCode, "BOLIK");
                }
                else if (empresaId == 20210705002)
                {
                    Productos = ObtenerContadorBodegaApi(ItemCode, WarehouseCode, "EMPAQUES");
                }
                else if (empresaId == 20210705003)
                {
                    Productos = ObtenerContadorBodegaApi(ItemCode, WarehouseCode, "ESCOCESA");
                }
                else if (empresaId == 20210705004)
                {
                    Productos = ObtenerContadorBodegaApi(ItemCode, WarehouseCode, "GRACO");
                }
            }
            catch (Exception)
            {
                Productos = new ResponseContadorBodega { Contador = 0 };            
            }

            return Productos;
        }


        public List<Warehouse> ObtenerBodegasApi(string CardCode, string parametro, string Empresa)
        {
            // Base URL de la API
            string apiUrl = $"{URL_SAP}Warehouse";

            // Crear la consulta con parámetros
            string requestUrl = $"{apiUrl}?Empresa={Empresa}&CardCode={CardCode}&Rol={parametro}";

            // Inicializar HttpClient (asegúrate de gestionar adecuadamente la vida útil de HttpClient)
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // Hacer la solicitud GET de manera síncrona
                    HttpResponseMessage response = client.GetAsync(requestUrl).GetAwaiter().GetResult();

                    // Verificar si la respuesta fue exitosa
                    if (response.IsSuccessStatusCode)
                    {
                        // Leer el contenido de la respuesta de manera síncrona
                        string jsonResponse = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                        // Deserializar el JSON a la lista de modelos
                        List<Warehouse> producto = JsonConvert.DeserializeObject<List<Warehouse>>(jsonResponse);

                        // Retornar los clientes deserializados
                        return producto;
                    }
                    else
                    {
                        // Manejo de error si la respuesta no fue exitosa
                        throw new HttpRequestException($"Error al llamar a la API: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    // Manejar la excepción
                    Console.WriteLine($"Excepción: {ex.Message}");
                    return new List<Warehouse>();
                }
            }
        }

        public ResponseContadorBodega ObtenerContadorBodegaApi(string ItemCode, string WarehouseCode, string Empresa)
        {
            // Base URL de la API
            string apiUrl = $"{URL_SAP}WarehouseProduct";

            // Crear la consulta con parámetros
            string requestUrl = $"{apiUrl}?Empresa={Empresa}&ItemCode={ItemCode}&Bodega={WarehouseCode}";

            // Inicializar HttpClient (asegúrate de gestionar adecuadamente la vida útil de HttpClient)
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // Hacer la solicitud GET de manera síncrona
                    HttpResponseMessage response = client.GetAsync(requestUrl).GetAwaiter().GetResult();

                    // Verificar si la respuesta fue exitosa
                    if (response.IsSuccessStatusCode)
                    {
                        // Leer el contenido de la respuesta de manera síncrona
                        string jsonResponse = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                        // Deserializar el JSON a la lista de modelos
                        List<ResponseContadorBodega> producto = JsonConvert.DeserializeObject<List<ResponseContadorBodega>>(jsonResponse);

                        // Retornar los clientes deserializados
                        return producto.FirstOrDefault();
                    }
                    else
                    {
                        // Manejo de error si la respuesta no fue exitosa
                        throw new HttpRequestException($"Error al llamar a la API: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    // Manejar la excepción
                    Console.WriteLine($"Excepción: {ex.Message}");
                    return new ResponseContadorBodega { Contador = 0 };
                }
            }
        }

        #endregion
    }
}
