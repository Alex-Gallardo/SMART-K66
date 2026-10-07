"use strict";

const assert = require("assert");
const fs = require("fs");
const path = require("path");
const vm = require("vm");
const ui = path.resolve(__dirname, "../../DiamDev.Give.UI");
const root = path.resolve(__dirname, "../..");
const read = p => fs.readFileSync(path.join(ui, p), "utf8");
const controller = read("Controllers/OTIFController.cs");
const sql = read("App_Data/otif.sql");
const view = read("Views/OTIF/Index.cshtml");
const js = read("Scripts/App/otif-viz.js");
const css = read("Content/otif-viz.css");
const project = read("DiamDev.Give.UI.csproj");
const permissionMigration = fs.readFileSync(path.join(root,
    "SqlMigrations/OTIF/01_registrar_permiso_global.sql"), "utf8");

assert(controller.includes("[Authorize]") && !/\[(Permiso|Seguridad|Role)/.test(controller),
    "La ruta OTIF requiere autenticación, sin permiso adicional para abrirla.");
assert(controller.includes('GlobalPermission = "Control.OTIF.VerTodos"') &&
       controller.includes("CustomHelper.Permiso(GlobalPermission)"),
    "La visión global debe depender de un permiso explícito.");
assert(controller.includes("ObtenerPorUsuarioId(CustomHelper.getUserId())") &&
       controller.includes("r.EmpresaId == companyId") &&
       controller.includes("assignments.Count == 0") &&
       controller.includes("JsonError(403"),
    "Las empresas no asignadas deben rechazarse en el servidor.");
assert(controller.indexOf("assignments.Count == 0") < controller.indexOf("new OdbcConnection"),
    "Una empresa no asignada no debe provocar ninguna consulta HANA.");
assert(controller.includes("ResolveAgentCodes(connection, assignments)") &&
       controller.includes("agentCodes.Count == 0") &&
       controller.includes("FROM OSLP") &&
       controller.includes('command.Parameters.Add("@agent" + agentIndex++'),
    "Los vendedores se deben resolver contra SAP y parametrizar en HANA.");
assert(controller.includes("ScopeMarker") && controller.includes("ApplyScope(") &&
       controller.includes('o.\\"SlpCode\\" IN (') &&
       controller.includes("markerAt < 0"),
    "La consulta no debe ejecutarse sin un filtro de alcance válido.");
assert(controller.includes("companyId = UsuarioEmpresaBL.ID_FAES") &&
       controller.includes('case "ESCOCESA"'),
    "La empresa FAES de Usuario_Empresa debe corresponder a Escocesa en OTIF.");
assert(controller.includes("DateTime.TryParseExact") && controller.includes("MaxRangeDays"));
assert(controller.includes("OdbcConnectionStringBuilder") && controller.includes('connectionBuilder["CS"] = companySchema'));
assert(controller.includes('Server.MapPath("~/App_Data/otif.sql")'));
assert(controller.includes("SetNoStore") && controller.includes("TrySkipIisCustomErrors"));
assert(!controller.includes("new { error = ex"), "No se deben devolver detalles internos.");

const sqlCode = sql.split(/\r?\n/).filter(line => !/^\s*--/.test(line)).join("\n");
assert.strictEqual((sqlCode.match(/\?/g) || []).length, 2,
    "El archivo base conserva dos fechas; los vendedores se agregan como parámetros al ejecutarse.");
assert.strictEqual((sqlCode.match(/\/\* OTIF_SCOPE_FILTER \*\//g) || []).length, 1,
    "El filtro de alcance debe estar dentro de CandidateOrders una sola vez.");
assert(sqlCode.indexOf("/* OTIF_SCOPE_FILTER */") < sqlCode.indexOf('"ValidDeliveries" AS'),
    "El filtro debe aplicarse antes de recuperar las entregas.");
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
assert(view.includes('@Url.Action("GetScope", "OTIF")') &&
       view.includes("initializeScope()") &&
       view.includes("scopeLoaded") &&
       view.includes("No tienes vendedores asignados"),
    "La UI debe cargar solo las empresas autorizadas y tratar el alcance vacío.");
assert(!view.includes('<option value="GRACO" selected>') &&
       view.includes("OTIFViz.setRawData([])") &&
       view.includes("r.status === 403"),
    "La UI no debe precargar Graco ni conservar datos tras perder acceso.");
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
assert(js.includes("if (raw.length === 0) {") &&
       /populateGlobalFilterOptions\(\);\s*populateCodeSuggestions\(\);/.test(js),
    "Al vaciar los resultados también deben borrarse los filtros con datos anteriores.");
assert(css.includes(".viz-root"));
assert(permissionMigration.includes("Control.OTIF.VerTodos") &&
       permissionMigration.includes("IF NOT EXISTS") &&
       !permissionMigration.includes("INSERT INTO dbo.Rol_Permiso"),
    "La migración debe registrar el permiso idempotentemente sin otorgarlo a roles.");
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
