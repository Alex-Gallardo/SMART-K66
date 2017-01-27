using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.BLL
{
    public class ClienteBL
    {

        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public ClienteBL()
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
                    Cliente ClienteActual = db.Set<Cliente>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (ClienteActual != null)
                    {
                        Inicial_Id = ClienteActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;
                }
                catch (Exception)
                {
                }

                return Id;
            }

            private long Agregar(Cliente entidad)
            {
                long ClienteId = -1;

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngClienteId = new Herramienta().Formato_Correlativo(Id);

                        if (lngClienteId > 0)
                        {
                            entidad.ClienteId = lngClienteId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            db.Set<Cliente>().Add(entidad);
                            db.SaveChanges();

                            ClienteId = entidad.ClienteId;
                        }
                    }

                }
                catch (Exception)
                {
                    ClienteId = -1;
                }

                return ClienteId;
            }

            private bool Actualizar(Cliente entidad)
            {
                bool ClienteActualizar = false;

                try
                {

                    Cliente ClienteActual = ObtenerPorId(entidad.ClienteId, false);

                    if (ClienteActual.ClienteId > 0)
                    {
                        ClienteActual.Nit = entidad.Nit;
                        ClienteActual.Nombre = entidad.Nombre;
                        ClienteActual.Direccion = entidad.Direccion;
                        ClienteActual.DPI = entidad.DPI;
                        ClienteActual.NoTelefono = entidad.NoTelefono;
                        ClienteActual.EmailCliente = entidad.EmailCliente;
                        ClienteActual.Vip = entidad.Vip;
                        ClienteActual.Activo = entidad.Activo;
                        ClienteActual.Descuento = entidad.Descuento;

                        db.SaveChanges();
                        ClienteActualizar = true;
                    }

                }
                catch (Exception)
                {
                }

                return ClienteActualizar;
            }

        #endregion

        #region Metodos Publicos

            public string Guardar(Cliente entidad)
            {
                string Mensaje = "OK";
                bool OperacionExitosa = false;

                if (!string.IsNullOrWhiteSpace(entidad.EmailCliente))
                {
                    if (!new Herramienta().ValidarEmail(entidad.EmailCliente))
                    {
                        return "El correo electrónico ingresado no es valido";
                    }
                }

                if (entidad.ClienteId > 0)
                {
                    OperacionExitosa = Actualizar(entidad);
                }
                else
                {
                    OperacionExitosa = Agregar(entidad) > 0 ;
                }

                if (!OperacionExitosa)
                {
                    Mensaje = "La información ingresada no es valida";
                }

                return Mensaje;
            }

            public long GuardarML(Cliente entidad) 
            {
                return Agregar(entidad);
            }

            public Cliente ObtenerPorId(long id, bool todo)
            {
                Cliente ClienteActual = new Cliente();

                try
                {
                    if (todo)
                    {
                        ClienteActual = db.Set<Cliente>().Where(x => x.ClienteId == id).FirstOrDefault();

                        //if (ClienteActual != null && ClienteActual.ClienteId > 0)
                        //{
                        //    ClienteActual.ReparacionHistorial = new List<ReparacionHistorial>();
                        //    ClienteActual.ReparacionHistorial = db.Set<Reparacion>().Include("Estado").Where(x => x.ClienteId == ClienteActual.ClienteId).AsEnumerable().Select(x => new ReparacionHistorial() { ReparacionId = x.ReparacionId, Marca = x.Marca, Falla = x.Falla, IMEI = x.IMEI, Descripcion = x.Descripcion, Garantia = x.Garantia, Fecha = x.Fecha, FechaEntrega = x.FechaEntrega, CostoServicio = x.CostoServicio, Estado = x.Estado.Nombre }).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ReparacionId).Take(10).ToList();

                        //    ClienteActual.MovimientoHistorial = new List<MovimientoHistorial>();
                        //    ClienteActual.MovimientoHistorial = db.Set<Movimiento>().Where(x => x.MovimientoTipoId == 2 && x.ClienteId == ClienteActual.ClienteId).Join(db.Set<MovimientoFormaPago>(), M => M.MovimientoId, MF => MF.MovimientoId, (M, MF) => new MovimientoHistorial() { MovimientoId = M.MovimientoId, Descripcion = M.Descripcion, Fecha = M.Fecha, Precio = MF.Valor }).GroupBy(m => new { m.MovimientoId, m.Descripcion, m.Fecha }).Select(g => new { g.Key, Total = g.Sum(x => x.Precio) }).AsEnumerable().Select(x => new MovimientoHistorial() { MovimientoId = x.Key.MovimientoId, Descripcion = x.Key.Descripcion, Fecha = x.Key.Fecha, Precio = x.Total }).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.MovimientoId).Take(10).ToList();

                        //    ClienteActual.CreditoHistorial = new List<CreditoHistorial>();
                        //    ClienteActual.CreditoHistorial = db.Set<Credito>().Include("Tipo").Include("Agencia").Include("Detalles").Include("Pagos").Where(x => x.ClienteId == ClienteActual.ClienteId).AsEnumerable().Select(x => new CreditoHistorial() { CreditoId = x.CreditoId, Tipo = x.Tipo.Nombre, Agencia = x.Agencia.Nombre, Descripcion = x.Descripcion, FechaInicial = x.FechaInicial, FechaFinal = x.FechaFinal, Fecha = x.Fecha, Finalizado = x.Finalizado, MontoCredito = x.Detalles.Sum(d => d.Cantidad * d.Precio), MontoCancelado = x.Pagos.Count() == 0 ? 0 : x.Pagos.Sum(p => p.Valor) }).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.CreditoId).Take(10).ToList();
                        //}
                    }
                    else
                    {
                        ClienteActual = db.Set<Cliente>().Where(x => x.ClienteId == id).FirstOrDefault();
                    }
                }
                catch (Exception)
                {
                }

                return ClienteActual;
            }

            public List<Cliente> ObtenerListado(bool todos, bool formato)
            {
                List<Cliente> Clientes = new List<Cliente>();

                try
                {
                    if (todos)
                    {
                        Clientes = db.Set<Cliente>().OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ClienteId).ToList();
                    }
                    else
                    {
                        if (formato)
                        {
                            Clientes = db.Set<Cliente>().Where(x => x.Activo == true).AsEnumerable().Select(x => new Cliente() { ClienteId = x.ClienteId, Nombre = x.Nombre }).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ClienteId).ToList();
                        }
                        else
                        {
                            Clientes = db.Set<Cliente>().Where(x => x.Activo == true).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ClienteId).ToList();
                        }
                    }
                }
                catch (Exception)
                {
                }

                return Clientes;
            }

            public List<Cliente> Buscar(string search)
            {
                List<Cliente> Clientes = new List<Cliente>();

                try
                {
                    Clientes = db.Set<Cliente>().Where(x => x.Nit.Contains(search) || x.Nombre.Contains(search) || x.Direccion.Contains(search) || x.NoTelefono.Contains(search) || x.EmailCliente.Contains(search)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ClienteId).ToList();
                }
                catch (Exception)
                {
                }

                return Clientes;
            }

            public int ObtenerDescuentoPorId(long id)
            {
                int Descuento = 0;

                try
                {
                    Cliente ClienteActual = db.Set<Cliente>().Where(x => x.ClienteId == id).FirstOrDefault();
                    if (ClienteActual != null)
                    {
                        Descuento = ClienteActual.Descuento;
                    }
                }
                catch (Exception)
                {
                }

                return Descuento;
            }

        #endregion

    }
}
