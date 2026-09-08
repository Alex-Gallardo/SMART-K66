"use strict";

const assert = require("assert");
const fs = require("fs");
const path = require("path");

const raiz = path.resolve(__dirname, "..", "..");
const leer = (...partes) => fs.readFileSync(path.join(raiz, ...partes), "utf8");
const vista = leer("DiamDev.Give.UI", "Views", "Cotizacion", "Index.cshtml");
const javascript = leer("DiamDev.Give.UI", "Scripts", "App", "Cotizacion-Index.js");
const estilos = leer("DiamDev.Give.UI", "Content", "cotizaciones.css");
const controlador = leer("DiamDev.Give.UI", "Controllers", "CotizacionController.cs");

function bloque(texto, inicio, siguiente) {
    const desde = texto.indexOf(inicio);
    assert(desde >= 0, `No se encontró ${inicio}.`);
    const hasta = texto.indexOf(siguiente, desde + inicio.length);
    assert(hasta > desde, `No se encontró el final de ${inicio}.`);
    return texto.substring(desde, hasta);
}

const input = vista.match(/<input[^>]*id="cotClienteCodigo"[^>]*>/i);
assert(input && !/\breadonly\b/i.test(input[0]),
    "cotClienteCodigo debe permitir escritura directa.");
["autocomplete=\"off\"", "role=\"combobox\"", "aria-autocomplete=\"list\"",
    "aria-controls=\"cotClienteDropdown\"", "aria-expanded=\"false\""]
    .forEach(atributo => assert(input[0].includes(atributo),
        `Falta ${atributo} en cotClienteCodigo.`));
assert(vista.includes('id="cotClienteCombobox"') &&
       vista.includes('id="cotClienteDropdown"') &&
       /id="cotClienteDropdown"[\s\S]*?role="listbox"/i.test(vista),
    "La vista debe incluir el listbox asociado al combobox.");

const disponibilidad = bloque(javascript,
    "function actualizarDisponibilidadCliente()", "function limpiarCliente()");
assert(disponibilidad.includes('!empresa() || !codigoOperador()') &&
       disponibilidad.includes('#cotBuscarCliente,#cotClienteCodigo') &&
       disponibilidad.includes('.prop("disabled", deshabilitado)'),
    "Código y botón sólo deben habilitarse con empresa y agente.");

const programar = bloque(javascript,
    "function programarBusquedaClientes(origen)", "function restaurarClienteSeleccionado()");
assert(programar.includes("filtro.length < 2"),
    "La búsqueda directa debe exigir dos caracteres.");
assert(programar.includes("}, 350);"),
    "La búsqueda debe usar el debounce de 350 ms de BorradorNc.");
assert(programar.includes("solicitud !== estado.clienteSolicitud") &&
       programar.includes("empresa() !== empresaConsulta") &&
       programar.includes("codigoOperador() !== operadorConsulta"),
    "Respuestas antiguas o de otro contexto deben ignorarse.");
assert(programar.includes("empresa: empresaConsulta") &&
       programar.includes("codigoOperador: operadorConsulta") &&
       programar.includes("filtro: filtro"),
    "La consulta debe conservar empresa, agente y filtro.");

assert(javascript.includes('$("#cotClienteCodigo").on("input", function () { programarBusquedaClientes("directo"); })') &&
       javascript.includes('$("#cotClienteFiltro").on("input", function () { programarBusquedaClientes("modal"); })'),
    "El input directo y el modal deben compartir la misma búsqueda.");
assert(javascript.includes('$("#cotClienteDropdown .cot-client-result:first").trigger("click")') &&
       javascript.includes("e.which === 27") &&
       javascript.includes("click.cotizacionCliente") &&
       javascript.includes("restaurarClienteSeleccionado()"),
    "Enter, Escape y clic exterior deben seguir el comportamiento de BorradorNc.");

const seleccionar = bloque(javascript,
    "function seleccionarCliente(c)", "function abrirClientes()");
["cotClienteCodigo", "cotClienteNombre", "cotNit", "cotCorreo", "cotDireccion", "cotMoneda"]
    .forEach(id => assert(seleccionar.includes(`#${id}`),
        `Seleccionar cliente debe actualizar ${id}.`));
assert(seleccionar.includes("window.confirm") &&
       seleccionar.includes("estado.lineas = []") &&
       seleccionar.includes("return false"),
    "Cambiar cliente con productos debe solicitar confirmación.");

assert(estilos.includes(".cot-client-combobox") &&
       estilos.includes(".cot-client-dropdown") &&
       estilos.includes(".cot-client-results.is-visible") &&
       estilos.includes(".cot-client-result:hover") &&
       estilos.includes("@media (max-width: 767px)") &&
       estilos.includes(".cot-client-dropdown { min-width: 0; }"),
    "El desplegable debe conservar interacción visual y adaptación móvil.");

const buscarClientes = bloque(controlador,
    "public JsonResult BuscarClientes(", "[HttpGet]");
assert(buscarClientes.includes("ResolverAgente(empresa, codigoOperador)") &&
       buscarClientes.includes("filtro ?? \"\""),
    "La búsqueda debe continuar validando empresa y agente en el servidor.");

assert(!bloque(javascript, "function validar()", "function guardar()")
        .includes("!estado.cliente"),
    "El combobox no debe volver obligatorio al cliente SAP para prospectos.");
assert(javascript.includes('IdCliente: estado.cliente ? estado.cliente.CardCode : ""'),
    "El guardado de prospectos debe seguir enviando IdCliente vacío.");

console.log("OK: combobox de clientes de Cotizaciones equivalente a BorradorNc.");
