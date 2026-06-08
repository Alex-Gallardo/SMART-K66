using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using Newtonsoft.Json;
using Sistema.Seguridad;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;

namespace DiamDev.Give.BLL
{
    public class UsuarioBL
    {
        #region Variables Globales

        private GiveContext db;
        private string URL_SAP;

        #endregion

        #region Constructores

        public UsuarioBL()
        {
            this.db = new GiveContext();
            this.URL_SAP = ConfigurationManager.AppSettings["URL_SAP"].ToString();
        }

        #endregion

        #region Metodos Privados

        private string Key(string key)
        {
            return Criptografia.Base64StringAHexString(Criptografia.EncriptarSha512(key));
        }

        private string Concat_Usuario(string Usuario, string Password)
        {
            return string.Concat(Usuario, Password, Usuario);
        }

        private int Correlativo()
        {
            int Id = 0;

            try
            {
                Usuario UsuarioActual = db.Set<Usuario>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                int Inicial_Id = 1;

                if (UsuarioActual != null)
                {
                    Inicial_Id = UsuarioActual.Correlativo + 1;
                }

                Id = Inicial_Id;
            }
            catch (Exception)
            {}

            return Id;
        }

        private bool ExisteLogin(string Login)
        {
            bool ExisteLogin = false;

            try
            {
                ExisteLogin = db.Set<Usuario>().Where(x => x.Login.Equals(Login)).Count() > 0;
            }
            catch (Exception)
            {
            }

            return ExisteLogin;
        }

        private string Agregar(Usuario entidad, bool correo)
        {
            string Mensaje = "OK";

            try
            {
                int Id = Correlativo();

                if (Id > 0)
                {
                    string concat = Concat_Usuario(entidad.Login, entidad.Password);
                    long lngUsuarioId = new Herramienta().Formato_Correlativo(Id);

                    if (lngUsuarioId > 0)
                    {
                        entidad.UsuarioId = lngUsuarioId;
                        entidad.Correlativo = Id;
                        entidad.Fecha = DateTime.Today;
                        entidad.NuevoPassword = entidad.Password;
                        entidad.Password = Key(concat);
                        entidad.PasswordAndroid = Herramienta.Key_Android(concat);

                        if (entidad.Roles != null && entidad.Roles.Count() > 0)
                        {
                            foreach (var Rol in entidad.Roles)
                            {
                                Rol.UsuarioId = entidad.UsuarioId;
                            }
                        }

                        if (entidad.Agencias != null && entidad.Agencias.Count() > 0)
                        {
                            foreach (var Centro in entidad.Agencias)
                            {
                                Centro.UsuarioId = entidad.UsuarioId;
                            }
                        }

                        if (entidad.AgenciaConsultas != null && entidad.AgenciaConsultas.Count() > 0)
                        {
                            foreach (var Centro in entidad.AgenciaConsultas)
                            {
                                Centro.UsuarioId = entidad.UsuarioId;
                            }
                        }

                        if (entidad.Empresas != null && entidad.Empresas.Count() > 0)
                        {
                            foreach (var Empresa in entidad.Empresas)
                            {
                                Empresa.UsuarioId = entidad.UsuarioId;
                            }
                        }

                        db.Set<Usuario>().Add(entidad);
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

        private string Actualizar(Usuario entidad)
        {
            string Mensaje = "OK";

            try
            {
                Usuario UsuarioActual = ObtenerPorId(entidad.UsuarioId, false);

                if (UsuarioActual.UsuarioId > 0)
                {

                    string concat = Concat_Usuario(entidad.Login, entidad.Password);

                    UsuarioActual.DepartamentoId = entidad.DepartamentoId;                    
                    UsuarioActual.VendedorId = entidad.VendedorId;
                    UsuarioActual.Nombre = entidad.Nombre;
                    UsuarioActual.Password = Key(concat);
                    UsuarioActual.PasswordAndroid = Herramienta.Key_Android(concat);
                    UsuarioActual.AutenticarSite = entidad.AutenticarSite;
                    UsuarioActual.AutenticarAndroid = entidad.AutenticarAndroid;
                    UsuarioActual.ReiniciarPassword = entidad.ReiniciarPassword;
                    UsuarioActual.Activo = entidad.Activo;

                    UsuarioActual.Token = entidad.Token;
                    UsuarioActual.Celular = entidad.Celular;
                    UsuarioActual.serie_sap = entidad.serie_sap;

                    if (UsuarioActual.VendedorId != null)
                    {
                        Vendedor VendedorActual = db.Set<Vendedor>().Where(x => x.VendedorId == UsuarioActual.VendedorId.Value).FirstOrDefault();
                        if (VendedorActual != null)
                        {
                            VendedorActual.Activo = UsuarioActual.Activo;                            
                        }
                    }      

                    if (entidad.Roles != null && entidad.Roles.Count() > 0)
                    {
                        //Eliminar por usuarioId                          
                        var Roles = db.Set<UsuarioRol>().Where(x => x.UsuarioId == UsuarioActual.UsuarioId).ToList();
                        db.Set<UsuarioRol>().RemoveRange(Roles);

                        //Agregar los nuevos roles
                        UsuarioActual.Roles = new List<UsuarioRol>();

                        foreach (var Rol in entidad.Roles)
                        {
                            if (!UsuarioActual.Roles.Any(x => x.RolId == Rol.RolId))
                            {
                                UsuarioActual.Roles.Add(new UsuarioRol() { UsuarioId = UsuarioActual.UsuarioId, RolId = Rol.RolId });
                            }
                        }
                    }

                    if (entidad.Agencias != null && entidad.Agencias.Count() > 0)
                    {
                        //Eliminar por usuarioId                          
                        var Agencias = db.Set<UsuarioAgencia>().Where(x => x.UsuarioId == UsuarioActual.UsuarioId).ToList();
                        db.Set<UsuarioAgencia>().RemoveRange(Agencias);

                        //Agregar las nuevas agencias
                        UsuarioActual.Agencias = new List<UsuarioAgencia>();

                        foreach (var Agencia in entidad.Agencias)
                        {
                            if (!UsuarioActual.Agencias.Any(x => x.AgenciaId == Agencia.AgenciaId))
                            {
                                UsuarioActual.Agencias.Add(new UsuarioAgencia() { UsuarioId = UsuarioActual.UsuarioId, AgenciaId = Agencia.AgenciaId });
                            }
                        }
                    }

                    if (entidad.AgenciaConsultas != null && entidad.AgenciaConsultas.Count() > 0)
                    {
                        //Eliminar por usuarioId                          
                        var Agencias = db.Set<UsuarioAgenciaConsulta>().Where(x => x.UsuarioId == UsuarioActual.UsuarioId).ToList();
                        db.Set<UsuarioAgenciaConsulta>().RemoveRange(Agencias);

                        //Agregar las nuevas agencias
                        UsuarioActual.AgenciaConsultas = new List<UsuarioAgenciaConsulta>();

                        foreach (var Agencia in entidad.AgenciaConsultas)
                        {
                            if (!UsuarioActual.AgenciaConsultas.Any(x => x.AgenciaId == Agencia.AgenciaId))
                            {
                                UsuarioActual.AgenciaConsultas.Add(new UsuarioAgenciaConsulta() { UsuarioId = UsuarioActual.UsuarioId, AgenciaId = Agencia.AgenciaId });
                            }
                        }
                    }

                    if (entidad.Empresas != null && entidad.Empresas.Count() > 0)
                    {
                        //Eliminar por usuarioId                          
                        var Empresas = db.Set<UsuarioEmpresa>().Where(x => x.UsuarioId == UsuarioActual.UsuarioId).ToList();
                        db.Set<UsuarioEmpresa>().RemoveRange(Empresas);

                        //Agregar las nuevas empresas
                        UsuarioActual.Empresas = new List<UsuarioEmpresa>();

                        foreach (var Empresa in entidad.Empresas)
                        {
                            UsuarioActual.Empresas.Add(new UsuarioEmpresa() { UsuarioId = UsuarioActual.UsuarioId, EmpresaId = Empresa.EmpresaId, Codigo = Empresa.Codigo, SERIE_SAP = Empresa.SERIE_SAP });
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

        private void ActualizarUltimaActividad(Usuario entidad)
        {
            try
            {                                
                Usuario UsuarioActual = ObtenerPorId(entidad.UsuarioId, false);

                if (UsuarioActual.UsuarioId > 0)
                {
                    UsuarioActual.FechaUltimaActividad = entidad.FechaUltimaActividad;
                    db.SaveChanges();
                }
            }
            catch (Exception)
            {}

        }

        #endregion

        #region Metodos Publicos

        public string ValidarUsuario(string usuario, string Encriptado_password, string password, bool Servicio = false)
        {
            string Mensaje = "OK";

            var Usuario = ObtenerPorLogin(usuario);

            if (Usuario == null)
            {
                return "El usuario que ingreso no se encuentra registrado";
            }

            if (Usuario.UsuarioId == 0)
            {
                return "El usuario que ingreso no se encuentra registrado";
            }

            if (!Usuario.Activo)
            {
                return "El usuario que ingreso no se encuentra activo";
            }

            bool Password_Invalido = false;

            if (Servicio)
            {
                if (!Usuario.AutenticarAndroid)
                {
                    return "El usuario que ingreso no tiene acceso para autenticarse en el dispositivo";
                }

                if (Usuario.PasswordAndroid == password)
                {
                    Password_Invalido = true;
                }
            }
            else
            {
                if (!Usuario.AutenticarSite)
                {
                    return "El usuario que ingreso no tiene acceso para autenticarse en el site";
                }

                if (Usuario.Password != Encriptado_password)
                {
                    return "El usuario o password están incorrectos";
                }
                else
                {
                    Password_Invalido = true;
                }
            }

            if (!Password_Invalido)
            {
                return "El usuario o password están incorrectos";
            }
            else
            {
                Usuario.FechaUltimaActividad = DateTime.Now;
                ActualizarUltimaActividad(Usuario);
            }

            return Mensaje;
        }

        public string Guardar(Usuario entidad, bool correo = false)
        {
            string Mensaje = "OK";

            if (!string.IsNullOrWhiteSpace(entidad.Celular))
            {
                int Inicial = 10000000;
                int Final = 99999999;
                int Celular = int.Parse(entidad.Celular.Replace("-",""));

                if (Celular <= Inicial && Celular >= Final)
                {
                    return "EL #TELEFONO NO ES VALIDO";
                }
            }
               
            if (entidad.UsuarioId > 0)
            {
                Mensaje = Actualizar(entidad);
            }
            else
            {
                if (!ExisteLogin(entidad.Login))
                {
                    Mensaje = Agregar(entidad, correo);
                }
                else
                {
                    Mensaje = "El usuario que ingreso ya existe en el sistema";
                }
            }
       
            return Mensaje;
        }

        public string DesactivarUsuario(int usuarioId)
        {
            string Mensaje = "OK";

            try
            {

                Usuario UsuarioActual = ObtenerPorId(usuarioId, false);

                if (UsuarioActual == null)
                {
                    return "El usuario que quiere desactivar no está registrado en el sistema";
                }

                if (UsuarioActual.UsuarioId == 0)
                {
                    return "El usuario que quiere desactivar no está registrado en el sistema";
                }

                UsuarioActual.Activo = false;
                db.SaveChanges();

            }
            catch (Exception)
            {
            }

            return Mensaje;
        }

        public string ActualizarPassword(Usuario entidad)
        {
            string Mensaje = "OK";

            try
            {

                Usuario UsuarioActual = ObtenerPorId(entidad.UsuarioId, false);

                if (UsuarioActual == null)
                {
                    return "El usuario que quiere actualizar el password no está registrado en el sistema";
                }

                if (UsuarioActual.UsuarioId == 0)
                {
                    return "El usuario que quiere actualizar el password no está registrado en el sistema";
                }

                string concat = Concat_Usuario(entidad.Login, entidad.Password);

                UsuarioActual.Password = Key(concat);
                UsuarioActual.ReiniciarPassword = false;

                db.SaveChanges();

            }
            catch (Exception)
            {
            }

            return Mensaje;
        }

        public Usuario ObtenerPorLogin(string usuario)
        {
            Usuario UsuarioActual = new Usuario();

            try
            {
                UsuarioActual = db.Set<Usuario>().Include("Roles").Include("Agencias").Include("Agencias.Agencia").Include("Empresas").Include("Empresas.Empresa").Where(x => x.Login.Equals(usuario)).FirstOrDefault();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Ocurrió un error: {0}", ex);
            }

            return UsuarioActual;
        }

        public Usuario ObtenerPorId(long id, bool todos)
        {
            Usuario UsuarioActual = new Usuario();

            try
            {
                if (todos)
                {
                    UsuarioActual = db.Set<Usuario>().Include("Roles").Include("Agencias").Include("AgenciaConsultas").Include("Empresas").Where(x => x.UsuarioId == id).FirstOrDefault();
                }
                else
                {
                    UsuarioActual = db.Set<Usuario>().Where(x => x.UsuarioId == id).FirstOrDefault();
                }
            }
            catch (Exception)
            {
            }

            return UsuarioActual;
        }

        public Usuario ObtenerUsuarioConRol(string usuario)
        {
            Usuario UsuarioActual = new Usuario();

            try
            {

                UsuarioActual = db.Set<Usuario>().Include("Roles").Include("Agencias").Where(x => x.Login.Equals(usuario)).FirstOrDefault();

                if (UsuarioActual != null)
                {
                    UsuarioActual.RolesPermiso = new RolBL().ObtenerPermisoPorUsuario(UsuarioActual.Login);
                }

            }
            catch (Exception)
            {
            }

            return UsuarioActual;
        }

        public List<Usuario> ObtenerListado()
        {
            List<Usuario> Usuarios = new List<Usuario>();

            try
            {
                Usuarios = db.Set<Usuario>().Include("Roles").Include("Agencias").Include("Empresas").OrderByDescending(x => x.Fecha).ThenByDescending(x => x.UsuarioId).ToList();
            }
            catch (Exception)
            {}

            return Usuarios;
        }

        public List<Usuario> Buscar(string Buscar)
        {
            List<Usuario> Usuarios = new List<Usuario>();

            try
            {
                Usuarios = db.Set<Usuario>().Include("Roles").Include("Agencias").Include("Empresas").Where(x => x.Nombre.Contains(Buscar)).ToList();
            }
            catch (Exception)
            {}

            return Usuarios;
        }

        public List<Usuario> ObtenerUsuarioxAgenciaId(long agenciaId)
        {
            List<Usuario> Usuarios = new List<Usuario>();

            try
            {
                Usuarios = db.Set<UsuarioAgencia>().Include("Usuario").AsNoTracking().Where(x => x.AgenciaId == agenciaId).Select(x => x.Usuario).ToList();       
            }
            catch (Exception)
            {
            }

            return Usuarios;
        }

        public List<Usuario> ObtenerTecnicos()
        {
            List<Usuario> Usuarios = new List<Usuario>();
            List<long> UsuarioIDs = new List<long>();

            try
            {
                UsuarioIDs = db.Set<UsuarioRol>().AsNoTracking().Where(x => x.RolId == 6).Select(x => x.UsuarioId).Distinct().ToList();
                if (UsuarioIDs != null && UsuarioIDs.Count() > 0)
                {
                    Usuarios = db.Set<Usuario>().AsNoTracking().Where(x => UsuarioIDs.Contains(x.UsuarioId)).ToList();                   
                }
            }
            catch (Exception)
            {}

            return Usuarios;
        }

        public bool Autorizar(string usuario, string password)
        {
            bool Autorizar = false;

            var Usuario = ObtenerPorLogin(usuario);

            if (Usuario == null)
            {
                return Autorizar;
            }

            if (Usuario.UsuarioId == 0)
            {
                return Autorizar;
            }

            if (!Usuario.Activo)
            {
                return Autorizar;
            }

            bool Password_Invalido = false;

            if (!Usuario.AutenticarSite)
            {
                return Autorizar;
            }

            string concat = Concat_Usuario(Usuario.Login, password);
            password = Key(concat);
         
            if (Usuario.Password != password)
            {
                return Autorizar;
            }
            else
            {
                Password_Invalido = true;
            }

            if (!Password_Invalido)
            {
                return Autorizar;
            }

            List<int> RolIDs = new List<int>(){ 8 };

            //Verifica Rol
            bool TieneRolSupervisor = db.Set<UsuarioRol>().AsNoTracking().Where(x => x.UsuarioId == Usuario.UsuarioId && RolIDs.Contains(x.RolId)).Count() > 0;


            return TieneRolSupervisor;
        }

        public List<Usuario> ObtenerActivos()
        {
            List<Usuario> Usuarios = new List<Usuario>();            

            try
            {
                Usuarios = db.Set<Usuario>().AsNoTracking().Where(x => x.Activo).ToList();
            }
            catch (Exception)
            {}

            return Usuarios;
        }

        public List<ModelSale> BuscarVendedoresxEmpresa(long empresaId)
        {
            List<ModelSale> SalesEmploye = new List<ModelSale>();

            try
            {
                //Productos.Add(new Warehouse { WarehouseId = "W1", Nombre = "producto terminado" });


                if (empresaId == 20210705001)
                {
                    SalesEmploye = ObtenerVendedoresApi("BOLIK");
                }
                else if (empresaId == 20210705002)
                {
                    SalesEmploye = ObtenerVendedoresApi( "EMPAQUES");
                }
                else if (empresaId == 20210705003)
                {
                    SalesEmploye = ObtenerVendedoresApi("ESCOCESA");
                }
                else if (empresaId == 20210705004)
                {
                    SalesEmploye = ObtenerVendedoresApi("GRACO");
                }
            }
            catch (Exception)
            { }

            return SalesEmploye;
        }


        public List<ModelSale> ObtenerVendedoresApi(string NombreEmpresa)
        {
            // Base URL de la API
            string apiUrl = $"{URL_SAP}Vendedor";

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
                        List<ModelSale> salesPerson = JsonConvert.DeserializeObject<List<ModelSale>>(jsonResponse);

                        // Retornar los clientes deserializados
                        return salesPerson;
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
                    return new List<ModelSale>();
                }
            }
        }

        #endregion
    }
}
