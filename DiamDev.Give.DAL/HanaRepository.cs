using System;
using System.Collections.Generic;
using System.Data;
using DiamDev.Give.Entities;

namespace DiamDev.Give.DAL
{
    /// <summary>
    /// Repositorio de clientes desde SAP HANA.
    /// 
    /// NO usa HanaConnection directo — delega en HanaHelper que ya está
    /// configurado en el proyecto con AppSettings HANA_Server / HANA_User / etc.
    /// 
    /// Por qué: Crystal Reports usa driver B1CRHPROXY embebido en el .rpt.
    /// Eso NO instala el NuGet Sap.Data.Hana en el proyecto.
    /// HanaHelper ya abstrae la conexión correctamente.
    /// </summary>
    public class HanaRepository
    {
        /// <summary>
        /// Llama al stored procedure INF_CLIENTES_REC del schema SAP
        /// correspondiente a la empresa seleccionada.
        /// 
        /// Equivale al método ObtenerCodClientesSAP() / txtIdCliente_TextChanged()
        /// del desktop (frmIngresoRecibo.vb) pero server-side y sin ListBox.
        /// </summary>
        public List<ClienteHana> BuscarClientes(string empresa, string agente)
        {
            var lista = new List<ClienteHana>();

            // Determinar schema SAP según empresa
            // (igual que ResolverSchemaSapDesdeEmpresa en ReporteController)
            string schema;
            switch ((empresa ?? "").Trim().ToUpper())
            {
                case "GRACO": schema = "SBO_GRACO"; break;
                case "FAES": schema = "SBOESCOCESA"; break;
                case "BOLIK": schema = "SBOBOLIK"; break;
                default:
                    return lista;  // empresa desconocida → lista vacía
            }

            // CALL en HANA: CALL "SCHEMA"."PROCEDURE"('parametro')
            // Usamos comillas dobles para respetar el case del schema
            string query = string.Format(
                "CALL \"{0}\".\"INF_CLIENTES_REC\"('{1}')",
                schema,
                Esc(agente ?? "")
            );

            try
            {
                // HanaHelper.EjecutarConsulta ya maneja la conexión HANA
                // con las credenciales de AppSettings (HANA_Server, HANA_User, etc.)
                DataTable dt = HanaHelper.EjecutarConsulta(query);

                foreach (DataRow row in dt.Rows)
                {
                    lista.Add(new ClienteHana
                    {
                        CardCode = LeerCampo(row, "CardCode"),
                        CardName = LeerCampo(row, "CardName"),
                        Address = LeerCampo(row, "Address"),
                        LicTradNum = LeerCampo(row, "LicTradNum"),
                        SlpName = LeerCampo(row, "SlpName"),
                        Email = LeerCampo(row, "E_mail"),
                        Currency = LeerCampo(row, "Currency")
                    });
                }
            }
            catch (Exception ex)
            {
                // Re-lanzamos con contexto para que el BLL lo muestre correctamente
                throw new Exception(
                    string.Format("Error HANA al buscar clientes ({0} / {1}): {2}",
                        empresa, schema, ex.Message), ex);
            }

            return lista;
        }

        // ── Helpers privados ─────────────────────────────────────────────────

        /// <summary>
        /// Escapa comillas simples en el parámetro para evitar SQL injection
        /// en la llamada al stored procedure HANA.
        /// </summary>
        private static string Esc(string valor) =>
            valor.Replace("'", "''");

        /// <summary>
        /// Lee un campo del DataRow de forma segura — devuelve "" si es null o DBNull.
        /// Equivalente al CStr(row(n)) que usaba el desktop.
        /// </summary>
        private static string LeerCampo(DataRow row, string columna)
        {
            try
            {
                return row[columna] != DBNull.Value
                    ? Convert.ToString(row[columna]) ?? ""
                    : "";
            }
            catch
            {
                // Si la columna no existe en el resultado del SP, no explota
                return "";
            }
        }
    }
}