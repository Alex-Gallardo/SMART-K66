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

const inicioBusqueda = bll.indexOf("public List<FacturaBorradorNc> BuscarFacturas(");
const finBusqueda = bll.indexOf("public FacturaBorradorNc ObtenerEstadoFactura(", inicioBusqueda);
assert(inicioBusqueda >= 0 && finBusqueda > inicioBusqueda,
    "No se encontró la búsqueda de facturas del módulo.");

const busqueda = bll.substring(inicioBusqueda, finBusqueda);
assert(busqueda.includes("facturas.Where(EsFacturaAbierta).ToList()"),
    "El modal debe filtrar las facturas mediante EsFacturaAbierta.");
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
    "El estado vacío debe explicar el filtro temporal.");

const abierta = (pagado, total) => pagado < total;
assert.strictEqual(abierta(0, 100), true, "sin pagos");
assert.strictEqual(abierta(99.999999, 100), true, "diferencia mínima");
assert.strictEqual(abierta(100, 100), false, "pagada exactamente");
assert.strictEqual(abierta(101, 100), false, "sobrepagada");

console.log("OK: el modal muestra únicamente facturas con saldo pendiente exacto.");
