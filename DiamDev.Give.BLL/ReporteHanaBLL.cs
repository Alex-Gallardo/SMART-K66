using System;
using System.Data;
using System.Data.Odbc;
using DiamDev.Give.DAL;

namespace DiamDev.Give.BLL
{
    /// <summary>
    /// Capa de negocio para reportes que leen de SAP HANA.
    /// Cada método corresponde a un reporte Crystal Reports (.rpt).
    ///
    /// PATRÓN: El método recibe los filtros (fechas, agente, empresa),
    /// construye el SQL de HANA, llama a HanaHelper y devuelve el DataTable
    /// que el controller pasará directamente al ReportDocument de Crystal.
    /// </summary>
    public class ReporteHanaBLL
    {
        // ────────────────────────────────────────────────────────────────
        // EJEMPLO 1: Inventario General (equivalente al de SSRS en Gerencia)
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Obtiene el inventario general de una empresa SAP.
        /// El parámetro empresa puede ser: BOLIK, FAES, GRACO, ESCOCESA, etc.
        /// </summary>
        
            // En HANA las tablas de SAP Business One siguen el estándar:
            // Nombre_Tabla = prefijo de 3 letras + nombre (ej: OITM, OITW)
            // Las vistas de los ViewModels (VMBOLIK, VMGRACO, etc.) ya
            // sincronizan esto a SQL Server. Aquí leemos directo de HANA.
           

            // IMPORTANTE: En HANA los nombres de columna son CASE SENSITIVE
            // y deben ir entre comillas dobles cuando tienen mayúsculas/minúsculas.
            // Si tu SQL usa nombres en minúsculas o de vistas, puede que no necesites comillas.


        // ────────────────────────────────────────────────────────────────
        // EJEMPLO 2: Backorder por Agente (con parámetro)
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Backorder filtrado por código de agente/vendedor SAP.
        /// Nota: En HANA ODBC los parámetros se pasan como "?" (signo de pregunta),
        /// a diferencia de SQL Server que usa "@NombreParametro".
        /// </summary>
        

            // El "?" es el placeholder para HANA ODBC.
            // En el orden en que aparecen en el SQL, así deben agregarse los parámetros.
           

        public DataTable ObtenerInventarioGeneral(string empresa)
        {
            // En HANA las tablas SAP B1 están en el schema SBOESCOCESA
            // Nombres de columna son case-sensitive → van entre comillas dobles
            string sql = $@"
        SELECT 
            T0.""ItemCode""   AS Codigo,
            T0.""ItemName""   AS Nombre,
            T0.""OnHand""     AS Existencia,
            T0.""AvgPrice""   AS PrecioCosto,
            T1.""WhsCode""    AS Bodega
        FROM ""SBOESCOCESA"".""OITM"" T0
        INNER JOIN ""SBOESCOCESA"".""OITW"" T1
               ON  T0.""ItemCode"" = T1.""ItemCode""
        WHERE T0.""Canceled"" = 'N'
        ORDER BY T0.""ItemCode""";

            return HanaHelper.EjecutarConsulta(sql);
        }

        public DataTable ObtenerBackorderPorAgente(int idAgente, string empresa)
        {
            string sql = @"
        SELECT 
            T0.""DocNum""      AS NoPedido,
            T0.""CardCode""    AS CodigoCliente,
            T0.""CardName""    AS Cliente,
            T0.""DocDate""     AS FechaPedido,
            T1.""ItemCode""    AS Codigo,
            T1.""Dscription""  AS Descripcion,
            T1.""Quantity""    AS CantidadPedida,
            T1.""OpenQty""     AS CantidadPendiente,
            T2.""SlpName""     AS Vendedor
        FROM ""SBOESCOCESA"".""ORDR"" T0
        INNER JOIN ""SBOESCOCESA"".""RDR1"" T1
               ON  T0.""DocEntry"" = T1.""DocEntry""
        INNER JOIN ""SBOESCOCESA"".""OSLP"" T2
               ON  T0.""SlpCode""  = T2.""SlpCode""
        WHERE T1.""OpenQty"" > 0
          AND T2.""SlpCode"" = ?
        ORDER BY T0.""DocDate"" DESC";

            OdbcParameter[] parametros = new[]
            {
        new OdbcParameter("idAgente", OdbcType.Int) { Value = idAgente }
    };

            return HanaHelper.EjecutarConsulta(sql, parametros);
        }

        // ────────────────────────────────────────────────────────────────
        // EJEMPLO 3: Ventas Diarias (con rango de fechas)
        // ────────────────────────────────────────────────────────────────

        public DataTable ObtenerVentasDiarias(DateTime fechaInicial, DateTime fechaFinal)
        {
            string sql = @"
                SELECT
                    T0.""DocNum""     AS NoFactura,
                    T0.""CardName""   AS Cliente,
                    T0.""DocDate""    AS Fecha,
                    T0.""DocTotal""   AS Total,
                    T2.""SlpName""    AS Vendedor
                FROM OINV T0
                INNER JOIN OSLP T2 ON T0.""SlpCode"" = T2.""SlpCode""
                WHERE T0.""DocDate"" BETWEEN ? AND ?
                  AND T0.""Canceled"" = 'N'
                ORDER BY T0.""DocDate""";

            OdbcParameter[] parametros = new[]
            {
                new OdbcParameter("fechaInicial", OdbcType.Date) { Value = fechaInicial.Date },
                new OdbcParameter("fechaFinal",   OdbcType.Date) { Value = fechaFinal.Date }
            };

            return HanaHelper.EjecutarConsulta(sql, parametros);
        }

        // Aquí irás agregando un método por cada reporte Crystal que migres.
        // El patrón es siempre: escribir el SQL de HANA → llamar HanaHelper → devolver DataTable.
    }
}