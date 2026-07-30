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