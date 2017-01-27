using DiamDev.Give.BLL;
using DiamDev.Give.Entities;
using DiamDev.Give.UI.App_Start;
using DiamDev.Give.UI.Models;
using Sistema.Seguridad;
using System;
using System.Collections.Generic;
using System.Linq;
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

                        if (!UsuarioActual.ReiniciarPassword)
                        {
                            if (UsuarioActual.Agencias != null && UsuarioActual.Agencias.Count() > 0)
                            {
                                if (UsuarioActual.Agencias.Count() == 1)
                                {
                                    Agencia AgenciaActual = UsuarioActual.Agencias[0].Agencia;
                                    CustomHelper.setAgencia(AgenciaActual);

                                    return RedirectToAction("Dashboard", "Inicio");
                                }
                                else
                                {
                                    return RedirectToAction("Agencias", "Inicio");
                                }
                            }
                            else
                            {
                                return RedirectToAction("Dashboard", "Inicio");
                            }
                        }
                        else
                        {
                            return RedirectToAction("ReiniciarPassword", new { id = UsuarioActual.UsuarioId });
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