using System;
using System.IO;

namespace DiamDev.Give.Sincronizador
{
    /// <summary>
    /// Logger de archivo simple. Escribe en \Logs\sync_yyyy-MM-dd.log junto al .exe.
    /// Un archivo por día. No lanza excepciones (si no puede loguear, no debe
    /// tumbar el proceso).
    /// </summary>
    public static class LogFile
    {
        private static readonly object _lock = new object();

        private static string CarpetaLogs
        {
            get
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory; // donde está el .exe
                string dir = Path.Combine(baseDir, "Logs");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                return dir;
            }
        }

        public static void Info(string mensaje) { Escribir("INFO", mensaje); }
        public static void Error(string mensaje) { Escribir("ERROR", mensaje); }

        public static void Error(string mensaje, Exception ex)
        {
            Escribir("ERROR", mensaje + " | " + ex.Message +
                              (ex.InnerException != null ? " | INNER: " + ex.InnerException.Message : ""));
        }

        private static void Escribir(string nivel, string mensaje)
        {
            try
            {
                string archivo = Path.Combine(CarpetaLogs,
                    "sync_" + DateTime.Now.ToString("yyyy-MM-dd") + ".log");
                string linea = string.Format("{0:yyyy-MM-dd HH:mm:ss} [{1}] {2}",
                    DateTime.Now, nivel, mensaje);

                lock (_lock)
                    File.AppendAllText(archivo, linea + Environment.NewLine);
            }
            catch
            {
                // Si falla el log, no hacemos nada: nunca tumbar el proceso por el logger.
            }
        }
    }
}