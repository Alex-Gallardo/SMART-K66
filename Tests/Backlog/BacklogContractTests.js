"use strict";

const assert = require("assert");
const fs = require("fs");
const path = require("path");

const root = path.resolve(__dirname, "..", "..");
const ui = path.join(root, "DiamDev.Give.UI");

const controller = fs.readFileSync(
    path.join(ui, "Controllers", "BacklogController.cs"), "utf8");
const sql = fs.readFileSync(
    path.join(ui, "App_Data", "backlog.sql"), "utf8");
const view = fs.readFileSync(
    path.join(ui, "Views", "Backlog", "Index.cshtml"), "utf8");
const javascript = fs.readFileSync(
    path.join(ui, "Scripts", "App", "backlog-viz.js"), "utf8");
const project = fs.readFileSync(
    path.join(ui, "DiamDev.Give.UI.csproj"), "utf8");

assert(controller.includes("[Authorize]"),
    "Backlog debe requerir una sesión autenticada.");
assert(!controller.includes("[Permiso") && !controller.includes("[Seguridad"),
    "Backlog no debe exigir permisos funcionales adicionales.");
assert(controller.includes("DateTime.TryParseExact") &&
       controller.includes("< MaxRangeDays") &&
       controller.includes("MaxRangeDays = 1096"),
    "El controlador debe validar formato, orden y tamaño del rango.");
assert(controller.includes("OdbcConnectionStringBuilder") &&
       controller.includes('connectionBuilder["CS"] = companySchema') &&
       !controller.includes('baseConnString.Replace'),
    "El schema debe cambiarse mediante el constructor ODBC.");
assert(controller.includes("TryGetCompanySchema") &&
       controller.includes("companySchema = null") &&
       controller.includes("return false;"),
    "Una empresa desconocida debe rechazarse.");
assert(controller.includes('Server.MapPath("~/App_Data/backlog.sql")'),
    "La consulta debe estar protegida dentro de App_Data.");
assert(controller.includes("CommandTimeoutSeconds") &&
       controller.includes("MaxJsonLengthBytes") &&
       controller.includes("SetNoStore"),
    "La API debe limitar ejecución/respuesta y deshabilitar caché.");
assert(controller.includes("Trace.TraceError") &&
       !controller.includes("new { error = ex"),
    "Los detalles internos no deben devolverse al navegador.");

const sqlCode = sql
    .split(/\r?\n/)
    .filter(line => !/^\s*--/.test(line))
    .join("\n");
assert.strictEqual((sqlCode.match(/\?/g) || []).length, 2,
    "La consulta debe tener exactamente dos parámetros ODBC.");
[
    "OrderDocEntry", "OrderNumber", "OrderDate", "DueDate",
    "CustomerCode", "CustomerName", "Origen", "SalesAgent",
    "CustomerState", "LineNumber", "ItemCode", "ItemDescription",
    "FamilyCode", "FamilyName", "OrderedQty", "OpenQty",
    "LineStatus", "LineShipDate", "StockOnHand", "StockByWarehouse"
].forEach(alias => assert(sql.includes('AS "' + alias + '"'),
    "Falta el alias HANA citado: " + alias));
assert(sql.includes('st."AdresType" = \'S\''),
    "El join de dirección debe limitarse a direcciones de envío.");
assert(sql.includes('(l."LineStatus" = \'O\' AND l."OpenQty" > 0)') &&
       sql.includes('OR r."DocDate" BETWEEN ? AND ?'),
    "La consulta debe incluir todo el backlog abierto y el histórico del rango.");
assert(sql.includes('SUM(o."OnHand") AS "OnHand"') &&
       sql.includes('STRING_AGG('),
    "Stock total y desglose por bodega deben calcularse por separado.");
assert(sql.includes("= 'CL' THEN 'Local'") &&
       sql.includes("= 'CE' THEN 'Extranjero'"),
    "El origen CL/CE debe conservar el mapeo validado.");

assert(view.includes('@Url.Action("GetData", "Backlog")'),
    "La vista debe consultar el controlador Backlog.");
assert(view.includes('~/Content/backlog-viz.css'),
    "La vista debe cargar el CSS desde Content.");
assert(view.includes('~/Scripts/App/backlog-viz.js'),
    "La vista debe cargar JavaScript desde Scripts/App.");
assert(view.includes("BacklogViz.setDefaultDateRange"),
    "La vista debe inicializar fechas con calendario local.");
assert(view.includes("latestRequest") && view.includes("requestId !== latestRequest"),
    "Las respuestas antiguas no deben reemplazar datos más recientes.");
[
    "Backlog de Pedidos de Venta", "Solo líneas abiertas",
    "Estado de entrega de pedidos abiertos", "Demanda abierta por item",
    "Volumen de pedidos — tendencia y estacionalidad"
].forEach(label => assert(view.includes(label),
    "La UI entregada perdió el elemento: " + label));
assert.strictEqual((view.match(/<table\b/g) || []).length, 2,
    "La UI debe conservar sus dos tablas.");
assert.strictEqual(
    ["bucketChart", "itemChart", "trendChart", "seasonChart"]
        .filter(id => view.includes('id="' + id + '"')).length,
    4,
    "La UI debe conservar sus cuatro visualizaciones.");

assert(javascript.includes("computeTentativeAvailability(raw)"),
    "La reserva FIFO debe calcularse antes de filtros visuales.");
assert(javascript.includes("let itemSort = { key: 'qty', dir: -1 }"),
    "La tabla de demanda debe iniciar ordenada por cantidad abierta.");
assert(javascript.includes("module.exports = BacklogViz"),
    "La lógica verificable debe exportarse para Node.");
assert(!javascript.includes("today.toISOString()"),
    "La fecha actual no debe depender de UTC.");

[
    'Content Include="App_Data\\backlog.sql"',
    'Content Include="Content\\backlog-viz.css"',
    'Content Include="Scripts\\App\\backlog-viz.js"',
    'Content Include="Views\\Backlog\\Index.cshtml"',
    'Compile Include="Controllers\\BacklogController.cs"'
].forEach(entry => assert(project.includes(entry),
    "El archivo no está registrado en el proyecto: " + entry));

console.log("OK: contrato MVC, SQL, UI, autenticación y publicación de Backlog verificados.");
