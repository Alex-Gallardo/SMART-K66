using DiamDev.Give.BLL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DiamDev.Give.UI.App_Start
{
    public static class CustomHelper
    {
        public static void getUserName()
        {
            if (HttpContext.Current.Session["Nombre"] == null)
            {
                var Usuario = new UsuarioBL().ObtenerPorLogin(HttpContext.Current.User.Identity.Name);

                if (Usuario != null)
                {
                    HttpContext.Current.Session["Usuario"] = Usuario;
                    HttpContext.Current.Session["Nombre"] = Usuario.Nombre;
                }
            }
        }

        public static void getUserName(string User)
        {
            if (HttpContext.Current.Session["Nombre"] == null)
            {
                var Usuario = new UsuarioBL().ObtenerPorLogin(User);

                if (Usuario != null)
                {
                    HttpContext.Current.Session["Usuario"] = Usuario;
                    HttpContext.Current.Session["Nombre"] = Usuario.Nombre;
                }

            }
        }

        public static long getUserId()
        {
            long UserId = 0;

            if (HttpContext.Current.Session["Usuario"] == null)
            {
                var Usuario = new UsuarioBL().ObtenerPorLogin(HttpContext.Current.User.Identity.Name);

                if (Usuario != null)
                {
                    HttpContext.Current.Session["Usuario"] = Usuario;
                    HttpContext.Current.Session["Nombre"] = Usuario.Nombre;
                    UserId = Usuario.UsuarioId;
                }
            }
            else
            {
                var Usuario = (Usuario)HttpContext.Current.Session["Usuario"];

                if (Usuario != null)
                {
                    UserId = Usuario.UsuarioId;
                }
            }

            return UserId;
        }

        public static void setTitle(string Header, string SubHeader)
        {
            HttpContext.Current.Session["Encabezado"] = Header;
            HttpContext.Current.Session["SubEncabezado"] = SubHeader;
        }

        public static bool Permiso(string Permiso)
        {
            return new RolBL().AutorizacionPermisoPorUsuario(HttpContext.Current.User.Identity.Name, Permiso);
        }

        public static void setAgencia(Agencia Agencia)
        {
            HttpContext.Current.Session["Agencia"] = Agencia;
        }

        public static Agencia getAgencia()
        {
            Agencia AgenciaActual = new Agencia();

            if (HttpContext.Current.Session["Agencia"] != null)
            {
                var Agencia = (Agencia)HttpContext.Current.Session["Agencia"];

                if (Agencia != null)
                {
                    AgenciaActual = Agencia;
                }
            }

            return AgenciaActual;
        }

        public static string getAgenciaNombre()
        {
            string Nombre = string.Empty;

            if (HttpContext.Current.Session["Agencia"] != null)
            {
                var Agencia = (Agencia)HttpContext.Current.Session["Agencia"];

                if (Agencia != null)
                {
                    Nombre = Agencia.Nombre;
                }
            }
            else
            {
                Nombre = "Give";
            }

            return Nombre;
        }

        public static long getAgenciaId()
        {
            long AgenciaId = 0;

            if (HttpContext.Current.Session["Agencia"] != null)
            {
                var Agencia = (Agencia)HttpContext.Current.Session["Agencia"];

                if (Agencia != null)
                {
                    AgenciaId = Agencia.AgenciaId;
                }
            }

            return AgenciaId;
        }  
    }
}