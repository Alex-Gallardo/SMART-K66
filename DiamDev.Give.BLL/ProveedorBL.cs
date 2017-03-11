using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
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

            private bool Agregar(Proveedor entidad)
            {
                bool ProveedorAgregar = false;

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

                            db.Set<Proveedor>().Add(entidad);
                            db.SaveChanges();
                            ProveedorAgregar = true;
                        }
                    }

                }
                catch (Exception)
                {
                }

                return ProveedorAgregar;
            }

            private bool Actualizar(Proveedor entidad)
            {
                bool ProveedorActualizar = false;

                try
                {

                    Proveedor ProveedorActual = ObtenerPorId(entidad.ProveedorId, false);

                    if (ProveedorActual.ProveedorId > 0)
                    {
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

                        if (entidad.Productos != null && entidad.Productos.Count() > 0)
                        {
                            var Productos = db.Set<ProveedorProducto>().Where(x => x.ProveedorId == entidad.ProveedorId);
                            db.Set<ProveedorProducto>().RemoveRange(Productos);

                            foreach (var Producto in entidad.Productos)
                            {
                                db.Set<ProveedorProducto>().Add(Producto);
                            }
                        }

                        db.SaveChanges();
                        ProveedorActualizar = true;
                    }

                }
                catch (Exception)
                {
                }

                return ProveedorActualizar;
            }

        #endregion

        #region Metodos Publicos

            public string Guardar(Proveedor entidad)
            {
                string Mensaje = "OK";
                bool OperacionExitosa = false;

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

            public Proveedor ObtenerPorId(long id, bool todo)
            {
                Proveedor ProveedorActual = new Proveedor();

                try
                {
                    if (todo)
                    {
                        ProveedorActual = db.Set<Proveedor>().Include("Productos").Include("Productos.Producto").Where(x => x.ProveedorId == id).FirstOrDefault();
                        if (ProveedorActual != null && ProveedorActual.ProveedorId > 0)
                        {
                            ProveedorActual.IngresoHistorial = new List<MovimientoHistorial>();
                            ProveedorActual.IngresoHistorial = db.Set<Movimiento>().Where(x => x.MovimientoTipoId == 1 && x.ProveedorId == ProveedorActual.ProveedorId).Join(db.Set<MovimientoDetalle>(), M => M.MovimientoId, MD => MD.MovimientoId, (M, MD) => new MovimientoHistorial() { MovimientoId = M.MovimientoId, Descripcion = M.Descripcion, Fecha = M.Fecha, Precio = MD.Cantidad * MD.Precio }).GroupBy(m => new { m.MovimientoId, m.Descripcion, m.Fecha }).Select(g => new { g.Key, Total = g.Sum(x => x.Precio) }).AsEnumerable().Select(x => new MovimientoHistorial() { MovimientoId = x.Key.MovimientoId, Descripcion = x.Key.Descripcion, Fecha = x.Key.Fecha, Precio = x.Total }).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.MovimientoId).Take(10).ToList();

                            //ProveedorActual.GarantiaHistorial = new List<MovimientoHistorial>();
                            //ProveedorActual.GarantiaHistorial = db.Set<Movimiento>().Where(x => x.MovimientoTipoId == 3 && x.ProveedorId == ProveedorActual.ProveedorId).Join(db.Set<MovimientoDetalle>(), M => M.MovimientoId, MD => MD.MovimientoId, (M, MD) => new MovimientoHistorial() { MovimientoId = M.MovimientoId, Descripcion = M.Descripcion, Fecha = M.Fecha, Precio = MD.Cantidad * MD.Precio }).GroupBy(m => new { m.MovimientoId, m.Descripcion, m.Fecha }).Select(g => new { g.Key, Total = g.Sum(x => x.Precio) }).AsEnumerable().Select(x => new MovimientoHistorial() { MovimientoId = x.Key.MovimientoId, Descripcion = x.Key.Descripcion, Fecha = x.Key.Fecha, Precio = x.Total }).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.MovimientoId).Take(10).ToList();
                        }
                    }
                    else
                    {
                        ProveedorActual = db.Set<Proveedor>().Include("Productos").Where(x => x.ProveedorId == id).FirstOrDefault();
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
                        Proveedores = db.Set<Proveedor>().Include("Productos").OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ProveedorId).ToList();
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

            public List<Proveedor> Buscar(string search)
            {
                List<Proveedor> Proveedores = new List<Proveedor>();

                try
                {
                    Proveedores = db.Set<Proveedor>().Include("Productos").Where(x => x.Nit.Contains(search) || x.Nombre.Contains(search) || x.Direccion.Contains(search) || x.NoTelefonoOficina.Contains(search) || x.EmailProveedor.Contains(search) || x.Contacto.Contains(search) || x.NoTelefonoContacto.Contains(search) || x.EmailContacto.Contains(search)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ProveedorId).ToList();
                }
                catch (Exception)
                {
                }

                return Proveedores;
            }

        #endregion

    }
}
