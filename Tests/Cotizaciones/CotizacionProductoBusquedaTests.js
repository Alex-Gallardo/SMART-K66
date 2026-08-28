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
const bll = fs.readFileSync(
    path.join(raiz, "DiamDev.Give.BLL", "CotizacionBLL.cs"),
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

assert(consultaProductos.includes('COALESCE(ECQ.""Price"", 0)>0'),
    "Un precio especial cero no debe bloquear las fuentes posteriores.");
assert(consultaProductos.includes('P.""AddPrice1""') &&
       consultaProductos.includes('P.""AddPrice2""'),
    "La lista debe considerar las monedas adicionales configuradas en ITM1.");
assert(!consultaProductos.includes('WHEN P.""Price"" IS NOT NULL THEN \'LISTA\''),
    "Una fila ITM1 con precio cero no debe etiquetarse como Lista de precios.");
assert(consultaProductos.includes("ELSE 'SIN_PRECIO' END"),
    "Los artículos sin fuente positiva deben identificarse explícitamente.");
assert(javascript.includes('Sin precio SAP') &&
       javascript.includes('Se requiere precio manual'),
    "La interfaz debe diferenciar un producto genuinamente sin precio SAP.");
assert(javascript.includes('numero(x.PrecioUnitario) <= 0'),
    "La interfaz no debe permitir guardar accidentalmente un precio cero.");
assert(javascript.includes("' · precio manual'"),
    "El detalle debe identificar precios capturados manualmente sin referencia SAP.");
assert(bll.includes('d.PrecioUnitario <= 0m'),
    "El servidor también debe rechazar precios netos en cero.");

console.log("OK: búsqueda, SellItem, stock cero y precios SAP verificados.");
