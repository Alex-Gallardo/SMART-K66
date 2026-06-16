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
        /// <summary>
        /// [LEGACY] Mantener por compatibilidad. El flujo nuevo usa ObtenerPlantaPorLogin.
        /// </summary>
        public string ObtenerPlantaUsuario(string idUsr) =>
            _apk.ObtenerPlantaUsuario(idUsr);

        /// <summary>
        /// Resuelve la PLANTA (DEPTO) del usuario de caja a partir del login POS.
        /// Lanza excepción con mensaje claro si el login no está vinculado en APK66,
        /// para evitar que el INSERT del correlativo falle con un error críptico.
        /// </summary>
        public string ObtenerPlantaPorLogin(string login)
        {
            string planta = _apk.ObtenerPlantaPorLogin(login);
            if (string.IsNullOrWhiteSpace(planta))
                throw new System.Exception(
                    $"El usuario '{login}' no está vinculado a un usuario de caja activo en APK66 " +
                    $"(REC_CAJA_USUARIOS), o no tiene PLANTA asignada. " +
                    $"Contacte al administrador para habilitarlo.");
            return planta;
        }

        /// <summary>Devuelve el ID_USR canónico de APK66 (mayúsculas) o el login si no hay vínculo.</summary>
        public string ObtenerIdUsrPorLogin(string login)
        {
            string idUsr = _apk.ObtenerIdUsrPorLogin(login);
            return string.IsNullOrWhiteSpace(idUsr) ? (login ?? "").ToUpper() : idUsr;
        }

        // ─── EMPRESAS DISPONIBLES POR USUARIO ─────────
        /// <summary>
        /// Devuelve solo las empresas de RECIBOS (GRACO/FAES/BOLIK) a las que el
        /// usuario tiene acceso según Usuario_Empresa (POS-SmartK66_DEV).
        ///
        /// Reutiliza UsuarioEmpresaDA/BL que ya existen. Filtra fuera EMPAQUES
        /// (...002) y cualquier otra empresa que recibos no maneja, y deduplica
        /// (Usuario_Empresa puede traer varias filas por empresa, una por agente).
        /// </summary>
        public List<dynamic> ObtenerEmpresasUsuario(long usuarioId)
        {
            // IDs numéricos que recibos sí maneja → su clave string para el front
            var permitidas = new Dictionary<long, string>
    {
        { UsuarioEmpresaBL.ID_GRACO, "GRACO" },
        { UsuarioEmpresaBL.ID_FAES,  "FAES"  },
        { UsuarioEmpresaBL.ID_BOLIK, "BOLIK" }
    };

            var nombres = new Dictionary<string, string>
    {
        { "GRACO", "Graco Pack"       },
        { "FAES",  "Fabrica Escocesa" },
        { "BOLIK", "Industrias Bolik" }
    };

            var registros = new UsuarioEmpresaDA().ObtenerPorUsuarioId(usuarioId);

            // Deduplicar por EmpresaId y quedarnos solo con las permitidas
            var idsUnicos = registros
                .Select(r => r.EmpresaId)
                .Where(id => permitidas.ContainsKey(id))
                .Distinct();

            var resultado = new List<dynamic>();
            foreach (var id in idsUnicos)
            {
                string clave = permitidas[id];
                resultado.Add(new { Id = clave, Nombre = nombres[clave] });
            }
            return resultado;
        }

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
        // ─── DOCUMENTOS ───────────────────────────────
        /// <summary>
        /// Enruta la fuente según el tipo:
        ///   FACTURA / PEDIDO → SAP HANA (vista RC_FACTURAS_REC_CAJ)
        ///   el resto         → APK66 (MA_RECC_DOCTOS), como antes.
        /// El controller y el front no cambian: misma firma, mismo DocumentoRecibo.
        /// </summary>
        public List<DocumentoRecibo> ObtenerDocumentos(string empresa, string clienteId, string tipoDoc)
        {
            var tipo = (tipoDoc ?? "").Trim().ToUpper();

            if (tipo == "FACTURA" || tipo == "PEDIDO")
                return _hana.ObtenerFacturas(empresa, clienteId, tipo);

            return _apk.ObtenerDocumentos(empresa, clienteId, tipo);
        }

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
        /// [LEGACY] Catálogo fijo de las 3 empresas. Mantener por si algo lo usa,
        /// pero la vista ahora se llena con ObtenerEmpresasUsuario (filtrado por permiso).
        /// </summary>
        public List<dynamic> ObtenerEmpresas()
        {
            return new List<dynamic>
    {
        new { Id = "GRACO", Nombre = "Graco Pack",       Permiso = "Control.ReciboCaja.Graco", Clase = "empresa-graco" },
        new { Id = "FAES",  Nombre = "Fabrica Escocesa", Permiso = "Control.ReciboCaja.Faes",  Clase = "empresa-faes"  },
        new { Id = "BOLIK", Nombre = "Industrias Bolik", Permiso = "Control.ReciboCaja.Bolik", Clase = "empresa-bolik" }
    };
        }
    }
}