"use strict";

const assert = require("assert");
const fs = require("fs");
const path = require("path");

const raiz = path.resolve(__dirname, "..", "..");
const leer = (...partes) => fs.readFileSync(path.join(raiz, ...partes), "utf8");
const bll = leer("DiamDev.Give.BLL", "CotizacionBLL.cs");
const javascript = leer("DiamDev.Give.UI", "Scripts", "App", "Cotizacion-Index.js");
const vista = leer("DiamDev.Give.UI", "Views", "Cotizacion", "Index.cshtml");
const impresion = leer("DiamDev.Give.UI", "Views", "Cotizacion", "Imprimir.cshtml");

function bloque(texto, inicio, siguiente) {
    const desde = texto.indexOf(inicio);
    assert(desde >= 0, `No se encontró ${inicio}.`);
    const hasta = texto.indexOf(siguiente, desde + inicio.length);
    assert(hasta > desde, `No se encontró el final de ${inicio}.`);
    return texto.substring(desde, hasta);
}

const calculoServidor = bloque(
    bll, "public static void CalcularLinea(", "public static string TotalEnLetras(");
const preciosSap = bloque(
    bll, "private static void UsarPreciosFinalesSap(",
    "private static void PrepararPreciosManuales(");
const calculoNavegador = bloque(
    javascript, "function calcularLinea(x)", "function renderLineas()");

assert(preciosSap.includes("producto.Precio = producto.PrecioBruto"),
    "El precio mostrado debe conservar el valor final entregado por SAP.");
assert(!bll.includes("PrecioNetoDesdeBruto"),
    "Cotizaciones no debe retirar IVA del precio SAP.");

assert(calculoServidor.includes("d.ImpuestoMonto = 0m") &&
       calculoServidor.includes("d.Total = d.Subtotal") &&
       !calculoServidor.includes("ImpuestoPorcentaje / 100m"),
    "El servidor no debe calcular ni sumar IVA sobre el precio final.");
assert(calculoNavegador.includes("impuesto: 0") &&
       calculoNavegador.includes("total: subtotal") &&
       !calculoNavegador.includes("ImpuestoPorcentaje"),
    "La vista previa debe usar la misma fórmula sin IVA adicional.");

assert(vista.includes("Precio con IVA") &&
       vista.includes("Total con IVA") &&
       !vista.includes('id="cotImpuesto"'),
    "La interfaz debe explicar que los valores ya incluyen IVA.");
assert(javascript.includes("IVA incluido ·") &&
       javascript.includes("precio final con IVA"),
    "Catálogo y captura manual deben usar la semántica de precio final.");

assert(impresion.includes("precioFinalConIva") &&
       impresion.includes("Los precios e importes ya incluyen IVA") &&
       impresion.includes("Documento histórico con IVA calculado por separado"),
    "La impresión debe distinguir documentos nuevos e históricos.");

console.log("OK: Cotizaciones conserva precios con IVA y no agrega impuesto adicional.");
