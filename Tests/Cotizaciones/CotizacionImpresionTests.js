"use strict";

const assert = require("assert");
const fs = require("fs");
const path = require("path");

const raiz = path.resolve(__dirname, "..", "..");
const vistaCrear = fs.readFileSync(
    path.join(raiz, "DiamDev.Give.UI", "Views", "Cotizacion", "Index.cshtml"),
    "utf8");
const vistaImprimir = fs.readFileSync(
    path.join(raiz, "DiamDev.Give.UI", "Views", "Cotizacion", "Imprimir.cshtml"),
    "utf8");
const javascript = fs.readFileSync(
    path.join(raiz, "DiamDev.Give.UI", "Scripts", "App", "Cotizacion-Index.js"),
    "utf8");
const controlador = fs.readFileSync(
    path.join(raiz, "DiamDev.Give.UI", "Controllers", "CotizacionController.cs"),
    "utf8");

assert(!vistaCrear.includes('id="cotCondicionesPago"') &&
       !vistaCrear.includes('id="cotTiempoEntrega"'),
    "La creación no debe mostrar condiciones de pago ni tiempo de entrega.");
assert(vistaCrear.includes('cot-field cot-col-12') &&
       vistaCrear.includes('id="cotObservaciones"'),
    "Observaciones debe conservarse y ocupar el ancho disponible.");
assert(!javascript.includes("cotCondicionesPago") &&
       !javascript.includes("cotTiempoEntrega"),
    "El navegador no debe intentar leer controles ocultos o eliminados.");
assert(javascript.includes("<small>Observaciones</small>") &&
       !javascript.includes("<small>Pago</small>") &&
       !javascript.includes("<small>Entrega</small>"),
    "El detalle debe mostrar observaciones y ocultar pago/entrega.");

assert(!vistaImprimir.includes(">Desc.</th>") &&
       !vistaImprimir.includes("d.DescuentoPorcentaje"),
    "La impresión no debe renderizar la columna ni los valores de descuento.");
assert(!vistaImprimir.includes("Importe bruto") &&
       !vistaImprimir.includes("Model.ImporteBruto") &&
       !vistaImprimir.includes("Model.DescuentoTotal"),
    "El resumen impreso no debe renderizar bruto ni descuento.");
assert(!vistaImprimir.includes("Condiciones de pago") &&
       !vistaImprimir.includes("Tiempo de entrega") &&
       vistaImprimir.includes(">Observaciones</span>"),
    "La impresión debe conservar únicamente Observaciones.");
assert(vistaImprimir.includes("ViewBag.TotalEnLetras") &&
       vistaImprimir.includes('class="amount-words"') &&
       controlador.includes("CotizacionBLL.TotalEnLetras"),
    "El total en letras debe calcularse en servidor y mostrarse en el documento.");
assert(vistaImprimir.includes("Model.Agente") &&
       !vistaImprimir.includes("Agente / asesor comercial"),
    "La firma debe usar el agente persistido en la cotización.");

console.log("OK: contrato simplificado de creación, detalle e impresión verificado.");
