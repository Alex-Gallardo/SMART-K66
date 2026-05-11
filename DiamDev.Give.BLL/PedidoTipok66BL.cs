using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;

namespace DiamDev.Give.BLL
{
    public class PedidoTipok66BL
    {
        #region Variables Globales

            private GiveContext db;
        private string URL_SAP;

        #endregion

        #region Constructores

        public PedidoTipok66BL()
            {
                this.db = new GiveContext();
            this.URL_SAP = ConfigurationManager.AppSettings["URL_SAP"].ToString();
        }

        #endregion

        #region Metodos Privados                  
        
            private string Agregar(PedidoTipoK66 entidad)
            {
                string Mensaje = "OK";

                try
                {   
                    entidad.Fecha = DateTime.Today;    
                    db.Set<PedidoTipoK66>().Add(entidad);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            private string Actualizar(PedidoTipoK66 entidad)
            {
                string Mensaje = "OK";

                try
                {
                    PedidoTipoK66 PedidoTipoActual = db.Set<PedidoTipoK66>().Where(x => x.TipoId == entidad.TipoId).FirstOrDefault();
                    if (PedidoTipoActual != null)
                    {
                        PedidoTipoActual.EmpresaId = entidad.EmpresaId;
                        PedidoTipoActual.Nombre = entidad.Nombre;
                        PedidoTipoActual.Descripcion = entidad.Descripcion;
                        PedidoTipoActual.CodigoIntregracion1 = entidad.CodigoIntregracion1;
                        PedidoTipoActual.CodigoIntregracion2 = entidad.CodigoIntregracion2;

                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            private bool Existe(Guid id) 
            {
                bool Existe = false;

                try
                {
                    Existe = db.Set<PedidoTipoK66>().AsNoTracking().Where(x => x.TipoId == id).Count() > 0;
                }
                catch (Exception)
                {}    

                return Existe;
            }

        #endregion

        #region Metodos Publicos

            public string Guardar(PedidoTipoK66 entidad)
            {
                string Mensaje = "OK";

                if (Existe(entidad.TipoId))
                {
                    Mensaje = Actualizar(entidad);
                }
                else
                {
                    Mensaje = Agregar(entidad);
                }

                return Mensaje;
            }           

            public PedidoTipoK66 ObtenerxId(Guid id) 
            {
                PedidoTipoK66 PedidoTipoActual = new PedidoTipoK66();

                try
                {
                    PedidoTipoActual = db.Set<PedidoTipoK66>().Include("Empresa").Include("Responsable").AsNoTracking().Where(x => x.TipoId == id).FirstOrDefault();
                }
                catch (Exception)
                {}

                return PedidoTipoActual;
            }

            public List<PedidoTipoK66> ObtenerListado()
            {
                List<PedidoTipoK66> PedidoTipos = new List<PedidoTipoK66>();

                try
                {
                    PedidoTipos = db.Set<PedidoTipoK66>().Include("Empresa").Include("Responsable").AsNoTracking().OrderByDescending(x => x.Fecha).ThenByDescending(x => x.TipoId).ToList();
                }
                catch (Exception)
                {}

                return PedidoTipos;
            }

            public List<PedidoTipoSap> ObtenerListadoxEmpresa(long empresaId)
            {
                List<PedidoTipoSap> PedidoTipos = new List<PedidoTipoSap>();

                try
                {
                    //PedidoTipos = db.Set<PedidoTipoK66>().Include("Empresa").Include("Responsable").AsNoTracking().Where(x => x.EmpresaId == empresaId).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.TipoId).ToList();
                    if (empresaId == 20210705001)
                    {
                        PedidoTipos = ObtenerTipoPedidoApi("BOLIK");
                    }
                    else if (empresaId == 20210705002)
                    {
                        PedidoTipos = ObtenerTipoPedidoApi("EMPAQUES");
                    }
                    else if (empresaId == 20210705003)
                    {
                        PedidoTipos = ObtenerTipoPedidoApi("ESCOCESA");
                    }
                    else if (empresaId == 20210705004)
                    {
                        PedidoTipos = ObtenerTipoPedidoApi("GRACO");
                    }
                }
                catch (Exception)
                { }

                return PedidoTipos;
            }

            public List<PedidoTipoSap> ObtenerTipoPedidoApi(string NombreEmpresa)
            {
                // Base URL de la API
                string apiUrl = $"{URL_SAP}TipoPedido";

                // Crear la consulta con parámetros
                string requestUrl = $"{apiUrl}?Empresa={NombreEmpresa}";

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
                            List<PedidoTipoSap> producto = JsonConvert.DeserializeObject<List<PedidoTipoSap>>(jsonResponse);

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
                        return new List<PedidoTipoSap>();
                    }
                }
            }

        public List<PedidoTipoK66> Buscar(string search)
            {
                List<PedidoTipoK66> PedidoTipos = new List<PedidoTipoK66>();               

                try
                {
                    PedidoTipos = db.Set<PedidoTipoK66>().Include("Empresa").Include("Responsable").AsNoTracking().Where(x => (x.Nombre.ToLower().Contains(search.ToLower()) || x.CodigoIntregracion1.ToLower().Contains(search.ToLower()) || x.CodigoIntregracion2.ToLower().Contains(search.ToLower()))).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.TipoId).ToList();
                }
                catch (Exception)
                {}

                return PedidoTipos;
            }            
            
        #endregion
    }
}
