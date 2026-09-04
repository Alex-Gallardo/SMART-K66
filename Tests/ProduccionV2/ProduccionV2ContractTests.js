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
const css = fs.readFileSync(
    path.join(ui, "Content", "produccion-v2-shared.css"), "utf8");
const project = fs.readFileSync(
    path.join(ui, "DiamDev.Give.UI.csproj"), "utf8");
const publishProfiles = fs.readdirSync(
    path.join(ui, "Properties", "PublishProfiles"))
    .filter(name => name.endsWith(".pubxml"))
    .map(name => ({
        name,
        content: fs.readFileSync(
            path.join(ui, "Properties", "PublishProfiles", name), "utf8")
    }));

assert(controller.includes("[Authorize]"),
    "El dashboard debe requerir una sesión autenticada.");
assert(!/\[Authorize\s*\([^\]]*(Roles|Users)\s*=/.test(controller),
    "Producción v2 no debe exigir roles o usuarios específicos.");
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
    'AS "Turno"',
    'AS "Supervisor"',
    'AS "Motivo de Paro"',
    'AS "Tiempo de Paro"',
    'AS "Cantidad Real Día"',
    'AS "Cantidad Real Rango"',
    'AS "Cantidad Hecha"'
].forEach(alias => assert(sql.includes(alias),
    "Falta la columna SQL requerida: " + alias));
assert(sql.includes('A."UDF1"') && sql.includes('A."UDF2"') &&
       sql.includes('A."UDF3"') && sql.includes('A."UDF4"'),
    "El SQL debe mapear supervisor, turno y datos de paro desde BEAS_ARBZEIT.");
assert(sql.includes('A."DocDate",') && sql.includes('A."UDF2"'),
    "Las horas diarias deben agruparse por turno.");
assert(!sql.includes('UPPER(D."unitMsr") <> \'KG\''),
    "Los recursos cuya producción se mide en KG no deben excluirse.");
assert(!sql.includes('UPPER(D."Dscription") NOT LIKE \'%COPRODUCTO%\''),
    "La producción válida de coproductos de molinos no debe excluirse.");

assert(view.includes('@Url.Action("GetData", "ProduccionV2")'),
    "La vista debe consultar el controlador v2.");
assert(view.includes('~/Content/produccion-v2-shared.css'),
    "La vista debe cargar la hoja de estilos registrada.");
assert(view.includes('~/Scripts/App/produccion-v2-shared.js'),
    "La vista debe cargar la lógica registrada.");
assert(view.includes('wireFilterControls({ onDateChange: load })'),
    "Cambiar fechas debe consultar nuevamente al servidor.");
assert.strictEqual((view.match(/<canvas\b/g) || []).length, 8,
    "La UI entregada contiene ocho gráficas y debe conservarlas.");
assert.strictEqual((view.match(/<th\b[^>]*data-key=/g) || []).length, 24,
    "La tabla por turnos debe conservar sus 24 columnas ordenables.");
[
    "Producto", "Recurso", "Familia", "Categoría", "Estado OT",
    "Horas Disponible / sin registrar", "Cambios de Molde",
    'data-key="Turno"', 'data-key="Supervisor"',
    'data-key="Motivo de Paro"', 'data-key="Tiempo de Paro"',
    'class="th-group"',
    "Cantidad producida en el tiempo", "Detalle por OT / Posición / Recurso"
].forEach(label => assert(view.includes(label),
    "La UI entregada perdió el elemento: " + label));

const viewIds = new Set(
    [...view.matchAll(/\bid="([^"]+)"/g)].map(match => match[1]));
const referencedIds = new Set(
    [...javascript.matchAll(/\$\('([^']+)'\)/g)].map(match => match[1]));
referencedIds.forEach(id => assert(viewIds.has(id),
    "JavaScript referencia un elemento que no existe en la vista: " + id));
assert(css.includes("thead tr:nth-child(2) th") &&
       css.includes("th.th-group") &&
       css.includes(".kpi .value.kpi-paro"),
    "Los estilos deben soportar el encabezado agrupado y el KPI de paro.");

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
    "computeCambiosDeMolde",
    "apportionPlanPorDia",
    "computeHorasPorTurno",
    "desglosarDiaRecurso",
    "enumerarDias"
].forEach(name => assert(javascript.includes(name),
    "Falta la lógica requerida para el dashboard por turnos: " + name));
assert(javascript.includes("const TURNO_HORAS = 12"),
    "La capacidad nominal debe conservar dos turnos de 12 horas.");

[
    'Content Include="App_Data\\dashboard_v2.sql"',
    'Content Include="Content\\produccion-v2-shared.css"',
    'Content Include="Scripts\\App\\produccion-v2-shared.js"',
    'Content Include="Views\\ProduccionV2\\Index.cshtml"',
    'Compile Include="Controllers\\ProduccionV2Controller.cs"'
].forEach(entry => assert(project.includes(entry),
    "El archivo no está registrado en el proyecto: " + entry));
publishProfiles.forEach(profile => assert(
    !/<ExcludeApp_Data>\s*True\s*<\/ExcludeApp_Data>/i.test(profile.content),
    profile.name + " no debe excluir App_Data durante la publicación."));

console.log("OK: contrato MVC, SQL, UI, seguridad y publicación de Producción v2 verificados.");
