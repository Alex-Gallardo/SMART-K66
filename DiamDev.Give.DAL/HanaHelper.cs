using System;
using System.Configuration;
using System.Data;
using System.Data.Odbc;

namespace DiamDev.Give.DAL
{
    public class HanaHelper
    {
        private static readonly string _server = ConfigurationManager.AppSettings["HANA_Server"];
        private static readonly string _tenantDb = ConfigurationManager.AppSettings["HANA_TenantDB"];
        private static readonly string _schema = ConfigurationManager.AppSettings["HANA_Database"];
        private static readonly string _user = ConfigurationManager.AppSettings["HANA_User"];
        private static readonly string _password = ConfigurationManager.AppSettings["HANA_Password"];
        private static readonly string _driver = ConfigurationManager.AppSettings["HANA_Driver"]
                                                    ?? "HDBODBC32";

        /// <summary>
        /// DATABASENAME = nombre del tenant HANA (NDB), NO el schema SAP B1 (SBOESCOCESA).
        /// El schema se referencia en el SQL: "SBOESCOCESA"."OITM"
        /// </summary>
        private static string ConnectionString =>
            $"Driver={{{_driver}}};" +
            $"SERVERNODE={_server};" +
            $"DATABASENAME={_tenantDb};" +   // NDB — el tenant real
            $"UID={_user};" +
            $"PWD={_password};";

        public static DataTable EjecutarConsulta(string sql,
            OdbcParameter[] parametros = null)
        {
            DataTable resultado = new DataTable();
            try
            {
                using (OdbcConnection conexion = new OdbcConnection(ConnectionString))
                {
                    conexion.Open();
                    using (OdbcCommand comando = new OdbcCommand(sql, conexion))
                    {
                        comando.CommandTimeout = 120;

                        if (parametros != null)
                            foreach (var p in parametros)
                                comando.Parameters.Add(p);

                        using (OdbcDataAdapter adapt = new OdbcDataAdapter(comando))
                            adapt.Fill(resultado);
                    }
                }
            }
            catch (OdbcException ex)
            {
                throw new Exception(
                    $"Error HANA. Driver:{_driver} | Server:{_server} | " +
                    $"Tenant:{_tenantDb} | Schema:{_schema} | {ex.Message}", ex);
            }
            return resultado;
        }

        public static bool ProbarConexion(out string mensajeError)
        {
            mensajeError = string.Empty;
            try
            {
                using (OdbcConnection conexion = new OdbcConnection(ConnectionString))
                {
                    conexion.Open();
                    using (OdbcCommand cmd = new OdbcCommand("SELECT 1 FROM DUMMY", conexion))
                        cmd.ExecuteScalar();
                    return true;
                }
            }
            catch (Exception ex)
            {
                mensajeError = ex.Message;
                return false;
            }
        }
    }
}