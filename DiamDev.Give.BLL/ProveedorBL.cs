using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class ProveedorBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public ProveedorBL()
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
                    Proveedor ProveedorActual = db.Set<Proveedor>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (ProveedorActual != null)
                    {
                        Inicial_Id = ProveedorActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;
                }
                catch (Exception)
                {
                }

                return Id;
            }

            private string Agregar(Proveedor entidad)
            {
                string Mensaje = "OK";

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngProveedorId = new Herramienta().Formato_Correlativo(Id);

                        if (lngProveedorId > 0)
                        {
                            entidad.ProveedorId = lngProveedorId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            if (entidad.Cuentas != null && entidad.Cuentas.Count() > 0)
                            {
                                int DetalleId = 1;
                                foreach (var Cuenta in entidad.Cuentas)
                                {
                                    Cuenta.ProveedorId = entidad.ProveedorId;
                                    Cuenta.DetalleId = DetalleId;
                                    DetalleId++;
                                }
                            }

                            if (entidad.Productos != null && entidad.Productos.Count() > 0)
                            {
                                foreach (var Producto in entidad.Productos)
                                {
                                    Producto.ProveedorId = entidad.ProveedorId;
                                }
                            }

                            db.Set<Proveedor>().Add(entidad);
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

            private string Actualizar(Proveedor entidad)
            {
                string Mensaje = "OK";

                try
                {

                    Proveedor ProveedorActual = ObtenerPorId(entidad.ProveedorId, false);

                    if (ProveedorActual.ProveedorId > 0)
                    {
                        ProveedorActual.TipoId = entidad.TipoId;
                        ProveedorActual.Nit = entidad.Nit;
                        ProveedorActual.Nombre = entidad.Nombre;
                        ProveedorActual.NombreCheque = entidad.NombreCheque;
                        ProveedorActual.Direccion = entidad.Direccion;
                        ProveedorActual.NoTelefonoOficina = entidad.NoTelefonoOficina;
                        ProveedorActual.Patente = entidad.Patente;
                        ProveedorActual.EmailProveedor = entidad.EmailProveedor;
                        ProveedorActual.Contacto = entidad.Contacto;
                        ProveedorActual.NoTelefonoContacto = entidad.NoTelefonoContacto;
                        ProveedorActual.EmailContacto = entidad.EmailContacto;
                        ProveedorActual.Activo = entidad.Activo;

                        if (entidad.Cuentas != null && entidad.Cuentas.Count() > 0)
                        {
                            var Cuentas = db.Set<ProveedorCuentaBancaria>().Where(x => x.ProveedorId == entidad.ProveedorId);
                            db.Set<ProveedorCuentaBancaria>().RemoveRange(Cuentas);

                            int DetalleId = 1;
                            foreach (var Cuenta in entidad.Cuentas)
                            {
                                Cuenta.ProveedorId = entidad.ProveedorId;
                                Cuenta.DetalleId = DetalleId;
                                DetalleId++;

                                db.Set<ProveedorCuentaBancaria>().Add(Cuenta);
                            }
                        }

                        if (entidad.Productos != null && entidad.Productos.Count() > 0)
                        {
                            var Productos = db.Set<ProveedorProducto>().Where(x => x.ProveedorId == entidad.ProveedorId);
                            db.Set<ProveedorProducto>().RemoveRange(Productos);

                            foreach (var Producto in entidad.Productos)
                            {
                                Producto.ProveedorId = entidad.ProveedorId;
                                db.Set<ProveedorProducto>().Add(Producto);
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

        #endregion

        #region Metodos Publicos

            public string Guardar(Proveedor entidad)
            {
                string Mensaje = "OK";

                if (!string.IsNullOrWhiteSpace(entidad.EmailProveedor))
                {
                    if (!new Herramienta().ValidarEmail(entidad.EmailProveedor))
                    {
                        return "El correo electrónico ingresado no es valido";
                    }
                }

                if (!string.IsNullOrWhiteSpace(entidad.EmailContacto))
                {
                    if (!new Herramienta().ValidarEmail(entidad.EmailContacto))
                    {
                        return "El correo electrónico ingresado no es valido";
                    }
                }

                if (entidad.ProveedorId > 0)
                {
                    Mensaje = Actualizar(entidad);
                }
                else
                {
                    Mensaje = Agregar(entidad);
                }

                return Mensaje;
            }

            public Proveedor ObtenerPorId(long id, bool todo)
            {
                Proveedor ProveedorActual = new Proveedor();

                try
                {
                    if (todo)
                    {
                        ProveedorActual = db.Set<Proveedor>().Include("Cuentas").Include("Cuentas.Banco").Include("Productos").Include("Productos.Producto").Where(x => x.ProveedorId == id).FirstOrDefault();
                        if (ProveedorActual != null && ProveedorActual.ProveedorId > 0)
                        {
                            ProveedorActual.Movimientos = new List<ProveedorMovimiento>();
                            ProveedorActual.Movimientos = db.Set<ProveedorMovimiento>().Include("Tipo").AsNoTracking().Where(x => x.ProveedorId == ProveedorActual.ProveedorId).OrderByDescending(x => x.FechaMovimiento).ThenByDescending(x => x.MovimientoId).Take(15).ToList();

                            ProveedorActual.IngresoHistorial = new List<MovimientoHistorial>();
                            ProveedorActual.IngresoHistorial = db.Set<Movimiento>().Where(x => x.MovimientoTipoId == 1 && x.ProveedorId == ProveedorActual.ProveedorId).Join(db.Set<MovimientoDetalle>(), M => M.MovimientoId, MD => MD.MovimientoId, (M, MD) => new MovimientoHistorial() { MovimientoId = M.MovimientoId, Descripcion = M.Descripcion, Fecha = M.Fecha, Precio = MD.Cantidad * MD.Precio }).GroupBy(m => new { m.MovimientoId, m.Descripcion, m.Fecha }).Select(g => new { g.Key, Total = g.Sum(x => x.Precio) }).AsEnumerable().Select(x => new MovimientoHistorial() { MovimientoId = x.Key.MovimientoId, Descripcion = x.Key.Descripcion, Fecha = x.Key.Fecha, Precio = x.Total }).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.MovimientoId).Take(10).ToList();

                            //ProveedorActual.GarantiaHistorial = new List<MovimientoHistorial>();
                            //ProveedorActual.GarantiaHistorial = db.Set<Movimiento>().Where(x => x.MovimientoTipoId == 3 && x.ProveedorId == ProveedorActual.ProveedorId).Join(db.Set<MovimientoDetalle>(), M => M.MovimientoId, MD => MD.MovimientoId, (M, MD) => new MovimientoHistorial() { MovimientoId = M.MovimientoId, Descripcion = M.Descripcion, Fecha = M.Fecha, Precio = MD.Cantidad * MD.Precio }).GroupBy(m => new { m.MovimientoId, m.Descripcion, m.Fecha }).Select(g => new { g.Key, Total = g.Sum(x => x.Precio) }).AsEnumerable().Select(x => new MovimientoHistorial() { MovimientoId = x.Key.MovimientoId, Descripcion = x.Key.Descripcion, Fecha = x.Key.Fecha, Precio = x.Total }).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.MovimientoId).Take(10).ToList();
                        }
                    }
                    else
                    {
                        ProveedorActual = db.Set<Proveedor>().Include("Cuentas").Include("Cuentas.Banco").Include("Productos").Where(x => x.ProveedorId == id).FirstOrDefault();
                    }
                }
                catch (Exception)
                {
                }

                return ProveedorActual;
            }

            public List<Proveedor> ObtenerListado(bool todos)
            {
                List<Proveedor> Proveedores = new List<Proveedor>();

                try
                {
                    if (todos)
                    {
                        Proveedores = db.Set<Proveedor>().Include("Cuentas").Include("Cuentas.Banco").Include("Productos").OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ProveedorId).ToList();
                    }
                    else
                    {
                        Proveedores = db.Set<Proveedor>().Where(x => x.Activo == true).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ProveedorId).ToList();
                    }
                }
                catch (Exception)
                {
                }

                return Proveedores;
            }

            public List<Proveedor> ObtenerListado(long usuarioId)
            {
                List<Proveedor> Proveedores = new List<Proveedor>();

                try
                {
                    bool Autorizacion = db.Set<Usuario>().Where(x => x.UsuarioId == usuarioId).Join(db.Set<UsuarioRol>(), U => U.UsuarioId, UR => UR.UsuarioId, (U, UR) => new { Roles = UR }).Join(db.Set<RolPermiso>(), R => R.Roles.RolId, RP => RP.RolId, (R, RP) => new { Permisos = RP }).Select(x => x.Permisos).Any(x => x.PermisoId.Equals("Control.Proveedor.Importaciones"));
                    if (Autorizacion)
                    {
                        Proveedores = db.Set<Proveedor>().Where(x => x.Activo == true).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ProveedorId).ToList();
                    }
                    else
                    {
                        Proveedores = db.Set<Proveedor>().Where(x => x.Activo == true && x.TipoId == 1).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ProveedorId).ToList();
                    }                    
                }
                catch (Exception)
                {
                }

                return Proveedores;
            }

            public List<Proveedor> Buscar(string search)
            {
                List<Proveedor> Proveedores = new List<Proveedor>();

                try
                {
                    Proveedores = db.Set<Proveedor>().Include("Cuentas").Include("Cuentas.Banco").Include("Productos").Where(x => x.Nit.Contains(search) || x.Nombre.Contains(search) || x.Direccion.Contains(search) || x.NoTelefonoOficina.Contains(search) || x.EmailProveedor.Contains(search) || x.Contacto.Contains(search) || x.NoTelefonoContacto.Contains(search) || x.EmailContacto.Contains(search)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ProveedorId).ToList();
                }
                catch (Exception)
                {
                }

                return Proveedores;
            }

            public List<ReporteProveedorProducto> ReporteProveedorProducto(long proveedorId, DateTime fechaInicial, DateTime fechaFinal)
            {
                List<ReporteProveedorProducto> Productos = new List<ReporteProveedorProducto>();

                try
                {
                    if (proveedorId == 0)
                    {
                        Productos = db.Database.SqlQuery<ReporteProveedorProducto>("dbo.sp_reporte_proveedor_producto @ProveedorId, @FechaInicial, @FechaFinal", new SqlParameter("@ProveedorId", DBNull.Value), new SqlParameter("@FechaInicial", fechaInicial), new SqlParameter("@FechaFinal", fechaFinal)).ToList();
                    }
                    else if (proveedorId != 0)
                    {
                        Productos = db.Database.SqlQuery<ReporteProveedorProducto>("dbo.sp_reporte_proveedor_producto @ProveedorId, @FechaInicial, @FechaFinal", new SqlParameter("@ProveedorId", proveedorId), new SqlParameter("@FechaInicial", fechaInicial), new SqlParameter("@FechaFinal", fechaFinal)).ToList();
                    }
                }
                catch (Exception)
                {
                }

                return Productos;
            }

        #endregion
    }
}
