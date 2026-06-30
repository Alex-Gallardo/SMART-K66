using System;
using System.Collections.Generic;
using System.Linq;
using DiamDev.Give.DAL;
using DiamDev.Give.Entities;

namespace DiamDev.Give.BLL
{
    /// <summary>
    /// Orquesta la sincronización de recibos: detecta en SAP (HANA/ORCT) cuáles
    /// recibos PENDIENTES ya fueron operados por créditos, y los marca en SQL.
    /// La lógica vive aquí (BLL); el Sincronizador solo invoca Ejecutar().
    /// </summary>
    public class ReciboCajaSyncBL
    {
        private static readonly string[] EMPRESAS = { "GRACO", "FAES", "BOLIK" };
        private const int LOTE_SQL = 500;   // pendientes por empresa por vuelta

        private readonly ReciboCajaSyncDA _sql = new ReciboCajaSyncDA();
        private readonly HanaRepository _hana = new HanaRepository();

        /// <summary>
        /// Resumen de una corrida, para que el Sincronizador lo muestre/loguee.
        /// </summary>
        public class ResultadoSync
        {
            public int Revisados { get; set; }
            public int Operados { get; set; }
            public List<string> Errores { get; } = new List<string>();
        }

        /// <summary>Procesa las 3 empresas. Devuelve el resumen agregado.</summary>
        public ResultadoSync Ejecutar()
        {
            var res = new ResultadoSync();

            foreach (string empresa in EMPRESAS)
            {
                try
                {
                    ProcesarEmpresa(empresa, res);
                }
                catch (Exception ex)
                {
                    // Una empresa que falla NO debe tumbar a las otras dos.
                    res.Errores.Add(string.Format("[{0}] {1}", empresa, ex.Message));
                }
            }

            return res;
        }

        private void ProcesarEmpresa(string empresa, ResultadoSync res)
        {
            // 1. Traer un lote de pendientes (cola rotativa: nuevos y más viejos de revisar)
            List<string> pendientes = _sql.ObtenerRecibosPendientes(empresa, LOTE_SQL);
            if (pendientes.Count == 0) return;

            res.Revisados += pendientes.Count;

            // 2. Preguntar a SAP cuáles de esos ya están operados (existen en ORCT, Canceled='N')
            List<SapCobroAplicado> operados = _hana.ObtenerCobrosOperados(empresa, pendientes);

            // 3. Marcar como OPERADO los que SAP confirmó
            var idsOperados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var cobro in operados)
            {
                try
                {
                    _sql.MarcarReciboOperado(cobro, empresa);
                    idsOperados.Add(cobro.IdRecibo);
                    res.Operados++;
                }
                catch (Exception ex)
                {
                    res.Errores.Add(string.Format("[{0}] {1}: {2}",
                        empresa, cobro.IdRecibo, ex.Message));
                }
            }

            // 4. A los que NO aparecieron en SAP, sellarles SYNC_ULTIMO_CHECK para que
            //    roten al final de la cola y no se queden atascando el lote.
            var noOperados = pendientes.Where(id => !idsOperados.Contains(id)).ToList();
            _sql.MarcarUltimoCheckLote(noOperados, empresa);
        }
    }
}