using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class ReservaBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public ReservaBL()
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
                    Reserva ReservaActual = db.Set<Reserva>().AsNoTracking().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (ReservaActual != null)
                    {
                        Inicial_Id = ReservaActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;
                }
                catch (Exception)
                {}

                return Id;
            }

            private string Agregar(Reserva entidad)
            {
                string Mensaje = string.Empty;

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngReservaId = new Herramienta().Formato_Correlativo(Id);

                        if (lngReservaId > 0)
                        {
                            entidad.ReservaId = lngReservaId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;
                            entidad.FechaHoraReserva = DateTime.Now;
                          
                            if (entidad.Detalles != null && entidad.Detalles.Count() > 0)
                            {
                                int i = 1;
                                foreach (var Detalle in entidad.Detalles)
                                {
                                    Detalle.DetalleId = i;
                                    Detalle.ReservaId = entidad.ReservaId;
                                    i++;
                                }
                            }

                            if (entidad.Pagos != null && entidad.Pagos.Count() > 0)
                            {
                                int i = 1;
                                foreach (var Pago in entidad.Pagos)
                                {
                                    Pago.DetalleId = i;
                                    Pago.ReservaId = entidad.ReservaId;
                                    Pago.Fecha = DateTime.Today;
                                    Pago.UsrOperacionId = entidad.UsrCreo;
                                    i++;
                                }
                            }

                            db.Set<Reserva>().Add(entidad);
                            db.SaveChanges();
                            Mensaje = "OK";
                        }
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }   

                return Mensaje;
            }

            private string Actualizar(Reserva entidad)
            {
                string Mensaje = string.Empty;

                try
                {

                    Reserva ReservaActual = ObtenerPorId(entidad.ReservaId);

                    if (ReservaActual.ReservaId > 0)
                    {
                        ReservaActual.Telefono = entidad.Telefono;
                        ReservaActual.Observaciones = entidad.Observaciones;

                        if (entidad.Detalles != null && entidad.Detalles.Count() > 0)
                        {
                            List<ReservaDetalle> Productos = db.Set<ReservaDetalle>().Where(x => x.ReservaId == entidad.ReservaId).ToList();
                            db.Set<ReservaDetalle>().RemoveRange(Productos);

                            int i = 1;
                            foreach (var Producto in entidad.Detalles)
                            {
                                Producto.DetalleId = i;
                                Producto.ReservaId = entidad.ReservaId;
                                db.Set<ReservaDetalle>().Add(Producto);
                                i++;
                            }                            
                        }

                        if (entidad.Pagos != null && entidad.Pagos.Count() > 0)
                        {
                            List<ReservaPago> Pagos = db.Set<ReservaPago>().Where(x => x.ReservaId == entidad.ReservaId).ToList();
                            db.Set<ReservaPago>().RemoveRange(Pagos);

                            int i = 1;
                            foreach (var Producto in entidad.Pagos)
                            {
                                Producto.DetalleId = i;
                                Producto.ReservaId = entidad.ReservaId;
                                db.Set<ReservaPago>().Add(Producto);
                                i++;
                            }   
                        }

                        db.SaveChanges();
                        Mensaje = "OK";
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

            public string Guardar(Reserva entidad)
            {
                string Mensaje = "OK";

                if (entidad.ReservaId > 0)
                {
                    Mensaje = Actualizar(entidad);
                }
                else
                {
                    Mensaje = Agregar(entidad);
                }
                                
                return Mensaje;
            }

            public string Pago(ReservaPagoModel entidad, long usuarioId) 
            {
                string Mensaje = "OK";

                try
                {
                    int Correlativo = 1;

                    ReservaPago PagoAnterior = db.Set<ReservaPago>().AsNoTracking().Where(x => x.ReservaId == entidad.ReservaId).OrderByDescending(x => x.DetalleId).FirstOrDefault();
                    if (PagoAnterior != null)
                    {
                        Correlativo = PagoAnterior.DetalleId + 1;                        
                    }

                    db.Set<ReservaPago>().Add(new ReservaPago() { DetalleId = Correlativo, ReservaId = entidad.ReservaId, FormaPagoId = entidad.FormaId, Valor = entidad.Monto, Nota = "", Fecha = DateTime.Today, UsrOperacionId = usuarioId });
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }   

                return Mensaje;
            }

            public string Anular(long reservaId, string comentario, long usuarioId)
            {
                string Mensaje = "OK";

                try
                {

                    Reserva ReservaActual = db.Set<Reserva>().Where(x => x.ReservaId == reservaId).FirstOrDefault();
                    if (ReservaActual == null)
                    {
                        return "La reserva que selecciono no se encuentra disponible";
                    }

                    ReservaActual.Comentario = comentario;
                    ReservaActual.Anulada = true;
                    ReservaActual.Operado = true;
                    ReservaActual.UsrAnular = usuarioId;
                    ReservaActual.FechaAnular = DateTime.Now;                    

                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            public Reserva ObtenerPorId(long id, bool todo = false)
            {
                Reserva ReservaActual = new Reserva();

                try
                {
                    if (todo)
                    {
                        ReservaActual = db.Set<Reserva>().Include("Agencia").Include("Cliente").Include("Detalles").Include("Detalles.Producto").Include("Detalles.Unidad").Include("Pagos").Include("Pagos.UsuarioOperacion").Include("Pagos.FormaPago").Where(x => x.ReservaId == id).FirstOrDefault();
                    }
                    else
                    {
                        ReservaActual = db.Set<Reserva>().Where(x => x.ReservaId == id).FirstOrDefault();
                    }
                }
                catch (Exception)
                {
                }

                return ReservaActual;
            }

            public List<Reserva> ObtenerListado(long usuarioId)
            {
                List<Reserva> Reservas = new List<Reserva>();

                try
                {
                    var AgenciasIds = db.Set<UsuarioAgencia>().Where(x => x.UsuarioId == usuarioId).AsEnumerable().Select(x => x.AgenciaId).ToList();
                    if (AgenciasIds != null && AgenciasIds.Count() > 0)
                    {
                        Reservas = db.Set<Reserva>().Include("Agencia").Include("Cliente").Include("Detalles").Include("Detalles.Producto").AsNoTracking().Where(x => AgenciasIds.Contains(x.AgenciaId) && !x.Operado).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ReservaId).Take(200).ToList();
                    }

                    if (Reservas != null && Reservas.Count() > 0)
                    {
                        Reservas.ForEach(x => 
                        {
                            x.Productos = string.Empty;
                            x.Detalles.ForEach(p => 
                            {
                                x.Productos += string.Format("{0}, ", p.Producto.Nombre);
                            });
                        });
                    }
                }
                catch (Exception)
                {}

                return Reservas;
            }

            public List<Reserva> ObtenerListadoxCliente(long clienteId)
            {
                List<Reserva> Reservas = new List<Reserva>();

                try
                {
                    Reservas = db.Set<Reserva>().Include("Agencia").Include("Cliente").Include("Detalles").Include("Detalles.Producto").Include("Pagos").AsNoTracking().Where(x => x.ClienteId == clienteId).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ReservaId).ToList();

                    if (Reservas != null && Reservas.Count() > 0)
                    {
                        Reservas.ForEach(x =>
                        {
                            x.Productos = string.Empty;
                            x.Detalles.ForEach(p =>
                            {
                                x.Productos += string.Format("{0}, ", p.Producto.Nombre);
                            });
                        });
                    }
                }
                catch (Exception)
                { }

                return Reservas;
            }

            public List<Reserva> Buscar(string search, long usuarioId)
            {
                List<Reserva> Reservas = new List<Reserva>();
                long ReservaId = 0;

                try
                {
                    long.TryParse(search, out ReservaId);

                     var AgenciasIds = db.Set<UsuarioAgencia>().Where(x => x.UsuarioId == usuarioId).AsEnumerable().Select(x => x.AgenciaId).ToList();
                     if (AgenciasIds != null && AgenciasIds.Count() > 0)
                     {
                         if (ReservaId > 0)
                         {
                             Reservas = db.Set<Reserva>().Include("Agencia").Include("Cliente").Include("Detalles").Include("Detalles.Producto").AsNoTracking().Where(x => x.ReservaId == ReservaId && AgenciasIds.Contains(x.AgenciaId)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ReservaId).ToList();
                         }
                         else
                         {
                             Reservas = db.Set<Reserva>().Include("Agencia").Include("Cliente").Include("Detalles").Include("Detalles.Producto").AsNoTracking().Where(x => (x.Agencia.Nombre.ToLower().Contains(search.ToLower()) || x.Cliente.Nombre.ToLower().Contains(search.ToLower())) && AgenciasIds.Contains(x.AgenciaId)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ReservaId).ToList();
                         }
                     }

                    if (Reservas != null && Reservas.Count() > 0)
                    {
                        Reservas.ForEach(x =>
                        {
                            x.Productos = string.Empty;
                            x.Detalles.ForEach(p =>
                            {
                                x.Productos += string.Format("{0}, ", p.Producto.Nombre);
                            });
                        });
                    }
                }
                catch (Exception)
                {}

                return Reservas;
            }

            public MensajePedido ObtenerReserva(long reservaId)
            {
                MensajePedido ReservaActual = new MensajePedido();
                string Productos = string.Empty;
                string Pagos = string.Empty;

                try
                {
                    Reserva Reserva = db.Set<Reserva>().Include("Detalles").Include("Detalles.Producto").Include("Detalles.Unidad").Include("Pagos").AsNoTracking().Where(x => x.ReservaId == reservaId).FirstOrDefault();
                    if (Reserva != null)
                    {
                        if (Reserva.Operado)
                        {
                            ReservaActual.MensajeId = -2;
                            ReservaActual.Mensaje = "La reserva ya se encuentra operada";
                        }
                        else
                        {
                            if (Reserva.Detalles != null && Reserva.Detalles.Count() > 0)
                            {
                                bool ReservaIncompleta = false;

                                foreach (var Producto in Reserva.Detalles)
                                {
                                    //Se obtiene el producto para convercion
                                    Producto ProductoPadreActual = new Producto();
                                    Producto ProductoHijoActual = new Producto();

                                    bool UnidadPadre = false;
                                    decimal Cantidad = Producto.Cantidad;
                                    decimal CantidadOriginal = 0;

                                    ProductoPadreActual = db.Set<Producto>().Where(x => x.ProductoId == Producto.ProductoId).FirstOrDefault();

                                    if (ProductoPadreActual != null)
                                    {
                                        if (ProductoPadreActual.UnidadId == Producto.UnidadId)
                                        {
                                            UnidadPadre = true;
                                            CantidadOriginal = ProductoPadreActual.Cantidad;
                                        }
                                    }

                                    if (!UnidadPadre)
                                    {
                                        ProductoHijoActual = db.Set<Producto>().Where(x => x.ProductoPadreId == Producto.ProductoId && x.UnidadId == Producto.UnidadId).FirstOrDefault();

                                        if (ProductoHijoActual != null)
                                        {
                                            Cantidad *= ProductoHijoActual.Cantidad;
                                            CantidadOriginal = ProductoHijoActual.Cantidad;
                                        }
                                    }

                                    ProductoInventario InventarioActual = db.Set<ProductoInventario>().Where(x => x.ProductoId == Producto.ProductoId && x.AgenciaId == Reserva.AgenciaId).FirstOrDefault();
                                    if (InventarioActual != null)
                                    {
                                        if (InventarioActual.Cantidad >= Cantidad)
                                        {
                                            Productos += string.Format("<tr><td>{0}</td><td>{1}</td><td>{2}</td><td></td><td>{3}</td><td>{4}%</td><td>{5}</td><td>{6}</td></tr>", Producto.Producto.Nombre, Producto.Unidad.Nombre, Producto.Cantidad, Producto.Precio.ToString("C"), 0, (Producto.Cantidad * Producto.Precio).ToString("C"), "<input type='hidden' name='productoIds' value='" + Producto.ProductoId + "' /><input type='hidden' name='nombreProductoIds' value='" + Producto.Producto.Nombre + "' /><input type='hidden' name='presentacionIds' value='" + Producto.UnidadId + "' /><input type='hidden' name='nombrePresentacionIds' value='" + Producto.Unidad.Nombre + "' /><input type='hidden' name='existenciaIds' value='" + Cantidad + "' /><input type='hidden' name='cantidadIds' value='" + Producto.Cantidad + "' /><input type='hidden' name='precioIds' value='" + Producto.Precio + "' /><input type='hidden' name='descuentoIds' value='0' /><input type='hidden' name='idIds' value='' />");
                                        }
                                        else
                                        {
                                            ReservaIncompleta = true;
                                            break;
                                        }
                                    }
                                }

                                if (Reserva.Pagos != null && Reserva.Pagos.Count() > 0)
                                {
                                    decimal TotalReserva = Reserva.Pagos.Sum(y => y.Valor);

                                    Pagos += string.Format("<tr><td>{0}</td><td>Q{1}</td><td>{2}</td><td>{3}</td></tr>", "Reserva", TotalReserva, "", "<input type='hidden' name='formaIds' value='20190128002' /><input type='hidden' name='pagarIds' value='" + TotalReserva + "' /><input type='hidden' name='notaIds' value='' />");                                    
                                }

                                if (ReservaIncompleta)
                                {
                                    ReservaActual.MensajeId = -4;
                                    ReservaActual.Mensaje = "La reserva no contiene suficientes existencias para ser despachada";
                                }
                                else
                                {
                                    ReservaActual.MensajeId = -5;
                                    ReservaActual.Mensaje = Productos;
                                    ReservaActual.Pago = Pagos;
                                    ReservaActual.Pendiente = Reserva.Detalles.Sum(y => y.Cantidad * y.Precio) - Reserva.Pagos.Sum(y => y.Valor);
                                }                               
                            }
                            else
                            {
                                ReservaActual.MensajeId = -3;
                                ReservaActual.Mensaje = "La reserva no contiene productos";
                            }
                        }
                    }
                    else
                    {
                        ReservaActual.MensajeId = -1;
                        ReservaActual.Mensaje = "La reserva no se encuentra registrada en el sistema";
                    }
                }
                catch (Exception)
                {
                }

                return ReservaActual;
            }
        #endregion
    }
}
