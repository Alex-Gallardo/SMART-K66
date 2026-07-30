using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class ReparacionBL
    {

        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public ReparacionBL()
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

                    Reparacion ReparacionActual = db.Set<Reparacion>().AsNoTracking().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (ReparacionActual != null)
                    {
                        Inicial_Id = ReparacionActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;

                }
                catch (Exception)
                {
                }

                return Id;
            }

            private bool Agregar(Reparacion entidad)
            {
                bool ReparacionAgregar = false;

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {

                        long lngReparacionId = new Herramienta().Formato_Correlativo(Id);

                        if (lngReparacionId > 0)
                        {
                            entidad.ReparacionId = lngReparacionId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            if (entidad.Servicios != null && entidad.Servicios.Count() > 0)
                            {
                                foreach (var Servicio in entidad.Servicios)
                                {
                                    Servicio.ReparacionId = entidad.ReparacionId;
                                }
                            }

                            if (entidad.Piezas != null && entidad.Piezas.Count() > 0)
                            {
                                foreach (var Pieza in entidad.Piezas)
                                {
                                    Pieza.ReparacionId = entidad.ReparacionId;

                                    ProductoInventario InventarioActual = db.Set<ProductoInventario>().Where(x => x.ProductoId == Pieza.ProductoId && x.AgenciaId == entidad.AgenciaId).FirstOrDefault();
                                    if (InventarioActual != null)
                                    {
                                        InventarioActual.Cantidad -= Pieza.Cantidad;
                                    }
                                }
                            }

                            if (entidad.Imagenes != null && entidad.Imagenes.Count() > 0)
                            {
                                int imagenId = 1;
                                foreach (var Imagen in entidad.Imagenes)
                                {
                                    Imagen.FotografiaId = imagenId;
                                    Imagen.ReparacionId = entidad.ReparacionId;
                                    imagenId++;
                                }
                            }

                            if (entidad.Politicas != null && entidad.Politicas.Count() > 0)
                            {
                                int OrdenId = 1;
                                foreach (var Politica in entidad.Politicas)
                                {
                                    Politica.ReparacionId = entidad.ReparacionId;
                                    Politica.OrdenId = OrdenId;
                                    OrdenId++;
                                }
                            }

                            db.Set<Reparacion>().Add(entidad);
                            db.SaveChanges();
                            ReparacionAgregar = true;
                        }
                    }
                }
                catch (Exception)
                {
                }

                return ReparacionAgregar;
            }

            private bool Actualizar(Reparacion entidad)
            {
                bool ReparacionActualizar = false;

                try
                {
                    Reparacion ReparacionActual = ObtenerPorId(entidad.ReparacionId, false, false);

                    if (ReparacionActual.ReparacionId > 0)
                    {
                        if (!string.IsNullOrWhiteSpace(entidad.Descripcion))
                        {
                            ReparacionActual.Descripcion = entidad.Descripcion;
                        }

                        if (entidad.CostoServicio > 0)
                        {
                            ReparacionActual.CostoServicio = entidad.CostoServicio;
                        }

                        if (entidad.FechaIniciaReparacion.HasValue)
                        {
                            ReparacionActual.FechaIniciaReparacion = entidad.FechaIniciaReparacion.Value;
                            ReparacionActual.UsrAsignado = entidad.UsrAsignado.Value;
                        }

                        if (entidad.Operado)
                        {
                            ReparacionActual.FechaFinalizaReparacion = DateTime.Today;
                            ReparacionActual.Operado = entidad.Operado;
                        }

                        if (entidad.UsrEntrega.HasValue)
                        {
                            ReparacionActual.UsrEntrega = entidad.UsrEntrega.Value;
                            ReparacionActual.FechaCancelacion = DateTime.Today;
                        }

                        ReparacionActual.EstadoId = entidad.EstadoId;

                        if (entidad.EstadoId == 3)
                        {
                            if (!string.IsNullOrWhiteSpace(entidad.Comentario))
                            {
                                ReparacionActual.Comentario = entidad.Comentario;
                            }
                        }

                        if (entidad.EstadoId == 4)
                        {
                            ReparacionActual.TipoId = entidad.TipoId;
                            if (entidad.TipoId == 2)
                            {
                                ReparacionActual.Serie = entidad.Serie;
                                ReparacionActual.Factura = entidad.Factura;
                            }
                        }

                        if (entidad.EstadoId == 5)
                        {
                            if (ReparacionActual.FechaFinalizaReparacion == null)
                            {
                                ReparacionActual.FechaFinalizaReparacion = DateTime.Today;
                            }

                            ReparacionActual.Operado = true;
                        }

                        if (entidad.Pagos != null && entidad.Pagos.Count() > 0)
                        {
                            foreach (var Pago in entidad.Pagos)
                            {
                                db.Set<ReparacionFormaPago>().Add(new ReparacionFormaPago() { ReparacionId = entidad.ReparacionId, FormaPagoId = Pago.FormaPagoId, Valor = Pago.Valor, Nota = Pago.Nota });
                            }
                        }

                        db.SaveChanges();
                        ReparacionActualizar = true;
                    }

                }
                catch (Exception)
                {
                }

                return ReparacionActualizar;
            }


            private string MensajePersonalizadoPorEstadoId(int estadoId)
            {
                string Mensaje = string.Empty;

                try
                {
                    switch (estadoId)
                    {
                        case 1:
                            Mensaje = "El equipo será asignado a un técnico";
                            break;
                        case 2:
                            Mensaje = "El equipo ya está siendo examinado por un técnico";
                            break;
                        case 3:
                            Mensaje = "El equipo ya se encuentra disponible para entrega";
                            break;
                    }
                }
                catch (Exception)
                {
                }

                return Mensaje;
            }

        #endregion

        #region Metodos Publicos

            public string Guardar(Reparacion entidad)
            {
                string Mensaje = "OK";
                bool OperacionExitosa = false;

                if (entidad.ReparacionId > 0)
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

            public string Anular(long reparacionId, string comentario, long usuarioId)
            {
                string Mensaje = "OK";

                try
                {

                    Reparacion ReparacionActual = db.Set<Reparacion>().Where(x => x.ReparacionId == reparacionId).FirstOrDefault();
                    if (ReparacionActual == null)
                    {
                        return "La reparación que selecciono no se encuentra disponible";
                    }

                    ReparacionActual.Comentario = comentario;
                    ReparacionActual.Anulada = true;
                    ReparacionActual.UsrAnular = usuarioId;
                    ReparacionActual.FechaAnular = DateTime.Now;                  

                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            public string Aprobar(long reparacionId)
            {
                string Mensaje = "OK";

                try
                {
                    Reparacion ReparacionActual = db.Set<Reparacion>().Where(x => x.ReparacionId == reparacionId).FirstOrDefault();
                    if (ReparacionActual == null)
                    {
                        return "La reparación que selecciono no se encuentra disponible";
                    }

                    ReparacionActual.EstadoId = 6;                 
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            public Reparacion ObtenerPorId(long id, bool comentario, bool todo, long tecnicoId = 0)
            {
                Reparacion ReparacionActual = new Reparacion();

                try
                {
                    if (todo)
                    {
                        if (comentario)
                        {
                            ReparacionActual = db.Set<Reparacion>().Include("Agencia").Include("Cliente").Include("Servicios").Include("Servicios.Servicio").Include("Piezas").Include("Piezas.Producto").Include("Comentarios").Include("Comentarios.UsuarioAnotacion").Include("Estado").Include("Imagenes").Include("Pagos").Include("Pagos.FormaPago").Include("Politicas").Include("Politicas.Politica").Where(x => x.ReparacionId == id).FirstOrDefault();
                        }
                        else
                        {
                            ReparacionActual = db.Set<Reparacion>().Include("Agencia").Include("Cliente").Include("Servicios").Include("Servicios.Servicio").Include("Piezas").Include("Piezas.Producto").Include("Estado").Include("Imagenes").Include("Politicas").Include("Politicas.Politica").Include("Politicas.Politica.Politicas").Include("Politicas.Politica.Politicas.Politica").Where(x => x.ReparacionId == id).FirstOrDefault();
                        }

                        if (ReparacionActual != null)
                        {
                            if (ReparacionActual.Piezas != null && ReparacionActual.Piezas.Count() > 0)
                            {
                                ReparacionActual.CostoProducto = ReparacionActual.Piezas.Sum(x => x.Cantidad * x.Precio);
                            }

                            ReparacionActual.Costo = ReparacionActual.CostoServicio - ReparacionActual.CostoProducto;

                        }
                    }
                    else
                    {
                        ReparacionActual = db.Set<Reparacion>().Include("Agencia").Include("Cliente").Include("Piezas").Where(x => x.ReparacionId == id).FirstOrDefault();

                        if (ReparacionActual != null)
                        {
                            if (ReparacionActual.Piezas != null && ReparacionActual.Piezas.Count() > 0)
                            {
                                ReparacionActual.CostoProducto = ReparacionActual.Piezas.Sum(x => x.Cantidad * x.Precio);
                            }
                        }
                    }

                    if (tecnicoId > 0)
                    {
                        if (ReparacionActual != null)
                        {
                            if (ReparacionActual.EstadoId == 2)
                            {
                                if (ReparacionActual.UsrAsignado == tecnicoId)
                                {
                                    List<ReparacionAnotacion> Anotaciones = db.Set<ReparacionAnotacion>().Where(x => x.ReparacionId == ReparacionActual.ReparacionId && !x.Visto).ToList();
                                    if (Anotaciones != null && Anotaciones.Count() > 0)
                                    {
                                        Anotaciones.ForEach(x =>
                                        {
                                            x.Visto = true;
                                        });

                                        db.SaveChanges();
                                    }                                    
                                }
                            }                            
                        }                        
                    }
                }
                catch (Exception)
                {
                }

                return ReparacionActual;
            }

            public List<Reparacion> ObtenerListadoPorFecha(long agenciaId, long usuarioId, DateTime fechaInicial, DateTime fechaFinal)
            {
                List<Reparacion> Reparaciones = new List<Reparacion>();
                List<long> AgenciaIds = new List<long>();

                try
                {

                    if (agenciaId == 0)
                    {
                        AgenciaIds = db.Set<UsuarioAgencia>().AsNoTracking().Where(x => x.UsuarioId == usuarioId).Select(x => x.AgenciaId).ToList();
                    }
                    else
                    {
                        AgenciaIds.Add(agenciaId);
                    }

                    Reparaciones = db.Set<Reparacion>().Include("Agencia").Include("Cliente").Include("UsuarioAsignado").Include("UsuarioEntrega").Include("Estado").AsNoTracking().Where(x => AgenciaIds.Contains(x.AgenciaId) && x.Fecha >= fechaInicial && x.Fecha <= fechaFinal).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ReparacionId).ToList();
                    if (Reparaciones != null && Reparaciones.Count() > 0)
                    {
                        foreach (var item in Reparaciones.Where(x => x.Operado == true))
                        {
                            TimeSpan tspan = DateTime.Today - item.FechaEntrega;
                            item.DiasGames = tspan.Days;
                        }
                    }
                }
                catch (Exception)
                {
                }

                return Reparaciones;
            }

            public List<Reparacion> ObtenerListadoPorFecha(long usuarioId, DateTime fechaInicial, DateTime fechaFinal)
            {
                List<Reparacion> Reparaciones = new List<Reparacion>();
                List<long> AgenciaIds = new List<long>();

                try
                {
                    AgenciaIds = db.Set<UsuarioAgencia>().AsNoTracking().Where(x => x.UsuarioId == usuarioId).Select(x => x.AgenciaId).ToList();
                    if (AgenciaIds !=  null && AgenciaIds.Count() > 0)
                    {
                        Reparaciones = db.Set<Reparacion>().Include("Agencia").Include("Cliente").Include("UsuarioAsignado").Include("UsuarioEntrega").Include("Estado").AsNoTracking().Where(x => AgenciaIds.Contains(x.AgenciaId) && x.Fecha >= fechaInicial && x.Fecha <= fechaFinal).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ReparacionId).ToList();
                        if (Reparaciones != null && Reparaciones.Count() > 0)
                        {
                            foreach (var item in Reparaciones.Where(x => x.Operado == true))
                            {
                                TimeSpan tspan = DateTime.Today - item.FechaEntrega;
                                item.DiasGames = tspan.Days;
                            }
                        }                         
                    }                   
                }
                catch (Exception)
                {
                }

                return Reparaciones;
            }

            public List<Reparacion> Buscar(string search, long usuarioId)
            {
                List<Reparacion> Reparaciones = new List<Reparacion>();
                long ReparacionId = 0;

                try
                {
                    long.TryParse(search, out ReparacionId);

                    var AgenciasIds = db.Set<UsuarioAgencia>().Where(x => x.UsuarioId == usuarioId).AsEnumerable().Select(x => x.AgenciaId).ToList();
                    if (AgenciasIds != null && AgenciasIds.Count() > 0)
                    {
                        if (ReparacionId > 0)
                        {
                            Reparaciones = db.Set<Reparacion>().Include("Agencia").Include("Cliente").Include("UsuarioAsignado").Include("UsuarioEntrega").Include("Estado").AsNoTracking().Where(x => x.ReparacionId == ReparacionId && AgenciasIds.Contains(x.AgenciaId)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ReparacionId).ToList();
                        }
                        else
                        {
                            Reparaciones = db.Set<Reparacion>().Include("Agencia").Include("Cliente").Include("UsuarioAsignado").Include("UsuarioEntrega").Include("Estado").AsNoTracking().Where(x => (x.Agencia.Nombre.ToLower().Contains(search.ToLower()) || x.Cliente.Nombre.ToLower().Contains(search.ToLower())) && AgenciasIds.Contains(x.AgenciaId)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ReparacionId).ToList();
                        }

                        if (Reparaciones != null && Reparaciones.Count() > 0)
                        {
                            foreach (var item in Reparaciones.Where(x => x.Operado == true))
                            {
                                TimeSpan tspan = DateTime.Today - item.FechaEntrega;
                                item.DiasGames = tspan.Days;
                            }
                        }     
                    }
                }
                catch (Exception)
                {
                }

                return Reparaciones;
            }

            public List<Reparacion> ObtenerListadoPorUsuarioYDepartamento(long usuarioId, long departamentoId, int estadoId, bool noAsignados = true, bool pendientes5Meses = false, bool pendientes6Meses = false)
            {
                List<Reparacion> Reparaciones = new List<Reparacion>();
                List<long> AgenciaIds = new List<long>();

                try
                {

                    AgenciaIds = db.Set<UsuarioAgencia>().AsNoTracking().Where(x => x.UsuarioId == usuarioId).Select(x => x.AgenciaId).ToList();

                    if (noAsignados)
                    {
                        Reparaciones = db.Set<Reparacion>().Include("Agencia").Include("Cliente").AsNoTracking().Where(x => AgenciaIds.Contains(x.AgenciaId) && x.UsrAsignado == null && !x.Operado && !x.Anulada).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ReparacionId).ToList();
                    }
                    else
                    {
                        if (estadoId == 2)
                        {
                            Reparaciones = db.Set<Reparacion>().Include("Agencia").Include("Cliente").Include("Comentarios").AsNoTracking().Where(x => AgenciaIds.Contains(x.AgenciaId) && x.UsrAsignado == usuarioId && x.EstadoId == estadoId && !x.Operado && !x.Anulada).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ReparacionId).ToList();                           
                        }
                        else if (estadoId == 6)
                        {
                            Reparaciones = db.Set<Reparacion>().Include("Agencia").Include("Cliente").AsNoTracking().Where(x => AgenciaIds.Contains(x.AgenciaId) && x.EstadoId == estadoId && !x.Anulada).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ReparacionId).ToList();
                        }
                    }

                    if (Reparaciones != null && Reparaciones.Count() > 0)
                    {
                        foreach (var item in Reparaciones.Where(x => x.Operado == true))
                        {
                            TimeSpan tspan = DateTime.Today - item.FechaEntrega;
                            item.DiasGames = tspan.Days;
                        }
                    }

                    if (estadoId == 6)
                    {
                        if (pendientes5Meses)
                        {
                            Reparaciones = Reparaciones.Where(x => x.DiasGames >= 60 && x.DiasGames <= 150).ToList();
                        }

                        if (pendientes6Meses)
                        {
                            Reparaciones = Reparaciones.Where(x => x.DiasGames >= 180).ToList();
                        }
                    }
                }
                catch (Exception)
                {
                }

                return Reparaciones;
            }

            public ReparacionFotografia Fotografia(int fotografiaId, long reparacionId)
            {
                ReparacionFotografia FotografiaActual = new ReparacionFotografia();

                try
                {
                    FotografiaActual = db.Set<ReparacionFotografia>().AsNoTracking().Where(x => x.FotografiaId == fotografiaId && x.ReparacionId == reparacionId).FirstOrDefault();
                }
                catch (Exception)
                {
                }

                return FotografiaActual;
            }

            public bool NuevoProducto(ReparacionPieza pieza)
            {
                bool OperacionExitosa = false;

                try
                {
                    Reparacion ReparacionActual = ObtenerPorId(pieza.ReparacionId, false, false);

                    if (ReparacionActual != null)
                    {
                        db.Set<ReparacionPieza>().Add(new ReparacionPieza() { ReparacionId = pieza.ReparacionId, ProductoId = pieza.ProductoId, Cantidad = pieza.Cantidad, Precio = pieza.Precio });

                        ProductoInventario InventarioActual = db.Set<ProductoInventario>().Where(x => x.ProductoId == pieza.ProductoId && x.AgenciaId == ReparacionActual.AgenciaId).FirstOrDefault();
                        if (InventarioActual != null)
                        {
                            InventarioActual.Cantidad -= pieza.Cantidad;
                        }

                        ReparacionActual.CostoServicio += pieza.Cantidad * pieza.Precio;

                        db.SaveChanges();
                        OperacionExitosa = true;
                    }
                }
                catch (Exception)
                {
                }

                return OperacionExitosa;
            }      

            public string ActualizarCosto(Reparacion entidad)
            {
                string Mensaje = "OK";

                try
                {
                    Reparacion ReparacionActual = ObtenerPorId(entidad.ReparacionId, false, false);

                    if (ReparacionActual.ReparacionId > 0)
                    {
                        ReparacionActual.IMEI = entidad.IMEI;
                        ReparacionActual.Marca = entidad.Marca;
                        ReparacionActual.Falla = entidad.Falla;
                        ReparacionActual.Descripcion = entidad.Descripcion;
                        ReparacionActual.Garantia = entidad.Garantia;
                        ReparacionActual.FechaEntrega = entidad.FechaEntrega;
                        ReparacionActual.CostoServicio = entidad.Costo + ReparacionActual.CostoProducto;

                        if (entidad.Servicios != null && entidad.Servicios.Count() > 0)
                        {
                            List<ReparacionServicio> Servicios = db.Set<ReparacionServicio>().Where(x => x.ReparacionId == entidad.ReparacionId).ToList();
                            db.Set<ReparacionServicio>().RemoveRange(Servicios);

                            foreach (var Servicio in entidad.Servicios)
                            {
                                Servicio.ReparacionId = entidad.ReparacionId;
                                db.Set<ReparacionServicio>().Add(Servicio);
                            }
                        }

                        if (entidad.Politicas != null && entidad.Politicas.Count() > 0)
                        {
                            List<ReparacionPoliticaCategoria> Politicas = db.Set<ReparacionPoliticaCategoria>().Where(x => x.ReparacionId == entidad.ReparacionId).ToList();
                            db.Set<ReparacionPoliticaCategoria>().RemoveRange(Politicas);

                            int OrdenId = 1;
                            foreach (var Politica in entidad.Politicas)
                            {
                                Politica.ReparacionId = entidad.ReparacionId;
                                Politica.OrdenId = OrdenId;
                                db.Set<ReparacionPoliticaCategoria>().Add(Politica);
                                OrdenId++;
                            }
                        }

                        db.SaveChanges();
                    }

                }
                catch (Exception)
                {
                    Mensaje = "La información ingresada no es valida";
                }

                return Mensaje;
            }

            public bool EliminarPieza(long ReparacionId, long AgenciaId, string ProductoId)
            {
                try
                {
                    ReparacionPieza PiezaActual = db.Set<ReparacionPieza>().Where(x => x.ReparacionId == ReparacionId && x.ProductoId == ProductoId).FirstOrDefault();
                    if (PiezaActual != null && PiezaActual.ReparacionId > 0)
                    {
                        db.Set<ReparacionPieza>().Remove(PiezaActual);
                    }

                    ProductoInventario InventarioActual = db.Set<ProductoInventario>().Where(x => x.AgenciaId == AgenciaId && x.ProductoId == ProductoId).FirstOrDefault();
                    if (InventarioActual != null)
                    {
                        InventarioActual.Cantidad += PiezaActual.Cantidad;
                    }

                    Reparacion ReparacionActual = db.Set<Reparacion>().Where(x => x.ReparacionId == ReparacionId).FirstOrDefault();
                    if (ReparacionActual != null && ReparacionActual.ReparacionId > 0)
                    {
                        ReparacionActual.CostoServicio -= PiezaActual.Precio;
                    }

                    db.SaveChanges();
                }
                catch (Exception)
                {
                    return false;
                }

                return true;
            }

            public List<ReparacionFotografia> ObtenerFotografias(long reparacionId)
            {
                List<ReparacionFotografia> Fotografias = new List<ReparacionFotografia>();

                try
                {
                    Fotografias = db.Set<ReparacionFotografia>().AsNoTracking().Where(x => x.ReparacionId == reparacionId).ToList();
                }
                catch (Exception)
                {
                }

                return Fotografias;
            }

            public List<Reparacion> ObtenerListadoxEstados(long agenciaId, long usuarioId)
            {
                List<Reparacion> Reparaciones = new List<Reparacion>();
                List<long> AgenciaIds = new List<long>();
                List<long> EstadoIds = new List<long>() { 1, 2, 3 };

                try
                {

                    if (agenciaId == 0)
                    {
                        AgenciaIds = db.Set<UsuarioAgencia>().AsNoTracking().Where(x => x.UsuarioId == usuarioId).Select(x => x.AgenciaId).ToList();
                    }
                    else
                    {
                        AgenciaIds.Add(agenciaId);
                    }

                    Reparaciones = db.Set<Reparacion>().Include("Agencia").Include("Cliente").Include("UsuarioAsignado").Include("UsuarioEntrega").Include("Estado").AsNoTracking().Where(x => AgenciaIds.Contains(x.AgenciaId) && EstadoIds.Contains(x.EstadoId)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ReparacionId).ToList();
                    if (Reparaciones != null && Reparaciones.Count() > 0)
                    {
                        foreach (var item in Reparaciones.Where(x => x.Operado == true))
                        {
                            TimeSpan tspan = DateTime.Today - item.FechaEntrega;
                            item.DiasGames = tspan.Days;
                        }
                    }
                }
                catch (Exception)
                {
                }

                return Reparaciones;
            }

            public List<HistorialReparacion> ObtenerHistorialReparacionxTecnicoFecha(long tecnicoId, DateTime fechaInicial, DateTime fechaFinal)
            {
                List<HistorialReparacion> Reparaciones = new List<HistorialReparacion>();
                List<long> AgenciaIds = new List<long>();
                List<int> EstadoIds = new List<int>() { 3, 4, 5 };

                try
                {
                    AgenciaIds = db.Set<UsuarioAgencia>().AsNoTracking().Where(x => x.UsuarioId == tecnicoId).Select(x => x.AgenciaId).ToList();
                    if (AgenciaIds != null && AgenciaIds.Count() > 0)
                    {
                        Reparaciones = db.Set<Reparacion>().Include("Agencia").Include("Cliente").Include("UsuarioAsignado").AsNoTracking().Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && AgenciaIds.Contains(x.AgenciaId) && x.UsrAsignado == tecnicoId && EstadoIds.Contains(x.EstadoId)).AsEnumerable().Select(x => new HistorialReparacion() { ReparacionId = x.ReparacionId, Agencia = x.Agencia.Nombre, Cliente = x.Cliente.Nombre, Fecha = x.Fecha, FechaAsignacion = x.FechaIniciaReparacion.Value, FechaFinalizacion = x.FechaEntrega, Tecnico = x.UsuarioAsignado.Nombre }).ToList();                       
                    }

                    if (Reparaciones != null && Reparaciones.Count() > 0)
                    {
                        //Se obtienen productos x categoria
                        List<string> ProductoIDS = db.Set<Producto>().AsNoTracking().Where(x => x.CategoriaId == 20190305002).Select(x => x.ProductoId).ToList();
                        if (ProductoIDS != null && ProductoIDS.Count() > 0)
                        {
                            Reparaciones.ForEach(x => 
                            {
                                x.Total = 0;

                                List<ReparacionPieza> Productos = db.Set<ReparacionPieza>().AsNoTracking().Where(y => y.ReparacionId == x.ReparacionId && ProductoIDS.Contains(y.ProductoId)).ToList();
                                if (Productos != null && Productos.Count() > 0)
                                {
                                    x.Total = Productos.Sum(y => y.Cantidad * y.Precio);                               
                                }
                            });                            
                        }
                    }
                }
                catch (Exception)
                {
                }

                return Reparaciones;
            }

            public int ObtenerConteoComentariosNuevos(long tecnicoId, long agenciaId) 
            {
                int ComentariosNuevos = 0;

                try
                {
                    List<ReparacionAnotacion> Anotaciones = db.Set<Reparacion>().AsNoTracking().Where(x => x.UsrAsignado == tecnicoId && x.AgenciaId == agenciaId && x.EstadoId == 2 && !x.Anulada).Join(db.Set<ReparacionAnotacion>().AsNoTracking().Where(x => !x.Visto), R => R.ReparacionId, RA => RA.ReparacionId, (R, RA) => new { RA }).Select(x => x.RA).ToList();
                    if (Anotaciones != null && Anotaciones.Count() > 0)
                    {
                        ComentariosNuevos = Anotaciones.Count();       
                    }
                }
                catch (Exception)
                {
                }

                return ComentariosNuevos;
            }

        #endregion

    }
}
