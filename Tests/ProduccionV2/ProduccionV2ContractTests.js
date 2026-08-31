"use strict";

const assert = require("assert");
const fs = require("fs");
const path = require("path");

const root = path.resolve(__dirname, "..", "..");
const ui = path.join(root, "DiamDev.Give.UI");

const controller = fs.readFileSync(
    path.join(ui, "Controllers", "ProduccionV2Controller.cs"), "utf8");
const sql = fs.readFileSync(
    path.join(ui, "App_Data", "dashboard_v2.sql"), "utf8");
const view = fs.readFileSync(
    path.join(ui, "Views", "ProduccionV2", "Index.cshtml"), "utf8");
const javascript = fs.readFileSync(
    path.join(ui, "Scripts", "App", "produccion-v2-shared.js"), "utf8");
const project = fs.readFileSync(
    path.join(ui, "DiamDev.Give.UI.csproj"), "utf8");

assert(controller.includes("[Authorize]"),
    "El dashboard debe requerir una sesión autenticada.");
assert(controller.includes("DateTime.TryParseExact") &&
       controller.includes("< MaxRangeDays"),
    "El controlador debe validar formato, orden y tamaño del rango.");
assert(controller.includes("OdbcConnectionStringBuilder") &&
       controller.includes('connectionBuilder["CS"] = companySchema'),
    "El schema debe cambiarse mediante el constructor ODBC.");
assert(controller.includes("TryGetCompanySchema") &&
       controller.includes("companySchema = null") &&
       controller.includes("return false;"),
    "Una empresa desconocida debe rechazarse, no caer en GRACO.");
assert(controller.includes('Server.MapPath("~/App_Data/dashboard_v2.sql")'),
    "La consulta debe residir dentro de App_Data.");
assert(controller.includes("Trace.TraceError") &&
       !controller.includes("return Json(new { error = ex"),
    "Los detalles de una excepción no deben devolverse al navegador.");

assert.strictEqual((sql.match(/\?/g) || []).length, 2,
    "La consulta debe conservar exactamente dos parámetros posicionales.");
assert.strictEqual(
    (sql.match(/AND POS\."ItemCode" = D\."ItemCode"/g) || []).length,
    3,
    "Los ingresos diario, de rango y total deben filtrar el item planificado.");
[
    'AS "FamilyCode"',
    'AS "FamilyName"',
    'AS "TipoItem"',
    'AS "Cantidad Real Día"',
    'AS "Cantidad Real Rango"',
    'AS "Cantidad Hecha"'
].forEach(alias => assert(sql.includes(alias),
    "Falta la columna SQL requerida: " + alias));

assert(view.includes('@Url.Action("GetData", "ProduccionV2")'),
    "La vista debe consultar el controlador v2.");
assert(view.includes('~/Content/produccion-v2-shared.css'),
    "La vista debe cargar la hoja de estilos registrada.");
assert(view.includes('~/Scripts/App/produccion-v2-shared.js'),
    "La vista debe cargar la lógica registrada.");
assert.strictEqual((view.match(/<canvas\b/g) || []).length, 8,
    "La UI entregada contiene ocho gráficas y debe conservarlas.");
[
    "Producto", "Recurso", "Familia", "Categoría", "Estado OT",
    "Cantidad producida en el tiempo", "Detalle por OT / Posición / Recurso"
].forEach(label => assert(view.includes(label),
    "La UI entregada perdió el elemento: " + label));

const fillStart = javascript.indexOf("function fillTable(rows)");
const fillEnd = javascript.indexOf("// ---- tabla:", fillStart);
assert(fillStart >= 0 && fillEnd > fillStart,
    "No se encontró el render de la tabla.");
const fillTable = javascript.substring(fillStart, fillEnd);
assert(!fillTable.includes("innerHTML"),
    "La tabla no debe insertar valores SAP mediante innerHTML.");
assert(fillTable.includes("textContent"),
    "La tabla debe insertar valores SAP como texto.");
assert(javascript.includes("aggregateDailyQuantity"),
    "Las gráficas deben usar el agregador de cantidad diaria.");

[
    'Content Include="App_Data\\dashboard_v2.sql"',
    'Content Include="Content\\produccion-v2-shared.css"',
    'Content Include="Scripts\\App\\produccion-v2-shared.js"',
    'Content Include="Views\\ProduccionV2\\Index.cshtml"',
    'Compile Include="Controllers\\ProduccionV2Controller.cs"'
].forEach(entry => assert(project.includes(entry),
    "El archivo no está registrado en el proyecto: " + entry));

console.log("OK: contrato MVC, SQL, UI, seguridad y publicación de Producción v2 verificados.");
