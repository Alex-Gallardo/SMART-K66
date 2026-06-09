using System;
using System.Collections.Generic;
using System.Linq;
using DiamDev.Give.DAL;
using DiamDev.Give.Entities;

namespace DiamDev.Give.BLL
{
    public class ReciboCajaBLL
    {
        private readonly APK66Context _apk;
        private readonly HanaRepository _hana;

        public ReciboCajaBLL()
        {
            _apk = new APK66Context();
            _hana = new HanaRepository();
        }

        // ─── USUARIOS ────────────────────────────────
        public string ObtenerPlantaUsuario(string idUsr) =>
            _apk.ObtenerPlantaUsuario(idUsr);

        // ─── CLIENTES (HANA) ─────────────────────────
        /// <summary>
        /// Trae todos los clientes del agente para esa empresa desde HANA,
        /// luego filtra localmente (igual que el desktop con el ListBox).
        /// El parámetro 'filtro' puede ser código o nombre parcial.
        /// </summary>
        public List<ClienteHana> BuscarClientes(string empresa, string agente, string filtro)
        {
            var todos = _hana.BuscarClientes(empresa, agente);
            if (string.IsNullOrWhiteSpace(filtro)) return todos.Take(50).ToList();

            var f = filtro.ToUpper();
            return todos
                .Where(c => c.CardCode.ToUpper().Contains(f) || c.CardName.ToUpper().Contains(f))
                .Take(30)
                .ToList();
        }

        // ─── DOCUMENTOS (APK66) ───────────────────────
        public List<DocumentoRecibo> ObtenerDocumentos(string empresa, string clienteId, string tipoDoc) =>
            _apk.ObtenerDocumentos(empresa, clienteId, tipoDoc);

        // ─── GUARDAR RECIBO ───────────────────────────
        /// <summary>
        /// Valida las reglas de negocio y guarda el recibo completo.
        /// Reglas extraídas del btnGuardar_Click del desktop:
        ///   1. Debe tener al menos un cobro y un documento.
        ///   2. Si monedas iguales → saldo debe ser 0.
        ///   3. Si monedas distintas → se guarda con advertencia (saldo permitido).
        /// </summary>
        public ResultadoRecibo GuardarRecibo(ReciboCajaEncabezado enc, string depto)
        {
            try
            {
                if (enc.Cobros == null || !enc.Cobros.Any())
                    return ResultadoRecibo.Error("Debe agregar al menos un cobro.");

                if (enc.Documentos == null || !enc.Documentos.Any())
                    return ResultadoRecibo.Error("Debe agregar al menos un documento.");

                if (string.IsNullOrWhiteSpace(enc.NombreCliente))
                    return ResultadoRecibo.Error("Debe seleccionar un cliente.");

                // Calcular totales
                enc.MontoTotalRecibo = enc.Cobros.Sum(c => c.Monto);
                enc.MontoTotalDoc = enc.Documentos.Sum(d => d.Monto);
                enc.Saldo = enc.MontoTotalRecibo - enc.MontoTotalDoc;

                // Validar monedas y saldo (lógica original del desktop)
                string monedaCobro = enc.Cobros.Select(c => c.Moneda).FirstOrDefault() ?? "";
                string monedaDoc = enc.Documentos.Select(d => d.Moneda).FirstOrDefault() ?? "";

                bool monedasIguales = monedaCobro == monedaDoc;
                bool saldoCero = enc.Saldo == 0;

                if (monedasIguales && !saldoCero)
                    return ResultadoRecibo.Error(
                        $"El monto de cobros ({enc.MontoTotalRecibo:N2}) " +
                        $"no coincide con el total de documentos ({enc.MontoTotalDoc:N2}). " +
                        $"Saldo: {enc.Saldo:N2}");

                // Si monedas distintas, se permite guardar aunque haya saldo
                // (el desktop mostraba advertencia pero guardaba igual)

                _apk.GuardarReciboCompleto(enc, depto);

                string aviso = monedasIguales
                    ? ""
                    : " (guardado con monedas diferentes)";

                return ResultadoRecibo.Ok(enc.IdRecibo);
            }
            catch (Exception ex)
            {
                return ResultadoRecibo.Error("Error al guardar: " + ex.Message);
            }
        }

        // ─── BUSCAR RECIBO ────────────────────────────
        public ReciboCajaEncabezado BuscarRecibo(string idRecibo, string empresa) =>
            _apk.BuscarRecibo(idRecibo, empresa);

        // ─── EMPRESAS DISPONIBLES ─────────────────────
        /// <summary>
        /// Lista de empresas con sus colores y claves de permiso.
        /// La vista filtra con CustomHelper.Permiso() para mostrar
        /// solo las que el usuario tiene asignadas.
        /// </summary>
        public List<dynamic> ObtenerEmpresas()
        {
            return new List<dynamic>
            {
                new { Id = "GRACO", Nombre = "Graco Pack",        Permiso = "Control.ReciboCaja.Graco", Clase = "empresa-graco" },
                new { Id = "FAES",  Nombre = "Fabrica Escocesa",  Permiso = "Control.ReciboCaja.Faes",  Clase = "empresa-faes"  },
                new { Id = "BOLIK", Nombre = "Industrias Bolik",  Permiso = "Control.ReciboCaja.Bolik", Clase = "empresa-bolik" }
            };
        }
    }
}