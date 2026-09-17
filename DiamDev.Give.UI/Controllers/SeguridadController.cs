using DiamDev.Give.BLL;
using DiamDev.Give.Entities;
using DiamDev.Give.UI.App_Start;
using DiamDev.Give.UI.Models;
using Sistema.Seguridad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace DiamDev.Give.UI.Controllers
{
    [Authorize]
    [HandleError]
    public class SeguridadController : Controller
    {
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginModel model, string returnUrl)
        {
            Session.Remove("Pilotos.Autenticado");
            Session.Remove("Pilotos.Desafio");
            if (ModelState.IsValid)
            {

                try
                {
                    string Token = string.Concat(model.Usuario, model.Password, model.Usuario);
                    string Key = Criptografia.Base64StringAHexString(Criptografia.EncriptarSha512(Token));
                    string Mensaje = new UsuarioBL().ValidarUsuario(model.Usuario, Key, model.Password);

                    if (Mensaje.Equals("OK"))
                    {
                        Usuario UsuarioActual = new UsuarioBL().ObtenerPorLogin(model.Usuario);
                        if (new RolBL().UsuarioTieneRol(UsuarioActual.Login, "PILOTO"))
                            return IniciarPiloto(UsuarioActual);
                        FormsAuthentication.SetAuthCookie(model.Usuario, true);

                        CustomHelper.getUserName(model.Usuario);

                        if (UsuarioActual.Agencias.Count() == 1)
                        {
                            Agencia AgenciaActual = UsuarioActual.Agencias[0].Agencia;
                            CustomHelper.setAgencia(AgenciaActual);
                        }

                        if (UsuarioActual.Token)
                        {
                            try
                            {
                                if (!string.IsNullOrWhiteSpace(UsuarioActual.Celular))
                                {                                    
                                    Configuracion ConfiguracionSMS = new ConfiguracionBL().ObtenerPorId(20210308001);
                                    if (ConfiguracionSMS != null)
                                    {
                                        //GENERA CODIGO
                                        int num = new Random().Next(1000, 9999);
                                        string MensajeSMS = string.Format("TOKEN DE K66 ES: {0}", num);

                                        String servidor = "https://api.sms.to/sms/send?api_key=" + ConfiguracionSMS.Valor + "&to=" + string.Format("+502{0}", UsuarioActual.Celular.Replace("-","")) + "&message=" + MensajeSMS + "&sender_id=smsto";

                                        WebClient client = new WebClient();
                                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                                        string reply = client.DownloadString(servidor);

                                        return RedirectToAction("Token", new { id = num });
                                    }                                   
                                }
                                else
                                {
                                    ModelState.AddModelError("", "El usuario no contiene configurado el #telefono.");
                                }
                            }
                            catch (Exception)
                            { }
                        }
                        else
                        {
                            return RedirectToAction("Dashboard", "Inicio");
                        }
                    }

                }
                catch (Exception ex)
                {
                    ViewBag.Error = string.Format("Message: {0} StackTrace: {1}", ex.Message, ex.StackTrace);
                    return View("~/Views/Shared/Error.cshtml");
                }

            }

            ModelState.AddModelError("", "El usuario o la clave son incorrectos.");
            return View(model);
        }

        public ActionResult Token(int id)
        {
            return View(new AutenticarToken() { Token = id });
        }

        [HttpPost]
        public ActionResult Token(AutenticarToken model)
        {
            if (model.Token == model.ValidarToken)
            {
                return RedirectToAction("Dashboard", "Inicio");
            }
            else
            {
                ModelState.AddModelError("", "El token de K66 ingresado no es valido.");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            FormsAuthentication.SignOut();
            Session.Abandon();
            return RedirectToAction("Login", "Seguridad");
        }

        private ActionResult IniciarPiloto(Usuario usuario)
        {
            FormsAuthentication.SignOut();
            Session.Remove("Pilotos.Autenticado");
            Session.Remove("Pilotos.Desafio");
            if (!usuario.Token) return CompletarLoginPiloto(usuario);
            if (string.IsNullOrWhiteSpace(usuario.Celular))
            {
                ModelState.AddModelError("", "No esta configurado el telefono de validacion.");
                return View("Login");
            }
            try
            {
                var config = new ConfiguracionBL().ObtenerPorId(20210308001);
                if (config == null || string.IsNullOrWhiteSpace(config.Valor)) throw new InvalidOperationException();
                var desafio = new PilotoDesafio(usuario.Login, DateTime.UtcNow);
                var contenido = Newtonsoft.Json.JsonConvert.SerializeObject(new {
                    to = "+502" + usuario.Celular.Replace("-", ""),
                    message = "TOKEN DE K66 ES: " + desafio.Codigo, sender_id = "smsto"
                });
                using (var cliente = new WebClient())
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    cliente.Headers[HttpRequestHeader.Authorization] = "Bearer " + config.Valor;
                    cliente.Headers[HttpRequestHeader.ContentType] = "application/json";
                    cliente.Encoding = System.Text.Encoding.UTF8;
                    var respuesta = Newtonsoft.Json.Linq.JObject.Parse(cliente.UploadString("https://api.sms.to/sms/send", "POST", contenido));
                    if ((bool?)respuesta["success"] != true) throw new InvalidOperationException();
                }
                Session["Pilotos.Desafio"] = desafio;
                return RedirectToAction("PilotoToken");
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "No se pudo enviar el codigo. Intenta iniciar sesion nuevamente.");
                return View("Login");
            }
        }

        private ActionResult CompletarLoginPiloto(Usuario usuario)
        {
            // El contexto del piloto solo se crea despues de validar sus factores.
            Session.Remove("Nombre"); Session.Remove("Usuario"); Session.Remove("Agencia");
            CustomHelper.getUserName(usuario.Login);
            if (usuario.Agencias != null && usuario.Agencias.Count == 1) CustomHelper.setAgencia(usuario.Agencias[0].Agencia);
            FormsAuthentication.SetAuthCookie(usuario.Login, false);
            Session["Pilotos.Autenticado"] = usuario.Login;
            Session.Remove("Pilotos.Desafio");
            return RedirectToAction("Index", "Piloto");
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult PilotoToken()
        {
            Response.Cache.SetNoStore();
            if (Session["Pilotos.Desafio"] == null) return RedirectToAction("Login");
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PilotoToken(string codigo)
        {
            Response.Cache.SetNoStore();
            var desafio = Session["Pilotos.Desafio"] as PilotoDesafio;
            if (desafio != null && desafio.Verificar(codigo, DateTime.UtcNow))
            {
                var usuario = new UsuarioBL().ObtenerPorLogin(desafio.Login);
                Session.Remove("Pilotos.Desafio");
                if (usuario != null && usuario.Activo && usuario.AutenticarSite && new RolBL().UsuarioTieneRol(usuario.Login, "PILOTO"))
                    return CompletarLoginPiloto(usuario);
                return RedirectToAction("Login");
            }
            if (desafio == null || desafio.Intentos >= 5 || DateTime.UtcNow >= desafio.VenceUtc)
            {
                Session.Remove("Pilotos.Desafio"); return RedirectToAction("Login");
            }
            ModelState.AddModelError("", "Codigo incorrecto. Revisa el mensaje recibido.");
            return View();
        }

        public ActionResult Menu()
        {
            return PartialView("~/Views/Shared/_Menu.cshtml", new MenuBL().ObtenerMenuPorUsuario(System.Web.HttpContext.Current.User.Identity.Name));
        }

        public ActionResult NoAccess()
        {
            return View();
        }

        public ActionResult ReiniciarPassword(long id)
        {
            Usuario UsuarioActual = new UsuarioBL().ObtenerPorId(id, false);

            if (UsuarioActual == null)
            {
                return HttpNotFound();
            }

            return View(new UsuarioModel() { UsuarioId = UsuarioActual.UsuarioId, Login = UsuarioActual.Login });
        }

        [HttpPost]
        public ActionResult ReiniciarPassword(UsuarioModel model)
        {
            if (ModelState.IsValid)
            {
                string strMensaje = string.Empty;

                strMensaje = new UsuarioBL().ActualizarPassword(new Usuario() { UsuarioId = model.UsuarioId, Login = model.Login, Password = model.Password });

                if (strMensaje.Equals("OK"))
                {
                    return RedirectToAction("Dashboard", "Inicio");
                }
            }

            return View(model);
        }
    }
}
