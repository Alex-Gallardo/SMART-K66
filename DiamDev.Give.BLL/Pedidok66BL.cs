using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Newtonsoft.Json;
using System.Net.Http;
using System.Configuration;
using System.IO;

namespace DiamDev.Give.BLL
{
    public class Pedidok66BL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public Pedidok66BL()
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
                    PedidoK66 PedidoActual = db.Set<PedidoK66>().AsNoTracking().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (PedidoActual != null)
                    {
                        Inicial_Id = PedidoActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;
                }
                catch (Exception)
                {}

                return Id;
            }            
        
            private string Agregar(PedidoK66 entidad)
            {
                string Mensaje = "OK";

                string PathDocumento = ConfigurationManager.AppSettings["Path_Fotografia_Pedido_K66"].ToString();
                string UrlDocumento = ConfigurationManager.AppSettings["Url_Fotografia_Pedido_K66"].ToString();

                try
                {
                    if(entidad.DireccionEntrega==null || entidad.DireccionEntrega.Equals(""))
                    {
                    throw new Exception("Debe ingresar Dirección de Entrega.");
                    }
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngPedidoId = new Herramienta().Formato_Correlativo(Id);

                        if (lngPedidoId > 0)
                        {
                            entidad.PedidoId = lngPedidoId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;
                            entidad.FechaHoraPedido = DateTime.Now;

                            if (!string.IsNullOrWhiteSpace(entidad.DocumentoOrdenCompraRespaldo))
                            {
                                entidad.DocumentoOrdenCompraRespaldo = string.Format(@"{0}{1}/{2}", UrlDocumento, entidad.PedidoId, entidad.DocumentoOrdenCompraRespaldo);
                            }

                            if (entidad.Documentos != null && entidad.Documentos.Count() > 0)
                            {
                                int DocumentoId = 1;
                                entidad.DImportantes = new List<PedidoDocumentoImportanteK66>();
                                entidad.Documentos.ForEach(d => 
                                {
                                    //string FotografiaActual = string.Format(@"{0}{1}/DOC/{2}", UrlDocumento, entidad.PedidoId, d.Nombre.Replace(" ", "_"));
                                    string FotografiaActual = string.Format(@"{0}{1}/{2}", UrlDocumento, entidad.PedidoId, d.Nombre.Replace(" ", "_"));
                                    entidad.DImportantes.Add(new PedidoDocumentoImportanteK66() { DocumentoId = DocumentoId, PedidoId = entidad.PedidoId, Nombre = d.Nombre.Replace(" ", "_"), FotografiaApp = FotografiaActual });
                                    DocumentoId++;
                                });
                            }

                            bool PrecioCambiado = entidad.Detalles.Where(x => x.PrecioCambiado).Count() > 0;
                            if (PrecioCambiado)
                            {
                                entidad.EstadoId = 3;
                            }
                            else
                            {
                                entidad.EstadoId = 2;
                            }                            

                            if (entidad.Detalles != null && entidad.Detalles.Count() > 0)
                            {
                                int i = 1;
                                foreach (var Detalle in entidad.Detalles)
                                {
                                    Detalle.DetalleId = i;
                                    Detalle.PedidoId = entidad.PedidoId;
                                    i++;
                                }
                            }

                            db.Set<PedidoK66>().Add(entidad);
                            db.SaveChanges();

                            if (Mensaje.Equals("OK"))
                            {
                                //Se crea carpeta por documento
                                string Path_Documento = string.Format(@"{0}\{1}", PathDocumento, entidad.PedidoId);

                                if (!(Directory.Exists(Path_Documento)))
                                {
                                    Directory.CreateDirectory(Path_Documento);
                                }

                                if (entidad.Documento != null)
                                {
                                    File.WriteAllBytes(string.Format(@"{0}\{1}\{2}", PathDocumento, entidad.PedidoId, entidad.Documento.Nombre.Replace(" ", "_")), entidad.Documento.Content);
                                }

                                string PathDocumentoImportante = Path_Documento;
                                PathDocumentoImportante = string.Format(@"{0}\DOC", PathDocumentoImportante);

                                if (!(Directory.Exists(PathDocumentoImportante)))
                                {
                                    Directory.CreateDirectory(PathDocumentoImportante);
                                }

                                if (entidad.Documentos != null && entidad.Documentos.Count() > 0)
                                {     
                                    entidad.Documentos.ForEach(d =>
                                    {
                                        File.WriteAllBytes(string.Format(@"{0}\{1}", PathDocumentoImportante,  d.Nombre.Replace(" ", "_")), d.Content);
                                    });
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

            private string Actualizar(PedidoK66 entidad)
            {
                string Mensaje = "OK";

                string PathDocumento = ConfigurationManager.AppSettings["Path_Fotografia_Pedido_K66"].ToString();
                string UrlDocumento = ConfigurationManager.AppSettings["Url_Fotografia_Pedido_K66"].ToString();

                try
                {
                    PedidoK66 PedidoK66Actual = db.Set<PedidoK66>().Where(x => x.PedidoId == entidad.PedidoId).FirstOrDefault();
                    if (PedidoK66Actual != null)
                    {
                        PedidoK66Actual.TipoPedidoId = entidad.TipoPedidoId;
                        PedidoK66Actual.OrdenCompraCliente = entidad.OrdenCompraCliente;
                        PedidoK66Actual.ObservacionesGenerales = entidad.ObservacionesGenerales;
                        PedidoK66Actual.FechaPrometida = entidad.FechaPrometida;

                        if (!string.IsNullOrWhiteSpace(entidad.DocumentoOrdenCompraRespaldo))
                        {
                            PedidoK66Actual.DocumentoOrdenCompraRespaldo = string.Format(@"{0}{1}/{2}", UrlDocumento, entidad.PedidoId, entidad.DocumentoOrdenCompraRespaldo);
                        }

                        if (entidad.Documentos != null && entidad.Documentos.Count() > 0)
                        {
                            int DocumentoId = 1;

                            PedidoDocumentoImportanteK66 PedidoDocumentoImportanteK66Ultimo = db.Set<PedidoDocumentoImportanteK66>().AsNoTracking().Where(x => x.PedidoId == PedidoK66Actual.PedidoId).OrderByDescending(x => x.DocumentoId).FirstOrDefault();
                            if (PedidoDocumentoImportanteK66Ultimo != null)
                            {
                                DocumentoId = PedidoDocumentoImportanteK66Ultimo.DocumentoId + 1;
                            }                           
                           
                            entidad.Documentos.ForEach(d =>
                            {
                                string FotografiaActual = string.Format(@"{0}{1}/DOC/{2}", UrlDocumento, entidad.PedidoId, d.Nombre.Replace(" ", "_"));
                                db.Set<PedidoDocumentoImportanteK66>().Add(new PedidoDocumentoImportanteK66() { DocumentoId = DocumentoId, PedidoId = entidad.PedidoId, Nombre = d.Nombre.Replace(" ", "_"), FotografiaApp = FotografiaActual });
                                DocumentoId++;
                            });
                        }                      

                        if (entidad.Detalles != null && entidad.Detalles.Count() > 0)
                        {
                            List<PedidoDetalleK66> Detalles = db.Set<PedidoDetalleK66>().Where(x => x.PedidoId == PedidoK66Actual.PedidoId).ToList();
                            db.Set<PedidoDetalleK66>().RemoveRange(Detalles);

                            int i = 1;
                            foreach (var Detalle in entidad.Detalles)
                            {
                                Detalle.DetalleId = i;
                                Detalle.PedidoId = entidad.PedidoId;

                                db.Set<PedidoDetalleK66>().Add(Detalle);
                                i++;
                            }
                        }
                        
                        db.SaveChanges();

                        if (Mensaje.Equals("OK"))
                        {
                            //Se crea carpeta por documento
                            string Path_Documento = string.Format(@"{0}\{1}", PathDocumento, entidad.PedidoId);

                            if (!(Directory.Exists(Path_Documento)))
                            {
                                Directory.CreateDirectory(Path_Documento);
                            }

                            if (entidad.Documento != null)
                            {
                                File.WriteAllBytes(string.Format(@"{0}\{1}\{2}", PathDocumento, entidad.PedidoId, entidad.Documento.Nombre.Replace(" ", "_")), entidad.Documento.Content);
                            }

                            string PathDocumentoImportante = Path_Documento;
                            PathDocumentoImportante = string.Format(@"{0}\DOC", PathDocumentoImportante);

                            if (!(Directory.Exists(PathDocumentoImportante)))
                            {
                                Directory.CreateDirectory(PathDocumentoImportante);
                            }

                            if (entidad.Documentos != null && entidad.Documentos.Count() > 0)
                            {
                                entidad.Documentos.ForEach(d =>
                                {
                                    File.WriteAllBytes(string.Format(@"{0}\{1}", PathDocumentoImportante, d.Nombre.Replace(" ", "_")), d.Content);
                                });
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

            public EstadoxPedidoK66 ObtenerEstadoxPedido(long empresaId, string id)
            {
                EstadoxPedidoK66 EstadoActual = new EstadoxPedidoK66();

                try
                {
                    if (empresaId == 20210705001)
                    {
                        using (var dbK66 = new VMBOLIKContext())
                        {
                            EstadoActual = dbK66.Database.SqlQuery<EstadoxPedidoK66>("dbo.sp_obtener_estado_x_pedido @ID", new SqlParameter("@ID", id)).FirstOrDefault();
                        }
                    }
                    else if (empresaId == 20210705002)
                    {
                        using (var dbK66 = new VMEMPAQUESContext())
                        {
                            EstadoActual = dbK66.Database.SqlQuery<EstadoxPedidoK66>("dbo.sp_obtener_estado_x_pedido @ID", new SqlParameter("@ID", id)).FirstOrDefault();
                        }
                    }
                    else if (empresaId == 20210705003)
                    {
                        using (var dbK66 = new VMFAESContext())
                        {
                            EstadoActual = dbK66.Database.SqlQuery<EstadoxPedidoK66>("dbo.sp_obtener_estado_x_pedido @ID", new SqlParameter("@ID", id)).FirstOrDefault();
                        }
                    }
                    else if (empresaId == 20210705004)
                    {
                        using (var dbK66 = new VMGRACOContext())
                        {
                            EstadoActual = dbK66.Database.SqlQuery<EstadoxPedidoK66>("dbo.sp_obtener_estado_x_pedido @ID", new SqlParameter("@ID", id)).FirstOrDefault();
                        }
                    }
                }
                catch (Exception)
                { }

                return EstadoActual;
            }

        #endregion

        #region Metodos Publicos

            public string Guardar(PedidoK66 entidad)
            {
                string Mensaje = "OK";

                if (entidad.PedidoId == 0)
                {
                    Mensaje = Agregar(entidad);
                }
                else
                {
                    Mensaje = Actualizar(entidad);
                }

                return Mensaje;
            }

            public string GuardarCustomerOrder(long id, string orderId)
            {
                string Mensaje = "OK";

                try
                {
                    PedidoK66 PedidoK66Actual = db.Set<PedidoK66>().Where(x => x.PedidoId == id).FirstOrDefault();
                    if (PedidoK66Actual != null)
                    {
                        PedidoK66Actual.EstadoId = 4;
                        PedidoK66Actual.CUSTOMERORDERID = orderId;

                        PedidoK66Actual.FechaHoraSincronizacion = DateTime.Now;
                        PedidoK66Actual.FechaHoraUltimoIntento = DateTime.Now;
                        PedidoK66Actual.Sincronizado = true;

                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            public string Aprobar(PedidoK66 entidad, long responsableId)
            {
                string Mensaje = "OK";

                try
                {
                    PedidoK66 PedidoK66Actual = db.Set<PedidoK66>().Where(x => x.PedidoId == entidad.PedidoId).FirstOrDefault();
                    if (PedidoK66Actual != null)
                    {
                        PedidoK66Actual.EstadoId = 2;
                        PedidoK66Actual.ComentarioAprobacion = entidad.ComentarioAprobacion;
                        PedidoK66Actual.ResponsableAprobacionId = responsableId;

                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            public string Rechazar(PedidoK66 entidad, long responsableId)
            {
                string Mensaje = "OK";

                try
                {
                    PedidoK66 PedidoK66Actual = db.Set<PedidoK66>().Where(x => x.PedidoId == entidad.PedidoId).FirstOrDefault();
                    if (PedidoK66Actual != null)
                    {
                        PedidoK66Actual.EstadoId = 1;
                        PedidoK66Actual.ComentarioAprobacion = entidad.ComentarioAprobacion;
                        PedidoK66Actual.ResponsableAprobacionId = responsableId;

                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            public PedidoK66 ObtenerxId(long id) 
            {
                PedidoK66 PedidoK66Actual = new PedidoK66();

                try
                {
                //PedidoK66Actual = db.Set<PedidoK66>().Include("Empresa").Include("TipoPedido").Include("Estado").Include("Responsable").Include("Detalles").Include("DImportantes").AsNoTracking().Where(x => x.PedidoId == id).FirstOrDefault();
                PedidoK66Actual = db.Set<PedidoK66>().Include("Empresa").Include("Estado").Include("Responsable").Include("Detalles").Include("DImportantes").AsNoTracking().Where(x => x.PedidoId == id).FirstOrDefault();
            }
                catch (Exception e)
                {
            
                }

                return PedidoK66Actual;
            }

            public List<PedidoK66> ObtenerListadoxFecha(DateTime fechaInicial, DateTime fechaFinal, long responsableId)
            {
                List<PedidoK66> Pedidos = new List<PedidoK66>();

                try
                {
                    bool Administrador = db.Set<UsuarioRol>().AsNoTracking().Where(x => x.UsuarioId == responsableId && x.RolId == 36).Count() > 0;
                    if (Administrador)
                    {
                        Pedidos = db.Set<PedidoK66>().Include("Empresa").Include("Responsable").Include("Estado").Include("Detalles").AsNoTracking().Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && x.EstadoId != 1).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.PedidoId).ToList();
                    }
                    else
                    {
                        Pedidos = db.Set<PedidoK66>().Include("Empresa").Include("Responsable").Include("Estado").Include("Detalles").AsNoTracking().Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && x.ResponsableId == responsableId && x.EstadoId != 1).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.PedidoId).ToList();
                    }

                    if (Pedidos != null && Pedidos.Count() > 0)
                    {
                        Pedidos.ForEach(p =>
                        {
                            p.EstadoK66 = "PENDIENTE";

                            if (!string.IsNullOrWhiteSpace(p.CUSTOMERORDERID))
                            {
                                EstadoxPedidoK66 EstadoActual = new EstadoxPedidoK66 {ID = p.Estado?.EstadoId.ToString(),ESTADO = p.Estado?.Nombre };//ObtenerEstadoxPedido(p.EmpresaId, p.CUSTOMERORDERID);
                                if (EstadoActual != null)
                                {
                                    p.EstadoK66 = EstadoActual.ESTADO;
                                }
                            }
                            else
                            {
                                p.EstadoK66 = p.Estado == null ? "NO DISPONIBLE" : p.Estado.Nombre.ToUpper();
                            }
                        });
                    }
                }
                catch (Exception)
                {}

                return Pedidos;
            }

            public List<PedidoK66> ObtenerListadoxFechaxEstado(DateTime fechaInicial, DateTime fechaFinal, int estadoId, long responsableId)
            {
                List<PedidoK66> Pedidos = new List<PedidoK66>();

                try
                {
                    if (responsableId == 0)
                    {
                        Pedidos = db.Set<PedidoK66>().Include("Empresa").Include("Responsable").Include("Estado").Include("Detalles").AsNoTracking().Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && x.EstadoId == estadoId).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.PedidoId).ToList();
                    }
                    else if (responsableId > 0)
                    {
                        bool Administrador = db.Set<UsuarioRol>().AsNoTracking().Where(x => x.UsuarioId == responsableId && x.RolId == 36).Count() > 0;
                        if (Administrador)
                        {
                            Pedidos = db.Set<PedidoK66>().Include("Empresa").Include("Responsable").Include("Estado").Include("Detalles").AsNoTracking().Where(x => x.EstadoId == estadoId).OrderBy(x => x.Fecha).ThenBy(x => x.PedidoId).ToList();
                        }
                        else
                        {
                            Pedidos = db.Set<PedidoK66>().Include("Empresa").Include("Responsable").Include("Estado").Include("Detalles").AsNoTracking().Where(x => x.EstadoId == estadoId && x.ResponsableId == responsableId).OrderBy(x => x.Fecha).ThenBy(x => x.PedidoId).ToList();
                        }                    
                    }
                }
                catch (Exception)
                { }

                return Pedidos;
            }

            public List<PedidoK66> Buscar(string search, long responsableId)
            {
                List<PedidoK66> Pedidos = new List<PedidoK66>();
                long PedidoId = 0;

                try
                {
                    long.TryParse(search, out PedidoId);

                    bool Administrador = db.Set<UsuarioRol>().AsNoTracking().Where(x => x.UsuarioId == responsableId && x.RolId == 36).Count() > 0;
                    if (Administrador)
                    {
                        if (PedidoId > 0)
                        {
                            Pedidos = db.Set<PedidoK66>().Include("Empresa").Include("Responsable").Include("Estado").Include("Detalles").AsNoTracking().Where(x => x.PedidoId == PedidoId && x.EstadoId != 1).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.PedidoId).ToList();
                        }
                        else
                        {
                            Pedidos = db.Set<PedidoK66>().Include("Empresa").Include("Responsable").Include("Estado").Include("Detalles").AsNoTracking().Where(x => (x.IDK66.ToLower().Contains(search.ToLower()) || x.Nombre.ToLower().Contains(search.ToLower())) && x.EstadoId != 1).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.PedidoId).ToList();
                        }
                    }
                    else
                    {
                        if (PedidoId > 0)
                        {
                            Pedidos = db.Set<PedidoK66>().Include("Empresa").Include("Responsable").Include("Estado").Include("Detalles").AsNoTracking().Where(x => x.PedidoId == PedidoId && x.ResponsableId == responsableId && x.EstadoId != 1).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.PedidoId).ToList();
                        }
                        else
                        {
                            Pedidos = db.Set<PedidoK66>().Include("Empresa").Include("Responsable").Include("Estado").Include("Detalles").AsNoTracking().Where(x => (x.IDK66.ToLower().Contains(search.ToLower()) || x.Nombre.ToLower().Contains(search.ToLower())) && (x.ResponsableId == responsableId) && x.EstadoId != 1).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.PedidoId).ToList();
                        }
                    }

                    if (Pedidos != null && Pedidos.Count() > 0)
                    {
                        Pedidos.ForEach(p =>
                        {
                            p.EstadoK66 = "PENDIENTE";

                            if (!string.IsNullOrWhiteSpace(p.CUSTOMERORDERID))
                            {
                                EstadoxPedidoK66 EstadoActual = ObtenerEstadoxPedido(p.EmpresaId, p.CUSTOMERORDERID);
                                if (EstadoActual != null)
                                {
                                    p.EstadoK66 = EstadoActual.ESTADO;
                                }
                            }
                            else
                            {
                                p.EstadoK66 = p.Estado == null ? "NO DISPONIBLE" : p.Estado.Nombre.ToUpper();
                            }
                        });
                    }
                }
                catch (Exception)
                { }

                return Pedidos;
            }

            public List<PedidoK66> BuscarxEstado(string search, int estadoId, long responsableId)
            {
                List<PedidoK66> Pedidos = new List<PedidoK66>();
                long PedidoId = 0;

                try
                {
                    long.TryParse(search, out PedidoId);

                    if (responsableId == 0)
                    {
                        if (PedidoId > 0)
                        {
                            Pedidos = db.Set<PedidoK66>().Include("Empresa").Include("Responsable").Include("Estado").Include("Detalles").AsNoTracking().Where(x => x.PedidoId == PedidoId && x.EstadoId == estadoId).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.PedidoId).ToList();
                        }
                        else
                        {
                            Pedidos = db.Set<PedidoK66>().Include("Empresa").Include("Responsable").Include("Estado").Include("Detalles").AsNoTracking().Where(x => (x.IDK66.ToLower().Contains(search.ToLower()) || x.Nombre.ToLower().Contains(search.ToLower()) || x.Responsable.Nombre.ToLower().Contains(search.ToLower())) && x.EstadoId == estadoId).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.PedidoId).ToList();
                        }
                    }
                    else if (responsableId > 0)
                    {
                        bool Administrador = db.Set<UsuarioRol>().AsNoTracking().Where(x => x.UsuarioId == responsableId && x.RolId == 36).Count() > 0;
                        if (Administrador)
                        {
                            if (PedidoId > 0)
                            {
                                Pedidos = db.Set<PedidoK66>().Include("Empresa").Include("Responsable").Include("Estado").Include("Detalles").AsNoTracking().Where(x => x.PedidoId == PedidoId && x.EstadoId == estadoId).OrderBy(x => x.Fecha).ThenBy(x => x.PedidoId).ToList();
                            }
                            else
                            {
                                Pedidos = db.Set<PedidoK66>().Include("Empresa").Include("Responsable").Include("Estado").Include("Detalles").AsNoTracking().Where(x => (x.IDK66.ToLower().Contains(search.ToLower()) || x.Nombre.ToLower().Contains(search.ToLower()) || x.Responsable.Nombre.ToLower().Contains(search.ToLower())) && x.EstadoId == estadoId).OrderBy(x => x.Fecha).ThenBy(x => x.PedidoId).ToList();
                            }
                        }
                        else
                        {
                            if (PedidoId > 0)
                            {
                                Pedidos = db.Set<PedidoK66>().Include("Empresa").Include("Responsable").Include("Estado").Include("Detalles").AsNoTracking().Where(x => x.PedidoId == PedidoId && x.EstadoId == estadoId && x.ResponsableId == responsableId).OrderBy(x => x.Fecha).ThenBy(x => x.PedidoId).ToList();
                            }
                            else
                            {
                                Pedidos = db.Set<PedidoK66>().Include("Empresa").Include("Responsable").Include("Estado").Include("Detalles").AsNoTracking().Where(x => (x.IDK66.ToLower().Contains(search.ToLower()) || x.Nombre.ToLower().Contains(search.ToLower()) || x.Responsable.Nombre.ToLower().Contains(search.ToLower())) && x.EstadoId == estadoId && x.ResponsableId == responsableId).OrderBy(x => x.Fecha).ThenBy(x => x.PedidoId).ToList();
                            }
                        }
                    }
                }
                catch (Exception)
                { }

                return Pedidos;
            }

            public List<PedidoPendienteK66> ObtenerPendientesSincronizar()
            {
                List<PedidoPendienteK66> Pedidos = new List<PedidoPendienteK66>();

                try
                {
                    Pedidos = db.Database.SqlQuery<PedidoPendienteK66>("dbo.sp_obtener_pendientes_sincronizar_smart").ToList();
                }
                catch (Exception)
                { }

                return Pedidos;
            }


            public ERPPedidoEncabezadoK66 ObtenerPendientexId(long id)
            {
                ERPPedidoEncabezadoK66 PedidoActual = new ERPPedidoEncabezadoK66();

                try
                {
                    PedidoActual = db.Database.SqlQuery<ERPPedidoEncabezadoK66>("dbo.sp_EncabezadoPedidoSincronizar_smart @pedidoid", new SqlParameter("@pedidoid", id)).FirstOrDefault();
                }
                catch (Exception)
                { }

                return PedidoActual;
            }

            public List<ERPPedidoDetalleK66> ObtenerPendienteDetallexId(long id)
            {
                List<ERPPedidoDetalleK66> Detalles = new List<ERPPedidoDetalleK66>();

                try
                {
                    Detalles = db.Database.SqlQuery<ERPPedidoDetalleK66>("dbo.sp_DetallePedidoSincronizar @pedidoid", new SqlParameter("@pedidoid", id)).ToList();
                }
                catch (Exception)
                { }

                return Detalles;
            }

        #endregion
    }
}
