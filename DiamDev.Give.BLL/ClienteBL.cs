using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace DiamDev.Give.BLL
{
    public class ClienteBL
    {
        #region Variables Globales

            private GiveContext db;
            private VFINContext dbK66;

        #endregion

        string URL_SAP;

        #region Constructores

        public ClienteBL()
            {
                this.db = new GiveContext();
                this.dbK66 = new VFINContext();
                this.URL_SAP = ConfigurationManager.AppSettings["URL_SAP"].ToString();
        }

        #endregion

        #region Metodos Privados

            private int Correlativo()
            {
                int Id = 0;

                try
                {
                    Cliente ClienteActual = db.Set<Cliente>().AsNoTracking().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (ClienteActual != null)
                    {
                        Inicial_Id = ClienteActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;
                }
                catch (Exception)
                {}

                return Id;
            }

            private long Agregar(Cliente entidad)
            {
                long ClienteId = -1;

                if (!string.IsNullOrWhiteSpace(entidad.NoTelefono))
                {
                    entidad.NoTelefono = entidad.NoTelefono.Replace("-", "");
                }                

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
                            entidad.LimiteCredito = 1000;
                     
                            entidad.Fecha = DateTime.Today;

                            if (entidad.Imagenes != null && entidad.Imagenes.Count() > 0)
                            {
                                int imagenId = 1;
                                foreach (var Imagen in entidad.Imagenes)
                                {
                                    Imagen.FotografiaId = imagenId;
                                    Imagen.ClienteId = entidad.ClienteId;
                                    imagenId++;
                                }
                            }

                            DireccionCliente nueva = new DireccionCliente();
                            nueva.ClienteId = lngClienteId;
                            nueva.Direccion = entidad.Direccion;
                            nueva.Longitud = 1;
                            nueva.LocalidadId = 1;
                      
                            db.Set<Cliente>().Add(entidad);
                            db.SaveChanges();

                            ClienteId = entidad.ClienteId;
                            db.Set<DireccionCliente>().Add(nueva);
                            db.SaveChanges();
                        }
                    }
                }
                catch (Exception)
                {
                    ClienteId = -1;
                }

                return ClienteId;
            }

        public long AgregarApp(Cliente entidad,long localidad)
        {
            long ClienteId = -1;
            entidad.NoTelefono = entidad.NoTelefono.Replace("-", "");
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
                        entidad.LimiteCredito = 1000;
                        entidad.Fecha = DateTime.Today;

                        if (entidad.Imagenes != null && entidad.Imagenes.Count() > 0)
                        {
                            int imagenId = 1;
                            foreach (var Imagen in entidad.Imagenes)
                            {
                                Imagen.FotografiaId = imagenId;
                                Imagen.ClienteId = entidad.ClienteId;
                                imagenId++;
                            }
                        }
                        DireccionCliente nueva = new DireccionCliente();
                        nueva.ClienteId = lngClienteId;
                        nueva.Direccion = entidad.Direccion;
                        nueva.LocalidadId = localidad;
                        nueva.Longitud = 1;

                        db.Set<Cliente>().Add(entidad);
                        db.SaveChanges();

                        ClienteId = entidad.ClienteId;
                        db.Set<DireccionCliente>().Add(nueva);
                        db.SaveChanges();
                    }
                }

            }
            catch (Exception)
            {
                ClienteId = -1;
            }

            return ClienteId;
        }

            private string Actualizar(Cliente entidad)
            {
                string Mensaje = "OK";

                try
                {

                    Cliente ClienteActual = ObtenerPorId(entidad.ClienteId, false);

                    if (ClienteActual.ClienteId > 0)
                    {
                        ClienteActual.TipoId = entidad.TipoId;
                        ClienteActual.RegionId = entidad.RegionId;
                        ClienteActual.Nit = entidad.Nit;
                        ClienteActual.Nombre = entidad.Nombre;
                        ClienteActual.Direccion = entidad.Direccion;
                        ClienteActual.DPI = entidad.DPI;
                        ClienteActual.NoTelefono = entidad.NoTelefono;
                        ClienteActual.EmailCliente = entidad.EmailCliente;
                        ClienteActual.Vip = entidad.Vip;
                        ClienteActual.Activo = entidad.Activo;
                        ClienteActual.Descuento = entidad.Descuento;
                        ClienteActual.VendedorId = entidad.VendedorId;
                        ClienteActual.DiasCredito = entidad.DiasCredito;
                        ClienteActual.LimiteCredito = entidad.LimiteCredito;
                        ClienteActual.Pass = entidad.Pass;
                        ClienteActual.Latitud = entidad.Latitud;
                        ClienteActual.Longitud = entidad.Longitud;

                        //DATOS DE CONTACTO
                        ClienteActual.NombreContacto = entidad.NombreContacto;
                        ClienteActual.TelefonoContacto = entidad.TelefonoContacto;
                        ClienteActual.CelularContacto = entidad.CelularContacto;
                        ClienteActual.CorreoContacto = entidad.CorreoContacto;
                        ClienteActual.NotaContacto = entidad.NotaContacto;

                        if (entidad.Imagenes != null && entidad.Imagenes.Count() > 0)
                        {
                            int i = 1;

                            ClienteFotografia DocumentoFinal = db.Set<ClienteFotografia>().Where(x => x.ClienteId == ClienteActual.ClienteId).OrderByDescending(x => x.FotografiaId).FirstOrDefault();
                            if (DocumentoFinal != null)
                            {
                                i = DocumentoFinal.FotografiaId + 1;
                            }

                            foreach (var item in entidad.Imagenes)
                            {
                                item.FotografiaId = i++;
                                item.ClienteId = ClienteActual.ClienteId;

                                db.Set<ClienteFotografia>().Add(item);
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

                 entidad.NoTelefono.Replace("-","");

                if (entidad.ClienteId > 0)
                {
                    Mensaje = Actualizar(entidad);
                    if (Mensaje.Equals("OK"))
                    {
                        OperacionExitosa = true;
                    }
                }
                else
                {
                    OperacionExitosa = Agregar(entidad) > 0;
                }

                if (!OperacionExitosa)
                {
                    Mensaje = "La información ingresada no es valida";
                }

                return Mensaje;
            }

            public long GuardarML(Cliente entidad, long empresaId)
            {
                var cliente = ObtenerPorNit(entidad.Nit, empresaId);
                if (cliente != null) return cliente.ClienteId;
                entidad.EmailCliente = string.IsNullOrWhiteSpace(entidad.EmailCliente) ? "sincorreo@sincorreo.com" : entidad.EmailCliente;
                entidad.RegionId = 20191016001;
                entidad.DiasCredito = 10;
                entidad.DPI = "SINDPI";
                entidad.TipoId = 20190310001;
                entidad.Vip = false;
                entidad.Activo = true;
                entidad.VendedorId = 20180925001;

                return Agregar(entidad);
            }

        public string GuardarContacto(ClienteContacto entidad) 
        {
            string Mensaje = "OK";

            try
            {
                int ContactoId = 1;

                ClienteContacto UltimoContacto = db.Set<ClienteContacto>().AsNoTracking().Where(x => x.ClienteId == entidad.ClienteId).OrderByDescending(x => x.ContactoId).FirstOrDefault();
                if (UltimoContacto != null)
                {
                    ContactoId = UltimoContacto.ContactoId + 1;
                }

                entidad.ContactoId = ContactoId;
                db.Set<ClienteContacto>().Add(entidad);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                Mensaje = string.Format("Descripción del Error {0}", ex.Message);
            }

            return Mensaje;
        }


        private long AgregarDireccion(DireccionCliente entidad)
        {
            long Mensaje = 1;
             try
            {
                int Id = Correlativo();
                if (entidad.LocalidadId != null) {
                    Localidad loca = new LocalidadBL().ObtenerPorId(Convert.ToInt64(entidad.LocalidadId));


                    string localidad = loca.Nombre;

                    string municipio = new MunicipioBL().ObtenerPorId(loca.MunicipioId).Nombre;

                    entidad.Direccion = municipio + " " + localidad + " " + entidad.Direccion;

                }
                entidad.Longitud = 1;
                      
                        db.Set<DireccionCliente>().Add(entidad);
                        db.SaveChanges();

                     
               

            }
            catch (Exception)
            {
                Mensaje = -1;
            }

            return Mensaje;
        }


        public List<DireccionCliente> ObtenerDireccionesClientePorId(long id) {

            List<DireccionCliente> dev = db.DireccionesCliente.Where(x => x.ClienteId == id&&x.Longitud==1).ToList();
            foreach (DireccionCliente item in dev) {
                if (item.LocalidadId != null)
                {
                    item.Latitud = new LocalidadBL().ObtenerPorId(Convert.ToInt64(item.LocalidadId)).CostoEnvio;
                }
                else {
                    item.Latitud =10;
                }
            }
            return dev;

        }
        
            
            public DireccionCliente ObtenerDireccionPorId(int dir) {

            DireccionCliente ClienteActual = db.DireccionesCliente.Where(x => x.DireccionId == dir).FirstOrDefault();

            return ClienteActual;


        }

        private string ActualizarDireccion(DireccionCliente entidad)
        {
            string Mensaje = "OK";

            try
            {

                DireccionCliente ClienteActual = ObtenerDireccionPorId(entidad.DireccionId);

                if (ClienteActual.DireccionId > 0)
                {
                    ClienteActual.Direccion = entidad.Direccion;
                    ClienteActual.Longitud = entidad.Longitud;

                    db.SaveChanges();
                }

            }
            catch (Exception ex)
            {
                Mensaje = string.Format("Descripción del Error {0}", ex.Message);
            }

            return Mensaje;
        }


        public string GuardarDireccion(DireccionCliente entidad)
        {
            string Mensaje = "OK";
            bool OperacionExitosa = false;
            //entidad.LocalidadId = 1;
           
            if (entidad.DireccionId > 0)
            {
                Mensaje = ActualizarDireccion(entidad);
                if (Mensaje.Equals("OK"))
                {
                    OperacionExitosa = true;
                }
            }
            else
            {
                OperacionExitosa = AgregarDireccion(entidad) > 0;
            }

            if (!OperacionExitosa)
            {
                Mensaje = "La información ingresada no es valida";
            }
            else {
                Mensaje = "Direccion Actualizada Exitosamente";
            }

            return Mensaje;
        }





        public List<string> ObtenerCobertura(string direccionc) {
            List<string> Alumnos = new List<string>();
            try
            {

                Alumnos = db.Database.SqlQuery<string>("exec BuscarAreaCubre  @direccion",new SqlParameter("direccion",direccionc)).ToList();

             

            }
            catch (Exception)
            {
            }
            //List<AgrupadorPedido> dev = new List<AgrupadorPedido>();
            //foreach (Pedido item in Alumnos) {
            //    AgrupadorPedido n = new AgrupadorPedido();
            //    n.Nombre = item.Descripcion;
            //    n.Cantidad = Convert.ToInt32(item.ClienteId);
            //    dev.Add(n);
            //}
            return Alumnos;
        }
        public DireccionCliente Direccion(int fotografiaId, long clienteId)
        {
            DireccionCliente FotografiaActual = new DireccionCliente();

            try
            {
                FotografiaActual = db.Set<DireccionCliente>().Where(x => x.DireccionId == fotografiaId && x.ClienteId == clienteId).FirstOrDefault();
            }
            catch (Exception)
            {
            }

            return FotografiaActual;
        }

        public bool EliminarDireccion(long clienteId, int fotografiaId)
        {
            bool Eliminar = false;

            try
            {
                DireccionCliente FotografiaActual = db.Set<DireccionCliente>().Where(x => x.DireccionId == fotografiaId && x.ClienteId == clienteId).FirstOrDefault();
                if (FotografiaActual != null)
                {
                    db.Set<DireccionCliente>().Remove(FotografiaActual);
                    db.SaveChanges();

                    Eliminar = true;
                }
            }
            catch (Exception)
            {
            }

            return Eliminar;
        }


        public Cliente ObtenerPorId(long id, bool todo, bool imagen = false, bool pagos = false)
            {
                Cliente ClienteActual = new Cliente();

                try
                {
                    if (todo)
                    {
                        if (imagen)
                        {
                            ClienteActual = db.Set<Cliente>().Include("Region").Include("Imagenes").Include("Direcciones").Include("Contactos").Include("Contactos.Departamento").AsNoTracking().Where(x => x.ClienteId == id).FirstOrDefault();
                            if (pagos)
                            {
                                if (ClienteActual != null)
                                {
                                    ClienteActual.Facturas = new List<Factura>();
                                    ClienteActual.Facturas = db.Set<Factura>().Include("Tipo").Include("Serie").Include("Agencia").Include("Detalles").Where(x => x.ClienteId == ClienteActual.ClienteId && !x.Anulada).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.FacturaId).Take(15).ToList();

                                    ClienteActual.Recibos = new List<Recibo>();
                                    ClienteActual.Recibos = db.Set<Recibo>().Include("Tipo").Include("Agencia").Include("Detalles").Where(x => x.ClienteId == ClienteActual.ClienteId && !x.Anulada).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ReciboId).Take(15).ToList();

                                    ClienteActual.Credito = 0;
                                    List<long> ReciboIDs = db.Set<Recibo>().AsNoTracking().Where(y => y.ClienteId == ClienteActual.ClienteId && !y.Anulada && !y.Pagada).Select(y => y.ReciboId).ToList();
                                    if (ReciboIDs != null && ReciboIDs.Count() > 0)
                                    {
                                        ClienteActual.Credito = db.Set<ReciboDetalle>().AsNoTracking().Where(y => ReciboIDs.Contains(y.ReciboId)).Sum(y => y.Cantidad * y.Precio);
                                    }
                                }                            
                            }
                        }
                        else
                        {
                            ClienteActual = db.Set<Cliente>().AsNoTracking().Where(x => x.ClienteId == id).FirstOrDefault();
                        }
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

        public Cliente ObtenerClientePorNumero(string telefono, bool todo, bool imagen = false, bool pagos = false)
        {
            Cliente ClienteActual = new Cliente();

            try
            {
                if (todo)
                {
                    if (imagen)
                    {
                        ClienteActual = db.Set<Cliente>().Include("Region").Include("Imagenes").Include("Direcciones").AsNoTracking().Where(x => x.NoTelefono == telefono).FirstOrDefault();
                        if (pagos)
                        {
                            if (ClienteActual != null)
                            {
                                ClienteActual.Facturas = new List<Factura>();
                                ClienteActual.Facturas = db.Set<Factura>().Include("Tipo").Include("Serie").Include("Agencia").Include("Detalles").Where(x => x.ClienteId == ClienteActual.ClienteId && !x.Anulada).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.FacturaId).Take(15).ToList();

                                ClienteActual.Recibos = new List<Recibo>();
                                ClienteActual.Recibos = db.Set<Recibo>().Include("Tipo").Include("Agencia").Include("Detalles").Where(x => x.ClienteId == ClienteActual.ClienteId && !x.Anulada).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ReciboId).Take(15).ToList();

                                ClienteActual.Credito = 0;
                                List<long> ReciboIDs = db.Set<Recibo>().AsNoTracking().Where(y => y.ClienteId == ClienteActual.ClienteId && !y.Anulada && !y.Pagada).Select(y => y.ReciboId).ToList();
                                if (ReciboIDs != null && ReciboIDs.Count() > 0)
                                {
                                    ClienteActual.Credito = db.Set<ReciboDetalle>().AsNoTracking().Where(y => ReciboIDs.Contains(y.ReciboId)).Sum(y => y.Cantidad * y.Precio);
                                }
                            }
                        }
                    }
                    else
                    {
                        ClienteActual = db.Set<Cliente>().AsNoTracking().Where(x => x.NoTelefono == telefono).FirstOrDefault();
                    }
                }
                else
                {
                    ClienteActual = db.Set<Cliente>().AsNoTracking().Where(x => x.NoTelefono == telefono).FirstOrDefault();
                }
            }
            catch (Exception)
            {
            }

            return ClienteActual;
        }

        public string EliminarDireccionPorId(int direccionid) {

            string mensaje = "";
            try
            {
                DireccionCliente dir = ObtenerDireccionPorId(direccionid);
                dir.Longitud = 0;
                mensaje=GuardarDireccion(dir);
            }
            catch (Exception) 
            {}

            return mensaje;
        }
        

    
            public Cliente ObtenerPorNit(string nit, long empresaId)
            {
                return db.Clientes.Include("Region").Include("Tipo").Include("Direcciones").FirstOrDefault(x => x.Nit == nit && x.EmpresaId == empresaId);
            }

            public List<Cliente> ObtenerListado(bool todos, bool formato, long empresaId = 0)
            {
                List<Cliente> Clientes = new List<Cliente>();

                try
                {
                    if (todos)
                    {
                        Clientes = db.Set<Cliente>().Include("Region").Include("Vendedor").Include("Contactos").Include("Contactos.Departamento").AsNoTracking().Where(x => x.EmpresaId == empresaId).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ClienteId).Take(200).ToList();
                    }
                    else
                    {
                        if (formato)
                        {
                            Clientes = db.Set<Cliente>().Where(x => x.Activo == true).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ClienteId).AsEnumerable().Select(x => new Cliente() { ClienteId = x.ClienteId, Nombre = x.Nombre }).ToList();
                        }
                        else
                        {
                            Clientes = db.Set<Cliente>().Include("Region").Include("Vendedor").Where(x => x.Activo == true).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ClienteId).ToList();
                        }
                    }

                    if (Clientes != null && Clientes.Count() > 0)
                    {
                        Clientes.ForEach(x => 
                        {
                            x.Credito = 0;
                            List<long> ReciboIDs = db.Set<Recibo>().AsNoTracking().Where(y => y.ClienteId == x.ClienteId && !y.Anulada && !y.Pagada).Select(y => y.ReciboId).ToList();
                            if (ReciboIDs != null && ReciboIDs.Count() > 0)
                            {
                                x.Credito = db.Set<ReciboDetalle>().AsNoTracking().Where(y => ReciboIDs.Contains(y.ReciboId)).Sum(y => y.Cantidad * y.Precio);                            
                            }
                        });                    
                    }
                }
                catch (Exception)
                {}

                return Clientes;
            }

            public List<Cliente> ObtenerListadoxRegionId(long regionId, long empresaId)
            {
                List<Cliente> Clientes = new List<Cliente>();

                try
                {
                    Clientes = db.Set<Cliente>().Include("Region").Include("Vendedor").Include("Contactos").Include("Contactos.Departamento").AsNoTracking().Where(x => x.RegionId == regionId && x.EmpresaId == empresaId).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ClienteId).Take(200).ToList();

                    if (Clientes != null && Clientes.Count() > 0)
                    {
                        Clientes.ForEach(x =>
                        {
                            x.Credito = 0;
                            List<long> ReciboIDs = db.Set<Recibo>().AsNoTracking().Where(y => y.ClienteId == x.ClienteId && !y.Anulada && !y.Pagada).Select(y => y.ReciboId).ToList();
                            if (ReciboIDs != null && ReciboIDs.Count() > 0)
                            {
                                x.Credito = db.Set<ReciboDetalle>().AsNoTracking().Where(y => ReciboIDs.Contains(y.ReciboId)).Sum(y => y.Cantidad * y.Precio);
                            }
                        });
                    }
                }
                catch (Exception)
                {}

                return Clientes;
            }

            public List<Cliente> Buscar(string search, long empresaId)
            {
                List<Cliente> Clientes = new List<Cliente>();

                try
                {
                    Clientes = db.Set<Cliente>().Include("Region").Include("Vendedor").Include("Direcciones").Include("Contactos").Include("Contactos.Departamento").AsNoTracking().Where(x => (x.Nit.Contains(search) || x.Nombre.Contains(search) || x.Direccion.Contains(search) || x.NoTelefono.Contains(search) || x.EmailCliente.Contains(search)) && (x.EmpresaId == empresaId)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ClienteId).Take(200).ToList();

                    if (Clientes != null && Clientes.Count() > 0)
                    {
                        Clientes.ForEach(x =>
                        {
                            x.Credito = 0;
                            List<long> ReciboIDs = db.Set<Recibo>().AsNoTracking().Where(y => y.ClienteId == x.ClienteId && !y.Anulada && !y.Pagada).Select(y => y.ReciboId).ToList();
                            if (ReciboIDs != null && ReciboIDs.Count() > 0)
                            {
                                x.Credito = db.Set<ReciboDetalle>().AsNoTracking().Where(y => ReciboIDs.Contains(y.ReciboId)).Sum(y => y.Cantidad * y.Precio);
                            }
                        });
                    }
                }
                catch (Exception)
                {}

                return Clientes;
            }

            public List<Cliente> BuscarxRegionId(string search, long regionId, long empresaId)
            {
                List<Cliente> Clientes = new List<Cliente>();

                try
                {
                    Clientes = db.Set<Cliente>().Include("Region").Include("Vendedor").Include("Contactos").Include("Contactos.Departamento").AsNoTracking().Where(x => (x.Nit.Contains(search) || x.Nombre.Contains(search) || x.Direccion.Contains(search) || x.NoTelefono.Contains(search) || x.EmailCliente.Contains(search)) && (x.RegionId == regionId) && (x.EmpresaId == empresaId)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ClienteId).Take(200).ToList();

                    if (Clientes != null && Clientes.Count() > 0)
                    {
                        Clientes.ForEach(x =>
                        {
                            x.Credito = 0;
                            List<long> ReciboIDs = db.Set<Recibo>().AsNoTracking().Where(y => y.ClienteId == x.ClienteId && !y.Anulada && !y.Pagada).Select(y => y.ReciboId).ToList();
                            if (ReciboIDs != null && ReciboIDs.Count() > 0)
                            {
                                x.Credito = db.Set<ReciboDetalle>().AsNoTracking().Where(y => ReciboIDs.Contains(y.ReciboId)).Sum(y => y.Cantidad * y.Precio);
                            }
                        });
                    }
                }
                catch (Exception)
                {}

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

            public List<ClienteConsultaModel> BuscarClientexNombre(string search, long empresaId)
            {
                List<ClienteConsultaModel> Clientes = new List<ClienteConsultaModel>();

                try
                {
                    Clientes = db.Database.SqlQuery<ClienteConsultaModel>("dbo.spConsultaClientexNombre @Query, @EmpresaId", new SqlParameter("@Query", search), new SqlParameter("@EmpresaId", empresaId)).ToList();

                    if (Clientes != null && Clientes.Count() > 0)
                    {
                        Clientes = Clientes.OrderBy(x => x.Nombre).ToList();
                    }
                }
                catch (Exception EX)
                {
                string Es = EX.Message;
                }

                return Clientes;
            }

            public List<Cliente> BuscarClientexTextoLibre(string search, long empresaId)
            {
                List<Cliente> Clientes = new List<Cliente>();

                try
                {
                    if (string.IsNullOrWhiteSpace(search))
                    {
                        return new List<Cliente>();
                    }

                    int Inicio = search.LastIndexOf(")") + 1;
                    search = search.Substring(Inicio);

                    int Fin = search.LastIndexOf("|");
                    search = search.Substring(0, Fin);

                    List<ClienteConsultaModel> Consultas = db.Database.SqlQuery<ClienteConsultaModel>("dbo.sp_busqueda_libre_de_cliente @Buscar, @EmpresaId", new SqlParameter("@Buscar", search), new SqlParameter("@EmpresaId", empresaId)).ToList();
                    if (Consultas != null && Consultas.Count() > 0)
                    {
                        Clientes = Consultas.Select(x => new Cliente() { ClienteId = x.ClienteId, Nombre = x.Nombre }).ToList();
                    }
                }
                catch (Exception)
                {}

                return Clientes;
            }

            public bool VerificarNIT(string nit, long empresaId)
            {
                bool Existe = false;

                try
                {
                    Existe = db.Set<Cliente>().AsNoTracking().Where(x => x.Nit.Equals(nit) && x.EmpresaId == empresaId).Count() > 0;
                }
                catch (Exception)
                {}

                return Existe;
            }
        public bool VerificarCelular(string nit)
        {
            bool Existe = false;
            string cel = nit.Replace("-", "");
            try
            {
                Existe = db.Set<Cliente>().AsNoTracking().Where(x => x.NoTelefono.Equals(cel)).Count() > 0;
            }
            catch (Exception)
            {
            }

            return Existe;
        }

        public ClienteFotografia Fotografia(int fotografiaId, long clienteId)
            {
                ClienteFotografia FotografiaActual = new ClienteFotografia();

                try
                {
                    FotografiaActual = db.Set<ClienteFotografia>().Where(x => x.FotografiaId == fotografiaId && x.ClienteId == clienteId).FirstOrDefault();
                }
                catch (Exception)
                {
                }

                return FotografiaActual;
            }

            public bool EliminarFotografia(long clienteId, int fotografiaId)
            {
                bool Eliminar = false;

                try
                {
                    ClienteFotografia FotografiaActual = db.Set<ClienteFotografia>().Where(x => x.FotografiaId == fotografiaId && x.ClienteId == clienteId).FirstOrDefault();
                    if (FotografiaActual != null)
                    {
                        db.Set<ClienteFotografia>().Remove(FotografiaActual);
                        db.SaveChanges();

                        Eliminar = true;
                    }
                }
                catch (Exception)
                {
                }

                return Eliminar;
            }

            public ClienteHistorial ObtenerPorIdHistorial(long id, DateTime fechaInicial, DateTime fechaFinal)
            {
                ClienteHistorial ClienteHistorialActual = new ClienteHistorial();

                try
                {                
                    ClienteHistorialActual.Cliente = db.Set<Cliente>().AsNoTracking().Where(x => x.ClienteId == id).FirstOrDefault();
                    if (ClienteHistorialActual.Cliente != null)
                    {        
                        ClienteHistorialActual.Recibos = new List<Recibo>();
                        ClienteHistorialActual.Recibos = db.Set<Recibo>().Include("Tipo").Include("Agencia").Include("Cliente").Include("Detalles").Where(x => x.ClienteId == id && x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && !x.Anulada).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.ReciboId).ToList();
                    }
                }
                catch (Exception)
                {
                }

                return ClienteHistorialActual;
            }

            public Cliente ObtenerClientexTelefono(string telefono) 
            {
                Cliente ClienteActual = new Cliente();

                try
                {
                    ClienteActual = db.Set<Cliente>().AsNoTracking().Where(x => x.NoTelefono.Equals(telefono)).FirstOrDefault();
                }
                catch (Exception)
                {}

                return ClienteActual;
            }

            //CONSULTAS DE CLIENTES K66
            public List<ClienteConsultaModel> BuscarClientexNombreK66(string buscar, long usuarioId, long empresaId)
            {
                List<ClienteConsultaModel> Clientes = new List<ClienteConsultaModel>();

                try
                {
                    List<UsuarioEmpresa> UsuarioEmpresasActual = db.Set<UsuarioEmpresa>().Include("Empresa").AsNoTracking().Where(x => x.UsuarioId == usuarioId && x.EmpresaId == empresaId).ToList();
                    if (UsuarioEmpresasActual != null && UsuarioEmpresasActual.Count() > 0)
                    {
                        string NombreEmpresa = UsuarioEmpresasActual.Select(x => x.Empresa.Nombre).FirstOrDefault();
                        string Codigo = string.Empty;

                        if (UsuarioEmpresasActual.Count() == 0)
                        {
                            UsuarioEmpresasActual.ForEach(e =>
                            {
                                Codigo = e.Codigo;
                            });
                        }
                        else if (UsuarioEmpresasActual.Count() > 0)
                        {
                            UsuarioEmpresasActual.ForEach(e =>
                            {
                                Codigo += string.Format("{0},", e.Codigo);
                            });
                        }
                    var codigos = UsuarioEmpresasActual
                    .Select(u => u.Codigo) // Reemplaza "Codigo" por el nombre exacto de la propiedad si es diferente
                    .ToList();
                    var soloCodigos = ObtenerCodigos(codigos);
                        // Combinar los códigos en una cadena separada por comas
                        string Codigos = string.Join(",", soloCodigos);
                    Clientes = ObtenerClientesDesdeApi(buscar, NombreEmpresa, Codigos);
                        //dbK66.Database.SqlQuery<ClienteConsultaModel>("dbo.sp_consulta_cliente_texto_libre @Buscar, @Empresa, @Codigo", new SqlParameter("@Buscar", buscar), new SqlParameter("@Empresa", NombreEmpresa), new SqlParameter("@Codigo", Codigo)).ToList();
                    }                             

                    if (Clientes != null && Clientes.Count() > 0)
                    {
                        Clientes = Clientes.OrderBy(x => x.Nombre).ToList();
                    }
                }
                catch (Exception)
                {
                throw;
                // Console.WriteLine($"Excepción: Entre aqui");
                //return new List<ClienteConsultaModel>();
            }

                return Clientes;
            }

        static List<string> ObtenerCodigos(List<string> entradas)
        {
            var resultado = new List<string>();

            foreach (var entrada in entradas)
            {
                // Dividir la cadena en el delimitador '-'
                var partes = entrada.Split('-');

                // Agregar solo la primera parte (el código) a la lista de resultados
                if (partes.Length > 0)
                {
                    resultado.Add(partes[0]);
                }
            }

            return resultado;
        }

        public List<ClienteConsultaModel> ObtenerClientesDesdeApi(string buscar, string NombreEmpresa, string Codigo)
            {
                // Base URL de la API
                string apiUrl = $"{URL_SAP}Client";

                // Crear la consulta con parámetros
                string requestUrl = $"{apiUrl}?CardCode={buscar}&Empresa={NombreEmpresa}&Codigo={Codigo}";

                // Inicializar HttpClient (asegúrate de gestionar adecuadamente la vida útil de HttpClient)
                using (HttpClient client = new HttpClient())
                {
                    try
                    {
                    Console.WriteLine("Entró a la API");
                    System.Diagnostics.Debug.WriteLine("Entró a la API");
                    // Hacer la solicitud GET de manera síncrona
                    HttpResponseMessage response = client.GetAsync(requestUrl).GetAwaiter().GetResult();

                        // Verificar si la respuesta fue exitosa
                        if (response.IsSuccessStatusCode)
                        {
                            // Leer el contenido de la respuesta de manera síncrona
                            string jsonResponse = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                            // Deserializar el JSON a la lista de modelos
                            List<ClienteConsultaModel> clientes = new List<ClienteConsultaModel>();
                            try
                            {
                                clientes = JsonConvert.DeserializeObject<List<ClienteConsultaModel>>(jsonResponse);
                            }
                            catch(Exception ex)
                            {
                                var cliente = JsonConvert.DeserializeObject<ClienteConsultaModel>(jsonResponse);
                                clientes.Add(cliente);
                            }
                            // Retornar los clientes deserializados
                            return clientes;
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
                        return new List<ClienteConsultaModel>();
                    }
                }
            }

        public List<DireccionK66> ObtenerClientesDireccionDesdeApi(string buscar, string NombreEmpresa)
        {
            // Base URL de la API
            string apiUrl = $"{URL_SAP}ClienteDireccion";

            // Crear la consulta con parámetros
            string requestUrl = $"{apiUrl}?CardCode={buscar}&Empresa={NombreEmpresa}";

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
                        List<DireccionK66> clientes = new List<DireccionK66>();
                        try
                        {
                            clientes = JsonConvert.DeserializeObject<List<DireccionK66>>(jsonResponse);
                        }
                        catch(Exception ex)
                        {
                            var cliente = JsonConvert.DeserializeObject<DireccionK66>(jsonResponse);
                            clientes.Add(cliente);
                        }
                        // Retornar los clientes deserializados
                        return clientes;
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
                    return new List<DireccionK66>();
                }
            }
        }

        public List<ClienteConsultaModel> ObtenerClientesDesdeApiNombre(string buscar, string NombreEmpresa, string Codigo)
        {
            // Base URL de la API
            string apiUrl = $"{URL_SAP}ClientName";

            // Crear la consulta con parámetros
            string requestUrl = $"{apiUrl}?CardCode={buscar}&Empresa={NombreEmpresa}&Codigo={Codigo}";

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
                        //ClienteConsultaModelName clientesName = JsonConvert.DeserializeObject<ClienteConsultaModelName>(jsonResponse);
                        //List<ClienteConsultaModel> clientes = new List<ClienteConsultaModel>();
                        //clientes.Add(new ClienteConsultaModel { ClienteId = -1, ID = clientesName.ID, Nombre = clientesName.Nombre });
                        List<ClienteConsultaModel> clientesName = JsonConvert.DeserializeObject<List<ClienteConsultaModel>>(jsonResponse);
                        // Retornar los clientes deserializados
                        return clientesName;
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
                    return new List<ClienteConsultaModel>();
                }
            }
        }

        public ClienteK66 ObtenerClientesDesdeApiId(string buscar, string NombreEmpresa, string Codigo)
        {
            // Base URL de la API
            string apiUrl = $"{URL_SAP}ClientId";

            // Crear la consulta con parámetros
            string requestUrl = $"{apiUrl}?CardCode={buscar}&Empresa={NombreEmpresa}&Codigo={Codigo}";

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
                        List<ClienteK66> clienteK66 = JsonConvert.DeserializeObject < List< ClienteK66>>(jsonResponse);
                        
                        return clienteK66.FirstOrDefault();
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
                    return new ClienteK66();
                }
            }
        }

        public List<ClienteConsultaModel> BuscarClientexNombreVisitaK66(string buscar, long usuarioId, bool bolik, bool empaques, bool faes, bool graco)
            {
                List<ClienteConsultaModel> Clientes = new List<ClienteConsultaModel>();

                long empresaBolik = 20210705001;
                long empresaEmpaques = 20210705002;
                long empresaFaes = 20210705003;
                long empresaGraco = 20210705004;

                try
                {
                    if (bolik)
                    {
                        UsuarioEmpresa UsuarioEmpresaActual = db.Set<UsuarioEmpresa>().Include("Empresa").AsNoTracking().Where(x => x.UsuarioId == usuarioId && x.EmpresaId == empresaBolik).FirstOrDefault();
                        if (UsuarioEmpresaActual != null)
                        {
                            Clientes = dbK66.Database.SqlQuery<ClienteConsultaModel>("dbo.sp_consulta_cliente_texto_libre @Buscar, @Empresa, @Codigo", new SqlParameter("@Buscar", buscar), new SqlParameter("@Empresa", UsuarioEmpresaActual.Empresa.Nombre), new SqlParameter("@Codigo", UsuarioEmpresaActual.Codigo)).ToList();
                        }
                    }

                    if (empaques)
                    {
                        UsuarioEmpresa UsuarioEmpresaActual = db.Set<UsuarioEmpresa>().Include("Empresa").AsNoTracking().Where(x => x.UsuarioId == usuarioId && x.EmpresaId == empresaEmpaques).FirstOrDefault();
                        if (UsuarioEmpresaActual != null)
                        {
                            Clientes.AddRange(dbK66.Database.SqlQuery<ClienteConsultaModel>("dbo.sp_consulta_cliente_texto_libre @Buscar, @Empresa, @Codigo", new SqlParameter("@Buscar", buscar), new SqlParameter("@Empresa", UsuarioEmpresaActual.Empresa.Nombre), new SqlParameter("@Codigo", UsuarioEmpresaActual.Codigo)).ToList());
                        }
                    }

                    if (faes)
                    {
                        UsuarioEmpresa UsuarioEmpresaActual = db.Set<UsuarioEmpresa>().Include("Empresa").AsNoTracking().Where(x => x.UsuarioId == usuarioId && x.EmpresaId == empresaFaes).FirstOrDefault();
                        if (UsuarioEmpresaActual != null)
                        {
                            Clientes.AddRange(dbK66.Database.SqlQuery<ClienteConsultaModel>("dbo.sp_consulta_cliente_texto_libre @Buscar, @Empresa, @Codigo", new SqlParameter("@Buscar", buscar), new SqlParameter("@Empresa", UsuarioEmpresaActual.Empresa.Nombre), new SqlParameter("@Codigo", UsuarioEmpresaActual.Codigo)).ToList());
                        }
                    }

                    if (graco)
                    {
                        UsuarioEmpresa UsuarioEmpresaActual = db.Set<UsuarioEmpresa>().Include("Empresa").AsNoTracking().Where(x => x.UsuarioId == usuarioId && x.EmpresaId == empresaGraco).FirstOrDefault();
                        if (UsuarioEmpresaActual != null)
                        {
                            Clientes.AddRange(dbK66.Database.SqlQuery<ClienteConsultaModel>("dbo.sp_consulta_cliente_texto_libre @Buscar, @Empresa, @Codigo", new SqlParameter("@Buscar", buscar), new SqlParameter("@Empresa", UsuarioEmpresaActual.Empresa.Nombre), new SqlParameter("@Codigo", UsuarioEmpresaActual.Codigo)).ToList());
                        }
                    }

                    if (Clientes != null && Clientes.Count() > 0)
                    {
                        Clientes = Clientes.OrderBy(x => x.Nombre).ToList();
                    }
                }
                catch (Exception)
                { }

                return Clientes;
            }

            public List<ClienteConsultaModel> BuscarClientexTextoLibreK66(string buscar, long usuarioId, long empresaId)
            {
                List<ClienteConsultaModel> Clientes = new List<ClienteConsultaModel>();

                try
                {
                    if (string.IsNullOrWhiteSpace(buscar))
                    {
                        return new List<ClienteConsultaModel>();
                    }

                    int Inicio = buscar.LastIndexOf(")") + 4;
                    buscar = buscar.Substring(Inicio);

                    int Fin = buscar.LastIndexOf("|") + 1;
                    buscar = buscar.Substring(Fin);
                    buscar = buscar.Trim();

                    buscar = buscar.Trim();                   

                    List<UsuarioEmpresa> UsuarioEmpresasActual = db.Set<UsuarioEmpresa>().Include("Empresa").AsNoTracking().Where(x => x.UsuarioId == usuarioId && x.EmpresaId == empresaId).ToList();
                    if (UsuarioEmpresasActual != null && UsuarioEmpresasActual.Count() > 0)
                    {
                        string NombreEmpresa = UsuarioEmpresasActual.Select(x => x.Empresa.Nombre).FirstOrDefault();
                        string Codigo = string.Empty;

                        if (UsuarioEmpresasActual.Count() == 0)
                        {
                            UsuarioEmpresasActual.ForEach(e =>
                            {
                                Codigo = e.Codigo;
                            });
                        }
                        else if (UsuarioEmpresasActual.Count() > 0)
                        {
                            UsuarioEmpresasActual.ForEach(e =>
                            {
                                Codigo += string.Format("{0},", e.Codigo);
                            });
                        }
                    var codigos = UsuarioEmpresasActual
                    .Select(u => u.Codigo) // Reemplaza "Codigo" por el nombre exacto de la propiedad si es diferente
                    .ToList();
                    var soloCodigos = ObtenerCodigos(codigos);
                    // Combinar los códigos en una cadena separada por comas
                    string Codigos = string.Join(",", soloCodigos);
                    //marvin
                    //ObtenerClientesDesdeApiNombre
                    Clientes = ObtenerClientesDesdeApiNombre(buscar, NombreEmpresa, Codigos);
                        //Clientes = dbK66.Database.SqlQuery<ClienteConsultaModel>("dbo.sp_consulta_cliente_x_nombre @Buscar, @Empresa, @Codigo", new SqlParameter("@Buscar", buscar), new SqlParameter("@Empresa", NombreEmpresa), new SqlParameter("@Codigo", Codigo)).ToList();
                    }

                    if (Clientes != null && Clientes.Count() > 0)
                    {
                        Clientes = Clientes.OrderBy(x => x.Nombre).ToList();
                    }
                }
                catch (Exception)
                { }

                return Clientes;
            }

            public List<ClienteConsultaModel> BuscarClienteVisitaxTextoLibreK66(string buscar, long usuarioId, bool bolik, bool empaques, bool faes, bool graco)
            {
                List<ClienteConsultaModel> Clientes = new List<ClienteConsultaModel>();

                long empresaBolik = 20210705001;
                long empresaEmpaques = 20210705002;
                long empresaFaes = 20210705003;
                long empresaGraco = 20210705004;

                try
                {
                    if (string.IsNullOrWhiteSpace(buscar))
                    {
                        return new List<ClienteConsultaModel>();
                    }

                    int Inicio = buscar.LastIndexOf(")") + 4;
                    buscar = buscar.Substring(Inicio);

                    int Fin = buscar.LastIndexOf("|");
                    buscar = buscar.Substring(0, Fin);

                    buscar = buscar.Trim();

                    if (bolik)
                    {
                        UsuarioEmpresa UsuarioEmpresaActual = db.Set<UsuarioEmpresa>().Include("Empresa").AsNoTracking().Where(x => x.UsuarioId == usuarioId && x.EmpresaId == empresaBolik).FirstOrDefault();
                        if (UsuarioEmpresaActual != null)
                        {
                            Clientes = dbK66.Database.SqlQuery<ClienteConsultaModel>("dbo.sp_consulta_cliente_x_nombre @Buscar, @Empresa, @Codigo", new SqlParameter("@Buscar", buscar), new SqlParameter("@Empresa", UsuarioEmpresaActual.Empresa.Nombre), new SqlParameter("@Codigo", UsuarioEmpresaActual.Codigo)).ToList();
                        }
                    }

                    if (empaques)
                    {
                        UsuarioEmpresa UsuarioEmpresaActual = db.Set<UsuarioEmpresa>().Include("Empresa").AsNoTracking().Where(x => x.UsuarioId == usuarioId && x.EmpresaId == empresaEmpaques).FirstOrDefault();
                        if (UsuarioEmpresaActual != null)
                        {
                            Clientes.AddRange(dbK66.Database.SqlQuery<ClienteConsultaModel>("dbo.sp_consulta_cliente_x_nombre @Buscar, @Empresa, @Codigo", new SqlParameter("@Buscar", buscar), new SqlParameter("@Empresa", UsuarioEmpresaActual.Empresa.Nombre), new SqlParameter("@Codigo", UsuarioEmpresaActual.Codigo)).ToList());
                        }
                    }

                    if (faes)
                    {
                        UsuarioEmpresa UsuarioEmpresaActual = db.Set<UsuarioEmpresa>().Include("Empresa").AsNoTracking().Where(x => x.UsuarioId == usuarioId && x.EmpresaId == empresaFaes).FirstOrDefault();
                        if (UsuarioEmpresaActual != null)
                        {
                            Clientes.AddRange(dbK66.Database.SqlQuery<ClienteConsultaModel>("dbo.sp_consulta_cliente_x_nombre @Buscar, @Empresa, @Codigo", new SqlParameter("@Buscar", buscar), new SqlParameter("@Empresa", UsuarioEmpresaActual.Empresa.Nombre), new SqlParameter("@Codigo", UsuarioEmpresaActual.Codigo)).ToList());
                        }
                    }

                    if (graco)
                    {
                        UsuarioEmpresa UsuarioEmpresaActual = db.Set<UsuarioEmpresa>().Include("Empresa").AsNoTracking().Where(x => x.UsuarioId == usuarioId && x.EmpresaId == empresaGraco).FirstOrDefault();
                        if (UsuarioEmpresaActual != null)
                        {
                            Clientes.AddRange(dbK66.Database.SqlQuery<ClienteConsultaModel>("dbo.sp_consulta_cliente_x_nombre @Buscar, @Empresa, @Codigo", new SqlParameter("@Buscar", buscar), new SqlParameter("@Empresa", UsuarioEmpresaActual.Empresa.Nombre), new SqlParameter("@Codigo", UsuarioEmpresaActual.Codigo)).ToList());
                        }
                    }

                    if (Clientes != null && Clientes.Count() > 0)
                    {
                        Clientes = Clientes.OrderBy(x => x.Nombre).ToList();
                    }
                }
                catch (Exception)
                { }

                return Clientes;
            }

            public ClienteK66 ObtenerxIDK66(string id, long empresaId, long usuarioId)
            {
                ClienteK66 ClienteActual = new ClienteK66();

                try
                {
                    Empresa EmpresaActual = db.Set<Empresa>().AsNoTracking().Where(x => x.EmpresaId == empresaId).FirstOrDefault();
                    if (EmpresaActual != null)
                    {//puta
                        List<UsuarioEmpresa> UsuarioEmpresasActual = db.Set<UsuarioEmpresa>().Include("Empresa").AsNoTracking().Where(x => x.UsuarioId == usuarioId && x.EmpresaId == empresaId).ToList();
                        if (UsuarioEmpresasActual != null && UsuarioEmpresasActual.Count() > 0)
                        {
                            string NombreEmpresa = UsuarioEmpresasActual.Select(x => x.Empresa.Nombre).FirstOrDefault();
                        var codigos = UsuarioEmpresasActual
                        .Select(u => u.Codigo) // Reemplaza "Codigo" por el nombre exacto de la propiedad si es diferente
                        .ToList();
                        var soloCodigos = ObtenerCodigos(codigos);
                        // Combinar los códigos en una cadena separada por comas
                        string Codigos = string.Join(",", soloCodigos);
                        ClienteActual = ObtenerClientesDesdeApiId(id, EmpresaActual.Nombre, Codigos);
                        }
                        
                        //dbK66.Database.SqlQuery<ClienteK66>("dbo.sp_obtener_cliente_x_id_empresa @ID, @Empresa", new SqlParameter("@ID", id), new SqlParameter("@Empresa", EmpresaActual.Nombre)).FirstOrDefault();
                    }
                }
                catch (Exception)
                { }

                return ClienteActual;
            }

            public ClienteK66 ObtenerxIDGeneralK66(string id)
            {
                ClienteK66 ClienteActual = new ClienteK66();

                try
                {
                    ClienteActual = dbK66.Database.SqlQuery<ClienteK66>("dbo.sp_obtener_cliente_x_id_general @ID", new SqlParameter("@ID", id)).FirstOrDefault();
                }
                catch (Exception)
                { }

                return ClienteActual;
            }

            public List<DireccionK66> ObtenerDireccionxCliente(string clienteId, long empresaId)
            {
                List<DireccionK66> Direcciones = new List<DireccionK66>();

                try
                {                    
                    if (empresaId == 20210705001)
                    {
                        Direcciones = ObtenerClientesDireccionDesdeApi(clienteId, "BOLIK");
                    }
                    else if (empresaId == 20210705002)
                    {
                        Direcciones = ObtenerClientesDireccionDesdeApi(clienteId, "EMPAQUES");
                    }
                    else if (empresaId == 20210705003)
                    {
                        Direcciones = ObtenerClientesDireccionDesdeApi(clienteId, "ESCOCESA");
                    }
                    else if (empresaId == 20210705004)
                    {
                        Direcciones = ObtenerClientesDireccionDesdeApi(clienteId, "GRACO");
                    }
                }
                catch (Exception)
                { }

                return Direcciones;
            }

        #endregion
    }
}
