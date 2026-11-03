"use strict";

const assert = require("assert");
const fs = require("fs");
const path = require("path");

const raiz = path.resolve(__dirname, "..", "..");
const bll = fs.readFileSync(
    path.join(raiz, "DiamDev.Give.BLL", "BorradorNcBLL.cs"), "utf8");
const webConfig = fs.readFileSync(
    path.join(raiz, "DiamDev.Give.UI", "Web.config"), "utf8");
const vista = fs.readFileSync(
    path.join(raiz, "DiamDev.Give.UI", "Views", "BorradorNc", "Index.cshtml"), "utf8");
const javascript = fs.readFileSync(
    path.join(raiz, "DiamDev.Give.UI", "Scripts", "App", "BorradorNc-Index.js"), "utf8");
const repositorioHana = fs.readFileSync(
    path.join(raiz, "DiamDev.Give.DAL", "HanaRepository.cs"), "utf8");
const autorizaciones = fs.readFileSync(
    path.join(raiz, "DiamDev.Give.UI", "Scripts", "App", "BorradorNc-Autorizaciones.js"), "utf8");
const migracionHana = fs.readFileSync(
    path.join(raiz, "SqlMigrations", "BorradoresNC",
        "09_exportacion_todas_desde_enero_hana.sql"), "utf8");
const correccionHana = fs.readFileSync(
    path.join(raiz, "SqlMigrations", "BorradoresNC",
        "10_corregir_pagado_moneda_factura_hana.sql"), "utf8");

const inicioBusqueda = bll.indexOf("public List<FacturaBorradorNc> BuscarFacturas(");
const finBusqueda = bll.indexOf("public FacturaBorradorNc ObtenerEstadoFactura(", inicioBusqueda);
assert(inicioBusqueda >= 0 && finBusqueda > inicioBusqueda,
    "No se encontró la búsqueda de facturas del módulo.");

const busqueda = bll.substring(inicioBusqueda, finBusqueda);
assert(busqueda.includes("facturas.Where(EsFacturaAbierta).ToList()"),
    "Los agentes ordinarios deben filtrar las facturas mediante EsFacturaAbierta.");
assert(busqueda.includes('"EXPORTACION"') &&
       busqueda.includes("else if (!MostrarFacturasPagadas)"),
    "EXPORTACION debe omitir el filtro de pagadas sin cambiar a los demás agentes.");
assert(busqueda.includes("new DateTime(hoy.Year, 1, 1)") &&
       busqueda.includes("f.DocDate.Date >= desde") &&
       busqueda.includes("f.DocDate.Date <= hoy"),
    "EXPORTACION debe limitarse desde enero hasta hoy.");
assert(busqueda.indexOf("facturas.Where(EsFacturaAbierta).ToList()") <
       busqueda.indexOf("var docs = facturas.Select"),
    "Las pagadas deben excluirse antes de consultar acumulados y NC previas.");

const inicioRegla = bll.indexOf("private static bool EsFacturaAbierta(");
const finRegla = bll.indexOf("public FacturaBorradorNc ObtenerEstadoFactura(", inicioRegla);
assert(inicioRegla >= 0 && finRegla > inicioRegla,
    "No se encontró la regla de factura abierta.");

const regla = bll.substring(inicioRegla, finRegla);
assert(regla.includes("factura.Pagado < factura.DocTotal"),
    "Cualquier diferencia pendiente debe conservar la factura abierta.");
assert(!regla.includes("TOLERANCIA"),
    "El filtro del modal no debe aplicar tolerancia monetaria.");

assert(webConfig.includes('key="BorradorNC.MostrarFacturasPagadas" value="false"'),
    "Las facturas pagadas deben permanecer ocultas por configuración.");
assert(vista.includes("Facturas abiertas disponibles") &&
       vista.includes("Facturas abiertas de SAP"),
    "El modal debe explicar que muestra facturas abiertas.");
assert(javascript.includes("Sin facturas abiertas disponibles") &&
       javascript.includes("Solo se muestran facturas con saldo pendiente"),
    "El estado vacío de los demás agentes debe explicar el filtro de saldo.");
assert(javascript.includes("Facturas de EXPORTACION desde enero") &&
       javascript.includes("Sin facturas desde enero") &&
       javascript.includes("facturas desde enero"),
    "El modal de EXPORTACION debe describir su alcance real.");
assert((migracionHana.match(/^CREATE OR REPLACE VIEW/gm) || []).length === 3 &&
       (migracionHana.match(/T1\."SlpName" = 'EXPORTACION' AND T0\."DocCur" <> 'QTZ' THEN T0\."PaidFC"/g) || []).length === 3 &&
       (migracionHana.match(/T0\."DocDate" < ADD_DAYS\(CURRENT_DATE, 1\)/g) || []).length === 3,
    "La migración 09 histórica debe conservar su alcance de EXPORTACION.");

const definiciones = sql => sql.slice(sql.search(/^CREATE OR REPLACE VIEW /m),
    sql.indexOf("/* Debe devolver tres vistas válidas. */"));
const pagoAnterior = definiciones(migracionHana);
const pagoCorregido = pagoAnterior
    .replace(/            T0\."PaidToDate"(?=\s+FROM)/g,
        '            CASE WHEN T0."DocCur" <> \'QTZ\' THEN T0."PaidFC"\n                 ELSE T0."PaidToDate" END AS "PaidToDate"')
    .replace(/CASE WHEN T1\."SlpName" = 'EXPORTACION' AND T0\."DocCur" <> 'QTZ' THEN T0\."PaidFC"/g,
        'CASE WHEN T0."DocCur" <> \'QTZ\' THEN T0."PaidFC"');
assert.strictEqual(definiciones(correccionHana).replace(/\r\n/g, "\n"),
    pagoCorregido.replace(/\r\n/g, "\n"),
    "La corrección solo debe cambiar el pago; filtros, fechas y totales se conservan.");
assert.strictEqual((correccionHana.match(/CASE WHEN T0\."DocCur" <> 'QTZ' THEN T0\."PaidFC"/g) || []).length, 6,
    "Las dos ramas de cada sociedad deben leer PaidFC en moneda extranjera.");
assert(repositorioHana.includes('DocTotal = LeerDecimal(row, "DocTotal")') &&
       repositorioHana.includes('Pagado = LeerDecimal(row, "PaidToDate")') &&
       bll.includes("detalle.Pagado = facturaSap.Pagado") &&
       bll.includes("if (d.Pagado >= d.TotalFactura)") &&
       javascript.includes("if (f.GeneraSaldoAFavor)") &&
       autorizaciones.includes("numero(d.Pagado) >= numero(d.TotalFactura)"),
    "Selector, guardado y autorización deben consumir la misma pareja de la vista.");

const abierta = (pagado, total) => pagado < total;
assert.strictEqual(abierta(0, 100), true, "sin pagos");
assert.strictEqual(abierta(99.999999, 100), true, "diferencia mínima");
assert.strictEqual(abierta(100, 100), false, "pagada exactamente");
assert.strictEqual(abierta(101, 100), false, "sobrepagada");
assert.strictEqual(abierta(22_473.04, 30_000), true,
    "un pago parcial en USD no se debe comparar con su equivalente en GTQ");
assert.strictEqual(abierta(30_000, 30_000), false, "factura extranjera pagada por completo");

console.log("OK: pago y total en la misma moneda para todos los agentes; filtros y avisos conservados.");
