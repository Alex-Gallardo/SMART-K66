using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DiamDev.Give.BLL
{
    public class Herramienta
    {
        private string Formato_Inicial(long Id)
        {
            string strCorrelativo = string.Empty;
            string strDigitoRelleno = "0";
            int Cantidad = 3;

            try
            {
                int CantidadNecesaria = Cantidad - Id.ToString().Length;
                strCorrelativo = string.Format("{0}{1}", strDigitoRelleno.PadLeft(CantidadNecesaria, '0'), Id);
            }
            catch (Exception)
            {
                strCorrelativo = string.Empty;
            }

            return strCorrelativo;
        }

        public long Formato_Correlativo(long Id)
        {
            long lngId = 0;
            string strId = string.Empty;
            string strFormato_Inicial = Formato_Inicial(Id);

            try
            {
                if (!string.IsNullOrWhiteSpace(strFormato_Inicial))
                {
                    strId = string.Format("{0}{1}", DateTime.Now.ToString("yyyyMMdd"), strFormato_Inicial);

                    if (!long.TryParse(strId, out lngId))
                    {
                        return 0;
                    }
                }
            }
            catch (Exception)
            {
            }

            return lngId;
        }

        public static string Key_Android(string Password)
        {
            string Password_Android = string.Empty;

            try
            {
                UnicodeEncoding UE = new UnicodeEncoding();
                byte[] hashValue;
                byte[] message = UE.GetBytes(Password);

                SHA512Managed hashString = new SHA512Managed();
                string hex = "";

                hashValue = hashString.ComputeHash(message);
                foreach (byte x in hashValue)
                {
                    hex += String.Format("{0:x2}", x);
                }

                if (!string.IsNullOrWhiteSpace(hex))
                {
                    Password_Android = hex;
                }
            }
            catch (Exception)
            {
            }

            return Password_Android;
        }

        public bool ValidarEmail(string Email)
        {
            try
            {
                Regex Val = new Regex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}");

                if (Val.IsMatch(Email))
                {
                    return true;
                }
            }
            catch (Exception)
            {
            }

            return false;
        }

        public static void EnviarCorreo(string Mensaje, string Correo)
        {
            try
            {

                using (MailMessage Mail = new MailMessage())
                {
                    Mail.From = new MailAddress(ConfigurationManager.AppSettings["Correo_Notificacion"].ToString());
                    Mail.To.Add(Correo);
                    Mail.Subject = ConfigurationManager.AppSettings["Titulo_Notificacion"].ToString();

                    Mail.BodyEncoding = System.Text.Encoding.UTF8;
                    Mail.IsBodyHtml = true;
                    Mail.Priority = MailPriority.High;
                    Mail.SubjectEncoding = System.Text.Encoding.UTF8;
                    Mail.Body = Mensaje;

                    using (SmtpClient SmtpServer = new SmtpClient(ConfigurationManager.AppSettings["Smtp_Notificacion"].ToString()))
                    {
                        SmtpServer.Port = Convert.ToInt32(ConfigurationManager.AppSettings["Puerto_Smtp_Notificacion"].ToString());
                        SmtpServer.Credentials = new NetworkCredential(ConfigurationManager.AppSettings["Correo_Notificacion"].ToString(), ConfigurationManager.AppSettings["Password_Notificacion"].ToString());
                        SmtpServer.EnableSsl = Convert.ToBoolean(ConfigurationManager.AppSettings["EnableSSL_Notificacion"].ToString());

                        SmtpServer.Send(Mail);
                    }
                }

            }
            catch (Exception)
            {
            }

        }

        public static void EnviarCorreoAsync(string Mensaje, string Correo)
        {
            Task.Run(() => EnviarCorreo(Mensaje, Correo));
        }
    }
}
