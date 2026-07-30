using System;
using System.Configuration;
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

        public string MesTexto(int mes)
        {
            string Mensaje = string.Empty;

            switch (mes)
            {
                case 1:
                    Mensaje = "Enero";
                    break;
                case 2:
                    Mensaje = "Febrero";
                    break;
                case 3:
                    Mensaje = "Marzo";
                    break;
                case 4:
                    Mensaje = "Abril";
                    break;
                case 5:
                    Mensaje = "Mayo";
                    break;
                case 6:
                    Mensaje = "Junio";
                    break;
                case 7:
                    Mensaje = "Julio";
                    break;
                case 8:
                    Mensaje = "Agosto";
                    break;
                case 9:
                    Mensaje = "Septiembre";
                    break;
                case 10:
                    Mensaje = "Octubre";
                    break;
                case 11:
                    Mensaje = "Noviembre";
                    break;
                case 12:
                    Mensaje = "Diciembre";
                    break;
            }

            return Mensaje;
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
            {}

            return false;
        }

        public bool ValidarNIT(string Nit)
        {
            try
            {
                if (Nit.Equals("C/F") || Nit.Equals("c/f") || Nit.Equals("CF") || Nit.Equals("cf"))
                {
                    return true;
                }

                if (!Nit.Contains("-"))
                {
                    int Longitud = Nit.Length;
                    string NitTemporal = Nit.Substring(0, Longitud - 1);
                    string Identificador = Nit.Substring(Longitud - 1);
                    Nit = string.Format("{0}-{1}", NitTemporal, Identificador);
                }

                int pos = Nit.IndexOf("-");
                string Correlativo = Nit.Substring(0, pos);
                string DigitoVerificador = Nit.Substring(pos + 1);
                int Factor = Correlativo.Length + 1;
                int Suma = 0;
                int Valor = 0;

                for (int x = 0; x <= Nit.IndexOf("-") - 1; x++)
                {
                    Valor = Convert.ToInt32(Nit.Substring(x, 1));
                    Suma = Suma + (Valor * Factor);
                    Factor = Factor - 1;
                }

                double xMOd11 = 0;
                xMOd11 = (11 - (Suma % 11)) % 11;
                string s = Convert.ToString(xMOd11);
                if ((xMOd11 == 10 & DigitoVerificador == "K") | (s.Trim() == DigitoVerificador))
                {
                    return true;
                }
            }
            catch (Exception)
            { }

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
            {}

        }

        public static void EnviarCorreoAsync(string Mensaje, string Correo)
        {
            Task.Run(() => EnviarCorreo(Mensaje, Correo));
        }
    }
}
