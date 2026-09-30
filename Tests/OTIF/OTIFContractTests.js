"use strict";

const assert = require("assert");
const fs = require("fs");
const path = require("path");
const vm = require("vm");
const ui = path.resolve(__dirname, "../../DiamDev.Give.UI");
const read = p => fs.readFileSync(path.join(ui, p), "utf8");
const controller = read("Controllers/OTIFController.cs");
const sql = read("App_Data/otif.sql");
const view = read("Views/OTIF/Index.cshtml");
const js = read("Scripts/App/otif-viz.js");
const css = read("Content/otif-viz.css");
const project = read("DiamDev.Give.UI.csproj");

assert(controller.includes("[Authorize]") && !/\[(Permiso|Seguridad|Role)/.test(controller),
    "La ruta OTIF solo requiere autenticación.");
assert(controller.includes("DateTime.TryParseExact") && controller.includes("MaxRangeDays"));
assert(controller.includes("OdbcConnectionStringBuilder") && controller.includes('connectionBuilder["CS"] = companySchema'));
assert(controller.includes('Server.MapPath("~/App_Data/otif.sql")'));
assert(controller.includes("SetNoStore") && controller.includes("TrySkipIisCustomErrors"));
assert(!controller.includes("new { error = ex"), "No se deben devolver detalles internos.");

const sqlCode = sql.split(/\r?\n/).filter(line => !/^\s*--/.test(line)).join("\n");
assert.strictEqual((sqlCode.match(/\?/g) || []).length, 2,
    "La consulta debe tener dos parámetros ODBC posicionales.");
assert(sqlCode.includes('d."CANCELED" = \'N\'') && sqlCode.includes('l."BaseType" = 17'));
assert(sqlCode.includes('JOIN "CandidateOrders" selected') &&
       sqlCode.includes('JOIN RDR1 r ON r."DocEntry" = o."DocEntry"'),
    "El modo pedido debe recibir todas las líneas de los pedidos candidatos.");
["OrderDocEntry", "LineNumber", "DeliveryDocEntry", "DeliveryLineNumber",
 "DeliveryDate", "DeliveryQty", "OrderedQty", "OpenQty", "LineShipDate"]
    .forEach(alias => assert(sqlCode.includes('AS "' + alias + '"'), "Falta alias " + alias));

assert(view.includes('@Url.Content("~/Content/otif-viz.css') &&
       view.includes('@Url.Content("~/Scripts/App/otif-viz.js'));
assert(view.includes('@Url.Action("GetData", "OTIF")'));
assert(view.includes("Promise.all") && view.includes("thisRequest !== requestId"),
    "Una respuesta antigua no debe reemplazar una consulta reciente.");
assert(view.includes("isoLocal(today)"), "Los presets deben usar la fecha local.");
const inlineScripts = [...view.matchAll(/<script(?:\s[^>]*)?>([\s\S]*?)<\/script>/g)]
    .map(match => match[1]).filter(Boolean);
assert.strictEqual(inlineScripts.length, 1);
new vm.Script(inlineScripts[0], { filename: "Views/OTIF/Index.cshtml:inline" });
assert(view.includes('id="compareToggle"') && view.includes('id="otifMode"') &&
       view.includes('id="fillThreshold"') && view.includes('id="toleranceDays"'));
const viewIds = new Set([...view.matchAll(/\bid="([^"]+)"/g)].map(match => match[1]));
for (const match of js.matchAll(/\$\('([^']+)'\)/g)) {
    assert(viewIds.has(match[1]), "Falta el elemento de UI requerido por JavaScript: " + match[1]);
}
assert(js.includes("deliveredByDeadline / ordered >= f.fillThreshold"));
assert(js.includes("selectRowsForMode"));
assert(css.includes(".viz-root"));
[
    "App_Data\\otif.sql", "Content\\otif-viz.css", "Scripts\\App\\otif-viz.js",
    "Controllers\\OTIFController.cs", "Views\\OTIF\\Index.cshtml"
].forEach(file => assert(project.includes(file), "El proyecto no publica " + file));
for (const name of fs.readdirSync(path.join(ui, "Properties/PublishProfiles")).filter(n => n.endsWith(".pubxml"))) {
    const profile = read("Properties/PublishProfiles/" + name);
    assert(/<ExcludeApp_Data>False<\/ExcludeApp_Data>/i.test(profile),
        name + " excluye App_Data y causaría HTTP 500 en producción.");
}

console.log("OTIFContractTests: OK");
