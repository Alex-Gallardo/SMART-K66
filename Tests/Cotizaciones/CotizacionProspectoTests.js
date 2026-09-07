"use strict";

const assert = require("assert");
const fs = require("fs");
const path = require("path");

const raiz = path.resolve(__dirname, "..", "..");
const leer = (...partes) => fs.readFileSync(path.join(raiz, ...partes), "utf8");
const vista = leer("DiamDev.Give.UI", "Views", "Cotizacion", "Index.cshtml");
const javascript = leer("DiamDev.Give.UI", "Scripts", "App", "Cotizacion-Index.js");
const controlador = leer("DiamDev.Give.UI", "Controllers", "CotizacionController.cs");
const bll = leer("DiamDev.Give.BLL", "CotizacionBLL.cs");
const hana = leer("DiamDev.Give.DAL", "HanaRepository.cs");
const da = leer("DiamDev.Give.DAL", "CotizacionDA.cs");

function bloque(texto, inicio, siguiente) {
    const desde = texto.indexOf(inicio);
    assert(desde >= 0, `No se encontró el bloque ${inicio}.`);
    const hasta = texto.indexOf(siguiente, desde + inicio.length);
    assert(hasta > desde, `No se encontró el final de ${inicio}.`);
    return texto.substring(desde, hasta);
}

function sinComentarios(texto) {
    return texto
        .replace(/\/\*[\s\S]*?\*\//g, "")
        .replace(/\/\/[^\r\n]*/g, "");
}

const abrirProductos = bloque(
    javascript, "function abrirProductos()", "function cancelarBusquedaProductos()");
const validarNavegador = bloque(
    javascript, "function validar()", "function guardar()");
const guardarNavegador = bloque(
    javascript, "function guardar()", "function resetFormulario(");
const buscarProductosControlador = bloque(
    controlador, "public JsonResult BuscarProductos(", "[CotizacionPermiso(PermisoVer)]");
const buscarProductosBll = bloque(
    bll, "public PaginaProductosCotizacionHana BuscarProductos(",
    "public ProductoCotizacionHana ObtenerPrecioProducto(");
const guardarBll = bloque(
    bll, "public ResultadoCotizacion Guardar(", "public static void CalcularLinea(");
const validarEncabezado = bloque(
    bll, "private static string ValidarEncabezado(", "private static string ValidarLinea(");
const productosHana = bloque(
    hana, "// PRODUCTOS PARA COTIZACIONES", "// ── Helpers privados");

// UI: el código SAP es opcional, los datos comerciales siguen editables y el
// usuario cuenta con una acción explícita para iniciar/cambiar a prospecto.
const etiquetaCodigo = vista.match(/<label[^>]*for="cotClienteCodigo"[^>]*>([\s\S]*?)<\/label>/i);
assert(etiquetaCodigo && !/<span[^>]*>\s*\*\s*<\/span>/i.test(etiquetaCodigo[1]),
    "El código SAP no debe mostrarse como obligatorio para un prospecto.");
assert(vista.includes('id="cotNuevoProspecto"'),
    "La vista debe ofrecer una acción explícita para capturar un prospecto.");
["cotClienteNombre", "cotNit", "cotCorreo", "cotDireccion"].forEach(id => {
    const input = vista.match(new RegExp(`<input[^>]*id="${id}"[^>]*>`, "i"));
    assert(input && !/\b(readonly|disabled)\b/i.test(input[0]),
        `${id} debe permanecer editable para el prospecto.`);
});

// Catálogo: requiere empresa y agente, pero no un registro OCRD seleccionado.
const contextoSinCliente = abrirProductos.match(
    /if\s*\(\s*!estado\.cliente\s*\)\s*\{([\s\S]*?)\}/);
assert(!abrirProductos.includes("Seleccione un cliente") &&
       (!contextoSinCliente || !/\breturn\s*;/.test(contextoSinCliente[1])),
    "Abrir el catálogo puede activar el prospecto, pero no debe bloquearlo.");
assert(abrirProductos.includes("empresa()") &&
       abrirProductos.includes("codigoOperador()"),
    "El catálogo debe conservar la validación de empresa y agente autorizados.");
assert(javascript.includes(
    'clienteId: estado.cliente ? estado.cliente.CardCode : ""'),
    "La búsqueda debe enviar un cliente vacío cuando se cotiza a un prospecto.");
assert(!buscarProductosControlador.includes("IsNullOrWhiteSpace(clienteId)") &&
       !buscarProductosControlador.includes("Seleccione un cliente"),
    "El controlador no debe rechazar búsquedas de productos sin cliente.");

// Guardado: nombre sí es obligatorio; IdCliente es condicional y el precio
// manual positivo es el dato comercial usado en el flujo sin lista SAP.
assert(!validarNavegador.includes("!estado.cliente") &&
       validarNavegador.includes("cotClienteNombre") &&
       validarNavegador.includes("Ingrese el nombre del cliente"),
    "El navegador debe exigir nombre, no una selección SAP.");
assert(/IdCliente\s*:\s*estado\.cliente\s*\?\s*estado\.cliente\.CardCode\s*:\s*""/.test(
    guardarNavegador),
    "El payload debe enviar IdCliente vacío de forma segura para prospectos.");
assert(validarNavegador.includes("numero(x.PrecioUnitario) <= 0") &&
       guardarNavegador.includes("].PrecioUnitario"),
    "El precio manual debe viajar al servidor y ser mayor que cero.");
assert(!/IsNullOrWhiteSpace\(enc\.IdCliente\)[\s\S]{0,120}(cliente|obligator)/i.test(
    validarEncabezado) &&
       /IsNullOrWhiteSpace\(enc\.NombreCliente\)/.test(validarEncabezado),
    "El BLL debe aceptar IdCliente vacío y seguir exigiendo NombreCliente.");
assert(/IsNullOrWhiteSpace\(enc\.IdCliente\)/.test(guardarBll) &&
       /ObtenerClienteAsignado/.test(guardarBll),
    "Guardar debe validar en SAP únicamente cuando se recibió un código de cliente.");
assert(/IsNullOrWhiteSpace\(clienteId\)/.test(buscarProductosBll),
    "El BLL debe distinguir explícitamente el catálogo de un prospecto.");
assert(da.includes(
    'Add("@idCliente", SqlDbType.NVarChar, 20).Value = Nulo(enc.IdCliente)'),
    "La persistencia debe convertir IdCliente vacío en NULL de SQL Server.");

// HANA: el contexto sin OCRD debe ser válido, manual y usar exactamente las
// reglas de visibilidad ya acordadas (SellItem=Y, incluso con stock cero).
assert(!/if\s*\(string\.IsNullOrWhiteSpace\(clienteId\)\)[\s\S]{0,100}return/.test(
    productosHana),
    "HANA no debe devolver un catálogo vacío sólo porque falta clienteId.");
assert(/codigoCliente/i.test(productosHana) && /FROM DUMMY/i.test(productosHana),
    "La consulta HANA debe modelar explícitamente el contexto sin cliente SAP.");
assert(productosHana.includes('I.""SellItem"" = \'Y\''),
    "El catálogo de prospecto debe incluir sólo productos de venta.");
const desdeVisibilidad = productosHana.indexOf('WHERE I.""SellItem"" = \'Y\'');
const hastaVisibilidad = productosHana.indexOf(
    'ORDER BY I.""ItemCode""', desdeVisibilidad);
assert(desdeVisibilidad >= 0 && hastaVisibilidad > desdeVisibilidad,
    "No se encontró el tramo de visibilidad HANA.");
assert(!/(OnHand|IsCommited|Disponible)[^\r\n]*(>|>=|<>)[^\r\n]*0/i.test(
    productosHana.substring(desdeVisibilidad, hastaVisibilidad)),
    "Los productos con stock cero deben seguir visibles.");
assert(/SIN_PRECIO/.test(productosHana) &&
       /(Precio\s*=\s*0m|PrecioBruto\s*=\s*0m|ForzarPrecioManual|PrepararPreciosManuales|SinPrecio)/i.test(
           buscarProductosBll + guardarBll),
    "Sin cliente, el catálogo debe declarar precio manual y no inventar una lista SAP.");

// La migración es reejecutable y cambia sólo la nulabilidad de ID_CLIENTE.
const carpetaMigraciones = path.join(raiz, "SqlMigrations", "Cotizaciones");
const migracionesProspecto = fs.readdirSync(carpetaMigraciones)
    .filter(nombre => /^04.*\.sql$/i.test(nombre));
assert.strictEqual(migracionesProspecto.length, 1,
    "Debe existir una única migración 04 para habilitar prospectos.");
const migracion = leer("SqlMigrations", "Cotizaciones", migracionesProspecto[0]);
assert(/USE\s+\[POS-SmartK66\]/i.test(migracion),
    "La migración debe apuntar explícitamente a POS-SmartK66.");
assert(/ALTER\s+TABLE\s+(?:dbo\.)?COT_ENC[\s\S]*ALTER\s+COLUMN\s+ID_CLIENTE\s+nvarchar\(20\)\s+NULL/i.test(
    migracion),
    "La migración debe permitir NULL en COT_ENC.ID_CLIENTE.");
assert(/BEGIN\s+TRANSACTION/i.test(migracion) &&
       /COMMIT\s+TRANSACTION/i.test(migracion) &&
       /ROLLBACK\s+TRANSACTION/i.test(migracion),
    "La migración debe ser transaccional.");
assert(/is_nullable/i.test(migracion),
    "La migración debe verificar la nulabilidad resultante.");
assert(!/\b(DROP|TRUNCATE|DELETE)\b/i.test(sinComentarios(migracion)),
    "La migración no debe borrar objetos ni datos.");

// No regresión fiscal: este cambio no altera la fuente, edición ni cálculo de IVA.
assert(javascript.includes(
    'readonly data-field="ImpuestoPorcentaje"') &&
       validarNavegador.includes("numero(x.ImpuestoPorcentaje) < 0") &&
       validarNavegador.includes("numero(x.ImpuestoPorcentaje) > 100"),
    "IVA debe seguir siendo de solo lectura y conservar su validación de rango.");
assert(bll.includes("d.ImpuestoPorcentaje = producto.ImpuestoPorcentaje;") &&
       bll.includes("d.Subtotal * d.ImpuestoPorcentaje / 100m"),
    "El servidor debe conservar el IVA de SAP y la fórmula fiscal existente.");
assert(productosHana.includes('LEFT JOIN ""{0}"".""OSTC"" T') &&
       productosHana.includes("COALESCE(C.\"\"VatStatus\"\", 'Y')='N'") &&
       productosHana.includes("THEN 'EXE' ELSE 'IVA' END") &&
       productosHana.includes('COALESCE(T.""Rate"", 0) AS ""ImpuestoPorcentaje""'),
    "HANA debe conservar OSTC y la determinación IVA/EXE existente.");

console.log("OK: prospecto sin cliente SAP, catálogo, precio manual, SQL e IVA verificados.");
