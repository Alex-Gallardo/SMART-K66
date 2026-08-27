"use strict";

const assert = require("assert");
const fs = require("fs");
const path = require("path");

const raiz = path.resolve(__dirname, "..", "..");
const javascript = fs.readFileSync(
    path.join(raiz, "DiamDev.Give.UI", "Scripts", "App", "Cotizacion-Index.js"),
    "utf8");
const hana = fs.readFileSync(
    path.join(raiz, "DiamDev.Give.DAL", "HanaRepository.cs"),
    "utf8");

const inicioProductos = hana.indexOf("// PRODUCTOS PARA COTIZACIONES");
const finProductos = hana.indexOf("// ── Helpers privados", inicioProductos);
assert(inicioProductos >= 0 && finProductos > inicioProductos,
    "No se encontró la sección HANA de productos para cotizaciones.");

const consultaProductos = hana.substring(inicioProductos, finProductos);

assert(javascript.includes('$("#cotProductoFiltro").on("input"'),
    "El filtro de productos debe reaccionar al evento input.");
assert(javascript.includes("estado.productoRequest.abort()"),
    "La búsqueda debe cancelar la solicitud anterior.");
assert(javascript.includes("solicitud !== estado.productoSolicitud"),
    "La búsqueda debe ignorar respuestas AJAX obsoletas.");
assert(javascript.includes("}, 300);"),
    "La búsqueda debe aplicar el debounce acordado de 300 ms.");

assert(consultaProductos.includes('I.""SellItem"" = \'Y\''),
    "La consulta debe mostrar únicamente artículos con SellItem=Y.");
assert(!consultaProductos.includes('I.""validFor""'),
    "validFor no debe determinar la visibilidad comercial del artículo.");
assert(!/ItemCode[^\r\n]*(LIKE|LEFT|SUBSTRING)[^\r\n]*PT/i.test(consultaProductos),
    "La consulta no debe limitar los productos al prefijo PT.");

const inicioVisibilidad = consultaProductos.indexOf('WHERE I.""SellItem"" = \'Y\'');
const finVisibilidad = consultaProductos.indexOf(
    'ORDER BY I.""ItemCode""', inicioVisibilidad);
assert(inicioVisibilidad >= 0 && finVisibilidad > inicioVisibilidad,
    "No se encontró el filtro de visibilidad de productos.");

const filtroVisibilidad = consultaProductos.substring(
    inicioVisibilidad, finVisibilidad);
assert(!/(OnHand|IsCommited|Disponible)/i.test(filtroVisibilidad),
    "El stock cero no debe excluir ni bloquear productos con SellItem=Y.");

console.log("OK: búsqueda reactiva, SellItem y stock cero verificados.");
